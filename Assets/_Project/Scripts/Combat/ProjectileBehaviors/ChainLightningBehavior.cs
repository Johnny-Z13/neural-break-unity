using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Entities;
using System.Collections.Generic;

namespace NeuralBreak.Combat.ProjectileBehaviors
{
    /// <summary>
    /// Projectile behavior: jumps to nearby enemies on hit.
    /// </summary>
    public class ChainLightningBehavior : ProjectileBehaviorBase
    {
        private int m_maxTargets;
        private float m_chainRange;
        private float m_damageMultiplier;
        private HashSet<EnemyBase> m_hitEnemies;

        public ChainLightningBehavior(int maxTargets = 4, float chainRange = 5f, float damageMultiplier = 0.6f)
        {
            m_maxTargets = maxTargets;
            m_chainRange = chainRange;
            m_damageMultiplier = damageMultiplier;
            m_hitEnemies = new HashSet<EnemyBase>();
        }

        public override void Initialize(MonoBehaviour proj)
        {
            base.Initialize(proj);
            m_hitEnemies.Clear();
        }

        public override void Update(float deltaTime)
        {
            // No per-frame update needed
        }

        public override bool OnHitEnemy(EnemyBase enemy)
        {
            if (enemy == null || m_hitEnemies.Contains(enemy)) return true;

            // Add to hit list
            m_hitEnemies.Add(enemy);

            // Chain to nearby enemies
            if (m_hitEnemies.Count < m_maxTargets)
            {
                ChainToNearbyEnemies(enemy.transform.position);
            }

            // Always destroy projectile after initial hit
            return true;
        }

        private void ChainToNearbyEnemies(Vector2 fromPosition)
        {
            List<EnemyBase> chainTargets = new List<EnemyBase>();

            // Find all enemies in chain range
            Collider2D[] colliders = Physics2D.OverlapCircleAll(fromPosition, m_chainRange);
            foreach (var col in colliders)
            {
                if (!col.CompareTag("Enemy")) continue;

                var enemy = col.GetComponent<EnemyBase>();
                if (enemy == null || !enemy.IsAlive) continue;
                if (m_hitEnemies.Contains(enemy)) continue; // Already hit

                chainTargets.Add(enemy);

                // Stop if we have enough targets
                if (m_hitEnemies.Count + chainTargets.Count >= m_maxTargets)
                {
                    break;
                }
            }

            // Apply damage to chain targets
            var enhancedProj = GetAsEnhancedProjectile();
            var basicProj = GetAsProjectile();
            int currentDamage = enhancedProj != null ? enhancedProj.GetDamage() :
                               (basicProj != null ? basicProj.GetDamage() : 0);
            int chainDamage = Mathf.RoundToInt(currentDamage * m_damageMultiplier);
            foreach (var target in chainTargets)
            {
                target.TakeDamage(chainDamage, target.transform.position);
                m_hitEnemies.Add(target);

                // Create visual effect
                CreateChainVFX(fromPosition, target.transform.position);
            }

            // Publish event
            if (chainTargets.Count > 0)
            {
                EventBus.Publish(new ChainLightningEvent
                {
                    targets = chainTargets
                });
            }
        }

        private void CreateChainVFX(Vector2 from, Vector2 to)
        {
            // Create line renderer or particle trail
            // For now, just visual feedback (could use LineRenderer with lifetime)
            Debug.DrawLine(from, to, Color.cyan, 0.2f);
        }
    }
}
