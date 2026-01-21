using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using NeuralBreak.Core;
using NeuralBreak.Entities;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays a minimap/radar showing player, enemies, and pickups.
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        public static Minimap Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float _mapRadius = 15f;
        [SerializeField] private float _displayRadius = 60f;
        [SerializeField] private float _updateInterval = 0.1f;
        [SerializeField] private bool _rotateWithPlayer = false;

        [Header("Colors")]
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color _borderColor = new Color(0.3f, 0.8f, 1f, 0.8f);
        [SerializeField] private Color _playerColor = new Color(0.3f, 1f, 0.4f);
        [SerializeField] private Color _enemyColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color _eliteColor = new Color(1f, 0.6f, 0.1f);
        [SerializeField] private Color _bossColor = new Color(1f, 0.2f, 1f);
        [SerializeField] private Color _pickupColor = new Color(1f, 1f, 0.3f);

        [Header("Blip Sizes")]
        [SerializeField] private float _playerBlipSize = 8f;
        [SerializeField] private float _enemyBlipSize = 4f;
        [SerializeField] private float _bossBlipSize = 10f;
        [SerializeField] private float _pickupBlipSize = 3f;

        // UI
        private Canvas _canvas;
        private RectTransform _mapContainer;
        private Image _backgroundImage;
        private Image _borderImage;
        private RectTransform _playerBlip;
        private List<RectTransform> _enemyBlipPool = new List<RectTransform>();
        private List<RectTransform> _pickupBlipPool = new List<RectTransform>();
        private int _activeEnemyBlips;
        private int _activePickupBlips;

        // References
        private Transform _playerTransform;
        private float _nextUpdateTime;

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
            // Find player
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (Time.time < _nextUpdateTime) return;
            _nextUpdateTime = Time.time + _updateInterval;

            UpdateMinimap();
        }

        private void CreateUI()
        {
            // Create canvas
            var canvasGO = new GameObject("MinimapCanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 80;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Map container (bottom-right corner)
            var containerGO = new GameObject("MapContainer");
            containerGO.transform.SetParent(canvasGO.transform);
            _mapContainer = containerGO.AddComponent<RectTransform>();
            _mapContainer.anchorMin = new Vector2(1, 0);
            _mapContainer.anchorMax = new Vector2(1, 0);
            _mapContainer.pivot = new Vector2(1, 0);
            _mapContainer.anchoredPosition = new Vector2(-20, 20);
            _mapContainer.sizeDelta = new Vector2(_displayRadius * 2, _displayRadius * 2);

            // Background (circle)
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(_mapContainer);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            _backgroundImage = bgGO.AddComponent<Image>();
            _backgroundImage.sprite = CreateCircleSprite(64);
            _backgroundImage.color = _backgroundColor;

            // Border
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(_mapContainer);
            var borderRect = borderGO.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-2, -2);
            borderRect.offsetMax = new Vector2(2, 2);

            _borderImage = borderGO.AddComponent<Image>();
            _borderImage.sprite = CreateRingSprite(64, 0.9f);
            _borderImage.color = _borderColor;

            // Player blip (center, always visible)
            var playerGO = new GameObject("PlayerBlip");
            playerGO.transform.SetParent(_mapContainer);
            _playerBlip = playerGO.AddComponent<RectTransform>();
            _playerBlip.anchorMin = new Vector2(0.5f, 0.5f);
            _playerBlip.anchorMax = new Vector2(0.5f, 0.5f);
            _playerBlip.pivot = new Vector2(0.5f, 0.5f);
            _playerBlip.anchoredPosition = Vector2.zero;
            _playerBlip.sizeDelta = new Vector2(_playerBlipSize, _playerBlipSize);

            var playerImage = playerGO.AddComponent<Image>();
            playerImage.sprite = CreateTriangleSprite();
            playerImage.color = _playerColor;

            // Pre-create blip pools
            for (int i = 0; i < 50; i++)
            {
                _enemyBlipPool.Add(CreateBlip("EnemyBlip", _enemyBlipSize, _enemyColor));
            }
            for (int i = 0; i < 20; i++)
            {
                _pickupBlipPool.Add(CreateBlip("PickupBlip", _pickupBlipSize, _pickupColor));
            }
        }

        private RectTransform CreateBlip(string name, float size, Color color)
        {
            var blipGO = new GameObject(name);
            blipGO.transform.SetParent(_mapContainer);
            var rect = blipGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);

            var image = blipGO.AddComponent<Image>();
            image.sprite = CreateCircleSprite(16);
            image.color = color;

            blipGO.SetActive(false);
            return rect;
        }

        private void UpdateMinimap()
        {
            if (_playerTransform == null)
            {
                var player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    _playerTransform = player.transform;
                }
                else
                {
                    return;
                }
            }

            Vector3 playerPos = _playerTransform.position;

            // Rotate player blip if needed
            if (_rotateWithPlayer)
            {
                _playerBlip.localRotation = Quaternion.Euler(0, 0, -_playerTransform.eulerAngles.z);
            }

            // Update enemy blips
            var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            _activeEnemyBlips = 0;

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                Vector3 offset = enemy.transform.position - playerPos;
                float distance = offset.magnitude;

                if (distance > _mapRadius) continue;

                // Get or create blip
                if (_activeEnemyBlips >= _enemyBlipPool.Count)
                {
                    _enemyBlipPool.Add(CreateBlip("EnemyBlip", _enemyBlipSize, _enemyColor));
                }

                var blip = _enemyBlipPool[_activeEnemyBlips];
                blip.gameObject.SetActive(true);

                // Position on map
                Vector2 mapPos = new Vector2(offset.x, offset.y) / _mapRadius * _displayRadius;
                blip.anchoredPosition = mapPos;

                // Color based on enemy type
                var blipImage = blip.GetComponent<Image>();
                var eliteMod = enemy.GetComponent<EliteModifier>();

                if (enemy.EnemyType == EnemyType.Boss)
                {
                    blipImage.color = _bossColor;
                    blip.sizeDelta = new Vector2(_bossBlipSize, _bossBlipSize);
                }
                else if (eliteMod != null && eliteMod.IsElite)
                {
                    blipImage.color = _eliteColor;
                    blip.sizeDelta = new Vector2(_enemyBlipSize * 1.5f, _enemyBlipSize * 1.5f);
                }
                else
                {
                    blipImage.color = _enemyColor;
                    blip.sizeDelta = new Vector2(_enemyBlipSize, _enemyBlipSize);
                }

                _activeEnemyBlips++;
            }

            // Hide unused enemy blips
            for (int i = _activeEnemyBlips; i < _enemyBlipPool.Count; i++)
            {
                _enemyBlipPool[i].gameObject.SetActive(false);
            }

            // Update pickup blips
            var pickups = FindObjectsByType<PickupBase>(FindObjectsSortMode.None);
            _activePickupBlips = 0;

            foreach (var pickup in pickups)
            {
                if (pickup == null || !pickup.gameObject.activeInHierarchy) continue;

                Vector3 offset = pickup.transform.position - playerPos;
                float distance = offset.magnitude;

                if (distance > _mapRadius) continue;

                if (_activePickupBlips >= _pickupBlipPool.Count)
                {
                    _pickupBlipPool.Add(CreateBlip("PickupBlip", _pickupBlipSize, _pickupColor));
                }

                var blip = _pickupBlipPool[_activePickupBlips];
                blip.gameObject.SetActive(true);

                Vector2 mapPos = new Vector2(offset.x, offset.y) / _mapRadius * _displayRadius;
                blip.anchoredPosition = mapPos;

                _activePickupBlips++;
            }

            // Hide unused pickup blips
            for (int i = _activePickupBlips; i < _pickupBlipPool.Count; i++)
            {
                _pickupBlipPool[i].gameObject.SetActive(false);
            }
        }

        private Sprite CreateCircleSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateRingSprite(int size, float innerRadius)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;
            float outerR = size / 2f - 1f;
            float innerR = outerR * innerRadius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = 0f;

                    if (dist <= outerR && dist >= innerR)
                    {
                        alpha = 1f;
                        // Smooth edges
                        if (dist > outerR - 1f)
                            alpha = Mathf.Clamp01(outerR - dist + 1f);
                        else if (dist < innerR + 1f)
                            alpha = Mathf.Clamp01(dist - innerR + 1f);
                    }

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateTriangleSprite()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];

            // Simple triangle pointing up
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float relY = (float)y / size;
                    float halfWidth = (1f - relY) * 0.5f;
                    float relX = (float)x / size - 0.5f;

                    bool inside = y > size * 0.1f && Mathf.Abs(relX) < halfWidth;
                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Set map visibility
        /// </summary>
        public void SetVisible(bool visible)
        {
            _canvas.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Set map range
        /// </summary>
        public void SetRange(float radius)
        {
            _mapRadius = radius;
        }

        #region Debug

        [ContextMenu("Debug: Toggle Visibility")]
        private void DebugToggle()
        {
            SetVisible(!_canvas.gameObject.activeSelf);
        }

        #endregion
    }
}
