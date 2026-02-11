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
        [SerializeField] private float m_entranceDelay = 0f;
        [SerializeField] private float m_entranceDuration = 0.4f;
        [SerializeField] private float m_hoverScale = 1.08f;
        [SerializeField] private float m_hoverSpeed = 12f;
        [SerializeField] private float m_selectPunchScale = 1.2f;
        [SerializeField] private float m_selectDuration = 0.25f;

        [Header("Glow Settings")]
        [SerializeField] private float m_glowPulseSpeed = 3f;
        [SerializeField] private float m_glowIntensityMin = 0.4f;
        [SerializeField] private float m_glowIntensityMax = 0.9f;

        [Header("References")]
        [SerializeField] private Image m_background;
        [SerializeField] private CanvasGroup m_canvasGroup;

        // Selection glow components
        private Transform m_selectionGlow;
        private Image m_glowImage;
        private Outline[] m_glowOutlines;

        private UpgradeCard m_card;
        private Vector3 m_originalScale;
        private float m_currentScale = 1f;
        private bool m_isHovered = false;
        private bool m_isAnimatingEntrance = false;
        private float m_entranceTimer = 0f;
        private bool m_isAnimatingSelect = false;
        private float m_selectTimer = 0f;
        private float m_glowTime = 0f;

        // Audio
        private AudioSource m_audioSource;

        private void Awake()
        {
            m_card = GetComponent<UpgradeCard>();
            EnsureCanvasGroup();
            FindSelectionGlow();
            EnsureAudioSource();

            m_originalScale = transform.localScale;
            if (m_originalScale == Vector3.zero)
            {
                m_originalScale = Vector3.one;
            }
        }

        private void EnsureCanvasGroup()
        {
            if (m_canvasGroup == null)
            {
                m_canvasGroup = GetComponent<CanvasGroup>();
            }
            if (m_canvasGroup == null)
            {
                m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void FindSelectionGlow()
        {
            m_selectionGlow = transform.Find("SelectionGlow");
            if (m_selectionGlow != null)
            {
                m_glowImage = m_selectionGlow.GetComponent<Image>();
                m_glowOutlines = m_selectionGlow.GetComponents<Outline>();
            }
        }

        private void EnsureAudioSource()
        {
            m_audioSource = GetComponent<AudioSource>();
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }
            m_audioSource.playOnAwake = false;
            m_audioSource.spatialBlend = 0f;
            m_audioSource.volume = 0.5f;
        }

        private void Update()
        {
            // Entrance animation
            if (m_isAnimatingEntrance)
            {
                m_entranceTimer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(m_entranceTimer / m_entranceDuration);

                // Ease out back for bouncy feel
                t = 1f - Mathf.Pow(1f - t, 3f);

                if (m_canvasGroup != null) m_canvasGroup.alpha = t;
                m_currentScale = Mathf.Lerp(0.7f, 1f, t);

                if (m_entranceTimer >= m_entranceDuration)
                {
                    m_isAnimatingEntrance = false;
                    if (m_canvasGroup != null) m_canvasGroup.alpha = 1f;
                    m_currentScale = 1f;
                }
            }

            // Selection punch animation
            if (m_isAnimatingSelect)
            {
                m_selectTimer += Time.unscaledDeltaTime;
                float t = m_selectTimer / m_selectDuration;

                if (t <= 0.4f)
                {
                    // Punch out fast
                    float punchT = t / 0.4f;
                    punchT = 1f - Mathf.Pow(1f - punchT, 2f); // Ease out
                    m_currentScale = Mathf.Lerp(1f, m_selectPunchScale, punchT);
                }
                else
                {
                    // Settle back
                    float punchT = (t - 0.4f) / 0.6f;
                    m_currentScale = Mathf.Lerp(m_selectPunchScale, 1f, punchT);
                }

                if (t >= 1f)
                {
                    m_isAnimatingSelect = false;
                    m_currentScale = 1f;
                }
            }

            // Hover scale
            if (!m_isAnimatingSelect && !m_isAnimatingEntrance)
            {
                float targetScale = m_isHovered ? m_hoverScale : 1f;
                m_currentScale = Mathf.Lerp(m_currentScale, targetScale, m_hoverSpeed * Time.unscaledDeltaTime);
            }

            // Apply scale
            transform.localScale = m_originalScale * m_currentScale;

            // Update selection glow pulse
            if (m_isHovered && m_selectionGlow != null)
            {
                UpdateSelectionGlow();
            }
        }

        private void UpdateSelectionGlow()
        {
            m_glowTime += Time.unscaledDeltaTime * m_glowPulseSpeed;
            float pulse = (Mathf.Sin(m_glowTime * Mathf.PI * 2f) * 0.5f + 0.5f);
            float intensity = Mathf.Lerp(m_glowIntensityMin, m_glowIntensityMax, pulse);

            // Cyan/magenta color shift
            Color glowColor = Color.Lerp(UITheme.Primary, UITheme.Accent, pulse * 0.3f);
            glowColor.a = intensity;

            if (m_glowOutlines != null)
            {
                foreach (var outline in m_glowOutlines)
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
            m_entranceDelay = delay;
            StartCoroutine(EntranceCoroutine());
        }

        private System.Collections.IEnumerator EntranceCoroutine()
        {
            EnsureCanvasGroup();
            FindSelectionGlow();

            if (m_originalScale == Vector3.zero)
            {
                m_originalScale = transform.localScale;
                if (m_originalScale == Vector3.zero)
                {
                    m_originalScale = Vector3.one;
                }
            }

            // Start invisible and small
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 0f;
            }
            transform.localScale = m_originalScale * 0.7f;

            // Hide glow initially
            SetGlowVisible(false);

            if (m_entranceDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(m_entranceDelay);
            }

            m_isAnimatingEntrance = true;
            m_entranceTimer = 0f;

            // Play entrance sound
            PlayEntranceSound();
        }

        /// <summary>
        /// Play hover effect.
        /// </summary>
        public void PlayHoverEffect()
        {
            m_isHovered = true;
            m_glowTime = 0f;
            SetGlowVisible(true);
            PlayHoverSound();
        }

        /// <summary>
        /// Stop hover effect.
        /// </summary>
        public void StopHoverEffect()
        {
            m_isHovered = false;
            SetGlowVisible(false);
        }

        /// <summary>
        /// Play selection effect.
        /// </summary>
        public void PlaySelectEffect()
        {
            m_isAnimatingSelect = true;
            m_selectTimer = 0f;

            // Flash background
            if (m_background != null)
            {
                StartCoroutine(FlashBackground());
            }

            // Flash glow bright
            if (m_selectionGlow != null)
            {
                StartCoroutine(FlashGlow());
            }

            PlaySelectSound();
        }

        private void SetGlowVisible(bool visible)
        {
            if (m_glowOutlines == null) return;

            foreach (var outline in m_glowOutlines)
            {
                if (outline != null)
                {
                    outline.effectColor = visible
                        ? UITheme.Primary.WithAlpha(m_glowIntensityMin)
                        : Color.clear;
                }
            }
        }

        private System.Collections.IEnumerator FlashBackground()
        {
            Color originalColor = m_background.color;
            Color flashColor = UITheme.Primary.WithAlpha(0.6f);

            float duration = 0.15f;
            float timer = 0f;

            // Flash in
            while (timer < duration * 0.3f)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / (duration * 0.3f);
                m_background.color = Color.Lerp(originalColor, flashColor, t);
                yield return null;
            }

            // Flash out
            timer = 0f;
            while (timer < duration * 0.7f)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / (duration * 0.7f);
                m_background.color = Color.Lerp(flashColor, originalColor, t);
                yield return null;
            }

            m_background.color = originalColor;
        }

        private System.Collections.IEnumerator FlashGlow()
        {
            if (m_glowOutlines == null) yield break;

            Color flashColor = Color.white;
            float duration = 0.2f;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / duration;
                Color currentColor = Color.Lerp(flashColor, UITheme.Primary, t);
                currentColor.a = Mathf.Lerp(1f, m_glowIntensityMax, t);

                foreach (var outline in m_glowOutlines)
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
            if (m_audioSource == null) return;

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

            m_audioSource.PlayOneShot(clip, 1f);
        }

        #endregion
    }
}
