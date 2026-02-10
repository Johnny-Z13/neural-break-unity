using UnityEngine;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Handles player ship visual feedback:
    /// - RED flash when hit
    /// - BLUE glow when shields active
    /// - Normal color when no shields
    /// Provides clear visual feedback for player state.
    /// </summary>
    public class PlayerVisuals : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color m_normalColor = new Color(0.3f, 0.8f, 1f); // Cyan ship
        [SerializeField] private Color m_shieldColor = new Color(0.2f, 0.5f, 1f); // Blue glow with shields
        [SerializeField] private Color m_damageColor = Color.red; // Red flash when hit

        [Header("Timing")]
        [SerializeField] private float m_damageFashDuration = 0.4f;
        [SerializeField] private float m_damageFlashSpeed = 15f;

        [Header("Shield Glow")]
        [SerializeField] private float m_shieldGlowIntensity = 1.3f; // Brightness multiplier
        [SerializeField] private float m_shieldPulseSpeed = 2f;
        [SerializeField] private float m_shieldPulseAmount = 0.2f;

        // Components
        private SpriteRenderer m_spriteRenderer;
        private PlayerHealth m_playerHealth;

        // State
        private bool m_isFlashing;
        private float m_flashTimer;
        private bool m_hasShields;
        private float m_shieldPulseTime;

        private void Awake()
        {
            m_spriteRenderer = GetComponent<SpriteRenderer>();
            m_playerHealth = GetComponent<PlayerHealth>();

            if (m_spriteRenderer == null)
            {
                Debug.LogError("[PlayerVisuals] No SpriteRenderer found! Visual feedback will not work.");
            }

            if (m_playerHealth == null)
            {
                Debug.LogError("[PlayerVisuals] No PlayerHealth found! Cannot track shield state.");
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<ShieldChangedEvent>(OnShieldChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<ShieldChangedEvent>(OnShieldChanged);
        }

        private void Start()
        {
            // Initialize color based on current shield state
            UpdateShieldVisual();
        }

        private void Update()
        {
            // Handle damage flash
            if (m_isFlashing)
            {
                m_flashTimer += Time.deltaTime;

                // Flash red rapidly
                float t = Mathf.PingPong(m_flashTimer * m_damageFlashSpeed, 1f);
                Color targetColor = m_hasShields ? m_shieldColor : m_normalColor;
                m_spriteRenderer.color = Color.Lerp(m_damageColor, targetColor, t);

                // End flash after duration
                if (m_flashTimer >= m_damageFashDuration)
                {
                    m_isFlashing = false;
                    UpdateShieldVisual();
                }
            }
            else if (m_hasShields)
            {
                // Gentle pulsing blue glow when shields active
                m_shieldPulseTime += Time.deltaTime * m_shieldPulseSpeed;
                float pulse = 1f + Mathf.Sin(m_shieldPulseTime) * m_shieldPulseAmount;
                Color glowColor = m_shieldColor * m_shieldGlowIntensity * pulse;
                glowColor.a = 1f; // Keep alpha at 1
                m_spriteRenderer.color = glowColor;
            }
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            // Start red flash
            m_isFlashing = true;
            m_flashTimer = 0f;
            m_spriteRenderer.color = m_damageColor;

            LogHelper.Log("[PlayerVisuals] Player hit! Flashing red.");
        }

        private void OnShieldChanged(ShieldChangedEvent evt)
        {
            m_hasShields = evt.currentShields > 0;

            // Update visual immediately if not flashing
            if (!m_isFlashing)
            {
                UpdateShieldVisual();
            }

            LogHelper.Log($"[PlayerVisuals] Shields changed: {evt.currentShields}/{evt.maxShields}. Blue glow: {m_hasShields}");
        }

        private void UpdateShieldVisual()
        {
            if (m_spriteRenderer == null) return;

            if (m_hasShields)
            {
                // Blue glow when shields active
                m_spriteRenderer.color = m_shieldColor * m_shieldGlowIntensity;
                m_shieldPulseTime = 0f; // Reset pulse
            }
            else
            {
                // Normal cyan color when no shields
                m_spriteRenderer.color = m_normalColor;
            }
        }

        #region Debug

        [ContextMenu("Debug: Flash Red")]
        private void DebugFlashRed()
        {
            m_isFlashing = true;
            m_flashTimer = 0f;
        }

        [ContextMenu("Debug: Toggle Shields")]
        private void DebugToggleShields()
        {
            m_hasShields = !m_hasShields;
            UpdateShieldVisual();
        }

        #endregion
    }
}
