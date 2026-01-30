using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using NeuralBreak.Core;
using NeuralBreak.Entities;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Displays a minimap/radar showing player, enemies, and pickups.
    /// Optimized: Uses cached EnemySpawner.ActiveEnemies instead of FindObjectsByType.
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _mapRadius = 15f;
        [SerializeField] private float _displayRadius = 60f;
        [SerializeField] private float _updateInterval = 0.1f;
        [SerializeField] private bool _rotateWithPlayer = false;

        [Header("Colors (Uses UITheme)")]
        [SerializeField] private bool _useThemeColors = true;

        private Color BackgroundColor => _useThemeColors ? UITheme.MinimapBackground : _customBackgroundColor;
        private Color BorderColor => _useThemeColors ? UITheme.MinimapBorder : _customBorderColor;
        private Color PlayerColor => _useThemeColors ? UITheme.MinimapPlayer : _customPlayerColor;
        private Color EnemyColor => _useThemeColors ? UITheme.MinimapEnemy : _customEnemyColor;
        private Color EliteColor => _useThemeColors ? UITheme.MinimapElite : _customEliteColor;
        private Color BossColor => _useThemeColors ? UITheme.MinimapBoss : _customBossColor;
        private Color PickupColor => _useThemeColors ? UITheme.MinimapPickup : _customPickupColor;

        [SerializeField] private Color _customBackgroundColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color _customBorderColor = new Color(0.3f, 0.8f, 1f, 0.8f);
        [SerializeField] private Color _customPlayerColor = new Color(0.3f, 1f, 0.4f);
        [SerializeField] private Color _customEnemyColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color _customEliteColor = new Color(1f, 0.6f, 0.1f);
        [SerializeField] private Color _customBossColor = new Color(1f, 0.2f, 1f);
        [SerializeField] private Color _customPickupColor = new Color(1f, 1f, 0.3f);

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

        // Cached references (avoids FindObjectsByType!)
        private Transform _playerTransform;
        private EnemySpawner _enemySpawner;
        private PickupSpawner _pickupSpawner;
        private float _nextUpdateTime;

        // Cached sprites (generated once, reused)
        private static Sprite _circleSprite64;
        private static Sprite _circleSprite16;
        private static Sprite _ringSprite;
        private static Sprite _triangleSprite;

        private void Awake()
        {
            CacheSprites();
            CreateUI();
        }

        private void Start()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            CacheReferences();
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            _playerTransform = null;
            CacheReferences();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void CacheReferences()
        {
            // Cache spawner references once - no FindObjectsByType in Update!
            if (_enemySpawner == null)
                _enemySpawner = FindFirstObjectByType<EnemySpawner>();
            if (_pickupSpawner == null)
                _pickupSpawner = FindFirstObjectByType<PickupSpawner>();
        }

        private void CacheSprites()
        {
            // Generate sprites once, reuse everywhere
            if (_circleSprite64 == null)
                _circleSprite64 = CreateCircleSprite(64);
            if (_circleSprite16 == null)
                _circleSprite16 = CreateCircleSprite(16);
            if (_ringSprite == null)
                _ringSprite = CreateRingSprite(64, 0.9f);
            if (_triangleSprite == null)
                _triangleSprite = CreateTriangleSprite();
        }

        private void Update()
        {
            if (Time.time < _nextUpdateTime) return;
            _nextUpdateTime = Time.time + _updateInterval;
            UpdateMinimap();
        }

        private void CreateUI()
        {
            var canvasGO = new GameObject("MinimapCanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = UITheme.SortOrder.Minimap;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Map container (bottom-left corner)
            var containerGO = new GameObject("MapContainer");
            containerGO.transform.SetParent(canvasGO.transform);
            _mapContainer = containerGO.AddComponent<RectTransform>();
            _mapContainer.anchorMin = new Vector2(0, 0);
            _mapContainer.anchorMax = new Vector2(0, 0);
            _mapContainer.pivot = new Vector2(0, 0);
            _mapContainer.anchoredPosition = new Vector2(24, 24);
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
            _backgroundImage.sprite = _circleSprite64;
            _backgroundImage.color = BackgroundColor;

            // Border
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(_mapContainer);
            var borderRect = borderGO.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-2, -2);
            borderRect.offsetMax = new Vector2(2, 2);

            _borderImage = borderGO.AddComponent<Image>();
            _borderImage.sprite = _ringSprite;
            _borderImage.color = BorderColor;

            // Player blip
            var playerGO = new GameObject("PlayerBlip");
            playerGO.transform.SetParent(_mapContainer);
            _playerBlip = playerGO.AddComponent<RectTransform>();
            _playerBlip.anchorMin = new Vector2(0.5f, 0.5f);
            _playerBlip.anchorMax = new Vector2(0.5f, 0.5f);
            _playerBlip.pivot = new Vector2(0.5f, 0.5f);
            _playerBlip.anchoredPosition = Vector2.zero;
            _playerBlip.sizeDelta = new Vector2(_playerBlipSize, _playerBlipSize);

            var playerImage = playerGO.AddComponent<Image>();
            playerImage.sprite = _triangleSprite;
            playerImage.color = PlayerColor;

            // Pre-create blip pools (reuse cached sprite)
            for (int i = 0; i < 50; i++)
                _enemyBlipPool.Add(CreateBlip("EnemyBlip", _enemyBlipSize, EnemyColor));
            for (int i = 0; i < 20; i++)
                _pickupBlipPool.Add(CreateBlip("PickupBlip", _pickupBlipSize, PickupColor));
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
            image.sprite = _circleSprite16; // Reuse cached sprite
            image.color = color;

            blipGO.SetActive(false);
            return rect;
        }

        private void UpdateMinimap()
        {
            // Cache player transform
            if (_playerTransform == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                    _playerTransform = playerGO.transform;
                else
                    return;
            }

            Vector3 playerPos = _playerTransform.position;

            if (_rotateWithPlayer)
                _playerBlip.localRotation = Quaternion.Euler(0, 0, -_playerTransform.eulerAngles.z);

            // Update enemy blips - use cached EnemySpawner.ActiveEnemies (no FindObjectsByType!)
            _activeEnemyBlips = 0;
            if (_enemySpawner != null)
            {
                var enemies = _enemySpawner.ActiveEnemies;
                foreach (var enemy in enemies)
                {
                    if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                    Vector3 offset = enemy.transform.position - playerPos;
                    if (offset.sqrMagnitude > _mapRadius * _mapRadius) continue;

                    if (_activeEnemyBlips >= _enemyBlipPool.Count)
                        _enemyBlipPool.Add(CreateBlip("EnemyBlip", _enemyBlipSize, EnemyColor));

                    var blip = _enemyBlipPool[_activeEnemyBlips];
                    blip.gameObject.SetActive(true);
                    blip.anchoredPosition = new Vector2(offset.x, offset.y) / _mapRadius * _displayRadius;

                    var blipImage = blip.GetComponent<Image>();
                    var eliteMod = enemy.GetComponent<EliteModifier>();

                    if (enemy.EnemyType == EnemyType.Boss)
                    {
                        blipImage.color = BossColor;
                        blip.sizeDelta = new Vector2(_bossBlipSize, _bossBlipSize);
                    }
                    else if (eliteMod != null && eliteMod.IsElite)
                    {
                        blipImage.color = EliteColor;
                        blip.sizeDelta = new Vector2(_enemyBlipSize * 1.5f, _enemyBlipSize * 1.5f);
                    }
                    else
                    {
                        blipImage.color = EnemyColor;
                        blip.sizeDelta = new Vector2(_enemyBlipSize, _enemyBlipSize);
                    }

                    _activeEnemyBlips++;
                }
            }

            // Hide unused enemy blips
            for (int i = _activeEnemyBlips; i < _enemyBlipPool.Count; i++)
                _enemyBlipPool[i].gameObject.SetActive(false);

            // Update pickup blips - use cached PickupSpawner.ActivePickups
            _activePickupBlips = 0;
            if (_pickupSpawner != null)
            {
                var pickups = _pickupSpawner.ActivePickups;
                foreach (var pickup in pickups)
                {
                    if (pickup == null || !pickup.gameObject.activeInHierarchy) continue;

                    Vector3 offset = pickup.transform.position - playerPos;
                    if (offset.sqrMagnitude > _mapRadius * _mapRadius) continue;

                    if (_activePickupBlips >= _pickupBlipPool.Count)
                        _pickupBlipPool.Add(CreateBlip("PickupBlip", _pickupBlipSize, PickupColor));

                    var blip = _pickupBlipPool[_activePickupBlips];
                    blip.gameObject.SetActive(true);
                    blip.anchoredPosition = new Vector2(offset.x, offset.y) / _mapRadius * _displayRadius;

                    _activePickupBlips++;
                }
            }

            // Hide unused pickup blips
            for (int i = _activePickupBlips; i < _pickupBlipPool.Count; i++)
                _pickupBlipPool[i].gameObject.SetActive(false);
        }

        #region Sprite Generation (Static Cache)

        private static Sprite CreateCircleSprite(int size)
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

        private static Sprite CreateRingSprite(int size, float innerRadius)
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

        private static Sprite CreateTriangleSprite()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];

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

        #endregion

        public void SetVisible(bool visible) => _canvas.gameObject.SetActive(visible);
        public void SetRange(float radius) => _mapRadius = radius;
    }
}
