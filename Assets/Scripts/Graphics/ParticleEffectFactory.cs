using UnityEngine;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Factory for creating particle effect prefabs at runtime.
    /// Used when we don't have designer-created particle systems.
    /// Creates simple but effective arcade-style effects.
    /// </summary>
    public static class ParticleEffectFactory
    {
        /// <summary>
        /// Global VFX juice multiplier - makes all particles bigger and more dramatic!
        /// </summary>
        private const float GLOBAL_VFX_SCALE = 2.0f; // 2X size for extra juice!

        /// <summary>
        /// Get or create a URP-compatible particle material with proper soft texture
        /// </summary>
        private static Material GetParticleMaterial(Color color, bool additive = true)
        {
            // Use VFXHelpers to create material with proper soft texture and blending
            return VFX.VFXHelpers.CreateParticleMaterial(color, emissionIntensity: 1f, additive: additive);
        }

        /// <summary>
        /// Apply the correct material to a particle system renderer
        /// </summary>
        private static void SetupRenderer(ParticleSystemRenderer renderer, Color color)
        {
            if (renderer == null) return;

            Material mat = GetParticleMaterial(color);
            if (mat != null)
            {
                renderer.material = mat;
            }
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 100;
        }

        /// <summary>
        /// Create a burst explosion effect with multiple layers for visual polish
        /// </summary>
        public static ParticleSystem CreateExplosion(Transform parent, string name, float size, int particleCount, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            // Main particle system - core explosion particles
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(size * 3f * GLOBAL_VFX_SCALE, size * 6f * GLOBAL_VFX_SCALE);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.1f * GLOBAL_VFX_SCALE, size * 0.3f * GLOBAL_VFX_SCALE);
            main.startColor = color;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = particleCount * 2;

            // Emission - burst
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, particleCount)
            });

            // Shape - sphere
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = size * 0.1f * GLOBAL_VFX_SCALE;

            // Color over lifetime - bright flash then fade
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.2f), new GradientColorKey(color * 0.5f, 0.7f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.4f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            // Size over lifetime - shrink
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(0.5f, 0.6f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Renderer
            SetupRenderer(go.GetComponent<ParticleSystemRenderer>(), color);

            // Add sub-emitter for flash/glow core
            AddExplosionFlash(go, size, color);

            // Add sub-emitter for expanding ring
            AddExplosionRing(go, size, color);

            // Add sub-emitter for debris/sparks
            AddExplosionSparks(go, size, color, particleCount / 3);

            go.SetActive(false);
            return ps;
        }

        /// <summary>
        /// Add a bright flash at the explosion center
        /// </summary>
        private static void AddExplosionFlash(GameObject parent, float size, Color color)
        {
            GameObject flashGO = new GameObject("Flash");
            flashGO.transform.SetParent(parent.transform);
            flashGO.transform.localPosition = Vector3.zero;

            ParticleSystem ps = flashGO.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.15f;
            main.loop = false;
            main.startLifetime = 0.15f;
            main.startSpeed = 0f;
            main.startSize = size * 1.5f * GLOBAL_VFX_SCALE;
            main.startColor = Color.white;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 1;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

            var shape = ps.shape;
            shape.enabled = false;

            // Rapid fade
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.5f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            // Quick shrink
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 0.2f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            SetupRenderer(flashGO.GetComponent<ParticleSystemRenderer>(), color);
        }

        /// <summary>
        /// Add an expanding ring effect
        /// </summary>
        private static void AddExplosionRing(GameObject parent, float size, Color color)
        {
            GameObject ringGO = new GameObject("Ring");
            ringGO.transform.SetParent(parent.transform);
            ringGO.transform.localPosition = Vector3.zero;

            ParticleSystem ps = ringGO.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = 0.3f;
            main.startSpeed = size * 8f * GLOBAL_VFX_SCALE;
            main.startSize = size * 0.08f * GLOBAL_VFX_SCALE;
            main.startColor = color;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 24;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 24) });

            // Circle shape for ring
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = size * 0.05f * GLOBAL_VFX_SCALE;
            shape.arc = 360f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

            // Fade out
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color * 0.7f, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            // Shrink as they spread
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 0.3f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            SetupRenderer(ringGO.GetComponent<ParticleSystemRenderer>(), color);
        }

        /// <summary>
        /// Add lingering sparks/debris
        /// </summary>
        private static void AddExplosionSparks(GameObject parent, float size, Color color, int count)
        {
            GameObject sparksGO = new GameObject("Sparks");
            sparksGO.transform.SetParent(parent.transform);
            sparksGO.transform.localPosition = Vector3.zero;

            ParticleSystem ps = sparksGO.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.8f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(size * 1f * GLOBAL_VFX_SCALE, size * 3f * GLOBAL_VFX_SCALE);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.03f * GLOBAL_VFX_SCALE, size * 0.08f * GLOBAL_VFX_SCALE);
            main.startColor = color;
            main.gravityModifier = 0.3f; // Light gravity for debris feel
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = count * 2;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            // Delayed burst for secondary explosion feel
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0.05f, count)
            });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = size * 0.2f * GLOBAL_VFX_SCALE;

            // Twinkle effect - oscillate alpha
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.3f), new GradientColorKey(color * 0.5f, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            SetupRenderer(sparksGO.GetComponent<ParticleSystemRenderer>(), color);
        }

        /// <summary>
        /// Create a hit spark effect
        /// </summary>
        public static ParticleSystem CreateHitSpark(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            // Stop immediately to prevent warnings when modifying properties
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f * GLOBAL_VFX_SCALE, 8f * GLOBAL_VFX_SCALE);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f * GLOBAL_VFX_SCALE, 0.15f * GLOBAL_VFX_SCALE);
            main.startColor = color;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 30;

            // Emission - small burst
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 8, 15)
            });

            // Shape - hemisphere outward
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.1f * GLOBAL_VFX_SCALE;

            // Color over lifetime
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.5f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            // Renderer
            SetupRenderer(go.GetComponent<ParticleSystemRenderer>(), color);

            go.SetActive(false);
            return ps;
        }

        /// <summary>
        /// Create a pickup collect effect with sparkles, burst ring, and rising particles
        /// </summary>
        public static ParticleSystem CreatePickupEffect(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            // Main particle system - sparkle burst outward
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f * GLOBAL_VFX_SCALE, 6f * GLOBAL_VFX_SCALE);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f * GLOBAL_VFX_SCALE, 0.18f * GLOBAL_VFX_SCALE);
            main.startColor = color;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 40;

            // Emission - burst
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 20)
            });

            // Shape - sphere for omni-directional burst
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f * GLOBAL_VFX_SCALE;

            // Color over lifetime - bright flash then fade with color
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(color, 0.2f),
                    new GradientColorKey(color * 0.7f, 0.7f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.9f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            // Size over lifetime - shrink
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(0.3f, 0.8f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Renderer
            SetupRenderer(go.GetComponent<ParticleSystemRenderer>(), color);

            // Add sub-effects
            AddPickupFlash(go, color);
            AddPickupRing(go, color);
            AddPickupSparkles(go, color);

            go.SetActive(false);
            return ps;
        }

        /// <summary>
        /// Add a bright central flash for pickup
        /// </summary>
        private static void AddPickupFlash(GameObject parent, Color color)
        {
            GameObject flashGO = new GameObject("Flash");
            flashGO.transform.SetParent(parent.transform);
            flashGO.transform.localPosition = Vector3.zero;

            ParticleSystem ps = flashGO.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = 0.2f;
            main.startSpeed = 0f;
            main.startSize = 1.2f * GLOBAL_VFX_SCALE;
            main.startColor = Color.white;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 1;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

            var shape = ps.shape;
            shape.enabled = false;

            // Rapid color transition and fade
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.3f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            // Quick expand then fade
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.5f);
            curve.AddKey(0.2f, 1f);
            curve.AddKey(1f, 1.5f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            SetupRenderer(flashGO.GetComponent<ParticleSystemRenderer>(), Color.white);
        }

        /// <summary>
        /// Add an expanding ring for pickup
        /// </summary>
        private static void AddPickupRing(GameObject parent, Color color)
        {
            GameObject ringGO = new GameObject("Ring");
            ringGO.transform.SetParent(parent.transform);
            ringGO.transform.localPosition = Vector3.zero;

            ParticleSystem ps = ringGO.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = 0.4f;
            main.startSpeed = 8f * GLOBAL_VFX_SCALE;
            main.startSize = 0.06f * GLOBAL_VFX_SCALE;
            main.startColor = color;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 20;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) });

            // Circle shape for expanding ring in XY plane
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f * GLOBAL_VFX_SCALE;
            shape.arc = 360f;
            shape.rotation = Vector3.zero; // Emit in XY plane

            // Fade out
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 0.5f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            SetupRenderer(ringGO.GetComponent<ParticleSystemRenderer>(), color);
        }

        /// <summary>
        /// Add rising sparkle particles for pickup
        /// </summary>
        private static void AddPickupSparkles(GameObject parent, Color color)
        {
            GameObject sparklesGO = new GameObject("Sparkles");
            sparklesGO.transform.SetParent(parent.transform);
            sparklesGO.transform.localPosition = Vector3.zero;

            ParticleSystem ps = sparklesGO.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.8f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * GLOBAL_VFX_SCALE, 2f * GLOBAL_VFX_SCALE);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f * GLOBAL_VFX_SCALE, 0.1f * GLOBAL_VFX_SCALE);
            main.startColor = color;
            main.gravityModifier = -1.5f; // Float upward
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 20;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0.05f, 8),
                new ParticleSystem.Burst(0.15f, 6)
            });

            // Emit from small area
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f * GLOBAL_VFX_SCALE;
            shape.rotation = Vector3.zero;

            // Twinkle effect
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(color, 0.2f),
                    new GradientColorKey(color * 1.2f, 0.5f),
                    new GradientColorKey(color, 0.8f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            // Shrink over time
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            SetupRenderer(sparklesGO.GetComponent<ParticleSystemRenderer>(), color);
        }

        /// <summary>
        /// Create a trail effect for moving objects
        /// </summary>
        public static ParticleSystem CreateTrail(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            // Stop immediately to prevent warnings when modifying properties
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f * GLOBAL_VFX_SCALE, 0.2f * GLOBAL_VFX_SCALE);
            main.startColor = color;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.maxParticles = 100;

            // Emission - continuous
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 30;

            // No shape - emit from transform position
            var shape = ps.shape;
            shape.enabled = false;

            // Color over lifetime - fade
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            // Size over lifetime
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Renderer
            SetupRenderer(go.GetComponent<ParticleSystemRenderer>(), color);

            return ps;
        }
    }
}
