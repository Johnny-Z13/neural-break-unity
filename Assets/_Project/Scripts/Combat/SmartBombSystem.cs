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

        // Staggered kill state
        private Coroutine m_killSequenceCoroutine;

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

            // EPIC camera shake!
            var camera = FindFirstObjectByType<Graphics.CameraController>();
            if (camera != null)
            {
                camera.Shake(2.0f, 0.8f); // MASSIVE shake!
            }

            // Visual effect
            if (m_explosionParticles != null)
            {
                m_explosionParticles.Play();
            }

            // Create epic flash effect
            CreateEpicFlash();

            // Kill all enemies with staggered timing (0.5s total)
            if (m_killSequenceCoroutine != null)
            {
                StopCoroutine(m_killSequenceCoroutine);
            }
            m_killSequenceCoroutine = StartCoroutine(KillAllEnemiesStaggered());

            // Publish event
            EventBus.Publish(new SmartBombActivatedEvent { position = transform.position });
            EventBus.Publish(new SmartBombCountChangedEvent
            {
                count = m_currentBombs,
                maxCount = MaxBombs
            });

            Debug.Log($"[SmartBombSystem] SMART BOMB ACTIVATED! Bombs remaining: {m_currentBombs}");
        }

        private System.Collections.IEnumerator KillAllEnemiesStaggered()
        {
            // Use cached EnemySpawner.ActiveEnemies instead of FindObjectsByType
            if (m_enemySpawner == null)
                m_enemySpawner = FindFirstObjectByType<Entities.EnemySpawner>();

            var enemiesToKill = new System.Collections.Generic.List<EnemyBase>();

            if (m_enemySpawner != null)
            {
                // Collect alive enemies
                var enemies = m_enemySpawner.ActiveEnemies;
                foreach (var enemy in enemies)
                {
                    if (enemy != null && enemy.IsAlive)
                    {
                        enemiesToKill.Add(enemy);
                    }
                }
            }
            else
            {
                // Fallback to FindObjectsByType if spawner not found
                var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
                foreach (var enemy in enemies)
                {
                    if (enemy != null && enemy.IsAlive)
                    {
                        enemiesToKill.Add(enemy);
                    }
                }
            }

            int totalEnemies = enemiesToKill.Count;
            Debug.Log($"[SmartBombSystem] Starting staggered kill sequence: {totalEnemies} enemies");

            if (totalEnemies == 0)
            {
                m_killSequenceCoroutine = null;
                yield break;
            }

            // Shuffle for random death order
            for (int i = totalEnemies - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                var temp = enemiesToKill[i];
                enemiesToKill[i] = enemiesToKill[randomIndex];
                enemiesToKill[randomIndex] = temp;
            }

            // Calculate delay to fit all kills in 0.5s
            float totalDuration = 0.5f;
            float delayPerKill = totalEnemies > 1 ? totalDuration / (totalEnemies - 1) : 0f;

            // Kill enemies one by one
            foreach (var enemy in enemiesToKill)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.Kill();
                }
                yield return new WaitForSeconds(delayPerKill);
            }

            Debug.Log($"[SmartBombSystem] Staggered kill sequence complete!");
            m_killSequenceCoroutine = null;
        }

        private void CreateEpicFlash()
        {
            // Create massive screen flash
            var flashGO = new GameObject("SmartBombFlash");
            flashGO.transform.position = transform.position;

            var ps = flashGO.AddComponent<ParticleSystem>();

            // Stop the system first to ensure we can modify it
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = 0.3f;
            main.startSpeed = 0f;
            main.startSize = 50f; // HUGE - covers entire screen
            main.startColor = new Color(1f, 1f, 0.8f, 1f); // Bright white-yellow
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 1;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

            var shape = ps.shape;
            shape.enabled = false;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var curve = new AnimationCurve();
            curve.AddKey(0f, 1.5f);
            curve.AddKey(0.1f, 1f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0.3f),
                    new GradientColorKey(new Color(1f, 0.5f, 0f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.6f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            // Create material with proper texture and blending
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var mat = Graphics.VFX.VFXHelpers.CreateParticleMaterial(
                new Color(1f, 1f, 0.8f, 1f),
                emissionIntensity: 2f,
                additive: true // Additive for bright flash
            );

            if (mat != null)
            {
                mat.renderQueue = 4000; // Render on top
                renderer.material = mat;
                renderer.sortingOrder = 1000; // Very high
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }

            ps.Play();
            Destroy(flashGO, 1f);
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
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(15f, 30f); // MUCH faster!
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 2.5f); // BIGGER!
            main.maxParticles = 800; // MORE particles!
            main.loop = false;

            var emission = m_explosionParticles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 600), // First wave
                new ParticleSystem.Burst(0.1f, 200) // Second wave for extra impact
            });

            var shape = m_explosionParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 20f; // LARGER radius - full screen coverage!

            var colorOverLifetime = m_explosionParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 1f, 0.8f), 0f), // Bright white
                    new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.3f), // Yellow
                    new GradientColorKey(new Color(1f, 0.3f, 0f), 1f) // Orange-red
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = m_explosionParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));
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
