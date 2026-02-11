using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Entities;
using NeuralBreak.Graphics;
using System.Collections.Generic;
using Z13.Core;

namespace NeuralBreak.Combat.ProjectileBehaviors
{
    /// <summary>
    /// Projectile behavior: creates AOE explosion on impact.
    /// </summary>
    public class ExplosionBehavior : ProjectileBehaviorBase
    {
        private float m_radius;
        private float m_damageMultiplier;

        public ExplosionBehavior(float radius = 2f, float damageMultiplier = 0.7f)
        {
            m_radius = radius;
            m_damageMultiplier = damageMultiplier;
        }

        public override void Initialize(MonoBehaviour proj)
        {
            base.Initialize(proj);
        }

        public override void Update(float deltaTime)
        {
            // No per-frame update needed
        }

        public override bool OnHitEnemy(EnemyBase enemy)
        {
            // Get damage from either projectile type
            var enhancedProj = GetAsEnhancedProjectile();
            var basicProj = GetAsProjectile();
            int currentDamage = enhancedProj != null ? enhancedProj.GetDamage() :
                               (basicProj != null ? basicProj.GetDamage() : 0);

            // Trigger explosion
            CreateExplosion(projectileHost.transform.position, currentDamage);

            // Always destroy on hit (explosion is final)
            return true;
        }

        public override void OnDeactivate()
        {
            // Could trigger explosion on lifetime expire if desired
        }

        private void CreateExplosion(Vector2 center, int projectileDamage)
        {
            // Calculate explosion damage
            int explosionDamage = Mathf.RoundToInt(projectileDamage * m_damageMultiplier);

            // Find all enemies in radius
            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, m_radius);
            HashSet<EnemyBase> hitEnemies = new HashSet<EnemyBase>();

            foreach (var col in colliders)
            {
                if (!col.CompareTag("Enemy")) continue;

                var enemy = col.GetComponent<EnemyBase>();
                if (enemy == null || !enemy.IsAlive) continue;
                if (hitEnemies.Contains(enemy)) continue; // Avoid double-hit

                // Apply damage
                enemy.TakeDamage(explosionDamage, center);
                hitEnemies.Add(enemy);
            }

            // Visual effect
            CreateExplosionVFX(center);

            // Publish event
            EventBus.Publish(new ExplosionTriggeredEvent
            {
                position = center,
                radius = m_radius
            });
        }

        private void CreateExplosionVFX(Vector2 center)
        {
            // ParticleEffectFactory doesn't have CreateBurst - use CreateExplosion instead
            // Or just use Debug.DrawLine as placeholder
            Debug.DrawLine(center, center + Vector2.up * m_radius, new Color(1f, 0.6f, 0.1f), 0.5f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * m_radius;
                Debug.DrawLine(center, center + dir, new Color(1f, 0.6f, 0.1f), 0.5f);
            }
        }
    }
}
