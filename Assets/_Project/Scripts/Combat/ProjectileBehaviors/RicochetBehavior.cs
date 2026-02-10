using UnityEngine;
using NeuralBreak.Entities;

namespace NeuralBreak.Combat.ProjectileBehaviors
{
    /// <summary>
    /// Projectile behavior: bounces off walls and enemies.
    /// </summary>
    public class RicochetBehavior : ProjectileBehaviorBase
    {
        private int m_maxBounces;
        private int m_currentBounces;
        private float m_damageRetention;

        // Cached camera reference (Camera.main allocates ~64 bytes per call via FindGameObjectWithTag)
        private Camera m_camera;

        public RicochetBehavior(int maxBounces = 3, float damageRetention = 0.8f)
        {
            m_maxBounces = maxBounces;
            m_damageRetention = damageRetention;
            m_currentBounces = 0;
        }

        /// <summary>
        /// Reset parameters for reuse (zero allocation).
        /// </summary>
        public void Reset(int maxBounces, float damageRetention = 0.8f)
        {
            m_maxBounces = maxBounces;
            m_damageRetention = damageRetention;
        }

        public override void Initialize(MonoBehaviour proj)
        {
            base.Initialize(proj);
            m_currentBounces = 0;
            m_camera = Camera.main;
        }

        public override void Update(float deltaTime)
        {
            // Check for wall collision (could use raycasts)
            CheckWallBounce();
        }

        public override bool OnHitEnemy(EnemyBase enemy)
        {
            m_currentBounces++;

            // Reduce damage on each bounce (try both projectile types)
            var enhancedProj = GetAsEnhancedProjectile();
            var basicProj = GetAsProjectile();

            int currentDamage = enhancedProj != null ? enhancedProj.GetDamage() :
                               (basicProj != null ? basicProj.GetDamage() : 0);
            int newDamage = Mathf.RoundToInt(currentDamage * m_damageRetention);

            if (enhancedProj != null)
            {
                enhancedProj.SetDamage(newDamage);
            }
            else if (basicProj != null)
            {
                basicProj.SetDamage(newDamage);
            }

            // Bounce off enemy (reverse direction)
            Vector2 bounceDirection = enhancedProj != null ? -enhancedProj.GetDirection() :
                                     (basicProj != null ? -basicProj.GetDirection() : Vector2.zero);

            if (enhancedProj != null)
            {
                enhancedProj.SetDirection(bounceDirection);
            }
            else if (basicProj != null)
            {
                basicProj.SetDirection(bounceDirection);
            }

            // Update rotation
            float angle = Mathf.Atan2(bounceDirection.y, bounceDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // Destroy if max bounces reached
            return m_currentBounces >= m_maxBounces;
        }

        private void CheckWallBounce()
        {
            // Get screen bounds (use cached camera - Camera.main allocates per call)
            if (m_camera == null) return;

            Vector3 pos = transform.position;
            Vector3 viewportPos = m_camera.WorldToViewportPoint(pos);

            // Get current direction (try both projectile types)
            var enhancedProj = GetAsEnhancedProjectile();
            var basicProj = GetAsProjectile();
            Vector2 currentDir = enhancedProj != null ? enhancedProj.GetDirection() :
                                (basicProj != null ? basicProj.GetDirection() : Vector2.zero);

            bool bounced = false;

            // Check horizontal bounds
            if (viewportPos.x < 0f || viewportPos.x > 1f)
            {
                currentDir.x = -currentDir.x;
                bounced = true;
            }

            // Check vertical bounds
            if (viewportPos.y < 0f || viewportPos.y > 1f)
            {
                currentDir.y = -currentDir.y;
                bounced = true;
            }

            if (bounced)
            {
                m_currentBounces++;

                // Set new direction
                if (enhancedProj != null)
                {
                    enhancedProj.SetDirection(currentDir);
                }
                else if (basicProj != null)
                {
                    basicProj.SetDirection(currentDir);
                }

                // Update rotation
                float angle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

                // Deactivate if max bounces reached
                if (m_currentBounces >= m_maxBounces)
                {
                    if (enhancedProj != null)
                    {
                        enhancedProj.Deactivate();
                    }
                    else if (basicProj != null)
                    {
                        basicProj.Deactivate();
                    }
                }
            }
        }
    }
}
