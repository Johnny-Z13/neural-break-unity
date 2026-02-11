using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Entities;
using NeuralBreak.Config;
using Z13.Core;

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
        [SerializeField] private Transform m_target;
        [SerializeField] private Vector3 m_offset = new Vector3(0, 0, -10);

        [Header("Follow Settings")]
        [SerializeField] private float m_followSpeed = 5f;
        [SerializeField] private float m_deadZone = 0.5f;

        [Header("Dynamic Zoom")]
        [SerializeField] private bool m_enableDynamicZoom = true;
        [Tooltip("Minimum zoom - zoomed IN, tight view")]
        [SerializeField] private float m_minZoom = 5f;
        [Tooltip("Maximum zoom - zoomed OUT for many enemies")]
        [SerializeField] private float m_maxZoom = 18f;
        [Tooltip("Base/default camera size - starts here with 0 enemies")]
        [SerializeField] private float m_baseSize = 8f;
        [Tooltip("How fast zoom changes")]
        [SerializeField] private float m_zoomSpeed = 4f;
        [Tooltip("Enemy count for max intensity")]
        [SerializeField] private int m_maxEnemiesForZoom = 15;
        [Tooltip("Combo count for max intensity")]
        [SerializeField] private int m_maxComboForZoom = 10;

        [Header("Screen Shake")]
        [SerializeField] private float m_shakeDecay = 0.85f;
        [SerializeField] private float m_maxShakeOffset = 1.5f;
        [SerializeField] private float m_shakeMultiplier = 1.5f;
        [SerializeField] private bool m_shakeEnabled = true;

        // Note: MMFeedbacks removed

        // Components
        private Camera m_camera;

        // Shake state
        private float m_shakeIntensity;
        private float m_shakeDuration;
        private float m_shakeTimer;
        private Vector3 m_shakeOffset;

        // Cached for zero-allocation updates
        private Vector3 m_cachedShakeOffset;

        // Dynamic zoom state (from TypeScript)
        private float m_targetSize;
        private float m_currentSize;
        private float m_gameplayIntensity;
        private int m_enemyCount;
        private int m_comboCount;

        // Throttle timer for zoom calculations (TS: 50ms interval)
        private float m_zoomThrottleTimer;
        private const float ZOOM_THROTTLE_INTERVAL = 0.05f;

        // Public accessors
        public Camera Camera => m_camera;
        public float CurrentSize => m_camera != null ? m_camera.orthographicSize : m_baseSize;
        public float GameplayIntensity => m_gameplayIntensity;

        private void Awake()
        {
            m_camera = GetComponent<Camera>();
            if (m_camera == null)
            {
                m_camera = Camera.main;
            }

            // Initialize at base size (comfortable starting view)
            m_targetSize = m_baseSize;
            m_currentSize = m_baseSize;
            m_enemyCount = 0;
            m_comboCount = 0;
            m_gameplayIntensity = 0f;

            if (m_camera != null)
            {
                m_camera.orthographic = true;
                m_camera.orthographicSize = m_baseSize;
                Debug.Log($"[CameraController] Initialized at base size: {m_baseSize}");
            }
        }

        private void Start()
        {
            // Auto-find player target if not assigned
            if (m_target == null)
            {
                var player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    m_target = player.transform;
                    Debug.Log("[CameraController] Auto-found player target");
                }
            }

            // Subscribe to events
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<EnemySpawnedEvent>(OnEnemySpawned);
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);

            // Load shake setting from PlayerPrefs
            m_shakeEnabled = PlayerPrefs.GetInt("NeuralBreak_ScreenShake", 1) == 1;

            // Load config values if available
            LoadConfigValues();
        }

        private void LoadConfigValues()
        {
            var feedback = ConfigProvider.Balance?.feedback;
            if (feedback != null)
            {
                m_minZoom = feedback.minZoom;
                m_maxZoom = feedback.maxZoom;
                m_zoomSpeed = feedback.zoomSpeed;
                m_baseSize = feedback.baseZoom;

                // Also update current values to match
                m_targetSize = m_baseSize;
                m_currentSize = m_baseSize;
                if (m_camera != null)
                {
                    m_camera.orthographicSize = m_baseSize;
                }

                Debug.Log($"[CameraController] Loaded config: base={m_baseSize}, min={m_minZoom}, max={m_maxZoom}");
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
            if (m_target == null) return;

            Vector3 targetPos = m_target.position + m_offset;

            // Apply dead zone
            Vector3 diff = targetPos - transform.position;
            diff.z = 0; // Keep z offset constant

            if (diff.magnitude > m_deadZone)
            {
                // Smooth follow
                Vector3 newPos = Vector3.Lerp(
                    transform.position,
                    targetPos,
                    m_followSpeed * Time.deltaTime
                );

                // Apply shake offset
                transform.position = newPos + m_shakeOffset;
            }
        }

        /// <summary>
        /// Set the follow target
        /// </summary>
        public void SetTarget(Transform target)
        {
            m_target = target;
        }

        /// <summary>
        /// Snap camera to target immediately
        /// </summary>
        public void SnapToTarget()
        {
            if (m_target != null)
            {
                transform.position = m_target.position + m_offset;
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
            if (!m_enableDynamicZoom || m_camera == null) return;

            // Throttle intensity calculations (TS: every 50ms)
            m_zoomThrottleTimer += Time.deltaTime;

            if (m_zoomThrottleTimer >= ZOOM_THROTTLE_INTERVAL)
            {
                m_zoomThrottleTimer = 0f;

                // Calculate gameplay intensity (0-1)
                // Based on: enemy count, combo count
                float enemyIntensity = Mathf.Min((float)m_enemyCount / m_maxEnemiesForZoom, 1f);
                float comboIntensity = Mathf.Min((float)m_comboCount / m_maxComboForZoom, 1f);

                // Combined intensity - higher = more zoomed out
                // TS: Math.max(audioIntensity, enemyIntensity, comboIntensity * 0.7)
                m_gameplayIntensity = Mathf.Max(enemyIntensity, comboIntensity * 0.7f);

                // Calculate target zoom:
                // - At 0 intensity: m_baseSize (22)
                // - At 1 intensity: m_maxZoom (38.5)
                // This zooms OUT as more enemies appear
                float zoomRange = m_maxZoom - m_baseSize;
                m_targetSize = m_baseSize + (m_gameplayIntensity * zoomRange);

                // Clamp to valid range
                m_targetSize = Mathf.Clamp(m_targetSize, m_minZoom, m_maxZoom);
            }

            // Always lerp to target for smooth animation
            float previousSize = m_currentSize;
            m_currentSize = Mathf.Lerp(m_currentSize, m_targetSize, m_zoomSpeed * Time.deltaTime);

            // Only update camera if zoom actually changed
            if (Mathf.Abs(m_currentSize - previousSize) > 0.001f)
            {
                m_camera.orthographicSize = m_currentSize;
            }
        }

        /// <summary>
        /// Set gameplay data for zoom calculation (called by game systems)
        /// </summary>
        public void SetGameplayData(int enemyCount, int comboCount)
        {
            m_enemyCount = enemyCount;
            m_comboCount = comboCount;
        }

        /// <summary>
        /// Override zoom size temporarily
        /// </summary>
        public void SetZoom(float size, bool instant = false)
        {
            m_targetSize = Mathf.Clamp(size, m_minZoom, m_maxZoom);

            if (instant && m_camera != null)
            {
                m_currentSize = m_targetSize;
                m_camera.orthographicSize = m_targetSize;
            }
        }

        /// <summary>
        /// Reset to base zoom
        /// </summary>
        public void ResetZoom()
        {
            m_targetSize = m_baseSize;
            m_enemyCount = 0;
            m_comboCount = 0;
            m_gameplayIntensity = 0f;
        }

        #endregion

        #region Shake

        /// <summary>
        /// Enable or disable screen shake
        /// </summary>
        public void SetShakeEnabled(bool enabled)
        {
            m_shakeEnabled = enabled;
            PlayerPrefs.SetInt("NeuralBreak_ScreenShake", enabled ? 1 : 0);

            if (!enabled)
            {
                // Clear any active shake
                m_shakeOffset = Vector3.zero;
                m_shakeIntensity = 0f;
                m_shakeTimer = 0f;
            }
        }

        /// <summary>
        /// Check if screen shake is enabled
        /// </summary>
        public bool IsShakeEnabled => m_shakeEnabled;

        /// <summary>
        /// Trigger screen shake
        /// </summary>
        public void Shake(float intensity, float duration)
        {
            if (!m_shakeEnabled) return;

            // Apply shake multiplier for punchier feel
            float boostedIntensity = intensity * m_shakeMultiplier;

            // Manual shake - accumulate intensity
            m_shakeIntensity = Mathf.Max(m_shakeIntensity, boostedIntensity);
            m_shakeDuration = Mathf.Max(m_shakeDuration, duration);
            m_shakeTimer = m_shakeDuration;
        }

        private void UpdateShake()
        {
            if (m_shakeTimer > 0)
            {
                m_shakeTimer -= Time.deltaTime;

                // Calculate shake offset using Perlin noise for smooth shake
                float x = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f;

                float currentIntensity = m_shakeIntensity * (m_shakeTimer / m_shakeDuration);

                // Zero-allocation: use cached Vector3 and Set() method
                float offsetMagnitude = currentIntensity * m_maxShakeOffset;
                m_cachedShakeOffset.Set(x * offsetMagnitude, y * offsetMagnitude, 0f);
                m_shakeOffset = m_cachedShakeOffset;

                // Decay (TS: SHAKE_DECAY = 0.9)
                m_shakeIntensity *= m_shakeDecay;
            }
            else
            {
                m_shakeOffset = Vector3.zero;
                m_shakeIntensity = 0f;
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
            m_enemyCount = Mathf.Max(0, m_enemyCount - 1);

            // Shake intensity varies by enemy type (from TS CollisionSystem)
            float intensity;
            float duration;

            switch (evt.enemyType)
            {
                case EnemyType.Boss:
                    intensity = 1.0f;
                    duration = 0.4f;
                    break;
                case EnemyType.VoidSphere:
                    intensity = 0.7f;
                    duration = 0.3f;
                    break;
                case EnemyType.ChaosWorm:
                    intensity = 0.8f; // Bigger shake for worm!
                    duration = 0.35f;
                    break;
                case EnemyType.CrystalShard:
                    intensity = 0.5f;
                    duration = 0.25f;
                    break;
                case EnemyType.UFO:
                case EnemyType.ScanDrone:
                    intensity = 0.35f;
                    duration = 0.18f;
                    break;
                case EnemyType.Fizzer:
                    intensity = 0.25f;
                    duration = 0.12f;
                    break;
                default: // DataMite
                    intensity = 0.15f;
                    duration = 0.1f;
                    break;
            }

            Shake(intensity, duration);
        }

        private void OnEnemySpawned(EnemySpawnedEvent evt)
        {
            // Increment enemy count for zoom calculation
            m_enemyCount++;
        }

        private void OnComboChanged(ComboChangedEvent evt)
        {
            m_comboCount = evt.comboCount;
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
            m_enemyCount = 20;
            Debug.Log($"[CameraController] Simulating {m_enemyCount} enemies - intensity should increase");
        }

        [ContextMenu("Reset Enemy Count")]
        private void ResetEnemyCount()
        {
            m_enemyCount = 0;
            Debug.Log("[CameraController] Enemy count reset");
        }

        private void OnDrawGizmosSelected()
        {
            // Draw zoom range info
            Gizmos.color = Color.cyan;
            Vector3 pos = transform.position;
            pos.z = 0;

            // Min zoom circle
            Gizmos.DrawWireSphere(pos, m_minZoom);

            // Max zoom circle
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pos, m_maxZoom);
        }

        #endregion
    }
}
