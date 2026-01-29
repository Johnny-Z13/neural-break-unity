using UnityEngine;

namespace NeuralBreak.Graphics.VFX
{
    /// <summary>
    /// Shared helper methods for VFX generation.
    /// Static utility class to avoid code duplication across VFX generators.
    /// </summary>
    public static class VFXHelpers
    {
        // Cached soft particle texture (generated once, reused)
        private static Texture2D _softParticleTexture;

        /// <summary>
        /// Gets or creates a soft circular particle texture for proper particle rendering.
        /// </summary>
        public static Texture2D GetSoftParticleTexture()
        {
            if (_softParticleTexture != null) return _softParticleTexture;

            int size = 64;
            _softParticleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _softParticleTexture.filterMode = FilterMode.Bilinear;
            _softParticleTexture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;
            float maxDist = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Soft falloff from center
                    float alpha = 1f - Mathf.Clamp01(dist / maxDist);
                    alpha = alpha * alpha; // Quadratic falloff for softer edges

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            _softParticleTexture.SetPixels(pixels);
            _softParticleTexture.Apply();
            return _softParticleTexture;
        }

        /// <summary>
        /// Creates a base particle system with common defaults for 2D effects.
        /// </summary>
        public static ParticleSystem CreateBaseParticleSystem(GameObject parent, string name)
        {
            var psGO = new GameObject(name);
            psGO.transform.SetParent(parent.transform);
            psGO.transform.localPosition = Vector3.zero;

            var ps = psGO.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;

            var shape = ps.shape;
            shape.enabled = true;
            shape.rotation = Vector3.zero; // Emit in XY plane for 2D

            return ps;
        }

        /// <summary>
        /// Sets up color fade over lifetime with alpha fadeout.
        /// </summary>
        public static void SetupColorFade(ParticleSystem ps, Color startColor, Color endColor)
        {
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(startColor, 0.4f),
                    new GradientColorKey(endColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        /// <summary>
        /// Sets up size shrinking over lifetime.
        /// </summary>
        public static void SetupShrink(ParticleSystem ps)
        {
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        }

        /// <summary>
        /// Sets up the particle renderer with material and settings.
        /// </summary>
        public static void SetupRenderer(ParticleSystem ps, Material material)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 100;
        }

        /// <summary>
        /// Creates a material instance with the specified color and emission.
        /// Assigns a soft circular particle texture to avoid quad rendering.
        /// </summary>
        public static Material CreateMaterialForColor(Material baseMaterial, Color color, float emissionIntensity)
        {
            var mat = new Material(baseMaterial);
            Color emissiveColor = color * emissionIntensity;

            // Assign soft particle texture to avoid quad rendering
            var softTexture = GetSoftParticleTexture();
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", softTexture);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", softTexture);

            mat.SetColor("_BaseColor", emissiveColor);
            mat.SetColor("_Color", emissiveColor);
            mat.SetColor("_EmissionColor", emissiveColor);

            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 1);
            }

            return mat;
        }

        /// <summary>
        /// Adds a flash effect to the parent GameObject.
        /// </summary>
        public static void AddFlash(GameObject parent, Material baseMaterial, Color color, float size, float emissionIntensity)
        {
            var flashGO = new GameObject("Flash");
            flashGO.transform.SetParent(parent.transform);
            flashGO.transform.localPosition = Vector3.zero;

            var flashPS = flashGO.AddComponent<ParticleSystem>();
            flashPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = flashPS.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = 0.15f;
            main.startSpeed = 0f;
            main.startSize = size;
            main.startColor = color * emissionIntensity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 1;

            var emission = flashPS.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

            var shape = flashPS.shape;
            shape.enabled = false;

            var colorOverLifetime = flashPS.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.5f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = flashPS.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0.8f);
            curve.AddKey(0.2f, 1.2f);
            curve.AddKey(1f, 0.5f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            SetupRenderer(flashPS, CreateMaterialForColor(baseMaterial, color, emissionIntensity));
            flashPS.Play();
        }
    }
}
