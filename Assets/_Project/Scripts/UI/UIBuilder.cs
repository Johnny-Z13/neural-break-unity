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
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private HUDController _hudController;

        [Header("Font")]
        [SerializeField] private TMP_FontAsset _fontAsset;

        [Header("Colors (Uses UITheme if not overridden)")]
        [SerializeField] private bool _useThemeColors = true;

        [SerializeField] private Color _customBackgroundColor = new Color(0.05f, 0.05f, 0.1f, 0.9f);
        [SerializeField] private Color _customPrimaryColor = new Color(0f, 1f, 1f, 1f);
        [SerializeField] private Color _customAccentColor = new Color(1f, 0.3f, 0.5f, 1f);
        [SerializeField] private Color _customTextColor = Color.white;

        // Specialized builders
        private HUDBuilder _hudBuilder;
        private StartScreenBuilder _startScreenBuilder;
        private PauseMenuBuilder _pauseMenuBuilder;
        private GameOverScreenBuilder _gameOverScreenBuilder;
        private UpgradeSelectionBuilder _upgradeSelectionBuilder;

        // Built references
        private GameObject _hudRoot;
        private StartScreen _startScreen;
        private PauseScreen _pauseScreen;
        private GameOverScreen _gameOverScreen;
        private UpgradeSelectionScreen _upgradeSelectionScreen;

        private void Awake()
        {
            // Ensure EventSystem exists
            EnsureEventSystem();

            // Load font if not assigned
            LoadFont();

            Debug.Log($"[UIBuilder] Building UI with font: {(_fontAsset != null ? _fontAsset.name : "NULL")}");

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
            if (_fontAsset != null)
            {
                Debug.Log($"[UIBuilder] Font already assigned: {_fontAsset.name}");
                return;
            }

            Debug.Log("[UIBuilder] No font asset assigned, attempting to load...");

            // Try Resources folder
            _fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/Lato SDF");
            if (_fontAsset != null)
            {
                Debug.Log("[UIBuilder] Loaded font from Resources");
                return;
            }

            // Try AssetDatabase (Editor only)
            #if UNITY_EDITOR
            _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Feel/MMTools/Demos/MMTween/Fonts/Lato/SDF/Lato SDF.asset");
            if (_fontAsset != null)
            {
                Debug.Log("[UIBuilder] Loaded font from AssetDatabase");
                return;
            }
            #endif

            // Try TMP default font
            _fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (_fontAsset != null) return;

            // Try TMP Settings
            try
            {
                if (TMP_Settings.instance != null)
                {
                    _fontAsset = TMP_Settings.defaultFontAsset;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UIBuilder] TMP_Settings access failed: {e.Message}");
            }

            if (_fontAsset == null)
            {
                Debug.LogError("[UIBuilder] CRITICAL: No font found! Text will not render!");
            }
        }

        #endregion

        #region Builder Initialization

        private void InitializeBuilders()
        {
            _hudBuilder = new HUDBuilder(_fontAsset, _useThemeColors);
            _startScreenBuilder = new StartScreenBuilder(_fontAsset, _useThemeColors);
            _pauseMenuBuilder = new PauseMenuBuilder(_fontAsset, _useThemeColors);
            _gameOverScreenBuilder = new GameOverScreenBuilder(_fontAsset, _useThemeColors);
            _upgradeSelectionBuilder = new UpgradeSelectionBuilder(_fontAsset);
        }

        #endregion

        #region UI Building

        private void BuildAllUI()
        {
            // Build HUD
            _hudRoot = _hudBuilder.BuildHUD(transform);

            // Build Screens
            var startScreenGO = _startScreenBuilder.BuildStartScreen(transform);
            _startScreen = startScreenGO.GetComponent<StartScreen>();

            var pauseScreenGO = _pauseMenuBuilder.BuildPauseScreen(transform);
            _pauseScreen = pauseScreenGO.GetComponent<PauseScreen>();

            var gameOverScreenGO = _gameOverScreenBuilder.BuildGameOverScreen(transform);
            _gameOverScreen = gameOverScreenGO.GetComponent<GameOverScreen>();

            // Build Upgrade Selection Screen
            _upgradeSelectionScreen = _upgradeSelectionBuilder.Build(transform);
        }

        #endregion

        #region Reference Wiring

        private void WireReferences()
        {
            // Find UIManager
            if (_uiManager == null)
            {
                var uiManagerGO = GameObject.Find("UIManager");
                if (uiManagerGO != null)
                {
                    _uiManager = uiManagerGO.GetComponent<UIManager>();
                }

                if (_uiManager == null)
                {
                    Debug.LogError("[UIBuilder] UIManager not found! Ensure UIManager exists in scene.");
                    return;
                }
            }

            // Wire UIManager fields via reflection
            var type = typeof(UIManager);
            type.GetField("_startScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_uiManager, _startScreen);
            type.GetField("_pauseScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_uiManager, _pauseScreen);
            type.GetField("_gameOverScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_uiManager, _gameOverScreen);
            type.GetField("_upgradeSelectionScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_uiManager, _upgradeSelectionScreen);
            type.GetField("_hudRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_uiManager, _hudRoot);

            // Wire HUDController
            if (_hudController == null)
            {
                _hudController = _hudRoot.GetComponent<HUDController>();
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
