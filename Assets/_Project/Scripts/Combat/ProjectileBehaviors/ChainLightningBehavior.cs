using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Entities;
using System.Collections.Generic;
using Z13.Core;

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

        private static readonly Collider2D[] s_colliderBuffer = new Collider2D[64];
        private static readonly List<EnemyBase> s_chainTargetsBuffer = new List<EnemyBase>(16);

        public ChainLightningBehavior(int maxTargets = 4, float chainRange = 5f, float damageMultiplier = 0.6f)
        {
            m_maxTargets = maxTargets;
            m_chainRange = chainRange;
            m_damageMultiplier = damageMultiplier;
            // HashSet created lazily in Initialize to avoid allocation if behavior is cached but unused
        }

        /// <summary>
        /// Reset parameters for reuse (zero allocation).
        /// </summary>
        public void Reset(int maxTargets, float chainRange = 5f, float damageMultiplier = 0.6f)
        {
            m_maxTargets = maxTargets;
            m_chainRange = chainRange;
            m_damageMultiplier = damageMultiplier;
        }

        public override void Initialize(MonoBehaviour proj)
        {
            base.Initialize(proj);
            if (m_hitEnemies == null)
                m_hitEnemies = new HashSet<EnemyBase>();
            else
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
            // Use static buffer to avoid allocation
            s_chainTargetsBuffer.Clear();

            // Find all enemies in chain range (NonAlloc - zero GC)
            int count = Physics2D.OverlapCircleNonAlloc(fromPosition, m_chainRange, s_colliderBuffer);
            for (int i = 0; i < count; i++)
            {
                var col = s_colliderBuffer[i];
                if (!col.CompareTag("Enemy")) continue;

                var enemy = col.GetComponent<EnemyBase>();
                if (enemy == null || !enemy.IsAlive) continue;
                if (m_hitEnemies.Contains(enemy)) continue; // Already hit

                s_chainTargetsBuffer.Add(enemy);

                // Stop if we have enough targets
                if (m_hitEnemies.Count + s_chainTargetsBuffer.Count >= m_maxTargets)
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
            for (int i = 0; i < s_chainTargetsBuffer.Count; i++)
            {
                var target = s_chainTargetsBuffer[i];
                target.TakeDamage(chainDamage, target.transform.position);
                m_hitEnemies.Add(target);

                // Create visual effect
                CreateChainVFX(fromPosition, target.transform.position);
            }

            // Publish event
            if (s_chainTargetsBuffer.Count > 0)
            {
                EventBus.Publish(new ChainLightningEvent
                {
                    targets = new List<EnemyBase>(s_chainTargetsBuffer)  // Create copy for event
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
