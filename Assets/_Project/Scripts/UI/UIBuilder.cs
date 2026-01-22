using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Builds the UI hierarchy at runtime. Attach to MainCanvas.
    /// Creates all necessary UI elements programmatically.
    /// </summary>
    public class UIBuilder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private HUDController _hudController;

        [Header("Font")]
        [SerializeField] private TMP_FontAsset _fontAsset; // Assign in inspector or loaded at runtime

        [Header("Colors (Uses UITheme if not overridden)")]
        [SerializeField] private bool _useThemeColors = true;

        // These are now derived from UITheme unless overridden
        private Color _backgroundColor => _useThemeColors ? UITheme.BackgroundDark : _customBackgroundColor;
        private Color _primaryColor => _useThemeColors ? UITheme.Primary : _customPrimaryColor;
        private Color _accentColor => _useThemeColors ? UITheme.Accent : _customAccentColor;
        private Color _textColor => _useThemeColors ? UITheme.TextPrimary : _customTextColor;

        [SerializeField] private Color _customBackgroundColor = new Color(0.05f, 0.05f, 0.1f, 0.9f);
        [SerializeField] private Color _customPrimaryColor = new Color(0f, 1f, 1f, 1f);
        [SerializeField] private Color _customAccentColor = new Color(1f, 0.3f, 0.5f, 1f);
        [SerializeField] private Color _customTextColor = Color.white;

        private Canvas _canvas;
        private RectTransform _canvasRect;

        // Built references
        private GameObject _hudRoot;
        private StartScreen _startScreen;
        private PauseScreen _pauseScreen;
        private GameOverScreen _gameOverScreen;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvasRect = GetComponent<RectTransform>();

            // Ensure EventSystem exists for UI navigation (keyboard/mouse/gamepad)
            EnsureEventSystem();

            // Try to load a font if not assigned
            if (_fontAsset == null)
            {
                Debug.Log("[UIBuilder] No font asset assigned, attempting to load...");

                // Try loading from Resources folder
                _fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/Lato SDF");
                if (_fontAsset != null)
                {
                    Debug.Log("[UIBuilder] Loaded font from Resources");
                }

                // If not in Resources, try to load via asset database path (Editor only)
                #if UNITY_EDITOR
                if (_fontAsset == null)
                {
                    _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Feel/MMTools/Demos/MMTween/Fonts/Lato/SDF/Lato SDF.asset");
                    if (_fontAsset != null)
                    {
                        Debug.Log("[UIBuilder] Loaded font from AssetDatabase");
                    }
                }
                #endif

                if (_fontAsset == null)
                {
                    // Try loading TMP default font
                    _fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                }
                if (_fontAsset == null)
                {
                    // Try to load TMP default font if TMP_Settings is configured
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
                }
            }
            else
            {
                Debug.Log($"[UIBuilder] Font already assigned: {_fontAsset.name}");
            }

            Debug.Log($"[UIBuilder] Final font state: {(_fontAsset != null ? _fontAsset.name : "NULL - text will not render!")}");

            Debug.Log($"[UIBuilder] Building UI with font: {(_fontAsset != null ? _fontAsset.name : "NULL")}");
            BuildAllUI();
            Debug.Log("[UIBuilder] Wiring references...");
            WireReferences();
            Debug.Log("[UIBuilder] UI Build complete!");
        }

        private void BuildAllUI()
        {
            // Build HUD
            _hudRoot = BuildHUD();

            // Build Screens
            var startScreenGO = BuildStartScreen();
            _startScreen = startScreenGO.GetComponent<StartScreen>();

            var pauseScreenGO = BuildPauseScreen();
            _pauseScreen = pauseScreenGO.GetComponent<PauseScreen>();

            var gameOverScreenGO = BuildGameOverScreen();
            _gameOverScreen = gameOverScreenGO.GetComponent<GameOverScreen>();
        }

        private void WireReferences()
        {
            // Find or create UIManager
            if (_uiManager == null)
            {
                _uiManager = FindFirstObjectByType<UIManager>();
            }

            if (_uiManager != null)
            {
                // Use reflection to set private fields
                var type = typeof(UIManager);
                type.GetField("_startScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_uiManager, _startScreen);
                type.GetField("_pauseScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_uiManager, _pauseScreen);
                type.GetField("_gameOverScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_uiManager, _gameOverScreen);
                type.GetField("_hudRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_uiManager, _hudRoot);
            }

            // Wire HUDController
            if (_hudController == null)
            {
                _hudController = _hudRoot.GetComponent<HUDController>();
            }
        }

        #region HUD Building

        private GameObject BuildHUD()
        {
            var hud = CreateUIObject("HUD", transform);
            StretchToFill(hud);
            hud.gameObject.AddComponent<HUDController>();
            // Start inactive - UIManager will show based on game state
            hud.gameObject.SetActive(false);

            // Top Left - Health
            var topLeft = CreateUIObject("TopLeft", hud.transform);
            SetAnchors(topLeft, new Vector2(0, 1), new Vector2(0, 1));
            topLeft.anchoredPosition = new Vector2(20, -20);
            topLeft.sizeDelta = new Vector2(300, 100);
            topLeft.pivot = new Vector2(0, 1);

            var healthDisplay = BuildHealthDisplay(topLeft);

            // Top Right - Score & Level
            var topRight = CreateUIObject("TopRight", hud.transform);
            SetAnchors(topRight, new Vector2(1, 1), new Vector2(1, 1));
            topRight.anchoredPosition = new Vector2(-20, -20);
            topRight.sizeDelta = new Vector2(300, 120);
            topRight.pivot = new Vector2(1, 1);

            var scoreDisplay = BuildScoreDisplay(topRight);
            var levelDisplay = BuildLevelDisplay(topRight);

            // Bottom Center - Heat
            var bottomCenter = CreateUIObject("BottomCenter", hud.transform);
            SetAnchors(bottomCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            bottomCenter.anchoredPosition = new Vector2(0, 30);
            bottomCenter.sizeDelta = new Vector2(300, 40);
            bottomCenter.pivot = new Vector2(0.5f, 0);

            var heatDisplay = BuildHeatDisplay(bottomCenter);

            // Wire HUD Controller
            var hudCtrl = hud.gameObject.GetComponent<HUDController>();
            var hudType = typeof(HUDController);
            hudType.GetField("_healthDisplay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudCtrl, healthDisplay);
            hudType.GetField("_scoreDisplay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudCtrl, scoreDisplay);
            hudType.GetField("_heatDisplay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudCtrl, heatDisplay);
            hudType.GetField("_levelDisplay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudCtrl, levelDisplay);

            return hud.gameObject;
        }

        private HealthDisplay BuildHealthDisplay(RectTransform parent)
        {
            var root = CreateUIObject("HealthDisplay", parent);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(250, 30);
            root.pivot = new Vector2(0, 1);

            var display = root.gameObject.AddComponent<HealthDisplay>();

            // Health bar background
            var bgRect = CreateUIObject("HealthBG", root);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(200, 20);
            var bgImg = bgRect.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Health bar fill
            var fillRect = CreateUIObject("HealthFill", bgRect);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fillRect.gameObject.AddComponent<Image>();
            fillImg.color = Color.green;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;

            // Health text
            var textRect = CreateUIObject("HealthText", root);
            textRect.anchoredPosition = new Vector2(210, 0);
            textRect.sizeDelta = new Vector2(80, 20);
            var healthText = AddTextComponent(textRect.gameObject);
            healthText.text = "130/130";
            healthText.fontSize = 14;
            healthText.color = _textColor;
            healthText.alignment = TextAlignmentOptions.Left;

            // Shield icons container
            var shieldContainer = CreateUIObject("Shields", root);
            shieldContainer.anchoredPosition = new Vector2(0, -25);
            shieldContainer.sizeDelta = new Vector2(100, 20);

            Image[] shieldIcons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var shieldRect = CreateUIObject($"Shield{i}", shieldContainer);
                shieldRect.anchoredPosition = new Vector2(i * 25, 0);
                shieldRect.sizeDelta = new Vector2(20, 20);
                var shieldImg = shieldRect.gameObject.AddComponent<Image>();
                shieldImg.color = new Color(0.2f, 0.8f, 1f, 0.3f);
                shieldIcons[i] = shieldImg;
            }

            // Wire display fields
            var displayType = typeof(HealthDisplay);
            displayType.GetField("_healthFill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, fillImg);
            displayType.GetField("_healthText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, healthText);
            displayType.GetField("_shieldIcons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, shieldIcons);
            displayType.GetField("_shieldContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, shieldContainer.transform);

            return display;
        }

        private ScoreDisplay BuildScoreDisplay(RectTransform parent)
        {
            var root = CreateUIObject("ScoreDisplay", parent);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(300, 60);
            root.pivot = new Vector2(1, 1);

            var display = root.gameObject.AddComponent<ScoreDisplay>();

            // Score text
            var scoreRect = CreateUIObject("ScoreText", root);
            scoreRect.anchoredPosition = Vector2.zero;
            scoreRect.sizeDelta = new Vector2(300, 40);
            scoreRect.pivot = new Vector2(1, 1);
            var scoreText = AddTextComponent(scoreRect.gameObject);
            scoreText.text = "0";
            scoreText.fontSize = 36;
            scoreText.color = _primaryColor;
            scoreText.alignment = TextAlignmentOptions.Right;
            scoreText.fontStyle = FontStyles.Bold;

            // Delta text (score popup)
            var deltaRect = CreateUIObject("DeltaText", root);
            deltaRect.anchoredPosition = new Vector2(-50, -30);
            deltaRect.sizeDelta = new Vector2(100, 24);
            var deltaText = AddTextComponent(deltaRect.gameObject);
            deltaText.text = "+100";
            deltaText.fontSize = 18;
            deltaText.color = Color.yellow;
            deltaText.alignment = TextAlignmentOptions.Right;
            deltaText.gameObject.SetActive(false);

            // Combo container
            var comboContainer = CreateUIObject("ComboContainer", root);
            comboContainer.anchoredPosition = new Vector2(0, -50);
            comboContainer.sizeDelta = new Vector2(200, 30);
            comboContainer.pivot = new Vector2(1, 1);

            // Combo text
            var comboRect = CreateUIObject("ComboText", comboContainer);
            comboRect.anchoredPosition = Vector2.zero;
            comboRect.sizeDelta = new Vector2(120, 24);
            var comboText = AddTextComponent(comboRect.gameObject);
            comboText.text = "";
            comboText.fontSize = 16;
            comboText.color = _accentColor;
            comboText.alignment = TextAlignmentOptions.Right;

            // Multiplier text
            var multRect = CreateUIObject("MultiplierText", comboContainer);
            multRect.anchoredPosition = new Vector2(-130, 0);
            multRect.sizeDelta = new Vector2(60, 24);
            var multText = AddTextComponent(multRect.gameObject);
            multText.text = "";
            multText.fontSize = 16;
            multText.color = Color.yellow;
            multText.alignment = TextAlignmentOptions.Right;

            comboContainer.gameObject.SetActive(false);

            // Milestone text (center screen announcements)
            var milestoneRect = CreateUIObject("MilestoneText", root);
            milestoneRect.anchoredPosition = new Vector2(-150, -100);
            milestoneRect.sizeDelta = new Vector2(300, 60);
            var milestoneText = AddTextComponent(milestoneRect.gameObject);
            milestoneText.text = "NICE!";
            milestoneText.fontSize = 48;
            milestoneText.color = Color.yellow;
            milestoneText.alignment = TextAlignmentOptions.Center;
            milestoneText.fontStyle = FontStyles.Bold;
            milestoneRect.gameObject.SetActive(false);

            // Wire display fields
            var displayType = typeof(ScoreDisplay);
            displayType.GetField("_scoreText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, scoreText);
            displayType.GetField("_deltaText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, deltaText);
            displayType.GetField("_comboText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, comboText);
            displayType.GetField("_multiplierText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, multText);
            displayType.GetField("_comboContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, comboContainer.gameObject);
            displayType.GetField("_milestoneText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, milestoneText);

            return display;
        }

        private LevelDisplay BuildLevelDisplay(RectTransform parent)
        {
            var root = CreateUIObject("LevelDisplay", parent);
            root.anchoredPosition = new Vector2(0, -90);
            root.sizeDelta = new Vector2(150, 30);
            root.pivot = new Vector2(1, 1);

            var display = root.gameObject.AddComponent<LevelDisplay>();

            // Level text
            var textRect = CreateUIObject("LevelText", root);
            StretchToFill(textRect);
            var levelText = AddTextComponent(textRect.gameObject);
            levelText.text = "LEVEL 1";
            levelText.fontSize = 20;
            levelText.color = _textColor;
            levelText.alignment = TextAlignmentOptions.Right;

            // Wire display field
            var displayType = typeof(LevelDisplay);
            displayType.GetField("_levelText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, levelText);

            return display;
        }

        private WeaponHeatDisplay BuildHeatDisplay(RectTransform parent)
        {
            var root = CreateUIObject("WeaponHeatDisplay", parent);
            StretchToFill(root);

            var display = root.gameObject.AddComponent<WeaponHeatDisplay>();

            // Heat bar background
            var bgRect = CreateUIObject("HeatBG", root);
            bgRect.anchorMin = new Vector2(0.1f, 0.3f);
            bgRect.anchorMax = new Vector2(0.9f, 0.7f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgRect.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            // Heat bar fill
            var fillRect = CreateUIObject("HeatFill", bgRect);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            var fillImg = fillRect.gameObject.AddComponent<Image>();
            fillImg.color = Color.cyan;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0;

            // Overheat warning
            var warningRect = CreateUIObject("OverheatWarning", root);
            warningRect.anchoredPosition = new Vector2(0, 25);
            warningRect.sizeDelta = new Vector2(150, 25);
            var warningText = AddTextComponent(warningRect.gameObject);
            warningText.text = "OVERHEAT!";
            warningText.fontSize = 18;
            warningText.color = Color.red;
            warningText.alignment = TextAlignmentOptions.Center;
            warningText.fontStyle = FontStyles.Bold;
            warningRect.gameObject.SetActive(false);

            // Power level text (left side of heat bar)
            var powerRect = CreateUIObject("PowerLevelText", root);
            powerRect.anchorMin = new Vector2(0, 0.5f);
            powerRect.anchorMax = new Vector2(0, 0.5f);
            powerRect.anchoredPosition = new Vector2(-5, 0);
            powerRect.sizeDelta = new Vector2(60, 25);
            powerRect.pivot = new Vector2(1, 0.5f);
            var powerText = AddTextComponent(powerRect.gameObject);
            powerText.text = "PWR 0";
            powerText.fontSize = 14;
            powerText.color = new Color(1f, 0.8f, 0f); // Gold/yellow
            powerText.alignment = TextAlignmentOptions.Right;
            powerText.fontStyle = FontStyles.Bold;

            // Wire display fields
            var displayType = typeof(WeaponHeatDisplay);
            displayType.GetField("_heatFill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, fillImg);
            displayType.GetField("_overheatText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, warningText);
            displayType.GetField("_warningContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, warningRect.gameObject);
            displayType.GetField("_powerLevelText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(display, powerText);

            return display;
        }

        #endregion

        #region Screen Building

        private GameObject BuildStartScreen()
        {
            var screen = CreateUIObject("StartScreen", transform);
            StretchToFill(screen);

            var startScreen = screen.gameObject.AddComponent<StartScreen>();

            // Background panel
            var bgImg = screen.gameObject.AddComponent<Image>();
            bgImg.color = _backgroundColor;

            // Container for centering
            var container = CreateUIObject("Container", screen);
            container.anchoredPosition = Vector2.zero;
            container.sizeDelta = new Vector2(600, 400);

            // Title
            var titleRect = CreateUIObject("Title", container);
            titleRect.anchoredPosition = new Vector2(0, 100);
            titleRect.sizeDelta = new Vector2(600, 100);
            var titleText = AddTextComponent(titleRect.gameObject);
            titleText.text = "NEURAL BREAK";
            titleText.fontSize = 72;
            titleText.color = _primaryColor;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // Subtitle
            var subRect = CreateUIObject("Subtitle", container);
            subRect.anchoredPosition = new Vector2(0, 30);
            subRect.sizeDelta = new Vector2(600, 40);
            var subText = AddTextComponent(subRect.gameObject);
            subText.text = "SURVIVE THE DIGITAL SWARM";
            subText.fontSize = 24;
            subText.color = _textColor;
            subText.alignment = TextAlignmentOptions.Center;

            // Play button
            var playBtn = CreateButton("PlayButton", container, "PLAY", new Vector2(0, -80), new Vector2(200, 60));

            // Wire StartScreen fields
            var screenType = typeof(StartScreen);
            screenType.GetField("_screenRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(startScreen, screen.gameObject);
            screenType.GetField("_titleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(startScreen, titleText);
            screenType.GetField("_subtitleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(startScreen, subText);
            screenType.GetField("_playButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(startScreen, playBtn);
            screenType.GetField("_firstSelected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(startScreen, playBtn);

            return screen.gameObject;
        }

        private GameObject BuildPauseScreen()
        {
            var screen = CreateUIObject("PauseScreen", transform);
            StretchToFill(screen);

            var pauseScreen = screen.gameObject.AddComponent<PauseScreen>();

            // Semi-transparent background
            var bgImg = screen.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.7f);

            // Container
            var container = CreateUIObject("Container", screen);
            container.anchoredPosition = Vector2.zero;
            container.sizeDelta = new Vector2(400, 350);

            // Panel background
            var panelRect = CreateUIObject("Panel", container);
            StretchToFill(panelRect);
            var panelImg = panelRect.gameObject.AddComponent<Image>();
            panelImg.color = _backgroundColor;

            // Title
            var titleRect = CreateUIObject("Title", container);
            titleRect.anchoredPosition = new Vector2(0, 120);
            titleRect.sizeDelta = new Vector2(300, 60);
            var titleText = AddTextComponent(titleRect.gameObject);
            titleText.text = "PAUSED";
            titleText.fontSize = 48;
            titleText.color = _primaryColor;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // Buttons
            var resumeBtn = CreateButton("ResumeButton", container, "RESUME", new Vector2(0, 30), new Vector2(180, 50));
            var restartBtn = CreateButton("RestartButton", container, "RESTART", new Vector2(0, -35), new Vector2(180, 50));
            var quitBtn = CreateButton("QuitButton", container, "MAIN MENU", new Vector2(0, -100), new Vector2(180, 50));

            // Wire PauseScreen fields
            var screenType = typeof(PauseScreen);
            screenType.GetField("_screenRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(pauseScreen, screen.gameObject);
            screenType.GetField("_resumeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(pauseScreen, resumeBtn);
            screenType.GetField("_restartButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(pauseScreen, restartBtn);
            screenType.GetField("_quitButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(pauseScreen, quitBtn);
            screenType.GetField("_firstSelected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(pauseScreen, resumeBtn);

            screen.gameObject.SetActive(false);
            return screen.gameObject;
        }

        private GameObject BuildGameOverScreen()
        {
            var screen = CreateUIObject("GameOverScreen", transform);
            StretchToFill(screen);

            var gameOverScreen = screen.gameObject.AddComponent<GameOverScreen>();

            // Background
            var bgImg = screen.gameObject.AddComponent<Image>();
            bgImg.color = _backgroundColor;

            // Container
            var container = CreateUIObject("Container", screen);
            container.anchoredPosition = Vector2.zero;
            container.sizeDelta = new Vector2(500, 500);

            // Title
            var titleRect = CreateUIObject("Title", container);
            titleRect.anchoredPosition = new Vector2(0, 200);
            titleRect.sizeDelta = new Vector2(500, 70);
            var titleText = AddTextComponent(titleRect.gameObject);
            titleText.text = "GAME OVER";
            titleText.fontSize = 56;
            titleText.color = _accentColor;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // Stats
            float yPos = 120;
            float yStep = 35;

            var scoreText = CreateStatText(container, "FinalScore", "FINAL SCORE: 0", yPos);
            yPos -= yStep;
            var timeText = CreateStatText(container, "TimeSurvived", "TIME: 00:00", yPos);
            yPos -= yStep;
            var killsText = CreateStatText(container, "EnemiesKilled", "ENEMIES KILLED: 0", yPos);
            yPos -= yStep;
            var levelText = CreateStatText(container, "LevelReached", "LEVEL REACHED: 1", yPos);
            yPos -= yStep;
            var comboText = CreateStatText(container, "HighestCombo", "HIGHEST COMBO: 0x", yPos);
            yPos -= yStep;
            var multText = CreateStatText(container, "HighestMult", "BEST MULTIPLIER: 1.0x", yPos);

            // Buttons
            var restartBtn = CreateButton("RestartButton", container, "RESTART", new Vector2(0, -120), new Vector2(180, 50));
            var menuBtn = CreateButton("MainMenuButton", container, "MAIN MENU", new Vector2(0, -185), new Vector2(180, 50));

            // Wire GameOverScreen fields
            var screenType = typeof(GameOverScreen);
            screenType.GetField("_screenRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameOverScreen, screen.gameObject);
            screenType.GetField("_titleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameOverScreen, titleText);
            screenType.GetField("_finalScoreText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameOverScreen, scoreText);
            screenType.GetField("_timeSurvivedText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameOverScreen, timeText);
            screenType.GetField("_enemiesKilledText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameOverScreen, killsText);
            screenType.GetField("_levelReachedText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameOverScreen, levelText);
            screenType.GetField("_highestComboText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameOverScreen, comboText);
            screenType.GetField("_highestMultiplierText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameOverScreen, multText);
            screenType.GetField("_restartButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameOverScreen, restartBtn);
            screenType.GetField("_mainMenuButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameOverScreen, menuBtn);
            screenType.GetField("_firstSelected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gameOverScreen, restartBtn);

            screen.gameObject.SetActive(false);
            return screen.gameObject;
        }

        private TextMeshProUGUI CreateStatText(RectTransform parent, string name, string text, float yPos)
        {
            var rect = CreateUIObject(name, parent);
            rect.anchoredPosition = new Vector2(0, yPos);
            rect.sizeDelta = new Vector2(400, 30);
            var tmp = AddTextComponent(rect.gameObject);
            tmp.text = text;
            tmp.fontSize = 22;
            tmp.color = _textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        #endregion

        #region EventSystem Setup

        /// <summary>
        /// Ensure EventSystem exists for keyboard/mouse/gamepad UI navigation
        /// </summary>
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
            var eventSystem = eventSystemGO.AddComponent<EventSystem>();

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

            // Try to find and assign our GameInput actions asset
            var inputAsset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("Input/GameInput");
            if (inputAsset == null)
            {
                // Try loading from _Project folder
                inputAsset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("GameInput");
            }

            if (inputAsset != null)
            {
                // Find the UI action map
                var uiMap = inputAsset.FindActionMap("UI");
                if (uiMap != null)
                {
                    // The InputSystemUIInputModule will work with default bindings
                    // Submit = South button (A/X), Cancel = East button (B/Circle)
                    Debug.Log("[UIBuilder] Found UI action map in GameInput");
                }
            }

            Debug.Log("[UIBuilder] InputSystemUIInputModule configured for keyboard/mouse/gamepad navigation");
        }

        #endregion

        #region UI Helpers

        private RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private TextMeshProUGUI AddTextComponent(GameObject go)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (_fontAsset != null)
            {
                tmp.font = _fontAsset;
            }
            return tmp;
        }

        private void StretchToFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
        }

        private TextMeshProUGUI CreateText(RectTransform parent, string name, string text, int fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
        {
            var textRect = CreateUIObject(name, parent);
            StretchToFill(textRect);
            var tmp = AddTextComponent(textRect.gameObject);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            return tmp;
        }

        private Button CreateButton(string name, RectTransform parent, string text, Vector2 position, Vector2 size)
        {
            var btnRect = CreateUIObject(name, parent);
            btnRect.anchoredPosition = position;
            btnRect.sizeDelta = size;

            // Button background
            var btnImg = btnRect.gameObject.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.2f, 0.3f, 1f);

            // Button component
            var btn = btnRect.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.3f, 1f);
            colors.highlightedColor = _primaryColor * 0.8f;
            colors.pressedColor = _primaryColor;
            colors.selectedColor = _primaryColor * 0.6f;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            // Enable automatic navigation (keyboard/gamepad)
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.Automatic;
            btn.navigation = nav;

            // Button text
            CreateText(btnRect, "Text", text, 24, _textColor, TextAlignmentOptions.Center, FontStyles.Bold);

            return btn;
        }

        #endregion
    }
}
