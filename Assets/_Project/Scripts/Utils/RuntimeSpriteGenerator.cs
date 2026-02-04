using UnityEngine;

namespace NeuralBreak.Utils
{
    /// <summary>
    /// Generates simple sprites at runtime for testing.
    /// Attach to any GameObject that needs a sprite.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class RuntimeSpriteGenerator : MonoBehaviour
    {
        public enum SpriteShape
        {
            Circle,
            Square,
            Diamond,
            Triangle
        }

        [SerializeField] private SpriteShape m_shape = SpriteShape.Circle;
        [SerializeField] private int m_resolution = 64;
        [SerializeField] private Color m_color = Color.white;
        [SerializeField] private bool m_generateOnAwake = true;

        private SpriteRenderer m_spriteRenderer;

        private void Awake()
        {
            m_spriteRenderer = GetComponent<SpriteRenderer>();

            if (m_generateOnAwake)
            {
                GenerateSprite();
            }
        }

        public void GenerateSprite()
        {
            if (m_spriteRenderer == null)
                m_spriteRenderer = GetComponent<SpriteRenderer>();

            Texture2D texture = CreateTexture(m_shape, m_resolution);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                m_resolution
            );

            m_spriteRenderer.sprite = sprite;
            m_spriteRenderer.color = m_color;
        }

        private Texture2D CreateTexture(SpriteShape shape, int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    bool inside = false;

                    switch (shape)
                    {
                        case SpriteShape.Circle:
                            inside = (dx * dx + dy * dy) <= (radius * radius);
                            break;

                        case SpriteShape.Square:
                            inside = Mathf.Abs(dx) <= radius && Mathf.Abs(dy) <= radius;
                            break;

                        case SpriteShape.Diamond:
                            inside = (Mathf.Abs(dx) + Mathf.Abs(dy)) <= radius;
                            break;

                        case SpriteShape.Triangle:
                            float normalizedY = (y - (size * 0.1f)) / (size * 0.8f);
                            float halfWidth = normalizedY * radius;
                            inside = normalizedY >= 0 && normalizedY <= 1 && Mathf.Abs(dx) <= halfWidth;
                            break;
                    }

                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        // Static helper to generate sprites without component
        public static Sprite CreateCircleSprite(int resolution = 64)
        {
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[resolution * resolution];
            float center = resolution / 2f;
            float radius = resolution / 2f - 1;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    bool inside = (dx * dx + dy * dy) <= (radius * radius);
                    pixels[y * resolution + x] = inside ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                resolution
            );
        }

        public static Sprite CreateSquareSprite(int resolution = 64)
        {
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[resolution * resolution];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                resolution
            );
        }
    }
}
