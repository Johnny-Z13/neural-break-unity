using UnityEngine;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.Audio
{
    /// <summary>
    /// Audio manager that wraps MMSoundManager and uses procedural placeholder sounds.
    /// Subscribes to game events and plays appropriate sounds.
    ///
    /// TRUE SINGLETON - Lives in Boot scene, persists across all scenes.
    /// </summary>
    public class AudioManager : MonoBehaviour, IBootable
    {
        public static AudioManager Instance { get; private set; }

        [Header("Volume Settings")]
        [SerializeField] [Range(0f, 1f)] private float m_masterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float m_sfxVolume = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float m_musicVolume = 0.4f;
        [SerializeField] [Range(0f, 1f)] private float m_uiVolume = 0.6f;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource m_sfxSource;
        [SerializeField] private AudioSource m_musicSource;
        [SerializeField] private AudioSource m_uiSource;

        [Header("Override Clips (optional)")]
        [SerializeField] private AudioClip m_shootClipOverride;
        [SerializeField] private AudioClip m_hitClipOverride;
        [SerializeField] private AudioClip m_explosionClipOverride;
        [SerializeField] private AudioClip m_damageClipOverride;
        [SerializeField] private AudioClip m_pickupClipOverride;
        [SerializeField] private AudioClip m_menuClickClipOverride;

        // Procedural clips
        private AudioClip m_shootClip;
        private AudioClip m_hitClip;
        private AudioClip m_explosionClip;
        private AudioClip m_damageClip;
        private AudioClip m_pickupClip;
        private AudioClip m_menuClickClip;
        private AudioClip m_shieldHitClip;
        private AudioClip m_levelUpClip;
        private AudioClip m_gameOverClip;
        private AudioClip m_overheatClip;
        private AudioClip m_bgmClip;
        private AudioClip[] m_enemyDeathClips;

        [Header("Sound Throttling")]
        [SerializeField] private int m_maxSimultaneousSounds = 8;
        [SerializeField] private float m_throttleWindowTime = 0.1f;

        // Sound throttling tracking
        private System.Collections.Generic.Dictionary<string, float> m_lastPlayedTimes = new System.Collections.Generic.Dictionary<string, float>();
        private System.Collections.Generic.Dictionary<string, int> m_soundCountsInWindow = new System.Collections.Generic.Dictionary<string, int>();
        private float m_lastThrottleCleanupTime;
        private const float THROTTLE_CLEANUP_INTERVAL = 5f;

        // Static buffer for throttle cleanup (zero allocation)
        private static readonly System.Collections.Generic.List<string> s_keysToRemoveBuffer = new System.Collections.Generic.List<string>(32);

        // Combo pitch tracking
        private int m_currentCombo;
        private float m_comboPitchBonus;

        /// <summary>
        /// Called by BootManager for controlled initialization order.
        /// </summary>
        public void Initialize()
        {
            Instance = this;

            // Create audio sources if not assigned (fast operation)
            EnsureAudioSources();

            Debug.Log("[AudioManager] Initialized via BootManager - clips will be generated in Start()");
        }

        private void Awake()
        {
            // If already initialized by BootManager, skip
            if (Instance == this) return;

            // Fallback for running main scene directly (development only)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Development fallback - initialize directly
            Initialize();
            Debug.LogWarning("[AudioManager] Initialized via Awake fallback - should use Boot scene in production");
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
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
            m_shootClip = m_shootClipOverride != null ? m_shootClipOverride : ProceduralSFX.CreateShoot();
            m_hitClip = m_hitClipOverride != null ? m_hitClipOverride : ProceduralSFX.CreateHit();
            m_menuClickClip = m_menuClickClipOverride != null ? m_menuClickClipOverride : ProceduralSFX.CreateMenuClick();
            yield return null; // Allow other systems to initialize

            m_explosionClip = m_explosionClipOverride != null ? m_explosionClipOverride : ProceduralSFX.CreateExplosion();
            m_damageClip = m_damageClipOverride != null ? m_damageClipOverride : ProceduralSFX.CreateDamage();
            m_pickupClip = m_pickupClipOverride != null ? m_pickupClipOverride : ProceduralSFX.CreatePickup();
            yield return null;

            m_shieldHitClip = ProceduralSFX.CreateShieldHit();
            m_levelUpClip = ProceduralSFX.CreateLevelUp();
            m_gameOverClip = ProceduralSFX.CreateGameOver();
            m_overheatClip = ProceduralSFX.CreateOverheatWarning();
            yield return null;

            // Generate varied enemy death sounds (one per enemy type)
            m_enemyDeathClips = new AudioClip[8];
            for (int i = 0; i < 8; i++)
            {
                m_enemyDeathClips[i] = ProceduralSFX.CreateEnemyDeath(i);
                if (i % 2 == 1) yield return null; // Yield every 2 clips
            }

            // Background music is the heaviest - generate last
            yield return null;
            m_bgmClip = ProceduralSFX.CreateBackgroundMusic();

            float elapsed = Time.realtimeSinceStartup - startTime;
            Debug.Log($"[AudioManager] Clips generated in {elapsed:F2}s: shoot={m_shootClip != null}, hit={m_hitClip != null}, bgm={m_bgmClip != null}");
        }

        // Note: GenerateProceduralClips moved to GenerateProceduralClipsAsync() coroutine
        // to avoid blocking the main thread during boot

        private void EnsureAudioSources()
        {
            if (m_sfxSource == null)
            {
                var sfxGO = new GameObject("SFX_Source");
                sfxGO.transform.SetParent(transform);
                m_sfxSource = sfxGO.AddComponent<AudioSource>();
                m_sfxSource.playOnAwake = false;
            }

            if (m_uiSource == null)
            {
                var uiGO = new GameObject("UI_Source");
                uiGO.transform.SetParent(transform);
                m_uiSource = uiGO.AddComponent<AudioSource>();
                m_uiSource.playOnAwake = false;
                m_uiSource.ignoreListenerPause = true; // UI sounds during pause
            }

            if (m_musicSource == null)
            {
                var musicGO = new GameObject("Music_Source");
                musicGO.transform.SetParent(transform);
                m_musicSource = musicGO.AddComponent<AudioSource>();
                m_musicSource.playOnAwake = false;
                m_musicSource.loop = true;
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

        private void Update()
        {
            // Periodically clean up old throttle entries to prevent dictionary bloat
            if (Time.unscaledTime - m_lastThrottleCleanupTime > THROTTLE_CLEANUP_INTERVAL)
            {
                CleanupThrottleTracking();
                m_lastThrottleCleanupTime = Time.unscaledTime;
            }
        }

        /// <summary>
        /// Remove old entries from throttle tracking dictionaries
        /// </summary>
        private void CleanupThrottleTracking()
        {
            float currentTime = Time.unscaledTime;
            s_keysToRemoveBuffer.Clear();

            // Find expired entries using enumerator pattern (avoids foreach boxing)
            var enumerator = m_lastPlayedTimes.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var kvp = enumerator.Current;
                if (currentTime - kvp.Value > m_throttleWindowTime * 2f)
                {
                    s_keysToRemoveBuffer.Add(kvp.Key);
                }
            }
            enumerator.Dispose();

            // Remove expired entries (indexed for loop - zero allocation)
            for (int i = 0; i < s_keysToRemoveBuffer.Count; i++)
            {
                m_lastPlayedTimes.Remove(s_keysToRemoveBuffer[i]);
                m_soundCountsInWindow.Remove(s_keysToRemoveBuffer[i]);
            }
        }

        #region Event Handlers

        private void OnProjectileFired(ProjectileFiredEvent evt)
        {
            PlaySFX(m_shootClip, 0.3f, Random.Range(0.95f, 1.05f));
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            // Play enemy-type specific death sound with combo pitch bonus
            int typeIndex = (int)evt.enemyType;
            if (typeIndex >= 0 && typeIndex < m_enemyDeathClips.Length && m_enemyDeathClips[typeIndex] != null)
            {
                float pitch = Random.Range(0.95f, 1.05f) + m_comboPitchBonus;
                PlaySFX(m_enemyDeathClips[typeIndex], 0.5f, pitch);
            }

            // Also play explosion
            PlaySFX(m_explosionClip, 0.4f, Random.Range(0.9f, 1.1f) + m_comboPitchBonus * 0.5f);
        }

        private void OnEnemyDamaged(EnemyDamagedEvent evt)
        {
            PlaySFX(m_hitClip, 0.4f, Random.Range(0.95f, 1.05f));
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            PlaySFX(m_damageClip, 0.8f);
        }

        private void OnShieldChanged(ShieldChangedEvent evt)
        {
            // Play shield hit sound (implied shield was just used)
            PlaySFX(m_shieldHitClip, 0.7f);
        }

        private void OnPickupCollected(PickupCollectedEvent evt)
        {
            PlaySFX(m_pickupClip, 0.6f);
        }

        private void OnWeaponOverheated(WeaponOverheatedEvent evt)
        {
            PlaySFX(m_overheatClip, 0.6f);
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            if (evt.levelNumber > 1)
            {
                PlaySFX(m_levelUpClip, 0.7f);
            }
        }

        private void OnGameOver(GameOverEvent evt)
        {
            PlaySFX(m_gameOverClip, 0.8f);
        }

        private void OnVictory(VictoryEvent evt)
        {
            PlaySFX(m_levelUpClip, 1f); // Use level up sound for victory
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            // Start background music
            PlayMusic();
            m_currentCombo = 0;
            m_comboPitchBonus = 0f;
        }

        private void OnComboChanged(ComboChangedEvent evt)
        {
            m_currentCombo = evt.comboCount;
            // Increase pitch slightly with combo (max +0.3 semitones)
            m_comboPitchBonus = Mathf.Min(evt.comboCount * 0.01f, 0.3f);

            // Reset on combo break
            if (evt.comboCount == 0)
            {
                m_comboPitchBonus = 0f;
            }
        }

        #endregion

        #region Playback Methods

        /// <summary>
        /// Play a sound effect with automatic throttling to prevent audio overload
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
        {
            if (clip == null || m_sfxSource == null) return;

            // Use clip name as throttle key
            string clipKey = clip.name;
            float currentTime = Time.unscaledTime;

            // Check if this sound type is being throttled
            if (m_lastPlayedTimes.ContainsKey(clipKey))
            {
                float timeSinceLastPlay = currentTime - m_lastPlayedTimes[clipKey];

                // If within throttle window, check count
                if (timeSinceLastPlay < m_throttleWindowTime)
                {
                    // Increment count in window
                    if (m_soundCountsInWindow.ContainsKey(clipKey))
                    {
                        m_soundCountsInWindow[clipKey]++;
                    }
                    else
                    {
                        m_soundCountsInWindow[clipKey] = 1;
                    }

                    // Skip if over limit
                    if (m_soundCountsInWindow[clipKey] >= m_maxSimultaneousSounds)
                    {
                        return; // Throttle this sound
                    }
                }
                else
                {
                    // Window expired, reset count
                    m_soundCountsInWindow[clipKey] = 1;
                }
            }
            else
            {
                // First play of this sound
                m_soundCountsInWindow[clipKey] = 1;
            }

            // Update last played time
            m_lastPlayedTimes[clipKey] = currentTime;

            // Play the sound
            m_sfxSource.pitch = pitchMultiplier;
            m_sfxSource.PlayOneShot(clip, m_masterVolume * m_sfxVolume * volumeMultiplier);
        }

        /// <summary>
        /// Play a UI sound (ignores listener pause)
        /// </summary>
        public void PlayUI(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null || m_uiSource == null) return;

            m_uiSource.PlayOneShot(clip, m_masterVolume * m_uiVolume * volumeMultiplier);
        }

        /// <summary>
        /// Play menu click sound
        /// </summary>
        public void PlayMenuClick()
        {
            PlayUI(m_menuClickClip);
        }

        /// <summary>
        /// Start playing background music
        /// </summary>
        public void PlayMusic()
        {
            if (m_musicSource == null || m_bgmClip == null) return;

            m_musicSource.clip = m_bgmClip;
            m_musicSource.volume = m_masterVolume * m_musicVolume;
            m_musicSource.Play();
        }

        /// <summary>
        /// Stop background music
        /// </summary>
        public void StopMusic()
        {
            if (m_musicSource != null)
            {
                m_musicSource.Stop();
            }
        }

        /// <summary>
        /// Set master volume
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            m_masterVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Set SFX volume
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            m_sfxVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Set UI volume
        /// </summary>
        public void SetUIVolume(float volume)
        {
            m_uiVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Set music volume
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            m_musicVolume = Mathf.Clamp01(volume);
            if (m_musicSource != null)
            {
                m_musicSource.volume = m_masterVolume * m_musicVolume;
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Play Shoot")]
        private void DebugShoot() => PlaySFX(m_shootClip);

        [ContextMenu("Debug: Play Explosion")]
        private void DebugExplosion() => PlaySFX(m_explosionClip);

        [ContextMenu("Debug: Play Damage")]
        private void DebugDamage() => PlaySFX(m_damageClip);

        [ContextMenu("Debug: Play Pickup")]
        private void DebugPickup() => PlaySFX(m_pickupClip);

        [ContextMenu("Debug: Play Level Up")]
        private void DebugLevelUp() => PlaySFX(m_levelUpClip);

        [ContextMenu("Debug: Play Game Over")]
        private void DebugGameOver() => PlaySFX(m_gameOverClip);

        [ContextMenu("Debug: Play Music")]
        private void DebugMusic() => PlayMusic();

        [ContextMenu("Debug: Stop Music")]
        private void DebugStopMusic() => StopMusic();

        #endregion
    }
}
