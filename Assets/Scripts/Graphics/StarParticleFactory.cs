using UnityEngine;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Factory for creating star particle systems with procedural textures.
    /// Handles particle system setup and material creation for starfield.
    /// Creates soft glowing star particles matching the TS Starfield.ts aesthetic.
    /// </summary>
    public class StarParticleFactory
    {
        private const int DEFAULT_CIRCLE_SIZE = 64;
        private const int GLOW_TEXTURE_SIZE = 128; // Larger for better glow quality
        private const float ALPHA_CURVE_EXPONENT = 2.0f; // Softer falloff
        private const float BRIGHTNESS_CURVE_EXPONENT = 0.7f;
        private const float GLOW_RADIUS_MULTIPLIER = 1.5f; // Glow extends beyond core

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
        /// Create star material with procedural texture and proper blending
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
            Texture2D starTexture = CreateGlowingStarTexture(GLOW_TEXTURE_SIZE);

            // Set texture on all common property names
            mat.mainTexture = starTexture;
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", starTexture);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", starTexture);

            mat.color = Color.white;

            // Enable additive blending for glow effect
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 1);   // Additive-ish
            }

            // Set blend mode for additive glow
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.renderQueue = 3000; // Transparent queue

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

        /// <summary>
        /// Create a glowing star texture with bright core and soft halo.
        /// Matches the TS Starfield.ts radial gradient glow effect.
        /// </summary>
        public static Texture2D CreateGlowingStarTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            float center = size / 2f;
            float coreRadius = center * 0.4f;  // Bright core is 40% of total
            float glowRadius = center;          // Glow extends to edge
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha;
                    float brightness;

                    if (dist <= coreRadius)
                    {
                        // Bright core - full brightness with slight falloff
                        float t = dist / coreRadius;
                        brightness = 1f - t * 0.2f; // 100% to 80% brightness in core
                        alpha = 1f;
                    }
                    else if (dist <= glowRadius)
                    {
                        // Glow halo - smooth falloff from core edge to outer edge
                        float t = (dist - coreRadius) / (glowRadius - coreRadius);
                        t = Mathf.Clamp01(t);

                        // Quadratic falloff for soft glow
                        float falloff = 1f - t;
                        falloff = falloff * falloff; // Quadratic

                        brightness = 0.8f * falloff;
                        alpha = falloff;
                    }
                    else
                    {
                        // Outside glow radius
                        brightness = 0f;
                        alpha = 0f;
                    }

                    pixels[y * size + x] = new Color(brightness, brightness, brightness, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
