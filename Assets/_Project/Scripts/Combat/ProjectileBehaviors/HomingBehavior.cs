using UnityEngine;
using NeuralBreak.Entities;

namespace NeuralBreak.Combat.ProjectileBehaviors
{
    /// <summary>
    /// Projectile behavior: tracks and follows nearby enemies.
    /// </summary>
    public class HomingBehavior : ProjectileBehaviorBase
    {
        private float _strength;
        private float _range;
        private Vector2 _direction;

        public HomingBehavior(float strength = 5f, float range = 10f)
        {
            _strength = strength;
            _range = range;
        }

        public override void Initialize(MonoBehaviour proj)
        {
            base.Initialize(proj);
            _direction = (proj.transform.up).normalized; // Initial direction
        }

        public override void Update(float deltaTime)
        {
            // Find nearest enemy
            Transform nearestEnemy = FindNearestEnemy();
            if (nearestEnemy == null) return;

            // Calculate direction to enemy
            Vector2 toEnemy = ((Vector2)nearestEnemy.position - (Vector2)transform.position).normalized;

            // Smoothly turn toward enemy
            _direction = Vector2.Lerp(_direction, toEnemy, _strength * deltaTime).normalized;

            // Update projectile rotation to face direction
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // Update velocity - try both projectile types
            var enhancedProj = GetAsEnhancedProjectile();
            if (enhancedProj != null)
            {
                enhancedProj.SetDirection(_direction);
            }
            else
            {
                var basicProj = GetAsProjectile();
                if (basicProj != null)
                {
                    basicProj.SetDirection(_direction);
                }
            }
        }

        public override bool OnHitEnemy(EnemyBase enemy)
        {
            // Don't destroy on hit if piercing is also active
            return true; // Let other behaviors decide
        }

        private Transform FindNearestEnemy()
        {
            Transform nearest = null;
            float nearestDist = _range;

            // Find all colliders in range
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _range);
            foreach (var col in colliders)
            {
                if (!col.CompareTag("Enemy")) continue;

                var enemy = col.GetComponent<EnemyBase>();
                if (enemy == null || !enemy.IsAlive) continue;

                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = col.transform;
                }
            }

            return nearest;
        }
    }
}
