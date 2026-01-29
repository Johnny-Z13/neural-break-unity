using UnityEngine;
using UnityEngine.UI;
using NeuralBreak.Combat;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Handles all animations for upgrade cards.
    /// Includes glowing selection ring, hover effects, and procedural sounds.
    /// </summary>
    [RequireComponent(typeof(UpgradeCard))]
    public class UpgradeCardAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _entranceDelay = 0f;
        [SerializeField] private float _entranceDuration = 0.4f;
        [SerializeField] private float _hoverScale = 1.08f;
        [SerializeField] private float _hoverSpeed = 12f;
        [SerializeField] private float _selectPunchScale = 1.2f;
        [SerializeField] private float _selectDuration = 0.25f;

        [Header("Glow Settings")]
        [SerializeField] private float _glowPulseSpeed = 3f;
        [SerializeField] private float _glowIntensityMin = 0.4f;
        [SerializeField] private float _glowIntensityMax = 0.9f;

        [Header("References")]
        [SerializeField] private Image _background;
        [SerializeField] private CanvasGroup _canvasGroup;

        // Selection glow components
        private Transform _selectionGlow;
        private Image _glowImage;
        private Outline[] _glowOutlines;

        private UpgradeCard _card;
        private Vector3 _originalScale;
        private float _currentScale = 1f;
        private bool _isHovered = false;
        private bool _isAnimatingEntrance = false;
        private float _entranceTimer = 0f;
        private bool _isAnimatingSelect = false;
        private float _selectTimer = 0f;
        private float _glowTime = 0f;

        // Audio
        private AudioSource _audioSource;

        private void Awake()
        {
            _card = GetComponent<UpgradeCard>();
            EnsureCanvasGroup();
            FindSelectionGlow();
            EnsureAudioSource();

            _originalScale = transform.localScale;
            if (_originalScale == Vector3.zero)
            {
                _originalScale = Vector3.one;
            }
        }

        private void EnsureCanvasGroup()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void FindSelectionGlow()
        {
            _selectionGlow = transform.Find("SelectionGlow");
            if (_selectionGlow != null)
            {
                _glowImage = _selectionGlow.GetComponent<Image>();
                _glowOutlines = _selectionGlow.GetComponents<Outline>();
            }
        }

        private void EnsureAudioSource()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
            _audioSource.volume = 0.5f;
        }

        private void Update()
        {
            // Entrance animation
            if (_isAnimatingEntrance)
            {
                _entranceTimer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_entranceTimer / _entranceDuration);

                // Ease out back for bouncy feel
                t = 1f - Mathf.Pow(1f - t, 3f);

                if (_canvasGroup != null) _canvasGroup.alpha = t;
                _currentScale = Mathf.Lerp(0.7f, 1f, t);

                if (_entranceTimer >= _entranceDuration)
                {
                    _isAnimatingEntrance = false;
                    if (_canvasGroup != null) _canvasGroup.alpha = 1f;
                    _currentScale = 1f;
                }
            }

            // Selection punch animation
            if (_isAnimatingSelect)
            {
                _selectTimer += Time.unscaledDeltaTime;
                float t = _selectTimer / _selectDuration;

                if (t <= 0.4f)
                {
                    // Punch out fast
                    float punchT = t / 0.4f;
                    punchT = 1f - Mathf.Pow(1f - punchT, 2f); // Ease out
                    _currentScale = Mathf.Lerp(1f, _selectPunchScale, punchT);
                }
                else
                {
                    // Settle back
                    float punchT = (t - 0.4f) / 0.6f;
                    _currentScale = Mathf.Lerp(_selectPunchScale, 1f, punchT);
                }

                if (t >= 1f)
                {
                    _isAnimatingSelect = false;
                    _currentScale = 1f;
                }
            }

            // Hover scale
            if (!_isAnimatingSelect && !_isAnimatingEntrance)
            {
                float targetScale = _isHovered ? _hoverScale : 1f;
                _currentScale = Mathf.Lerp(_currentScale, targetScale, _hoverSpeed * Time.unscaledDeltaTime);
            }

            // Apply scale
            transform.localScale = _originalScale * _currentScale;

            // Update selection glow pulse
            if (_isHovered && _selectionGlow != null)
            {
                UpdateSelectionGlow();
            }
        }

        private void UpdateSelectionGlow()
        {
            _glowTime += Time.unscaledDeltaTime * _glowPulseSpeed;
            float pulse = (Mathf.Sin(_glowTime * Mathf.PI * 2f) * 0.5f + 0.5f);
            float intensity = Mathf.Lerp(_glowIntensityMin, _glowIntensityMax, pulse);

            // Cyan/magenta color shift
            Color glowColor = Color.Lerp(UITheme.Primary, UITheme.Accent, pulse * 0.3f);
            glowColor.a = intensity;

            if (_glowOutlines != null)
            {
                foreach (var outline in _glowOutlines)
                {
                    if (outline != null)
                    {
                        outline.effectColor = glowColor;
                    }
                }
            }
        }

        /// <summary>
        /// Play entrance animation with delay.
        /// </summary>
        public void PlayEntranceAnimation(float delay = 0f)
        {
            _entranceDelay = delay;
            StartCoroutine(EntranceCoroutine());
        }

        private System.Collections.IEnumerator EntranceCoroutine()
        {
            EnsureCanvasGroup();
            FindSelectionGlow();

            if (_originalScale == Vector3.zero)
            {
                _originalScale = transform.localScale;
                if (_originalScale == Vector3.zero)
                {
                    _originalScale = Vector3.one;
                }
            }

            // Start invisible and small
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
            transform.localScale = _originalScale * 0.7f;

            // Hide glow initially
            SetGlowVisible(false);

            if (_entranceDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(_entranceDelay);
            }

            _isAnimatingEntrance = true;
            _entranceTimer = 0f;

            // Play entrance sound
            PlayEntranceSound();
        }

        /// <summary>
        /// Play hover effect.
        /// </summary>
        public void PlayHoverEffect()
        {
            _isHovered = true;
            _glowTime = 0f;
            SetGlowVisible(true);
            PlayHoverSound();
        }

        /// <summary>
        /// Stop hover effect.
        /// </summary>
        public void StopHoverEffect()
        {
            _isHovered = false;
            SetGlowVisible(false);
        }

        /// <summary>
        /// Play selection effect.
        /// </summary>
        public void PlaySelectEffect()
        {
            _isAnimatingSelect = true;
            _selectTimer = 0f;

            // Flash background
            if (_background != null)
            {
                StartCoroutine(FlashBackground());
            }

            // Flash glow bright
            if (_selectionGlow != null)
            {
                StartCoroutine(FlashGlow());
            }

            PlaySelectSound();
        }

        private void SetGlowVisible(bool visible)
        {
            if (_glowOutlines == null) return;

            foreach (var outline in _glowOutlines)
            {
                if (outline != null)
                {
                    outline.effectColor = visible
                        ? UITheme.Primary.WithAlpha(_glowIntensityMin)
                        : Color.clear;
                }
            }
        }

        private System.Collections.IEnumerator FlashBackground()
        {
            Color originalColor = _background.color;
            Color flashColor = UITheme.Primary.WithAlpha(0.6f);

            float duration = 0.15f;
            float timer = 0f;

            // Flash in
            while (timer < duration * 0.3f)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / (duration * 0.3f);
                _background.color = Color.Lerp(originalColor, flashColor, t);
                yield return null;
            }

            // Flash out
            timer = 0f;
            while (timer < duration * 0.7f)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / (duration * 0.7f);
                _background.color = Color.Lerp(flashColor, originalColor, t);
                yield return null;
            }

            _background.color = originalColor;
        }

        private System.Collections.IEnumerator FlashGlow()
        {
            if (_glowOutlines == null) yield break;

            Color flashColor = Color.white;
            float duration = 0.2f;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / duration;
                Color currentColor = Color.Lerp(flashColor, UITheme.Primary, t);
                currentColor.a = Mathf.Lerp(1f, _glowIntensityMax, t);

                foreach (var outline in _glowOutlines)
                {
                    if (outline != null)
                    {
                        outline.effectColor = currentColor;
                    }
                }
                yield return null;
            }
        }

        #region Procedural Sound Effects

        private void PlayEntranceSound()
        {
            // Generate a quick "whoosh" sound
            PlayProceduralSound(0.08f, 800f, 400f, 0.3f);
        }

        private void PlayHoverSound()
        {
            // Generate a subtle "blip" sound
            PlayProceduralSound(0.05f, 1200f, 1000f, 0.2f);
        }

        private void PlaySelectSound()
        {
            // Generate a satisfying "confirm" sound - two tones
            PlayProceduralSound(0.06f, 800f, 1200f, 0.4f);
            StartCoroutine(DelayedTone(0.06f, 0.08f, 1200f, 1600f, 0.35f));
        }

        private System.Collections.IEnumerator DelayedTone(float delay, float duration, float startFreq, float endFreq, float volume)
        {
            yield return new WaitForSecondsRealtime(delay);
            PlayProceduralSound(duration, startFreq, endFreq, volume);
        }

        private void PlayProceduralSound(float duration, float startFreq, float endFreq, float volume)
        {
            if (_audioSource == null) return;

            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, t);

                // Generate sine wave with envelope
                float envelope = 1f - t; // Linear fade out
                envelope = envelope * envelope; // Quadratic for snappier decay

                float phase = 2f * Mathf.PI * freq * i / sampleRate;
                samples[i] = Mathf.Sin(phase) * envelope * volume;

                // Add slight harmonics for richer sound
                samples[i] += Mathf.Sin(phase * 2f) * envelope * volume * 0.3f;
                samples[i] += Mathf.Sin(phase * 3f) * envelope * volume * 0.1f;
            }

            AudioClip clip = AudioClip.Create("ProceduralUI", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);

            _audioSource.PlayOneShot(clip, 1f);
        }

        #endregion
    }
}
