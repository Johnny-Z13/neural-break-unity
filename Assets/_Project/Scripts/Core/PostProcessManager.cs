using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Manages post-processing effects at runtime.
    /// Controls bloom, chromatic aberration, vignette, and other effects.
    /// Singleton pattern for easy access from other systems.
    /// </summary>
    public class PostProcessManager : MonoBehaviour
    {
        public static PostProcessManager Instance { get; private set; }

        [Header("Volume Reference")]
        [SerializeField] private Volume _globalVolume;
        [SerializeField] private VolumeProfile _defaultProfile;

        [Header("Default Bloom Settings")]
        [SerializeField] private float _defaultBloomIntensity = 1f;
        [SerializeField] private float _defaultBloomThreshold = 0.9f;
        [SerializeField] private float _defaultBloomScatter = 0.7f;

        [Header("Default Vignette Settings")]
        [SerializeField] private float _defaultVignetteIntensity = 0.3f;
        [SerializeField] private Color _defaultVignetteColor = Color.black;

        [Header("Default Chromatic Aberration Settings")]
        [SerializeField] private float _defaultChromaticIntensity = 0f;

        [Header("Effect Presets")]
        [SerializeField] private bool _enableDamageFlash = true;
        [SerializeField] private bool _enableKillFeedback = true;

        // Volume components
        private Bloom _bloom;
        private Vignette _vignette;
        private ChromaticAberration _chromaticAberration;
        private ColorAdjustments _colorAdjustments;
        private LensDistortion _lensDistortion;

        // Temporary effect states
        private float _damageFlashTimer;
        private float _killFlashTimer;
        private float _screenShakeIntensity;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupVolume();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void SetupVolume()
        {
            // Find or create global volume
            if (_globalVolume == null)
            {
                _globalVolume = FindFirstObjectByType<Volume>();
            }

            if (_globalVolume == null)
            {
                // Create global volume
                var volumeGO = new GameObject("GlobalVolume");
                volumeGO.transform.SetParent(transform);
                _globalVolume = volumeGO.AddComponent<Volume>();
                _globalVolume.isGlobal = true;
                _globalVolume.priority = 100;
            }

            // Create or use profile
            if (_globalVolume.profile == null)
            {
                if (_defaultProfile != null)
                {
                    // Clone the default profile so we don't modify the asset
                    _globalVolume.profile = Instantiate(_defaultProfile);
                }
                else
                {
                    _globalVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                }
            }

            // Get or add components
            EnsureVolumeComponents();
            ApplyDefaultSettings();
        }

        private void EnsureVolumeComponents()
        {
            var profile = _globalVolume.profile;

            // Bloom
            if (!profile.TryGet(out _bloom))
            {
                _bloom = profile.Add<Bloom>();
            }

            // Vignette
            if (!profile.TryGet(out _vignette))
            {
                _vignette = profile.Add<Vignette>();
            }

            // Chromatic Aberration
            if (!profile.TryGet(out _chromaticAberration))
            {
                _chromaticAberration = profile.Add<ChromaticAberration>();
            }

            // Color Adjustments
            if (!profile.TryGet(out _colorAdjustments))
            {
                _colorAdjustments = profile.Add<ColorAdjustments>();
            }

            // Lens Distortion
            if (!profile.TryGet(out _lensDistortion))
            {
                _lensDistortion = profile.Add<LensDistortion>();
            }
        }

        private void ApplyDefaultSettings()
        {
            // Bloom
            _bloom.active = true;
            _bloom.intensity.overrideState = true;
            _bloom.intensity.value = _defaultBloomIntensity;
            _bloom.threshold.overrideState = true;
            _bloom.threshold.value = _defaultBloomThreshold;
            _bloom.scatter.overrideState = true;
            _bloom.scatter.value = _defaultBloomScatter;

            // Vignette
            _vignette.active = true;
            _vignette.intensity.overrideState = true;
            _vignette.intensity.value = _defaultVignetteIntensity;
            _vignette.color.overrideState = true;
            _vignette.color.value = _defaultVignetteColor;

            // Chromatic Aberration
            _chromaticAberration.active = true;
            _chromaticAberration.intensity.overrideState = true;
            _chromaticAberration.intensity.value = _defaultChromaticIntensity;

            // Color Adjustments
            _colorAdjustments.active = true;
            _colorAdjustments.postExposure.overrideState = true;
            _colorAdjustments.saturation.overrideState = true;
            _colorAdjustments.contrast.overrideState = true;

            // Lens Distortion
            _lensDistortion.active = true;
            _lensDistortion.intensity.overrideState = true;
            _lensDistortion.intensity.value = 0f;
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
            if (_damageFlashTimer > 0)
            {
                _damageFlashTimer -= Time.deltaTime * 5f;
                float t = Mathf.Max(0, _damageFlashTimer);

                // Red vignette during damage
                _vignette.color.value = Color.Lerp(_defaultVignetteColor, Color.red, t * 0.5f);
                _vignette.intensity.value = _defaultVignetteIntensity + t * 0.3f;

                // Chromatic aberration spike
                _chromaticAberration.intensity.value = t * 0.5f;
            }
            else
            {
                _vignette.color.value = _defaultVignetteColor;
                _vignette.intensity.value = _defaultVignetteIntensity;
                _chromaticAberration.intensity.value = _defaultChromaticIntensity;
            }

            // Kill flash decay
            if (_killFlashTimer > 0)
            {
                _killFlashTimer -= Time.deltaTime * 8f;
                float t = Mathf.Max(0, _killFlashTimer);

                // Brief bloom intensity spike
                _bloom.intensity.value = _defaultBloomIntensity + t * 0.5f;
            }
            else
            {
                _bloom.intensity.value = _defaultBloomIntensity;
            }
        }

        #region Public API

        /// <summary>
        /// Set the bloom intensity (0-10)
        /// </summary>
        public void SetBloomIntensity(float intensity)
        {
            _defaultBloomIntensity = Mathf.Clamp(intensity, 0f, 10f);
            _bloom.intensity.value = _defaultBloomIntensity;
        }

        /// <summary>
        /// Set the bloom threshold (0-2)
        /// </summary>
        public void SetBloomThreshold(float threshold)
        {
            _defaultBloomThreshold = Mathf.Clamp(threshold, 0f, 2f);
            _bloom.threshold.value = _defaultBloomThreshold;
        }

        /// <summary>
        /// Set bloom scatter/diffusion (0-1)
        /// </summary>
        public void SetBloomScatter(float scatter)
        {
            _defaultBloomScatter = Mathf.Clamp01(scatter);
            _bloom.scatter.value = _defaultBloomScatter;
        }

        /// <summary>
        /// Set vignette intensity (0-1)
        /// </summary>
        public void SetVignetteIntensity(float intensity)
        {
            _defaultVignetteIntensity = Mathf.Clamp01(intensity);
            _vignette.intensity.value = _defaultVignetteIntensity;
        }

        /// <summary>
        /// Set chromatic aberration intensity (0-1)
        /// </summary>
        public void SetChromaticAberration(float intensity)
        {
            _defaultChromaticIntensity = Mathf.Clamp01(intensity);
            _chromaticAberration.intensity.value = _defaultChromaticIntensity;
        }

        /// <summary>
        /// Trigger a damage flash effect
        /// </summary>
        public void TriggerDamageFlash(float intensity = 1f)
        {
            if (!_enableDamageFlash) return;
            _damageFlashTimer = Mathf.Max(_damageFlashTimer, intensity);
        }

        /// <summary>
        /// Trigger a brief bloom pulse for kills
        /// </summary>
        public void TriggerKillFlash(float intensity = 0.5f)
        {
            if (!_enableKillFeedback) return;
            _killFlashTimer = Mathf.Max(_killFlashTimer, intensity);
        }

        /// <summary>
        /// Set post exposure (-5 to 5)
        /// </summary>
        public void SetExposure(float exposure)
        {
            _colorAdjustments.postExposure.value = Mathf.Clamp(exposure, -5f, 5f);
        }

        /// <summary>
        /// Set saturation (-100 to 100)
        /// </summary>
        public void SetSaturation(float saturation)
        {
            _colorAdjustments.saturation.value = Mathf.Clamp(saturation, -100f, 100f);
        }

        /// <summary>
        /// Set contrast (-100 to 100)
        /// </summary>
        public void SetContrast(float contrast)
        {
            _colorAdjustments.contrast.value = Mathf.Clamp(contrast, -100f, 100f);
        }

        /// <summary>
        /// Set lens distortion intensity (-1 to 1)
        /// </summary>
        public void SetLensDistortion(float intensity)
        {
            _lensDistortion.intensity.value = Mathf.Clamp(intensity, -1f, 1f);
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
            _bloom.intensity.value = _defaultBloomIntensity + comboBoost;
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
