using System.Collections.Generic;
using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Manages visual effects - explosions, impacts, trails, and screen effects.
    /// Listens to game events and spawns appropriate particle effects.
    /// Based on TypeScript EffectsSystem.ts.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Explosion Effects")]
        [SerializeField] private ParticleSystem _smallExplosionPrefab;
        [SerializeField] private ParticleSystem _mediumExplosionPrefab;
        [SerializeField] private ParticleSystem _largeExplosionPrefab;
        [SerializeField] private ParticleSystem _bossExplosionPrefab;

        [Header("Hit Effects")]
        [SerializeField] private ParticleSystem _bulletHitPrefab;
        [SerializeField] private ParticleSystem _playerHitPrefab;
        [SerializeField] private ParticleSystem _shieldHitPrefab;

        [Header("Pickup Effects")]
        [SerializeField] private ParticleSystem _pickupCollectPrefab;
        [SerializeField] private ParticleSystem _powerUpPrefab;
        [SerializeField] private ParticleSystem _healPrefab;

        [Header("Special Effects")]
        [SerializeField] private ParticleSystem _invulnerabilityPrefab;
        [SerializeField] private ParticleSystem _comboEffectPrefab;
        [SerializeField] private ParticleSystem _levelUpPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int _poolSizePerEffect = 20;
        [SerializeField] private Transform _effectContainer;

        // Particle pools
        private Dictionary<ParticleSystem, Queue<ParticleSystem>> _particlePools = new Dictionary<ParticleSystem, Queue<ParticleSystem>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePools();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            UnsubscribeFromEvents();
        }

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Subscribe<ShieldChangedEvent>(OnShieldChanged);
            EventBus.Subscribe<PickupCollectedEvent>(OnPickupCollected);
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Subscribe<PowerUpChangedEvent>(OnPowerUpChanged);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Unsubscribe<ShieldChangedEvent>(OnShieldChanged);
            EventBus.Unsubscribe<PickupCollectedEvent>(OnPickupCollected);
            EventBus.Unsubscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Unsubscribe<PowerUpChangedEvent>(OnPowerUpChanged);
        }

        #endregion

        #region Event Handlers

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            // Choose explosion size based on enemy type
            ParticleSystem prefab = evt.enemyType switch
            {
                EnemyType.DataMite => _smallExplosionPrefab,
                EnemyType.Fizzer => _smallExplosionPrefab,
                EnemyType.ScanDrone => _mediumExplosionPrefab,
                EnemyType.UFO => _mediumExplosionPrefab,
                EnemyType.ChaosWorm => _largeExplosionPrefab,
                EnemyType.VoidSphere => _largeExplosionPrefab,
                EnemyType.CrystalShard => _mediumExplosionPrefab,
                EnemyType.Boss => _bossExplosionPrefab,
                _ => _smallExplosionPrefab
            };

            // Get color based on enemy type
            Color explosionColor = GetEnemyColor(evt.enemyType);

            PlayEffect(prefab, evt.position, explosionColor);
        }

        private void OnEnemyDamaged(EnemyDamagedEvent evt)
        {
            // Small hit effect
            PlayEffect(_bulletHitPrefab, evt.position, Color.white);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            PlayEffect(_playerHitPrefab, evt.damageSource, Color.red);
        }

        private void OnPlayerHealed(PlayerHealedEvent evt)
        {
            // Find player position
            var player = FindFirstObjectByType<Entities.PlayerController>();
            if (player != null)
            {
                PlayEffect(_healPrefab, player.transform.position, Color.green);
            }
        }

        private void OnShieldChanged(ShieldChangedEvent evt)
        {
            // Shield hit/gain effect
            var player = FindFirstObjectByType<Entities.PlayerController>();
            if (player != null)
            {
                PlayEffect(_shieldHitPrefab, player.transform.position, Color.cyan);
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

            PlayEffect(_pickupCollectPrefab, evt.position, pickupColor);

            // Special effect for invulnerable
            if (evt.pickupType == PickupType.Invulnerable)
            {
                var player = FindFirstObjectByType<Entities.PlayerController>();
                if (player != null)
                {
                    PlayEffect(_invulnerabilityPrefab, player.transform.position, Color.yellow);
                }
            }
        }

        private void OnComboChanged(ComboChangedEvent evt)
        {
            // Big combo milestones
            if (evt.comboCount > 0 && evt.comboCount % 10 == 0)
            {
                var player = FindFirstObjectByType<Entities.PlayerController>();
                if (player != null)
                {
                    PlayEffect(_comboEffectPrefab, player.transform.position, Color.magenta);
                }
            }
        }

        private void OnPowerUpChanged(PowerUpChangedEvent evt)
        {
            var player = FindFirstObjectByType<Entities.PlayerController>();
            if (player != null)
            {
                PlayEffect(_powerUpPrefab, player.transform.position, new Color(1f, 0.8f, 0f));
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
                ExplosionSize.Small => _smallExplosionPrefab,
                ExplosionSize.Medium => _mediumExplosionPrefab,
                ExplosionSize.Large => _largeExplosionPrefab,
                ExplosionSize.Boss => _bossExplosionPrefab,
                _ => _smallExplosionPrefab
            };

            PlayEffect(prefab, position, color ?? Color.white);
        }

        /// <summary>
        /// Play a hit effect at position
        /// </summary>
        public void PlayHitEffect(Vector3 position, Color? color = null)
        {
            PlayEffect(_bulletHitPrefab, position, color ?? Color.white);
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

            effect.gameObject.SetActive(true);
            effect.Play();

            // Return to pool when done
            StartCoroutine(ReturnToPoolWhenDone(effect, prefab));
        }

        #endregion

        #region Pool Management

        private void InitializePools()
        {
            if (_effectContainer == null)
            {
                _effectContainer = new GameObject("VFX_Pool").transform;
                _effectContainer.SetParent(transform);
            }

            // Create default effects if not assigned
            CreateDefaultEffectsIfNeeded();

            // Initialize pools for each prefab
            InitializePool(_smallExplosionPrefab);
            InitializePool(_mediumExplosionPrefab);
            InitializePool(_largeExplosionPrefab);
            InitializePool(_bossExplosionPrefab);
            InitializePool(_bulletHitPrefab);
            InitializePool(_playerHitPrefab);
            InitializePool(_shieldHitPrefab);
            InitializePool(_pickupCollectPrefab);
            InitializePool(_powerUpPrefab);
            InitializePool(_healPrefab);
            InitializePool(_invulnerabilityPrefab);
            InitializePool(_comboEffectPrefab);
            InitializePool(_levelUpPrefab);
        }

        private void CreateDefaultEffectsIfNeeded()
        {
            // Create simple particle effects if prefabs not assigned
            // This allows the game to run without designer-created effects

            if (_smallExplosionPrefab == null)
                _smallExplosionPrefab = ParticleEffectFactory.CreateExplosion(_effectContainer, "SmallExplosion", 0.5f, 15, Color.white);

            if (_mediumExplosionPrefab == null)
                _mediumExplosionPrefab = ParticleEffectFactory.CreateExplosion(_effectContainer, "MediumExplosion", 1f, 25, Color.white);

            if (_largeExplosionPrefab == null)
                _largeExplosionPrefab = ParticleEffectFactory.CreateExplosion(_effectContainer, "LargeExplosion", 2f, 40, Color.white);

            if (_bossExplosionPrefab == null)
                _bossExplosionPrefab = ParticleEffectFactory.CreateExplosion(_effectContainer, "BossExplosion", 4f, 80, Color.red);

            if (_bulletHitPrefab == null)
                _bulletHitPrefab = ParticleEffectFactory.CreateHitSpark(_effectContainer, "BulletHit", Color.yellow);

            if (_playerHitPrefab == null)
                _playerHitPrefab = ParticleEffectFactory.CreateHitSpark(_effectContainer, "PlayerHit", Color.red);

            if (_shieldHitPrefab == null)
                _shieldHitPrefab = ParticleEffectFactory.CreateHitSpark(_effectContainer, "ShieldHit", Color.cyan);

            if (_pickupCollectPrefab == null)
                _pickupCollectPrefab = ParticleEffectFactory.CreatePickupEffect(_effectContainer, "PickupCollect", Color.white);

            if (_powerUpPrefab == null)
                _powerUpPrefab = ParticleEffectFactory.CreatePickupEffect(_effectContainer, "PowerUp", new Color(1f, 0.8f, 0f));

            if (_healPrefab == null)
                _healPrefab = ParticleEffectFactory.CreatePickupEffect(_effectContainer, "Heal", Color.green);

            if (_invulnerabilityPrefab == null)
                _invulnerabilityPrefab = ParticleEffectFactory.CreatePickupEffect(_effectContainer, "Invulnerability", Color.yellow);

            if (_comboEffectPrefab == null)
                _comboEffectPrefab = ParticleEffectFactory.CreateExplosion(_effectContainer, "ComboEffect", 1.5f, 30, Color.magenta);

            if (_levelUpPrefab == null)
                _levelUpPrefab = ParticleEffectFactory.CreateExplosion(_effectContainer, "LevelUp", 2f, 50, Color.cyan);
        }

        private void InitializePool(ParticleSystem prefab)
        {
            if (prefab == null) return;

            var pool = new Queue<ParticleSystem>();

            for (int i = 0; i < _poolSizePerEffect; i++)
            {
                ParticleSystem instance = Instantiate(prefab, _effectContainer);
                instance.gameObject.SetActive(false);
                pool.Enqueue(instance);
            }

            _particlePools[prefab] = pool;
        }

        private ParticleSystem GetFromPool(ParticleSystem prefab)
        {
            if (prefab == null) return null;

            if (!_particlePools.TryGetValue(prefab, out var pool))
            {
                // Create pool on demand
                InitializePool(prefab);
                pool = _particlePools[prefab];
            }

            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }

            // Pool exhausted, create new instance
            ParticleSystem newInstance = Instantiate(prefab, _effectContainer);
            return newInstance;
        }

        private void ReturnToPool(ParticleSystem effect, ParticleSystem prefab)
        {
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.gameObject.SetActive(false);

            if (_particlePools.TryGetValue(prefab, out var pool))
            {
                pool.Enqueue(effect);
            }
        }

        private System.Collections.IEnumerator ReturnToPoolWhenDone(ParticleSystem effect, ParticleSystem prefab)
        {
            // Wait for particle system to finish
            yield return new WaitUntil(() => !effect.isPlaying);

            ReturnToPool(effect, prefab);
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
