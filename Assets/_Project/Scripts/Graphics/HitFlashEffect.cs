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
        [SerializeField] private float _flashDuration = 0.08f;
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private bool _useUnscaledTime = true;

        private SpriteRenderer _spriteRenderer;
        private Color _originalColor;
        private Coroutine _flashCoroutine;
        private Material _flashMaterial;
        private Material _originalMaterial;

        private static readonly int FlashColorProperty = Shader.PropertyToID("_FlashColor");
        private static readonly int FlashAmountProperty = Shader.PropertyToID("_FlashAmount");

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
                _originalMaterial = _spriteRenderer.material;
            }
        }

        /// <summary>
        /// Trigger a flash effect
        /// </summary>
        public void Flash()
        {
            if (_spriteRenderer == null) return;

            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }
            _flashCoroutine = StartCoroutine(FlashCoroutine());
        }

        /// <summary>
        /// Trigger a flash with custom color
        /// </summary>
        public void Flash(Color color)
        {
            if (_spriteRenderer == null) return;

            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }
            _flashCoroutine = StartCoroutine(FlashCoroutine(color));
        }

        private IEnumerator FlashCoroutine()
        {
            yield return FlashCoroutine(_flashColor);
        }

        private IEnumerator FlashCoroutine(Color color)
        {
            // Store original color
            Color original = _spriteRenderer.color;

            // Flash white
            _spriteRenderer.color = color;

            // Wait
            if (_useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(_flashDuration);
            }
            else
            {
                yield return new WaitForSeconds(_flashDuration);
            }

            // Restore
            _spriteRenderer.color = original;
            _flashCoroutine = null;
        }

        /// <summary>
        /// Reset to original color (for pooled objects)
        /// </summary>
        public void ResetColor()
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
            }
        }

        /// <summary>
        /// Set the original color (for enemies that change color on state change)
        /// </summary>
        public void SetOriginalColor(Color color)
        {
            _originalColor = color;
        }
    }
}
