using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Entities;
using MoreMountains.Feedbacks;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Orthographic camera controller with dynamic zoom and shake.
    /// Based on TypeScript SceneManager.ts camera system.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0, 0, -10);

        [Header("Follow Settings")]
        [SerializeField] private float _followSpeed = 5f;
        [SerializeField] private float _deadZone = 0.5f;

        [Header("Orthographic Settings")]
        [SerializeField] private float _baseSize = 15f;
        [SerializeField] private float _minSize = 12f;
        [SerializeField] private float _maxSize = 22.5f;

        [Header("Dynamic Zoom")]
        [SerializeField] private bool _enableDynamicZoom = true;
        [SerializeField] private float _zoomSpeed = 3f;
        [SerializeField] private float _zoomIntensityFactor = 0.1f;

        [Header("Screen Shake")]
        [SerializeField] private float _shakeDecay = 0.9f;
        [SerializeField] private float _maxShakeOffset = 1f;
        [SerializeField] private bool _shakeEnabled = true;

        [Header("Feel Feedbacks")]
        [SerializeField] private MMF_Player _impactShakeFeedback;

        // Components
        private Camera _camera;

        // Shake state
        private float _shakeIntensity;
        private float _shakeDuration;
        private float _shakeTimer;
        private Vector3 _shakeOffset;

        // Zoom state
        private float _targetSize;
        private float _currentIntensity;

        // Public accessors
        public Camera Camera => _camera;
        public float CurrentSize => _camera.orthographicSize;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            _targetSize = _baseSize;

            if (_camera != null)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = _baseSize;
            }
        }

        private void Start()
        {
            // Subscribe to events that should trigger shake
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);

            // Load shake setting from PlayerPrefs
            _shakeEnabled = PlayerPrefs.GetInt("NeuralBreak_ScreenShake", 1) == 1;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void LateUpdate()
        {
            UpdateFollow();
            UpdateZoom();
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

        #region Zoom

        private void UpdateZoom()
        {
            if (!_enableDynamicZoom || _camera == null) return;

            // Calculate target size based on gameplay intensity
            float intensityZoom = _currentIntensity * _zoomIntensityFactor * (_maxSize - _baseSize);
            _targetSize = Mathf.Clamp(_baseSize + intensityZoom, _minSize, _maxSize);

            // Smooth zoom
            _camera.orthographicSize = Mathf.Lerp(
                _camera.orthographicSize,
                _targetSize,
                _zoomSpeed * Time.deltaTime
            );
        }

        /// <summary>
        /// Set gameplay intensity for dynamic zoom (0-1)
        /// </summary>
        public void SetIntensity(float intensity)
        {
            _currentIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// Override zoom size temporarily
        /// </summary>
        public void SetZoom(float size, bool instant = false)
        {
            _targetSize = Mathf.Clamp(size, _minSize, _maxSize);

            if (instant && _camera != null)
            {
                _camera.orthographicSize = _targetSize;
            }
        }

        /// <summary>
        /// Reset to base zoom
        /// </summary>
        public void ResetZoom()
        {
            _targetSize = _baseSize;
        }

        #endregion

        #region Shake

        /// <summary>
        /// Enable or disable screen shake
        /// </summary>
        public void SetShakeEnabled(bool enabled)
        {
            _shakeEnabled = enabled;
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

            // Use Feel feedback if available
            if (_impactShakeFeedback != null)
            {
                _impactShakeFeedback.PlayFeedbacks();
                return;
            }

            // Manual shake fallback
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
                float x = (Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(0f, Time.time * 20f) - 0.5f) * 2f;

                float currentIntensity = _shakeIntensity * (_shakeTimer / _shakeDuration);
                _shakeOffset = new Vector3(x, y, 0) * currentIntensity * _maxShakeOffset;

                // Decay
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
            // Shake on player damage
            Shake(0.3f, 0.2f);
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            // Shake intensity varies by enemy type
            float intensity;
            float duration;

            switch (evt.enemyType)
            {
                case EnemyType.Boss:
                    intensity = 0.6f;
                    duration = 0.5f;
                    break;
                case EnemyType.VoidSphere:
                    intensity = 0.25f;
                    duration = 0.3f;
                    break;
                case EnemyType.ChaosWorm:
                    intensity = 0.2f;
                    duration = 0.25f;
                    break;
                case EnemyType.UFO:
                case EnemyType.ScanDrone:
                    intensity = 0.1f;
                    duration = 0.15f;
                    break;
                default:
                    intensity = 0.06f;
                    duration = 0.1f;
                    break;
            }

            Shake(intensity, duration);
        }

        #endregion

        #region Debug

        [ContextMenu("Test Small Shake")]
        private void TestSmallShake() => Shake(0.15f, 0.2f);

        [ContextMenu("Test Big Shake")]
        private void TestBigShake() => Shake(0.5f, 0.4f);

        #endregion
    }
}
