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
    /// Stats: HP=2, Speed=8.0 (VERY FAST), Damage=6, XP=15
    /// Burst Fire: 2 shots, 3.0s between bursts, 0.2s between shots
    /// Death Damage: 15 in 2.0 radius (electric explosion)
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
        [SerializeField] private SpriteRenderer m_spriteRenderer;
        [SerializeField] private TrailRenderer m_trailRenderer;
        [SerializeField] private FizzerVisuals m_visuals;
        [SerializeField] private Color m_electricColor = new Color(0.2f, 0.8f, 1f); // Electric cyan-blue

        [Header("Vapor Trail")]
        [SerializeField] private bool m_enableVaporTrail = true;
        private ParticleSystem m_vaporParticles;

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

            // Setup trail renderer with material and color
            if (m_trailRenderer != null)
            {
                // Load trail material if not assigned
                if (m_trailRenderer.sharedMaterial == null)
                {
                    var trailMaterial = Resources.Load<Material>("Materials/VFX/FizzerTrail");
                    if (trailMaterial != null)
                    {
                        m_trailRenderer.sharedMaterial = trailMaterial;
                    }
                    else
                    {
                        Debug.LogWarning("[Fizzer] FizzerTrail material not found in Resources!");
                    }
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
                    var mat = new Material(shader);
                    mat.SetColor("_BaseColor", Color.white);
                    // Enable alpha blending
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.SetFloat("_Blend", 0);   // Alpha blend
                    mat.renderQueue = 3000;
                    renderer.material = mat;
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
        }

        public override void Kill()
        {
            // Electric death explosion
            // Feedback (Feel removed)

            // Damage nearby enemies
            DealDeathDamage();

            base.Kill();
        }

        private void DealDeathDamage()
        {
            // Find all enemies in radius and damage them
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, m_deathDamageRadius);

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                EnemyBase enemy = hit.GetComponent<EnemyBase>();
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

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Death damage radius
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, m_deathDamageRadius);
        }
    }
}
