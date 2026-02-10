using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Fizzer - Fast, erratic, high-speed chaos enemy.
    /// Very fast movement with unpredictable zigzag patterns.
    /// Based on TypeScript Fizzer.ts.
    ///
    /// Stats: HP=1 (GLASS CANNON), Speed=8.0 (VERY FAST), Damage=51, XP=15
    /// Burst Fire: 2 shots, 3.0s between bursts, 0.2s between shots
    /// Death Damage: 15 in 2.0 radius (electric explosion)
    /// Design: Fast and dangerous but dies in one hit - high risk/reward
    /// </summary>
    public class Fizzer : EnemyBase
    {
        public override EnemyType EnemyType => EnemyType.Fizzer;

        [Header("Fizzer Settings")]
        [SerializeField] private float m_directionChangeInterval = 0.15f;
        [SerializeField] private float m_zigzagAmplitude = 3f;
        [SerializeField] private float m_zigzagFrequency = 8f;

        [Header("Death Explosion")]
        [SerializeField] private float m_deathDamageRadius = 2f;
        [SerializeField] private int m_deathDamageAmount = 15;

        // Config-driven shooting values
        private float m_burstCooldown => EnemyConfig?.fireRate ?? 3f;
        private int m_burstCount => EnemyConfig?.burstCount ?? 2;
        private float m_burstDelay => EnemyConfig?.burstDelay ?? 0.2f;
        private float m_projectileSpeed => EnemyConfig?.projectileSpeed ?? 9f;
        private int m_projectileDamage => EnemyConfig?.projectileDamage ?? 6;

        [Header("Visual")]
        // m_spriteRenderer inherited from EnemyBase (protected field)
        [SerializeField] private TrailRenderer m_trailRenderer;
        [SerializeField] private FizzerVisuals m_visuals;
        [SerializeField] private Color m_electricColor = new Color(0.2f, 0.8f, 1f); // Electric cyan-blue

        [Header("Vapor Trail")]
        [SerializeField] private bool m_enableVaporTrail = true;
        private ParticleSystem m_vaporParticles;

        [Header("Audio")]
        [SerializeField] private AudioSource m_audioSource;
        private AudioClip m_sparkleSound;
        private AudioClip m_fireSound;

        // Note: MMFeedbacks removed

        // Movement state
        private Vector2 m_currentDirection;
        private float m_directionTimer;
        private float m_zigzagOffset;
        private float m_zigzagPhase;

        // Attack state
        private float m_burstTimer;
        private bool m_isFiringBurst;
        private bool m_visualsGenerated;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            m_currentDirection = GetDirectionToPlayer();
            m_directionTimer = 0f;
            m_zigzagOffset = Random.Range(0f, Mathf.PI * 2f); // Random start phase
            m_zigzagPhase = 0f;
            m_burstTimer = m_burstCooldown * Random.Range(0.3f, 0.7f); // Random initial delay
            m_isFiringBurst = false;

            // Ensure audio source
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
                m_audioSource.spatialBlend = 0.5f; // Partial 3D sound
                m_audioSource.volume = 0.4f;
                m_audioSource.minDistance = 5f;
                m_audioSource.maxDistance = 30f;
            }

            // Generate sparkly sounds if not already created
            if (m_sparkleSound == null)
            {
                m_sparkleSound = GenerateSparkleSound();
            }
            if (m_fireSound == null)
            {
                m_fireSound = GenerateFireSound();
            }

            // Play spawn sparkle sound
            if (m_sparkleSound != null && m_audioSource != null)
            {
                m_audioSource.pitch = Random.Range(0.95f, 1.05f);
                m_audioSource.PlayOneShot(m_sparkleSound, 0.6f);
            }

            // Setup trail renderer with material and color
            if (m_trailRenderer != null)
            {
                // Use VFXHelpers to create proper material with texture and blending
                var trailMat = Graphics.VFX.VFXHelpers.CreateParticleMaterial(
                    m_electricColor,
                    emissionIntensity: 1.5f,
                    additive: true // Additive for glowing trail
                );

                if (trailMat != null)
                {
                    trailMat.name = "FizzerTrail_Runtime";
                    m_trailRenderer.sharedMaterial = trailMat;
                }
                else
                {
                    Debug.LogWarning("[Fizzer] Failed to create trail material via VFXHelpers");
                }

                m_trailRenderer.startColor = m_electricColor;
                m_trailRenderer.endColor = new Color(m_electricColor.r, m_electricColor.g, m_electricColor.b, 0f);
                m_trailRenderer.time = 0.3f;
                m_trailRenderer.widthMultiplier = 0.2f;
            }

            // Generate procedural visuals if not yet done
            if (!m_visualsGenerated)
            {
                EnsureVisuals();
                m_visualsGenerated = true;
            }

            // Setup vapor trail particles
            if (m_enableVaporTrail)
            {
                EnsureVaporTrail();
            }
        }

        private void EnsureVaporTrail()
        {
            if (m_vaporParticles != null) return;

            // Create vapor trail particle system
            var vaporGO = new GameObject("VaporTrail");
            vaporGO.transform.SetParent(transform, false);
            vaporGO.transform.localPosition = Vector3.zero;

            m_vaporParticles = vaporGO.AddComponent<ParticleSystem>();

            var main = m_vaporParticles.main;
            main.startLifetime = 0.4f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startColor = new Color(m_electricColor.r, m_electricColor.g, m_electricColor.b, 0.4f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 50;

            var emission = m_vaporParticles.emission;
            emission.rateOverTime = 30f;

            var shape = m_vaporParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var colorOverLifetime = m_vaporParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(m_electricColor, 0f),
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(m_electricColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.5f, 0f),
                    new GradientAlphaKey(0.3f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = m_vaporParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0f)
            ));

            // Set up renderer with proper URP particle material
            var renderer = vaporGO.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Try to load existing particle material, or create one with proper shader
            var particleMat = Resources.Load<Material>("Materials/VFX/ParticleAdditive");
            if (particleMat != null)
            {
                renderer.material = particleMat;
            }
            else
            {
                // Fallback: try multiple shader names for URP compatibility
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                          ?? Shader.Find("Particles/Standard Unlit")
                          ?? Shader.Find("Sprites/Default");

                if (shader != null)
                {
                    var mat = Graphics.VFX.VFXHelpers.CreateParticleMaterial(
                        Color.white,
                        emissionIntensity: 1f,
                        additive: true // Fizzer trail uses additive
                    );
                    if (mat != null)
                    {
                        renderer.material = mat;
                    }
                }
            }
            renderer.sortingOrder = -1;
        }

        private void EnsureVisuals()
        {
            if (m_visuals == null)
            {
                m_visuals = GetComponentInChildren<FizzerVisuals>();
            }

            if (m_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                m_visuals = visualsGO.AddComponent<FizzerVisuals>();
            }
        }

        protected override void UpdateAI()
        {
            UpdateMovement();
            UpdateAttack();
        }

        private void UpdateMovement()
        {
            // Change direction periodically for erratic behavior
            m_directionTimer += Time.deltaTime;
            if (m_directionTimer >= m_directionChangeInterval)
            {
                UpdateDirection();
                m_directionTimer = 0f;
            }

            // Calculate zigzag offset perpendicular to movement
            m_zigzagPhase += Time.deltaTime * m_zigzagFrequency;
            float zigzag = Mathf.Sin(m_zigzagPhase + m_zigzagOffset) * m_zigzagAmplitude;

            // Perpendicular direction for zigzag
            Vector2 perpendicular = new Vector2(-m_currentDirection.y, m_currentDirection.x);

            // Combined movement: toward player + zigzag
            Vector2 movement = (m_currentDirection * m_speed + perpendicular * zigzag) * Time.deltaTime;
            transform.position = (Vector2)transform.position + movement;

            // Visual rotation based on velocity
            if (movement.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }
        }

        private void UpdateDirection()
        {
            // Mostly toward player, but with random variation
            Vector2 toPlayer = GetDirectionToPlayer();

            // Add random offset for unpredictability
            float randomAngle = Random.Range(-45f, 45f) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(randomAngle);
            float sin = Mathf.Sin(randomAngle);

            m_currentDirection = new Vector2(
                toPlayer.x * cos - toPlayer.y * sin,
                toPlayer.x * sin + toPlayer.y * cos
            ).normalized;
        }

        private void UpdateAttack()
        {
            if (m_isFiringBurst) return;

            m_burstTimer += Time.deltaTime;
            if (m_burstTimer >= m_burstCooldown)
            {
                StartCoroutine(FireBurst());
                m_burstTimer = 0f;
            }
        }

        private System.Collections.IEnumerator FireBurst()
        {
            m_isFiringBurst = true;
            // Feedback (Feel removed)

            for (int i = 0; i < m_burstCount; i++)
            {
                FireProjectile();

                if (i < m_burstCount - 1)
                {
                    yield return new WaitForSeconds(m_burstDelay);
                }
            }

            m_isFiringBurst = false;
        }

        private void FireProjectile()
        {
            if (EnemyProjectilePool.Instance == null) return;

            Vector2 direction = GetDirectionToPlayer();
            Vector2 firePos = (Vector2)transform.position + direction * 0.3f;

            EnemyProjectilePool.Instance.Fire(
                firePos,
                direction,
                m_projectileSpeed,
                m_projectileDamage,
                m_electricColor
            );

            // Play electric zap sound
            if (m_fireSound != null && m_audioSource != null)
            {
                m_audioSource.pitch = Random.Range(0.9f, 1.1f);
                m_audioSource.PlayOneShot(m_fireSound, 0.5f);
            }
        }

        public override void Kill()
        {
            // Electric death explosion
            // Feedback (Feel removed)

            // Damage nearby enemies
            DealDeathDamage();

            base.Kill();
        }

        // Cached array for overlap checks (zero allocation)
        private static Collider2D[] s_hitBuffer = new Collider2D[32];

        private void DealDeathDamage()
        {
            // Find all enemies in radius and damage them (zero allocation via buffer)
            int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, m_deathDamageRadius, s_hitBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                if (s_hitBuffer[i].gameObject == gameObject) continue;

                EnemyBase enemy = s_hitBuffer[i].GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(m_deathDamageAmount, transform.position);
                }
            }
        }

        protected override void OnStateChanged(EnemyState newState)
        {
            base.OnStateChanged(newState);

            // Control vapor trail based on state
            if (m_vaporParticles != null)
            {
                var emission = m_vaporParticles.emission;
                emission.enabled = (newState == EnemyState.Alive);
            }

            if (m_trailRenderer != null)
            {
                m_trailRenderer.enabled = (newState == EnemyState.Alive);
            }

            if (m_spriteRenderer == null) return;

            switch (newState)
            {
                case EnemyState.Spawning:
                    m_spriteRenderer.color = new Color(m_electricColor.r, m_electricColor.g, m_electricColor.b, 0.5f);
                    break;
                case EnemyState.Alive:
                    m_spriteRenderer.color = m_electricColor;
                    break;
                case EnemyState.Dying:
                    m_spriteRenderer.color = Color.white;
                    break;
            }
        }

        public override void OnReturnToPool()
        {
            base.OnReturnToPool();

            // Clear vapor trail
            if (m_vaporParticles != null)
            {
                m_vaporParticles.Clear();
            }

            // Clear trail renderer
            if (m_trailRenderer != null)
            {
                m_trailRenderer.Clear();
            }
        }

        /// <summary>
        /// Generate procedural sparkly electric sound for Fizzer
        /// </summary>
        private AudioClip GenerateSparkleSound()
        {
            int sampleRate = 44100;
            float duration = 0.3f;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            // Create sparkly electric sound with multiple frequency components
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;

                // High-frequency sparkle (3-6 kHz range with FM modulation)
                float sparkle1 = Mathf.Sin(2f * Mathf.PI * 3500f * t + 8f * Mathf.Sin(2f * Mathf.PI * 12f * t));
                float sparkle2 = Mathf.Sin(2f * Mathf.PI * 5200f * t + 5f * Mathf.Sin(2f * Mathf.PI * 18f * t));
                float sparkle3 = Mathf.Sin(2f * Mathf.PI * 4100f * t + 6f * Mathf.Sin(2f * Mathf.PI * 25f * t));

                // Add some noise for crackle
                float noise = Random.Range(-0.15f, 0.15f);

                // Combine with exponential decay envelope
                float envelope = Mathf.Exp(-8f * t);
                samples[i] = (sparkle1 * 0.3f + sparkle2 * 0.25f + sparkle3 * 0.2f + noise * 0.25f) * envelope * 0.4f;
            }

            AudioClip clip = AudioClip.Create("FizzerSparkle", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generate quick electric zap sound for firing
        /// </summary>
        private AudioClip GenerateFireSound()
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            int sampleCount = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;

                // Sharp electric zap (higher frequency)
                float zap = Mathf.Sin(2f * Mathf.PI * 7000f * t + 10f * Mathf.Sin(2f * Mathf.PI * 30f * t));

                // Quick attack/decay envelope
                float envelope = Mathf.Exp(-25f * t);
                samples[i] = zap * envelope * 0.3f;
            }

            AudioClip clip = AudioClip.Create("FizzerFire", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Death damage radius
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, m_deathDamageRadius);
        }
    }
}
