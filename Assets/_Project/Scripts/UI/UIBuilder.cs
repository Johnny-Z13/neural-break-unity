using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using NeuralBreak.UI.Builders;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Main UI Builder coordinator. Delegates screen building to specialized builders.
    /// Attach to MainCanvas to build all UI at runtime.
    /// Refactored from 766 LOC monolith into modular builder pattern.
    /// </summary>
    public class UIBuilder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIManager m_uiManager;
        [SerializeField] private HUDController m_hudController;

        [Header("Font")]
        [SerializeField] private TMP_FontAsset m_fontAsset;

        [Header("Colors (Uses UITheme if not overridden)")]
        [SerializeField] private bool m_useThemeColors = true;

        [SerializeField] private Color m_customBackgroundColor = new Color(0.05f, 0.05f, 0.1f, 0.9f);
        [SerializeField] private Color m_customPrimaryColor = new Color(0f, 1f, 1f, 1f);
        [SerializeField] private Color m_customAccentColor = new Color(1f, 0.3f, 0.5f, 1f);
        [SerializeField] private Color m_customTextColor = Color.white;

        // Specialized builders
        private HUDBuilderArcade m_hudBuilder;
        private StartScreenBuilder m_startScreenBuilder;
        private PauseMenuBuilder m_pauseMenuBuilder;
        private GameOverScreenBuilder m_gameOverScreenBuilder;
        private UpgradeSelectionBuilder m_upgradeSelectionBuilder;

        // Built references
        private GameObject m_hudRoot;
        private StartScreen m_startScreen;
        private PauseScreen m_pauseScreen;
        private GameOverScreen m_gameOverScreen;
        private UpgradeSelectionScreen m_upgradeSelectionScreen;

        private void Awake()
        {
            // Ensure EventSystem exists
            EnsureEventSystem();

            // Load font if not assigned
            LoadFont();

            Debug.Log($"[UIBuilder] Building UI with font: {(m_fontAsset != null ? m_fontAsset.name : "NULL")}");

            // Initialize builders
            InitializeBuilders();

            // Build all UI
            BuildAllUI();

            // Wire references
            WireReferences();

            Debug.Log("[UIBuilder] UI Build complete!");
        }

        #region Font Loading

        private void LoadFont()
        {
            if (m_fontAsset != null)
            {
                Debug.Log($"[UIBuilder] Font already assigned: {m_fontAsset.name}");
                return;
            }

            Debug.Log("[UIBuilder] No font asset assigned, attempting to load...");

            // Try Resources folder
            m_fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/Lato SDF");
            if (m_fontAsset != null)
            {
                Debug.Log("[UIBuilder] Loaded font from Resources");
                return;
            }

            // Try AssetDatabase (Editor only)
            #if UNITY_EDITOR
            m_fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Feel/MMTools/Demos/MMTween/Fonts/Lato/SDF/Lato SDF.asset");
            if (m_fontAsset != null)
            {
                Debug.Log("[UIBuilder] Loaded font from AssetDatabase");
                return;
            }
            #endif

            // Try TMP default font
            m_fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (m_fontAsset != null) return;

            // Try TMP Settings
            try
            {
                if (TMP_Settings.instance != null)
                {
                    m_fontAsset = TMP_Settings.defaultFontAsset;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UIBuilder] TMP_Settings access failed: {e.Message}");
            }

            if (m_fontAsset == null)
            {
                Debug.LogError("[UIBuilder] CRITICAL: No font found! Text will not render!");
            }
        }

        #endregion

        #region Builder Initialization

        private void InitializeBuilders()
        {
            m_hudBuilder = new HUDBuilderArcade(m_fontAsset, m_useThemeColors);
            m_startScreenBuilder = new StartScreenBuilder(m_fontAsset, m_useThemeColors);
            m_pauseMenuBuilder = new PauseMenuBuilder(m_fontAsset, m_useThemeColors);
            m_gameOverScreenBuilder = new GameOverScreenBuilder(m_fontAsset, m_useThemeColors);
            m_upgradeSelectionBuilder = new UpgradeSelectionBuilder(m_fontAsset);
        }

        #endregion

        #region UI Building

        private void BuildAllUI()
        {
            // Build HUD
            m_hudRoot = m_hudBuilder.BuildHUD(transform);

            // Build Screens
            var startScreenGO = m_startScreenBuilder.BuildStartScreen(transform);
            m_startScreen = startScreenGO.GetComponent<StartScreen>();

            var pauseScreenGO = m_pauseMenuBuilder.BuildPauseScreen(transform);
            m_pauseScreen = pauseScreenGO.GetComponent<PauseScreen>();

            var gameOverScreenGO = m_gameOverScreenBuilder.BuildGameOverScreen(transform);
            m_gameOverScreen = gameOverScreenGO.GetComponent<GameOverScreen>();

            // Build Upgrade Selection Screen
            m_upgradeSelectionScreen = m_upgradeSelectionBuilder.Build(transform);
        }

        #endregion

        #region Reference Wiring

        private void WireReferences()
        {
            // Find UIManager
            if (m_uiManager == null)
            {
                var uiManagerGO = GameObject.Find("UIManager");
                if (uiManagerGO != null)
                {
                    m_uiManager = uiManagerGO.GetComponent<UIManager>();
                }

                if (m_uiManager == null)
                {
                    Debug.LogError("[UIBuilder] UIManager not found! Ensure UIManager exists in scene.");
                    return;
                }
            }

            // Wire UIManager fields via reflection
            var type = typeof(UIManager);
            type.GetField("m_startScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(m_uiManager, m_startScreen);
            type.GetField("m_pauseScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(m_uiManager, m_pauseScreen);
            type.GetField("m_gameOverScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(m_uiManager, m_gameOverScreen);
            type.GetField("m_upgradeSelectionScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(m_uiManager, m_upgradeSelectionScreen);
            type.GetField("m_hudRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(m_uiManager, m_hudRoot);

            // Wire HUDController
            if (m_hudController == null)
            {
                m_hudController = m_hudRoot.GetComponent<HUDController>();
            }
        }

        #endregion

        #region EventSystem Setup

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                Debug.Log("[UIBuilder] EventSystem already exists");
                EnsureInputModule(EventSystem.current.gameObject);
                return;
            }

            // Create EventSystem
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            EnsureInputModule(eventSystemGO);

            Debug.Log("[UIBuilder] Created EventSystem with InputSystemUIInputModule");
        }

        private void EnsureInputModule(GameObject eventSystemGO)
        {
            // Check for new Input System module
            var inputModule = eventSystemGO.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputModule == null)
            {
                // Remove old StandaloneInputModule if present
                var oldModule = eventSystemGO.GetComponent<StandaloneInputModule>();
                if (oldModule != null)
                {
                    Destroy(oldModule);
                }

                // Add new Input System module
                inputModule = eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Try to find GameInput actions asset
            var inputAsset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("Input/GameInput");
            if (inputAsset == null)
            {
                inputAsset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("GameInput");
            }

            if (inputAsset != null)
            {
                var uiMap = inputAsset.FindActionMap("UI");
                if (uiMap != null)
                {
                    Debug.Log("[UIBuilder] Found UI action map in GameInput");
                }
            }

            Debug.Log("[UIBuilder] InputSystemUIInputModule configured");
        }

        #endregion
    }
}
