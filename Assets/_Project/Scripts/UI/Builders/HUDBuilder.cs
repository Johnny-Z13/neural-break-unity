using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuralBreak.UI.Builders
{
    /// <summary>
    /// Builds the in-game HUD (Health, Score, Heat, Level displays).
    /// Constructs all HUD components and wires them to HUDController.
    /// </summary>
    public class HUDBuilder : UIScreenBuilderBase
    {
        public HUDBuilder(TMP_FontAsset fontAsset, bool useThemeColors = true)
            : base(fontAsset, useThemeColors) { }

        /// <summary>
        /// Build complete HUD hierarchy
        /// </summary>
        public GameObject BuildHUD(Transform canvasTransform)
        {
            var hud = CreateUIObject("HUD", canvasTransform);
            StretchToFill(hud);
            hud.gameObject.AddComponent<HUDController>();
            hud.gameObject.SetActive(false); // UIManager will show based on game state

            // Build HUD sections
            var healthDisplay = BuildHealthDisplay(hud);
            var scoreDisplay = BuildScoreDisplay(hud);
            var levelDisplay = BuildLevelDisplay(hud);
            var heatDisplay = BuildHeatDisplay(hud);

            // Wire HUDController
            var hudCtrl = hud.gameObject.GetComponent<HUDController>();
            SetPrivateField(hudCtrl, "_healthDisplay", healthDisplay);
            SetPrivateField(hudCtrl, "_scoreDisplay", scoreDisplay);
            SetPrivateField(hudCtrl, "_heatDisplay", heatDisplay);
            SetPrivateField(hudCtrl, "_levelDisplay", levelDisplay);

            return hud.gameObject;
        }

        #region Health Display

        private HealthDisplay BuildHealthDisplay(RectTransform hudRoot)
        {
            // Top Left container
            var topLeft = CreateUIObject("TopLeft", hudRoot);
            SetAnchors(topLeft, new Vector2(0, 1), new Vector2(0, 1));
            topLeft.anchoredPosition = new Vector2(20, -20);
            topLeft.sizeDelta = new Vector2(300, 100);
            topLeft.pivot = new Vector2(0, 1);

            // Health display root
            var root = CreateUIObject("HealthDisplay", topLeft);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(250, 30);
            root.pivot = new Vector2(0, 1);

            var display = root.gameObject.AddComponent<HealthDisplay>();

            // Health bar
            var bgRect = CreateUIObject("HealthBG", root);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(200, 20);
            var bgImg = bgRect.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Health bar fill
            var fillRect = CreateUIObject("HealthFill", bgRect);
            StretchToFill(fillRect);
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
            healthText.color = TextColor;
            healthText.alignment = TextAlignmentOptions.Left;

            // Shield icons
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
            SetPrivateField(display, "_healthFill", fillImg);
            SetPrivateField(display, "_healthText", healthText);
            SetPrivateField(display, "_shieldIcons", shieldIcons);
            SetPrivateField(display, "_shieldContainer", shieldContainer.transform);

            return display;
        }

        #endregion

        #region Score Display

        private ScoreDisplay BuildScoreDisplay(RectTransform hudRoot)
        {
            // Top Right container
            var topRight = CreateUIObject("TopRight", hudRoot);
            SetAnchors(topRight, new Vector2(1, 1), new Vector2(1, 1));
            topRight.anchoredPosition = new Vector2(-20, -20);
            topRight.sizeDelta = new Vector2(300, 120);
            topRight.pivot = new Vector2(1, 1);

            // Score display root
            var root = CreateUIObject("ScoreDisplay", topRight);
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
            scoreText.color = PrimaryColor;
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
            comboText.color = AccentColor;
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

            // Milestone text
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
            SetPrivateField(display, "_scoreText", scoreText);
            SetPrivateField(display, "_deltaText", deltaText);
            SetPrivateField(display, "_comboText", comboText);
            SetPrivateField(display, "_multiplierText", multText);
            SetPrivateField(display, "_comboContainer", comboContainer.gameObject);
            SetPrivateField(display, "_milestoneText", milestoneText);

            return display;
        }

        #endregion

        #region Level Display

        private LevelDisplay BuildLevelDisplay(RectTransform hudRoot)
        {
            // Top Right, below score
            var topRight = CreateUIObject("TopRightLevel", hudRoot);
            SetAnchors(topRight, new Vector2(1, 1), new Vector2(1, 1));
            topRight.anchoredPosition = new Vector2(-20, -110);
            topRight.sizeDelta = new Vector2(150, 30);
            topRight.pivot = new Vector2(1, 1);

            var root = CreateUIObject("LevelDisplay", topRight);
            StretchToFill(root);

            var display = root.gameObject.AddComponent<LevelDisplay>();

            // Level text
            var textRect = CreateUIObject("LevelText", root);
            StretchToFill(textRect);
            var levelText = AddTextComponent(textRect.gameObject);
            levelText.text = "LEVEL 1";
            levelText.fontSize = 20;
            levelText.color = TextColor;
            levelText.alignment = TextAlignmentOptions.Right;

            // Wire display field
            SetPrivateField(display, "_levelText", levelText);

            return display;
        }

        #endregion

        #region Heat Display

        private WeaponHeatDisplay BuildHeatDisplay(RectTransform hudRoot)
        {
            // Bottom Center
            var bottomCenter = CreateUIObject("BottomCenter", hudRoot);
            SetAnchors(bottomCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            bottomCenter.anchoredPosition = new Vector2(0, 30);
            bottomCenter.sizeDelta = new Vector2(300, 40);
            bottomCenter.pivot = new Vector2(0.5f, 0);

            var root = CreateUIObject("WeaponHeatDisplay", bottomCenter);
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

            // Power level text
            var powerRect = CreateUIObject("PowerLevelText", root);
            powerRect.anchorMin = new Vector2(0, 0.5f);
            powerRect.anchorMax = new Vector2(0, 0.5f);
            powerRect.anchoredPosition = new Vector2(-5, 0);
            powerRect.sizeDelta = new Vector2(60, 25);
            powerRect.pivot = new Vector2(1, 0.5f);
            var powerText = AddTextComponent(powerRect.gameObject);
            powerText.text = "PWR 0";
            powerText.fontSize = 14;
            powerText.color = new Color(1f, 0.8f, 0f);
            powerText.alignment = TextAlignmentOptions.Right;
            powerText.fontStyle = FontStyles.Bold;

            // Wire display fields
            SetPrivateField(display, "_heatFill", fillImg);
            SetPrivateField(display, "_overheatText", warningText);
            SetPrivateField(display, "_warningContainer", warningRect.gameObject);
            SetPrivateField(display, "_powerLevelText", powerText);

            return display;
        }

        #endregion
    }
}
