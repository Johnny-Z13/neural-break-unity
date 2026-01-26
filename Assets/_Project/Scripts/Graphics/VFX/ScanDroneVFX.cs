using UnityEngine;

namespace NeuralBreak.Graphics.VFX
{
    /// <summary>
    /// ScanDrone death VFX: Mechanical explosion with orange sparks and debris.
    /// Medium-sized explosion with hot sparks and gravity.
    /// </summary>
    public class ScanDroneVFX : IEnemyVFXGenerator
    {
        public float GetEffectLifetime() => 1.2f;

        public GameObject GenerateDeathEffect(Vector3 position, Material particleMaterial, float emissionIntensity)
        {
            var go = new GameObject("DeathVFX_ScanDrone");
            go.transform.position = position;

            Color primaryColor = new Color(1f, 0.7f, 0f);
            Color secondaryColor = new Color(1f, 0.3f, 0f);
            Color sparkColor = new Color(1f, 1f, 0.5f);

            // Core explosion
            var mainPS = VFXHelpers.CreateBaseParticleSystem(go, "Explosion");
            var main = mainPS.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            main.startColor = primaryColor * emissionIntensity;
            main.gravityModifier = 0.3f;
            main.maxParticles = 35;

            var emission = mainPS.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var shape = mainPS.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            VFXHelpers.SetupColorFade(mainPS, primaryColor, secondaryColor);
            VFXHelpers.SetupShrink(mainPS);
            VFXHelpers.SetupRenderer(mainPS, VFXHelpers.CreateMaterialForColor(particleMaterial, primaryColor, emissionIntensity));

            // Hot sparks
            var sparkPS = VFXHelpers.CreateBaseParticleSystem(go, "Sparks");
            var sparkMain = sparkPS.main;
            sparkMain.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(8f, 15f);
            sparkMain.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            sparkMain.startColor = sparkColor * emissionIntensity;
            sparkMain.gravityModifier = 0.5f;
            sparkMain.maxParticles = 20;

            var sparkEmission = sparkPS.emission;
            sparkEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

            var sparkShape = sparkPS.shape;
            sparkShape.shapeType = ParticleSystemShapeType.Sphere;
            sparkShape.radius = 0.15f;

            VFXHelpers.SetupColorFade(sparkPS, sparkColor, secondaryColor);
            VFXHelpers.SetupRenderer(sparkPS, VFXHelpers.CreateMaterialForColor(particleMaterial, sparkColor, emissionIntensity));

            // Flash
            VFXHelpers.AddFlash(go, particleMaterial, primaryColor, 0.8f, emissionIntensity);

            mainPS.Play();
            sparkPS.Play();
            return go;
        }
    }
}
