using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuralBreak.UI.Builders
{
    /// <summary>
    /// NEURAL BREAK HUD V2 - HOLOGRAPHIC RETRO-FUTURISM
    ///
    /// Design Philosophy:
    /// - CRT warmth meets holographic projection
    /// - Hexagonal/angular geometry (circuit board aesthetic)
    /// - Glowing edges with chromatic aberration hints
    /// - Strategic negative space with floating elements
    /// - Deep space blacks, electric cyan/magenta accents
    ///
    /// Layout:
    /// ┌─────────────────────────────────────────────────────────┐
    /// │  [HEALTH BAR]              [LV X]           [SCORE]     │
    /// │  ████████░░ 87/130                         1,234,567    │
    /// │  [⬡⬡⬡] shields                            x3.2 COMBO   │
    /// │                                                         │
    /// │                    [XP BAR]                              │
    /// │                                                         │
    /// │                                                         │
    /// │                                                         │
    /// │                                                         │
    /// │  [BOMBS]                                                 │
    /// │  ◉◉○                     [═══HEAT═══]        [PWR 5]    │
    /// │  [UPGRADES]                                              │
    /// └─────────────────────────────────────────────────────────┘
    /// </summary>
    public class HUDBuilderV2 : UIScreenBuilderBase
    {
        // Accent colors from UITheme
        private static readonly Color CyanPrimary = UITheme.Primary;
        private static readonly Color MagentaAccent = UITheme.Accent;
        private static readonly Color NeonGreen = UITheme.Good;
        private static readonly Color WarningOrange = UITheme.Warning;
        private static readonly Color DangerRed = UITheme.Danger;
        private static readonly Color GoldLegendary = UITheme.Legendary;

        // Panel styling
        private static readonly Color PanelBg = new Color(0.02f, 0.02f, 0.05f, 0.85f);
        private static readonly Color PanelBorder = new Color(0f, 0.8f, 1f, 0.4f);
        private static readonly Color GlowColor = new Color(0f, 1f, 1f, 0.15f);

        public HUDBuilderV2(TMP_FontAsset fontAsset, bool useThemeColors = true)
            : base(fontAsset, useThemeColors) { }

        /// <summary>
        /// Build the redesigned HUD
        /// </summary>
        public GameObject BuildHUD(Transform canvasTransform)
        {
            var hud = CreateUIObject("HUD_V2", canvasTransform);
            StretchToFill(hud);
            hud.gameObject.AddComponent<HUDController>();
            hud.gameObject.SetActive(false);

            // Build all HUD components with new design
            var healthDisplay = BuildHealthDisplayV2(hud);
            var scoreDisplay = BuildScoreDisplayV2(hud);
            var levelDisplay = BuildLevelDisplayV2(hud);
            var heatDisplay = BuildHeatDisplayV2(hud);

            // Wire HUDController
            var hudCtrl = hud.gameObject.GetComponent<HUDController>();
            SetPrivateField(hudCtrl, "m_healthDisplay", healthDisplay);
            SetPrivateField(hudCtrl, "m_scoreDisplay", scoreDisplay);
            SetPrivateField(hudCtrl, "m_heatDisplay", heatDisplay);
            SetPrivateField(hudCtrl, "m_levelDisplay", levelDisplay);

            // Add decorative scanline overlay
            BuildScanlineOverlay(hud);

            return hud.gameObject;
        }

        #region Health Display V2

        private HealthDisplay BuildHealthDisplayV2(RectTransform hudRoot)
        {
            // Top-left floating panel
            var container = CreateUIObject("HealthContainer", hudRoot);
            SetAnchors(container, new Vector2(0, 1), new Vector2(0, 1));
            container.anchoredPosition = new Vector2(24, -24);
            container.sizeDelta = new Vector2(320, 80);
            container.pivot = new Vector2(0, 1);

            // Panel background with glow
            var panelBg = CreatePanelBackground(container, "HealthPanel", PanelBg, PanelBorder);

            // Health display root
            var root = CreateUIObject("HealthDisplay", container);
            root.anchoredPosition = new Vector2(16, -12);
            root.sizeDelta = new Vector2(288, 56);
            root.pivot = new Vector2(0, 1);

            var display = root.gameObject.AddComponent<HealthDisplay>();

            // Health label with hex accent
            var labelContainer = CreateUIObject("LabelRow", root);
            labelContainer.anchoredPosition = Vector2.zero;
            labelContainer.sizeDelta = new Vector2(280, 18);
            labelContainer.pivot = new Vector2(0, 1);

            var hexAccent = CreateUIObject("HexAccent", labelContainer);
            hexAccent.anchoredPosition = Vector2.zero;
            hexAccent.sizeDelta = new Vector2(18, 18);
            var hexImg = hexAccent.gameObject.AddComponent<Image>();
            hexImg.sprite = CreateHexagonSprite();
            hexImg.color = CyanPrimary;

            var label = CreateUIObject("Label", labelContainer);
            label.anchoredPosition = new Vector2(24, 0);
            label.sizeDelta = new Vector2(80, 18);
            var labelText = AddTextComponent(label.gameObject);
            labelText.text = "HULL";
            labelText.fontSize = 12;
            labelText.color = CyanPrimary;
            labelText.fontStyle = FontStyles.Bold;
            labelText.characterSpacing = 8f;
            labelText.alignment = TextAlignmentOptions.Left;

            // Health bar with angular ends
            var barContainer = CreateUIObject("BarContainer", root);
            barContainer.anchoredPosition = new Vector2(0, -22);
            barContainer.sizeDelta = new Vector2(220, 16);
            barContainer.pivot = new Vector2(0, 1);

            // Bar background
            var bgRect = CreateUIObject("BarBG", barContainer);
            StretchToFill(bgRect);
            var bgImg = bgRect.gameObject.AddComponent<Image>();
            bgImg.sprite = CreateAngularBarSprite();
            bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            bgImg.type = Image.Type.Sliced;

            // Bar fill
            var fillRect = CreateUIObject("BarFill", barContainer);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            var fillImg = fillRect.gameObject.AddComponent<Image>();
            fillImg.sprite = CreateAngularBarSprite();
            fillImg.color = NeonGreen;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;

            // Glow overlay on bar
            var glowRect = CreateUIObject("BarGlow", barContainer);
            StretchToFill(glowRect);
            glowRect.offsetMin = new Vector2(-3, -3);
            glowRect.offsetMax = new Vector2(3, 3);
            var glowImg = glowRect.gameObject.AddComponent<Image>();
            glowImg.sprite = CreateAngularBarSprite();
            glowImg.color = new Color(NeonGreen.r, NeonGreen.g, NeonGreen.b, 0.2f);
            glowImg.type = Image.Type.Sliced;
            glowImg.raycastTarget = false;

            // Health text (right side)
            var textRect = CreateUIObject("HealthText", root);
            textRect.anchoredPosition = new Vector2(230, -22);
            textRect.sizeDelta = new Vector2(60, 16);
            textRect.pivot = new Vector2(0, 1);
            var healthText = AddTextComponent(textRect.gameObject);
            healthText.text = "130";
            healthText.fontSize = 14;
            healthText.color = Color.white;
            healthText.fontStyle = FontStyles.Bold;
            healthText.alignment = TextAlignmentOptions.Left;

            // Shield icons row (hexagonal)
            var shieldContainer = CreateUIObject("Shields", root);
            shieldContainer.anchoredPosition = new Vector2(0, -44);
            shieldContainer.sizeDelta = new Vector2(120, 20);
            shieldContainer.pivot = new Vector2(0, 1);

            Image[] shieldIcons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var shieldRect = CreateUIObject($"Shield{i}", shieldContainer);
                shieldRect.anchoredPosition = new Vector2(i * 28, 0);
                shieldRect.sizeDelta = new Vector2(24, 24);
                var shieldImg = shieldRect.gameObject.AddComponent<Image>();
                shieldImg.sprite = CreateHexagonSprite();
                shieldImg.color = UITheme.ShieldInactive;
                shieldIcons[i] = shieldImg;
            }

            // Wire display fields
            SetPrivateField(display, "m_healthFill", fillImg);
            SetPrivateField(display, "m_healthText", healthText);
            SetPrivateField(display, "m_shieldIcons", shieldIcons);
            SetPrivateField(display, "m_shieldContainer", shieldContainer.transform);

            return display;
        }

        #endregion

        #region Score Display V2

        private ScoreDisplay BuildScoreDisplayV2(RectTransform hudRoot)
        {
            // Top-right floating panel
            var container = CreateUIObject("ScoreContainer", hudRoot);
            SetAnchors(container, new Vector2(1, 1), new Vector2(1, 1));
            container.anchoredPosition = new Vector2(-24, -24);
            container.sizeDelta = new Vector2(280, 100);
            container.pivot = new Vector2(1, 1);

            var panelBg = CreatePanelBackground(container, "ScorePanel", PanelBg, PanelBorder);

            var root = CreateUIObject("ScoreDisplay", container);
            root.anchoredPosition = new Vector2(-16, -12);
            root.sizeDelta = new Vector2(248, 76);
            root.pivot = new Vector2(1, 1);

            var display = root.gameObject.AddComponent<ScoreDisplay>();

            // Score label
            var labelRect = CreateUIObject("Label", root);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(248, 14);
            labelRect.pivot = new Vector2(1, 1);
            var label = AddTextComponent(labelRect.gameObject);
            label.text = "SCORE";
            label.fontSize = 10;
            label.color = new Color(0.5f, 0.55f, 0.6f);
            label.characterSpacing = 6f;
            label.alignment = TextAlignmentOptions.Right;

            // Main score text (large, glowing)
            var scoreRect = CreateUIObject("ScoreText", root);
            scoreRect.anchoredPosition = new Vector2(0, -14);
            scoreRect.sizeDelta = new Vector2(248, 36);
            scoreRect.pivot = new Vector2(1, 1);
            var scoreText = AddTextComponent(scoreRect.gameObject);
            scoreText.text = "0";
            scoreText.fontSize = 32;
            scoreText.color = Color.white;
            scoreText.fontStyle = FontStyles.Bold;
            scoreText.alignment = TextAlignmentOptions.Right;

            // Score glow shadow
            var scoreGlow = CreateUIObject("ScoreGlow", scoreRect);
            StretchToFill(scoreGlow);
            scoreGlow.anchoredPosition = new Vector2(2, -2);
            var glowText = AddTextComponent(scoreGlow.gameObject);
            glowText.text = "0";
            glowText.fontSize = 32;
            glowText.color = new Color(CyanPrimary.r, CyanPrimary.g, CyanPrimary.b, 0.3f);
            glowText.fontStyle = FontStyles.Bold;
            glowText.alignment = TextAlignmentOptions.Right;
            glowText.raycastTarget = false;

            // Delta text
            var deltaRect = CreateUIObject("DeltaText", root);
            deltaRect.anchoredPosition = new Vector2(-80, -50);
            deltaRect.sizeDelta = new Vector2(100, 20);
            deltaRect.pivot = new Vector2(1, 1);
            var deltaText = AddTextComponent(deltaRect.gameObject);
            deltaText.text = "+100";
            deltaText.fontSize = 16;
            deltaText.color = GoldLegendary;
            deltaText.alignment = TextAlignmentOptions.Right;
            deltaText.gameObject.SetActive(false);

            // Combo container
            var comboContainer = CreateUIObject("ComboContainer", root);
            comboContainer.anchoredPosition = new Vector2(0, -54);
            comboContainer.sizeDelta = new Vector2(200, 22);
            comboContainer.pivot = new Vector2(1, 1);

            // Combo text
            var comboRect = CreateUIObject("ComboText", comboContainer);
            comboRect.anchoredPosition = Vector2.zero;
            comboRect.sizeDelta = new Vector2(100, 20);
            comboRect.pivot = new Vector2(1, 1);
            var comboText = AddTextComponent(comboRect.gameObject);
            comboText.text = "";
            comboText.fontSize = 14;
            comboText.color = MagentaAccent;
            comboText.fontStyle = FontStyles.Bold;
            comboText.alignment = TextAlignmentOptions.Right;

            // Multiplier text
            var multRect = CreateUIObject("MultiplierText", comboContainer);
            multRect.anchoredPosition = new Vector2(-110, 0);
            multRect.sizeDelta = new Vector2(50, 20);
            multRect.pivot = new Vector2(1, 1);
            var multText = AddTextComponent(multRect.gameObject);
            multText.text = "";
            multText.fontSize = 14;
            multText.color = GoldLegendary;
            multText.alignment = TextAlignmentOptions.Right;

            comboContainer.gameObject.SetActive(false);

            // Milestone text (center screen popup)
            var milestoneRect = CreateUIObject("MilestoneText", hudRoot);
            SetAnchors(milestoneRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            milestoneRect.anchoredPosition = new Vector2(0, 100);
            milestoneRect.sizeDelta = new Vector2(400, 80);
            var milestoneText = AddTextComponent(milestoneRect.gameObject);
            milestoneText.text = "UNSTOPPABLE!";
            milestoneText.fontSize = 56;
            milestoneText.color = MagentaAccent;
            milestoneText.fontStyle = FontStyles.Bold;
            milestoneText.alignment = TextAlignmentOptions.Center;
            milestoneRect.gameObject.SetActive(false);

            // Wire display fields
            SetPrivateField(display, "m_scoreText", scoreText);
            SetPrivateField(display, "m_deltaText", deltaText);
            SetPrivateField(display, "m_comboText", comboText);
            SetPrivateField(display, "m_multiplierText", multText);
            SetPrivateField(display, "m_comboContainer", comboContainer.gameObject);
            SetPrivateField(display, "m_milestoneText", milestoneText);

            return display;
        }

        #endregion

        #region Level Display V2

        private LevelDisplay BuildLevelDisplayV2(RectTransform hudRoot)
        {
            // Top center
            var container = CreateUIObject("LevelContainer", hudRoot);
            SetAnchors(container, new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            container.anchoredPosition = new Vector2(0, -20);
            container.sizeDelta = new Vector2(120, 36);
            container.pivot = new Vector2(0.5f, 1);

            // Hexagonal badge background
            var badge = CreateUIObject("Badge", container);
            badge.anchoredPosition = Vector2.zero;
            badge.sizeDelta = new Vector2(120, 36);
            var badgeImg = badge.gameObject.AddComponent<Image>();
            badgeImg.sprite = CreateBadgeSprite();
            badgeImg.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);
            badgeImg.type = Image.Type.Sliced;

            // Badge border
            var badgeBorder = CreateUIObject("BadgeBorder", container);
            badgeBorder.anchoredPosition = Vector2.zero;
            badgeBorder.sizeDelta = new Vector2(120, 36);
            var borderImg = badgeBorder.gameObject.AddComponent<Image>();
            borderImg.sprite = CreateBadgeSprite();
            borderImg.color = CyanPrimary.WithAlpha(0.6f);
            borderImg.type = Image.Type.Sliced;
            borderImg.fillCenter = false;

            var root = CreateUIObject("LevelDisplay", container);
            StretchToFill(root);

            var display = root.gameObject.AddComponent<LevelDisplay>();

            // Level text
            var textRect = CreateUIObject("LevelText", root);
            StretchToFill(textRect);
            var levelText = AddTextComponent(textRect.gameObject);
            levelText.text = "LEVEL 1";
            levelText.fontSize = 16;
            levelText.color = CyanPrimary;
            levelText.fontStyle = FontStyles.Bold;
            levelText.characterSpacing = 4f;
            levelText.alignment = TextAlignmentOptions.Center;

            SetPrivateField(display, "m_levelText", levelText);

            return display;
        }

        #endregion

        #region Heat Display V2

        private WeaponHeatDisplay BuildHeatDisplayV2(RectTransform hudRoot)
        {
            // Bottom center weapon status
            var container = CreateUIObject("WeaponContainer", hudRoot);
            SetAnchors(container, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            container.anchoredPosition = new Vector2(0, 24);
            container.sizeDelta = new Vector2(400, 50);
            container.pivot = new Vector2(0.5f, 0);

            var root = CreateUIObject("WeaponHeatDisplay", container);
            StretchToFill(root);

            var display = root.gameObject.AddComponent<WeaponHeatDisplay>();

            // Heat bar container (centered, angular design)
            var barContainer = CreateUIObject("HeatBarContainer", root);
            barContainer.anchoredPosition = new Vector2(0, 0);
            barContainer.sizeDelta = new Vector2(280, 14);

            // Bar background
            var bgRect = CreateUIObject("HeatBG", barContainer);
            StretchToFill(bgRect);
            var bgImg = bgRect.gameObject.AddComponent<Image>();
            bgImg.sprite = CreateAngularBarSprite();
            bgImg.color = new Color(0.06f, 0.06f, 0.1f, 0.9f);
            bgImg.type = Image.Type.Sliced;

            // Heat fill
            var fillRect = CreateUIObject("HeatFill", barContainer);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            var fillImg = fillRect.gameObject.AddComponent<Image>();
            fillImg.sprite = CreateAngularBarSprite();
            fillImg.color = CyanPrimary;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0;

            // Heat label (left)
            var heatLabel = CreateUIObject("HeatLabel", root);
            heatLabel.anchoredPosition = new Vector2(-155, 0);
            heatLabel.sizeDelta = new Vector2(50, 14);
            var heatLabelText = AddTextComponent(heatLabel.gameObject);
            heatLabelText.text = "HEAT";
            heatLabelText.fontSize = 10;
            heatLabelText.color = new Color(0.5f, 0.55f, 0.6f);
            heatLabelText.characterSpacing = 4f;
            heatLabelText.alignment = TextAlignmentOptions.Right;

            // Power level (right side)
            var powerRect = CreateUIObject("PowerLevelText", root);
            powerRect.anchoredPosition = new Vector2(155, 0);
            powerRect.sizeDelta = new Vector2(60, 18);
            var powerText = AddTextComponent(powerRect.gameObject);
            powerText.text = "PWR 0";
            powerText.fontSize = 12;
            powerText.color = GoldLegendary;
            powerText.fontStyle = FontStyles.Bold;
            powerText.alignment = TextAlignmentOptions.Left;

            // Overheat warning
            var warningRect = CreateUIObject("OverheatWarning", root);
            warningRect.anchoredPosition = new Vector2(0, 24);
            warningRect.sizeDelta = new Vector2(160, 24);
            var warningText = AddTextComponent(warningRect.gameObject);
            warningText.text = "⚠ OVERHEAT ⚠";
            warningText.fontSize = 16;
            warningText.color = DangerRed;
            warningText.fontStyle = FontStyles.Bold;
            warningText.alignment = TextAlignmentOptions.Center;
            warningRect.gameObject.SetActive(false);

            // Wire display fields
            SetPrivateField(display, "m_heatFill", fillImg);
            SetPrivateField(display, "m_overheatText", warningText);
            SetPrivateField(display, "m_warningContainer", warningRect.gameObject);
            SetPrivateField(display, "m_powerLevelText", powerText);

            return display;
        }

        #endregion

        #region Decorative Elements

        private void BuildScanlineOverlay(RectTransform hudRoot)
        {
            var scanlines = CreateUIObject("ScanlineOverlay", hudRoot);
            StretchToFill(scanlines);
            var img = scanlines.gameObject.AddComponent<Image>();
            img.sprite = CreateScanlineSprite();
            img.color = new Color(0, 0, 0, 0.04f);
            img.type = Image.Type.Tiled;
            img.raycastTarget = false;

            // Ensure scanlines are behind interactive elements
            scanlines.SetAsFirstSibling();
        }

        private Image CreatePanelBackground(RectTransform parent, string name, Color bgColor, Color borderColor)
        {
            // Background
            var bg = CreateUIObject(name + "BG", parent);
            StretchToFill(bg);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = CreateAngularPanelSprite();
            bgImg.color = bgColor;
            bgImg.type = Image.Type.Sliced;
            bgImg.raycastTarget = false;

            // Border
            var border = CreateUIObject(name + "Border", parent);
            StretchToFill(border);
            var borderImg = border.gameObject.AddComponent<Image>();
            borderImg.sprite = CreateAngularPanelSprite();
            borderImg.color = borderColor;
            borderImg.type = Image.Type.Sliced;
            borderImg.fillCenter = false;
            borderImg.raycastTarget = false;

            // Glow
            var glow = CreateUIObject(name + "Glow", parent);
            StretchToFill(glow);
            glow.offsetMin = new Vector2(-4, -4);
            glow.offsetMax = new Vector2(4, 4);
            var glowImg = glow.gameObject.AddComponent<Image>();
            glowImg.sprite = CreateAngularPanelSprite();
            glowImg.color = GlowColor;
            glowImg.type = Image.Type.Sliced;
            glowImg.raycastTarget = false;
            glow.SetAsFirstSibling();

            return bgImg;
        }

        #endregion

        #region Sprite Generation

        private Sprite CreateHexagonSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size * 0.42f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y) - center;

                    // Hexagon distance function
                    float angle = Mathf.Atan2(pos.y, pos.x);
                    float hexAngle = Mathf.PI / 3f;
                    float sectorAngle = Mathf.Abs(((angle % hexAngle) + hexAngle) % hexAngle - hexAngle / 2f);
                    float hexDist = pos.magnitude * Mathf.Cos(sectorAngle) / Mathf.Cos(hexAngle / 2f);

                    if (hexDist <= radius)
                    {
                        float edge = radius - hexDist;
                        float alpha = Mathf.Clamp01(edge * 2f);
                        pixels[y * size + x] = new Color(1, 1, 1, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateAngularBarSprite()
        {
            int width = 64;
            int height = 16;
            int border = 4;
            int angle = 4; // Angle cut on ends

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = true;

                    // Left angular cut
                    if (x < angle)
                    {
                        int minY = angle - x;
                        int maxY = height - minY;
                        inside = y >= minY && y < maxY;
                    }
                    // Right angular cut
                    else if (x >= width - angle)
                    {
                        int offset = x - (width - angle);
                        int minY = offset;
                        int maxY = height - offset;
                        inside = y >= minY && y < maxY;
                    }

                    pixels[y * width + x] = inside ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        private Sprite CreateAngularPanelSprite()
        {
            int size = 32;
            int border = 8;
            int corner = 6;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = true;

                    // Top-left corner cut
                    if (x < corner && y >= size - corner)
                    {
                        inside = (x + (size - y)) >= corner;
                    }
                    // Bottom-right corner cut
                    if (x >= size - corner && y < corner)
                    {
                        inside = inside && ((size - x) + y) >= corner;
                    }

                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        private Sprite CreateBadgeSprite()
        {
            int width = 64;
            int height = 24;
            int border = 6;
            int angle = 8;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = true;

                    // Left angular cut
                    if (x < angle)
                    {
                        float progress = x / (float)angle;
                        int minY = (int)((1f - progress) * height * 0.3f);
                        int maxY = height - minY;
                        inside = y >= minY && y < maxY;
                    }
                    // Right angular cut
                    if (x >= width - angle)
                    {
                        float progress = (width - x) / (float)angle;
                        int minY = (int)((1f - progress) * height * 0.3f);
                        int maxY = height - minY;
                        inside = inside && y >= minY && y < maxY;
                    }

                    pixels[y * width + x] = inside ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        private Sprite CreateScanlineSprite()
        {
            int size = 4;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Every other row is darker
                    pixels[y * size + x] = (y % 2 == 0) ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, Vector4.zero);
        }

        #endregion
    }
}
