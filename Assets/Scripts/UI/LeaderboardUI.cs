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
        [SerializeField] private bool m_createOnAwake = true;
        [SerializeField] private Color m_backgroundColor = new Color(0f, 0f, 0.1f, 0.95f);
        [SerializeField] private Color m_headerColor = new Color(0f, 0.8f, 1f);
        [SerializeField] private Color m_labelColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color m_valueColor = Color.white;
        [SerializeField] private Color m_newRecordColor = new Color(1f, 0.8f, 0f);

        // UI Components
        private Canvas m_canvas;
        private CanvasGroup m_canvasGroup;
        private RectTransform m_panel;
        private TextMeshProUGUI m_titleText;
        private List<StatRow> m_statRows = new List<StatRow>();
        private Button m_closeButton;
        private TextMeshProUGUI m_closeButtonText;

        private bool m_isVisible;
        private HashSet<HighScoreType> m_newRecords = new HashSet<HighScoreType>();

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
            if (m_createOnAwake)
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
            if (m_isVisible)
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
            m_newRecords.Add(type);
        }

        private void OnNewHighScoreFloat(HighScoreType type, float value)
        {
            m_newRecords.Add(type);
        }

        private void CreateUI()
        {
            // Create canvas
            var canvasGO = new GameObject("LeaderboardCanvas");
            canvasGO.transform.SetParent(transform);
            m_canvas = canvasGO.AddComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = 250;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            m_canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            m_canvasGroup.alpha = 0;

            // Background overlay
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = m_backgroundColor;

            // Panel
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(canvasGO.transform);
            m_panel = panelGO.AddComponent<RectTransform>();
            m_panel.anchorMin = new Vector2(0.5f, 0.5f);
            m_panel.anchorMax = new Vector2(0.5f, 0.5f);
            m_panel.pivot = new Vector2(0.5f, 0.5f);
            m_panel.sizeDelta = new Vector2(500, 550);

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(m_panel);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(400, 50);

            m_titleText = titleGO.AddComponent<TextMeshProUGUI>();
            m_titleText.text = "HIGH SCORES";
            m_titleText.fontSize = 36;
            m_titleText.fontStyle = FontStyles.Bold;
            m_titleText.color = m_headerColor;
            m_titleText.alignment = TextAlignmentOptions.Center;

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
                    m_statRows.Add(null);
                    continue;
                }

                var row = CreateStatRow(labels[i], startY - (i * rowHeight), types[i]);
                m_statRows.Add(row);
            }
        }

        private StatRow CreateStatRow(string label, float yPos, HighScoreType? type)
        {
            var row = new StatRow { type = type };

            var rowGO = new GameObject($"Row_{label}");
            rowGO.transform.SetParent(m_panel);
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
            row.label.color = m_labelColor;
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
            row.value.color = m_valueColor;
            row.value.alignment = TextAlignmentOptions.Right;

            return row;
        }

        private void CreateCloseButton()
        {
            var buttonGO = new GameObject("CloseButton");
            buttonGO.transform.SetParent(m_panel);
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0, 20);
            buttonRect.sizeDelta = new Vector2(180, 45);

            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 0.9f);

            m_closeButton = buttonGO.AddComponent<Button>();
            m_closeButton.onClick.AddListener(Hide);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            m_closeButtonText = textGO.AddComponent<TextMeshProUGUI>();
            m_closeButtonText.text = "CLOSE";
            m_closeButtonText.fontSize = 22;
            m_closeButtonText.fontStyle = FontStyles.Bold;
            m_closeButtonText.color = Color.white;
            m_closeButtonText.alignment = TextAlignmentOptions.Center;
        }

        public void Show()
        {
            if (m_canvas == null)
            {
                CreateUI();
            }

            UpdateValues();
            m_isVisible = true;
            m_canvasGroup.alpha = 1f;
            m_canvasGroup.blocksRaycasts = true;

            // Clear new records after showing
            m_newRecords.Clear();
        }

        public void Hide()
        {
            m_isVisible = false;
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 0f;
                m_canvasGroup.blocksRaycasts = false;
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
            if (index < 0 || index >= m_statRows.Count || m_statRows[index] == null) return;

            var row = m_statRows[index];
            row.value.text = value;

            // Highlight new records
            if (type.HasValue && m_newRecords.Contains(type.Value))
            {
                row.value.color = m_newRecordColor;
                row.label.text = row.label.text.Replace(" (NEW!)", "") + " (NEW!)";
            }
            else
            {
                row.value.color = m_valueColor;
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
