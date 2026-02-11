using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Z13.Core;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Manages post-processing effects at runtime.
    /// Controls bloom, chromatic aberration, vignette, and other effects.
    /// Singleton pattern for easy access from other systems.
    /// </summary>
    public class PostProcessManager : MonoBehaviour
    {

        [Header("Volume Reference")]
        [SerializeField] private Volume m_globalVolume;
        [SerializeField] private VolumeProfile m_defaultProfile;

        [Header("Default Bloom Settings")]
        [SerializeField] private float m_defaultBloomIntensity = 1f;
        [SerializeField] private float m_defaultBloomThreshold = 0.9f;
        [SerializeField] private float m_defaultBloomScatter = 0.7f;

        [Header("Default Vignette Settings")]
        [SerializeField] private float m_defaultVignetteIntensity = 0.3f;
        [SerializeField] private Color m_defaultVignetteColor = Color.black;

        [Header("Default Chromatic Aberration Settings")]
        [SerializeField] private float m_defaultChromaticIntensity = 0f;

        [Header("Effect Presets")]
        [SerializeField] private bool m_enableDamageFlash = true;
        [SerializeField] private bool m_enableKillFeedback = true;

        // Volume components
        private Bloom m_bloom;
        private Vignette m_vignette;
        private ChromaticAberration m_chromaticAberration;
        private ColorAdjustments m_colorAdjustments;
        private LensDistortion m_lensDistortion;

        // Temporary effect states
        private float m_damageFlashTimer;
        private float m_killFlashTimer;
        private float m_screenShakeIntensity;

        private void Awake()
        {
            // Singleton setup

            SetupVolume();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

        }

        private void SetupVolume()
        {
            // Find volume via GameObject if not assigned
            if (m_globalVolume == null)
            {
                var volumeGO = GameObject.Find("GlobalVolume");
                if (volumeGO != null)
                {
                    m_globalVolume = volumeGO.GetComponent<Volume>();
                }
            }

            if (m_globalVolume == null)
            {
                // Create global volume
                var volumeGO = new GameObject("GlobalVolume");
                volumeGO.transform.SetParent(transform);
                m_globalVolume = volumeGO.AddComponent<Volume>();
                m_globalVolume.isGlobal = true;
                m_globalVolume.priority = 100;
            }

            // Create or use profile
            if (m_globalVolume.profile == null)
            {
                if (m_defaultProfile != null)
                {
                    // Clone the default profile so we don't modify the asset
                    m_globalVolume.profile = Instantiate(m_defaultProfile);
                }
                else
                {
                    m_globalVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                }
            }

            // Get or add components
            EnsureVolumeComponents();
            ApplyDefaultSettings();
        }

        private void EnsureVolumeComponents()
        {
            var profile = m_globalVolume.profile;

            // Bloom
            if (!profile.TryGet(out m_bloom))
            {
                m_bloom = profile.Add<Bloom>();
            }

            // Vignette
            if (!profile.TryGet(out m_vignette))
            {
                m_vignette = profile.Add<Vignette>();
            }

            // Chromatic Aberration
            if (!profile.TryGet(out m_chromaticAberration))
            {
                m_chromaticAberration = profile.Add<ChromaticAberration>();
            }

            // Color Adjustments
            if (!profile.TryGet(out m_colorAdjustments))
            {
                m_colorAdjustments = profile.Add<ColorAdjustments>();
            }

            // Lens Distortion
            if (!profile.TryGet(out m_lensDistortion))
            {
                m_lensDistortion = profile.Add<LensDistortion>();
            }
        }

        private void ApplyDefaultSettings()
        {
            // Bloom
            m_bloom.active = true;
            m_bloom.intensity.overrideState = true;
            m_bloom.intensity.value = m_defaultBloomIntensity;
            m_bloom.threshold.overrideState = true;
            m_bloom.threshold.value = m_defaultBloomThreshold;
            m_bloom.scatter.overrideState = true;
            m_bloom.scatter.value = m_defaultBloomScatter;

            // Vignette
            m_vignette.active = true;
            m_vignette.intensity.overrideState = true;
            m_vignette.intensity.value = m_defaultVignetteIntensity;
            m_vignette.color.overrideState = true;
            m_vignette.color.value = m_defaultVignetteColor;

            // Chromatic Aberration
            m_chromaticAberration.active = true;
            m_chromaticAberration.intensity.overrideState = true;
            m_chromaticAberration.intensity.value = m_defaultChromaticIntensity;

            // Color Adjustments
            m_colorAdjustments.active = true;
            m_colorAdjustments.postExposure.overrideState = true;
            m_colorAdjustments.saturation.overrideState = true;
            m_colorAdjustments.contrast.overrideState = true;

            // Lens Distortion
            m_lensDistortion.active = true;
            m_lensDistortion.intensity.overrideState = true;
            m_lensDistortion.intensity.value = 0f;
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        }

        private void Update()
        {
            UpdateTemporaryEffects();
        }

        private void UpdateTemporaryEffects()
        {
            // Damage flash decay
            if (m_damageFlashTimer > 0)
            {
                m_damageFlashTimer -= Time.deltaTime * 5f;
                float t = Mathf.Max(0, m_damageFlashTimer);

                // Red vignette during damage
                m_vignette.color.value = Color.Lerp(m_defaultVignetteColor, Color.red, t * 0.5f);
                m_vignette.intensity.value = m_defaultVignetteIntensity + t * 0.3f;

                // Chromatic aberration spike
                m_chromaticAberration.intensity.value = t * 0.5f;
            }
            else
            {
                m_vignette.color.value = m_defaultVignetteColor;
                m_vignette.intensity.value = m_defaultVignetteIntensity;
                m_chromaticAberration.intensity.value = m_defaultChromaticIntensity;
            }

            // Kill flash decay
            if (m_killFlashTimer > 0)
            {
                m_killFlashTimer -= Time.deltaTime * 8f;
                float t = Mathf.Max(0, m_killFlashTimer);

                // Brief bloom intensity spike
                m_bloom.intensity.value = m_defaultBloomIntensity + t * 0.5f;
            }
            else
            {
                m_bloom.intensity.value = m_defaultBloomIntensity;
            }
        }

        #region Public API

        /// <summary>
        /// Set the bloom intensity (0-10)
        /// </summary>
        public void SetBloomIntensity(float intensity)
        {
            m_defaultBloomIntensity = Mathf.Clamp(intensity, 0f, 10f);
            m_bloom.intensity.value = m_defaultBloomIntensity;
        }

        /// <summary>
        /// Set the bloom threshold (0-2)
        /// </summary>
        public void SetBloomThreshold(float threshold)
        {
            m_defaultBloomThreshold = Mathf.Clamp(threshold, 0f, 2f);
            m_bloom.threshold.value = m_defaultBloomThreshold;
        }

        /// <summary>
        /// Set bloom scatter/diffusion (0-1)
        /// </summary>
        public void SetBloomScatter(float scatter)
        {
            m_defaultBloomScatter = Mathf.Clamp01(scatter);
            m_bloom.scatter.value = m_defaultBloomScatter;
        }

        /// <summary>
        /// Set vignette intensity (0-1)
        /// </summary>
        public void SetVignetteIntensity(float intensity)
        {
            m_defaultVignetteIntensity = Mathf.Clamp01(intensity);
            m_vignette.intensity.value = m_defaultVignetteIntensity;
        }

        /// <summary>
        /// Set chromatic aberration intensity (0-1)
        /// </summary>
        public void SetChromaticAberration(float intensity)
        {
            m_defaultChromaticIntensity = Mathf.Clamp01(intensity);
            m_chromaticAberration.intensity.value = m_defaultChromaticIntensity;
        }

        /// <summary>
        /// Trigger a damage flash effect
        /// </summary>
        public void TriggerDamageFlash(float intensity = 1f)
        {
            if (!m_enableDamageFlash) return;
            m_damageFlashTimer = Mathf.Max(m_damageFlashTimer, intensity);
        }

        /// <summary>
        /// Trigger a brief bloom pulse for kills
        /// </summary>
        public void TriggerKillFlash(float intensity = 0.5f)
        {
            if (!m_enableKillFeedback) return;
            m_killFlashTimer = Mathf.Max(m_killFlashTimer, intensity);
        }

        /// <summary>
        /// Set post exposure (-5 to 5)
        /// </summary>
        public void SetExposure(float exposure)
        {
            m_colorAdjustments.postExposure.value = Mathf.Clamp(exposure, -5f, 5f);
        }

        /// <summary>
        /// Set saturation (-100 to 100)
        /// </summary>
        public void SetSaturation(float saturation)
        {
            m_colorAdjustments.saturation.value = Mathf.Clamp(saturation, -100f, 100f);
        }

        /// <summary>
        /// Set contrast (-100 to 100)
        /// </summary>
        public void SetContrast(float contrast)
        {
            m_colorAdjustments.contrast.value = Mathf.Clamp(contrast, -100f, 100f);
        }

        /// <summary>
        /// Set lens distortion intensity (-1 to 1)
        /// </summary>
        public void SetLensDistortion(float intensity)
        {
            m_lensDistortion.intensity.value = Mathf.Clamp(intensity, -1f, 1f);
        }

        /// <summary>
        /// Apply a preset for intense gameplay moments
        /// </summary>
        public void ApplyIntensePreset()
        {
            SetBloomIntensity(1.5f);
            SetBloomScatter(0.8f);
            SetVignetteIntensity(0.4f);
            SetChromaticAberration(0.1f);
        }

        /// <summary>
        /// Apply a calm/menu preset
        /// </summary>
        public void ApplyCalmPreset()
        {
            SetBloomIntensity(0.8f);
            SetBloomScatter(0.6f);
            SetVignetteIntensity(0.2f);
            SetChromaticAberration(0f);
        }

        /// <summary>
        /// Reset all effects to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            ApplyDefaultSettings();
        }

        #endregion

        #region Event Handlers

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            float normalizedDamage = Mathf.Clamp01(evt.damage / 50f);
            TriggerDamageFlash(0.5f + normalizedDamage * 0.5f);
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            TriggerKillFlash(0.3f);
        }

        private void OnComboChanged(ComboChangedEvent evt)
        {
            // Increase bloom slightly with combo
            float comboBoost = Mathf.Min(evt.comboCount * 0.02f, 0.3f);
            m_bloom.intensity.value = m_defaultBloomIntensity + comboBoost;
        }

        private void OnGameOver(GameOverEvent evt)
        {
            // Desaturate on game over
            SetSaturation(-30f);
            SetVignetteIntensity(0.5f);
        }

        #endregion

        #region Debug

        [ContextMenu("Test Damage Flash")]
        private void DebugDamageFlash() => TriggerDamageFlash(1f);

        [ContextMenu("Test Kill Flash")]
        private void DebugKillFlash() => TriggerKillFlash(1f);

        [ContextMenu("Apply Intense Preset")]
        private void DebugIntensePreset() => ApplyIntensePreset();

        [ContextMenu("Apply Calm Preset")]
        private void DebugCalmPreset() => ApplyCalmPreset();

        #endregion
    }
}
