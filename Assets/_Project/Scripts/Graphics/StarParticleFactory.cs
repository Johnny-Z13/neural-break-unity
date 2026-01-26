using UnityEngine;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Factory for creating star particle systems with procedural textures.
    /// Handles particle system setup and material creation for starfield.
    /// </summary>
    public class StarParticleFactory
    {
        private const int DEFAULT_CIRCLE_SIZE = 64;
        private const float ALPHA_CURVE_EXPONENT = 1.5f;
        private const float BRIGHTNESS_CURVE_EXPONENT = 0.5f;

        /// <summary>
        /// Create a complete particle system for stars
        /// </summary>
        public static ParticleSystem CreateStarParticleSystem(Transform parent, int maxParticles, float maxStarSize)
        {
            GameObject psGO = new GameObject("StarParticles");
            psGO.transform.SetParent(parent);
            psGO.transform.localPosition = Vector3.zero;

            ParticleSystem starParticles = psGO.AddComponent<ParticleSystem>();

            ConfigureParticleSystemMain(starParticles, maxParticles, maxStarSize);
            ConfigureParticleSystemEmission(starParticles);
            ConfigureParticleSystemRenderer(psGO);

            return starParticles;
        }

        /// <summary>
        /// Configure main particle system module
        /// </summary>
        private static void ConfigureParticleSystemMain(ParticleSystem ps, int maxParticles, float maxStarSize)
        {
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = float.MaxValue;
            main.startSpeed = 0;
            main.startSize = maxStarSize;
            main.startColor = Color.white;
        }

        /// <summary>
        /// Configure emission module (disabled for manual control)
        /// </summary>
        private static void ConfigureParticleSystemEmission(ParticleSystem ps)
        {
            var emission = ps.emission;
            emission.enabled = false;
        }

        /// <summary>
        /// Configure particle renderer with star material
        /// </summary>
        private static void ConfigureParticleSystemRenderer(GameObject psGO)
        {
            var renderer = psGO.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            Material starMaterial = CreateStarMaterial();
            renderer.material = starMaterial ?? CreateFallbackMaterial();
        }

        /// <summary>
        /// Create star material with procedural texture
        /// </summary>
        private static Material CreateStarMaterial()
        {
            Shader shader = FindBestParticleShader();
            if (shader == null)
            {
                Debug.LogError("[StarParticleFactory] Could not find any suitable shader!");
                return null;
            }

            Material mat = new Material(shader);
            mat.mainTexture = CreateCircleTexture(DEFAULT_CIRCLE_SIZE);
            mat.color = Color.white;

            Debug.Log($"[StarParticleFactory] Created star material with shader: {shader.name}");
            return mat;
        }

        /// <summary>
        /// Find the best available particle shader
        /// </summary>
        private static Shader FindBestParticleShader()
        {
            return Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                   Shader.Find("Sprites/Default") ??
                   Shader.Find("Particles/Standard Unlit");
        }

        /// <summary>
        /// Create fallback material
        /// </summary>
        private static Material CreateFallbackMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            return new Material(shader);
        }

        /// <summary>
        /// Create a soft circular gradient texture for star particles
        /// </summary>
        public static Texture2D CreateCircleTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            float center = size / 2f;
            float maxDist = center;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = 1f - Mathf.Clamp01(dist / maxDist);
                    alpha = Mathf.Pow(alpha, ALPHA_CURVE_EXPONENT);
                    float brightness = Mathf.Pow(alpha, BRIGHTNESS_CURVE_EXPONENT);

                    pixels[y * size + x] = new Color(brightness, brightness, brightness, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
