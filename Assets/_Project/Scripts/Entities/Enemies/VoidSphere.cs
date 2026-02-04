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
        [SerializeField] private SpriteRenderer m_spriteRenderer;
        [SerializeField] private SpriteRenderer m_innerGlow;
        [SerializeField] private VoidSphereVisuals m_visuals;
        [SerializeField] private Color m_voidColor = new Color(0.2f, 0f, 0.4f); // Deep purple
        [SerializeField] private Color m_glowColor = new Color(0.6f, 0f, 1f); // Purple glow

        // Note: MMFeedbacks removed

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
            m_baseScale = m_collisionRadius * 2f;

            // Generate procedural visuals if not yet done
            if (!m_visualsGenerated)
            {
                EnsureVisuals();
                m_visualsGenerated = true;
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
        }

        private void ApplyGravityPull()
        {
            if (m_playerTarget == null) return;

            // Pull player slightly toward the void sphere when in range
            float distanceToPlayer = GetDistanceToPlayer();
            if (distanceToPlayer <= m_gravityPullRadius && distanceToPlayer > 0.5f)
            {
                // Calculate pull strength (stronger when closer)
                float pullFactor = 1f - (distanceToPlayer / m_gravityPullRadius);
                Vector2 pullDirection = ((Vector2)transform.position - (Vector2)m_playerTarget.position).normalized;

                // Apply subtle pull (reduced by factor to not be too oppressive)
                var playerRb = m_playerTarget.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.AddForce(pullDirection * m_gravityPullStrength * pullFactor, ForceMode2D.Force);
                }
            }
        }

        public override void Kill()
        {
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

        private void DealDeathDamage()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, m_deathDamageRadius);

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                // Damage enemies
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
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
