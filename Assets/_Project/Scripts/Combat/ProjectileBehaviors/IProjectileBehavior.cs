using UnityEngine;
using NeuralBreak.Entities;

namespace NeuralBreak.Combat.ProjectileBehaviors
{
    /// <summary>
    /// Interface for projectile behaviors.
    /// Allows modular composition of projectile effects.
    /// </summary>
    public interface IProjectileBehavior
    {
        /// <summary>
        /// Called when behavior is initialized.
        /// Accepts any MonoBehaviour as the projectile host.
        /// </summary>
        void Initialize(MonoBehaviour projectile);

        /// <summary>
        /// Called every frame to update behavior.
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// Called when projectile hits an enemy.
        /// Returns true if projectile should be destroyed.
        /// </summary>
        bool OnHitEnemy(EnemyBase enemy);

        /// <summary>
        /// Called when projectile is deactivated.
        /// </summary>
        void OnDeactivate();
    }

    /// <summary>
    /// Base class for projectile behaviors with common functionality.
    /// </summary>
    public abstract class ProjectileBehaviorBase : IProjectileBehavior
    {
        protected MonoBehaviour projectileHost;
        protected Transform transform;

        public virtual void Initialize(MonoBehaviour proj)
        {
            projectileHost = proj;
            transform = proj.transform;
        }

        public abstract void Update(float deltaTime);
        public abstract bool OnHitEnemy(EnemyBase enemy);

        public virtual void OnDeactivate()
        {
            // Default: no cleanup needed
        }

        // Helper to get projectile as Projectile type (if needed)
        protected Projectile GetAsProjectile() => projectileHost as Projectile;

        // Helper to get projectile as EnhancedProjectile type (if needed)
        protected EnhancedProjectile GetAsEnhancedProjectile() => projectileHost as EnhancedProjectile;
    }
}
