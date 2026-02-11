using UnityEngine;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Renders and applies visual customizations to the player ship.
    /// Handles sprite generation, color application, and trail effects.
    /// </summary>
    public class ShipVisualsRenderer
    {
        private SpriteRenderer m_spriteRenderer;
        private TrailRenderer m_trailRenderer;
        private PlayerController m_player;

        /// <summary>
        /// Initialize the renderer with player references
        /// </summary>
        public void Initialize(PlayerController player)
        {
            m_player = player;
            if (m_player != null)
            {
                m_spriteRenderer = m_player.GetComponent<SpriteRenderer>();
                m_trailRenderer = m_player.GetComponentInChildren<TrailRenderer>();
            }
        }

        /// <summary>
        /// Apply visual customizations from a skin
        /// </summary>
        public void ApplyVisuals(ShipSkin skin)
        {
            if (m_player == null || skin == null)
            {
                Debug.LogWarning("[ShipVisualsRenderer] Cannot apply visuals - missing player or skin");
                return;
            }

            // Ensure components are found
            if (m_spriteRenderer == null)
            {
                m_spriteRenderer = m_player.GetComponent<SpriteRenderer>();
            }
            if (m_trailRenderer == null)
            {
                m_trailRenderer = m_player.GetComponentInChildren<TrailRenderer>();
            }

            // Apply sprite and color
            if (m_spriteRenderer != null)
            {
                m_spriteRenderer.sprite = GenerateShipSprite(skin.shape, 64);
                m_spriteRenderer.color = skin.primaryColor;

                // Apply glow if enabled
                if (skin.hasGlow)
                {
                    // Could add glow material here in the future
                }
            }

            // Apply trail colors
            if (m_trailRenderer != null)
            {
                m_trailRenderer.startColor = skin.trailColor;
                Color endColor = skin.trailColor;
                endColor.a = 0;
                m_trailRenderer.endColor = endColor;
            }
        }

        /// <summary>
        /// Generate a sprite for the given ship shape
        /// </summary>
        private Sprite GenerateShipSprite(ShipShape shape, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;

            switch (shape)
            {
                case ShipShape.Triangle:
                    DrawTriangle(pixels, size, center);
                    break;

                case ShipShape.Diamond:
                    DrawDiamond(pixels, size, center);
                    break;

                case ShipShape.Arrow:
                    DrawArrow(pixels, size, center);
                    break;

                case ShipShape.Circle:
                    DrawCircle(pixels, size, center);
                    break;

                case ShipShape.Hexagon:
                    DrawHexagon(pixels, size, center);
                    break;

                case ShipShape.Star:
                    DrawStar(pixels, size, center);
                    break;

                default:
                    DrawTriangle(pixels, size, center);
                    break;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        #region Shape Drawing Methods

        private void DrawTriangle(Color[] pixels, int size, float center)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float relY = (float)y / size;
                    float halfWidth = (1f - relY) * 0.45f;
                    float relX = (float)x / size - 0.5f;

                    bool inside = y > size * 0.1f && Mathf.Abs(relX) < halfWidth;
                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }
        }

        private void DrawDiamond(Color[] pixels, int size, float center)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center) / center;
                    float dy = Mathf.Abs(y - center) / center;

                    bool inside = dx + dy < 0.8f;
                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }
        }

        private void DrawArrow(Color[] pixels, int size, float center)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float relY = (float)y / size;
                    float relX = (float)x / size - 0.5f;

                    bool inside = false;

                    // Arrow head
                    if (relY > 0.5f)
                    {
                        float halfWidth = (1f - relY) * 0.8f;
                        inside = Mathf.Abs(relX) < halfWidth;
                    }
                    // Arrow body
                    else if (relY > 0.1f)
                    {
                        inside = Mathf.Abs(relX) < 0.15f;
                    }

                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }
        }

        private void DrawCircle(Color[] pixels, int size, float center)
        {
            float radius = size * 0.4f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
        }

        private void DrawHexagon(Color[] pixels, int size, float center)
        {
            float radius = size * 0.4f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float angle = Mathf.Atan2(dy, dx);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Hexagon distance
                    float hexDist = radius / Mathf.Cos(Mathf.Repeat(angle, Mathf.PI / 3f) - Mathf.PI / 6f);

                    bool inside = dist < hexDist;
                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }
        }

        private void DrawStar(Color[] pixels, int size, float center)
        {
            float outerRadius = size * 0.45f;
            float innerRadius = size * 0.2f;
            int points = 5;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float angle = Mathf.Atan2(dy, dx) + Mathf.PI / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Star radius at this angle
                    float normalizedAngle = Mathf.Repeat(angle, Mathf.PI * 2f / points);
                    float t = normalizedAngle / (Mathf.PI / points);
                    float starRadius = Mathf.Lerp(outerRadius, innerRadius, Mathf.Abs(t - 1f));

                    bool inside = dist < starRadius;
                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }
        }

        #endregion
    }
}
