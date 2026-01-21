using UnityEngine;
using System;
using System.Collections.Generic;
using NeuralBreak.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Ship skin definition
    /// </summary>
    [Serializable]
    public class ShipSkin
    {
        public string id;
        public string name;
        public string description;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.cyan;
        public Color trailColor = Color.cyan;
        public Color projectileColor = Color.yellow;
        public ShipShape shape = ShipShape.Triangle;
        public bool hasGlow = true;
        public float glowIntensity = 1f;
        public UnlockRequirement unlockRequirement;
        public int unlockValue;
    }

    /// <summary>
    /// Ship shape types
    /// </summary>
    public enum ShipShape
    {
        Triangle,       // Default arrow shape
        Diamond,        // Rotated square
        Arrow,          // Sharper arrow
        Circle,         // Round ship
        Hexagon,        // Six-sided
        Star,           // Star shape
        Custom          // Uses custom sprite
    }

    /// <summary>
    /// How to unlock a skin
    /// </summary>
    public enum UnlockRequirement
    {
        Default,        // Available from start
        Score,          // Reach score threshold
        Level,          // Reach level threshold
        Kills,          // Kill X enemies
        Bosses,         // Kill X bosses
        Combo,          // Achieve X combo
        Achievement,    // Unlock specific achievement
        Time            // Survive X seconds
    }

    /// <summary>
    /// Manages player ship customization.
    /// Handles skin selection, unlocks, and visual application.
    /// </summary>
    public class ShipCustomization : MonoBehaviour
    {
        public static ShipCustomization Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private string _currentSkinId = "default";

        [Header("Available Skins")]
        [SerializeField] private List<ShipSkin> _skins = new List<ShipSkin>();

        // Components
        private PlayerController _player;
        private SpriteRenderer _spriteRenderer;
        private TrailRenderer _trailRenderer;
        private ShipSkin _currentSkin;

        public ShipSkin CurrentSkin => _currentSkin;
        public IReadOnlyList<ShipSkin> AllSkins => _skins;

        public event Action<ShipSkin> OnSkinChanged;
        public event Action<ShipSkin> OnSkinUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeSkins();
        }

        private void Start()
        {
            _player = FindFirstObjectByType<PlayerController>();
            if (_player != null)
            {
                _spriteRenderer = _player.GetComponent<SpriteRenderer>();
                _trailRenderer = _player.GetComponentInChildren<TrailRenderer>();
            }

            // Load selected skin from save
            if (SaveSystem.Instance != null && SaveSystem.Instance.CurrentSave != null)
            {
                _currentSkinId = SaveSystem.Instance.CurrentSave.selectedShipSkin;
            }

            ApplySkin(_currentSkinId);

            // Subscribe to events for unlock checking
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<BossDefeatedEvent>(OnBossDefeated);
            EventBus.Subscribe<PlayerLevelUpEvent>(OnLevelUp);
            EventBus.Subscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<BossDefeatedEvent>(OnBossDefeated);
            EventBus.Unsubscribe<PlayerLevelUpEvent>(OnLevelUp);
            EventBus.Unsubscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void InitializeSkins()
        {
            _skins.Clear();

            // Default skin
            _skins.Add(new ShipSkin
            {
                id = "default",
                name = "Cyber Wing",
                description = "Standard issue neural fighter",
                primaryColor = Color.white,
                secondaryColor = new Color(0.3f, 0.8f, 1f),
                trailColor = new Color(0.3f, 0.8f, 1f, 0.8f),
                projectileColor = new Color(1f, 1f, 0.5f),
                shape = ShipShape.Triangle,
                hasGlow = true,
                glowIntensity = 1f,
                unlockRequirement = UnlockRequirement.Default
            });

            // Score unlocks
            _skins.Add(new ShipSkin
            {
                id = "golden",
                name = "Golden Ace",
                description = "For high scorers",
                primaryColor = new Color(1f, 0.85f, 0.3f),
                secondaryColor = new Color(1f, 0.6f, 0.1f),
                trailColor = new Color(1f, 0.8f, 0.2f, 0.8f),
                projectileColor = new Color(1f, 0.9f, 0.3f),
                shape = ShipShape.Arrow,
                hasGlow = true,
                glowIntensity = 1.5f,
                unlockRequirement = UnlockRequirement.Score,
                unlockValue = 100000
            });

            _skins.Add(new ShipSkin
            {
                id = "platinum",
                name = "Platinum Elite",
                description = "Score 500,000 points",
                primaryColor = new Color(0.9f, 0.9f, 0.95f),
                secondaryColor = new Color(0.7f, 0.8f, 1f),
                trailColor = new Color(0.8f, 0.9f, 1f, 0.8f),
                projectileColor = Color.white,
                shape = ShipShape.Diamond,
                hasGlow = true,
                glowIntensity = 2f,
                unlockRequirement = UnlockRequirement.Score,
                unlockValue = 500000
            });

            // Level unlocks
            _skins.Add(new ShipSkin
            {
                id = "void",
                name = "Void Walker",
                description = "Reach level 25",
                primaryColor = new Color(0.4f, 0.1f, 0.6f),
                secondaryColor = new Color(0.8f, 0.3f, 1f),
                trailColor = new Color(0.6f, 0.2f, 0.8f, 0.8f),
                projectileColor = new Color(0.8f, 0.4f, 1f),
                shape = ShipShape.Hexagon,
                hasGlow = true,
                glowIntensity = 1.2f,
                unlockRequirement = UnlockRequirement.Level,
                unlockValue = 25
            });

            _skins.Add(new ShipSkin
            {
                id = "matrix",
                name = "Code Runner",
                description = "Reach level 50",
                primaryColor = new Color(0.2f, 0.8f, 0.3f),
                secondaryColor = new Color(0.1f, 1f, 0.4f),
                trailColor = new Color(0.2f, 1f, 0.3f, 0.8f),
                projectileColor = new Color(0.3f, 1f, 0.4f),
                shape = ShipShape.Arrow,
                hasGlow = true,
                glowIntensity = 1f,
                unlockRequirement = UnlockRequirement.Level,
                unlockValue = 50
            });

            // Kill unlocks
            _skins.Add(new ShipSkin
            {
                id = "hunter",
                name = "Hunter",
                description = "Kill 1,000 enemies",
                primaryColor = new Color(1f, 0.3f, 0.2f),
                secondaryColor = new Color(1f, 0.5f, 0.1f),
                trailColor = new Color(1f, 0.4f, 0.2f, 0.8f),
                projectileColor = new Color(1f, 0.6f, 0.2f),
                shape = ShipShape.Arrow,
                hasGlow = true,
                glowIntensity = 1.3f,
                unlockRequirement = UnlockRequirement.Kills,
                unlockValue = 1000
            });

            _skins.Add(new ShipSkin
            {
                id = "slayer",
                name = "Slayer",
                description = "Kill 10,000 enemies",
                primaryColor = new Color(0.8f, 0.1f, 0.1f),
                secondaryColor = new Color(1f, 0.2f, 0.1f),
                trailColor = new Color(1f, 0.3f, 0.1f, 0.8f),
                projectileColor = new Color(1f, 0.4f, 0.2f),
                shape = ShipShape.Star,
                hasGlow = true,
                glowIntensity = 1.5f,
                unlockRequirement = UnlockRequirement.Kills,
                unlockValue = 10000
            });

            // Boss unlocks
            _skins.Add(new ShipSkin
            {
                id = "boss_hunter",
                name = "Boss Hunter",
                description = "Defeat 5 bosses",
                primaryColor = new Color(1f, 0.1f, 0.5f),
                secondaryColor = new Color(1f, 0.4f, 0.7f),
                trailColor = new Color(1f, 0.3f, 0.6f, 0.8f),
                projectileColor = new Color(1f, 0.5f, 0.8f),
                shape = ShipShape.Diamond,
                hasGlow = true,
                glowIntensity = 1.4f,
                unlockRequirement = UnlockRequirement.Bosses,
                unlockValue = 5
            });

            // Combo unlocks
            _skins.Add(new ShipSkin
            {
                id = "combo_king",
                name = "Combo King",
                description = "Achieve 100x combo",
                primaryColor = new Color(0.3f, 1f, 1f),
                secondaryColor = new Color(0.5f, 1f, 0.8f),
                trailColor = new Color(0.4f, 1f, 0.9f, 0.8f),
                projectileColor = new Color(0.5f, 1f, 1f),
                shape = ShipShape.Hexagon,
                hasGlow = true,
                glowIntensity = 1.6f,
                unlockRequirement = UnlockRequirement.Combo,
                unlockValue = 100
            });

            // Survival unlocks
            _skins.Add(new ShipSkin
            {
                id = "survivor",
                name = "Survivor",
                description = "Survive 10 minutes",
                primaryColor = new Color(0.6f, 0.8f, 1f),
                secondaryColor = new Color(0.4f, 0.6f, 0.9f),
                trailColor = new Color(0.5f, 0.7f, 1f, 0.8f),
                projectileColor = new Color(0.6f, 0.8f, 1f),
                shape = ShipShape.Circle,
                hasGlow = true,
                glowIntensity = 1f,
                unlockRequirement = UnlockRequirement.Time,
                unlockValue = 600
            });

            // Special skins
            _skins.Add(new ShipSkin
            {
                id = "neon",
                name = "Neon Dream",
                description = "Unlock all achievements",
                primaryColor = new Color(1f, 0.2f, 0.8f),
                secondaryColor = new Color(0.2f, 1f, 0.8f),
                trailColor = new Color(1f, 0.4f, 0.8f, 0.8f),
                projectileColor = new Color(0.8f, 1f, 0.4f),
                shape = ShipShape.Star,
                hasGlow = true,
                glowIntensity = 2f,
                unlockRequirement = UnlockRequirement.Achievement,
                unlockValue = 0 // Special case - all achievements
            });

            Debug.Log($"[ShipCustomization] Initialized {_skins.Count} skins");
        }

        #region Skin Application

        /// <summary>
        /// Apply a skin by ID
        /// </summary>
        public bool ApplySkin(string skinId)
        {
            var skin = _skins.Find(s => s.id == skinId);
            if (skin == null)
            {
                Debug.LogWarning($"[ShipCustomization] Skin not found: {skinId}");
                return false;
            }

            if (!IsSkinUnlocked(skinId))
            {
                Debug.LogWarning($"[ShipCustomization] Skin not unlocked: {skinId}");
                return false;
            }

            _currentSkin = skin;
            _currentSkinId = skinId;

            ApplyVisuals(skin);

            // Save selection
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.SetSelectedShipSkin(skinId);
            }

            OnSkinChanged?.Invoke(skin);
            Debug.Log($"[ShipCustomization] Applied skin: {skin.name}");
            return true;
        }

        private void ApplyVisuals(ShipSkin skin)
        {
            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerController>();
                if (_player != null)
                {
                    _spriteRenderer = _player.GetComponent<SpriteRenderer>();
                    _trailRenderer = _player.GetComponentInChildren<TrailRenderer>();
                }
            }

            if (_spriteRenderer != null)
            {
                // Generate and apply sprite
                _spriteRenderer.sprite = GenerateShipSprite(skin.shape, 64);
                _spriteRenderer.color = skin.primaryColor;

                // Apply glow if enabled
                if (skin.hasGlow)
                {
                    // Could add glow material here
                }
            }

            if (_trailRenderer != null)
            {
                _trailRenderer.startColor = skin.trailColor;
                Color endColor = skin.trailColor;
                endColor.a = 0;
                _trailRenderer.endColor = endColor;
            }
        }

        private Sprite GenerateShipSprite(ShipShape shape, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;

            switch (shape)
            {
                case ShipShape.Triangle:
                    DrawTriangle(pixels, size, center);
                    break;

                case ShipShape.Diamond:
                    DrawDiamond(pixels, size, center);
                    break;

                case ShipShape.Arrow:
                    DrawArrow(pixels, size, center);
                    break;

                case ShipShape.Circle:
                    DrawCircle(pixels, size, center);
                    break;

                case ShipShape.Hexagon:
                    DrawHexagon(pixels, size, center);
                    break;

                case ShipShape.Star:
                    DrawStar(pixels, size, center);
                    break;

                default:
                    DrawTriangle(pixels, size, center);
                    break;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void DrawTriangle(Color[] pixels, int size, float center)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float relY = (float)y / size;
                    float halfWidth = (1f - relY) * 0.45f;
                    float relX = (float)x / size - 0.5f;

                    bool inside = y > size * 0.1f && Mathf.Abs(relX) < halfWidth;
                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }
        }

        private void DrawDiamond(Color[] pixels, int size, float center)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center) / center;
                    float dy = Mathf.Abs(y - center) / center;

                    bool inside = dx + dy < 0.8f;
                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }
        }

        private void DrawArrow(Color[] pixels, int size, float center)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float relY = (float)y / size;
                    float relX = (float)x / size - 0.5f;

                    bool inside = false;

                    // Arrow head
                    if (relY > 0.5f)
                    {
                        float halfWidth = (1f - relY) * 0.8f;
                        inside = Mathf.Abs(relX) < halfWidth;
                    }
                    // Arrow body
                    else if (relY > 0.1f)
                    {
                        inside = Mathf.Abs(relX) < 0.15f;
                    }

                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }
        }

        private void DrawCircle(Color[] pixels, int size, float center)
        {
            float radius = size * 0.4f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
        }

        private void DrawHexagon(Color[] pixels, int size, float center)
        {
            float radius = size * 0.4f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float angle = Mathf.Atan2(dy, dx);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Hexagon distance
                    float hexDist = radius / Mathf.Cos(Mathf.Repeat(angle, Mathf.PI / 3f) - Mathf.PI / 6f);

                    bool inside = dist < hexDist;
                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }
        }

        private void DrawStar(Color[] pixels, int size, float center)
        {
            float outerRadius = size * 0.45f;
            float innerRadius = size * 0.2f;
            int points = 5;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float angle = Mathf.Atan2(dy, dx) + Mathf.PI / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Star radius at this angle
                    float normalizedAngle = Mathf.Repeat(angle, Mathf.PI * 2f / points);
                    float t = normalizedAngle / (Mathf.PI / points);
                    float starRadius = Mathf.Lerp(outerRadius, innerRadius, Mathf.Abs(t - 1f));

                    bool inside = dist < starRadius;
                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }
        }

        #endregion

        #region Unlock System

        /// <summary>
        /// Check if a skin is unlocked
        /// </summary>
        public bool IsSkinUnlocked(string skinId)
        {
            var skin = _skins.Find(s => s.id == skinId);
            if (skin == null) return false;

            if (skin.unlockRequirement == UnlockRequirement.Default)
            {
                return true;
            }

            // Check if already unlocked in save
            if (SaveSystem.Instance != null && SaveSystem.Instance.IsShipSkinUnlocked(skinId))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check unlock progress
        /// </summary>
        public (int current, int required) GetUnlockProgress(string skinId)
        {
            var skin = _skins.Find(s => s.id == skinId);
            if (skin == null) return (0, 0);

            int current = 0;
            int required = skin.unlockValue;

            if (SaveSystem.Instance != null && SaveSystem.Instance.CurrentSave != null)
            {
                var save = SaveSystem.Instance.CurrentSave;

                switch (skin.unlockRequirement)
                {
                    case UnlockRequirement.Score:
                        current = save.highScore;
                        break;
                    case UnlockRequirement.Level:
                        current = save.highestLevel;
                        break;
                    case UnlockRequirement.Kills:
                        current = save.totalEnemiesKilled;
                        break;
                    case UnlockRequirement.Bosses:
                        current = save.totalBossesKilled;
                        break;
                    case UnlockRequirement.Combo:
                        current = save.highestCombo;
                        break;
                    case UnlockRequirement.Time:
                        current = Mathf.RoundToInt(save.longestSurvivalTime);
                        break;
                }
            }

            return (current, required);
        }

        private void CheckAndUnlockSkins()
        {
            if (SaveSystem.Instance == null || SaveSystem.Instance.CurrentSave == null) return;

            var save = SaveSystem.Instance.CurrentSave;

            foreach (var skin in _skins)
            {
                if (IsSkinUnlocked(skin.id)) continue;

                bool shouldUnlock = false;

                switch (skin.unlockRequirement)
                {
                    case UnlockRequirement.Score:
                        shouldUnlock = save.highScore >= skin.unlockValue;
                        break;
                    case UnlockRequirement.Level:
                        shouldUnlock = save.highestLevel >= skin.unlockValue;
                        break;
                    case UnlockRequirement.Kills:
                        shouldUnlock = save.totalEnemiesKilled >= skin.unlockValue;
                        break;
                    case UnlockRequirement.Bosses:
                        shouldUnlock = save.totalBossesKilled >= skin.unlockValue;
                        break;
                    case UnlockRequirement.Combo:
                        shouldUnlock = save.highestCombo >= skin.unlockValue;
                        break;
                    case UnlockRequirement.Time:
                        shouldUnlock = save.longestSurvivalTime >= skin.unlockValue;
                        break;
                    case UnlockRequirement.Achievement:
                        // Special case - check all achievements
                        if (AchievementSystem.Instance != null)
                        {
                            shouldUnlock = AchievementSystem.Instance.GetUnlockedCount() >= AchievementSystem.Instance.GetTotalCount();
                        }
                        break;
                }

                if (shouldUnlock)
                {
                    UnlockSkin(skin.id);
                }
            }
        }

        private void UnlockSkin(string skinId)
        {
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.UnlockShipSkin(skinId);
            }

            var skin = _skins.Find(s => s.id == skinId);
            if (skin != null)
            {
                OnSkinUnlocked?.Invoke(skin);
                Debug.Log($"[ShipCustomization] Unlocked skin: {skin.name}");

                // Could show unlock notification here
                EventBus.Publish(new AchievementUnlockedEvent
                {
                    name = $"Ship Unlocked: {skin.name}",
                    description = skin.description
                });
            }
        }

        #endregion

        #region Event Handlers

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            CheckAndUnlockSkins();
        }

        private void OnBossDefeated(BossDefeatedEvent evt)
        {
            CheckAndUnlockSkins();
        }

        private void OnLevelUp(PlayerLevelUpEvent evt)
        {
            CheckAndUnlockSkins();
        }

        private void OnAchievementUnlocked(AchievementUnlockedEvent evt)
        {
            CheckAndUnlockSkins();
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: List All Skins")]
        private void DebugListSkins()
        {
            foreach (var skin in _skins)
            {
                bool unlocked = IsSkinUnlocked(skin.id);
                Debug.Log($"{skin.name} ({skin.id}): {(unlocked ? "UNLOCKED" : "LOCKED")} - {skin.unlockRequirement} {skin.unlockValue}");
            }
        }

        [ContextMenu("Debug: Unlock All Skins")]
        private void DebugUnlockAll()
        {
            foreach (var skin in _skins)
            {
                if (SaveSystem.Instance != null)
                {
                    SaveSystem.Instance.UnlockShipSkin(skin.id);
                }
            }
            Debug.Log("[ShipCustomization] All skins unlocked!");
        }

        [ContextMenu("Debug: Apply Random Skin")]
        private void DebugRandomSkin()
        {
            var unlocked = _skins.FindAll(s => IsSkinUnlocked(s.id));
            if (unlocked.Count > 0)
            {
                var randomSkin = unlocked[UnityEngine.Random.Range(0, unlocked.Count)];
                ApplySkin(randomSkin.id);
            }
        }

        #endregion
    }
}
