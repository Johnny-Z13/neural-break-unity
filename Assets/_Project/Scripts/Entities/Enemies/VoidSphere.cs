using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// VoidSphere - Massive tank boss enemy.
    /// Very slow but extremely tanky. Fires bursts of projectiles.
    /// Huge death explosion damages nearby enemies.
    /// Based on TypeScript VoidSphere.ts.
    ///
    /// Stats: HP=650 (MASSIVE), Speed=0.5 (very slow), Damage=40, XP=50
    /// Burst Fire: 4 shots, 3.0s between bursts, 0.25s between shots
    /// Death Damage: 50 in 8.0 radius (huge explosion)
    /// </summary>
    public class VoidSphere : EnemyBase
    {
        public override EnemyType EnemyType => EnemyType.VoidSphere;

        [Header("VoidSphere Settings")]
        [SerializeField] private float m_pulsateSpeed = 1f;
        [SerializeField] private float m_pulsateAmount = 0.1f;
        [SerializeField] private float m_gravityPullRadius = 6f;
        [SerializeField] private float m_gravityPullStrength = 2f;

        [Header("Burst Attack")]
        [SerializeField] private float m_spreadAngle = 30f;

        [Header("Death Explosion")]
        [SerializeField] private float m_deathDamageRadius = 8f;
        [SerializeField] private int m_deathDamageAmount = 50;

        // Config-driven shooting values
        private float m_burstCooldown => EnemyConfig?.fireRate ?? 3f;
        private int m_burstCount => EnemyConfig?.burstCount ?? 4;
        private float m_burstDelay => EnemyConfig?.burstDelay ?? 0.25f;
        private float m_projectileSpeed => EnemyConfig?.projectileSpeed ?? 5f;
        private int m_projectileDamage => EnemyConfig?.projectileDamage ?? 20;

        [Header("Visual")]
        // m_spriteRenderer inherited from EnemyBase (protected field)
        [SerializeField] private SpriteRenderer m_innerGlow;
        [SerializeField] private VoidSphereVisuals m_visuals;
        [SerializeField] private Color m_voidColor = new Color(0.2f, 0f, 0.4f); // Deep purple
        [SerializeField] private Color m_glowColor = new Color(0.6f, 0f, 1f); // Purple glow

        [Header("Spawn VFX")]
        [SerializeField] private bool m_enableSpawnVFX = true;
        private ParticleSystem m_spawnVFX;

        [Header("Audio")]
        [SerializeField] private AudioSource m_audioSource;
        private AudioClip m_spawnSound;
        private AudioClip m_ambienceLoop;
        private AudioClip m_fireSound;

        // Note: MMFeedbacks removed

        // Cached component references
        private Rigidbody2D m_playerRb;

        // State
        private float m_burstTimer;
        private bool m_isFiringBurst;
        private float m_pulsatePhase;
        private float m_chargeTimer;
        private bool m_isCharging;
        private bool m_visualsGenerated;
        private float m_baseScale; // Store the config-based scale

        protected override void OnInitialize()
        {
            base.OnInitialize();

            m_burstTimer = m_burstCooldown * 0.5f;
            m_isFiringBurst = false;
            m_pulsatePhase = Random.Range(0f, Mathf.PI * 2f);
            m_isCharging = false;

            // Cache the base scale set by EnemyBase (collisionRadius * 2)
            // Reduced by 10% per user request (2026-02-10)
            m_baseScale = m_collisionRadius * 2f * 0.9f;

            // Generate procedural visuals if not yet done
            if (!m_visualsGenerated)
            {
                EnsureVisuals();
                m_visualsGenerated = true;
            }

            // Create and play spawn manifestation VFX
            if (m_enableSpawnVFX)
            {
                CreateSpawnVFX();
            }

            // Setup audio
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
                m_audioSource.spatialBlend = 0.8f; // Mostly 3D
                m_audioSource.volume = 0.5f;
                m_audioSource.minDistance = 8f;
                m_audioSource.maxDistance = 40f;
                m_audioSource.priority = 100; // High priority for sub-bass
            }

            // Generate sub-bass sounds
            if (m_spawnSound == null)
            {
                m_spawnSound = GenerateSpawnSound();
            }
            if (m_ambienceLoop == null)
            {
                m_ambienceLoop = GenerateAmbienceLoop();
            }
            if (m_fireSound == null)
            {
                m_fireSound = GenerateFireSound();
            }

            // Play spawn sound
            if (m_spawnSound != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(m_spawnSound, 0.8f);
            }

            // Start looping ambience
            if (m_ambienceLoop != null && m_audioSource != null)
            {
                m_audioSource.clip = m_ambienceLoop;
                m_audioSource.loop = true;
                m_audioSource.volume = 0.3f;
                m_audioSource.Play();
            }
        }

        private void EnsureVisuals()
        {
            if (m_visuals == null)
            {
                m_visuals = GetComponentInChildren<VoidSphereVisuals>();
            }

            if (m_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                m_visuals = visualsGO.AddComponent<VoidSphereVisuals>();
            }
        }

        protected override void UpdateAI()
        {
            UpdateMovement();
            UpdatePulsate();
            UpdateAttack();
            ApplyGravityPull();
        }

        private void UpdateMovement()
        {
            // Slow, relentless advance toward player
            Vector2 direction = GetDirectionToPlayer();
            transform.position = (Vector2)transform.position + direction * m_speed * Time.deltaTime;
        }

        private void UpdatePulsate()
        {
            m_pulsatePhase += Time.deltaTime * m_pulsateSpeed;
            float pulseFactor = 1f + Mathf.Sin(m_pulsatePhase) * m_pulsateAmount;
            transform.localScale = Vector3.one * m_baseScale * pulseFactor;

            // Inner glow intensity
            if (m_innerGlow != null)
            {
                float glowIntensity = 0.5f + Mathf.Sin(m_pulsatePhase * 2f) * 0.3f;
                Color glow = m_glowColor;
                glow.a = glowIntensity;
                m_innerGlow.color = glow;
            }
        }

        private void UpdateAttack()
        {
            if (m_isFiringBurst) return;

            m_burstTimer += Time.deltaTime;

            // Start charging before burst
            if (!m_isCharging && m_burstTimer >= m_burstCooldown - 0.5f)
            {
                m_isCharging = true;
                // Feedback (Feel removed)
            }

            if (m_burstTimer >= m_burstCooldown)
            {
                StartCoroutine(FireBurst());
                m_burstTimer = 0f;
                m_isCharging = false;
            }
        }

        private System.Collections.IEnumerator FireBurst()
        {
            m_isFiringBurst = true;
            // Feedback (Feel removed)

            for (int i = 0; i < m_burstCount; i++)
            {
                FireProjectiles();

                if (i < m_burstCount - 1)
                {
                    yield return new WaitForSeconds(m_burstDelay);
                }
            }

            m_isFiringBurst = false;
        }

        private void FireProjectiles()
        {
            if (EnemyProjectilePool.Instance == null) return;

            Vector2 direction = GetDirectionToPlayer();

            // Fire spread of projectiles
            EnemyProjectilePool.Instance.FireSpread(
                transform.position,
                direction,
                m_projectileSpeed,
                m_projectileDamage,
                3, // 3 projectiles per shot
                m_spreadAngle,
                m_glowColor
            );

            // Play deep bass fire sound
            if (m_fireSound != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(m_fireSound, 0.6f);
            }
        }

        private void ApplyGravityPull()
        {
            if (m_playerTarget == null) return;

            // Cache the player's Rigidbody2D (lazy init, only once)
            if (m_playerRb == null)
            {
                m_playerRb = m_playerTarget.GetComponent<Rigidbody2D>();
                if (m_playerRb == null) return;
            }

            // Pull player slightly toward the void sphere when in range
            float distanceToPlayer = GetDistanceToPlayer();
            if (distanceToPlayer <= m_gravityPullRadius && distanceToPlayer > 0.5f)
            {
                // Calculate pull strength (stronger when closer)
                float pullFactor = 1f - (distanceToPlayer / m_gravityPullRadius);
                Vector2 pullDirection = ((Vector2)transform.position - (Vector2)m_playerTarget.position).normalized;

                // Apply subtle pull (reduced by factor to not be too oppressive)
                m_playerRb.AddForce(pullDirection * m_gravityPullStrength * pullFactor, ForceMode2D.Force);
            }
        }

        public override void Kill()
        {
            // Stop ambience loop
            if (m_audioSource != null && m_audioSource.isPlaying)
            {
                m_audioSource.Stop();
            }

            // Massive implosion/explosion
            // Feedback (Feel removed)
            DealDeathDamage();

            // Fire death nova
            if (EnemyProjectilePool.Instance != null)
            {
                EnemyProjectilePool.Instance.FireRing(
                    transform.position,
                    m_projectileSpeed * 1.5f,
                    m_projectileDamage,
                    16, // Big ring of bullets
                    m_glowColor
                );
            }

            base.Kill();
        }

        // Cached array for overlap checks (zero allocation)
        private static Collider2D[] s_hitBuffer = new Collider2D[32];

        private void DealDeathDamage()
        {
            int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, m_deathDamageRadius, s_hitBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                if (s_hitBuffer[i].gameObject == gameObject) continue;

                // Damage enemies
                EnemyBase enemy = s_hitBuffer[i].GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(m_deathDamageAmount, transform.position);
                }

                // Could also push player back here
            }
        }

        protected override void OnStateChanged(EnemyState newState)
        {
            base.OnStateChanged(newState);

            if (m_spriteRenderer == null) return;

            switch (newState)
            {
                case EnemyState.Spawning:
                    m_spriteRenderer.color = new Color(m_voidColor.r, m_voidColor.g, m_voidColor.b, 0.5f);
                    break;
                case EnemyState.Alive:
                    m_spriteRenderer.color = m_voidColor;
                    break;
                case EnemyState.Dying:
                    m_spriteRenderer.color = m_glowColor;
                    break;
            }
        }

        /// <summary>
        /// Create swirly manifestation particle effect for spawn
        /// </summary>
        private void CreateSpawnVFX()
        {
            if (m_spawnVFX != null)
            {
                // Stop completely before clearing and playing again
                m_spawnVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                m_spawnVFX.Clear();
                m_spawnVFX.Play();
                return;
            }

            // Create swirly void manifestation particle system
            var vfxGO = new GameObject("SpawnManifestationVFX");
            vfxGO.transform.SetParent(transform, false);
            vfxGO.transform.localPosition = Vector3.zero;

            m_spawnVFX = vfxGO.AddComponent<ParticleSystem>();
            m_spawnVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Stop initially

            // Main module - swirling inward spiral
            var main = m_spawnVFX.main;
            main.duration = 1.0f;
            main.loop = false;
            main.startLifetime = 1.2f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(m_voidColor, m_glowColor);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 150;
            main.gravityModifier = -2f; // Pull inward

            // Emission - burst on spawn
            var emission = m_spawnVFX.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 80, 120)
            });

            // Shape - sphere shell (particles spawn from outer shell, spiral inward)
            var shape = m_spawnVFX.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = m_baseScale * 2f; // Start from outer radius
            shape.radiusThickness = 0f; // Spawn from surface

            // Velocity over lifetime - create inward spiral
            var velocityOverLifetime = m_spawnVFX.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;

            // Radial velocity (inward)
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(-8f);

            // Orbital velocity (creates swirl)
            AnimationCurve orbitalCurve = new AnimationCurve();
            orbitalCurve.AddKey(0f, 3f);
            orbitalCurve.AddKey(0.5f, 6f);
            orbitalCurve.AddKey(1f, 2f);
            velocityOverLifetime.orbitalX = new ParticleSystem.MinMaxCurve(1f, orbitalCurve);
            velocityOverLifetime.orbitalY = new ParticleSystem.MinMaxCurve(1f, orbitalCurve);
            velocityOverLifetime.orbitalZ = new ParticleSystem.MinMaxCurve(1f, orbitalCurve);

            // Color over lifetime - bright to dark
            var colorOverLifetime = m_spawnVFX.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(m_glowColor, 0f),
                    new GradientColorKey(m_voidColor, 0.5f),
                    new GradientColorKey(Color.black, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(1f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            // Size over lifetime - grow then shrink as spiraling inward
            var sizeOverLifetime = m_spawnVFX.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.5f);
            sizeCurve.AddKey(0.3f, 1.2f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Rotation over lifetime - spin
            var rotationOverLifetime = m_spawnVFX.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

            // Set up renderer with proper material
            var renderer = vfxGO.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Use VFXHelpers for proper particle material
            var particleMat = Graphics.VFX.VFXHelpers.CreateParticleMaterial(
                m_glowColor,
                emissionIntensity: 2f,
                additive: true
            );
            if (particleMat != null)
            {
                renderer.material = particleMat;
            }
            renderer.sortingOrder = 10;

            // Play the effect
            m_spawnVFX.Play();
        }

        /// <summary>
        /// Generate deep sub-bass spawn sound with ominous rumble
        /// </summary>
        private AudioClip GenerateSpawnSound()
        {
            int sampleRate = 44100;
            float duration = 2.0f;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;

                // Deep sub-bass (40-80Hz)
                float bass = Mathf.Sin(2f * Mathf.PI * 45f * t);
                float subBass = Mathf.Sin(2f * Mathf.PI * 60f * t);

                // Very low frequency rumble modulation (3Hz)
                float rumbleModulation = Mathf.Sin(2f * Mathf.PI * 3f * t);

                // Combine with slow attack/release envelope
                float attack = Mathf.Min(1f, t * 3f);
                float release = Mathf.Max(0f, 1f - Mathf.Max(0f, (t - 1.5f) * 2f));
                float envelope = attack * release;

                samples[i] = (bass * 0.6f + subBass * 0.4f) * (1f + rumbleModulation * 0.3f) * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create("VoidSphereSpawn", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generate looping deep ambience for VoidSphere presence
        /// </summary>
        private AudioClip GenerateAmbienceLoop()
        {
            int sampleRate = 44100;
            float duration = 4.0f; // 4-second loop
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;

                // Very low frequency drone (30-50Hz)
                float drone1 = Mathf.Sin(2f * Mathf.PI * 35f * t);
                float drone2 = Mathf.Sin(2f * Mathf.PI * 48f * t);

                // Slow warble modulation (0.5Hz)
                float warble = Mathf.Sin(2f * Mathf.PI * 0.5f * t);

                // Seamless loop (fade in/out at edges)
                float loopFade = 1f;
                if (t < 0.1f)
                {
                    loopFade = t / 0.1f;
                }
                else if (t > duration - 0.1f)
                {
                    loopFade = (duration - t) / 0.1f;
                }

                samples[i] = (drone1 * 0.5f + drone2 * 0.5f) * (1f + warble * 0.2f) * loopFade * 0.3f;
            }

            AudioClip clip = AudioClip.Create("VoidSphereAmbience", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generate deep bass burst sound for firing
        /// </summary>
        private AudioClip GenerateFireSound()
        {
            int sampleRate = 44100;
            float duration = 0.3f;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;

                // Low frequency punch (80-120Hz)
                float punch = Mathf.Sin(2f * Mathf.PI * 100f * t);

                // Quick attack/decay
                float envelope = Mathf.Exp(-8f * t);

                samples[i] = punch * envelope * 0.4f;
            }

            AudioClip clip = AudioClip.Create("VoidSphereFire", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Gravity pull radius
            Gizmos.color = new Color(0.6f, 0f, 1f, 0.2f);
            Gizmos.DrawSphere(transform.position, m_gravityPullRadius);

            // Death damage radius
            Gizmos.color = new Color(1f, 0f, 0.5f, 0.2f);
            Gizmos.DrawSphere(transform.position, m_deathDamageRadius);
        }
    }
}
