using UnityEngine;
using NeuralBreak.Entities;

namespace NeuralBreak.Combat.ProjectileBehaviors
{
    /// <summary>
    /// Projectile behavior: tracks and follows enemies.
    /// Prioritizes enemies in the player's aim direction, then locks on.
    /// </summary>
    public class HomingBehavior : ProjectileBehaviorBase
    {
        private float m_strength;
        private float m_range;
        private Vector2 m_direction;
        private Vector2 m_initialDirection;
        private Transform m_lockedTarget;
        private EnemyBase m_lockedEnemy;
        private float m_reacquireDelay = 0.1f;
        private float m_reacquireTimer;

        // Shared static buffer for NonAlloc physics queries
        private static readonly Collider2D[] s_colliderBuffer = new Collider2D[64];
        private static readonly ContactFilter2D s_noFilter = ContactFilter2D.noFilter;

        // Cone angle for prioritizing targets in aim direction (degrees)
        private const float AIM_CONE_ANGLE = 90f;
        private const float AIM_PRIORITY_MULTIPLIER = 0.5f; // Enemies in cone get distance halved

        // Close-range tracking boost: when closer than this, turn rate is amplified
        private const float CLOSE_RANGE_THRESHOLD = 3f;
        private const float CLOSE_RANGE_BOOST = 3f;

        public HomingBehavior(float strength = 8f, float range = 15f)
        {
            m_strength = strength;
            m_range = range;
        }

        /// <summary>
        /// Reset parameters for reuse (zero allocation).
        /// </summary>
        public void Reset(float strength, float range = 15f)
        {
            m_strength = strength;
            m_range = range;
        }

        public override void Initialize(MonoBehaviour proj)
        {
            base.Initialize(proj);
            m_direction = proj.transform.up.normalized;
            m_initialDirection = m_direction;
            m_lockedTarget = null;
            m_lockedEnemy = null;
            m_reacquireTimer = 0f;

            // Acquire initial target immediately
            AcquireTarget();
        }

        public override void Update(float deltaTime)
        {
            // Check if we need to reacquire target
            if (m_lockedTarget == null || !IsTargetValid(m_lockedTarget))
            {
                m_reacquireTimer -= deltaTime;
                if (m_reacquireTimer <= 0f)
                {
                    AcquireTarget();
                    m_reacquireTimer = m_reacquireDelay;
                }
            }

            // If we have a valid target, home toward it
            if (m_lockedTarget != null && IsTargetValid(m_lockedTarget))
            {
                Vector2 toTarget = (Vector2)m_lockedTarget.position - (Vector2)transform.position;
                float distToTarget = toTarget.magnitude;
                Vector2 toTargetDir = distToTarget > 0.01f ? toTarget / distToTarget : m_direction;

                // Boost turn rate when close to target to prevent fly-by misses
                float effectiveStrength = m_strength;
                if (distToTarget < CLOSE_RANGE_THRESHOLD)
                {
                    float closeness = 1f - (distToTarget / CLOSE_RANGE_THRESHOLD);
                    effectiveStrength += m_strength * CLOSE_RANGE_BOOST * closeness;
                }

                m_direction = Vector2.Lerp(m_direction, toTargetDir, effectiveStrength * deltaTime).normalized;
            }
            // else: maintain current direction (fly straight)

            // Update projectile rotation to face direction
            float angle = Mathf.Atan2(m_direction.y, m_direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // Update velocity
            var enhancedProj = GetAsEnhancedProjectile();
            if (enhancedProj != null)
            {
                enhancedProj.SetDirection(m_direction);
            }
            else
            {
                var basicProj = GetAsProjectile();
                if (basicProj != null)
                {
                    basicProj.SetDirection(m_direction);
                }
            }
        }

        public override bool OnHitEnemy(EnemyBase enemy)
        {
            // If we hit our locked target, clear it so we can acquire a new one
            if (m_lockedTarget != null && enemy.transform == m_lockedTarget)
            {
                m_lockedTarget = null;
                m_lockedEnemy = null;
            }
            return true;
        }

        private void AcquireTarget()
        {
            m_lockedTarget = FindBestTarget();
            if (m_lockedTarget != null)
                m_lockedTarget.TryGetComponent<EnemyBase>(out m_lockedEnemy);
            else
                m_lockedEnemy = null;
        }

        private bool IsTargetValid(Transform target)
        {
            if (target == null) return false;

            // Check if still in range
            float dist = Vector2.Distance(transform.position, target.position);
            if (dist > m_range * 1.5f) return false; // Allow some buffer

            // Check if enemy is still alive (use cached ref - zero allocation)
            if (m_lockedEnemy == null || !m_lockedEnemy.IsAlive) return false;

            return true;
        }

        /// <summary>
        /// Find the best target, prioritizing enemies in the aim direction.
        /// </summary>
        private Transform FindBestTarget()
        {
            Transform bestTarget = null;
            float bestScore = float.MaxValue;

            int count = Physics2D.OverlapCircle(transform.position, m_range, s_noFilter, s_colliderBuffer);

            for (int i = 0; i < count; i++)
            {
                var col = s_colliderBuffer[i];
                if (!col.CompareTag("Enemy")) continue;

                if (!col.TryGetComponent<EnemyBase>(out var enemy) || !enemy.IsAlive) continue;

                Vector2 toEnemy = (Vector2)col.transform.position - (Vector2)transform.position;
                float dist = toEnemy.magnitude;

                if (dist < 0.1f) continue; // Too close

                // Calculate angle between aim direction and enemy direction
                Vector2 toEnemyDir = toEnemy.normalized;
                float dot = Vector2.Dot(m_initialDirection, toEnemyDir);
                float angleDeg = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;

                // Score = distance, with bonus for enemies in aim cone
                float score = dist;
                if (angleDeg <= AIM_CONE_ANGLE)
                {
                    // Prioritize enemies in the aim cone
                    score *= AIM_PRIORITY_MULTIPLIER;
                }
                else if (angleDeg > 120f)
                {
                    // Penalize enemies far behind the projectile
                    score *= 1.5f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = col.transform;
                }
            }

            return bestTarget;
        }
    }
}
