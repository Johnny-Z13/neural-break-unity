using UnityEngine;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Worm body segment that forwards damage to parent ChaosWorm.
    /// Ensures projectiles hitting any part of the worm deal damage to the whole creature.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class WormSegment : MonoBehaviour
    {
        private ChaosWorm m_parentWorm;
        private CircleCollider2D m_collider;

        public void Initialize(ChaosWorm parent)
        {
            m_parentWorm = parent;

            // Ensure collider is set up as trigger
            m_collider = GetComponent<CircleCollider2D>();
            if (m_collider == null)
            {
                m_collider = gameObject.AddComponent<CircleCollider2D>();
            }
            m_collider.isTrigger = true;
            m_collider.radius = 0.4f;

            // Tag as enemy for projectile detection
            gameObject.tag = "Enemy";
        }

        /// <summary>
        /// Forward damage to parent worm.
        /// Called by Projectile/EnhancedProjectile OnTriggerEnter2D.
        /// </summary>
        public void TakeDamage(int damage, Vector2 hitPosition)
        {
            if (m_parentWorm != null && m_parentWorm.IsAlive)
            {
                m_parentWorm.TakeDamage(damage, hitPosition);
            }
        }
    }
}
