using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuralBreak.UI.Builders
{
    /// <summary>
    /// Builds the game over screen with final stats and restart/menu buttons.
    /// </summary>
    public class GameOverScreenBuilder : UIScreenBuilderBase
    {
        public GameOverScreenBuilder(TMP_FontAsset fontAsset, bool useThemeColors = true)
            : base(fontAsset, useThemeColors) { }

        /// <summary>
        /// Build game over screen with stats
        /// </summary>
        public GameObject BuildGameOverScreen(Transform canvasTransform)
        {
            var screen = CreateUIObject("GameOverScreen", canvasTransform);
            StretchToFill(screen);

            var gameOverScreen = screen.gameObject.AddComponent<GameOverScreen>();

            // Background
            var bgImg = screen.gameObject.AddComponent<Image>();
            bgImg.color = BackgroundColor;

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
            titleText.color = AccentColor;
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
            SetPrivateField(gameOverScreen, "_screenRoot", screen.gameObject);
            SetPrivateField(gameOverScreen, "_titleText", titleText);
            SetPrivateField(gameOverScreen, "_finalScoreText", scoreText);
            SetPrivateField(gameOverScreen, "_timeSurvivedText", timeText);
            SetPrivateField(gameOverScreen, "_enemiesKilledText", killsText);
            SetPrivateField(gameOverScreen, "_levelReachedText", levelText);
            SetPrivateField(gameOverScreen, "_highestComboText", comboText);
            SetPrivateField(gameOverScreen, "_highestMultiplierText", multText);
            SetPrivateField(gameOverScreen, "_restartButton", restartBtn);
            SetPrivateField(gameOverScreen, "_mainMenuButton", menuBtn);
            SetPrivateField(gameOverScreen, "_firstSelected", restartBtn);

            screen.gameObject.SetActive(false);
            return screen.gameObject;
        }

        /// <summary>
        /// Create stat text line
        /// </summary>
        private TextMeshProUGUI CreateStatText(RectTransform parent, string name, string text, float yPos)
        {
            var rect = CreateUIObject(name, parent);
            rect.anchoredPosition = new Vector2(0, yPos);
            rect.sizeDelta = new Vector2(400, 30);
            var tmp = AddTextComponent(rect.gameObject);
            tmp.text = text;
            tmp.fontSize = 22;
            tmp.color = TextColor;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }
    }
}
