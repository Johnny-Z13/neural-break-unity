using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuralBreak.UI.Builders
{
    /// <summary>
    /// Builds the pause menu with resume, restart, and quit buttons.
    /// </summary>
    public class PauseMenuBuilder : UIScreenBuilderBase
    {
        public PauseMenuBuilder(TMP_FontAsset fontAsset, bool useThemeColors = true)
            : base(fontAsset, useThemeColors) { }

        /// <summary>
        /// Build pause menu screen
        /// </summary>
        public GameObject BuildPauseScreen(Transform canvasTransform)
        {
            var screen = CreateUIObject("PauseScreen", canvasTransform);
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
            panelImg.color = BackgroundColor;

            // Title
            var titleRect = CreateUIObject("Title", container);
            titleRect.anchoredPosition = new Vector2(0, 120);
            titleRect.sizeDelta = new Vector2(300, 60);
            var titleText = AddTextComponent(titleRect.gameObject);
            titleText.text = "PAUSED";
            titleText.fontSize = 48;
            titleText.color = PrimaryColor;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // Buttons
            var resumeBtn = CreateButton("ResumeButton", container, "RESUME", new Vector2(0, 30), new Vector2(180, 50));
            var restartBtn = CreateButton("RestartButton", container, "RESTART", new Vector2(0, -35), new Vector2(180, 50));
            var quitBtn = CreateButton("QuitButton", container, "MAIN MENU", new Vector2(0, -100), new Vector2(180, 50));

            // Wire PauseScreen fields
            SetPrivateField(pauseScreen, "m_screenRoot", screen.gameObject);
            SetPrivateField(pauseScreen, "m_resumeButton", resumeBtn);
            SetPrivateField(pauseScreen, "m_restartButton", restartBtn);
            SetPrivateField(pauseScreen, "m_quitButton", quitBtn);
            SetPrivateField(pauseScreen, "m_firstSelected", resumeBtn);

            screen.gameObject.SetActive(false);
            return screen.gameObject;
        }
    }
}
