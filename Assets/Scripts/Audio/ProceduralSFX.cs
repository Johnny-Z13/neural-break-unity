using UnityEngine;

namespace NeuralBreak.Audio
{
    /// <summary>
    /// Generates procedural placeholder sound effects using sine waves.
    /// No external audio assets required.
    /// </summary>
    public static class ProceduralSFX
    {
        private const int SAMPLE_RATE = 44100;

        /// <summary>
        /// Create a shoot/fire sound - high beep (880Hz, 0.1s)
        /// </summary>
        public static AudioClip CreateShoot()
        {
            float duration = 0.1f;
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            AudioClip clip = AudioClip.Create("Shoot", samples, 1, SAMPLE_RATE, false);

            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float envelope = 1f - (t / duration); // Linear decay
                envelope = envelope * envelope; // Exponential decay
                data[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * envelope * 0.5f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create a hit/impact sound - quick thud (440Hz, 0.05s)
        /// </summary>
        public static AudioClip CreateHit()
        {
            float duration = 0.05f;
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            AudioClip clip = AudioClip.Create("Hit", samples, 1, SAMPLE_RATE, false);

            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float envelope = 1f - (t / duration);
                envelope = envelope * envelope * envelope;
                // Mix low frequency with noise
                float tone = Mathf.Sin(2f * Mathf.PI * 440f * t);
                float noise = Random.Range(-0.3f, 0.3f);
                data[i] = (tone * 0.7f + noise) * envelope * 0.6f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create an explosion sound - white noise burst (0.3s)
        /// </summary>
        public static AudioClip CreateExplosion()
        {
            float duration = 0.3f;
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            AudioClip clip = AudioClip.Create("Explosion", samples, 1, SAMPLE_RATE, false);

            float[] data = new float[samples];
            // Use consistent random state for reproducibility
            System.Random rng = new System.Random(12345);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float envelope = 1f - (t / duration);
                envelope = envelope * envelope;

                // White noise with low-frequency rumble
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float rumble = Mathf.Sin(2f * Mathf.PI * 60f * t);
                data[i] = (noise * 0.6f + rumble * 0.4f) * envelope * 0.7f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create a damage/hurt sound - low impact (220Hz, 0.2s)
        /// </summary>
        public static AudioClip CreateDamage()
        {
            float duration = 0.2f;
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            AudioClip clip = AudioClip.Create("Damage", samples, 1, SAMPLE_RATE, false);

            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float envelope = 1f - (t / duration);
                envelope = envelope * envelope;

                // Frequency drops over time for impact feel
                float freq = 220f - (t * 100f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.6f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create a pickup/collect sound - rising chirp (440->880Hz, 0.15s)
        /// </summary>
        public static AudioClip CreatePickup()
        {
            float duration = 0.15f;
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            AudioClip clip = AudioClip.Create("Pickup", samples, 1, SAMPLE_RATE, false);

            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float envelope = 1f - (t / duration) * 0.5f; // Slow decay

                // Rising frequency
                float freq = 440f + (t / duration) * 440f;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create a menu click/UI sound - quick blip (660Hz, 0.05s)
        /// </summary>
        public static AudioClip CreateMenuClick()
        {
            float duration = 0.05f;
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            AudioClip clip = AudioClip.Create("MenuClick", samples, 1, SAMPLE_RATE, false);

            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float envelope = 1f - (t / duration);
                envelope = envelope * envelope;
                data[i] = Mathf.Sin(2f * Mathf.PI * 660f * t) * envelope * 0.3f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create a shield hit sound - metallic ping (1200Hz with harmonics, 0.15s)
        /// </summary>
        public static AudioClip CreateShieldHit()
        {
            float duration = 0.15f;
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            AudioClip clip = AudioClip.Create("ShieldHit", samples, 1, SAMPLE_RATE, false);

            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float envelope = 1f - (t / duration);
                envelope = envelope * envelope * envelope;

                // Multiple harmonics for metallic sound
                float tone1 = Mathf.Sin(2f * Mathf.PI * 1200f * t);
                float tone2 = Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.5f;
                float tone3 = Mathf.Sin(2f * Mathf.PI * 3600f * t) * 0.25f;
                data[i] = (tone1 + tone2 + tone3) * envelope * 0.3f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create a level up sound - triumphant arpeggio (0.4s)
        /// </summary>
        public static AudioClip CreateLevelUp()
        {
            float duration = 0.4f;
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            AudioClip clip = AudioClip.Create("LevelUp", samples, 1, SAMPLE_RATE, false);

            float[] data = new float[samples];
            float[] notes = { 523f, 659f, 784f, 1047f }; // C5, E5, G5, C6
            float noteLength = duration / notes.Length;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                int noteIndex = Mathf.Min((int)(t / noteLength), notes.Length - 1);
                float noteT = t - (noteIndex * noteLength);

                float envelope = Mathf.Clamp01(1f - (noteT / noteLength) * 0.7f);
                data[i] = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t) * envelope * 0.35f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create a game over sound - descending tone (0.6s)
        /// </summary>
        public static AudioClip CreateGameOver()
        {
            float duration = 0.6f;
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            AudioClip clip = AudioClip.Create("GameOver", samples, 1, SAMPLE_RATE, false);

            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float envelope = 1f - (t / duration) * 0.8f;

                // Descending frequency
                float freq = 440f - (t / duration) * 220f;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create an overheat warning beep (0.08s)
        /// </summary>
        public static AudioClip CreateOverheatWarning()
        {
            float duration = 0.08f;
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            AudioClip clip = AudioClip.Create("OverheatWarning", samples, 1, SAMPLE_RATE, false);

            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float envelope = 1f - (t / duration);
                // Alternating frequencies for alarm sound
                float freq = (i % 2000 < 1000) ? 800f : 1000f;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create enemy death sound with pitch variation based on enemy type
        /// </summary>
        public static AudioClip CreateEnemyDeath(int type)
        {
            float duration = 0.25f;
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            AudioClip clip = AudioClip.Create($"EnemyDeath_{type}", samples, 1, SAMPLE_RATE, false);

            // Base frequency varies by type
            float baseFreq = 200f + (type * 80f);
            System.Random rng = new System.Random(type * 1000);

            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float envelope = 1f - (t / duration);
                envelope = envelope * envelope;

                // Descending frequency with noise
                float freq = baseFreq - (t * 150f);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.3f;
                data[i] = (tone * 0.7f + noise) * envelope * 0.5f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Create procedural background music loop (synthwave style, ~8 seconds)
        /// </summary>
        public static AudioClip CreateBackgroundMusic()
        {
            float duration = 8f;
            int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            AudioClip clip = AudioClip.Create("BGM", samples, 1, SAMPLE_RATE, false);

            float[] data = new float[samples];
            float bpm = 120f;
            float beatDuration = 60f / bpm;

            // Minor pentatonic scale for dark/synthwave feel (A minor)
            float[] scale = { 220f, 261.63f, 293.66f, 329.63f, 392f, 440f, 523.25f, 587.33f };

            // Bass pattern (simple octave jump)
            float[] bassPattern = { 0, 0, 5, 5, 0, 0, 3, 3 };

            // Arpeggio pattern
            int[] arpPattern = { 0, 2, 4, 5, 4, 2 };

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float beatPosition = t / beatDuration;
                int currentBeat = (int)(beatPosition) % 8;
                float beatFrac = beatPosition - (int)beatPosition;

                float sample = 0f;

                // Bass (sub-octave, continuous with slight attack)
                int bassNote = (int)bassPattern[currentBeat];
                float bassFreq = scale[bassNote] * 0.5f;
                float bassEnv = Mathf.Min(1f, beatFrac * 8f) * 0.7f;
                sample += Mathf.Sin(2f * Mathf.PI * bassFreq * t) * bassEnv * 0.25f;

                // Kick drum on beats 0 and 4
                if (currentBeat % 4 == 0 && beatFrac < 0.15f)
                {
                    float kickEnv = 1f - (beatFrac / 0.15f);
                    kickEnv = kickEnv * kickEnv;
                    float kickFreq = 80f - (beatFrac * 200f);
                    sample += Mathf.Sin(2f * Mathf.PI * kickFreq * t) * kickEnv * 0.4f;
                }

                // Hi-hat on off-beats
                if (beatFrac < 0.05f && currentBeat % 2 == 1)
                {
                    float hihatEnv = 1f - (beatFrac / 0.05f);
                    // Use deterministic noise based on position
                    float noise = Mathf.Sin(t * 12000f) * 0.5f + Mathf.Sin(t * 17000f) * 0.3f;
                    sample += noise * hihatEnv * 0.1f;
                }

                // Arpeggio (synth lead)
                float arpSpeed = 4f; // 4 notes per beat
                int arpIndex = (int)(beatPosition * arpSpeed) % arpPattern.Length;
                float arpFreq = scale[arpPattern[arpIndex]];
                float arpEnv = 1f - ((beatPosition * arpSpeed) % 1f) * 0.6f;

                // Add slight detuned oscillator for thickness
                float osc1 = Mathf.Sin(2f * Mathf.PI * arpFreq * t);
                float osc2 = Mathf.Sin(2f * Mathf.PI * arpFreq * 1.005f * t);
                sample += (osc1 + osc2) * 0.5f * arpEnv * 0.15f;

                // Pad (chord, pulsed with beat to avoid constant drone)
                float padFreq1 = scale[0];
                float padFreq2 = scale[2];
                float padFreq3 = scale[4];
                float pad = Mathf.Sin(2f * Mathf.PI * padFreq1 * t) +
                           Mathf.Sin(2f * Mathf.PI * padFreq2 * t) * 0.7f +
                           Mathf.Sin(2f * Mathf.PI * padFreq3 * t) * 0.5f;
                // Pulse with beat envelope instead of constant drone
                float padBeatEnv = Mathf.Exp(-beatFrac * 4f) * 0.7f + 0.1f;
                sample += pad * 0.04f * padBeatEnv;

                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
