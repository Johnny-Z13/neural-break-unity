using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuralBreak.UI.Builders
{
    /// <summary>
    /// NEURAL BREAK HUD - CLASSIC ARCADE LAYOUT
    ///
    /// ┌─────────────────────────────────────────────────────────────────┐
    /// │ [═══HEALTH BAR═══]              x5       6,780,932             │
    /// │ ●●●                                        LEVEL 93            │
    /// │                                                                │
    /// │                                                                │
    /// │                                                                │
    /// │                                                                │
    /// │  ╭───╮                                                         │
    /// │  │   │                           [═══HEAT BAR═══]  PWR 5      │
    /// │  ╰───╯                                      ●●●               │
    /// └─────────────────────────────────────────────────────────────────┘
    ///
    /// UNIFORM MARGIN from all edges. Clean arcade aesthetic.
    /// </summary>
    public class HUDBuilderArcade : UIScreenBuilderBase
    {
        // Arcade color palette
        private static readonly Color HealthGreen = new Color(0.2f, 0.9f, 0.3f, 1f);
        private static readonly Color ShieldBlue = new Color(0.3f, 0.7f, 1f, 1f);
        private static readonly Color ShieldEmpty = new Color(0.2f, 0.2f, 0.25f, 0.6f);
        private static readonly Color ScoreGold = new Color(1f, 0.9f, 0.3f, 1f);
        private static readonly Color MultiplierYellow = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color LevelOrange = new Color(1f, 0.7f, 0.3f, 1f);
        private static readonly Color HeatOrange = new Color(1f, 0.5f, 0.15f, 1f);
        private static readonly Color OverheatRed = new Color(1f, 0.25f, 0.2f, 1f);
        private static readonly Color BombGold = new Color(1f, 0.8f, 0.2f, 1f);
        private static readonly Color BombEmpty = new Color(0.25f, 0.2f, 0.15f, 0.6f);
        private static readonly Color BarBackground = new Color(0.1f, 0.1f, 0.12f, 0.85f);

        // UNIFORM MARGIN - same distance from all screen edges
        private const float EDGE_MARGIN = 24f;

        // Component sizing (reduced 25% from original)
        private const float HEALTH_BAR_WIDTH = 180f;  // was 240
        private const float HEALTH_BAR_HEIGHT = 16f;  // was 22
        private const float SHIELD_SIZE = 14f;        // was 18
        private const float SHIELD_GAP = 5f;          // was 6
        private const float HEAT_BAR_WIDTH = 135f;    // was 180
        private const float HEAT_BAR_HEIGHT = 14f;    // was 18
        private const float BOMB_SIZE = 15f;          // was 20
        private const float BOMB_GAP = 5f;            // was 6
        private const float ROW_GAP = 6f;             // was 8

        public HUDBuilderArcade(TMP_FontAsset fontAsset, bool useThemeColors = true)
            : base(fontAsset, useThemeColors) { }

        public GameObject BuildHUD(Transform canvasTransform)
        {
            var hud = CreateUIObject("HUD_Arcade", canvasTransform);
            StretchToFill(hud);
            hud.gameObject.AddComponent<HUDController>();
            hud.gameObject.SetActive(false);

            // Build components with UNIFORM margins
            var healthDisplay = BuildHealthSection(hud);
            var scoreDisplay = BuildScoreSection(hud);
            var heatDisplay = BuildWeaponSection(hud);
            var levelDisplay = BuildLevelDisplay(hud);

            // Wire HUDController
            var hudCtrl = hud.gameObject.GetComponent<HUDController>();
            SetPrivateField(hudCtrl, "m_healthDisplay", healthDisplay);
            SetPrivateField(hudCtrl, "m_scoreDisplay", scoreDisplay);
            SetPrivateField(hudCtrl, "m_heatDisplay", heatDisplay);
            SetPrivateField(hudCtrl, "m_levelDisplay", levelDisplay);

            return hud.gameObject;
        }

        #region TOP LEFT - Health + Shields

        private HealthDisplay BuildHealthSection(RectTransform hudRoot)
        {
            // Container: top-left corner, EDGE_MARGIN from top and left
            var container = CreateUIObject("HealthSection", hudRoot);
            SetAnchors(container, new Vector2(0, 1), new Vector2(0, 1));
            container.pivot = new Vector2(0, 1);
            container.anchoredPosition = new Vector2(EDGE_MARGIN, -EDGE_MARGIN);
            container.sizeDelta = new Vector2(HEALTH_BAR_WIDTH + 15, 38);  // was +20, 50

            var display = container.gameObject.AddComponent<HealthDisplay>();

            // Health bar background
            var barBg = CreateUIObject("HealthBarBG", container);
            barBg.pivot = new Vector2(0, 1);
            barBg.anchoredPosition = Vector2.zero;
            barBg.sizeDelta = new Vector2(HEALTH_BAR_WIDTH, HEALTH_BAR_HEIGHT);
            var bgImg = barBg.gameObject.AddComponent<Image>();
            bgImg.color = BarBackground;

            // Health bar fill (uses Image.Type.Filled for proper shrinking)
            var barFill = CreateUIObject("HealthBarFill", barBg);
            barFill.anchorMin = Vector2.zero;
            barFill.anchorMax = Vector2.one;
            barFill.offsetMin = new Vector2(2, 2);
            barFill.offsetMax = new Vector2(-2, -2);
            barFill.pivot = new Vector2(0, 0.5f);
            var fillImg = barFill.gameObject.AddComponent<Image>();
            fillImg.sprite = CreateFillSprite(); // Sprite required for fill to work
            fillImg.color = HealthGreen;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 1f;

            // Shield icons row (below health bar)
            var shieldRow = CreateUIObject("ShieldRow", container);
            shieldRow.pivot = new Vector2(0, 1);
            shieldRow.anchoredPosition = new Vector2(0, -HEALTH_BAR_HEIGHT - ROW_GAP);
            shieldRow.sizeDelta = new Vector2(100, SHIELD_SIZE);

            Image[] shieldIcons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var shield = CreateUIObject($"Shield{i}", shieldRow);
                shield.pivot = new Vector2(0, 0.5f);
                shield.anchoredPosition = new Vector2(i * (SHIELD_SIZE + SHIELD_GAP), -SHIELD_SIZE / 2f);
                shield.sizeDelta = new Vector2(SHIELD_SIZE, SHIELD_SIZE);

                var shieldImg = shield.gameObject.AddComponent<Image>();
                shieldImg.sprite = CreateCircleSprite();
                shieldImg.color = ShieldBlue;
                shieldIcons[i] = shieldImg;
            }

            // Optional health text (right of bar)
            var healthText = CreateUIObject("HealthText", container);
            healthText.pivot = new Vector2(0, 0.5f);
            healthText.anchoredPosition = new Vector2(HEALTH_BAR_WIDTH + 6, -HEALTH_BAR_HEIGHT / 2f);
            healthText.sizeDelta = new Vector2(50, HEALTH_BAR_HEIGHT);
            var hText = AddTextComponent(healthText.gameObject);
            hText.text = "";
            hText.fontSize = 11;  // was 14
            hText.color = HealthGreen;
            hText.alignment = TextAlignmentOptions.Left;

            // Wire display
            SetPrivateField(display, "m_healthFill", fillImg);
            SetPrivateField(display, "m_healthBackground", bgImg);
            SetPrivateField(display, "m_healthText", hText);
            SetPrivateField(display, "m_shieldIcons", shieldIcons);
            SetPrivateField(display, "m_shieldContainer", shieldRow.transform);
            SetPrivateField(display, "m_customShieldActiveColor", ShieldBlue);
            SetPrivateField(display, "m_customShieldInactiveColor", ShieldEmpty);
            SetPrivateField(display, "m_animateChanges", true);
            SetPrivateField(display, "m_smoothSpeed", 10f);

            // Force initial fill amount to ensure Image is properly configured
            fillImg.fillAmount = 1f;

            return display;
        }

        #endregion

        #region TOP RIGHT - Score + Multiplier

        private ScoreDisplay BuildScoreSection(RectTransform hudRoot)
        {
            // Container: top-right corner, EDGE_MARGIN from top and right
            var container = CreateUIObject("ScoreSection", hudRoot);
            SetAnchors(container, new Vector2(1, 1), new Vector2(1, 1));
            container.pivot = new Vector2(1, 1);
            container.anchoredPosition = new Vector2(-EDGE_MARGIN, -EDGE_MARGIN);
            container.sizeDelta = new Vector2(230, 38);  // was 300, 50

            var display = container.gameObject.AddComponent<ScoreDisplay>();

            // Score text (large, right-aligned)
            var scoreRect = CreateUIObject("ScoreText", container);
            scoreRect.pivot = new Vector2(1, 1);
            scoreRect.anchoredPosition = Vector2.zero;
            scoreRect.sizeDelta = new Vector2(150, 28);
            var scoreText = AddTextComponent(scoreRect.gameObject);
            scoreText.text = "0";
            scoreText.fontSize = 24;  // was 32
            scoreText.color = ScoreGold;
            scoreText.alignment = TextAlignmentOptions.Right;
            scoreText.fontStyle = FontStyles.Bold;

            // Multiplier text (left of score)
            var multRect = CreateUIObject("MultiplierText", container);
            multRect.pivot = new Vector2(1, 1);
            multRect.anchoredPosition = new Vector2(-160, 0);
            multRect.sizeDelta = new Vector2(60, 28);
            var multText = AddTextComponent(multRect.gameObject);
            multText.text = "";
            multText.fontSize = 20;  // was 26
            multText.color = MultiplierYellow;
            multText.alignment = TextAlignmentOptions.Right;
            multText.fontStyle = FontStyles.Bold;

            // Combo container (for animations)
            var comboContainer = CreateUIObject("ComboContainer", container);
            comboContainer.pivot = new Vector2(1, 1);
            comboContainer.anchoredPosition = new Vector2(-160, 0);
            comboContainer.sizeDelta = new Vector2(60, 28);

            // Combo text
            var comboRect = CreateUIObject("ComboText", comboContainer);
            StretchToFill(comboRect);
            var comboText = AddTextComponent(comboRect.gameObject);
            comboText.text = "";
            comboText.fontSize = 14;  // was 18
            comboText.color = MultiplierYellow;
            comboText.alignment = TextAlignmentOptions.Right;

            // Delta text (popup, hidden by default)
            var deltaRect = CreateUIObject("DeltaText", container);
            deltaRect.pivot = new Vector2(1, 1);
            deltaRect.anchoredPosition = new Vector2(-40, -30);
            deltaRect.sizeDelta = new Vector2(80, 20);
            var deltaText = AddTextComponent(deltaRect.gameObject);
            deltaText.text = "+100";
            deltaText.fontSize = 12;  // was 16
            deltaText.color = ScoreGold;
            deltaText.alignment = TextAlignmentOptions.Right;
            deltaRect.gameObject.SetActive(false);

            // Milestone text (center screen)
            var milestoneRect = CreateUIObject("MilestoneText", hudRoot);
            SetAnchors(milestoneRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            milestoneRect.anchoredPosition = new Vector2(0, 80);
            milestoneRect.sizeDelta = new Vector2(400, 60);
            var milestoneText = AddTextComponent(milestoneRect.gameObject);
            milestoneText.text = "UNSTOPPABLE!";
            milestoneText.fontSize = 36;  // was 48
            milestoneText.color = MultiplierYellow;
            milestoneText.fontStyle = FontStyles.Bold;
            milestoneText.alignment = TextAlignmentOptions.Center;
            milestoneRect.gameObject.SetActive(false);

            // Wire display
            SetPrivateField(display, "m_scoreText", scoreText);
            SetPrivateField(display, "m_multiplierText", multText);
            SetPrivateField(display, "m_comboText", comboText);
            SetPrivateField(display, "m_deltaText", deltaText);
            SetPrivateField(display, "m_comboContainer", comboContainer.gameObject);
            SetPrivateField(display, "m_milestoneText", milestoneText);

            return display;
        }

        private LevelDisplay BuildLevelDisplay(RectTransform hudRoot)
        {
            // Below score section, same right margin
            var container = CreateUIObject("LevelSection", hudRoot);
            SetAnchors(container, new Vector2(1, 1), new Vector2(1, 1));
            container.pivot = new Vector2(1, 1);
            container.anchoredPosition = new Vector2(-EDGE_MARGIN, -EDGE_MARGIN - 32);
            container.sizeDelta = new Vector2(120, 20);

            var display = container.gameObject.AddComponent<LevelDisplay>();

            var levelRect = CreateUIObject("LevelText", container);
            StretchToFill(levelRect);
            var levelText = AddTextComponent(levelRect.gameObject);
            levelText.text = "LEVEL 1";
            levelText.fontSize = 14;  // was 18
            levelText.color = LevelOrange;
            levelText.alignment = TextAlignmentOptions.Right;
            levelText.fontStyle = FontStyles.Bold;

            SetPrivateField(display, "m_levelText", levelText);

            return display;
        }

        #endregion

        #region BOTTOM RIGHT - Heat Bar + Bombs

        private WeaponHeatDisplay BuildWeaponSection(RectTransform hudRoot)
        {
            // Container: bottom-right corner, EDGE_MARGIN from bottom and right
            var container = CreateUIObject("WeaponSection", hudRoot);
            SetAnchors(container, new Vector2(1, 0), new Vector2(1, 0));
            container.pivot = new Vector2(1, 0);
            container.anchoredPosition = new Vector2(-EDGE_MARGIN, EDGE_MARGIN);
            container.sizeDelta = new Vector2(HEAT_BAR_WIDTH + 60, 42);

            var display = container.gameObject.AddComponent<WeaponHeatDisplay>();

            // Row 1: Heat bar + Power level text
            // Heat bar background
            var barBg = CreateUIObject("HeatBarBG", container);
            barBg.pivot = new Vector2(1, 0);
            barBg.anchoredPosition = new Vector2(0, BOMB_SIZE + ROW_GAP);
            barBg.sizeDelta = new Vector2(HEAT_BAR_WIDTH, HEAT_BAR_HEIGHT);
            var bgImg = barBg.gameObject.AddComponent<Image>();
            bgImg.color = BarBackground;

            // Heat bar fill
            var barFill = CreateUIObject("HeatBarFill", barBg);
            barFill.anchorMin = Vector2.zero;
            barFill.anchorMax = Vector2.one;
            barFill.offsetMin = new Vector2(2, 2);
            barFill.offsetMax = new Vector2(-2, -2);
            barFill.pivot = new Vector2(0, 0.5f);
            var fillImg = barFill.gameObject.AddComponent<Image>();
            fillImg.sprite = CreateFillSprite(); // Sprite required for fill to work
            fillImg.color = HeatOrange;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 0f;

            // Power level text (left of heat bar)
            var powerRect = CreateUIObject("PowerText", container);
            powerRect.pivot = new Vector2(1, 0);
            powerRect.anchoredPosition = new Vector2(-HEAT_BAR_WIDTH - 6, BOMB_SIZE + ROW_GAP);
            powerRect.sizeDelta = new Vector2(50, HEAT_BAR_HEIGHT);
            var powerText = AddTextComponent(powerRect.gameObject);
            powerText.text = "PWR 0";
            powerText.fontSize = 11;  // was 14
            powerText.color = HeatOrange;
            powerText.alignment = TextAlignmentOptions.Right;
            powerText.fontStyle = FontStyles.Bold;

            // Overheat warning (above heat bar)
            var overheatRect = CreateUIObject("OverheatWarning", container);
            overheatRect.pivot = new Vector2(1, 0);
            overheatRect.anchoredPosition = new Vector2(0, BOMB_SIZE + ROW_GAP + HEAT_BAR_HEIGHT + 3);
            overheatRect.sizeDelta = new Vector2(80, 14);
            var overheatText = AddTextComponent(overheatRect.gameObject);
            overheatText.text = "OVERHEAT!";
            overheatText.fontSize = 11;  // was 14
            overheatText.color = OverheatRed;
            overheatText.alignment = TextAlignmentOptions.Right;
            overheatText.fontStyle = FontStyles.Bold;
            overheatRect.gameObject.SetActive(false);

            // Row 2: Bomb icons (bottom row, aligned right)
            var bombRow = CreateUIObject("BombRow", container);
            bombRow.pivot = new Vector2(1, 0);
            bombRow.anchoredPosition = Vector2.zero;
            bombRow.sizeDelta = new Vector2(100, BOMB_SIZE);

            // Create 3 bomb icons (right-aligned, so rightmost is index 0)
            Image[] bombIcons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var bomb = CreateUIObject($"Bomb{i}", bombRow);
                bomb.pivot = new Vector2(1, 0);
                bomb.anchoredPosition = new Vector2(-i * (BOMB_SIZE + BOMB_GAP), 0);
                bomb.sizeDelta = new Vector2(BOMB_SIZE, BOMB_SIZE);

                var bombImg = bomb.gameObject.AddComponent<Image>();
                bombImg.sprite = CreateCircleSprite();
                bombImg.color = BombEmpty;
                bombIcons[i] = bombImg;
            }

            // Add BombDisplay component and wire icons
            var bombDisplay = container.gameObject.AddComponent<BombDisplay>();
            bombDisplay.SetIcons(bombIcons);

            // Wire display
            SetPrivateField(display, "m_heatFill", fillImg);
            SetPrivateField(display, "m_overheatText", overheatText);
            SetPrivateField(display, "m_warningContainer", overheatRect.gameObject);
            SetPrivateField(display, "m_powerLevelText", powerText);

            return display;
        }

        #endregion

        #region Sprite Generation (Static Cache)

        // Cache sprites to avoid regenerating textures
        private static Sprite _cachedFillSprite;
        private static Sprite _cachedCircleSprite;

        private Sprite CreateFillSprite()
        {
            if (_cachedFillSprite != null) return _cachedFillSprite;

            int size = 4;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;

            tex.SetPixels(pixels);
            tex.Apply();

            _cachedFillSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _cachedFillSprite;
        }

        private Sprite CreateCircleSprite()
        {
            if (_cachedCircleSprite != null) return _cachedCircleSprite;

            int size = 64;
            int radius = 28;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float dist = Vector2.Distance(pos, center);

                    if (dist <= radius)
                        pixels[y * size + x] = Color.white;
                    else if (dist <= radius + 1.5f)
                        pixels[y * size + x] = new Color(1, 1, 1, 1f - (dist - radius) / 1.5f);
                    else
                        pixels[y * size + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            _cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _cachedCircleSprite;
        }

        #endregion
    }
}
