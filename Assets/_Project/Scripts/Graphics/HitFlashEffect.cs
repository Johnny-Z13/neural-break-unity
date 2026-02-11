using UnityEngine;
using System.Collections;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Flashes a sprite white briefly when hit.
    /// Attach to any entity with a SpriteRenderer.
    /// </summary>
    public class HitFlashEffect : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float m_flashDuration = 0.08f;
        [SerializeField] private Color m_flashColor = Color.white;
        [SerializeField] private bool m_useUnscaledTime = true;

        private SpriteRenderer m_spriteRenderer;
        private Color m_originalColor;
        private Coroutine m_flashCoroutine;
        private Material m_flashMaterial;
        private Material m_originalMaterial;

        private static readonly int FlashColorProperty = Shader.PropertyToID("_FlashColor");
        private static readonly int FlashAmountProperty = Shader.PropertyToID("_FlashAmount");

        private void Awake()
        {
            m_spriteRenderer = GetComponent<SpriteRenderer>();
            if (m_spriteRenderer != null)
            {
                m_originalColor = m_spriteRenderer.color;
                m_originalMaterial = m_spriteRenderer.material;
            }
        }

        /// <summary>
        /// Trigger a flash effect
        /// </summary>
        public void Flash()
        {
            if (m_spriteRenderer == null) return;

            if (m_flashCoroutine != null)
            {
                StopCoroutine(m_flashCoroutine);
            }
            m_flashCoroutine = StartCoroutine(FlashCoroutine());
        }

        /// <summary>
        /// Trigger a flash with custom color
        /// </summary>
        public void Flash(Color color)
        {
            if (m_spriteRenderer == null) return;

            if (m_flashCoroutine != null)
            {
                StopCoroutine(m_flashCoroutine);
            }
            m_flashCoroutine = StartCoroutine(FlashCoroutine(color));
        }

        private IEnumerator FlashCoroutine()
        {
            yield return FlashCoroutine(m_flashColor);
        }

        private IEnumerator FlashCoroutine(Color color)
        {
            // Store original color
            Color original = m_spriteRenderer.color;

            // Flash white
            m_spriteRenderer.color = color;

            // Wait
            if (m_useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(m_flashDuration);
            }
            else
            {
                yield return new WaitForSeconds(m_flashDuration);
            }

            // Restore
            m_spriteRenderer.color = original;
            m_flashCoroutine = null;
        }

        /// <summary>
        /// Reset to original color (for pooled objects)
        /// </summary>
        public void ResetColor()
        {
            if (m_flashCoroutine != null)
            {
                StopCoroutine(m_flashCoroutine);
                m_flashCoroutine = null;
            }

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
