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
        private int _maxTargets;
        private float _chainRange;
        private float _damageMultiplier;
        private HashSet<EnemyBase> _hitEnemies;

        public ChainLightningBehavior(int maxTargets = 4, float chainRange = 5f, float damageMultiplier = 0.6f)
        {
            _maxTargets = maxTargets;
            _chainRange = chainRange;
            _damageMultiplier = damageMultiplier;
            _hitEnemies = new HashSet<EnemyBase>();
        }

        public override void Initialize(MonoBehaviour proj)
        {
            base.Initialize(proj);
            _hitEnemies.Clear();
        }

        public override void Update(float deltaTime)
        {
            // No per-frame update needed
        }

        public override bool OnHitEnemy(EnemyBase enemy)
        {
            if (enemy == null || _hitEnemies.Contains(enemy)) return true;

            // Add to hit list
            _hitEnemies.Add(enemy);

            // Chain to nearby enemies
            if (_hitEnemies.Count < _maxTargets)
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
            Collider2D[] colliders = Physics2D.OverlapCircleAll(fromPosition, _chainRange);
            foreach (var col in colliders)
            {
                if (!col.CompareTag("Enemy")) continue;

                var enemy = col.GetComponent<EnemyBase>();
                if (enemy == null || !enemy.IsAlive) continue;
                if (_hitEnemies.Contains(enemy)) continue; // Already hit

                chainTargets.Add(enemy);

                // Stop if we have enough targets
                if (_hitEnemies.Count + chainTargets.Count >= _maxTargets)
                {
                    break;
                }
            }

            // Apply damage to chain targets
            var enhancedProj = GetAsEnhancedProjectile();
            var basicProj = GetAsProjectile();
            int currentDamage = enhancedProj != null ? enhancedProj.GetDamage() :
                               (basicProj != null ? basicProj.GetDamage() : 0);
            int chainDamage = Mathf.RoundToInt(currentDamage * _damageMultiplier);
            foreach (var target in chainTargets)
            {
                target.TakeDamage(chainDamage, target.transform.position);
                _hitEnemies.Add(target);

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
