using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays achievement unlock popups.
    /// Queues multiple achievements and shows them sequentially.
    /// </summary>
    public class AchievementPopup : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float m_slideInDuration = 0.3f;
        [SerializeField] private float m_displayDuration = 3f;
        [SerializeField] private float m_slideOutDuration = 0.3f;
        [SerializeField] private float m_slideDistance = 200f;

        [Header("Colors (Uses UITheme)")]
        [SerializeField] private bool m_useThemeColors = true;

        private Color BackgroundColor => m_useThemeColors ? UITheme.BackgroundDark : m_customBackgroundColor;
        private Color BorderColor => m_useThemeColors ? UITheme.Warning : m_customBorderColor;
        private Color TitleColor => m_useThemeColors ? UITheme.Warning : m_customTitleColor;
        private Color TextColor => m_useThemeColors ? UITheme.TextPrimary : Color.white;

        [SerializeField] private Color m_customBackgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        [SerializeField] private Color m_customBorderColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color m_customTitleColor = new Color(1f, 0.8f, 0.2f);

        // UI Components
        private Canvas m_canvas;
        private CanvasGroup m_canvasGroup;
        private RectTransform m_container;
        private TextMeshProUGUI m_titleText;
        private TextMeshProUGUI m_nameText;
        private TextMeshProUGUI m_descText;
        private Image m_iconImage;

        // Queue
        private Queue<AchievementUnlockedEvent> m_queue = new Queue<AchievementUnlockedEvent>();
        private bool m_isShowingPopup;

        private void Awake()
        {
            CreateUI();
        }

        private void Start()
        {
            EventBus.Subscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);
        }

        private void CreateUI()
        {
            // Create canvas
            var canvasGO = new GameObject("AchievementPopupCanvas");
            canvasGO.transform.SetParent(transform);
            m_canvas = canvasGO.AddComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = UITheme.SortOrder.Achievements;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            m_canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            m_canvasGroup.alpha = 0;
            m_canvasGroup.blocksRaycasts = false;

            // Container (top-center)
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(canvasGO.transform);
            m_container = containerGO.AddComponent<RectTransform>();
            m_container.anchorMin = new Vector2(0.5f, 1f);
            m_container.anchorMax = new Vector2(0.5f, 1f);
            m_container.pivot = new Vector2(0.5f, 1f);
            m_container.anchoredPosition = new Vector2(0, -20);
            m_container.sizeDelta = new Vector2(350, 90);

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(m_container);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = BackgroundColor;

            // Border
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(m_container);
            var borderRect = borderGO.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0, 0);
            borderRect.anchorMax = new Vector2(0, 1);
            borderRect.pivot = new Vector2(0, 0.5f);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(4, 0);

            var borderImage = borderGO.AddComponent<Image>();
            borderImage.color = BorderColor;

            // Icon
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(m_container);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(15, 0);
            iconRect.sizeDelta = new Vector2(50, 50);

            m_iconImage = iconGO.AddComponent<Image>();
            m_iconImage.sprite = CreateTrophySprite();
            m_iconImage.color = BorderColor;

            // Title "ACHIEVEMENT UNLOCKED"
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(m_container);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0, 1);
            titleRect.anchoredPosition = new Vector2(75, -10);
            titleRect.sizeDelta = new Vector2(260, 20);

            m_titleText = titleGO.AddComponent<TextMeshProUGUI>();
            m_titleText.text = "ACHIEVEMENT UNLOCKED";
            m_titleText.fontSize = 14;
            m_titleText.fontStyle = FontStyles.Bold;
            m_titleText.color = TitleColor;
            m_titleText.alignment = TextAlignmentOptions.Left;

            // Achievement name
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(m_container);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0, 1);
            nameRect.anchoredPosition = new Vector2(75, -35);
            nameRect.sizeDelta = new Vector2(260, 25);

            m_nameText = nameGO.AddComponent<TextMeshProUGUI>();
            m_nameText.text = "First Blood";
            m_nameText.fontSize = 18;
            m_nameText.fontStyle = FontStyles.Bold;
            m_nameText.color = TextColor;
            m_nameText.alignment = TextAlignmentOptions.Left;

            // Description
            var descGO = new GameObject("Description");
            descGO.transform.SetParent(m_container);
            var descRect = descGO.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 1);
            descRect.anchorMax = new Vector2(1, 1);
            descRect.pivot = new Vector2(0, 1);
            descRect.anchoredPosition = new Vector2(75, -60);
            descRect.sizeDelta = new Vector2(260, 20);

            m_descText = descGO.AddComponent<TextMeshProUGUI>();
            m_descText.text = "Kill your first enemy";
            m_descText.fontSize = 13;
            m_descText.color = new Color(0.7f, 0.7f, 0.7f);
            m_descText.alignment = TextAlignmentOptions.Left;
        }

        private Sprite CreateTrophySprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];

            // Simple trophy shape
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = false;
                    float cx = x - size / 2f;
                    float cy = y - size / 2f;

                    // Cup top (ellipse)
                    if (y > size * 0.4f && y < size * 0.85f)
                    {
                        float relY = (y - size * 0.4f) / (size * 0.45f);
                        float width = 0.4f - relY * 0.15f;
                        if (Mathf.Abs(cx) < size * width)
                        {
                            inside = true;
                        }
                    }

                    // Stem
                    if (y > size * 0.2f && y < size * 0.45f)
                    {
                        if (Mathf.Abs(cx) < size * 0.08f)
                        {
                            inside = true;
                        }
                    }

                    // Base
                    if (y > size * 0.1f && y < size * 0.25f)
                    {
                        if (Mathf.Abs(cx) < size * 0.2f)
                        {
                            inside = true;
                        }
                    }

                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void OnAchievementUnlocked(AchievementUnlockedEvent evt)
        {
            m_queue.Enqueue(evt);

            if (!m_isShowingPopup)
            {
                StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            m_isShowingPopup = true;

            while (m_queue.Count > 0)
            {
                var evt = m_queue.Dequeue();
                yield return ShowPopup(evt);

                // Small delay between popups
                yield return new WaitForSecondsRealtime(0.3f);
            }

            m_isShowingPopup = false;
        }

        private IEnumerator ShowPopup(AchievementUnlockedEvent evt)
        {
            // Set text
            m_nameText.text = evt.name;
            m_descText.text = evt.description;

            // Start position (above screen)
            Vector2 hiddenPos = new Vector2(0, m_slideDistance);
            Vector2 visiblePos = new Vector2(0, -20);

            m_container.anchoredPosition = hiddenPos;
            m_canvasGroup.alpha = 0;

            // Slide down
            float elapsed = 0f;
            while (elapsed < m_slideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / m_slideInDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f);

                m_container.anchoredPosition = Vector2.Lerp(hiddenPos, visiblePos, easeT);
                m_canvasGroup.alpha = easeT;

                yield return null;
            }

            m_container.anchoredPosition = visiblePos;
            m_canvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSecondsRealtime(m_displayDuration);

            // Slide up
            elapsed = 0f;
            while (elapsed < m_slideOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / m_slideOutDuration;

                m_container.anchoredPosition = Vector2.Lerp(visiblePos, hiddenPos, t);
                m_canvasGroup.alpha = 1f - t;

                yield return null;
            }

            m_canvasGroup.alpha = 0;
        }

        #region Debug

        [ContextMenu("Debug: Show Test Achievement")]
        private void DebugShowTest()
        {
            OnAchievementUnlocked(new AchievementUnlockedEvent
            {
                type = AchievementType.FirstBlood,
                name = "First Blood",
                description = "Kill your first enemy"
            });
        }

        #endregion
    }
}
