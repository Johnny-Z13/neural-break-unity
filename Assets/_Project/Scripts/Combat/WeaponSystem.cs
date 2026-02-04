using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Entities;
using NeuralBreak.Input;
using NeuralBreak.Config;
using NeuralBreak.Utils;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Player weapon system - handles firing patterns, heat, and weapon upgrades.
    /// 
    /// WEAPON PATTERNS:
    /// - Forward: Single, Double, Triple, Quad, X5 (with configurable spread)
    /// - Rear: Optional backward fire (special pickup)
    /// 
    /// All values driven by WeaponSystemConfig in GameBalanceConfig.
    /// </summary>
    public class WeaponSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController m_player;
        [SerializeField] private Projectile m_projectilePrefab;
        [SerializeField] private EnhancedProjectile m_enhancedProjectilePrefab;
        [SerializeField] private BeamWeapon m_beamWeapon;
        [SerializeField] private Transform m_projectileContainer;

        // Note: MMFeedbacks removed - using native Unity feedback system

        // Config references
        private WeaponSystemConfig Config => ConfigProvider.WeaponSystem;

        // Runtime state
        private int m_powerLevel = 0;
        private float m_fireTimer;
        private float m_rearFireTimer;
        private float m_currentHeat;
        private bool m_isOverheated;
        private float m_overheatTimer;

        // Active modifiers (from pickups)
        private bool m_rapidFireActive;
        private float m_rapidFireTimer;
        private bool m_damageBoostActive;
        private float m_damageBoostTimer;
        private bool m_rearWeaponActive;

        // Object pools
        private ObjectPool<Projectile> m_projectilePool;
        private ObjectPool<EnhancedProjectile> m_enhancedProjectilePool;
        private InputManager m_input;
        private WeaponUpgradeManager m_upgradeManager;
        private PermanentUpgradeManager m_permanentUpgrades;

        #region Public Accessors

        public float Heat => m_currentHeat;
        public float HeatMax => GetHeatMax();
        public float HeatPercent => m_currentHeat / GetHeatMax();
        public bool IsOverheated => m_isOverheated;
        public int PowerLevel => m_powerLevel;
        public int MaxPowerLevel => Config?.powerLevels?.maxLevel ?? 10;
        public ForwardFirePattern CurrentPattern => GetCurrentPattern();
        public bool HasRearWeapon => m_rearWeaponActive ||
                                      (Config?.rearWeapon?.enabled ?? false) ||
                                      (m_permanentUpgrades?.GetCombinedModifiers().enableRearFire ?? false);

        #endregion

        #region Initialization

        private void Awake()
        {
            InitializeProjectilePool();
        }

        private void InitializeProjectilePool()
        {
            // Create projectile container if not assigned
            if (m_projectileContainer == null)
            {
                var container = new GameObject("PlayerProjectiles");
                m_projectileContainer = container.transform;
            }

            // Create runtime projectile prefab if not assigned
            if (m_projectilePrefab == null)
            {
                m_projectilePrefab = CreateRuntimeProjectilePrefab();
            }

            // Create runtime enhanced projectile prefab if not assigned
            if (m_enhancedProjectilePrefab == null)
            {
                m_enhancedProjectilePrefab = CreateRuntimeEnhancedProjectilePrefab();
            }

            // Initialize basic projectile pool
            try
            {
                m_projectilePool = new ObjectPool<Projectile>(
                    m_projectilePrefab,
                    m_projectileContainer,
                    initialSize: 100, // Increased for multi-shot patterns
                    onReturn: proj => proj.OnReturnToPool()
                );
            }
            catch (System.Exception ex)
            {
                LogHelper.LogError($"[WeaponSystem] Failed to create projectile pool: {ex.Message}");
            }

            // Initialize enhanced projectile pool
            try
            {
                m_enhancedProjectilePool = new ObjectPool<EnhancedProjectile>(
                    m_enhancedProjectilePrefab,
                    m_projectileContainer,
                    initialSize: 50, // Smaller pool - only used for special behaviors
                    onReturn: proj => proj.OnReturnToPool()
                );
            }
            catch (System.Exception ex)
            {
                LogHelper.LogError($"[WeaponSystem] Failed to create enhanced projectile pool: {ex.Message}");
            }
        }

        private Projectile CreateRuntimeProjectilePrefab()
        {
            var projectileGO = new GameObject("RuntimeProjectile");
            projectileGO.layer = LayerMask.NameToLayer("Default");

            var rb = projectileGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;

            var collider = projectileGO.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            // Initial radius from config (will be updated in Initialize() with power level scaling)
            collider.radius = ConfigProvider.WeaponSystem?.projectileSize ?? 0.15f;

            var sr = projectileGO.AddComponent<SpriteRenderer>();
            sr.sprite = Graphics.SpriteGenerator.CreateCircle(32, new Color(0.2f, 0.9f, 1f), "ProjectileSprite");
            sr.sortingOrder = 100;
            sr.color = Color.white;

            var trail = projectileGO.AddComponent<TrailRenderer>();
            trail.time = 0.1f;
            trail.startWidth = 0.15f;
            trail.endWidth = 0.02f;
            trail.startColor = new Color(0.2f, 0.9f, 1f, 0.8f);
            trail.endColor = new Color(0.2f, 0.9f, 1f, 0f);
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.numCornerVertices = 4;
            trail.numCapVertices = 4;

            var projectile = projectileGO.AddComponent<Projectile>();
            projectileGO.SetActive(false);

            return projectile;
        }

        private EnhancedProjectile CreateRuntimeEnhancedProjectilePrefab()
        {
            var projectileGO = new GameObject("RuntimeEnhancedProjectile");
            projectileGO.layer = LayerMask.NameToLayer("Default");

            var rb = projectileGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;

            var collider = projectileGO.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            // Initial radius from config (will be updated in Initialize() with power level scaling)
            collider.radius = ConfigProvider.WeaponSystem?.projectileSize ?? 0.15f;

            var sr = projectileGO.AddComponent<SpriteRenderer>();
            sr.sprite = Graphics.SpriteGenerator.CreateCircle(32, new Color(0.2f, 0.9f, 1f), "EnhancedProjectileSprite");
            sr.sortingOrder = 100;
            sr.color = Color.white;

            var trail = projectileGO.AddComponent<TrailRenderer>();
            trail.time = 0.15f;
            trail.startWidth = 0.2f;
            trail.endWidth = 0.03f;
            trail.startColor = new Color(0.2f, 0.9f, 1f, 0.8f);
            trail.endColor = new Color(0.2f, 0.9f, 1f, 0f);
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.numCornerVertices = 5;
            trail.numCapVertices = 5;

            var projectile = projectileGO.AddComponent<EnhancedProjectile>();
            projectileGO.SetActive(false);

            return projectile;
        }

        private void Start()
        {
            m_input = InputManager.Instance;

            if (m_player == null)
            {
                m_player = GetComponent<PlayerController>();
            }

            // Cache WeaponUpgradeManager reference (performance fix - avoid FindObjectOfType per frame)
            m_upgradeManager = FindFirstObjectByType<WeaponUpgradeManager>();
            if (m_upgradeManager == null)
            {
                LogHelper.LogWarning("[WeaponSystem] WeaponUpgradeManager not found - pickup upgrades will not work");
            }

            // Cache PermanentUpgradeManager reference
            m_permanentUpgrades = PermanentUpgradeManager.Instance;
            if (m_permanentUpgrades == null)
            {
                LogHelper.LogWarning("[WeaponSystem] PermanentUpgradeManager not found - permanent upgrades will not work");
            }

            // Subscribe to pickup events
            EventBus.Subscribe<PickupCollectedEvent>(OnPickupCollected);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PickupCollectedEvent>(OnPickupCollected);
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

            UpdateModifierTimers();
            UpdateHeat();
            UpdateFiring();
        }

        private void UpdateModifierTimers()
        {
            // Rapid fire timer
            if (m_rapidFireActive)
            {
                m_rapidFireTimer -= Time.deltaTime;
                if (m_rapidFireTimer <= 0f)
                {
                    m_rapidFireActive = false;
                    LogHelper.Log("[WeaponSystem] Rapid fire expired");
                }
            }

            // Damage boost timer
            if (m_damageBoostActive)
            {
                m_damageBoostTimer -= Time.deltaTime;
                if (m_damageBoostTimer <= 0f)
                {
                    m_damageBoostActive = false;
                    LogHelper.Log("[WeaponSystem] Damage boost expired");
                }
            }
        }

        #endregion

        #region Firing System

        private void UpdateFiring()
        {
            m_fireTimer -= Time.deltaTime;
            m_rearFireTimer -= Time.deltaTime;

            if (m_input == null || !m_input.FireHeld || m_isOverheated) return;

            // Forward weapons
            if (m_fireTimer <= 0f)
            {
                FireForward();
                m_fireTimer = GetFireRate();
            }

            // Rear weapon (if enabled and synced or independent timer ready)
            if (HasRearWeapon)
            {
                var rearConfig = Config?.rearWeapon;
                if (rearConfig != null)
                {
                    if (rearConfig.syncWithForward)
                    {
                        // Already fired with forward
                    }
                    else if (m_rearFireTimer <= 0f)
                    {
                        FireRear();
                        m_rearFireTimer = rearConfig.independentFireRate;
                    }
                }
            }
        }

        private void FireForward()
        {
            if (m_player == null || m_projectilePool == null) return;

            Vector2 direction = m_player.FacingDirection;
            if (direction == Vector2.zero) direction = Vector2.up;

            int damage = CalculateDamage();

            float forwardOffset = Config?.forwardWeapon?.forwardOffset ?? 0.6f;
            Vector2 basePos = m_player.Position + direction * forwardOffset;

            // Check if beam weapon is active
            var modifiers = GetCombinedModifiers();
            if (modifiers.enableBeamWeapon && m_beamWeapon != null)
            {
                FireBeam(basePos, direction, damage, modifiers);
                return;
            }

            // Normal projectile firing
            ForwardFirePattern pattern = GetCurrentPattern();

            // Get spread angle modifier from upgrades
            float spreadAngleModifier = modifiers.spreadAngleAdd;

            // Fire based on pattern
            switch (pattern)
            {
                case ForwardFirePattern.Single:
                    FireSingle(basePos, direction, damage);
                    break;
                case ForwardFirePattern.Double:
                    FireDouble(basePos, direction, damage, spreadAngleModifier);
                    break;
                case ForwardFirePattern.Triple:
                    FireTriple(basePos, direction, damage, spreadAngleModifier);
                    break;
                case ForwardFirePattern.Quad:
                    FireQuad(basePos, direction, damage, spreadAngleModifier);
                    break;
                case ForwardFirePattern.X5:
                    FireX5(basePos, direction, damage, spreadAngleModifier);
                    break;
            }

            // Fire additional projectiles from upgrades (spread evenly)
            int additionalProjectiles = modifiers.additionalProjectiles;
            if (additionalProjectiles > 0)
            {
                FireAdditionalProjectiles(basePos, direction, damage, additionalProjectiles);
            }

            // Fire rear weapon if synced OR if enableRearFire modifier is active
            bool hasRear = HasRearWeapon || modifiers.enableRearFire;
            if (hasRear && (Config?.rearWeapon?.syncWithForward ?? true))
            {
                FireRear();
            }

            // Apply heat
            ApplyHeat(pattern);

            // Feedback
            // Fire feedback (Feel removed)

            // Event
            EventBus.Publish(new ProjectileFiredEvent
            {
                position = basePos,
                direction = direction,
                powerLevel = m_powerLevel
            });
        }

        /// <summary>
        /// Fire additional projectiles from upgrades, spread evenly around the main direction.
        /// </summary>
        private void FireAdditionalProjectiles(Vector2 position, Vector2 direction, int damage, int count)
        {
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float spreadPerProjectile = 15f; // degrees between each additional projectile

            for (int i = 0; i < count; i++)
            {
                // Alternate left and right of main direction
                float offset = ((i / 2) + 1) * spreadPerProjectile;
                float angle = (i % 2 == 0) ? baseAngle + offset : baseAngle - offset;
                FireProjectileAtAngle(position, angle, damage);
            }
        }

        private void FireBeam(Vector2 position, Vector2 direction, int damage, WeaponModifiers modifiers)
        {
            if (m_beamWeapon == null || m_beamWeapon.IsActive) return;

            float damageMultiplier = modifiers.damageMultiplier;
            m_beamWeapon.Fire(position, direction, damageMultiplier);

            // Apply heat for beam
            m_currentHeat += GetHeatPerShot() * 0.5f; // Beam uses less heat

            // Feedback
            // Fire feedback (Feel removed)

            // Schedule beam stop after duration
            StartCoroutine(StopBeamAfterDuration(modifiers.beamDuration > 0f ? modifiers.beamDuration : 0.5f));
        }

        private System.Collections.IEnumerator StopBeamAfterDuration(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (m_beamWeapon != null)
            {
                m_beamWeapon.Stop();
            }
        }

        #region Fire Patterns

        private void FireSingle(Vector2 position, Vector2 direction, int damage)
        {
            FireProjectile(position, direction, damage);
        }

        private void FireDouble(Vector2 position, Vector2 direction, int damage, float spreadMod = 0f)
        {
            float spreadAngle = (Config?.forwardWeapon?.doubleSpreadAngle ?? 15f) + spreadMod;
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Two projectiles with slight spread
            FireProjectileAtAngle(position, baseAngle - spreadAngle / 2f, damage);
            FireProjectileAtAngle(position, baseAngle + spreadAngle / 2f, damage);
        }

        private void FireTriple(Vector2 position, Vector2 direction, int damage, float spreadMod = 0f)
        {
            float spreadAngle = (Config?.forwardWeapon?.tripleSpreadAngle ?? 30f) + spreadMod;
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Center projectile
            FireProjectile(position, direction, damage);

            // Side projectiles
            FireProjectileAtAngle(position, baseAngle - spreadAngle / 2f, damage);
            FireProjectileAtAngle(position, baseAngle + spreadAngle / 2f, damage);
        }

        private void FireQuad(Vector2 position, Vector2 direction, int damage, float spreadMod = 0f)
        {
            float spreadAngle = (Config?.forwardWeapon?.quadSpreadAngle ?? 45f) + spreadMod;
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float step = spreadAngle / 3f;

            // Four projectiles evenly spread
            FireProjectileAtAngle(position, baseAngle - spreadAngle / 2f, damage);
            FireProjectileAtAngle(position, baseAngle - step / 2f, damage);
            FireProjectileAtAngle(position, baseAngle + step / 2f, damage);
            FireProjectileAtAngle(position, baseAngle + spreadAngle / 2f, damage);
        }

        private void FireX5(Vector2 position, Vector2 direction, int damage, float spreadMod = 0f)
        {
            float spreadAngle = (Config?.forwardWeapon?.x5SpreadAngle ?? 60f) + spreadMod;
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float step = spreadAngle / 4f;

            // Five projectiles evenly spread
            FireProjectileAtAngle(position, baseAngle - spreadAngle / 2f, damage);
            FireProjectileAtAngle(position, baseAngle - step, damage);
            FireProjectile(position, direction, damage); // Center
            FireProjectileAtAngle(position, baseAngle + step, damage);
            FireProjectileAtAngle(position, baseAngle + spreadAngle / 2f, damage);
        }

        private void FireRear()
        {
            if (m_player == null || m_projectilePool == null) return;

            var rearConfig = Config?.rearWeapon;
            if (rearConfig == null) return;

            Vector2 direction = m_player.FacingDirection;
            if (direction == Vector2.zero) direction = Vector2.up;

            // Rear direction is opposite of forward
            Vector2 rearDirection = -direction;
            float rearOffset = rearConfig.rearOffset;
            Vector2 rearPos = m_player.Position + rearDirection * rearOffset;

            // Calculate rear damage
            int baseDamage = CalculateDamage();
            int rearDamage = Mathf.RoundToInt(baseDamage * rearConfig.damageMultiplier);

            FireProjectile(rearPos, rearDirection, rearDamage);

            // Apply rear heat
            float rearHeatMult = Config?.heatSystem?.rearWeaponHeatMultiplier ?? 0.5f;
            m_currentHeat += GetHeatPerShot() * rearHeatMult;

            // Rear fire feedback (Feel removed)
        }

        #endregion

        #region Projectile Spawning

        /// <summary>
        /// Get combined modifiers from all sources (temporary pickups + permanent upgrades).
        /// </summary>
        private WeaponModifiers GetCombinedModifiers()
        {
            var modifiers = WeaponModifiers.Identity;

            // Add permanent upgrades
            if (m_permanentUpgrades != null)
            {
                modifiers = WeaponModifiers.Combine(modifiers, m_permanentUpgrades.GetCombinedModifiers());
            }

            // Add temporary pickup modifiers (convert to WeaponModifiers)
            if (m_upgradeManager != null)
            {
                var tempMods = WeaponModifiers.Identity;

                if (m_upgradeManager.HasHoming)
                {
                    tempMods.enableHoming = true;
                    tempMods.homingStrength = m_upgradeManager.HomingStrength;
                }

                if (m_upgradeManager.HasPiercing)
                {
                    tempMods.piercingCount = 5; // Default piercing count for pickup
                }

                modifiers = WeaponModifiers.Combine(modifiers, tempMods);
            }

            // Add config-based specials
            if (Config?.specials != null)
            {
                var configMods = WeaponModifiers.Identity;

                if (Config.specials.piercingEnabled)
                {
                    configMods.piercingCount = Config.specials.maxPierceCount;
                }

                if (Config.specials.homingEnabled)
                {
                    configMods.enableHoming = true;
                    configMods.homingStrength = Config.specials.homingStrength;
                }

                if (Config.specials.explosiveEnabled)
                {
                    configMods.enableExplosion = true;
                    configMods.explosionRadius = Config.specials.explosionRadius;
                }

                if (Config.specials.ricochetEnabled)
                {
                    configMods.enableRicochet = true;
                    configMods.ricochetCount = Config.specials.maxBounces;
                }

                if (Config.specials.chainLightningEnabled)
                {
                    configMods.enableChainLightning = true;
                    configMods.chainLightningTargets = Config.specials.maxChainJumps;
                }

                if (Config.specials.beamEnabled)
                {
                    configMods.enableBeamWeapon = true;
                    configMods.beamDuration = Config.specials.beamDuration;
                }

                modifiers = WeaponModifiers.Combine(modifiers, configMods);
            }

            return modifiers;
        }

        /// <summary>
        /// Check if any special behaviors are active (requires EnhancedProjectile).
        /// </summary>
        private bool HasSpecialBehaviors(WeaponModifiers modifiers)
        {
            return modifiers.enableExplosion ||
                   modifiers.enableChainLightning ||
                   modifiers.enableRicochet ||
                   modifiers.piercingCount > 0 ||
                   modifiers.enableHoming;
        }

        private void FireProjectile(Vector2 position, Vector2 direction, int damage)
        {
            var modifiers = GetCombinedModifiers();

            // Use EnhancedProjectile if special behaviors are active
            if (HasSpecialBehaviors(modifiers))
            {
                EnhancedProjectile proj = m_enhancedProjectilePool.Get(position, Quaternion.identity);
                proj.Initialize(position, direction, damage, m_powerLevel, ReturnEnhancedProjectile, modifiers);
            }
            else
            {
                // Use basic Projectile for performance (no special behaviors)
                Projectile proj = m_projectilePool.Get(position, Quaternion.identity);
                proj.Initialize(position, direction, damage, m_powerLevel, ReturnProjectile, false, false);
            }
        }

        private void FireProjectileAtAngle(Vector2 position, float angleDegrees, int damage)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            FireProjectile(position, direction, damage);
        }

        private void ReturnProjectile(Projectile proj)
        {
            m_projectilePool.Return(proj);
        }

        private void ReturnEnhancedProjectile(EnhancedProjectile proj)
        {
            m_enhancedProjectilePool.Return(proj);
        }

        #endregion

        #endregion

        #region Calculations

        private ForwardFirePattern GetCurrentPattern()
        {
            var powerConfig = Config?.powerLevels;

            // If auto-upgrade is enabled, use power level to determine pattern
            if (powerConfig != null && powerConfig.autoUpgradePattern)
            {
                return powerConfig.GetPatternForLevel(m_powerLevel);
            }

            // Otherwise use manual pattern from ForwardWeaponConfig
            return Config?.forwardWeapon?.pattern ?? ForwardFirePattern.Single;
        }

        private int CalculateDamage()
        {
            int baseDamage = Config?.baseDamage ?? 12;
            float damagePerLevel = Config?.powerLevels?.damagePerLevel ?? 0.1f;

            float multiplier = 1f + (m_powerLevel * damagePerLevel);

            // Apply damage boost modifier
            if (m_damageBoostActive)
            {
                multiplier *= Config?.modifiers?.damageBoostMultiplier ?? 2f;
            }

            // Apply PERMANENT damage multiplier
            if (m_permanentUpgrades != null)
            {
                var modifiers = m_permanentUpgrades.GetCombinedModifiers();
                multiplier *= modifiers.damageMultiplier;

                // Apply critical hit chance
                if (modifiers.criticalChance > 0f)
                {
                    if (Random.value < modifiers.criticalChance)
                    {
                        float critMultiplier = modifiers.criticalMultiplier > 0f ? modifiers.criticalMultiplier : 2f;
                        multiplier *= critMultiplier;
                        // Could publish a CriticalHitEvent here for VFX
                    }
                }
            }

            return Mathf.RoundToInt(baseDamage * multiplier);
        }

        private float GetFireRate()
        {
            float baseRate = Config?.baseFireRate ?? 0.12f;
            float ratePerLevel = Config?.powerLevels?.fireRatePerLevel ?? 0.005f;

            float rate = Mathf.Max(0.05f, baseRate - (m_powerLevel * ratePerLevel));

            // Apply rapid fire modifier from internal state
            if (m_rapidFireActive)
            {
                rate /= Config?.modifiers?.rapidFireMultiplier ?? 1.5f;
            }

            // Check WeaponUpgradeManager for rapid fire pickup (cached reference)
            if (m_upgradeManager != null && m_upgradeManager.HasRapidFire)
            {
                rate /= m_upgradeManager.RapidFireMultiplier;
            }

            // Apply PERMANENT fire rate modifier (NEW)
            if (m_permanentUpgrades != null)
            {
                float permMultiplier = m_permanentUpgrades.GetCombinedModifiers().fireRateMultiplier;
                if (permMultiplier > 1f)
                {
                    rate /= permMultiplier;
                }
            }

            return Mathf.Max(0.03f, rate);
        }

        private float GetHeatPerShot()
        {
            return Config?.heatSystem?.heatPerShot ?? 0.8f;
        }

        private float GetHeatMax()
        {
            return Config?.heatSystem?.maxHeat ?? 100f;
        }

        private void ApplyHeat(ForwardFirePattern pattern)
        {
            float baseHeat = GetHeatPerShot();
            int projectileCount = GetProjectileCountForPattern(pattern);

            // Multi-shot adds extra heat
            float multiShotMult = Config?.heatSystem?.multiShotHeatMultiplier ?? 0.3f;
            float totalHeat = baseHeat + (baseHeat * multiShotMult * (projectileCount - 1));

            m_currentHeat += totalHeat;

            if (m_currentHeat >= GetHeatMax())
            {
                TriggerOverheat();
            }
        }

        private int GetProjectileCountForPattern(ForwardFirePattern pattern)
        {
            return pattern switch
            {
                ForwardFirePattern.Single => 1,
                ForwardFirePattern.Double => 2,
                ForwardFirePattern.Triple => 3,
                ForwardFirePattern.Quad => 4,
                ForwardFirePattern.X5 => 5,
                _ => 1
            };
        }

        #endregion

        #region Heat System

        private void UpdateHeat()
        {
            var heatConfig = Config?.heatSystem;
            if (heatConfig == null || !heatConfig.enabled)
            {
                m_currentHeat = 0f;
                return;
            }

            if (m_isOverheated)
            {
                m_overheatTimer -= Time.deltaTime;
                if (m_overheatTimer <= 0f)
                {
                    ClearOverheat();
                }

                float overheatCoolMult = heatConfig.overheatCooldownMultiplier;
                m_currentHeat = Mathf.Max(0f, m_currentHeat - heatConfig.cooldownRate * overheatCoolMult * Time.deltaTime);
            }
            else if (m_input == null || !m_input.FireHeld)
            {
                m_currentHeat = Mathf.Max(0f, m_currentHeat - heatConfig.cooldownRate * Time.deltaTime);
            }

            EventBus.Publish(new WeaponHeatChangedEvent
            {
                heat = m_currentHeat,
                maxHeat = GetHeatMax(),
                isOverheated = m_isOverheated
            });
        }

        private void TriggerOverheat()
        {
            m_isOverheated = true;
            m_overheatTimer = Config?.heatSystem?.overheatDuration ?? 0.8f;

            // Overheat feedback (Feel removed)

            EventBus.Publish(new WeaponOverheatedEvent
            {
                cooldownDuration = m_overheatTimer
            });

            LogHelper.Log("[WeaponSystem] OVERHEATED!");
        }

        private void ClearOverheat()
        {
            m_isOverheated = false;
            // Overheat cleared feedback (Feel removed)
            LogHelper.Log("[WeaponSystem] Overheat cleared");
        }

        #endregion

        #region Power Level & Modifiers

        public void AddPowerLevel(int amount = 1)
        {
            if (amount <= 0) return;

            int previousLevel = m_powerLevel;
            m_powerLevel = Mathf.Min(m_powerLevel + amount, MaxPowerLevel);

            if (m_powerLevel != previousLevel)
            {
                EventBus.Publish(new PowerUpChangedEvent { newLevel = m_powerLevel });
                LogHelper.Log($"[WeaponSystem] Power level: {m_powerLevel} -> Pattern: {GetCurrentPattern()}");
            }
        }

        public void SetPowerLevel(int level)
        {
            m_powerLevel = Mathf.Clamp(level, 0, MaxPowerLevel);
            EventBus.Publish(new PowerUpChangedEvent { newLevel = m_powerLevel });
        }

        public void ActivateRapidFire(float duration = -1f)
        {
            m_rapidFireActive = true;
            m_rapidFireTimer = duration > 0 ? duration : (Config?.modifiers?.rapidFireDuration ?? 10f);
            LogHelper.Log($"[WeaponSystem] Rapid fire activated for {m_rapidFireTimer}s");
        }

        public void ActivateDamageBoost(float duration = -1f)
        {
            m_damageBoostActive = true;
            m_damageBoostTimer = duration > 0 ? duration : (Config?.modifiers?.damageBoostDuration ?? 8f);
            LogHelper.Log($"[WeaponSystem] Damage boost activated for {m_damageBoostTimer}s");
        }

        public void ActivateRearWeapon()
        {
            m_rearWeaponActive = true;
            LogHelper.Log("[WeaponSystem] Rear weapon activated!");
        }

        public void DeactivateRearWeapon()
        {
            m_rearWeaponActive = false;
            LogHelper.Log("[WeaponSystem] Rear weapon deactivated");
        }

        private void OnPickupCollected(PickupCollectedEvent evt)
        {
            // Handle weapon-related pickups
            switch (evt.pickupType)
            {
                case PickupType.PowerUp:
                    AddPowerLevel();
                    break;
                // Add more pickup types as needed
            }
        }

        public void Reset()
        {
            m_powerLevel = 0;
            m_currentHeat = 0f;
            m_isOverheated = false;
            m_fireTimer = 0f;
            m_rearFireTimer = 0f;
            m_rapidFireActive = false;
            m_damageBoostActive = false;
            m_rearWeaponActive = false;
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Add Power")]
        private void DebugAddPower() => AddPowerLevel();

        [ContextMenu("Debug: Max Power")]
        private void DebugMaxPower() => SetPowerLevel(MaxPowerLevel);

        [ContextMenu("Debug: Activate Rapid Fire")]
        private void DebugRapidFire() => ActivateRapidFire();

        [ContextMenu("Debug: Activate Damage Boost")]
        private void DebugDamageBoost() => ActivateDamageBoost();

        [ContextMenu("Debug: Toggle Rear Weapon")]
        private void DebugRearWeapon()
        {
            if (m_rearWeaponActive) DeactivateRearWeapon();
            else ActivateRearWeapon();
        }

        [ContextMenu("Debug: Trigger Overheat")]
        private void DebugOverheat()
        {
            m_currentHeat = GetHeatMax();
            TriggerOverheat();
        }

        #endregion
    }
}
