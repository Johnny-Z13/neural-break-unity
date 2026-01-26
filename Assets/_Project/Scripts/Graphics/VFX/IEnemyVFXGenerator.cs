using UnityEngine;

namespace NeuralBreak.Graphics.VFX
{
    /// <summary>
    /// Interface for enemy-specific death VFX generators.
    /// Each enemy type implements this to define its unique death effect.
    /// </summary>
    public interface IEnemyVFXGenerator
    {
        /// <summary>
        /// Generates the death effect at the specified position.
        /// </summary>
        /// <param name="position">World position for the effect</param>
        /// <param name="particleMaterial">Shared material for particles</param>
        /// <param name="emissionIntensity">Emission intensity multiplier</param>
        /// <returns>GameObject containing the VFX (will be destroyed after lifetime)</returns>
        GameObject GenerateDeathEffect(Vector3 position, Material particleMaterial, float emissionIntensity);

        /// <summary>
        /// Returns the maximum lifetime of this effect in seconds.
        /// Used to determine when to destroy the VFX GameObject.
        /// </summary>
        float GetEffectLifetime();
    }
}
