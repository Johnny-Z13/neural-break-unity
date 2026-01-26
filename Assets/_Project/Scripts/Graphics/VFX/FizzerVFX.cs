using UnityEngine;

namespace NeuralBreak.Graphics.VFX
{
    /// <summary>
    /// Fizzer death VFX: Electric discharge with pink/magenta lightning.
    /// Fast, chaotic particles with noise for erratic movement.
    /// </summary>
    public class FizzerVFX : IEnemyVFXGenerator
    {
        public float GetEffectLifetime() => 0.7f;

        public GameObject GenerateDeathEffect(Vector3 position, Material particleMaterial, float emissionIntensity)
        {
            var go = new GameObject("DeathVFX_Fizzer");
            go.transform.position = position;

            Color primaryColor = new Color(1f, 0f, 0.6f);
            Color secondaryColor = new Color(1f, 0.4f, 1f);
            Color electricColor = new Color(1f, 0.8f, 1f);

            // Electric burst - fast chaotic particles
            var mainPS = VFXHelpers.CreateBaseParticleSystem(go, "Electric");
            var main = mainPS.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = primaryColor * emissionIntensity;
            main.maxParticles = 50;

            var emission = mainPS.emission;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 30),
                new ParticleSystem.Burst(0.05f, 15)
            });

            var shape = mainPS.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            // Add noise for chaotic movement
            var noise = mainPS.noise;
            noise.enabled = true;
            noise.strength = 3f;
            noise.frequency = 2f;
            noise.scrollSpeed = 1f;

            VFXHelpers.SetupColorFade(mainPS, electricColor, primaryColor);
            VFXHelpers.SetupShrink(mainPS);
            VFXHelpers.SetupRenderer(mainPS, VFXHelpers.CreateMaterialForColor(particleMaterial, primaryColor, emissionIntensity));

            // Secondary glow particles
            var glowPS = VFXHelpers.CreateBaseParticleSystem(go, "Glow");
            var glowMain = glowPS.main;
            glowMain.startLifetime = 0.5f;
            glowMain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            glowMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            glowMain.startColor = secondaryColor * emissionIntensity * 0.5f;
            glowMain.maxParticles = 10;

            var glowEmission = glowPS.emission;
            glowEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8) });

            VFXHelpers.SetupColorFade(glowPS, secondaryColor, primaryColor);
            VFXHelpers.SetupShrink(glowPS);
            VFXHelpers.SetupRenderer(glowPS, VFXHelpers.CreateMaterialForColor(particleMaterial, secondaryColor, emissionIntensity));

            // Bright flash
            VFXHelpers.AddFlash(go, particleMaterial, electricColor, 0.6f, emissionIntensity);

            mainPS.Play();
            glowPS.Play();
            return go;
        }
    }
}
