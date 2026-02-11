using UnityEngine;

namespace NeuralBreak.Graphics.VFX
{
    /// <summary>
    /// UFO death VFX: Alien green implosion then explosion.
    /// Expanding ring with central glow and debris particles.
    /// </summary>
    public class UFOVFX : IEnemyVFXGenerator
    {
        public float GetEffectLifetime() => 1.2f;

        public GameObject GenerateDeathEffect(Vector3 position, Material particleMaterial, float emissionIntensity)
        {
            var go = new GameObject("DeathVFX_UFO");
            go.transform.position = position;

            Color primaryColor = new Color(0.3f, 1f, 0.3f);
            Color secondaryColor = new Color(0f, 1f, 0.5f);
            Color coreColor = new Color(0.8f, 1f, 0.8f);

            // Expanding ring
            var ringPS = VFXHelpers.CreateBaseParticleSystem(go, "Ring");
            var ringMain = ringPS.main;
            ringMain.startLifetime = 0.5f;
            ringMain.startSpeed = 12f;
            ringMain.startSize = 0.08f;
            ringMain.startColor = primaryColor * emissionIntensity;
            ringMain.maxParticles = 24;

            var ringEmission = ringPS.emission;
            ringEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

            var ringShape = ringPS.shape;
            ringShape.shapeType = ParticleSystemShapeType.Circle;
            ringShape.radius = 0.1f;
            ringShape.arc = 360f;

            VFXHelpers.SetupColorFade(ringPS, primaryColor, secondaryColor);
            VFXHelpers.SetupShrink(ringPS);
            VFXHelpers.SetupRenderer(ringPS, VFXHelpers.CreateMaterialForColor(particleMaterial, primaryColor, emissionIntensity));

            // Central glow that expands
            var corePS = VFXHelpers.CreateBaseParticleSystem(go, "Core");
            var coreMain = corePS.main;
            coreMain.startLifetime = 0.6f;
            coreMain.startSpeed = 0f;
            coreMain.startSize = 0.5f;
            coreMain.startColor = coreColor * emissionIntensity * 0.7f;
            coreMain.maxParticles = 1;

            var coreEmission = corePS.emission;
            coreEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

            var coreShape = corePS.shape;
            coreShape.enabled = false;

            var coreSize = corePS.sizeOverLifetime;
            coreSize.enabled = true;
            var expandCurve = new AnimationCurve();
            expandCurve.AddKey(0f, 0.3f);
            expandCurve.AddKey(0.2f, 1.2f);
            expandCurve.AddKey(1f, 0f);
            coreSize.size = new ParticleSystem.MinMaxCurve(1f, expandCurve);

            VFXHelpers.SetupColorFade(corePS, coreColor, primaryColor);
            VFXHelpers.SetupRenderer(corePS, VFXHelpers.CreateMaterialForColor(particleMaterial, coreColor, emissionIntensity));

            // Debris particles
            var debrisPS = VFXHelpers.CreateBaseParticleSystem(go, "Debris");
            var debrisMain = debrisPS.main;
            debrisMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
            debrisMain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            debrisMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            debrisMain.startColor = secondaryColor * emissionIntensity;
            debrisMain.gravityModifier = 0.2f;
            debrisMain.maxParticles = 20;

            var debrisEmission = debrisPS.emission;
            debrisEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.1f, 15) });

            var debrisShape = debrisPS.shape;
            debrisShape.shapeType = ParticleSystemShapeType.Sphere;
            debrisShape.radius = 0.4f;

            VFXHelpers.SetupColorFade(debrisPS, secondaryColor, primaryColor);
            VFXHelpers.SetupShrink(debrisPS);
            VFXHelpers.SetupRenderer(debrisPS, VFXHelpers.CreateMaterialForColor(particleMaterial, secondaryColor, emissionIntensity));

            ringPS.Play();
            corePS.Play();
            debrisPS.Play();
            return go;
        }
    }
}
