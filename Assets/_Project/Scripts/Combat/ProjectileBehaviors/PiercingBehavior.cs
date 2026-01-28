using UnityEngine;
using NeuralBreak.Entities;

namespace NeuralBreak.Combat.ProjectileBehaviors
{
    /// <summary>
    /// Projectile behavior: passes through multiple enemies.
    /// </summary>
    public class PiercingBehavior : ProjectileBehaviorBase
    {
        private int _maxPierceCount;
        private int _currentPierceCount;

        public PiercingBehavior(int maxPierceCount = 3)
        {
            _maxPierceCount = maxPierceCount;
            _currentPierceCount = 0;
        }

        public override void Initialize(MonoBehaviour proj)
        {
            base.Initialize(proj);
            _currentPierceCount = 0;
        }

        public override void Update(float deltaTime)
        {
            // No per-frame update needed
        }

        public override bool OnHitEnemy(EnemyBase enemy)
        {
            _currentPierceCount++;

            // Destroy if we've pierced max enemies
            return _currentPierceCount >= _maxPierceCount;
        }
    }
}
