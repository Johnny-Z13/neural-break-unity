using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Entities;
using NeuralBreak.Config;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Orthographic camera controller with dynamic zoom and shake.
    /// Based on TypeScript SceneManager.ts camera system.
    /// 
    /// Dynamic Zoom Logic (from TS):
    /// - Tracks enemy count and combo count
    /// - Calculates intensity: max(audioIntensity, enemyIntensity, comboIntensity * 0.7)
    /// - enemyIntensity = min(enemyCount / 20, 1) - max at 20 enemies
    /// - comboIntensity = min(comboCount / 15, 1) - max at 15 combo
    /// - Zooms OUT as intensity increases (more enemies = wider view)
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0, 0, -10);

        [Header("Follow Settings")]
        [SerializeField] private float _followSpeed = 5f;
        [SerializeField] private float _deadZone = 0.5f;

        [Header("Dynamic Zoom (from TypeScript visual.config.ts)")]
        [SerializeField] private bool _enableDynamicZoom = true;
        [Tooltip("Minimum zoom - zoomed IN, tight view (TS: MIN_ZOOM = 16.5)")]
        [SerializeField] private float _minZoom = 16.5f;
        [Tooltip("Maximum zoom - zoomed OUT for many enemies (TS: MAX_ZOOM = 38.5)")]
        [SerializeField] private float _maxZoom = 38.5f;
        [Tooltip("Base/default camera size - starts here with 0 enemies (TS: BASE_FRUSTUM_SIZE = 22)")]
        [SerializeField] private float _baseSize = 22f;
        [Tooltip("How fast zoom changes (TS: zoomLerpSpeed = 3.0)")]
        [SerializeField] private float _zoomSpeed = 3f;
        [Tooltip("Enemy count for max intensity (TS: 20)")]
        [SerializeField] private int _maxEnemiesForZoom = 20;
        [Tooltip("Combo count for max intensity (TS: 15)")]
        [SerializeField] private int _maxComboForZoom = 15;

        [Header("Screen Shake")]
        [SerializeField] private float _shakeDecay = 0.9f;
        [SerializeField] private float _maxShakeOffset = 1f;
        [SerializeField] private bool _shakeEnabled = true;

        // Note: MMFeedbacks removed

        // Components
        private Camera _camera;

        // Shake state
        private float _shakeIntensity;
        private float _shakeDuration;
        private float _shakeTimer;
        private Vector3 _shakeOffset;

        // Cached for zero-allocation updates
        private Vector3 _cachedShakeOffset;

        // Dynamic zoom state (from TypeScript)
        private float _targetSize;
        private float _currentSize;
        private float _gameplayIntensity;
        private int _enemyCount;
        private int _comboCount;

        // Throttle timer for zoom calculations (TS: 50ms interval)
        private float _zoomThrottleTimer;
        private const float ZOOM_THROTTLE_INTERVAL = 0.05f;

        // Public accessors
        public Camera Camera => _camera;
        public float CurrentSize => _camera != null ? _camera.orthographicSize : _baseSize;
        public float GameplayIntensity => _gameplayIntensity;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            // Initialize at base size (comfortable starting view)
            _targetSize = _baseSize;
            _currentSize = _baseSize;
            _enemyCount = 0;
            _comboCount = 0;
            _gameplayIntensity = 0f;

            if (_camera != null)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = _baseSize;
                Debug.Log($"[CameraController] Initialized at base size: {_baseSize}");
            }
        }

        private void Start()
        {
            // Auto-find player target if not assigned
            if (_target == null)
            {
                var player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    _target = player.transform;
                    Debug.Log("[CameraController] Auto-found player target");
                }
            }

            // Subscribe to events
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<EnemySpawnedEvent>(OnEnemySpawned);
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);

            // Load shake setting from PlayerPrefs
            _shakeEnabled = PlayerPrefs.GetInt("NeuralBreak_ScreenShake", 1) == 1;

            // Load config values if available
            LoadConfigValues();
        }

        private void LoadConfigValues()
        {
            var feedback = ConfigProvider.Balance?.feedback;
            if (feedback != null)
            {
                _minZoom = feedback.minZoom;
                _maxZoom = feedback.maxZoom;
                _zoomSpeed = feedback.zoomSpeed;
                _baseSize = feedback.baseZoom;
                
                // Also update current values to match
                _targetSize = _baseSize;
                _currentSize = _baseSize;
                if (_camera != null)
                {
                    _camera.orthographicSize = _baseSize;
                }
                
                Debug.Log($"[CameraController] Loaded config: base={_baseSize}, min={_minZoom}, max={_maxZoom}");
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<EnemySpawnedEvent>(OnEnemySpawned);
            EventBus.Unsubscribe<ComboChangedEvent>(OnComboChanged);
        }

        private void LateUpdate()
        {
            UpdateFollow();
            UpdateDynamicZoom();
            UpdateShake();
        }

        #region Follow

        private void UpdateFollow()
        {
            if (_target == null) return;

            Vector3 targetPos = _target.position + _offset;

            // Apply dead zone
            Vector3 diff = targetPos - transform.position;
            diff.z = 0; // Keep z offset constant

            if (diff.magnitude > _deadZone)
            {
                // Smooth follow
                Vector3 newPos = Vector3.Lerp(
                    transform.position,
                    targetPos,
                    _followSpeed * Time.deltaTime
                );

                // Apply shake offset
                transform.position = newPos + _shakeOffset;
            }
        }

        /// <summary>
        /// Set the follow target
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        /// <summary>
        /// Snap camera to target immediately
        /// </summary>
        public void SnapToTarget()
        {
            if (_target != null)
            {
                transform.position = _target.position + _offset;
            }
        }

        #endregion

        #region Dynamic Zoom (TypeScript Port)

        /// <summary>
        /// Update dynamic zoom based on gameplay intensity.
        /// Ported from TypeScript SceneManager.updateDynamicZoom()
        /// 
        /// Logic: Start at _baseSize (22), zoom OUT toward _maxZoom as enemies increase
        /// With 0 enemies: camera at _baseSize (22)
        /// With 20+ enemies: camera zooms toward _maxZoom (38.5)
        /// </summary>
        private void UpdateDynamicZoom()
        {
            if (!_enableDynamicZoom || _camera == null) return;

            // Throttle intensity calculations (TS: every 50ms)
            _zoomThrottleTimer += Time.deltaTime;

            if (_zoomThrottleTimer >= ZOOM_THROTTLE_INTERVAL)
            {
                _zoomThrottleTimer = 0f;

                // Calculate gameplay intensity (0-1)
                // Based on: enemy count, combo count
                float enemyIntensity = Mathf.Min((float)_enemyCount / _maxEnemiesForZoom, 1f);
                float comboIntensity = Mathf.Min((float)_comboCount / _maxComboForZoom, 1f);

                // Combined intensity - higher = more zoomed out
                // TS: Math.max(audioIntensity, enemyIntensity, comboIntensity * 0.7)
                _gameplayIntensity = Mathf.Max(enemyIntensity, comboIntensity * 0.7f);

                // Calculate target zoom:
                // - At 0 intensity: _baseSize (22)
                // - At 1 intensity: _maxZoom (38.5)
                // This zooms OUT as more enemies appear
                float zoomRange = _maxZoom - _baseSize;
                _targetSize = _baseSize + (_gameplayIntensity * zoomRange);
                
                // Clamp to valid range
                _targetSize = Mathf.Clamp(_targetSize, _minZoom, _maxZoom);
            }

            // Always lerp to target for smooth animation
            float previousSize = _currentSize;
            _currentSize = Mathf.Lerp(_currentSize, _targetSize, _zoomSpeed * Time.deltaTime);

            // Only update camera if zoom actually changed
            if (Mathf.Abs(_currentSize - previousSize) > 0.001f)
            {
                _camera.orthographicSize = _currentSize;
            }
        }

        /// <summary>
        /// Set gameplay data for zoom calculation (called by game systems)
        /// </summary>
        public void SetGameplayData(int enemyCount, int comboCount)
        {
            _enemyCount = enemyCount;
            _comboCount = comboCount;
        }

        /// <summary>
        /// Override zoom size temporarily
        /// </summary>
        public void SetZoom(float size, bool instant = false)
        {
            _targetSize = Mathf.Clamp(size, _minZoom, _maxZoom);

            if (instant && _camera != null)
            {
                _currentSize = _targetSize;
                _camera.orthographicSize = _targetSize;
            }
        }

        /// <summary>
        /// Reset to base zoom
        /// </summary>
        public void ResetZoom()
        {
            _targetSize = _baseSize;
            _enemyCount = 0;
            _comboCount = 0;
            _gameplayIntensity = 0f;
        }

        #endregion

        #region Shake

        /// <summary>
        /// Enable or disable screen shake
        /// </summary>
        public void SetShakeEnabled(bool enabled)
        {
            _shakeEnabled = enabled;
            PlayerPrefs.SetInt("NeuralBreak_ScreenShake", enabled ? 1 : 0);
            
            if (!enabled)
            {
                // Clear any active shake
                _shakeOffset = Vector3.zero;
                _shakeIntensity = 0f;
                _shakeTimer = 0f;
            }
        }

        /// <summary>
        /// Check if screen shake is enabled
        /// </summary>
        public bool IsShakeEnabled => _shakeEnabled;

        /// <summary>
        /// Trigger screen shake
        /// </summary>
        public void Shake(float intensity, float duration)
        {
            if (!_shakeEnabled) return;

            // Feedback (Feel removed)

            // Manual shake - accumulate intensity
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
            _shakeDuration = Mathf.Max(_shakeDuration, duration);
            _shakeTimer = _shakeDuration;
        }

        private void UpdateShake()
        {
            if (_shakeTimer > 0)
            {
                _shakeTimer -= Time.deltaTime;

                // Calculate shake offset using Perlin noise for smooth shake
                float x = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f;

                float currentIntensity = _shakeIntensity * (_shakeTimer / _shakeDuration);

                // Zero-allocation: use cached Vector3 and Set() method
                float offsetMagnitude = currentIntensity * _maxShakeOffset;
                _cachedShakeOffset.Set(x * offsetMagnitude, y * offsetMagnitude, 0f);
                _shakeOffset = _cachedShakeOffset;

                // Decay (TS: SHAKE_DECAY = 0.9)
                _shakeIntensity *= _shakeDecay;
            }
            else
            {
                _shakeOffset = Vector3.zero;
                _shakeIntensity = 0f;
            }
        }

        #endregion

        #region Event Handlers

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            // Shake on player damage (TS: 0.5, 0.2)
            Shake(0.5f, 0.2f);
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            // Decrement enemy count
            _enemyCount = Mathf.Max(0, _enemyCount - 1);

            // Shake intensity varies by enemy type (from TS CollisionSystem)
            float intensity;
            float duration;

            switch (evt.enemyType)
            {
                case EnemyType.Boss:
                    intensity = 0.6f;
                    duration = 0.3f;
                    break;
                case EnemyType.VoidSphere:
                    intensity = 0.4f;
                    duration = 0.25f;
                    break;
                case EnemyType.ChaosWorm:
                case EnemyType.CrystalShard:
                    intensity = 0.3f;
                    duration = 0.2f;
                    break;
                case EnemyType.UFO:
                case EnemyType.ScanDrone:
                    intensity = 0.2f;
                    duration = 0.15f;
                    break;
                case EnemyType.Fizzer:
                    intensity = 0.15f;
                    duration = 0.1f;
                    break;
                default: // DataMite
                    intensity = 0.1f;
                    duration = 0.08f;
                    break;
            }

            Shake(intensity, duration);
        }

        private void OnEnemySpawned(EnemySpawnedEvent evt)
        {
            // Increment enemy count for zoom calculation
            _enemyCount++;
        }

        private void OnComboChanged(ComboChangedEvent evt)
        {
            _comboCount = evt.comboCount;
        }

        #endregion

        #region Debug

        [ContextMenu("Test Small Shake")]
        private void TestSmallShake() => Shake(0.2f, 0.1f);

        [ContextMenu("Test Medium Shake")]
        private void TestMediumShake() => Shake(0.5f, 0.3f);

        [ContextMenu("Test Big Shake")]
        private void TestBigShake() => Shake(1.0f, 0.5f);

        [ContextMenu("Simulate 20 Enemies")]
        private void SimulateMaxEnemies()
        {
            _enemyCount = 20;
            Debug.Log($"[CameraController] Simulating {_enemyCount} enemies - intensity should increase");
        }

        [ContextMenu("Reset Enemy Count")]
        private void ResetEnemyCount()
        {
            _enemyCount = 0;
            Debug.Log("[CameraController] Enemy count reset");
        }

        private void OnDrawGizmosSelected()
        {
            // Draw zoom range info
            Gizmos.color = Color.cyan;
            Vector3 pos = transform.position;
            pos.z = 0;
            
            // Min zoom circle
            Gizmos.DrawWireSphere(pos, _minZoom);
            
            // Max zoom circle
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pos, _maxZoom);
        }

        #endregion
    }
}
