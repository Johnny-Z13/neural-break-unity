using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    /// Custom input handling: Left/Right to navigate, Fire to confirm.
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

        [Header("Input Settings")]
        [SerializeField] private float _inputRepeatDelay = 0.3f;

        // Current selection state
        private List<UpgradeCard> _activeCards = new List<UpgradeCard>();
        private List<UpgradeDefinition> _currentOptions;
        private int _selectedIndex = 0;
        private bool _selectionLocked = false; // Prevent double-confirm
        private float _lastInputTime;

        // Manager references
        private UpgradePoolManager _poolManager;
        private PermanentUpgradeManager _upgradeManager;
        private UpgradeSelectionBuilder _cardBuilder;

        // No upgrades state
        private bool _noUpgradesMode = false;

        protected override void Awake()
        {
            base.Awake();
            EventBus.Subscribe<UpgradeSelectionStartedEvent>(OnUpgradeSelectionStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<UpgradeSelectionStartedEvent>(OnUpgradeSelectionStarted);
        }

        protected override void Update()
        {
            // Skip base Update (we handle input ourselves)
            if (!_isVisible) return;

            if (_noUpgradesMode)
            {
                HandleNoUpgradesInput();
                return;
            }

            if (_selectionLocked || _activeCards.Count == 0) return;

            HandleNavigationInput();
            HandleConfirmInput();
        }

        private void HandleNavigationInput()
        {
            int direction = 0;

            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            // Keyboard: Arrow keys or A/D
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                    direction = -1;
                else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                    direction = 1;
            }

            // Gamepad: D-pad or left stick
            if (gamepad != null && direction == 0)
            {
                if (gamepad.dpad.left.wasPressedThisFrame)
                    direction = -1;
                else if (gamepad.dpad.right.wasPressedThisFrame)
                    direction = 1;

                // Left stick with repeat delay
                if (direction == 0 && Time.unscaledTime - _lastInputTime > _inputRepeatDelay)
                {
                    float stickX = gamepad.leftStick.x.ReadValue();
                    if (stickX < -0.5f)
                    {
                        direction = -1;
                        _lastInputTime = Time.unscaledTime;
                    }
                    else if (stickX > 0.5f)
                    {
                        direction = 1;
                        _lastInputTime = Time.unscaledTime;
                    }
                }
            }

            if (direction != 0)
            {
                ChangeSelection(direction);
            }
        }

        private void HandleConfirmInput()
        {
            bool confirmPressed = false;

            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            var mouse = Mouse.current;

            // Keyboard: Space, Enter
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame ||
                    keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame)
                {
                    confirmPressed = true;
                }
            }

            // Mouse: Left click confirms current selection
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                // Check if clicking on a card - if so, select that card first
                int clickedIndex = GetCardUnderMouse();
                if (clickedIndex >= 0)
                {
                    if (clickedIndex != _selectedIndex)
                    {
                        SetSelection(clickedIndex);
                    }
                    confirmPressed = true;
                }
                else
                {
                    // Clicked outside cards - just confirm current selection
                    confirmPressed = true;
                }
            }

            // Gamepad: A button (buttonSouth) or right trigger
            if (gamepad != null)
            {
                if (gamepad.buttonSouth.wasPressedThisFrame ||
                    gamepad.rightTrigger.wasPressedThisFrame)
                {
                    confirmPressed = true;
                }
            }

            if (confirmPressed && _selectedIndex >= 0 && _selectedIndex < _activeCards.Count)
            {
                ConfirmSelection();
            }
        }

        private int GetCardUnderMouse()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return -1;

            var pointerData = new PointerEventData(eventSystem)
            {
                position = Mouse.current.position.ReadValue()
            };

            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                for (int i = 0; i < _activeCards.Count; i++)
                {
                    if (_activeCards[i] != null &&
                        (result.gameObject == _activeCards[i].gameObject ||
                         result.gameObject.transform.IsChildOf(_activeCards[i].transform)))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private void ChangeSelection(int direction)
        {
            int newIndex = _selectedIndex + direction;
            newIndex = Mathf.Clamp(newIndex, 0, _activeCards.Count - 1);

            if (newIndex != _selectedIndex)
            {
                SetSelection(newIndex);
            }
        }

        private void SetSelection(int index)
        {
            // Deselect previous
            if (_selectedIndex >= 0 && _selectedIndex < _activeCards.Count)
            {
                var prevCard = _activeCards[_selectedIndex];
                if (prevCard != null)
                {
                    prevCard.SetHighlighted(false);
                }
            }

            _selectedIndex = index;

            // Select new
            if (_selectedIndex >= 0 && _selectedIndex < _activeCards.Count)
            {
                var newCard = _activeCards[_selectedIndex];
                if (newCard != null)
                {
                    newCard.SetHighlighted(true);
                    newCard.PlayHoverEffect();

                    // Also set EventSystem selection for visual consistency
                    if (newCard.Button != null && EventSystem.current != null)
                    {
                        EventSystem.current.SetSelectedGameObject(newCard.Button.gameObject);
                    }
                }
            }
        }

        private void ConfirmSelection()
        {
            if (_selectionLocked) return;
            if (_selectedIndex < 0 || _selectedIndex >= _currentOptions.Count) return;

            _selectionLocked = true;

            var selectedUpgrade = _currentOptions[_selectedIndex];
            var selectedCard = _activeCards[_selectedIndex];

            Debug.Log($"[UpgradeSelectionScreen] Confirming selection: {selectedUpgrade.displayName}");

            // Play selection effect
            if (selectedCard != null)
            {
                selectedCard.PlaySelectEffect();
            }

            // Apply upgrade after short delay for visual feedback
            StartCoroutine(ApplySelectionDelayed(selectedUpgrade, 0.15f));
        }

        private System.Collections.IEnumerator ApplySelectionDelayed(UpgradeDefinition upgrade, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            EnsureManagerReferences();

            // Add upgrade to permanent manager
            if (_upgradeManager != null)
            {
                _upgradeManager.AddUpgrade(upgrade);
                Debug.Log($"[UpgradeSelectionScreen] Applied upgrade: {upgrade.displayName}");
            }
            else
            {
                Debug.LogError("[UpgradeSelectionScreen] PermanentUpgradeManager.Instance is null!");
            }

            // Publish event
            EventBus.Publish(new UpgradeSelectedEvent { selected = upgrade });

            // Hide screen
            Hide();
        }

        private void HandleNoUpgradesInput()
        {
            bool firePressed = false;

            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            var mouse = Mouse.current;

            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame ||
                    keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame)
                {
                    firePressed = true;
                }
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                firePressed = true;
            }

            if (gamepad != null)
            {
                if (gamepad.buttonSouth.wasPressedThisFrame ||
                    gamepad.buttonEast.wasPressedThisFrame)
                {
                    firePressed = true;
                }
            }

            if (firePressed)
            {
                _noUpgradesMode = false;
                EventBus.Publish(new UpgradeSelectedEvent { selected = null });
                Hide();
            }
        }

        private void EnsureManagerReferences()
        {
            if (_poolManager == null)
                _poolManager = UpgradePoolManager.Instance;
            if (_upgradeManager == null)
                _upgradeManager = PermanentUpgradeManager.Instance;
        }

        private void OnUpgradeSelectionStarted(UpgradeSelectionStartedEvent evt)
        {
            Debug.Log("[UpgradeSelectionScreen] OnUpgradeSelectionStarted called");

            EnsureManagerReferences();

            if (_poolManager != null)
            {
                _currentOptions = _poolManager.GenerateSelection();
                Debug.Log($"[UpgradeSelectionScreen] Generated {_currentOptions.Count} options");
            }
            else
            {
                Debug.LogWarning("[UpgradeSelectionScreen] UpgradePoolManager is null!");
                _currentOptions = evt.options;
            }

            GenerateCards();
        }

        protected override void OnShow()
        {
            base.OnShow();

            _selectionLocked = false;
            _noUpgradesMode = false;
            _selectedIndex = 0;

            if (_titleText != null)
            {
                int level = GameManager.Instance != null ? GameManager.Instance.Stats.level : 1;
                _titleText.text = $"LEVEL {level} COMPLETE";
            }

            if (_subtitleText != null)
            {
                _subtitleText.text = "CHOOSE YOUR UPGRADE";
                _subtitleText.color = UITheme.TextSecondary;
            }
        }

        protected override void OnHide()
        {
            base.OnHide();
            ClearCards();
            _selectionLocked = false;
            _noUpgradesMode = false;
        }

        private void GenerateCards()
        {
            ClearCards();

            if (_currentOptions == null || _currentOptions.Count == 0)
            {
                Debug.LogError("[UpgradeSelectionScreen] No upgrade options!");
                _noUpgradesMode = true;

                if (_subtitleText != null)
                {
                    _subtitleText.text = "NO UPGRADES AVAILABLE\nPress Fire to continue...";
                    _subtitleText.color = Color.red;
                }
                return;
            }

            for (int i = 0; i < _currentOptions.Count; i++)
            {
                var upgrade = _currentOptions[i];
                UpgradeCard card = CreateCard(upgrade, i);
                _activeCards.Add(card);

                var animator = card.GetComponent<UpgradeCardAnimator>();
                if (animator != null)
                {
                    animator.PlayEntranceAnimation(i * 0.1f);
                }
            }

            // Select first card after entrance animation
            StartCoroutine(DelayedInitialSelection());
        }

        private System.Collections.IEnumerator DelayedInitialSelection()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            _selectedIndex = 0;
            SetSelection(0);
        }

        private UpgradeCard CreateCard(UpgradeDefinition upgrade, int index)
        {
            // Ensure card container exists
            EnsureCardContainer();

            UpgradeCard card;

            if (_cardPrefab != null)
            {
                card = Instantiate(_cardPrefab, _cardContainer);
            }
            else
            {
                EnsureCardBuilder();
                card = _cardBuilder.BuildCard(_cardContainer);
                if (card == null)
                {
                    Debug.LogError("[UpgradeSelectionScreen] Failed to build card!");
                    return null;
                }
                card.gameObject.name = $"Card_{upgrade.upgradeId}";
            }

            // Initialize without click callback - we handle confirmation ourselves
            card.Initialize(upgrade, null);

            return card;
        }

        private void EnsureCardContainer()
        {
            if (_cardContainer != null) return;

            // Try to find it by name
            var containerObj = transform.Find("ContentPanel/CardContainer");
            if (containerObj != null)
            {
                _cardContainer = containerObj;
                return;
            }

            // Create it if missing
            Debug.LogWarning("[UpgradeSelectionScreen] CardContainer not found, creating dynamically");
            var containerGO = new GameObject("CardContainer", typeof(RectTransform));
            containerGO.transform.SetParent(transform, false);
            var rect = containerGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1200f, 520f);

            var hlg = containerGO.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hlg.spacing = 40f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            _cardContainer = containerGO.transform;
        }

        private void EnsureCardBuilder()
        {
            if (_cardBuilder != null) return;

            if (_fontAsset == null)
            {
                // Try multiple fallback sources
                if (_titleText != null)
                    _fontAsset = _titleText.font;
                else if (_subtitleText != null)
                    _fontAsset = _subtitleText.font;

                if (_fontAsset == null)
                {
                    // Try common TMP font paths
                    _fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                }

                if (_fontAsset == null)
                {
                    _fontAsset = Resources.Load<TMP_FontAsset>("Fonts/LiberationSans SDF");
                }

                if (_fontAsset == null)
                {
                    // Last resort: find any TMP text in scene
                    var anyText = FindFirstObjectByType<TextMeshProUGUI>();
                    if (anyText != null)
                        _fontAsset = anyText.font;
                }

                if (_fontAsset == null)
                {
                    Debug.LogError("[UpgradeSelectionScreen] Could not find any TMP font asset!");
                }
            }

            _cardBuilder = new UpgradeSelectionBuilder(_fontAsset);
        }

        private void ClearCards()
        {
            foreach (var card in _activeCards)
            {
                if (card != null && card.gameObject != null)
                    Destroy(card.gameObject);
            }
            _activeCards.Clear();
            _selectedIndex = 0;
        }

        #region Debug
        [ContextMenu("Debug: Show Screen")]
        private void DebugShowScreen()
        {
            _currentOptions = new List<UpgradeDefinition>();
            Show();
        }
        #endregion
    }
}
