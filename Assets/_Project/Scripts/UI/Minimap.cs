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
        [SerializeField] private float m_mapRadius = 15f;
        [SerializeField] private float m_displayRadius = 60f;
        [SerializeField] private float m_updateInterval = 0.1f;
        [SerializeField] private bool m_rotateWithPlayer = false;

        [Header("Colors (Uses UITheme)")]
        [SerializeField] private bool m_useThemeColors = true;

        private Color BackgroundColor => m_useThemeColors ? UITheme.MinimapBackground : m_customBackgroundColor;
        private Color BorderColor => m_useThemeColors ? UITheme.MinimapBorder : m_customBorderColor;
        private Color PlayerColor => m_useThemeColors ? UITheme.MinimapPlayer : m_customPlayerColor;
        private Color EnemyColor => m_useThemeColors ? UITheme.MinimapEnemy : m_customEnemyColor;
        private Color EliteColor => m_useThemeColors ? UITheme.MinimapElite : m_customEliteColor;
        private Color BossColor => m_useThemeColors ? UITheme.MinimapBoss : m_customBossColor;
        private Color PickupColor => m_useThemeColors ? UITheme.MinimapPickup : m_customPickupColor;

        [SerializeField] private Color m_customBackgroundColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color m_customBorderColor = new Color(0.3f, 0.8f, 1f, 0.8f);
        [SerializeField] private Color m_customPlayerColor = new Color(0.3f, 1f, 0.4f);
        [SerializeField] private Color m_customEnemyColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color m_customEliteColor = new Color(1f, 0.6f, 0.1f);
        [SerializeField] private Color m_customBossColor = new Color(1f, 0.2f, 1f);
        [SerializeField] private Color m_customPickupColor = new Color(1f, 1f, 0.3f);

        [Header("Blip Sizes")]
        [SerializeField] private float m_playerBlipSize = 8f;
        [SerializeField] private float m_enemyBlipSize = 4f;
        [SerializeField] private float m_bossBlipSize = 10f;
        [SerializeField] private float m_pickupBlipSize = 3f;

        // UI
        private Canvas m_canvas;
        private RectTransform m_mapContainer;
        private Image m_backgroundImage;
        private Image m_borderImage;
        private RectTransform m_playerBlip;
        private List<RectTransform> m_enemyBlipPool = new List<RectTransform>();
        private List<RectTransform> m_pickupBlipPool = new List<RectTransform>();
        private int m_activeEnemyBlips;
        private int m_activePickupBlips;

        // Cached references (avoids FindObjectsByType!)
        private Transform m_playerTransform;
        private EnemySpawner m_enemySpawner;
        private PickupSpawner m_pickupSpawner;
        private float m_nextUpdateTime;

        // Cached sprites (generated once, reused)
        private static Sprite s_circleSprite64;
        private static Sprite s_circleSprite16;
        private static Sprite s_ringSprite;
        private static Sprite s_triangleSprite;

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
            m_playerTransform = null;
            CacheReferences();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void CacheReferences()
        {
            // Cache spawner references once - no FindObjectsByType in Update!
            if (m_enemySpawner == null)
                m_enemySpawner = FindFirstObjectByType<EnemySpawner>();
            if (m_pickupSpawner == null)
                m_pickupSpawner = FindFirstObjectByType<PickupSpawner>();
        }

        private void CacheSprites()
        {
            // Generate sprites once, reuse everywhere
            if (s_circleSprite64 == null)
                s_circleSprite64 = CreateCircleSprite(64);
            if (s_circleSprite16 == null)
                s_circleSprite16 = CreateCircleSprite(16);
            if (s_ringSprite == null)
                s_ringSprite = CreateRingSprite(64, 0.9f);
            if (s_triangleSprite == null)
                s_triangleSprite = CreateTriangleSprite();
        }

        private void Update()
        {
            if (Time.time < m_nextUpdateTime) return;
            m_nextUpdateTime = Time.time + m_updateInterval;
            UpdateMinimap();
        }

        private void CreateUI()
        {
            var canvasGO = new GameObject("MinimapCanvas");
            canvasGO.transform.SetParent(transform);
            m_canvas = canvasGO.AddComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = UITheme.SortOrder.Minimap;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Map container (bottom-left corner)
            var containerGO = new GameObject("MapContainer");
            containerGO.transform.SetParent(canvasGO.transform);
            m_mapContainer = containerGO.AddComponent<RectTransform>();
            m_mapContainer.anchorMin = new Vector2(0, 0);
            m_mapContainer.anchorMax = new Vector2(0, 0);
            m_mapContainer.pivot = new Vector2(0, 0);
            m_mapContainer.anchoredPosition = new Vector2(24, 24);
            m_mapContainer.sizeDelta = new Vector2(m_displayRadius * 2, m_displayRadius * 2);

            // Background (circle)
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(m_mapContainer);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            m_backgroundImage = bgGO.AddComponent<Image>();
            m_backgroundImage.sprite = s_circleSprite64;
            m_backgroundImage.color = BackgroundColor;

            // Border
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(m_mapContainer);
            var borderRect = borderGO.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-2, -2);
            borderRect.offsetMax = new Vector2(2, 2);

            m_borderImage = borderGO.AddComponent<Image>();
            m_borderImage.sprite = s_ringSprite;
            m_borderImage.color = BorderColor;

            // Player blip
            var playerGO = new GameObject("PlayerBlip");
            playerGO.transform.SetParent(m_mapContainer);
            m_playerBlip = playerGO.AddComponent<RectTransform>();
            m_playerBlip.anchorMin = new Vector2(0.5f, 0.5f);
            m_playerBlip.anchorMax = new Vector2(0.5f, 0.5f);
            m_playerBlip.pivot = new Vector2(0.5f, 0.5f);
            m_playerBlip.anchoredPosition = Vector2.zero;
            m_playerBlip.sizeDelta = new Vector2(m_playerBlipSize, m_playerBlipSize);

            var playerImage = playerGO.AddComponent<Image>();
            playerImage.sprite = s_triangleSprite;
            playerImage.color = PlayerColor;

            // Pre-create blip pools (reuse cached sprite)
            for (int i = 0; i < 50; i++)
                m_enemyBlipPool.Add(CreateBlip("EnemyBlip", m_enemyBlipSize, EnemyColor));
            for (int i = 0; i < 20; i++)
                m_pickupBlipPool.Add(CreateBlip("PickupBlip", m_pickupBlipSize, PickupColor));
        }

        private RectTransform CreateBlip(string name, float size, Color color)
        {
            var blipGO = new GameObject(name);
            blipGO.transform.SetParent(m_mapContainer);
            var rect = blipGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);

            var image = blipGO.AddComponent<Image>();
            image.sprite = s_circleSprite16; // Reuse cached sprite
            image.color = color;

            blipGO.SetActive(false);
            return rect;
        }

        private void UpdateMinimap()
        {
            // Cache player transform
            if (m_playerTransform == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                    m_playerTransform = playerGO.transform;
                else
                    return;
            }

            Vector3 playerPos = m_playerTransform.position;

            if (m_rotateWithPlayer)
                m_playerBlip.localRotation = Quaternion.Euler(0, 0, -m_playerTransform.eulerAngles.z);

            // Update enemy blips - use cached EnemySpawner.ActiveEnemies (no FindObjectsByType!)
            m_activeEnemyBlips = 0;
            if (m_enemySpawner != null)
            {
                var enemies = m_enemySpawner.ActiveEnemies;
                foreach (var enemy in enemies)
                {
                    if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                    Vector3 offset = enemy.transform.position - playerPos;
                    if (offset.sqrMagnitude > m_mapRadius * m_mapRadius) continue;

                    if (m_activeEnemyBlips >= m_enemyBlipPool.Count)
                        m_enemyBlipPool.Add(CreateBlip("EnemyBlip", m_enemyBlipSize, EnemyColor));

                    var blip = m_enemyBlipPool[m_activeEnemyBlips];
                    blip.gameObject.SetActive(true);
                    blip.anchoredPosition = new Vector2(offset.x, offset.y) / m_mapRadius * m_displayRadius;

                    var blipImage = blip.GetComponent<Image>();
                    var eliteMod = enemy.GetComponent<EliteModifier>();

                    if (enemy.EnemyType == EnemyType.Boss)
                    {
                        blipImage.color = BossColor;
                        blip.sizeDelta = new Vector2(m_bossBlipSize, m_bossBlipSize);
                    }
                    else if (eliteMod != null && eliteMod.IsElite)
                    {
                        blipImage.color = EliteColor;
                        blip.sizeDelta = new Vector2(m_enemyBlipSize * 1.5f, m_enemyBlipSize * 1.5f);
                    }
                    else
                    {
                        blipImage.color = EnemyColor;
                        blip.sizeDelta = new Vector2(m_enemyBlipSize, m_enemyBlipSize);
                    }

                    m_activeEnemyBlips++;
                }
            }

            // Hide unused enemy blips
            for (int i = m_activeEnemyBlips; i < m_enemyBlipPool.Count; i++)
                m_enemyBlipPool[i].gameObject.SetActive(false);

            // Update pickup blips - use cached PickupSpawner.ActivePickups
            m_activePickupBlips = 0;
            if (m_pickupSpawner != null)
            {
                var pickups = m_pickupSpawner.ActivePickups;
                foreach (var pickup in pickups)
                {
                    if (pickup == null || !pickup.gameObject.activeInHierarchy) continue;

                    Vector3 offset = pickup.transform.position - playerPos;
                    if (offset.sqrMagnitude > m_mapRadius * m_mapRadius) continue;

                    if (m_activePickupBlips >= m_pickupBlipPool.Count)
                        m_pickupBlipPool.Add(CreateBlip("PickupBlip", m_pickupBlipSize, PickupColor));

                    var blip = m_pickupBlipPool[m_activePickupBlips];
                    blip.gameObject.SetActive(true);
                    blip.anchoredPosition = new Vector2(offset.x, offset.y) / m_mapRadius * m_displayRadius;

                    m_activePickupBlips++;
                }
            }

            // Hide unused pickup blips
            for (int i = m_activePickupBlips; i < m_pickupBlipPool.Count; i++)
                m_pickupBlipPool[i].gameObject.SetActive(false);
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

        public void SetVisible(bool visible) => m_canvas.gameObject.SetActive(visible);
        public void SetRange(float radius) => m_mapRadius = radius;
    }
}
