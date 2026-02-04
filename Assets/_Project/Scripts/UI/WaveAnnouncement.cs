using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays dramatic wave/level announcements.
    /// Shows level name, number, and objectives.
    /// </summary>
    public class WaveAnnouncement : MonoBehaviour
    {

        [Header("Timing")]
        [SerializeField] private float m_slideInDuration = 0.3f;
        [SerializeField] private float m_holdDuration = 2f;
        [SerializeField] private float m_slideOutDuration = 0.4f;

        [Header("Layout")]
        [SerializeField] private float m_slideDistance = 300f;

        [Header("Colors (Uses UITheme)")]
        [SerializeField] private bool m_useThemeColors = true;

        // Derived from UITheme
        private Color m_levelTextColor => m_useThemeColors ? UITheme.Warning : m_customLevelColor;
        private Color m_nameTextColor => m_useThemeColors ? UITheme.TextPrimary : Color.white;
        private Color m_objectiveColor => m_useThemeColors ? UITheme.TextSecondary : new Color(0.7f, 0.7f, 0.7f);
        private Color m_warningColor => m_useThemeColors ? UITheme.Danger : new Color(1f, 0.3f, 0.3f);
        private Color m_bossColor => m_useThemeColors ? UITheme.Accent : new Color(1f, 0.2f, 0.4f);

        [SerializeField] private Color m_customLevelColor = new Color(1f, 0.9f, 0.3f);

        // UI Components
        private Canvas m_canvas;
        private CanvasGroup m_canvasGroup;
        private RectTransform m_container;
        private TextMeshProUGUI m_levelText;
        private TextMeshProUGUI m_nameText;
        private TextMeshProUGUI m_objectiveText;
        private Image m_backgroundBar;
        private Image m_accentLine;

        private Coroutine m_announcementCoroutine;

        private void Awake()
        {
            CreateUI();
        }

        private void Start()
        {
            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Subscribe<BossEncounterEvent>(OnBossEncounter);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Unsubscribe<BossEncounterEvent>(OnBossEncounter);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void CreateUI()
        {
            // Create canvas
            var canvasGO = new GameObject("WaveAnnouncementCanvas");
            canvasGO.transform.SetParent(transform);
            m_canvas = canvasGO.AddComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = UITheme.SortOrder.Announcements;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            m_canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            m_canvasGroup.alpha = 0;
            m_canvasGroup.blocksRaycasts = false;
            m_canvasGroup.interactable = false;

            // Container (positioned at bottom of top third - doesn't occlude player)
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(canvasGO.transform);
            m_container = containerGO.AddComponent<RectTransform>();
            m_container.anchorMin = new Vector2(0.5f, 0.5f);
            m_container.anchorMax = new Vector2(0.5f, 0.5f);
            m_container.pivot = new Vector2(0.5f, 0.5f);
            m_container.anchoredPosition = new Vector2(0, 220); // Top third (was 0)
            m_container.sizeDelta = new Vector2(600, 150);

            // Background bar
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(m_container);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            m_backgroundBar = bgGO.AddComponent<Image>();
            m_backgroundBar.color = new Color(0, 0, 0, 0.7f);

            // Accent line (top)
            var accentGO = new GameObject("AccentLine");
            accentGO.transform.SetParent(m_container);
            var accentRect = accentGO.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 1);
            accentRect.anchorMax = new Vector2(1, 1);
            accentRect.pivot = new Vector2(0.5f, 1);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0, 4);

            m_accentLine = accentGO.AddComponent<Image>();
            m_accentLine.color = m_levelTextColor;

            // Level text (e.g., "LEVEL 5")
            var levelGO = new GameObject("LevelText");
            levelGO.transform.SetParent(m_container);
            var levelRect = levelGO.AddComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.5f, 0.5f);
            levelRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelRect.pivot = new Vector2(0.5f, 0.5f);
            levelRect.anchoredPosition = new Vector2(0, 30);
            levelRect.sizeDelta = new Vector2(500, 50);

            m_levelText = levelGO.AddComponent<TextMeshProUGUI>();
            m_levelText.text = "LEVEL 1";
            m_levelText.fontSize = 42;
            m_levelText.fontStyle = FontStyles.Bold;
            m_levelText.color = m_levelTextColor;
            m_levelText.alignment = TextAlignmentOptions.Center;

            // Name text (e.g., "DATA SWARM")
            var nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(m_container);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.pivot = new Vector2(0.5f, 0.5f);
            nameRect.anchoredPosition = new Vector2(0, -10);
            nameRect.sizeDelta = new Vector2(500, 40);

            m_nameText = nameGO.AddComponent<TextMeshProUGUI>();
            m_nameText.text = "NEURAL INITIALIZATION";
            m_nameText.fontSize = 28;
            m_nameText.fontStyle = FontStyles.Normal;
            m_nameText.color = m_nameTextColor;
            m_nameText.alignment = TextAlignmentOptions.Center;

            // Objective text
            var objGO = new GameObject("ObjectiveText");
            objGO.transform.SetParent(m_container);
            var objRect = objGO.AddComponent<RectTransform>();
            objRect.anchorMin = new Vector2(0.5f, 0.5f);
            objRect.anchorMax = new Vector2(0.5f, 0.5f);
            objRect.pivot = new Vector2(0.5f, 0.5f);
            objRect.anchoredPosition = new Vector2(0, -45);
            objRect.sizeDelta = new Vector2(500, 30);

            m_objectiveText = objGO.AddComponent<TextMeshProUGUI>();
            m_objectiveText.text = "Eliminate all enemies";
            m_objectiveText.fontSize = 18;
            m_objectiveText.fontStyle = FontStyles.Italic;
            m_objectiveText.color = m_objectiveColor;
            m_objectiveText.alignment = TextAlignmentOptions.Center;
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            string modeName = evt.mode switch
            {
                GameMode.Arcade => "ARCADE MODE",
                GameMode.Rogue => "ROGUE MODE",
                GameMode.Test => "TEST MODE",
                _ => "GAME START"
            };

            ShowAnnouncement("GET READY", modeName, "Survive and conquer!", m_levelTextColor);
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            string levelText = $"LEVEL {evt.levelNumber}";
            string nameText = evt.levelName;
            string objective = GetObjectiveText(evt.levelNumber);

            // Special color for milestone levels
            Color accentColor = m_levelTextColor;
            if (evt.levelNumber % 10 == 0)
            {
                accentColor = new Color(1f, 0.5f, 0.8f); // Pink for milestone
            }
            if (evt.levelNumber >= 99)
            {
                accentColor = m_bossColor;
                objective = "Final challenge!";
            }

            ShowAnnouncement(levelText, nameText, objective, accentColor);
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            string timeText = FormatTime(evt.completionTime);
            ShowAnnouncement("LEVEL COMPLETE!", evt.levelName, $"Time: {timeText}", new Color(0.3f, 1f, 0.4f));
        }

        private void OnBossEncounter(BossEncounterEvent evt)
        {
            if (evt.isBossActive)
            {
                ShowAnnouncement("WARNING", "BOSS APPROACHING", "Destroy the boss to proceed!", m_bossColor, true);
            }
        }

        private string GetObjectiveText(int level)
        {
            // Generate contextual objective text
            if (level <= 3)
            {
                return "Clear the area";
            }
            else if (level <= 10)
            {
                return "Eliminate all hostiles";
            }
            else if (level <= 25)
            {
                return "Survive the onslaught";
            }
            else if (level <= 50)
            {
                return "Overwhelming forces inbound";
            }
            else if (level <= 75)
            {
                return "Maximum threat level";
            }
            else
            {
                return "Final stretch - hold the line!";
            }
        }

        private string FormatTime(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{mins}:{secs:D2}";
        }

        public void ShowAnnouncement(string levelText, string nameText, string objectiveText, Color accentColor, bool isWarning = false)
        {
            if (m_announcementCoroutine != null)
            {
                StopCoroutine(m_announcementCoroutine);
            }
            m_announcementCoroutine = StartCoroutine(AnnouncementCoroutine(levelText, nameText, objectiveText, accentColor, isWarning));
        }

        private IEnumerator AnnouncementCoroutine(string levelText, string nameText, string objectiveText, Color accentColor, bool isWarning)
        {
            // Set text
            m_levelText.text = levelText;
            m_nameText.text = nameText;
            m_objectiveText.text = objectiveText;
            m_levelText.color = accentColor;
            m_accentLine.color = accentColor;

            if (isWarning)
            {
                m_backgroundBar.color = new Color(0.2f, 0f, 0f, 0.8f);
            }
            else
            {
                m_backgroundBar.color = new Color(0, 0, 0, 0.7f);
            }

            // Start off-screen (left) - Y position matches container anchor offset
            float yPos = 220f; // Match container position in top third
            Vector2 startPos = new Vector2(-m_slideDistance, yPos);
            Vector2 centerPos = new Vector2(0, yPos);
            Vector2 endPos = new Vector2(m_slideDistance, yPos);

            m_container.anchoredPosition = startPos;
            m_canvasGroup.alpha = 0;

            // Slide in from left
            float elapsed = 0f;
            while (elapsed < m_slideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / m_slideInDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                m_container.anchoredPosition = Vector2.Lerp(startPos, centerPos, easeT);
                m_canvasGroup.alpha = easeT;

                yield return null;
            }

            m_container.anchoredPosition = centerPos;
            m_canvasGroup.alpha = 1f;

            // Hold with optional pulsing for warnings
            elapsed = 0f;
            while (elapsed < m_holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                if (isWarning)
                {
                    // Pulse effect
                    float pulse = Mathf.Sin(elapsed * 6f) * 0.5f + 0.5f;
                    m_levelText.color = Color.Lerp(accentColor, Color.white, pulse * 0.3f);
                }

                yield return null;
            }

            // Slide out to right
            elapsed = 0f;
            while (elapsed < m_slideOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / m_slideOutDuration;
                float easeT = t * t * t; // Ease in cubic

                m_container.anchoredPosition = Vector2.Lerp(centerPos, endPos, easeT);
                m_canvasGroup.alpha = 1f - easeT;

                yield return null;
            }

            m_canvasGroup.alpha = 0;
            m_announcementCoroutine = null;
        }

        #region Debug

        [ContextMenu("Debug: Show Level 1")]
        private void DebugLevel1() => ShowAnnouncement("LEVEL 1", "NEURAL INITIALIZATION", "Clear the area", m_levelTextColor);

        [ContextMenu("Debug: Show Level 10")]
        private void DebugLevel10() => ShowAnnouncement("LEVEL 10", "SCANNING PERIMETER", "Milestone reached!", new Color(1f, 0.5f, 0.8f));

        [ContextMenu("Debug: Show Boss Warning")]
        private void DebugBoss() => ShowAnnouncement("WARNING", "BOSS APPROACHING", "Destroy the boss!", m_bossColor, true);

        [ContextMenu("Debug: Show Level Complete")]
        private void DebugComplete() => ShowAnnouncement("LEVEL COMPLETE!", "GOOD JOB", "Time: 1:23", new Color(0.3f, 1f, 0.4f));

        #endregion
    }
}
