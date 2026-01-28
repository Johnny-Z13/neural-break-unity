using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using NeuralBreak.UI.Builders;
using System.Collections.Generic;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Screen for selecting permanent upgrades between levels.
    /// Displays 3 cards and waits for player selection.
    /// </summary>
    public class UpgradeSelectionScreen : ScreenBase
    {
        [Header("Upgrade Selection")]
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _subtitleText;

        [Header("Card Prefab")]
        [SerializeField] private UpgradeCard _cardPrefab;

        [Header("Font (for programmatic cards)")]
        [SerializeField] private TMP_FontAsset _fontAsset;

        // Current selection
        private List<UpgradeCard> _activeCards = new List<UpgradeCard>();
        private List<UpgradeDefinition> _currentOptions;
        private UpgradePoolManager _poolManager;
        private PermanentUpgradeManager _upgradeManager;
        private UpgradeSelectionBuilder _cardBuilder;

        protected override void Awake()
        {
            base.Awake();

            // Subscribe to events
            EventBus.Subscribe<UpgradeSelectionStartedEvent>(OnUpgradeSelectionStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<UpgradeSelectionStartedEvent>(OnUpgradeSelectionStarted);
        }

        /// <summary>
        /// Ensure manager references are valid (they may not exist when Start() runs).
        /// </summary>
        private void EnsureManagerReferences()
        {
            if (_poolManager == null)
            {
                _poolManager = UpgradePoolManager.Instance;
            }
            if (_upgradeManager == null)
            {
                _upgradeManager = PermanentUpgradeManager.Instance;
            }
        }

        private void OnUpgradeSelectionStarted(UpgradeSelectionStartedEvent evt)
        {
            Debug.Log($"[UpgradeSelectionScreen] OnUpgradeSelectionStarted called");

            // Ensure we have manager references
            EnsureManagerReferences();

            // Generate upgrade options
            if (_poolManager != null)
            {
                _currentOptions = _poolManager.GenerateSelection();
                Debug.Log($"[UpgradeSelectionScreen] Generated {_currentOptions.Count} upgrade options from PoolManager");
            }
            else
            {
                Debug.LogWarning("[UpgradeSelectionScreen] UpgradePoolManager.Instance is null! Using provided options.");
                _currentOptions = evt.options;
            }

            // Generate the upgrade cards now that we have options
            // (The screen is already shown by UIManager via GameStateChangedEvent)
            GenerateCards();
        }

        protected override void OnShow()
        {
            base.OnShow();

            // Update title
            if (_titleText != null)
            {
                int level = GameManager.Instance != null ? GameManager.Instance.Stats.level : 1;
                _titleText.text = $"LEVEL {level} COMPLETE";
            }

            if (_subtitleText != null)
            {
                _subtitleText.text = "CHOOSE YOUR UPGRADE";
                _subtitleText.color = UITheme.TextSecondary; // Reset color in case it was red
            }

            // Don't generate cards here - wait for OnUpgradeSelectionStarted to provide options
            // Cards will be generated when _currentOptions is populated
        }

        protected override void OnHide()
        {
            base.OnHide();

            // Clear cards
            ClearCards();
        }

        private void GenerateCards()
        {
            // Clear existing cards
            ClearCards();

            if (_currentOptions == null || _currentOptions.Count == 0)
            {
                Debug.LogError("[UpgradeSelectionScreen] No upgrade options available! Please run: Neural Break > Create Upgrades > Create Starter Pack");

                // Show error message to user
                if (_subtitleText != null)
                {
                    _subtitleText.text = "NO UPGRADES AVAILABLE\nRun: Neural Break > Create Upgrades > Create Starter Pack\nPress Fire to continue...";
                    _subtitleText.color = Color.red;
                }

                // Auto-close after showing error
                StartCoroutine(AutoCloseOnNoUpgrades());
                return;
            }

            // Create cards for each option with staggered entrance
            for (int i = 0; i < _currentOptions.Count; i++)
            {
                var upgrade = _currentOptions[i];
                UpgradeCard card = CreateCard(upgrade, i);
                _activeCards.Add(card);

                // Set staggered entrance delay
                var animator = card.GetComponent<UpgradeCardAnimator>();
                if (animator != null)
                {
                    animator.PlayEntranceAnimation(i * 0.1f); // 0.1s delay between cards
                }
            }

            // Select first card for gamepad/keyboard navigation (after a delay)
            if (_activeCards.Count > 0 && _activeCards[0].Button != null)
            {
                _firstSelected = _activeCards[0].Button;
                StartCoroutine(DelayedSelectFirst());
            }
        }

        private System.Collections.IEnumerator DelayedSelectFirst()
        {
            // Wait for cards to animate in
            yield return new WaitForSecondsRealtime(0.6f);
            SelectFirstElement();
        }

        private UpgradeCard CreateCard(UpgradeDefinition upgrade, int index)
        {
            UpgradeCard card;

            if (_cardPrefab != null)
            {
                // Use prefab
                card = Instantiate(_cardPrefab, _cardContainer);
            }
            else
            {
                // Create programmatically using builder
                EnsureCardBuilder();
                card = _cardBuilder.BuildCard(_cardContainer);
                card.gameObject.name = $"Card_{upgrade.upgradeId}";
            }

            // Initialize card
            card.Initialize(upgrade, () => OnCardSelected(upgrade));

            return card;
        }

        /// <summary>
        /// Ensure card builder exists, creating it if needed.
        /// </summary>
        private void EnsureCardBuilder()
        {
            if (_cardBuilder != null) return;

            // Try to find font asset if not assigned
            if (_fontAsset == null)
            {
                // Try to get from existing text component
                if (_titleText != null)
                {
                    _fontAsset = _titleText.font;
                }
                else if (_subtitleText != null)
                {
                    _fontAsset = _subtitleText.font;
                }
                else
                {
                    // Try to load default TMP font
                    _fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                    if (_fontAsset == null)
                    {
                        // Fallback: find any TMP font in scene
                        var anyText = FindFirstObjectByType<TextMeshProUGUI>();
                        if (anyText != null)
                        {
                            _fontAsset = anyText.font;
                        }
                    }
                }
            }

            _cardBuilder = new UpgradeSelectionBuilder(_fontAsset);
            Debug.Log($"[UpgradeSelectionScreen] Created card builder with font: {(_fontAsset != null ? _fontAsset.name : "null")}");
        }

        private void OnCardSelected(UpgradeDefinition upgrade)
        {
            Debug.Log($"[UpgradeSelectionScreen] Card selected: {upgrade.displayName}");

            // Ensure we have manager reference
            EnsureManagerReferences();

            // Add upgrade to permanent manager
            if (_upgradeManager != null)
            {
                _upgradeManager.AddUpgrade(upgrade);
                Debug.Log($"[UpgradeSelectionScreen] Added upgrade to PermanentUpgradeManager");
            }
            else
            {
                Debug.LogError("[UpgradeSelectionScreen] PermanentUpgradeManager.Instance is null! Upgrade will not persist.");
            }

            // Publish event
            EventBus.Publish(new UpgradeSelectedEvent
            {
                selected = upgrade
            });

            // Hide screen
            Hide();
        }

        private void ClearCards()
        {
            foreach (var card in _activeCards)
            {
                if (card != null && card.gameObject != null)
                {
                    Destroy(card.gameObject);
                }
            }
            _activeCards.Clear();
        }

        private System.Collections.IEnumerator AutoCloseOnNoUpgrades()
        {
            // Wait for fire button (gamepad/keyboard) using New Input System
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var gamepad = UnityEngine.InputSystem.Gamepad.current;

            while (true)
            {
                bool firePressed = false;

                // Check keyboard
                if (keyboard != null)
                {
                    if (keyboard.spaceKey.wasPressedThisFrame ||
                        keyboard.enterKey.wasPressedThisFrame ||
                        keyboard.numpadEnterKey.wasPressedThisFrame)
                    {
                        firePressed = true;
                    }
                }

                // Check gamepad
                if (gamepad != null)
                {
                    if (gamepad.buttonSouth.wasPressedThisFrame || // A button (Xbox) / Cross (PS)
                        gamepad.buttonEast.wasPressedThisFrame)    // B button (Xbox) / Circle (PS)
                    {
                        firePressed = true;
                    }
                }

                if (firePressed)
                {
                    break;
                }

                yield return null;
            }

            // Publish event to let GameManager know we're done (even with no selection)
            EventBus.Publish(new UpgradeSelectedEvent
            {
                selected = null // No upgrade selected
            });

            // Close screen
            Hide();
        }

        #region Debug

        [ContextMenu("Debug: Show Screen")]
        private void DebugShowScreen()
        {
            // Generate fake upgrades for testing
            _currentOptions = new List<UpgradeDefinition>();
            Show();
        }

        #endregion
    }
}
