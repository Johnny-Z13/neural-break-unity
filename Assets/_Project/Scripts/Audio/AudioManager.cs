using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Audio
{
    /// <summary>
    /// Audio manager that wraps MMSoundManager and uses procedural placeholder sounds.
    /// Subscribes to game events and plays appropriate sounds.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Volume Settings")]
        [SerializeField] [Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float _sfxVolume = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float _musicVolume = 0.4f;
        [SerializeField] [Range(0f, 1f)] private float _uiVolume = 0.6f;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _uiSource;

        [Header("Override Clips (optional)")]
        [SerializeField] private AudioClip _shootClipOverride;
        [SerializeField] private AudioClip _hitClipOverride;
        [SerializeField] private AudioClip _explosionClipOverride;
        [SerializeField] private AudioClip _damageClipOverride;
        [SerializeField] private AudioClip _pickupClipOverride;
        [SerializeField] private AudioClip _menuClickClipOverride;

        // Procedural clips
        private AudioClip _shootClip;
        private AudioClip _hitClip;
        private AudioClip _explosionClip;
        private AudioClip _damageClip;
        private AudioClip _pickupClip;
        private AudioClip _menuClickClip;
        private AudioClip _shieldHitClip;
        private AudioClip _levelUpClip;
        private AudioClip _gameOverClip;
        private AudioClip _overheatClip;
        private AudioClip _bgmClip;
        private AudioClip[] _enemyDeathClips;

        // Combo pitch tracking
        private int _currentCombo;
        private float _comboPitchBonus;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Create audio sources if not assigned (fast operation)
            EnsureAudioSources();

            // Defer clip generation to avoid blocking main thread on boot
            // This will happen over multiple frames via coroutine
            Debug.Log("[AudioManager] Awake complete - clips will be generated in Start()");
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            // Generate procedural clips (deferred from Awake to reduce boot hang)
            StartCoroutine(GenerateProceduralClipsAsync());

            SubscribeEvents();
        }

        private System.Collections.IEnumerator GenerateProceduralClipsAsync()
        {
            Debug.Log("[AudioManager] Starting async clip generation...");
            float startTime = Time.realtimeSinceStartup;

            // Generate short clips first (fast)
            _shootClip = _shootClipOverride != null ? _shootClipOverride : ProceduralSFX.CreateShoot();
            _hitClip = _hitClipOverride != null ? _hitClipOverride : ProceduralSFX.CreateHit();
            _menuClickClip = _menuClickClipOverride != null ? _menuClickClipOverride : ProceduralSFX.CreateMenuClick();
            yield return null; // Allow other systems to initialize

            _explosionClip = _explosionClipOverride != null ? _explosionClipOverride : ProceduralSFX.CreateExplosion();
            _damageClip = _damageClipOverride != null ? _damageClipOverride : ProceduralSFX.CreateDamage();
            _pickupClip = _pickupClipOverride != null ? _pickupClipOverride : ProceduralSFX.CreatePickup();
            yield return null;

            _shieldHitClip = ProceduralSFX.CreateShieldHit();
            _levelUpClip = ProceduralSFX.CreateLevelUp();
            _gameOverClip = ProceduralSFX.CreateGameOver();
            _overheatClip = ProceduralSFX.CreateOverheatWarning();
            yield return null;

            // Generate varied enemy death sounds (one per enemy type)
            _enemyDeathClips = new AudioClip[8];
            for (int i = 0; i < 8; i++)
            {
                _enemyDeathClips[i] = ProceduralSFX.CreateEnemyDeath(i);
                if (i % 2 == 1) yield return null; // Yield every 2 clips
            }

            // Background music is the heaviest - generate last
            yield return null;
            _bgmClip = ProceduralSFX.CreateBackgroundMusic();

            float elapsed = Time.realtimeSinceStartup - startTime;
            Debug.Log($"[AudioManager] Clips generated in {elapsed:F2}s: shoot={_shootClip != null}, hit={_hitClip != null}, bgm={_bgmClip != null}");
        }

        // Note: GenerateProceduralClips moved to GenerateProceduralClipsAsync() coroutine
        // to avoid blocking the main thread during boot

        private void EnsureAudioSources()
        {
            if (_sfxSource == null)
            {
                var sfxGO = new GameObject("SFX_Source");
                sfxGO.transform.SetParent(transform);
                _sfxSource = sfxGO.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
            }

            if (_uiSource == null)
            {
                var uiGO = new GameObject("UI_Source");
                uiGO.transform.SetParent(transform);
                _uiSource = uiGO.AddComponent<AudioSource>();
                _uiSource.playOnAwake = false;
                _uiSource.ignoreListenerPause = true; // UI sounds during pause
            }

            if (_musicSource == null)
            {
                var musicGO = new GameObject("Music_Source");
                musicGO.transform.SetParent(transform);
                _musicSource = musicGO.AddComponent<AudioSource>();
                _musicSource.playOnAwake = false;
                _musicSource.loop = true;
            }
        }

        #region Event Subscriptions

        private void SubscribeEvents()
        {
            EventBus.Subscribe<ProjectileFiredEvent>(OnProjectileFired);
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<ShieldChangedEvent>(OnShieldChanged);
            EventBus.Subscribe<PickupCollectedEvent>(OnPickupCollected);
            EventBus.Subscribe<WeaponOverheatedEvent>(OnWeaponOverheated);
            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<VictoryEvent>(OnVictory);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);
        }

        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<ProjectileFiredEvent>(OnProjectileFired);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<EnemyDamagedEvent>(OnEnemyDamaged);
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<ShieldChangedEvent>(OnShieldChanged);
            EventBus.Unsubscribe<PickupCollectedEvent>(OnPickupCollected);
            EventBus.Unsubscribe<WeaponOverheatedEvent>(OnWeaponOverheated);
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<VictoryEvent>(OnVictory);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<ComboChangedEvent>(OnComboChanged);
        }

        #endregion

        #region Event Handlers

        private void OnProjectileFired(ProjectileFiredEvent evt)
        {
            PlaySFX(_shootClip, 0.3f, Random.Range(0.95f, 1.05f));
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            // Play enemy-type specific death sound with combo pitch bonus
            int typeIndex = (int)evt.enemyType;
            if (typeIndex >= 0 && typeIndex < _enemyDeathClips.Length && _enemyDeathClips[typeIndex] != null)
            {
                float pitch = Random.Range(0.95f, 1.05f) + _comboPitchBonus;
                PlaySFX(_enemyDeathClips[typeIndex], 0.5f, pitch);
            }

            // Also play explosion
            PlaySFX(_explosionClip, 0.4f, Random.Range(0.9f, 1.1f) + _comboPitchBonus * 0.5f);
        }

        private void OnEnemyDamaged(EnemyDamagedEvent evt)
        {
            PlaySFX(_hitClip, 0.4f, Random.Range(0.95f, 1.05f));
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            PlaySFX(_damageClip, 0.8f);
        }

        private void OnShieldChanged(ShieldChangedEvent evt)
        {
            // Play shield hit sound (implied shield was just used)
            PlaySFX(_shieldHitClip, 0.7f);
        }

        private void OnPickupCollected(PickupCollectedEvent evt)
        {
            PlaySFX(_pickupClip, 0.6f);
        }

        private void OnWeaponOverheated(WeaponOverheatedEvent evt)
        {
            PlaySFX(_overheatClip, 0.6f);
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            if (evt.levelNumber > 1)
            {
                PlaySFX(_levelUpClip, 0.7f);
            }
        }

        private void OnGameOver(GameOverEvent evt)
        {
            PlaySFX(_gameOverClip, 0.8f);
        }

        private void OnVictory(VictoryEvent evt)
        {
            PlaySFX(_levelUpClip, 1f); // Use level up sound for victory
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            // Start background music
            PlayMusic();
            _currentCombo = 0;
            _comboPitchBonus = 0f;
        }

        private void OnComboChanged(ComboChangedEvent evt)
        {
            _currentCombo = evt.comboCount;
            // Increase pitch slightly with combo (max +0.3 semitones)
            _comboPitchBonus = Mathf.Min(evt.comboCount * 0.01f, 0.3f);

            // Reset on combo break
            if (evt.comboCount == 0)
            {
                _comboPitchBonus = 0f;
            }
        }

        #endregion

        #region Playback Methods

        /// <summary>
        /// Play a sound effect
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
        {
            if (clip == null || _sfxSource == null) return;

            _sfxSource.pitch = pitchMultiplier;
            _sfxSource.PlayOneShot(clip, _masterVolume * _sfxVolume * volumeMultiplier);
        }

        /// <summary>
        /// Play a UI sound (ignores listener pause)
        /// </summary>
        public void PlayUI(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null || _uiSource == null) return;

            _uiSource.PlayOneShot(clip, _masterVolume * _uiVolume * volumeMultiplier);
        }

        /// <summary>
        /// Play menu click sound
        /// </summary>
        public void PlayMenuClick()
        {
            PlayUI(_menuClickClip);
        }

        /// <summary>
        /// Start playing background music
        /// </summary>
        public void PlayMusic()
        {
            if (_musicSource == null || _bgmClip == null) return;

            _musicSource.clip = _bgmClip;
            _musicSource.volume = _masterVolume * _musicVolume;
            _musicSource.Play();
        }

        /// <summary>
        /// Stop background music
        /// </summary>
        public void StopMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.Stop();
            }
        }

        /// <summary>
        /// Set master volume
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Set SFX volume
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Set UI volume
        /// </summary>
        public void SetUIVolume(float volume)
        {
            _uiVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Set music volume
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            if (_musicSource != null)
            {
                _musicSource.volume = _masterVolume * _musicVolume;
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Play Shoot")]
        private void DebugShoot() => PlaySFX(_shootClip);

        [ContextMenu("Debug: Play Explosion")]
        private void DebugExplosion() => PlaySFX(_explosionClip);

        [ContextMenu("Debug: Play Damage")]
        private void DebugDamage() => PlaySFX(_damageClip);

        [ContextMenu("Debug: Play Pickup")]
        private void DebugPickup() => PlaySFX(_pickupClip);

        [ContextMenu("Debug: Play Level Up")]
        private void DebugLevelUp() => PlaySFX(_levelUpClip);

        [ContextMenu("Debug: Play Game Over")]
        private void DebugGameOver() => PlaySFX(_gameOverClip);

        [ContextMenu("Debug: Play Music")]
        private void DebugMusic() => PlayMusic();

        [ContextMenu("Debug: Stop Music")]
        private void DebugStopMusic() => StopMusic();

        #endregion
    }
}
