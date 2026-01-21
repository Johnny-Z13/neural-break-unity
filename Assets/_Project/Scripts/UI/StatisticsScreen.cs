using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays end-game statistics screen.
    /// Shows detailed stats with animated count-up effects.
    /// </summary>
    public class StatisticsScreen : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _fadeInDuration = 0.5f;
        [SerializeField] private float _statRevealDelay = 0.15f;
        [SerializeField] private float _countUpSpeed = 0.5f;

        [Header("Colors")]
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.9f);
        [SerializeField] private Color _titleColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color _labelColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color _valueColor = Color.white;
        [SerializeField] private Color _highlightColor = new Color(0.3f, 1f, 0.4f);

        // UI Components
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private RectTransform _container;
        private TextMeshProUGUI _titleText;
        private StatRow[] _statRows;
        private TextMeshProUGUI _continueText;

        private bool _isVisible;
        private bool _statsRevealed;
        private Coroutine _revealCoroutine;

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
            if (_isVisible && _statsRevealed)
            {
                // Any key/click to continue
                if (UnityEngine.Input.anyKeyDown || UnityEngine.Input.GetMouseButtonDown(0))
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
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 300;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = _backgroundColor;

            // Container
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(canvasGO.transform);
            _container = containerGO.AddComponent<RectTransform>();
            _container.anchorMin = new Vector2(0.5f, 0.5f);
            _container.anchorMax = new Vector2(0.5f, 0.5f);
            _container.pivot = new Vector2(0.5f, 0.5f);
            _container.anchoredPosition = Vector2.zero;
            _container.sizeDelta = new Vector2(500, 600);

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(_container);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(400, 60);

            _titleText = titleGO.AddComponent<TextMeshProUGUI>();
            _titleText.text = "GAME OVER";
            _titleText.fontSize = 48;
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = _titleColor;
            _titleText.alignment = TextAlignmentOptions.Center;

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

            _statRows = new StatRow[labels.Length];
            float startY = -100f;
            float rowHeight = 40f;

            for (int i = 0; i < labels.Length; i++)
            {
                _statRows[i] = CreateStatRow(_container, labels[i], startY - (i * rowHeight));
            }

            // Continue text
            var continueGO = new GameObject("ContinueText");
            continueGO.transform.SetParent(_container);
            var continueRect = continueGO.AddComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(0.5f, 0f);
            continueRect.anchorMax = new Vector2(0.5f, 0f);
            continueRect.pivot = new Vector2(0.5f, 0f);
            continueRect.anchoredPosition = new Vector2(0, 30);
            continueRect.sizeDelta = new Vector2(400, 30);

            _continueText = continueGO.AddComponent<TextMeshProUGUI>();
            _continueText.text = "Press any key to continue";
            _continueText.fontSize = 18;
            _continueText.color = new Color(0.5f, 0.5f, 0.5f);
            _continueText.alignment = TextAlignmentOptions.Center;
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
            row.labelText.color = _labelColor;
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
            row.valueText.color = _valueColor;
            row.valueText.alignment = TextAlignmentOptions.Right;

            return row;
        }

        private void OnGameOver(GameOverEvent evt)
        {
            _titleText.text = "GAME OVER";
            _titleText.color = new Color(1f, 0.3f, 0.3f);
            Show(evt.finalStats);
        }

        private void OnVictory(VictoryEvent evt)
        {
            _titleText.text = "VICTORY!";
            _titleText.color = _highlightColor;
            Show(evt.finalStats);
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            Hide();
        }

        public void Show(GameStats stats)
        {
            _isVisible = true;
            _statsRevealed = false;
            _canvasGroup.blocksRaycasts = true;

            if (_revealCoroutine != null)
            {
                StopCoroutine(_revealCoroutine);
            }
            _revealCoroutine = StartCoroutine(RevealStats(stats));
        }

        public void Hide()
        {
            _isVisible = false;
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;

            // Reset row visibility
            foreach (var row in _statRows)
            {
                row.canvasGroup.alpha = 0;
            }
        }

        private IEnumerator RevealStats(GameStats stats)
        {
            // Fade in background
            float elapsed = 0f;
            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = elapsed / _fadeInDuration;
                yield return null;
            }
            _canvasGroup.alpha = 1f;

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
                AchievementSystem.Instance != null
                    ? $"{AchievementSystem.Instance.GetUnlockedCount()}/{AchievementSystem.Instance.GetTotalCount()}"
                    : "0/0"
            };

            // Reveal each row with delay
            for (int i = 0; i < _statRows.Length; i++)
            {
                _statRows[i].valueText.text = values[i];

                // Fade in row
                elapsed = 0f;
                float fadeDuration = 0.2f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _statRows[i].canvasGroup.alpha = elapsed / fadeDuration;
                    yield return null;
                }
                _statRows[i].canvasGroup.alpha = 1f;

                // Highlight special values
                if (i == 0 && stats.score > 100000) // High score
                {
                    _statRows[i].valueText.color = _highlightColor;
                }
                else if (i == 4 && stats.highestCombo >= 50) // High combo
                {
                    _statRows[i].valueText.color = _highlightColor;
                }

                yield return new WaitForSecondsRealtime(_statRevealDelay);
            }

            // Show continue text
            _continueText.gameObject.SetActive(true);
            StartCoroutine(PulseContinueText());

            _statsRevealed = true;
        }

        private IEnumerator PulseContinueText()
        {
            while (_isVisible)
            {
                float pulse = Mathf.Sin(Time.unscaledTime * 3f) * 0.3f + 0.7f;
                Color c = _continueText.color;
                c.a = pulse;
                _continueText.color = c;
                yield return null;
            }
        }

        private string FormatTime(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{mins}:{secs:D2}";
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
