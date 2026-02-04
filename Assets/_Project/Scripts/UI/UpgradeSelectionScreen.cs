using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using NeuralBreak.UI.Builders;
using System.Collections.Generic;
using Z13.Core;

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
        [SerializeField] private Transform m_cardContainer;
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private TextMeshProUGUI m_subtitleText;

        [Header("Card Prefab")]
        [SerializeField] private UpgradeCard m_cardPrefab;

        [Header("Font (for programmatic cards)")]
        [SerializeField] private TMP_FontAsset m_fontAsset;

        [Header("Input Settings")]
        [SerializeField] private float m_inputRepeatDelay = 0.3f;

        // Current selection state
        private List<UpgradeCard> m_activeCards = new List<UpgradeCard>();
        private List<UpgradeDefinition> m_currentOptions;
        private int m_selectedIndex = 0;
        private bool m_selectionLocked = false; // Prevent double-confirm
        private float m_lastInputTime;

        // Manager references
        private UpgradePoolManager m_poolManager;
        private PermanentUpgradeManager m_upgradeManager;
        private UpgradeSelectionBuilder m_cardBuilder;

        // No upgrades state
        private bool m_noUpgradesMode = false;

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
            if (!m_isVisible) return;

            if (m_noUpgradesMode)
            {
                HandleNoUpgradesInput();
                return;
            }

            if (m_selectionLocked || m_activeCards.Count == 0) return;

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
                if (direction == 0 && Time.unscaledTime - m_lastInputTime > m_inputRepeatDelay)
                {
                    float stickX = gamepad.leftStick.x.ReadValue();
                    if (stickX < -0.5f)
                    {
                        direction = -1;
                        m_lastInputTime = Time.unscaledTime;
                    }
                    else if (stickX > 0.5f)
                    {
                        direction = 1;
                        m_lastInputTime = Time.unscaledTime;
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
                    if (clickedIndex != m_selectedIndex)
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

            if (confirmPressed && m_selectedIndex >= 0 && m_selectedIndex < m_activeCards.Count)
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
                for (int i = 0; i < m_activeCards.Count; i++)
                {
                    if (m_activeCards[i] != null &&
                        (result.gameObject == m_activeCards[i].gameObject ||
                         result.gameObject.transform.IsChildOf(m_activeCards[i].transform)))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private void ChangeSelection(int direction)
        {
            int newIndex = m_selectedIndex + direction;
            newIndex = Mathf.Clamp(newIndex, 0, m_activeCards.Count - 1);

            if (newIndex != m_selectedIndex)
            {
                SetSelection(newIndex);
            }
        }

        private void SetSelection(int index)
        {
            // Deselect previous
            if (m_selectedIndex >= 0 && m_selectedIndex < m_activeCards.Count)
            {
                var prevCard = m_activeCards[m_selectedIndex];
                if (prevCard != null)
                {
                    prevCard.SetHighlighted(false);
                }
            }

            m_selectedIndex = index;

            // Select new
            if (m_selectedIndex >= 0 && m_selectedIndex < m_activeCards.Count)
            {
                var newCard = m_activeCards[m_selectedIndex];
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
            if (m_selectionLocked) return;
            if (m_selectedIndex < 0 || m_selectedIndex >= m_currentOptions.Count) return;

            m_selectionLocked = true;

            var selectedUpgrade = m_currentOptions[m_selectedIndex];
            var selectedCard = m_activeCards[m_selectedIndex];

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
            if (m_upgradeManager != null)
            {
                m_upgradeManager.AddUpgrade(upgrade);
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
                m_noUpgradesMode = false;
                EventBus.Publish(new UpgradeSelectedEvent { selected = null });
                Hide();
            }
        }

        private void EnsureManagerReferences()
        {
            if (m_poolManager == null)
                m_poolManager = UpgradePoolManager.Instance;
            if (m_upgradeManager == null)
                m_upgradeManager = PermanentUpgradeManager.Instance;
        }

        private void OnUpgradeSelectionStarted(UpgradeSelectionStartedEvent evt)
        {
            Debug.Log("[UpgradeSelectionScreen] OnUpgradeSelectionStarted called");

            EnsureManagerReferences();

            if (m_poolManager != null)
            {
                m_currentOptions = m_poolManager.GenerateSelection();
                Debug.Log($"[UpgradeSelectionScreen] Generated {m_currentOptions.Count} options");
            }
            else
            {
                Debug.LogWarning("[UpgradeSelectionScreen] UpgradePoolManager is null!");
                m_currentOptions = evt.options;
            }

            GenerateCards();
        }

        protected override void OnShow()
        {
            base.OnShow();

            m_selectionLocked = false;
            m_noUpgradesMode = false;
            m_selectedIndex = 0;

            if (m_titleText != null)
            {
                int level = GameManager.Instance != null ? GameManager.Instance.Stats.level : 1;
                m_titleText.text = $"LEVEL {level} COMPLETE";
            }

            if (m_subtitleText != null)
            {
                m_subtitleText.text = "CHOOSE YOUR UPGRADE";
                m_subtitleText.color = UITheme.TextSecondary;
            }
        }

        protected override void OnHide()
        {
            base.OnHide();
            ClearCards();
            m_selectionLocked = false;
            m_noUpgradesMode = false;
        }

        private void GenerateCards()
        {
            ClearCards();

            if (m_currentOptions == null || m_currentOptions.Count == 0)
            {
                Debug.LogError("[UpgradeSelectionScreen] No upgrade options!");
                m_noUpgradesMode = true;

                if (m_subtitleText != null)
                {
                    m_subtitleText.text = "NO UPGRADES AVAILABLE\nPress Fire to continue...";
                    m_subtitleText.color = Color.red;
                }
                return;
            }

            for (int i = 0; i < m_currentOptions.Count; i++)
            {
                var upgrade = m_currentOptions[i];
                UpgradeCard card = CreateCard(upgrade, i);
                m_activeCards.Add(card);

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
            m_selectedIndex = 0;
            SetSelection(0);
        }

        private UpgradeCard CreateCard(UpgradeDefinition upgrade, int index)
        {
            // Ensure card container exists
            EnsureCardContainer();

            UpgradeCard card;

            if (m_cardPrefab != null)
            {
                card = Instantiate(m_cardPrefab, m_cardContainer);
            }
            else
            {
                EnsureCardBuilder();
                card = m_cardBuilder.BuildCard(m_cardContainer);
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
            if (m_cardContainer != null) return;

            // Try to find it by name
            var containerObj = transform.Find("ContentPanel/CardContainer");
            if (containerObj != null)
            {
                m_cardContainer = containerObj;
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

            m_cardContainer = containerGO.transform;
        }

        private void EnsureCardBuilder()
        {
            if (m_cardBuilder != null) return;

            if (m_fontAsset == null)
            {
                // Try multiple fallback sources
                if (m_titleText != null)
                    m_fontAsset = m_titleText.font;
                else if (m_subtitleText != null)
                    m_fontAsset = m_subtitleText.font;

                if (m_fontAsset == null)
                {
                    // Try common TMP font paths
                    m_fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                }

                if (m_fontAsset == null)
                {
                    m_fontAsset = Resources.Load<TMP_FontAsset>("Fonts/LiberationSans SDF");
                }

                if (m_fontAsset == null)
                {
                    // Last resort: find any TMP text in scene
                    var anyText = FindFirstObjectByType<TextMeshProUGUI>();
                    if (anyText != null)
                        m_fontAsset = anyText.font;
                }

                if (m_fontAsset == null)
                {
                    Debug.LogError("[UpgradeSelectionScreen] Could not find any TMP font asset!");
                }
            }

            m_cardBuilder = new UpgradeSelectionBuilder(m_fontAsset);
        }

        private void ClearCards()
        {
            foreach (var card in m_activeCards)
            {
                if (card != null && card.gameObject != null)
                    Destroy(card.gameObject);
            }
            m_activeCards.Clear();
            m_selectedIndex = 0;
        }

        #region Debug
        [ContextMenu("Debug: Show Screen")]
        private void DebugShowScreen()
        {
            m_currentOptions = new List<UpgradeDefinition>();
            Show();
        }
        #endregion
    }
}
