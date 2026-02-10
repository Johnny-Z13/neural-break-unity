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

        private static readonly Collider2D[] s_colliderBuffer = new Collider2D[64];
        private static readonly ContactFilter2D s_noFilter = ContactFilter2D.noFilter;
        private static readonly HashSet<EnemyBase> s_hitEnemiesBuffer = new HashSet<EnemyBase>();

        public ExplosionBehavior(float radius = 2f, float damageMultiplier = 0.7f)
        {
            m_radius = radius;
            m_damageMultiplier = damageMultiplier;
        }

        /// <summary>
        /// Reset parameters for reuse (zero allocation).
        /// </summary>
        public void Reset(float radius, float damageMultiplier = 0.7f)
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

            // Find all enemies in radius (NonAlloc - zero GC)
            int count = Physics2D.OverlapCircle(center, m_radius, s_noFilter, s_colliderBuffer);
            s_hitEnemiesBuffer.Clear();  // Reuse static buffer

            for (int i = 0; i < count; i++)
            {
                var col = s_colliderBuffer[i];
                if (!col.CompareTag("Enemy")) continue;

                var enemy = col.GetComponent<EnemyBase>();
                if (enemy == null || !enemy.IsAlive) continue;
                if (s_hitEnemiesBuffer.Contains(enemy)) continue; // Avoid double-hit

                // Apply damage
                enemy.TakeDamage(explosionDamage, center);
                s_hitEnemiesBuffer.Add(enemy);
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
