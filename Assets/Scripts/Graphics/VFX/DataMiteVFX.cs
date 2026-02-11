using UnityEngine;

namespace NeuralBreak.Graphics.VFX
{
    /// <summary>
    /// DataMite death VFX: Quick digital dissolve with cyan/blue data fragments.
    /// Small, fast particles representing data corruption.
    /// </summary>
    public class DataMiteVFX : IEnemyVFXGenerator
    {
        public float GetEffectLifetime() => 0.8f;

        public GameObject GenerateDeathEffect(Vector3 position, Material particleMaterial, float emissionIntensity)
        {
            var go = new GameObject("DeathVFX_DataMite");
            go.transform.position = position;

            Color primaryColor = new Color(0f, 1f, 0.8f);
            Color secondaryColor = new Color(0f, 0.6f, 1f);

            // Main burst - small fast particles
            var mainPS = VFXHelpers.CreateBaseParticleSystem(go, "Main");
            var main = mainPS.main;
            main.startLifetime = 0.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = primaryColor * emissionIntensity;
            main.maxParticles = 30;

            var emission = mainPS.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

            var shape = mainPS.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;

            VFXHelpers.SetupColorFade(mainPS, primaryColor, secondaryColor);
            VFXHelpers.SetupShrink(mainPS);
            VFXHelpers.SetupRenderer(mainPS, VFXHelpers.CreateMaterialForColor(particleMaterial, primaryColor, emissionIntensity));

            // Binary/glitch particles - tiny square-like particles
            var glitchPS = VFXHelpers.CreateBaseParticleSystem(go, "Glitch");
            var glitchMain = glitchPS.main;
            glitchMain.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            glitchMain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            glitchMain.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            glitchMain.startColor = secondaryColor * emissionIntensity;
            glitchMain.maxParticles = 15;

            var glitchEmission = glitchPS.emission;
            glitchEmission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 8),
                new ParticleSystem.Burst(0.1f, 5)
            });

            VFXHelpers.SetupColorFade(glitchPS, secondaryColor, primaryColor);
            VFXHelpers.SetupRenderer(glitchPS, VFXHelpers.CreateMaterialForColor(particleMaterial, secondaryColor, emissionIntensity));

            mainPS.Play();
            glitchPS.Play();
            return go;
        }
    }
}
