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
        private ChaosWorm _parentWorm;
        private CircleCollider2D _collider;

        public void Initialize(ChaosWorm parent)
        {
            _parentWorm = parent;

            // Ensure collider is set up as trigger
            _collider = GetComponent<CircleCollider2D>();
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<CircleCollider2D>();
            }
            _collider.isTrigger = true;
            _collider.radius = 0.4f;

            // Tag as enemy for projectile detection
            gameObject.tag = "Enemy";
        }

        /// <summary>
        /// Forward damage to parent worm.
        /// Called by Projectile/EnhancedProjectile OnTriggerEnter2D.
        /// </summary>
        public void TakeDamage(int damage, Vector2 hitPosition)
        {
            if (_parentWorm != null && _parentWorm.IsAlive)
            {
                _parentWorm.TakeDamage(damage, hitPosition);
            }
        }
    }
}
