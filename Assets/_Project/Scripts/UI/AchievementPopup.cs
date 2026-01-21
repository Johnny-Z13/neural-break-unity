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
        [SerializeField] private float _slideInDuration = 0.3f;
        [SerializeField] private float _displayDuration = 3f;
        [SerializeField] private float _slideOutDuration = 0.3f;
        [SerializeField] private float _slideDistance = 200f;

        [Header("Colors")]
        [SerializeField] private Color _backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        [SerializeField] private Color _borderColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color _titleColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color _textColor = Color.white;

        // UI Components
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private RectTransform _container;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _descText;
        private Image _iconImage;

        // Queue
        private Queue<AchievementUnlockedEvent> _queue = new Queue<AchievementUnlockedEvent>();
        private bool _isShowingPopup;

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
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 250;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;

            // Container (top-center)
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(canvasGO.transform);
            _container = containerGO.AddComponent<RectTransform>();
            _container.anchorMin = new Vector2(0.5f, 1f);
            _container.anchorMax = new Vector2(0.5f, 1f);
            _container.pivot = new Vector2(0.5f, 1f);
            _container.anchoredPosition = new Vector2(0, -20);
            _container.sizeDelta = new Vector2(350, 90);

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(_container);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = _backgroundColor;

            // Border
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(_container);
            var borderRect = borderGO.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0, 0);
            borderRect.anchorMax = new Vector2(0, 1);
            borderRect.pivot = new Vector2(0, 0.5f);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(4, 0);

            var borderImage = borderGO.AddComponent<Image>();
            borderImage.color = _borderColor;

            // Icon
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(_container);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(15, 0);
            iconRect.sizeDelta = new Vector2(50, 50);

            _iconImage = iconGO.AddComponent<Image>();
            _iconImage.sprite = CreateTrophySprite();
            _iconImage.color = _borderColor;

            // Title "ACHIEVEMENT UNLOCKED"
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(_container);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0, 1);
            titleRect.anchoredPosition = new Vector2(75, -10);
            titleRect.sizeDelta = new Vector2(260, 20);

            _titleText = titleGO.AddComponent<TextMeshProUGUI>();
            _titleText.text = "ACHIEVEMENT UNLOCKED";
            _titleText.fontSize = 14;
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = _titleColor;
            _titleText.alignment = TextAlignmentOptions.Left;

            // Achievement name
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(_container);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0, 1);
            nameRect.anchoredPosition = new Vector2(75, -35);
            nameRect.sizeDelta = new Vector2(260, 25);

            _nameText = nameGO.AddComponent<TextMeshProUGUI>();
            _nameText.text = "First Blood";
            _nameText.fontSize = 18;
            _nameText.fontStyle = FontStyles.Bold;
            _nameText.color = _textColor;
            _nameText.alignment = TextAlignmentOptions.Left;

            // Description
            var descGO = new GameObject("Description");
            descGO.transform.SetParent(_container);
            var descRect = descGO.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 1);
            descRect.anchorMax = new Vector2(1, 1);
            descRect.pivot = new Vector2(0, 1);
            descRect.anchoredPosition = new Vector2(75, -60);
            descRect.sizeDelta = new Vector2(260, 20);

            _descText = descGO.AddComponent<TextMeshProUGUI>();
            _descText.text = "Kill your first enemy";
            _descText.fontSize = 13;
            _descText.color = new Color(0.7f, 0.7f, 0.7f);
            _descText.alignment = TextAlignmentOptions.Left;
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
            _queue.Enqueue(evt);

            if (!_isShowingPopup)
            {
                StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            _isShowingPopup = true;

            while (_queue.Count > 0)
            {
                var evt = _queue.Dequeue();
                yield return ShowPopup(evt);

                // Small delay between popups
                yield return new WaitForSecondsRealtime(0.3f);
            }

            _isShowingPopup = false;
        }

        private IEnumerator ShowPopup(AchievementUnlockedEvent evt)
        {
            // Set text
            _nameText.text = evt.name;
            _descText.text = evt.description;

            // Start position (above screen)
            Vector2 hiddenPos = new Vector2(0, _slideDistance);
            Vector2 visiblePos = new Vector2(0, -20);

            _container.anchoredPosition = hiddenPos;
            _canvasGroup.alpha = 0;

            // Slide down
            float elapsed = 0f;
            while (elapsed < _slideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _slideInDuration;
                float easeT = 1f - Mathf.Pow(1f - t, 3f);

                _container.anchoredPosition = Vector2.Lerp(hiddenPos, visiblePos, easeT);
                _canvasGroup.alpha = easeT;

                yield return null;
            }

            _container.anchoredPosition = visiblePos;
            _canvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSecondsRealtime(_displayDuration);

            // Slide up
            elapsed = 0f;
            while (elapsed < _slideOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _slideOutDuration;

                _container.anchoredPosition = Vector2.Lerp(visiblePos, hiddenPos, t);
                _canvasGroup.alpha = 1f - t;

                yield return null;
            }

            _canvasGroup.alpha = 0;
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
