using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NeuralBreak.Core;
using NeuralBreak.Entities;

namespace NeuralBreak.Audio
{
    /// <summary>
    /// Track information
    /// </summary>
    [System.Serializable]
    public class MusicTrack
    {
        public string name;
        public AudioClip clip;
        public float bpm = 120f;
        public float intensity = 0.5f; // 0-1 for matching game intensity
        public bool isBossTrack = false;
    }

    /// <summary>
    /// Manages background music with multiple tracks.
    /// Handles cross-fading, intensity-based track selection, and boss music.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {

        [Header("Audio Sources")]
        [SerializeField] private AudioSource m_sourceA;
        [SerializeField] private AudioSource m_sourceB;

        [Header("Settings")]
        [SerializeField] private float m_crossfadeDuration = 2f;
        [SerializeField] private float m_masterVolume = 0.7f;
        [SerializeField] private bool m_autoSelectByIntensity = true;
        [SerializeField] private float m_intensityCheckInterval = 10f;

        [Header("Tracks")]
        [SerializeField] private List<MusicTrack> m_tracks = new List<MusicTrack>();

        // State
        private AudioSource m_currentSource;
        private MusicTrack m_currentTrack;
        private Coroutine m_fadeCoroutine;
        private float m_currentIntensity = 0f;
        private bool m_isBossFight = false;

        public MusicTrack CurrentTrack => m_currentTrack;
        public float Volume => m_masterVolume;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            SetupAudioSources();
            CreateDefaultTracks();
        }

        private void Start()
        {
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
            EventBus.Subscribe<BossSpawnedEvent>(OnBossSpawned);
            EventBus.Subscribe<BossDefeatedEvent>(OnBossDefeated);
            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);

            // Start with menu music
            PlayTrackByName("Menu");
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
            EventBus.Unsubscribe<BossSpawnedEvent>(OnBossSpawned);
            EventBus.Unsubscribe<BossDefeatedEvent>(OnBossDefeated);
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);

        }

        private void SetupAudioSources()
        {
            if (m_sourceA == null)
            {
                var goA = new GameObject("MusicSourceA");
                goA.transform.SetParent(transform);
                m_sourceA = goA.AddComponent<AudioSource>();
                m_sourceA.loop = true;
                m_sourceA.playOnAwake = false;
                m_sourceA.volume = 0;
            }

            if (m_sourceB == null)
            {
                var goB = new GameObject("MusicSourceB");
                goB.transform.SetParent(transform);
                m_sourceB = goB.AddComponent<AudioSource>();
                m_sourceB.loop = true;
                m_sourceB.playOnAwake = false;
                m_sourceB.volume = 0;
            }

            m_currentSource = m_sourceA;
        }

        private void CreateDefaultTracks()
        {
            // Create procedural music tracks if none assigned
            if (m_tracks.Count == 0)
            {
                m_tracks.Add(new MusicTrack { name = "Menu", bpm = 90, intensity = 0f });
                m_tracks.Add(new MusicTrack { name = "Ambient", bpm = 100, intensity = 0.2f });
                m_tracks.Add(new MusicTrack { name = "Action", bpm = 128, intensity = 0.5f });
                m_tracks.Add(new MusicTrack { name = "Intense", bpm = 140, intensity = 0.8f });
                m_tracks.Add(new MusicTrack { name = "Boss", bpm = 150, intensity = 1f, isBossTrack = true });
                m_tracks.Add(new MusicTrack { name = "Victory", bpm = 120, intensity = 0.3f });
                m_tracks.Add(new MusicTrack { name = "GameOver", bpm = 80, intensity = 0.1f });

                // Generate procedural audio for each track
                foreach (var track in m_tracks)
                {
                    track.clip = GenerateProceduralTrack(track);
                }

                Debug.Log("[MusicManager] Created procedural music tracks");
            }
        }

        private AudioClip GenerateProceduralTrack(MusicTrack track)
        {
            int sampleRate = 44100;
            float duration = 16f; // 16 second loop
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);

            AudioClip clip = AudioClip.Create(track.name, sampleCount, 1, sampleRate, false);
            float[] samples = new float[sampleCount];

            float beatDuration = 60f / track.bpm;
            int samplesPerBeat = Mathf.RoundToInt(sampleRate * beatDuration);

            // Generate based on track intensity
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float beatPhase = (i % samplesPerBeat) / (float)samplesPerBeat;
                int beatNumber = i / samplesPerBeat;

                float sample = 0f;

                // Bass drum on beats 1 and 3
                if (beatNumber % 4 == 0 || beatNumber % 4 == 2)
                {
                    float kickEnv = Mathf.Exp(-beatPhase * 20f);
                    float kickFreq = 60f + kickEnv * 40f;
                    sample += Mathf.Sin(2f * Mathf.PI * kickFreq * t) * kickEnv * 0.4f;
                }

                // Hi-hat (more frequent with higher intensity)
                if (track.intensity > 0.3f)
                {
                    float hatEnv = Mathf.Exp(-beatPhase * 50f);
                    if (beatNumber % 2 == 1 || (track.intensity > 0.6f && beatNumber % 1 == 0))
                    {
                        sample += (Random.value * 2f - 1f) * hatEnv * 0.1f * track.intensity;
                    }
                }

                // Snare on beats 2 and 4
                if (beatNumber % 4 == 1 || beatNumber % 4 == 3)
                {
                    float snareEnv = Mathf.Exp(-beatPhase * 15f);
                    sample += (Random.value * 2f - 1f) * snareEnv * 0.2f;
                    sample += Mathf.Sin(2f * Mathf.PI * 200f * t) * snareEnv * 0.15f;
                }

                // Bass synth
                float bassFreq = track.isBossTrack ? 55f : 65f;
                int bassPattern = beatNumber % 8;
                float bassNote = bassFreq * (bassPattern < 4 ? 1f : 1.5f);
                float bassEnv = Mathf.Exp(-beatPhase * 5f);
                sample += Mathf.Sin(2f * Mathf.PI * bassNote * t) * bassEnv * 0.25f * (0.5f + track.intensity * 0.5f);

                // Pad/atmosphere
                float padFreq = track.isBossTrack ? 220f : 330f;
                float pad = Mathf.Sin(2f * Mathf.PI * padFreq * t) * 0.05f;
                pad += Mathf.Sin(2f * Mathf.PI * padFreq * 1.5f * t) * 0.03f;
                pad += Mathf.Sin(2f * Mathf.PI * padFreq * 2f * t) * 0.02f;
                sample += pad * (1f - track.intensity * 0.5f);

                // Arpeggio for higher intensity
                if (track.intensity > 0.5f)
                {
                    int arpStep = (int)(t * track.bpm / 15f) % 4;
                    float[] arpNotes = { 440f, 550f, 660f, 550f };
                    float arpFreq = arpNotes[arpStep];
                    float arpEnv = Mathf.Exp(-(t * track.bpm / 15f % 1f) * 10f);
                    sample += Mathf.Sin(2f * Mathf.PI * arpFreq * t) * arpEnv * 0.1f * track.intensity;
                }

                // Boss track special effects
                if (track.isBossTrack)
                {
                    float wobble = Mathf.Sin(t * 4f) * 0.5f + 0.5f;
                    sample *= 0.8f + wobble * 0.2f;
                }

                samples[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            clip.SetData(samples, 0);
            return clip;
        }

        #region Event Handlers

        private void OnGameStarted(GameStartedEvent evt)
        {
            m_isBossFight = false;
            m_currentIntensity = 0.3f;
            PlayTrackByIntensity(m_currentIntensity);
            StartCoroutine(IntensityMonitor());
        }

        private void OnGameOver(GameOverEvent evt)
        {
            StopCoroutine(nameof(IntensityMonitor));
            PlayTrackByName("GameOver");
        }

        private void OnBossSpawned(BossSpawnedEvent evt)
        {
            m_isBossFight = true;
            PlayBossMusic();
        }

        private void OnBossDefeated(BossDefeatedEvent evt)
        {
            m_isBossFight = false;
            PlayTrackByIntensity(m_currentIntensity);
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            // Increase intensity with level
            m_currentIntensity = Mathf.Clamp01(evt.levelNumber / 50f);

            if (!m_isBossFight && m_autoSelectByIntensity)
            {
                PlayTrackByIntensity(m_currentIntensity);
            }
        }

        #endregion

        #region Playback Control

        /// <summary>
        /// Play a track by name
        /// </summary>
        public void PlayTrackByName(string trackName)
        {
            var track = m_tracks.Find(t => t.name == trackName);
            if (track != null)
            {
                PlayTrack(track);
            }
            else
            {
                Debug.LogWarning($"[MusicManager] Track not found: {trackName}");
            }
        }

        /// <summary>
        /// Play track matching intensity level
        /// </summary>
        public void PlayTrackByIntensity(float intensity)
        {
            MusicTrack bestTrack = null;
            float bestDiff = float.MaxValue;

            foreach (var track in m_tracks)
            {
                if (track.isBossTrack) continue;
                if (track.name == "Menu" || track.name == "Victory" || track.name == "GameOver") continue;

                float diff = Mathf.Abs(track.intensity - intensity);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestTrack = track;
                }
            }

            if (bestTrack != null && bestTrack != m_currentTrack)
            {
                PlayTrack(bestTrack);
            }
        }

        /// <summary>
        /// Play boss music
        /// </summary>
        public void PlayBossMusic()
        {
            var bossTrack = m_tracks.Find(t => t.isBossTrack);
            if (bossTrack != null)
            {
                PlayTrack(bossTrack);
            }
        }

        /// <summary>
        /// Play a specific track with crossfade
        /// </summary>
        public void PlayTrack(MusicTrack track)
        {
            if (track == m_currentTrack) return;

            m_currentTrack = track;

            if (m_fadeCoroutine != null)
            {
                StopCoroutine(m_fadeCoroutine);
            }
            m_fadeCoroutine = StartCoroutine(CrossfadeToTrack(track));
        }

        private IEnumerator CrossfadeToTrack(MusicTrack track)
        {
            AudioSource fadeOut = m_currentSource;
            AudioSource fadeIn = (m_currentSource == m_sourceA) ? m_sourceB : m_sourceA;

            fadeIn.clip = track.clip;
            fadeIn.Play();

            float elapsed = 0f;
            float startVolumeOut = fadeOut.volume;

            while (elapsed < m_crossfadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / m_crossfadeDuration;

                fadeOut.volume = Mathf.Lerp(startVolumeOut, 0, t);
                fadeIn.volume = Mathf.Lerp(0, m_masterVolume, t);

                yield return null;
            }

            fadeOut.Stop();
            fadeOut.volume = 0;
            fadeIn.volume = m_masterVolume;

            m_currentSource = fadeIn;
            m_fadeCoroutine = null;

            Debug.Log($"[MusicManager] Now playing: {track.name}");
        }

        private IEnumerator IntensityMonitor()
        {
            while (true)
            {
                yield return new WaitForSeconds(m_intensityCheckInterval);

                if (!m_isBossFight && m_autoSelectByIntensity && GameManager.Instance != null && GameManager.Instance.IsPlaying)
                {
                    // Calculate current game intensity based on enemy count
                    // Use GameObject.FindGameObjectsWithTag for better performance
                    float enemyIntensity = 0f;
                    var enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
                    if (enemyObjects != null && enemyObjects.Length > 0)
                    {
                        // More enemies = higher intensity
                        enemyIntensity = Mathf.Clamp01(enemyObjects.Length / 30f);
                    }

                    // Blend level-based and enemy-based intensity
                    float targetIntensity = Mathf.Lerp(m_currentIntensity, enemyIntensity, 0.5f);
                    m_currentIntensity = Mathf.MoveTowards(m_currentIntensity, targetIntensity, 0.1f);

                    PlayTrackByIntensity(m_currentIntensity);
                }
            }
        }

        /// <summary>
        /// Set master volume
        /// </summary>
        public void SetVolume(float volume)
        {
            m_masterVolume = Mathf.Clamp01(volume);
            if (m_currentSource != null)
            {
                m_currentSource.volume = m_masterVolume;
            }
        }

        /// <summary>
        /// Pause music
        /// </summary>
        public void Pause()
        {
            m_sourceA.Pause();
            m_sourceB.Pause();
        }

        /// <summary>
        /// Resume music
        /// </summary>
        public void Resume()
        {
            if (m_currentSource != null)
            {
                m_currentSource.UnPause();
            }
        }

        /// <summary>
        /// Stop all music
        /// </summary>
        public void Stop()
        {
            m_sourceA.Stop();
            m_sourceB.Stop();
            m_currentTrack = null;
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Play Menu")]
        private void DebugPlayMenu() => PlayTrackByName("Menu");

        [ContextMenu("Debug: Play Action")]
        private void DebugPlayAction() => PlayTrackByName("Action");

        [ContextMenu("Debug: Play Boss")]
        private void DebugPlayBoss() => PlayBossMusic();

        [ContextMenu("Debug: Next Track")]
        private void DebugNextTrack()
        {
            int currentIndex = m_tracks.IndexOf(m_currentTrack);
            int nextIndex = (currentIndex + 1) % m_tracks.Count;
            PlayTrack(m_tracks[nextIndex]);
        }

        #endregion
    }
}
