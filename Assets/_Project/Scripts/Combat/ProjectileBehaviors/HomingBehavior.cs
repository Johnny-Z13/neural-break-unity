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
        private Vector2 m_initialDirection; // The direction player was aiming
        private Transform m_lockedTarget;   // Locked target (sticky)
        private float m_reacquireDelay = 0.2f;
        private float m_reacquireTimer;

        // Cone angle for prioritizing targets in aim direction (degrees)
        private const float AIM_CONE_ANGLE = 45f;
        private const float AIM_PRIORITY_MULTIPLIER = 0.5f; // Enemies in cone get distance halved

        public HomingBehavior(float strength = 5f, float range = 10f)
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
                Vector2 toTarget = ((Vector2)m_lockedTarget.position - (Vector2)transform.position).normalized;
                m_direction = Vector2.Lerp(m_direction, toTarget, m_strength * deltaTime).normalized;
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
            }
            return true;
        }

        private void AcquireTarget()
        {
            m_lockedTarget = FindBestTarget();
        }

        private bool IsTargetValid(Transform target)
        {
            if (target == null) return false;

            // Check if still in range
            float dist = Vector2.Distance(transform.position, target.position);
            if (dist > m_range * 1.5f) return false; // Allow some buffer

            // Check if enemy is still alive
            var enemy = target.GetComponent<EnemyBase>();
            if (enemy == null || !enemy.IsAlive) return false;

            return true;
        }

        /// <summary>
        /// Find the best target, prioritizing enemies in the aim direction.
        /// </summary>
        private Transform FindBestTarget()
        {
            Transform bestTarget = null;
            float bestScore = float.MaxValue;

            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, m_range);

            foreach (var col in colliders)
            {
                if (!col.CompareTag("Enemy")) continue;

                var enemy = col.GetComponent<EnemyBase>();
                if (enemy == null || !enemy.IsAlive) continue;

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
                else if (angleDeg > 90f)
                {
                    // Penalize enemies behind the projectile
                    score *= 2f;
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
