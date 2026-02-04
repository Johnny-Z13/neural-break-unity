using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays end-game statistics screen.
    /// Shows detailed stats with animated count-up effects.
    /// </summary>
    public class StatisticsScreen : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float m_fadeInDuration = 0.5f;
        [SerializeField] private float m_statRevealDelay = 0.15f;
#pragma warning disable CS0414 // Reserved for animated stat count-up feature
        [SerializeField] private float m_countUpSpeed = 0.5f;
#pragma warning restore CS0414

        [Header("Colors")]
        [SerializeField] private Color m_backgroundColor = new Color(0f, 0f, 0f, 0.9f);
        [SerializeField] private Color m_titleColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color m_labelColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color m_valueColor = Color.white;
        [SerializeField] private Color m_highlightColor = new Color(0.3f, 1f, 0.4f);

        // UI Components
        private Canvas m_canvas;
        private CanvasGroup m_canvasGroup;
        private RectTransform m_container;
        private TextMeshProUGUI m_titleText;
        private StatRow[] m_statRows;
        private TextMeshProUGUI m_continueText;

        private bool m_isVisible;
        private bool m_statsRevealed;
        private Coroutine m_revealCoroutine;

        private class StatRow
        {
            public GameObject gameObject;
            public TextMeshProUGUI labelText;
            public TextMeshProUGUI valueText;
            public CanvasGroup canvasGroup;
        }

        private void Awake()
        {
            CreateUI();
            Hide();
        }

        private void Start()
        {
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<VictoryEvent>(OnVictory);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<VictoryEvent>(OnVictory);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void Update()
        {
            if (m_isVisible && m_statsRevealed)
            {
                // Any key/click to continue (using Input System)
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame ||
                    UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Hide();
                }
            }
        }

        private void CreateUI()
        {
            // Create canvas
            var canvasGO = new GameObject("StatisticsCanvas");
            canvasGO.transform.SetParent(transform);
            m_canvas = canvasGO.AddComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = 300;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            m_canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            m_canvasGroup.alpha = 0;

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = m_backgroundColor;

            // Container
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(canvasGO.transform);
            m_container = containerGO.AddComponent<RectTransform>();
            m_container.anchorMin = new Vector2(0.5f, 0.5f);
            m_container.anchorMax = new Vector2(0.5f, 0.5f);
            m_container.pivot = new Vector2(0.5f, 0.5f);
            m_container.anchoredPosition = Vector2.zero;
            m_container.sizeDelta = new Vector2(500, 600);

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(m_container);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(400, 60);

            m_titleText = titleGO.AddComponent<TextMeshProUGUI>();
            m_titleText.text = "GAME OVER";
            m_titleText.fontSize = 48;
            m_titleText.fontStyle = FontStyles.Bold;
            m_titleText.color = m_titleColor;
            m_titleText.alignment = TextAlignmentOptions.Center;

            // Create stat rows
            string[] labels = new string[]
            {
                "Final Score",
                "Highest Level",
                "Survival Time",
                "Enemies Killed",
                "Highest Combo",
                "Max Multiplier",
                "Total XP Earned",
                "Damage Taken",
                "Bosses Defeated",
                "Achievements"
            };

            m_statRows = new StatRow[labels.Length];
            float startY = -100f;
            float rowHeight = 40f;

            for (int i = 0; i < labels.Length; i++)
            {
                m_statRows[i] = CreateStatRow(m_container, labels[i], startY - (i * rowHeight));
            }

            // Continue text
            var continueGO = new GameObject("ContinueText");
            continueGO.transform.SetParent(m_container);
            var continueRect = continueGO.AddComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(0.5f, 0f);
            continueRect.anchorMax = new Vector2(0.5f, 0f);
            continueRect.pivot = new Vector2(0.5f, 0f);
            continueRect.anchoredPosition = new Vector2(0, 30);
            continueRect.sizeDelta = new Vector2(400, 30);

            m_continueText = continueGO.AddComponent<TextMeshProUGUI>();
            m_continueText.text = "Press any key to continue";
            m_continueText.fontSize = 18;
            m_continueText.color = new Color(0.5f, 0.5f, 0.5f);
            m_continueText.alignment = TextAlignmentOptions.Center;
        }

        private StatRow CreateStatRow(RectTransform parent, string label, float yPos)
        {
            var row = new StatRow();

            row.gameObject = new GameObject($"Row_{label}");
            row.gameObject.transform.SetParent(parent);

            var rowRect = row.gameObject.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 1f);
            rowRect.anchorMax = new Vector2(0.5f, 1f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = new Vector2(0, yPos);
            rowRect.sizeDelta = new Vector2(450, 35);

            row.canvasGroup = row.gameObject.AddComponent<CanvasGroup>();
            row.canvasGroup.alpha = 0;

            // Label (left)
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(row.gameObject.transform);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0, 0.5f);
            labelRect.pivot = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(0, 0);
            labelRect.sizeDelta = new Vector2(250, 30);

            row.labelText = labelGO.AddComponent<TextMeshProUGUI>();
            row.labelText.text = label;
            row.labelText.fontSize = 20;
            row.labelText.color = m_labelColor;
            row.labelText.alignment = TextAlignmentOptions.Left;

            // Value (right)
            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(row.gameObject.transform);
            var valueRect = valueGO.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(1, 0.5f);
            valueRect.anchorMax = new Vector2(1, 0.5f);
            valueRect.pivot = new Vector2(1, 0.5f);
            valueRect.anchoredPosition = new Vector2(0, 0);
            valueRect.sizeDelta = new Vector2(200, 30);

            row.valueText = valueGO.AddComponent<TextMeshProUGUI>();
            row.valueText.text = "0";
            row.valueText.fontSize = 22;
            row.valueText.fontStyle = FontStyles.Bold;
            row.valueText.color = m_valueColor;
            row.valueText.alignment = TextAlignmentOptions.Right;

            return row;
        }

        private void OnGameOver(GameOverEvent evt)
        {
            m_titleText.text = "GAME OVER";
            m_titleText.color = new Color(1f, 0.3f, 0.3f);
            Show(evt.finalStats);
        }

        private void OnVictory(VictoryEvent evt)
        {
            m_titleText.text = "VICTORY!";
            m_titleText.color = m_highlightColor;
            Show(evt.finalStats);
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            Hide();
        }

        public void Show(GameStats stats)
        {
            m_isVisible = true;
            m_statsRevealed = false;
            m_canvasGroup.blocksRaycasts = true;

            if (m_revealCoroutine != null)
            {
                StopCoroutine(m_revealCoroutine);
            }
            m_revealCoroutine = StartCoroutine(RevealStats(stats));
        }

        public void Hide()
        {
            m_isVisible = false;
            m_canvasGroup.alpha = 0;
            m_canvasGroup.blocksRaycasts = false;

            // Reset row visibility
            foreach (var row in m_statRows)
            {
                row.canvasGroup.alpha = 0;
            }
        }

        private IEnumerator RevealStats(GameStats stats)
        {
            // Fade in background
            float elapsed = 0f;
            while (elapsed < m_fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                m_canvasGroup.alpha = elapsed / m_fadeInDuration;
                yield return null;
            }
            m_canvasGroup.alpha = 1f;

            // Prepare values
            string[] values = new string[]
            {
                stats.score.ToString("N0"),
                $"Level {stats.level}",
                FormatTime(stats.survivedTime),
                stats.enemiesKilled.ToString("N0"),
                $"{stats.highestCombo}x",
                $"x{stats.highestMultiplier:F1}",
                stats.totalXP.ToString("N0"),
                stats.damageTaken.ToString("N0"),
                stats.bossesKilled.ToString(),
                GetAchievementProgress()
            };

            // Reveal each row with delay
            for (int i = 0; i < m_statRows.Length; i++)
            {
                m_statRows[i].valueText.text = values[i];

                // Fade in row
                elapsed = 0f;
                float fadeDuration = 0.2f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    m_statRows[i].canvasGroup.alpha = elapsed / fadeDuration;
                    yield return null;
                }
                m_statRows[i].canvasGroup.alpha = 1f;

                // Highlight special values
                if (i == 0 && stats.score > 100000) // High score
                {
                    m_statRows[i].valueText.color = m_highlightColor;
                }
                else if (i == 4 && stats.highestCombo >= 50) // High combo
                {
                    m_statRows[i].valueText.color = m_highlightColor;
                }

                yield return new WaitForSecondsRealtime(m_statRevealDelay);
            }

            // Show continue text
            m_continueText.gameObject.SetActive(true);
            StartCoroutine(PulseContinueText());

            m_statsRevealed = true;
        }

        private IEnumerator PulseContinueText()
        {
            while (m_isVisible)
            {
                float pulse = Mathf.Sin(Time.unscaledTime * 3f) * 0.3f + 0.7f;
                Color c = m_continueText.color;
                c.a = pulse;
                m_continueText.color = c;
                yield return null;
            }
        }

        private string FormatTime(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{mins}:{secs:D2}";
        }

        private string GetAchievementProgress()
        {
            var achievementSystem = FindAnyObjectByType<AchievementSystem>();
            if (achievementSystem != null)
            {
                return $"{achievementSystem.GetUnlockedCount()}/{achievementSystem.GetTotalCount()}";
            }
            return "0/0";
        }

        #region Debug

        [ContextMenu("Debug: Show Sample Stats")]
        private void DebugShowStats()
        {
            var stats = new GameStats
            {
                score = 125000,
                level = 15,
                survivedTime = 312f,
                enemiesKilled = 487,
                highestCombo = 35,
                highestMultiplier = 5.2f,
                totalXP = 892,
                damageTaken = 45,
                bossesKilled = 2
            };
            Show(stats);
        }

        #endregion
    }
}
