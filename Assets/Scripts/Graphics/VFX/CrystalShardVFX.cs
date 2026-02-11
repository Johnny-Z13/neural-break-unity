using UnityEngine;

namespace NeuralBreak.Graphics.VFX
{
    /// <summary>
    /// CrystalShard death VFX: Sharp crystalline shatter with ice-blue shards.
    /// Fast shards with rotation and sparkle particles.
    /// </summary>
    public class CrystalShardVFX : IEnemyVFXGenerator
    {
        public float GetEffectLifetime() => 0.9f;

        public GameObject GenerateDeathEffect(Vector3 position, Material particleMaterial, float emissionIntensity)
        {
            var go = new GameObject("DeathVFX_CrystalShard");
            go.transform.position = position;

            Color primaryColor = new Color(0.4f, 0.9f, 1f);
            Color secondaryColor = new Color(0.8f, 1f, 1f);
            Color sparkleColor = Color.white;

            // Fast sharp shards
            var shardPS = VFXHelpers.CreateBaseParticleSystem(go, "Shards");
            var shardMain = shardPS.main;
            shardMain.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            shardMain.startSpeed = new ParticleSystem.MinMaxCurve(12f, 20f);
            shardMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            shardMain.startColor = primaryColor * emissionIntensity;
            shardMain.gravityModifier = 0.4f;
            shardMain.maxParticles = 35;

            // Rotation for tumbling shards
            shardMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var shardEmission = shardPS.emission;
            shardEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var shardShape = shardPS.shape;
            shardShape.shapeType = ParticleSystemShapeType.Sphere;
            shardShape.radius = 0.15f;

            var shardRotation = shardPS.rotationOverLifetime;
            shardRotation.enabled = true;
            shardRotation.z = new ParticleSystem.MinMaxCurve(-5f, 5f);

            VFXHelpers.SetupColorFade(shardPS, secondaryColor, primaryColor);
            VFXHelpers.SetupRenderer(shardPS, VFXHelpers.CreateMaterialForColor(particleMaterial, primaryColor, emissionIntensity));

            // Sparkle dust
            var sparklePS = VFXHelpers.CreateBaseParticleSystem(go, "Sparkles");
            var sparkleMain = sparklePS.main;
            sparkleMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            sparkleMain.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            sparkleMain.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            sparkleMain.startColor = sparkleColor * emissionIntensity;
            sparkleMain.maxParticles = 25;

            var sparkleEmission = sparklePS.emission;
            sparkleEmission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 15),
                new ParticleSystem.Burst(0.1f, 10)
            });

            var sparkleShape = sparklePS.shape;
            sparkleShape.shapeType = ParticleSystemShapeType.Sphere;
            sparkleShape.radius = 0.25f;

            VFXHelpers.SetupColorFade(sparklePS, sparkleColor, primaryColor);
            VFXHelpers.SetupRenderer(sparklePS, VFXHelpers.CreateMaterialForColor(particleMaterial, sparkleColor, emissionIntensity));

            VFXHelpers.AddFlash(go, particleMaterial, secondaryColor, 0.5f, emissionIntensity);

            shardPS.Play();
            sparklePS.Play();
            return go;
        }
    }
}
