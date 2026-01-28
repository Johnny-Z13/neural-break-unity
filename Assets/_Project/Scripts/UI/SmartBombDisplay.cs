using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays smart bomb count with icons near the weapon UI.
    /// Shows current/max bombs with visual feedback.
    /// </summary>
    public class SmartBombDisplay : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _iconSize = 45f;
        [SerializeField] private float _iconSpacing = 8f;

        [Header("Colors")]
        [SerializeField] private Color _activeBombColor = new Color(1f, 0.8f, 0.2f); // Gold/yellow
        [SerializeField] private Color _inactiveBombColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Gray
        [SerializeField] private Color _pulseColor = new Color(1f, 0.4f, 0.1f); // Orange pulse

        [Header("Animation")]
        [SerializeField] private float _pulseSpeed = 3f;
        [SerializeField] private float _pulseAmount = 0.2f;

        private Canvas _canvas;
        private RectTransform _container;
        private List<BombIcon> _icons = new List<BombIcon>();

        private int _currentBombs;
        private int _maxBombs;

        private class BombIcon
        {
            public GameObject gameObject;
            public Image background;
            public TextMeshProUGUI label;
            public bool isActive;
        }

        private void Start()
        {
            CreateUI();
            EventBus.Subscribe<SmartBombCountChangedEvent>(OnBombCountChanged);
            EventBus.Subscribe<SmartBombActivatedEvent>(OnBombActivated);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);

            // Request initial state from SmartBombSystem (it may have already published)
            StartCoroutine(RequestInitialState());
        }

        private System.Collections.IEnumerator RequestInitialState()
        {
            // Wait one frame for all systems to initialize
            yield return null;

            // Find SmartBombSystem and get current state
            var bombSystem = FindFirstObjectByType<SmartBombSystem>();
            if (bombSystem != null)
            {
                OnBombCountChanged(new SmartBombCountChangedEvent
                {
                    count = bombSystem.CurrentBombs,
                    maxCount = bombSystem.MaxBombs
                });
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SmartBombCountChangedEvent>(OnBombCountChanged);
            EventBus.Unsubscribe<SmartBombActivatedEvent>(OnBombActivated);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void CreateUI()
        {
            // Create canvas
            var canvasGO = new GameObject("SmartBombCanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 85;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Create container (bottom-left, above weapon upgrades)
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(canvasGO.transform);
            _container = containerGO.AddComponent<RectTransform>();
            _container.anchorMin = new Vector2(0, 0);
            _container.anchorMax = new Vector2(0, 0);
            _container.pivot = new Vector2(0, 0);
            _container.anchoredPosition = new Vector2(20, 150); // Above ActiveUpgradesDisplay
            _container.sizeDelta = new Vector2(300, 100);

            // Add horizontal layout
            var layout = containerGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = _iconSpacing;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Add label
            CreateLabel();
        }

        private void CreateLabel()
        {
            var labelGO = new GameObject("BombsLabel");
            labelGO.transform.SetParent(_container);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(120, _iconSize);

            var layoutElement = labelGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 120;
            layoutElement.preferredHeight = _iconSize;

            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "BOMBS:";
            label.fontSize = 18;
            label.color = new Color(1f, 0.8f, 0.2f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontStyle = FontStyles.Bold;
        }

        private BombIcon CreateIcon(int index)
        {
            var icon = new BombIcon();

            // Create icon container
            icon.gameObject = new GameObject($"Bomb_{index}");
            icon.gameObject.transform.SetParent(_container);

            var rect = icon.gameObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(_iconSize, _iconSize);

            var layoutElement = icon.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = _iconSize;
            layoutElement.preferredHeight = _iconSize;

            // Background
            icon.background = icon.gameObject.AddComponent<Image>();
            icon.background.sprite = CreateBombSprite();
            icon.background.color = _inactiveBombColor;
            icon.isActive = false;

            // "B" label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(icon.gameObject.transform);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            icon.label = labelGO.AddComponent<TextMeshProUGUI>();
            icon.label.text = "B";
            icon.label.fontSize = 24;
            icon.label.color = Color.white;
            icon.label.alignment = TextAlignmentOptions.Center;
            icon.label.fontStyle = FontStyles.Bold;

            return icon;
        }

        private Sprite CreateBombSprite()
        {
            // Create a simple circle sprite for bomb icon
            int size = 64;
            int radius = 28;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float dist = Vector2.Distance(pos, center);

                    if (dist <= radius)
                    {
                        // Solid circle
                        pixels[y * size + x] = Color.white;
                    }
                    else if (dist <= radius + 2)
                    {
                        // Anti-aliased edge
                        float alpha = 1f - (dist - radius) / 2f;
                        pixels[y * size + x] = new Color(1, 1, 1, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void Update()
        {
            // Pulse active bomb icons
            for (int i = 0; i < _icons.Count; i++)
            {
                if (_icons[i].isActive)
                {
                    float pulse = Mathf.Sin(Time.time * _pulseSpeed + i * 0.3f) * _pulseAmount + 1f;
                    _icons[i].gameObject.transform.localScale = Vector3.one * pulse;

                    // Color pulse
                    float colorPulse = Mathf.Sin(Time.time * _pulseSpeed * 0.5f + i * 0.3f) * 0.5f + 0.5f;
                    _icons[i].background.color = Color.Lerp(_activeBombColor, _pulseColor, colorPulse * 0.3f);
                }
            }
        }

        private void OnBombCountChanged(SmartBombCountChangedEvent evt)
        {
            _currentBombs = evt.count;
            _maxBombs = evt.maxCount;

            // Create icons if needed
            while (_icons.Count < _maxBombs)
            {
                _icons.Add(CreateIcon(_icons.Count));
            }

            // Update icon states
            for (int i = 0; i < _icons.Count; i++)
            {
                if (i < _maxBombs)
                {
                    _icons[i].gameObject.SetActive(true);
                    _icons[i].isActive = i < _currentBombs;
                    _icons[i].background.color = _icons[i].isActive ? _activeBombColor : _inactiveBombColor;
                    _icons[i].label.color = _icons[i].isActive ? Color.white : new Color(0.5f, 0.5f, 0.5f);

                    if (!_icons[i].isActive)
                    {
                        _icons[i].gameObject.transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    _icons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnBombActivated(SmartBombActivatedEvent evt)
        {
            // Flash effect on activation
            StartCoroutine(FlashEffect());
        }

        private System.Collections.IEnumerator FlashEffect()
        {
            // Flash all icons white
            foreach (var icon in _icons)
            {
                if (icon.isActive)
                {
                    icon.background.color = Color.white;
                }
            }

            yield return new WaitForSeconds(0.1f);

            // Return to normal
            for (int i = 0; i < _icons.Count; i++)
            {
                if (_icons[i].isActive)
                {
                    _icons[i].background.color = _activeBombColor;
                }
            }
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            // Reset will be handled by SmartBombCountChangedEvent
        }
    }
}
