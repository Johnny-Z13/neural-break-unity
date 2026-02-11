using UnityEngine;

namespace NeuralBreak.Graphics.VFX
{
    /// <summary>
    /// Boss death VFX: Massive multi-stage explosion with screen-filling particles.
    /// Multiple burst stages with shockwave ring and smoke.
    /// </summary>
    public class BossVFX : IEnemyVFXGenerator
    {
        public float GetEffectLifetime() => 2.5f;

        public GameObject GenerateDeathEffect(Vector3 position, Material particleMaterial, float emissionIntensity)
        {
            var go = new GameObject("DeathVFX_Boss");
            go.transform.position = position;

            Color primaryColor = new Color(1f, 0.2f, 0f);
            Color secondaryColor = new Color(1f, 0.6f, 0f);
            Color coreColor = new Color(1f, 1f, 0.5f);

            // Massive core explosion
            var corePS = VFXHelpers.CreateBaseParticleSystem(go, "Core");
            var coreMain = corePS.main;
            coreMain.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            coreMain.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
            coreMain.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            coreMain.startColor = primaryColor * emissionIntensity;
            coreMain.maxParticles = 80;

            var coreEmission = corePS.emission;
            coreEmission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 40),
                new ParticleSystem.Burst(0.15f, 25),
                new ParticleSystem.Burst(0.3f, 15)
            });

            var coreShape = corePS.shape;
            coreShape.shapeType = ParticleSystemShapeType.Sphere;
            coreShape.radius = 0.8f;

            VFXHelpers.SetupColorFade(corePS, coreColor, primaryColor);
            VFXHelpers.SetupShrink(corePS);
            VFXHelpers.SetupRenderer(corePS, VFXHelpers.CreateMaterialForColor(particleMaterial, primaryColor, emissionIntensity));

            // Secondary fire particles
            var firePS = VFXHelpers.CreateBaseParticleSystem(go, "Fire");
            var fireMain = firePS.main;
            fireMain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            fireMain.startSpeed = new ParticleSystem.MinMaxCurve(8f, 15f);
            fireMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            fireMain.startColor = secondaryColor * emissionIntensity;
            fireMain.gravityModifier = -0.3f;
            fireMain.maxParticles = 50;

            var fireEmission = firePS.emission;
            fireEmission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0.1f, 30),
                new ParticleSystem.Burst(0.25f, 20)
            });

            var fireShape = firePS.shape;
            fireShape.shapeType = ParticleSystemShapeType.Sphere;
            fireShape.radius = 0.6f;

            VFXHelpers.SetupColorFade(firePS, secondaryColor, primaryColor);
            VFXHelpers.SetupShrink(firePS);
            VFXHelpers.SetupRenderer(firePS, VFXHelpers.CreateMaterialForColor(particleMaterial, secondaryColor, emissionIntensity));

            // Expanding shockwave ring
            var ringPS = VFXHelpers.CreateBaseParticleSystem(go, "Ring");
            var ringMain = ringPS.main;
            ringMain.startLifetime = 0.6f;
            ringMain.startSpeed = 20f;
            ringMain.startSize = 0.1f;
            ringMain.startColor = coreColor * emissionIntensity;
            ringMain.maxParticles = 36;

            var ringEmission = ringPS.emission;
            ringEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 32) });

            var ringShape = ringPS.shape;
            ringShape.shapeType = ParticleSystemShapeType.Circle;
            ringShape.radius = 0.2f;
            ringShape.arc = 360f;

            VFXHelpers.SetupColorFade(ringPS, coreColor, secondaryColor);
            VFXHelpers.SetupShrink(ringPS);
            VFXHelpers.SetupRenderer(ringPS, VFXHelpers.CreateMaterialForColor(particleMaterial, coreColor, emissionIntensity));

            // Smoke/debris
            var smokePS = VFXHelpers.CreateBaseParticleSystem(go, "Smoke");
            var smokeMain = smokePS.main;
            smokeMain.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
            smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            smokeMain.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            smokeMain.startColor = new Color(0.3f, 0.15f, 0f) * emissionIntensity * 0.5f;
            smokeMain.gravityModifier = -0.1f;
            smokeMain.maxParticles = 30;

            var smokeEmission = smokePS.emission;
            smokeEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.2f, 20) });

            var smokeShape = smokePS.shape;
            smokeShape.shapeType = ParticleSystemShapeType.Sphere;
            smokeShape.radius = 0.5f;

            var smokeNoise = smokePS.noise;
            smokeNoise.enabled = true;
            smokeNoise.strength = 2f;
            smokeNoise.frequency = 0.5f;

            VFXHelpers.SetupColorFade(smokePS, new Color(0.4f, 0.2f, 0f), new Color(0.1f, 0.05f, 0f));
            VFXHelpers.SetupRenderer(smokePS, VFXHelpers.CreateMaterialForColor(particleMaterial, new Color(0.3f, 0.15f, 0f), emissionIntensity));

            // Big flash
            VFXHelpers.AddFlash(go, particleMaterial, coreColor, 2f, emissionIntensity);

            corePS.Play();
            firePS.Play();
            ringPS.Play();
            smokePS.Play();
            return go;
        }
    }
}
