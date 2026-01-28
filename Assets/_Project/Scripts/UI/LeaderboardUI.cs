using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays high scores and personal best records.
    /// Can be shown from main menu or after game over.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _createOnAwake = true;
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0.1f, 0.95f);
        [SerializeField] private Color _headerColor = new Color(0f, 0.8f, 1f);
        [SerializeField] private Color _labelColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color _valueColor = Color.white;
        [SerializeField] private Color _newRecordColor = new Color(1f, 0.8f, 0f);

        // UI Components
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private RectTransform _panel;
        private TextMeshProUGUI _titleText;
        private List<StatRow> _statRows = new List<StatRow>();
        private Button _closeButton;
        private TextMeshProUGUI _closeButtonText;

        private bool _isVisible;
        private HashSet<HighScoreType> _newRecords = new HashSet<HighScoreType>();

        private class StatRow
        {
            public RectTransform row;
            public TextMeshProUGUI label;
            public TextMeshProUGUI value;
            public Image icon;
            public HighScoreType? type;
        }

        private void Awake()
        {
            if (_createOnAwake)
            {
                CreateUI();
                Hide();
            }
        }

        private void Start()
        {
            if (FindFirstObjectByType<HighScoreManager>() != null)
            {
                FindFirstObjectByType<HighScoreManager>().OnNewHighScore += OnNewHighScore;
                FindFirstObjectByType<HighScoreManager>().OnNewHighScoreFloat += OnNewHighScoreFloat;
            }
        }

        private void OnDestroy()
        {
            if (FindFirstObjectByType<HighScoreManager>() != null)
            {
                FindFirstObjectByType<HighScoreManager>().OnNewHighScore -= OnNewHighScore;
                FindFirstObjectByType<HighScoreManager>().OnNewHighScoreFloat -= OnNewHighScoreFloat;
            }
        }

        private void Update()
        {
            if (_isVisible)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                {
                    Hide();
                }
            }
        }

        private void OnNewHighScore(HighScoreType type, int value)
        {
            _newRecords.Add(type);
        }

        private void OnNewHighScoreFloat(HighScoreType type, float value)
        {
            _newRecords.Add(type);
        }

        private void CreateUI()
        {
            // Create canvas
            var canvasGO = new GameObject("LeaderboardCanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 250;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;

            // Background overlay
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = _backgroundColor;

            // Panel
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(canvasGO.transform);
            _panel = panelGO.AddComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(500, 550);

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(_panel);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(400, 50);

            _titleText = titleGO.AddComponent<TextMeshProUGUI>();
            _titleText.text = "HIGH SCORES";
            _titleText.fontSize = 36;
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = _headerColor;
            _titleText.alignment = TextAlignmentOptions.Center;

            // Create stat rows
            CreateStatRows();

            // Close button
            CreateCloseButton();
        }

        private void CreateStatRows()
        {
            string[] labels = new string[]
            {
                "High Score",
                "Best Level",
                "Longest Survival",
                "Most Kills",
                "Highest Combo",
                "Best Multiplier",
                "",
                "Games Played",
                "Total Play Time"
            };

            HighScoreType?[] types = new HighScoreType?[]
            {
                HighScoreType.Score,
                HighScoreType.Level,
                HighScoreType.Survival,
                HighScoreType.Kills,
                HighScoreType.Combo,
                HighScoreType.Multiplier,
                null,
                null,
                null
            };

            float startY = -90f;
            float rowHeight = 45f;

            for (int i = 0; i < labels.Length; i++)
            {
                if (string.IsNullOrEmpty(labels[i]))
                {
                    // Spacer
                    _statRows.Add(null);
                    continue;
                }

                var row = CreateStatRow(labels[i], startY - (i * rowHeight), types[i]);
                _statRows.Add(row);
            }
        }

        private StatRow CreateStatRow(string label, float yPos, HighScoreType? type)
        {
            var row = new StatRow { type = type };

            var rowGO = new GameObject($"Row_{label}");
            rowGO.transform.SetParent(_panel);
            row.row = rowGO.AddComponent<RectTransform>();
            row.row.anchorMin = new Vector2(0.5f, 1f);
            row.row.anchorMax = new Vector2(0.5f, 1f);
            row.row.pivot = new Vector2(0.5f, 0.5f);
            row.row.anchoredPosition = new Vector2(0, yPos);
            row.row.sizeDelta = new Vector2(420, 40);

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(row.row);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0, 0.5f);
            labelRect.pivot = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(20, 0);
            labelRect.sizeDelta = new Vector2(220, 35);

            row.label = labelGO.AddComponent<TextMeshProUGUI>();
            row.label.text = label;
            row.label.fontSize = 22;
            row.label.color = _labelColor;
            row.label.alignment = TextAlignmentOptions.Left;

            // Value
            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(row.row);
            var valueRect = valueGO.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(1, 0.5f);
            valueRect.anchorMax = new Vector2(1, 0.5f);
            valueRect.pivot = new Vector2(1, 0.5f);
            valueRect.anchoredPosition = new Vector2(-20, 0);
            valueRect.sizeDelta = new Vector2(180, 35);

            row.value = valueGO.AddComponent<TextMeshProUGUI>();
            row.value.text = "0";
            row.value.fontSize = 24;
            row.value.fontStyle = FontStyles.Bold;
            row.value.color = _valueColor;
            row.value.alignment = TextAlignmentOptions.Right;

            return row;
        }

        private void CreateCloseButton()
        {
            var buttonGO = new GameObject("CloseButton");
            buttonGO.transform.SetParent(_panel);
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0, 20);
            buttonRect.sizeDelta = new Vector2(180, 45);

            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 0.9f);

            _closeButton = buttonGO.AddComponent<Button>();
            _closeButton.onClick.AddListener(Hide);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _closeButtonText = textGO.AddComponent<TextMeshProUGUI>();
            _closeButtonText.text = "CLOSE";
            _closeButtonText.fontSize = 22;
            _closeButtonText.fontStyle = FontStyles.Bold;
            _closeButtonText.color = Color.white;
            _closeButtonText.alignment = TextAlignmentOptions.Center;
        }

        public void Show()
        {
            if (_canvas == null)
            {
                CreateUI();
            }

            UpdateValues();
            _isVisible = true;
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;

            // Clear new records after showing
            _newRecords.Clear();
        }

        public void Hide()
        {
            _isVisible = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        private void UpdateValues()
        {
            var mgr = FindAnyObjectByType<HighScoreManager>();
            if (mgr == null) return;

            // Update values
            SetRowValue(0, FormatNumber(mgr.HighScore), HighScoreType.Score);
            SetRowValue(1, $"Level {mgr.BestLevel}", HighScoreType.Level);
            SetRowValue(2, mgr.GetFormattedSurvivalTime(), HighScoreType.Survival);
            SetRowValue(3, FormatNumber(mgr.MostKills), HighScoreType.Kills);
            SetRowValue(4, $"{mgr.HighestCombo}x", HighScoreType.Combo);
            SetRowValue(5, $"x{mgr.HighestMultiplier:F1}", HighScoreType.Multiplier);
            // Row 6 is spacer
            SetRowValue(7, mgr.GamesPlayed.ToString());
            SetRowValue(8, mgr.GetFormattedTotalPlayTime());
        }

        private void SetRowValue(int index, string value, HighScoreType? type = null)
        {
            if (index < 0 || index >= _statRows.Count || _statRows[index] == null) return;

            var row = _statRows[index];
            row.value.text = value;

            // Highlight new records
            if (type.HasValue && _newRecords.Contains(type.Value))
            {
                row.value.color = _newRecordColor;
                row.label.text = row.label.text.Replace(" (NEW!)", "") + " (NEW!)";
            }
            else
            {
                row.value.color = _valueColor;
            }
        }

        private string FormatNumber(int value)
        {
            if (value >= 1000000)
                return $"{value / 1000000f:F1}M";
            if (value >= 1000)
                return $"{value / 1000f:F1}K";
            return value.ToString("N0");
        }

        #region Debug

        [ContextMenu("Debug: Show Leaderboard")]
        private void DebugShow() => Show();

        [ContextMenu("Debug: Hide Leaderboard")]
        private void DebugHide() => Hide();

        #endregion
    }
}
