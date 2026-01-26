using UnityEngine;

namespace NeuralBreak.Graphics.VFX
{
    /// <summary>
    /// ChaosWorm death VFX: Chaotic purple/pink energy dispersal with swirling particles.
    /// Strong noise and orbital velocity for swirl effect.
    /// </summary>
    public class ChaosWormVFX : IEnemyVFXGenerator
    {
        public float GetEffectLifetime() => 1.5f;

        public GameObject GenerateDeathEffect(Vector3 position, Material particleMaterial, float emissionIntensity)
        {
            var go = new GameObject("DeathVFX_ChaosWorm");
            go.transform.position = position;

            Color primaryColor = new Color(0.8f, 0f, 1f);
            Color secondaryColor = new Color(1f, 0f, 0.7f);
            Color coreColor = new Color(1f, 0.5f, 1f);

            // Swirling chaos particles
            var mainPS = VFXHelpers.CreateBaseParticleSystem(go, "Chaos");
            var main = mainPS.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            main.startColor = primaryColor * emissionIntensity;
            main.maxParticles = 45;

            var emission = mainPS.emission;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 25),
                new ParticleSystem.Burst(0.15f, 15)
            });

            var shape = mainPS.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            // Strong swirling noise
            var noise = mainPS.noise;
            noise.enabled = true;
            noise.strength = 5f;
            noise.frequency = 1.5f;
            noise.scrollSpeed = 2f;
            noise.damping = true;

            // Orbital velocity for swirl effect
            var velocity = mainPS.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(-3f, 3f);

            VFXHelpers.SetupColorFade(mainPS, coreColor, primaryColor);
            VFXHelpers.SetupShrink(mainPS);
            VFXHelpers.SetupRenderer(mainPS, VFXHelpers.CreateMaterialForColor(particleMaterial, primaryColor, emissionIntensity));

            // Energy wisps
            var wispPS = VFXHelpers.CreateBaseParticleSystem(go, "Wisps");
            var wispMain = wispPS.main;
            wispMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            wispMain.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f);
            wispMain.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            wispMain.startColor = secondaryColor * emissionIntensity;
            wispMain.maxParticles = 25;

            var wispEmission = wispPS.emission;
            wispEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.05f, 20) });

            var wispNoise = wispPS.noise;
            wispNoise.enabled = true;
            wispNoise.strength = 4f;
            wispNoise.frequency = 3f;

            VFXHelpers.SetupColorFade(wispPS, secondaryColor, primaryColor);
            VFXHelpers.SetupRenderer(wispPS, VFXHelpers.CreateMaterialForColor(particleMaterial, secondaryColor, emissionIntensity));

            VFXHelpers.AddFlash(go, particleMaterial, coreColor, 1f, emissionIntensity);

            mainPS.Play();
            wispPS.Play();
            return go;
        }
    }
}
