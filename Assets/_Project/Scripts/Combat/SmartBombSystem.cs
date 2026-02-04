using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Entities;
using NeuralBreak.Input;
using NeuralBreak.Config;
using Z13.Core;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Smart Bomb system - screen-clearing super weapon.
    /// Kills all enemies on screen with epic full-screen VFX and camera shake.
    /// Player starts with 1, max 3.
    /// </summary>
    public class SmartBombSystem : MonoBehaviour
    {
        [Header("Smart Bomb Settings (from GameBalanceConfig)")]
        [Tooltip("These values are loaded from GameBalanceConfig at runtime")]
        [SerializeField] private int m_startingBombs = 0;
        [SerializeField] private int m_maxBombs = 3;
        [SerializeField] private float m_activationDuration = 0.5f;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem m_explosionParticles;
        [SerializeField] private Color m_explosionStartColor = new Color(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color m_explosionEndColor = new Color(1f, 0.3f, 0.1f, 0f);

        // Note: MMFeedbacks removed

        [Header("Audio")]
        [SerializeField] private AudioClip m_epicExplosionSound;
        [SerializeField] private float m_explosionVolume = 0.8f;

        // State
        private int m_currentBombs;
        private bool m_isActivating;
        private float m_activationTimer;
        private AudioSource m_audioSource;
        private int m_bonusBombs;

        // Cached reference - avoids FindObjectsByType every bomb!
        private Entities.EnemySpawner m_enemySpawner;

        // Public accessors
        public int CurrentBombs => m_currentBombs;
        public int MaxBombs => m_maxBombs + m_bonusBombs;
        public bool CanUseBomb => m_currentBombs > 0 && !m_isActivating;

        private void Awake()
        {
            // Load settings from GameBalanceConfig
            LoadConfigValues();

            m_currentBombs = m_startingBombs;

            // Create audio source
            m_audioSource = gameObject.AddComponent<AudioSource>();
            m_audioSource.playOnAwake = false;
            m_audioSource.spatialBlend = 0f; // 2D sound
        }

        private void LoadConfigValues()
        {
            var config = ConfigProvider.Balance?.smartBomb;
            if (config != null)
            {
                m_startingBombs = config.startingBombs;
                m_maxBombs = config.maxBombs;
                m_activationDuration = config.activationDuration;
                Debug.Log($"[SmartBombSystem] Loaded config: starting={m_startingBombs}, max={m_maxBombs}");
            }
            else
            {
                Debug.LogWarning("[SmartBombSystem] GameBalanceConfig.smartBomb not found, using defaults");
            }
        }

        private void Start()
        {
            // Subscribe to events
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<WeaponModifiersChangedEvent>(OnModifiersChanged);

            // Subscribe to input events
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnSmartBombPressed += TryActivateBomb;
                Debug.Log("[SmartBombSystem] Subscribed to OnSmartBombPressed - press B to activate!");
            }
            else
            {
                Debug.LogError("[SmartBombSystem] InputManager.Instance is null! Smart Bomb input won't work.");
            }

            // Publish initial state
            EventBus.Publish(new SmartBombCountChangedEvent
            {
                count = m_currentBombs,
                maxCount = MaxBombs
            });

            // Ensure particles are set up
            if (m_explosionParticles != null)
            {
                SetupExplosionParticles();
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<WeaponModifiersChangedEvent>(OnModifiersChanged);

            // Unsubscribe from input
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnSmartBombPressed -= TryActivateBomb;
            }
        }

        private void OnModifiersChanged(WeaponModifiersChangedEvent evt)
        {
            int bombDelta = evt.modifiers.bonusSmartBombs - m_bonusBombs;
            if (bombDelta != 0)
            {
                m_bonusBombs = evt.modifiers.bonusSmartBombs;

                // If bonus increased, add bombs
                if (bombDelta > 0)
                {
                    m_currentBombs = Mathf.Min(m_currentBombs + bombDelta, MaxBombs);
                }

                EventBus.Publish(new SmartBombCountChangedEvent
                {
                    count = m_currentBombs,
                    maxCount = MaxBombs
                });

                Debug.Log($"[SmartBombSystem] Bomb bonus changed: +{m_bonusBombs}. Max bombs: {MaxBombs}");
            }
        }

        private void Update()
        {
            if (m_isActivating)
            {
                m_activationTimer -= Time.deltaTime;
                if (m_activationTimer <= 0f)
                {
                    m_isActivating = false;
                }
            }
        }

        /// <summary>
        /// Try to activate a smart bomb
        /// </summary>
        public void TryActivateBomb()
        {
            Debug.Log($"[SmartBombSystem] TryActivateBomb called! Bombs: {m_currentBombs}, IsActivating: {m_isActivating}, CanUseBomb: {CanUseBomb}");

            if (!CanUseBomb)
            {
                Debug.LogWarning("[SmartBombSystem] Cannot activate - no bombs available or already activating!");
                return;
            }

            ActivateBomb();
        }

        private void ActivateBomb()
        {
            m_currentBombs--;
            m_isActivating = true;
            m_activationTimer = m_activationDuration;

            // Epic audio
            if (m_epicExplosionSound != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(m_epicExplosionSound, m_explosionVolume);
            }

            // Feedback (Feel removed)

            // Visual effect
            if (m_explosionParticles != null)
            {
                m_explosionParticles.Play();
            }

            // Kill all enemies
            KillAllEnemies();

            // Publish event
            EventBus.Publish(new SmartBombActivatedEvent { position = transform.position });
            EventBus.Publish(new SmartBombCountChangedEvent
            {
                count = m_currentBombs,
                maxCount = MaxBombs
            });

            Debug.Log($"[SmartBombSystem] SMART BOMB ACTIVATED! Bombs remaining: {m_currentBombs}");
        }

        private void KillAllEnemies()
        {
            // Use cached EnemySpawner.ActiveEnemies instead of FindObjectsByType
            if (m_enemySpawner == null)
                m_enemySpawner = FindFirstObjectByType<Entities.EnemySpawner>();

            int killedCount = 0;

            if (m_enemySpawner != null)
            {
                // Iterate the cached active enemies list
                var enemies = m_enemySpawner.ActiveEnemies;
                foreach (var enemy in enemies)
                {
                    // Only kill enemies that are alive (not spawning, not already dead)
                    if (enemy != null && enemy.IsAlive)
                    {
                        enemy.Kill();
                        killedCount++;
                    }
                }
            }
            else
            {
                // Fallback to FindObjectsByType if spawner not found
                var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
                foreach (var enemy in enemies)
                {
                    if (enemy.IsAlive)
                    {
                        enemy.Kill();
                        killedCount++;
                    }
                }
            }

            Debug.Log($"[SmartBombSystem] Killed {killedCount} enemies!");
        }

        /// <summary>
        /// Add a smart bomb (from pickup or reward)
        /// </summary>
        public void AddBomb()
        {
            if (m_currentBombs >= MaxBombs)
            {
                Debug.LogWarning($"[SmartBombSystem] Already at max bombs ({MaxBombs})!");
                return;
            }

            m_currentBombs++;
            EventBus.Publish(new SmartBombCountChangedEvent
            {
                count = m_currentBombs,
                maxCount = MaxBombs
            });

            Debug.Log($"[SmartBombSystem] Smart bomb added! Total: {m_currentBombs}/{MaxBombs}");
        }

        /// <summary>
        /// Reset bombs for new game
        /// </summary>
        public void Reset()
        {
            m_currentBombs = m_startingBombs;
            m_isActivating = false;
            m_activationTimer = 0f;

            EventBus.Publish(new SmartBombCountChangedEvent
            {
                count = m_currentBombs,
                maxCount = MaxBombs
            });
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            Reset();
        }

        private void SetupExplosionParticles()
        {
            var main = m_explosionParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(m_explosionStartColor, m_explosionEndColor);
            main.startLifetime = 1f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 20f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            main.maxParticles = 500;
            main.loop = false;

            var emission = m_explosionParticles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 500)
            });

            var shape = m_explosionParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 15f; // Cover full screen

            var colorOverLifetime = m_explosionParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;

            var sizeOverLifetime = m_explosionParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
        }

        [ContextMenu("Debug: Activate Smart Bomb")]
        private void DebugActivateBomb()
        {
            TryActivateBomb();
        }

        [ContextMenu("Debug: Add Smart Bomb")]
        private void DebugAddBomb()
        {
            AddBomb();
        }
    }
}
