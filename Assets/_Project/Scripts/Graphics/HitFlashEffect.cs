using UnityEngine;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Flashes a sprite white briefly when hit.
    /// Attach to any entity with a SpriteRenderer.
    /// Uses timer-based approach instead of coroutines for zero allocation.
    /// </summary>
    public class HitFlashEffect : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float m_flashDuration = 0.08f;
        [SerializeField] private Color m_flashColor = Color.white;
        [SerializeField] private bool m_useUnscaledTime = true;

        private SpriteRenderer m_spriteRenderer;
        private Color m_originalColor;

        // Timer-based flash (zero allocation - no coroutines)
        private bool m_isFlashing;
        private float m_flashEndTime;
        private Color m_storedOriginalColor;

        private void Awake()
        {
            m_spriteRenderer = GetComponent<SpriteRenderer>();
            if (m_spriteRenderer != null)
            {
                m_originalColor = m_spriteRenderer.color;
            }
        }

        private void Update()
        {
            if (!m_isFlashing) return;

            float currentTime = m_useUnscaledTime ? Time.unscaledTime : Time.time;
            if (currentTime >= m_flashEndTime)
            {
                // Restore color
                m_spriteRenderer.color = m_storedOriginalColor;
                m_isFlashing = false;
            }
        }

        /// <summary>
        /// Trigger a flash effect
        /// </summary>
        public void Flash()
        {
            if (m_spriteRenderer == null) return;

            // Store original color before flash (if not already flashing, use current color)
            if (!m_isFlashing)
            {
                m_storedOriginalColor = m_spriteRenderer.color;
            }

            // Flash white
            m_spriteRenderer.color = m_flashColor;

            // Set end time
            float currentTime = m_useUnscaledTime ? Time.unscaledTime : Time.time;
            m_flashEndTime = currentTime + m_flashDuration;
            m_isFlashing = true;
        }

        /// <summary>
        /// Trigger a flash with custom color
        /// </summary>
        public void Flash(Color color)
        {
            if (m_spriteRenderer == null) return;

            // Store original color before flash (if not already flashing, use current color)
            if (!m_isFlashing)
            {
                m_storedOriginalColor = m_spriteRenderer.color;
            }

            // Flash with custom color
            m_spriteRenderer.color = color;

            // Set end time
            float currentTime = m_useUnscaledTime ? Time.unscaledTime : Time.time;
            m_flashEndTime = currentTime + m_flashDuration;
            m_isFlashing = true;
        }

        /// <summary>
        /// Reset to original color (for pooled objects)
        /// </summary>
        public void ResetColor()
        {
            m_isFlashing = false;

            if (m_spriteRenderer != null)
            {
                m_spriteRenderer.color = m_originalColor;
            }
        }

        /// <summary>
        /// Set the original color (for enemies that change color on state change)
        /// </summary>
        public void SetOriginalColor(Color color)
        {
            m_originalColor = color;
        }
    }
}
