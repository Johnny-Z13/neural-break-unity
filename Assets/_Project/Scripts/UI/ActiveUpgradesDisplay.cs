using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays active weapon upgrades with countdown timers.
    /// Shows icons/text for each active upgrade.
    /// </summary>
    public class ActiveUpgradesDisplay : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _iconSize = 40f;
        [SerializeField] private float _iconSpacing = 5f;
        [SerializeField] private float _lowTimeThreshold = 3f;

        [Header("Colors")]
        [SerializeField] private Color _spreadShotColor = new Color(1f, 0.6f, 0.2f);
        [SerializeField] private Color _piercingColor = new Color(1f, 0.3f, 0.1f);
        [SerializeField] private Color _rapidFireColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color _homingColor = new Color(0.3f, 1f, 0.4f);
        [SerializeField] private Color _lowTimeColor = new Color(1f, 0.3f, 0.3f);

        private Canvas _canvas;
        private RectTransform _container;
        private Dictionary<PickupType, UpgradeIcon> _icons = new Dictionary<PickupType, UpgradeIcon>();

        private class UpgradeIcon
        {
            public GameObject gameObject;
            public Image background;
            public TextMeshProUGUI label;
            public TextMeshProUGUI timer;
            public Color baseColor;
        }

        private void Start()
        {
            CreateUI();
            EventBus.Subscribe<WeaponUpgradeActivatedEvent>(OnUpgradeActivated);
            EventBus.Subscribe<WeaponUpgradeExpiredEvent>(OnUpgradeExpired);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<WeaponUpgradeActivatedEvent>(OnUpgradeActivated);
            EventBus.Unsubscribe<WeaponUpgradeExpiredEvent>(OnUpgradeExpired);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void CreateUI()
        {
            // Create canvas
            var canvasGO = new GameObject("UpgradesCanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 85;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Create container (bottom-left positioning)
            var containerGO = new GameObject("Container");
            containerGO.transform.SetParent(canvasGO.transform);
            _container = containerGO.AddComponent<RectTransform>();
            _container.anchorMin = new Vector2(0, 0);
            _container.anchorMax = new Vector2(0, 0);
            _container.pivot = new Vector2(0, 0);
            _container.anchoredPosition = new Vector2(20, 80);
            _container.sizeDelta = new Vector2(300, 200);

            // Add horizontal layout
            var layout = containerGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = _iconSpacing;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private UpgradeIcon CreateIcon(PickupType type)
        {
            var icon = new UpgradeIcon();

            // Create icon container
            icon.gameObject = new GameObject(type.ToString());
            icon.gameObject.transform.SetParent(_container);

            var rect = icon.gameObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(_iconSize, _iconSize + 20);

            var layout = icon.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(icon.gameObject.transform);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(_iconSize, _iconSize);

            icon.background = bgGO.AddComponent<Image>();
            icon.baseColor = GetColorForType(type);
            icon.background.color = icon.baseColor;

            // Add rounded corners effect (simple)
            icon.background.sprite = CreateRoundedRect();
            icon.background.type = Image.Type.Sliced;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(bgGO.transform);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            icon.label = labelGO.AddComponent<TextMeshProUGUI>();
            icon.label.text = GetShortName(type);
            icon.label.fontSize = 14;
            icon.label.color = Color.white;
            icon.label.alignment = TextAlignmentOptions.Center;
            icon.label.fontStyle = FontStyles.Bold;

            // Timer text below
            var timerGO = new GameObject("Timer");
            timerGO.transform.SetParent(icon.gameObject.transform);
            var timerRect = timerGO.AddComponent<RectTransform>();
            timerRect.sizeDelta = new Vector2(_iconSize, 16);

            var timerLayout = timerGO.AddComponent<LayoutElement>();
            timerLayout.preferredHeight = 16;

            icon.timer = timerGO.AddComponent<TextMeshProUGUI>();
            icon.timer.fontSize = 12;
            icon.timer.color = Color.white;
            icon.timer.alignment = TextAlignmentOptions.Center;

            icon.gameObject.SetActive(false);
            return icon;
        }

        private Sprite CreateRoundedRect()
        {
            int size = 32;
            int border = 4;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Simple rounded rect
                    bool inside = true;
                    int cornerRadius = 6;

                    // Check corners
                    if (x < cornerRadius && y < cornerRadius)
                    {
                        int dx = cornerRadius - x;
                        int dy = cornerRadius - y;
                        inside = (dx * dx + dy * dy) <= cornerRadius * cornerRadius;
                    }
                    else if (x >= size - cornerRadius && y < cornerRadius)
                    {
                        int dx = x - (size - cornerRadius - 1);
                        int dy = cornerRadius - y;
                        inside = (dx * dx + dy * dy) <= cornerRadius * cornerRadius;
                    }
                    else if (x < cornerRadius && y >= size - cornerRadius)
                    {
                        int dx = cornerRadius - x;
                        int dy = y - (size - cornerRadius - 1);
                        inside = (dx * dx + dy * dy) <= cornerRadius * cornerRadius;
                    }
                    else if (x >= size - cornerRadius && y >= size - cornerRadius)
                    {
                        int dx = x - (size - cornerRadius - 1);
                        int dy = y - (size - cornerRadius - 1);
                        inside = (dx * dx + dy * dy) <= cornerRadius * cornerRadius;
                    }

                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        private Color GetColorForType(PickupType type)
        {
            switch (type)
            {
                case PickupType.SpreadShot: return _spreadShotColor;
                case PickupType.Piercing: return _piercingColor;
                case PickupType.RapidFire: return _rapidFireColor;
                case PickupType.Homing: return _homingColor;
                default: return Color.white;
            }
        }

        private string GetShortName(PickupType type)
        {
            switch (type)
            {
                case PickupType.SpreadShot: return "SPR";
                case PickupType.Piercing: return "PRC";
                case PickupType.RapidFire: return "RPD";
                case PickupType.Homing: return "HOM";
                default: return "???";
            }
        }

        private void Update()
        {
            if (FindObjectOfType<WeaponUpgradeManager>() == null) return;

            foreach (var kvp in _icons)
            {
                PickupType type = kvp.Key;
                UpgradeIcon icon = kvp.Value;

                float remaining = FindObjectOfType<WeaponUpgradeManager>().GetRemainingTime(type);

                if (remaining > 0)
                {
                    if (!icon.gameObject.activeSelf)
                    {
                        icon.gameObject.SetActive(true);
                    }

                    icon.timer.text = remaining.ToString("F1") + "s";

                    // Pulse when low time
                    if (remaining <= _lowTimeThreshold)
                    {
                        float pulse = Mathf.Sin(Time.time * 10f) * 0.5f + 0.5f;
                        icon.background.color = Color.Lerp(_lowTimeColor, icon.baseColor, pulse);
                        icon.timer.color = Color.Lerp(Color.red, Color.white, pulse);
                    }
                    else
                    {
                        icon.background.color = icon.baseColor;
                        icon.timer.color = Color.white;
                    }
                }
                else
                {
                    if (icon.gameObject.activeSelf)
                    {
                        icon.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnUpgradeActivated(WeaponUpgradeActivatedEvent evt)
        {
            if (!_icons.ContainsKey(evt.upgradeType))
            {
                _icons[evt.upgradeType] = CreateIcon(evt.upgradeType);
            }

            _icons[evt.upgradeType].gameObject.SetActive(true);
        }

        private void OnUpgradeExpired(WeaponUpgradeExpiredEvent evt)
        {
            if (_icons.TryGetValue(evt.upgradeType, out var icon))
            {
                icon.gameObject.SetActive(false);
            }
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            foreach (var icon in _icons.Values)
            {
                icon.gameObject.SetActive(false);
            }
        }
    }
}
