using System.Collections.Generic;
using UnityEngine;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Manages visual effects - explosions, impacts, trails, and screen effects.
    /// Listens to game events and spawns appropriate particle effects.
    /// Based on TypeScript EffectsSystem.ts.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        // Tracked active effects for timer-based pool returns (zero-alloc, no coroutines)
        private struct ActiveEffect
        {
            public ParticleSystem effect;
            public ParticleSystem prefab;
        }
        private readonly List<ActiveEffect> m_activeEffects = new List<ActiveEffect>(64);

        [Header("Explosion Effects")]
        [SerializeField] private ParticleSystem m_smallExplosionPrefab;
        [SerializeField] private ParticleSystem m_mediumExplosionPrefab;
        [SerializeField] private ParticleSystem m_largeExplosionPrefab;
        [SerializeField] private ParticleSystem m_bossExplosionPrefab;

        [Header("Hit Effects")]
        [SerializeField] private ParticleSystem m_bulletHitPrefab;
        [SerializeField] private ParticleSystem m_playerHitPrefab;
        [SerializeField] private ParticleSystem m_shieldHitPrefab;

        [Header("Pickup Effects")]
        [SerializeField] private ParticleSystem m_pickupCollectPrefab;
        [SerializeField] private ParticleSystem m_powerUpPrefab;
        [SerializeField] private ParticleSystem m_healPrefab;

        [Header("Special Effects")]
        [SerializeField] private ParticleSystem m_invulnerabilityPrefab;
        [SerializeField] private ParticleSystem m_comboEffectPrefab;
        [SerializeField] private ParticleSystem m_levelUpPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int m_poolSizePerEffect = 20;
        [SerializeField] private Transform m_effectContainer;

        // Particle pools
        private Dictionary<ParticleSystem, Queue<ParticleSystem>> m_particlePools = new Dictionary<ParticleSystem, Queue<ParticleSystem>>();

        // Cached ParticleSystemRenderer per particle instance (avoids GetComponent per play)
        private Dictionary<ParticleSystem, ParticleSystemRenderer> m_cachedRenderers = new Dictionary<ParticleSystem, ParticleSystemRenderer>();

        // Cached references
        private Transform m_playerTransform;
        private SpriteRenderer m_playerSpriteRenderer;

        private void Awake()
        {
            InitializePools();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void Update()
        {
            // Timer-based pool returns (replaces StartCoroutine per effect - zero allocation)
            for (int i = m_activeEffects.Count - 1; i >= 0; i--)
            {
                var active = m_activeEffects[i];
                if (active.effect == null || !active.effect.isPlaying)
                {
                    if (active.effect != null)
                    {
                        ReturnToPool(active.effect, active.prefab);
                    }
                    m_activeEffects.RemoveAt(i);
                }
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            // EnemyKilledEvent VFX handled exclusively by EnemyDeathVFX (per-type customization)
            EventBus.Subscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Subscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Subscribe<ShieldChangedEvent>(OnShieldChanged);
            EventBus.Subscribe<PickupCollectedEvent>(OnPickupCollected);
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Subscribe<PowerUpChangedEvent>(OnPowerUpChanged);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            // Re-enable player sprite if it was hidden from death
            // First, try to find player if not cached
            CachePlayerTransform();

            if (m_playerSpriteRenderer != null)
            {
                m_playerSpriteRenderer.enabled = true;
                Debug.Log("[VFXManager] Re-enabled player sprite on game start");
            }
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Unsubscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Unsubscribe<ShieldChangedEvent>(OnShieldChanged);
            EventBus.Unsubscribe<PickupCollectedEvent>(OnPickupCollected);
            EventBus.Unsubscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Unsubscribe<PowerUpChangedEvent>(OnPowerUpChanged);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        #endregion

        #region Event Handlers

        private void OnEnemyDamaged(EnemyDamagedEvent evt)
        {
            // Small hit effect
            PlayEffect(m_bulletHitPrefab, evt.position, Color.white);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            PlayEffect(m_playerHitPrefab, evt.damageSource, Color.red);
        }

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            // Spectacular multi-stage death sequence
            StartCoroutine(PlayerDeathExplosionSequence(evt.position));

            // Hide player ship
            CachePlayerTransform();
            if (m_playerSpriteRenderer != null)
            {
                m_playerSpriteRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Spectacular multi-stage player death explosion sequence.
        /// Creates a series of explosions with varying sizes and colors for maximum impact.
        /// </summary>
        private System.Collections.IEnumerator PlayerDeathExplosionSequence(Vector3 position)
        {
            // Stage 1: Small rapid explosions (fragmentation)
            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.5f;
                Color fragmentColor = Color.Lerp(Color.cyan, Color.white, UnityEngine.Random.value);
                PlayExplosion(position + (Vector3)offset, ExplosionSize.Small, fragmentColor);
                yield return new WaitForSeconds(0.05f);
            }

            yield return new WaitForSeconds(0.1f);

            // Stage 2: Medium explosions (expanding shockwave)
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1f;
                Color shockwaveColor = Color.Lerp(Color.cyan, new Color(0.5f, 1f, 1f), 0.5f);
                PlayExplosion(position + (Vector3)offset, ExplosionSize.Medium, shockwaveColor);
                yield return new WaitForSeconds(0.08f);
            }

            yield return new WaitForSeconds(0.15f);

            // Stage 3: MASSIVE central explosion (final burst)
            PlayExplosion(position, ExplosionSize.Boss, Color.cyan);

            yield return new WaitForSeconds(0.05f);

            // Add secondary burst for extra impact
            PlayExplosion(position, ExplosionSize.Boss, Color.white);

            yield return new WaitForSeconds(0.1f);

            // Stage 4: Lingering glow particles (aftermath)
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 2f;
                PlayExplosion(position + (Vector3)offset, ExplosionSize.Small, new Color(0.2f, 0.8f, 1f, 0.5f));
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void OnPlayerHealed(PlayerHealedEvent evt)
        {
            CachePlayerTransform();
            if (m_playerTransform != null)
            {
                PlayEffect(m_healPrefab, m_playerTransform.position, Color.green);
            }
        }

        private void OnShieldChanged(ShieldChangedEvent evt)
        {
            CachePlayerTransform();
            if (m_playerTransform != null)
            {
                PlayEffect(m_shieldHitPrefab, m_playerTransform.position, Color.cyan);
            }
        }

        private void OnPickupCollected(PickupCollectedEvent evt)
        {
            Color pickupColor = evt.pickupType switch
            {
                PickupType.PowerUp => new Color(1f, 0.8f, 0f),
                PickupType.SpeedUp => new Color(1f, 0.3f, 0.8f),
                PickupType.MedPack => Color.green,
                PickupType.Shield => Color.cyan,
                PickupType.Invulnerable => Color.yellow,
                _ => Color.white
            };

            PlayEffect(m_pickupCollectPrefab, evt.position, pickupColor);

            // Special effect for invulnerable
            if (evt.pickupType == PickupType.Invulnerable)
            {
                CachePlayerTransform();
                if (m_playerTransform != null)
                {
                    PlayEffect(m_invulnerabilityPrefab, m_playerTransform.position, Color.yellow);
                }
            }
        }

        private void OnComboChanged(ComboChangedEvent evt)
        {
            // Big combo milestones
            if (evt.comboCount > 0 && evt.comboCount % 10 == 0)
            {
                CachePlayerTransform();
                if (m_playerTransform != null)
                {
                    PlayEffect(m_comboEffectPrefab, m_playerTransform.position, Color.magenta);
                }
            }
        }

        private void OnPowerUpChanged(PowerUpChangedEvent evt)
        {
            CachePlayerTransform();
            if (m_playerTransform != null)
            {
                PlayEffect(m_powerUpPrefab, m_playerTransform.position, new Color(1f, 0.8f, 0f));
            }
        }

        private void CachePlayerTransform()
        {
            if (m_playerTransform == null)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                {
                    m_playerTransform = playerGO.transform;
                    m_playerSpriteRenderer = playerGO.GetComponent<SpriteRenderer>();
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Play an explosion effect at position
        /// </summary>
        public void PlayExplosion(Vector3 position, ExplosionSize size, Color? color = null)
        {
            ParticleSystem prefab = size switch
            {
                ExplosionSize.Small => m_smallExplosionPrefab,
                ExplosionSize.Medium => m_mediumExplosionPrefab,
                ExplosionSize.Large => m_largeExplosionPrefab,
                ExplosionSize.Boss => m_bossExplosionPrefab,
                _ => m_smallExplosionPrefab
            };

            PlayEffect(prefab, position, color ?? Color.white);
        }

        /// <summary>
        /// Play a hit effect at position
        /// </summary>
        public void PlayHitEffect(Vector3 position, Color? color = null)
        {
            PlayEffect(m_bulletHitPrefab, position, color ?? Color.white);
        }

        /// <summary>
        /// Play a custom effect
        /// </summary>
        public void PlayEffect(ParticleSystem prefab, Vector3 position, Color color)
        {
            if (prefab == null) return;

            ParticleSystem effect = GetFromPool(prefab);
            if (effect == null) return;

            // Stop any previous playback before modifying properties
            if (effect.isPlaying)
            {
                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            effect.transform.position = position;

            // Set color (safe now that system is stopped)
            var main = effect.main;
            main.startColor = color;

            // Update material color for URP compatibility (use cached renderer)
            if (m_cachedRenderers.TryGetValue(effect, out var cachedRenderer) && cachedRenderer != null && cachedRenderer.material != null)
            {
                Color emissiveColor = color * 3f; // Match emission intensity
                cachedRenderer.material.SetColor("_BaseColor", emissiveColor);
                cachedRenderer.material.SetColor("_Color", emissiveColor);
            }

            effect.gameObject.SetActive(true);
            effect.Play();

            // Track for timer-based pool return (zero allocation - no coroutine)
            m_activeEffects.Add(new ActiveEffect { effect = effect, prefab = prefab });
        }

        #endregion

        #region Pool Management

        private void InitializePools()
        {
            if (m_effectContainer == null)
            {
                m_effectContainer = new GameObject("VFX_Pool").transform;
                m_effectContainer.SetParent(transform);
            }

            // Create default effects if not assigned
            CreateDefaultEffectsIfNeeded();

            // Initialize pools for each prefab
            InitializePool(m_smallExplosionPrefab);
            InitializePool(m_mediumExplosionPrefab);
            InitializePool(m_largeExplosionPrefab);
            InitializePool(m_bossExplosionPrefab);
            InitializePool(m_bulletHitPrefab);
            InitializePool(m_playerHitPrefab);
            InitializePool(m_shieldHitPrefab);
            InitializePool(m_pickupCollectPrefab);
            InitializePool(m_powerUpPrefab);
            InitializePool(m_healPrefab);
            InitializePool(m_invulnerabilityPrefab);
            InitializePool(m_comboEffectPrefab);
            InitializePool(m_levelUpPrefab);
        }

        private void CreateDefaultEffectsIfNeeded()
        {
            // Create simple particle effects if prefabs not assigned
            // This allows the game to run without designer-created effects

            if (m_smallExplosionPrefab == null)
                m_smallExplosionPrefab = ParticleEffectFactory.CreateExplosion(m_effectContainer, "SmallExplosion", 0.5f, 15, Color.white);

            if (m_mediumExplosionPrefab == null)
                m_mediumExplosionPrefab = ParticleEffectFactory.CreateExplosion(m_effectContainer, "MediumExplosion", 1f, 25, Color.white);

            if (m_largeExplosionPrefab == null)
                m_largeExplosionPrefab = ParticleEffectFactory.CreateExplosion(m_effectContainer, "LargeExplosion", 2f, 40, Color.white);

            if (m_bossExplosionPrefab == null)
                m_bossExplosionPrefab = ParticleEffectFactory.CreateExplosion(m_effectContainer, "BossExplosion", 4f, 80, Color.red);

            if (m_bulletHitPrefab == null)
                m_bulletHitPrefab = ParticleEffectFactory.CreateHitSpark(m_effectContainer, "BulletHit", Color.yellow);

            if (m_playerHitPrefab == null)
                m_playerHitPrefab = ParticleEffectFactory.CreateHitSpark(m_effectContainer, "PlayerHit", Color.red);

            if (m_shieldHitPrefab == null)
                m_shieldHitPrefab = ParticleEffectFactory.CreateHitSpark(m_effectContainer, "ShieldHit", Color.cyan);

            if (m_pickupCollectPrefab == null)
                m_pickupCollectPrefab = ParticleEffectFactory.CreatePickupEffect(m_effectContainer, "PickupCollect", Color.white);

            if (m_powerUpPrefab == null)
                m_powerUpPrefab = ParticleEffectFactory.CreatePickupEffect(m_effectContainer, "PowerUp", new Color(1f, 0.8f, 0f));

            if (m_healPrefab == null)
                m_healPrefab = ParticleEffectFactory.CreatePickupEffect(m_effectContainer, "Heal", Color.green);

            if (m_invulnerabilityPrefab == null)
                m_invulnerabilityPrefab = ParticleEffectFactory.CreatePickupEffect(m_effectContainer, "Invulnerability", Color.yellow);

            if (m_comboEffectPrefab == null)
                m_comboEffectPrefab = ParticleEffectFactory.CreateExplosion(m_effectContainer, "ComboEffect", 1.5f, 30, Color.magenta);

            if (m_levelUpPrefab == null)
                m_levelUpPrefab = ParticleEffectFactory.CreateExplosion(m_effectContainer, "LevelUp", 2f, 50, Color.cyan);
        }

        private void InitializePool(ParticleSystem prefab)
        {
            if (prefab == null) return;

            var pool = new Queue<ParticleSystem>();

            for (int i = 0; i < m_poolSizePerEffect; i++)
            {
                ParticleSystem instance = Instantiate(prefab, m_effectContainer);
                instance.gameObject.SetActive(false);
                // Cache ParticleSystemRenderer to avoid GetComponent during gameplay
                var psr = instance.GetComponent<ParticleSystemRenderer>();
                if (psr != null)
                {
                    m_cachedRenderers[instance] = psr;
                }
                pool.Enqueue(instance);
            }

            m_particlePools[prefab] = pool;
        }

        private ParticleSystem GetFromPool(ParticleSystem prefab)
        {
            if (prefab == null) return null;

            if (!m_particlePools.TryGetValue(prefab, out var pool))
            {
                // Create pool on demand
                InitializePool(prefab);
                pool = m_particlePools[prefab];
            }

            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }

            // Pool exhausted, create new instance
            ParticleSystem newInstance = Instantiate(prefab, m_effectContainer);
            // Cache renderer for overflow instances
            var psr = newInstance.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                m_cachedRenderers[newInstance] = psr;
            }
            return newInstance;
        }

        private void ReturnToPool(ParticleSystem effect, ParticleSystem prefab)
        {
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.gameObject.SetActive(false);

            if (m_particlePools.TryGetValue(prefab, out var pool))
            {
                pool.Enqueue(effect);
            }
        }

        #endregion

        #region Helpers

        private Color GetEnemyColor(EnemyType type)
        {
            return type switch
            {
                EnemyType.DataMite => new Color(1f, 0.5f, 0f), // Orange
                EnemyType.ScanDrone => new Color(0.3f, 0.8f, 1f), // Cyan
                EnemyType.Fizzer => new Color(1f, 1f, 0.3f), // Electric yellow
                EnemyType.UFO => new Color(0.5f, 1f, 0.5f), // Green
                EnemyType.ChaosWorm => new Color(0.8f, 0.2f, 0.5f), // Purple-pink
                EnemyType.VoidSphere => new Color(0.6f, 0f, 1f), // Purple
                EnemyType.CrystalShard => new Color(0.4f, 0.8f, 1f), // Ice blue
                EnemyType.Boss => Color.red,
                _ => Color.white
            };
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Small Explosion")]
        private void DebugSmallExplosion() => PlayExplosion(Vector3.zero, ExplosionSize.Small, Color.red);

        [ContextMenu("Debug: Large Explosion")]
        private void DebugLargeExplosion() => PlayExplosion(Vector3.zero, ExplosionSize.Large, Color.yellow);

        #endregion
    }

    public enum ExplosionSize
    {
        Small,
        Medium,
        Large,
        Boss
    }
}
