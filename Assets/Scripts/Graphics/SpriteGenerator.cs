using UnityEngine;
using System.Collections.Generic;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Generates simple procedural sprites for game entities.
    /// Creates geometric shapes with color variations for a cohesive arcade look.
    /// </summary>
    public static class SpriteGenerator
    {
        private static Dictionary<string, Sprite> s_cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Create a circle sprite
        /// </summary>
        public static Sprite CreateCircle(int size, Color color, string cacheName = null)
        {
            if (!string.IsNullOrEmpty(cacheName) && s_cache.TryGetValue(cacheName, out var cached))
                return cached;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            int center = size / 2;
            float radius = size / 2f - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist < radius)
                    {
                        // Soft edge
                        float alpha = Mathf.Clamp01((radius - dist) / 2f);
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);

            if (!string.IsNullOrEmpty(cacheName))
                s_cache[cacheName] = sprite;

            return sprite;
        }

        /// <summary>
        /// Create a diamond/rhombus sprite
        /// </summary>
        public static Sprite CreateDiamond(int size, Color color, string cacheName = null)
        {
            if (!string.IsNullOrEmpty(cacheName) && s_cache.TryGetValue(cacheName, out var cached))
                return cached;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            int center = size / 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center);
                    float dy = Mathf.Abs(y - center);
                    float dist = dx + dy;

                    if (dist < center - 1)
                    {
                        float alpha = Mathf.Clamp01((center - 1 - dist) / 2f);
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);

            if (!string.IsNullOrEmpty(cacheName))
                s_cache[cacheName] = sprite;

            return sprite;
        }

        /// <summary>
        /// Create a triangle sprite (pointing up)
        /// </summary>
        public static Sprite CreateTriangle(int size, Color color, string cacheName = null)
        {
            if (!string.IsNullOrEmpty(cacheName) && s_cache.TryGetValue(cacheName, out var cached))
                return cached;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Triangle shape (point at top, base at bottom)
                    float yNorm = 1f - (float)y / size;
                    float halfWidth = yNorm * 0.5f;
                    float xNorm = (float)x / size - 0.5f;

                    if (y < size - 1 && Mathf.Abs(xNorm) < halfWidth)
                    {
                        float edgeDist = Mathf.Min(halfWidth - Mathf.Abs(xNorm), yNorm, 1f - yNorm);
                        float alpha = Mathf.Clamp01(edgeDist * size / 2f);
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);

            if (!string.IsNullOrEmpty(cacheName))
                s_cache[cacheName] = sprite;

            return sprite;
        }

        /// <summary>
        /// Create a hexagon sprite
        /// </summary>
        public static Sprite CreateHexagon(int size, Color color, string cacheName = null)
        {
            if (!string.IsNullOrEmpty(cacheName) && s_cache.TryGetValue(cacheName, out var cached))
                return cached;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            int center = size / 2;
            float radius = size / 2f - 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x - center;
                    float py = y - center;

                    // Hexagon distance function
                    float q2x = Mathf.Abs(px);
                    float q2y = Mathf.Abs(py);
                    float dist = Mathf.Max(q2x * 0.866f + q2y * 0.5f, q2y);

                    if (dist < radius)
                    {
                        float alpha = Mathf.Clamp01((radius - dist) / 2f);
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);

            if (!string.IsNullOrEmpty(cacheName))
                s_cache[cacheName] = sprite;

            return sprite;
        }

        /// <summary>
        /// Create a star sprite
        /// </summary>
        public static Sprite CreateStar(int size, int points, Color color, string cacheName = null)
        {
            if (!string.IsNullOrEmpty(cacheName) && s_cache.TryGetValue(cacheName, out var cached))
                return cached;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            int center = size / 2;
            float outerRadius = size / 2f - 2;
            float innerRadius = outerRadius * 0.4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x - center;
                    float py = y - center;
                    float angle = Mathf.Atan2(py, px);
                    float dist = Mathf.Sqrt(px * px + py * py);

                    // Star distance function
                    float starAngle = Mathf.Repeat(angle + Mathf.PI, Mathf.PI * 2f / points) - Mathf.PI / points;
                    float starRadius = Mathf.Lerp(innerRadius, outerRadius, Mathf.Cos(starAngle * points));

                    if (dist < starRadius)
                    {
                        float alpha = Mathf.Clamp01((starRadius - dist) / 2f);
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);

            if (!string.IsNullOrEmpty(cacheName))
                s_cache[cacheName] = sprite;

            return sprite;
        }

        /// <summary>
        /// Create a glow circle sprite for pickups
        /// </summary>
        public static Sprite CreateGlow(int size, Color color, string cacheName = null)
        {
            if (!string.IsNullOrEmpty(cacheName) && s_cache.TryGetValue(cacheName, out var cached))
                return cached;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            int center = size / 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float normalizedDist = dist / center;

                    if (normalizedDist < 1f)
                    {
                        // Soft radial gradient
                        float alpha = 1f - normalizedDist;
                        alpha = alpha * alpha; // Quadratic falloff
                        tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);

            if (!string.IsNullOrEmpty(cacheName))
                s_cache[cacheName] = sprite;

            return sprite;
        }

        /// <summary>
        /// Clear the sprite cache
        /// </summary>
        public static void ClearCache()
        {
            foreach (var sprite in s_cache.Values)
            {
                if (sprite != null && sprite.texture != null)
                {
                    Object.Destroy(sprite.texture);
                }
            }
            s_cache.Clear();
        }
    }
}
