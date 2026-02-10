using UnityEngine;
using NeuralBreak.Entities;

namespace NeuralBreak.Combat.ProjectileBehaviors
{
    /// <summary>
    /// Projectile behavior: passes through multiple enemies.
    /// </summary>
    public class PiercingBehavior : ProjectileBehaviorBase
    {
        private int m_maxPierceCount;
        private int m_currentPierceCount;

        public PiercingBehavior(int maxPierceCount = 3)
        {
            m_maxPierceCount = maxPierceCount;
            m_currentPierceCount = 0;
        }

        /// <summary>
        /// Reset parameters for reuse (zero allocation).
        /// </summary>
        public void Reset(int maxPierceCount)
        {
            m_maxPierceCount = maxPierceCount;
        }

        public override void Initialize(MonoBehaviour proj)
        {
            base.Initialize(proj);
            m_currentPierceCount = 0;
        }

        public override void Update(float deltaTime)
        {
            // No per-frame update needed
        }

        public override bool OnHitEnemy(EnemyBase enemy)
        {
            m_currentPierceCount++;

            // Destroy if we've pierced max enemies
            return m_currentPierceCount >= m_maxPierceCount;
        }
    }
}
