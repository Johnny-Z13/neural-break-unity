using UnityEngine;

namespace NeuralBreak.Graphics.VFX
{
    /// <summary>
    /// VoidSphere death VFX: Dark implosion with void energy and slow particles.
    /// Negative speed for implosion effect, then delayed explosion.
    /// </summary>
    public class VoidSphereVFX : IEnemyVFXGenerator
    {
        public float GetEffectLifetime() => 3.5f;

        public GameObject GenerateDeathEffect(Vector3 position, Material particleMaterial, float emissionIntensity)
        {
            var go = new GameObject("DeathVFX_VoidSphere");
            go.transform.position = position;

            Color primaryColor = new Color(0.4f, 0f, 0.8f);
            Color secondaryColor = new Color(0.8f, 0f, 1f);
            Color voidColor = new Color(0.1f, 0f, 0.3f);
            Color coreColor = new Color(1f, 0.3f, 1f);

            // MASSIVE Implosion effect - particles moving inward first
            var implodePS = VFXHelpers.CreateBaseParticleSystem(go, "Implode");
            var implodeMain = implodePS.main;
            implodeMain.startLifetime = 0.6f;
            implodeMain.startSpeed = -15f; // Negative speed = implode (much faster)
            implodeMain.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f); // LARGER particles
            implodeMain.startColor = secondaryColor * emissionIntensity;
            implodeMain.maxParticles = 80;

            var implodeEmission = implodePS.emission;
            implodeEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 60) }); // MORE particles

            var implodeShape = implodePS.shape;
            implodeShape.shapeType = ParticleSystemShapeType.Sphere;
            implodeShape.radius = 2.5f; // LARGER spawn radius

            VFXHelpers.SetupColorFade(implodePS, secondaryColor, voidColor);
            VFXHelpers.SetupRenderer(implodePS, VFXHelpers.CreateMaterialForColor(particleMaterial, secondaryColor, emissionIntensity));

            // MASSIVE expanding void explosion
            var voidPS = VFXHelpers.CreateBaseParticleSystem(go, "Void");
            var voidMain = voidPS.main;
            voidMain.startDelay = 0.5f;
            voidMain.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            voidMain.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f); // MUCH faster
            voidMain.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f); // MUCH LARGER
            voidMain.startColor = primaryColor * emissionIntensity;
            voidMain.maxParticles = 80;

            var voidEmission = voidPS.emission;
            voidEmission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 50),
                new ParticleSystem.Burst(0.1f, 30) // Second wave
            });

            var voidShape = voidPS.shape;
            voidShape.shapeType = ParticleSystemShapeType.Sphere;
            voidShape.radius = 0.5f;

            VFXHelpers.SetupColorFade(voidPS, primaryColor, voidColor);
            VFXHelpers.SetupShrink(voidPS);
            VFXHelpers.SetupRenderer(voidPS, VFXHelpers.CreateMaterialForColor(particleMaterial, primaryColor, emissionIntensity));

            // Energy rings expanding outward
            var ringsPS = VFXHelpers.CreateBaseParticleSystem(go, "EnergyRings");
            var ringsMain = ringsPS.main;
            ringsMain.startDelay = 0.4f;
            ringsMain.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
            ringsMain.startSpeed = new ParticleSystem.MinMaxCurve(10f, 15f);
            ringsMain.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
            ringsMain.startColor = coreColor * emissionIntensity;
            ringsMain.maxParticles = 50;

            var ringsEmission = ringsPS.emission;
            ringsEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) });

            VFXHelpers.SetupColorFade(ringsPS, coreColor, primaryColor);
            VFXHelpers.SetupShrink(ringsPS);
            VFXHelpers.SetupRenderer(ringsPS, VFXHelpers.CreateMaterialForColor(particleMaterial, coreColor, emissionIntensity));

            // MASSIVE epic flash
            CreateDarkFlash(go, particleMaterial, emissionIntensity);
            VFXHelpers.AddFlash(go, particleMaterial, coreColor, 3.5f, emissionIntensity * 1.5f); // HUGE flash

            implodePS.Play();
            voidPS.Play();
            ringsPS.Play();
            return go;
        }

        private void CreateDarkFlash(GameObject parent, Material baseMaterial, float emissionIntensity)
        {
            Color flashColor = new Color(0.9f, 0.3f, 1f); // Bright purple-pink

            var flashGO = new GameObject("DarkFlash");
            flashGO.transform.SetParent(parent.transform);
            flashGO.transform.localPosition = Vector3.zero;
            var flashPS = flashGO.AddComponent<ParticleSystem>();
            flashPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var flashMain = flashPS.main;
            flashMain.duration = 0.8f;
            flashMain.loop = false;
            flashMain.startDelay = 0.45f; // Sync with explosion
            flashMain.startLifetime = 0.5f;
            flashMain.startSpeed = 0f;
            flashMain.startSize = 4.0f; // MUCH LARGER
            flashMain.startColor = flashColor * emissionIntensity * 1.5f;
            flashMain.simulationSpace = ParticleSystemSimulationSpace.World;
            flashMain.playOnAwake = false;
            flashMain.maxParticles = 1;

            var flashEmission = flashPS.emission;
            flashEmission.enabled = true;
            flashEmission.rateOverTime = 0;
            flashEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

            var flashShape = flashPS.shape;
            flashShape.enabled = false;

            var flashSize = flashPS.sizeOverLifetime;
            flashSize.enabled = true;
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0f);
            curve.AddKey(0.1f, 1.5f); // Bigger peak
            curve.AddKey(1f, 0f);
            flashSize.size = new ParticleSystem.MinMaxCurve(1f, curve);

            VFXHelpers.SetupRenderer(flashPS, VFXHelpers.CreateMaterialForColor(baseMaterial, flashColor, emissionIntensity * 1.5f));
            flashPS.Play();
        }
    }
}
