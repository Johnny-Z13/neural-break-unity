using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Entities;
using NeuralBreak.Input;
using NeuralBreak.Config;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Smart Bomb system - screen-clearing super weapon.
    /// Kills all enemies on screen with epic full-screen VFX and camera shake.
    /// Player starts with 1, max 3.
    /// </summary>
    public class SmartBombSystem : MonoBehaviour
    {
        [Header("Smart Bomb Settings")]
        [SerializeField] private int _startingBombs = 1;
        [SerializeField] private int _maxBombs = 3;
        [SerializeField] private float _activationDuration = 0.5f;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _explosionParticles;
        [SerializeField] private Color _explosionStartColor = new Color(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color _explosionEndColor = new Color(1f, 0.3f, 0.1f, 0f);

        // Note: MMFeedbacks removed

        [Header("Audio")]
        [SerializeField] private AudioClip _epicExplosionSound;
        [SerializeField] private float _explosionVolume = 0.8f;

        // State
        private int _currentBombs;
        private bool _isActivating;
        private float _activationTimer;
        private AudioSource _audioSource;
        private int _bonusBombs;

        // Public accessors
        public int CurrentBombs => _currentBombs;
        public int MaxBombs => _maxBombs + _bonusBombs;
        public bool CanUseBomb => _currentBombs > 0 && !_isActivating;

        private void Awake()
        {
            _currentBombs = _startingBombs;

            // Create audio source
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D sound
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
                count = _currentBombs,
                maxCount = MaxBombs
            });

            // Ensure particles are set up
            if (_explosionParticles != null)
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
            int bombDelta = evt.modifiers.bonusSmartBombs - _bonusBombs;
            if (bombDelta != 0)
            {
                _bonusBombs = evt.modifiers.bonusSmartBombs;

                // If bonus increased, add bombs
                if (bombDelta > 0)
                {
                    _currentBombs = Mathf.Min(_currentBombs + bombDelta, MaxBombs);
                }

                EventBus.Publish(new SmartBombCountChangedEvent
                {
                    count = _currentBombs,
                    maxCount = MaxBombs
                });

                Debug.Log($"[SmartBombSystem] Bomb bonus changed: +{_bonusBombs}. Max bombs: {MaxBombs}");
            }
        }

        private void Update()
        {
            if (_isActivating)
            {
                _activationTimer -= Time.deltaTime;
                if (_activationTimer <= 0f)
                {
                    _isActivating = false;
                }
            }
        }

        /// <summary>
        /// Try to activate a smart bomb
        /// </summary>
        public void TryActivateBomb()
        {
            Debug.Log($"[SmartBombSystem] TryActivateBomb called! Bombs: {_currentBombs}, IsActivating: {_isActivating}, CanUseBomb: {CanUseBomb}");

            if (!CanUseBomb)
            {
                Debug.LogWarning("[SmartBombSystem] Cannot activate - no bombs available or already activating!");
                return;
            }

            ActivateBomb();
        }

        private void ActivateBomb()
        {
            _currentBombs--;
            _isActivating = true;
            _activationTimer = _activationDuration;

            // Epic audio
            if (_epicExplosionSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_epicExplosionSound, _explosionVolume);
            }

            // Feedback (Feel removed)

            // Visual effect
            if (_explosionParticles != null)
            {
                _explosionParticles.Play();
            }

            // Kill all enemies
            KillAllEnemies();

            // Publish event
            EventBus.Publish(new SmartBombActivatedEvent { position = transform.position });
            EventBus.Publish(new SmartBombCountChangedEvent
            {
                count = _currentBombs,
                maxCount = MaxBombs
            });

            Debug.Log($"[SmartBombSystem] SMART BOMB ACTIVATED! Bombs remaining: {_currentBombs}");
        }

        private void KillAllEnemies()
        {
            // Find all enemies in the scene (Unity 6 API)
            var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            int killedCount = 0;

            foreach (var enemy in enemies)
            {
                // Only kill enemies that are alive (not spawning, not already dead)
                if (enemy.IsAlive)
                {
                    enemy.Kill();
                    killedCount++;
                }
            }

            Debug.Log($"[SmartBombSystem] Killed {killedCount} enemies!");
        }

        /// <summary>
        /// Add a smart bomb (from pickup or reward)
        /// </summary>
        public void AddBomb()
        {
            if (_currentBombs >= MaxBombs)
            {
                Debug.LogWarning($"[SmartBombSystem] Already at max bombs ({MaxBombs})!");
                return;
            }

            _currentBombs++;
            EventBus.Publish(new SmartBombCountChangedEvent
            {
                count = _currentBombs,
                maxCount = MaxBombs
            });

            Debug.Log($"[SmartBombSystem] Smart bomb added! Total: {_currentBombs}/{MaxBombs}");
        }

        /// <summary>
        /// Reset bombs for new game
        /// </summary>
        public void Reset()
        {
            _currentBombs = _startingBombs;
            _isActivating = false;
            _activationTimer = 0f;

            EventBus.Publish(new SmartBombCountChangedEvent
            {
                count = _currentBombs,
                maxCount = MaxBombs
            });
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            Reset();
        }

        private void SetupExplosionParticles()
        {
            var main = _explosionParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(_explosionStartColor, _explosionEndColor);
            main.startLifetime = 1f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 20f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            main.maxParticles = 500;
            main.loop = false;

            var emission = _explosionParticles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 500)
            });

            var shape = _explosionParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 15f; // Cover full screen

            var colorOverLifetime = _explosionParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;

            var sizeOverLifetime = _explosionParticles.sizeOverLifetime;
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
