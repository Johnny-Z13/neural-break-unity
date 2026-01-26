using UnityEngine;

namespace NeuralBreak.Graphics.VFX
{
    /// <summary>
    /// VoidSphere death VFX: Dark implosion with void energy and slow particles.
    /// Negative speed for implosion effect, then delayed explosion.
    /// </summary>
    public class VoidSphereVFX : IEnemyVFXGenerator
    {
        public float GetEffectLifetime() => 2.0f;

        public GameObject GenerateDeathEffect(Vector3 position, Material particleMaterial, float emissionIntensity)
        {
            var go = new GameObject("DeathVFX_VoidSphere");
            go.transform.position = position;

            Color primaryColor = new Color(0.3f, 0f, 0.6f);
            Color secondaryColor = new Color(0.6f, 0f, 1f);
            Color voidColor = new Color(0.1f, 0f, 0.2f);

            // Implosion effect - particles moving inward first
            var implodePS = VFXHelpers.CreateBaseParticleSystem(go, "Implode");
            var implodeMain = implodePS.main;
            implodeMain.startLifetime = 0.4f;
            implodeMain.startSpeed = -8f; // Negative speed = implode
            implodeMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            implodeMain.startColor = secondaryColor * emissionIntensity;
            implodeMain.maxParticles = 30;

            var implodeEmission = implodePS.emission;
            implodeEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

            var implodeShape = implodePS.shape;
            implodeShape.shapeType = ParticleSystemShapeType.Sphere;
            implodeShape.radius = 1.2f;

            VFXHelpers.SetupColorFade(implodePS, secondaryColor, voidColor);
            VFXHelpers.SetupRenderer(implodePS, VFXHelpers.CreateMaterialForColor(particleMaterial, secondaryColor, emissionIntensity));

            // Slow expanding void particles
            var voidPS = VFXHelpers.CreateBaseParticleSystem(go, "Void");
            var voidMain = voidPS.main;
            voidMain.startDelay = 0.3f;
            voidMain.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.8f);
            voidMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            voidMain.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            voidMain.startColor = primaryColor * emissionIntensity;
            voidMain.maxParticles = 25;

            var voidEmission = voidPS.emission;
            voidEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

            var voidShape = voidPS.shape;
            voidShape.shapeType = ParticleSystemShapeType.Sphere;
            voidShape.radius = 0.2f;

            VFXHelpers.SetupColorFade(voidPS, primaryColor, voidColor);
            VFXHelpers.SetupShrink(voidPS);
            VFXHelpers.SetupRenderer(voidPS, VFXHelpers.CreateMaterialForColor(particleMaterial, primaryColor, emissionIntensity));

            // Dark flash - use brighter purple for visibility
            CreateDarkFlash(go, particleMaterial, emissionIntensity);

            implodePS.Play();
            voidPS.Play();
            return go;
        }

        private void CreateDarkFlash(GameObject parent, Material baseMaterial, float emissionIntensity)
        {
            Color flashColor = new Color(0.8f, 0.2f, 1f); // Bright purple

            var flashGO = new GameObject("DarkFlash");
            flashGO.transform.SetParent(parent.transform);
            flashGO.transform.localPosition = Vector3.zero;
            var flashPS = flashGO.AddComponent<ParticleSystem>();
            flashPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var flashMain = flashPS.main;
            flashMain.duration = 0.5f;
            flashMain.loop = false;
            flashMain.startDelay = 0.25f;
            flashMain.startLifetime = 0.3f;
            flashMain.startSpeed = 0f;
            flashMain.startSize = 1.5f;
            flashMain.startColor = flashColor * emissionIntensity;
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
            curve.AddKey(0.15f, 1.2f);
            curve.AddKey(1f, 0f);
            flashSize.size = new ParticleSystem.MinMaxCurve(1f, curve);

            VFXHelpers.SetupRenderer(flashPS, VFXHelpers.CreateMaterialForColor(baseMaterial, flashColor, emissionIntensity));
            flashPS.Play();
        }
    }
}
