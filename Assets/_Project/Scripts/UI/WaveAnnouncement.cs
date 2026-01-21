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
        public static WaveAnnouncement Instance { get; private set; }

        [Header("Timing")]
        [SerializeField] private float _slideInDuration = 0.3f;
        [SerializeField] private float _holdDuration = 2f;
        [SerializeField] private float _slideOutDuration = 0.4f;

        [Header("Layout")]
        [SerializeField] private float _slideDistance = 300f;

        [Header("Colors")]
        [SerializeField] private Color _levelTextColor = new Color(1f, 0.9f, 0.3f);
        [SerializeField] private Color _nameTextColor = Color.white;
        [SerializeField] private Color _objectiveColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color _warningColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color _bossColor = new Color(1f, 0.2f, 0.4f);

        // UI Components
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private RectTransform _container;
        private TextMeshProUGUI _levelText;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _objectiveText;
        private Image _backgroundBar;
        private Image _accentLine;

        private Coroutine _announcementCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

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

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void CreateUI()
        {
            // Create canvas
            var canvasGO = new GameObject("WaveAnnouncementCanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 180;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            // Container
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(canvasGO.transform);
            _container = containerGO.AddComponent<RectTransform>();
            _container.anchorMin = new Vector2(0.5f, 0.5f);
            _container.anchorMax = new Vector2(0.5f, 0.5f);
            _container.pivot = new Vector2(0.5f, 0.5f);
            _container.anchoredPosition = Vector2.zero;
            _container.sizeDelta = new Vector2(600, 150);

            // Background bar
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(_container);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            _backgroundBar = bgGO.AddComponent<Image>();
            _backgroundBar.color = new Color(0, 0, 0, 0.7f);

            // Accent line (top)
            var accentGO = new GameObject("AccentLine");
            accentGO.transform.SetParent(_container);
            var accentRect = accentGO.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 1);
            accentRect.anchorMax = new Vector2(1, 1);
            accentRect.pivot = new Vector2(0.5f, 1);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0, 4);

            _accentLine = accentGO.AddComponent<Image>();
            _accentLine.color = _levelTextColor;

            // Level text (e.g., "LEVEL 5")
            var levelGO = new GameObject("LevelText");
            levelGO.transform.SetParent(_container);
            var levelRect = levelGO.AddComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.5f, 0.5f);
            levelRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelRect.pivot = new Vector2(0.5f, 0.5f);
            levelRect.anchoredPosition = new Vector2(0, 30);
            levelRect.sizeDelta = new Vector2(500, 50);

            _levelText = levelGO.AddComponent<TextMeshProUGUI>();
            _levelText.text = "LEVEL 1";
            _levelText.fontSize = 42;
            _levelText.fontStyle = FontStyles.Bold;
            _levelText.color = _levelTextColor;
            _levelText.alignment = TextAlignmentOptions.Center;

            // Name text (e.g., "DATA SWARM")
            var nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(_container);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.pivot = new Vector2(0.5f, 0.5f);
            nameRect.anchoredPosition = new Vector2(0, -10);
            nameRect.sizeDelta = new Vector2(500, 40);

            _nameText = nameGO.AddComponent<TextMeshProUGUI>();
            _nameText.text = "NEURAL INITIALIZATION";
            _nameText.fontSize = 28;
            _nameText.fontStyle = FontStyles.Normal;
            _nameText.color = _nameTextColor;
            _nameText.alignment = TextAlignmentOptions.Center;

            // Objective text
            var objGO = new GameObject("ObjectiveText");
            objGO.transform.SetParent(_container);
            var objRect = objGO.AddComponent<RectTransform>();
            objRect.anchorMin = new Vector2(0.5f, 0.5f);
            objRect.anchorMax = new Vector2(0.5f, 0.5f);
            objRect.pivot = new Vector2(0.5f, 0.5f);
            objRect.anchoredPosition = new Vector2(0, -45);
            objRect.sizeDelta = new Vector2(500, 30);

            _objectiveText = objGO.AddComponent<TextMeshProUGUI>();
            _objectiveText.text = "Eliminate all enemies";
            _objectiveText.fontSize = 18;
            _objectiveText.fontStyle = FontStyles.Italic;
            _objectiveText.color = _objectiveColor;
            _objectiveText.alignment = TextAlignmentOptions.Center;
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

            ShowAnnouncement("GET READY", modeName, "Survive and conquer!", _levelTextColor);
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            string levelText = $"LEVEL {evt.levelNumber}";
            string nameText = evt.levelName;
            string objective = GetObjectiveText(evt.levelNumber);

            // Special color for milestone levels
            Color accentColor = _levelTextColor;
            if (evt.levelNumber % 10 == 0)
            {
                accentColor = new Color(1f, 0.5f, 0.8f); // Pink for milestone
            }
            if (evt.levelNumber >= 99)
            {
                accentColor = _bossColor;
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
                ShowAnnouncement("WARNING", "BOSS APPROACHING", "Destroy the boss to proceed!", _bossColor, true);
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
            if (_announcementCoroutine != null)
            {
                StopCoroutine(_announcementCoroutine);
            }
            _announcementCoroutine = StartCoroutine(AnnouncementCoroutine(levelText, nameText, objectiveText, accentColor, isWarning));
        }

        private IEnumerator AnnouncementCoroutine(string levelText, string nameText, string objectiveText, Color accentColor, bool isWarning)
        {
            // Set text
            _levelText.text = levelText;
            _nameText.text = nameText;
            _objectiveText.text = objectiveText;
            _levelText.color = accentColor;
            _accentLine.color = accentColor;

            if (isWarning)
            {
                _backgroundBar.color = new Color(0.2f, 0f, 0f, 0.8f);
            }
            else
            {
                _backgroundBar.color = new Color(0, 0, 0, 0.7f);
            }

            // Start off-screen (left)
            Vector2 startPos = new Vector2(-_slideDistance, 0);
            Vector2 centerPos = Vector2.zero;
            Vector2 endPos = new Vector2(_slideDistance, 0);

            _container.anchoredPosition = startPos;
            _canvasGroup.alpha = 0;

            // Slide in from left
            float elapsed = 0f;
            while (elapsed < _slideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _slideInDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                _container.anchoredPosition = Vector2.Lerp(startPos, centerPos, easeT);
                _canvasGroup.alpha = easeT;

                yield return null;
            }

            _container.anchoredPosition = centerPos;
            _canvasGroup.alpha = 1f;

            // Hold with optional pulsing for warnings
            elapsed = 0f;
            while (elapsed < _holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                if (isWarning)
                {
                    // Pulse effect
                    float pulse = Mathf.Sin(elapsed * 6f) * 0.5f + 0.5f;
                    _levelText.color = Color.Lerp(accentColor, Color.white, pulse * 0.3f);
                }

                yield return null;
            }

            // Slide out to right
            elapsed = 0f;
            while (elapsed < _slideOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _slideOutDuration;
                float easeT = t * t * t; // Ease in cubic

                _container.anchoredPosition = Vector2.Lerp(centerPos, endPos, easeT);
                _canvasGroup.alpha = 1f - easeT;

                yield return null;
            }

            _canvasGroup.alpha = 0;
            _announcementCoroutine = null;
        }

        #region Debug

        [ContextMenu("Debug: Show Level 1")]
        private void DebugLevel1() => ShowAnnouncement("LEVEL 1", "NEURAL INITIALIZATION", "Clear the area", _levelTextColor);

        [ContextMenu("Debug: Show Level 10")]
        private void DebugLevel10() => ShowAnnouncement("LEVEL 10", "SCANNING PERIMETER", "Milestone reached!", new Color(1f, 0.5f, 0.8f));

        [ContextMenu("Debug: Show Boss Warning")]
        private void DebugBoss() => ShowAnnouncement("WARNING", "BOSS APPROACHING", "Destroy the boss!", _bossColor, true);

        [ContextMenu("Debug: Show Level Complete")]
        private void DebugComplete() => ShowAnnouncement("LEVEL COMPLETE!", "GOOD JOB", "Time: 1:23", new Color(0.3f, 1f, 0.4f));

        #endregion
    }
}
