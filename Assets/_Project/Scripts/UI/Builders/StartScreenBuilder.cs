using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuralBreak.UI.Builders
{
    /// <summary>
    /// Builds the start/title screen with game logo and play button.
    /// </summary>
    public class StartScreenBuilder : UIScreenBuilderBase
    {
        public StartScreenBuilder(TMP_FontAsset fontAsset, bool useThemeColors = true)
            : base(fontAsset, useThemeColors) { }

        /// <summary>
        /// Build start screen with title and play button
        /// </summary>
        public GameObject BuildStartScreen(Transform canvasTransform)
        {
            var screen = CreateUIObject("StartScreen", canvasTransform);
            StretchToFill(screen);

            var startScreen = screen.gameObject.AddComponent<StartScreen>();

            // Background panel
            var bgImg = screen.gameObject.AddComponent<Image>();
            bgImg.color = BackgroundColor;

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
            titleText.color = PrimaryColor;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // Subtitle
            var subRect = CreateUIObject("Subtitle", container);
            subRect.anchoredPosition = new Vector2(0, 30);
            subRect.sizeDelta = new Vector2(600, 40);
            var subText = AddTextComponent(subRect.gameObject);
            subText.text = "SURVIVE THE DIGITAL SWARM";
            subText.fontSize = 24;
            subText.color = TextColor;
            subText.alignment = TextAlignmentOptions.Center;

            // Play button
            var playBtn = CreateButton("PlayButton", container, "PLAY", new Vector2(0, -80), new Vector2(200, 60));

            // Wire StartScreen fields
            SetPrivateField(startScreen, "_screenRoot", screen.gameObject);
            SetPrivateField(startScreen, "_titleText", titleText);
            SetPrivateField(startScreen, "_subtitleText", subText);
            SetPrivateField(startScreen, "_playButton", playBtn);
            SetPrivateField(startScreen, "_firstSelected", playBtn);

            return screen.gameObject;
        }
    }
}
