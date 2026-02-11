using UnityEngine;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// CRT-style scanline visual effect for retro aesthetic.
    /// Creates horizontal scanlines with configurable intensity.
    /// </summary>
    public class ScanlineEffect
    {
        private readonly GameObject m_scanlineObject;
        private readonly Material m_scanlineMaterial;
        private readonly float m_scanlineIntensity;

        private const int TEXTURE_WIDTH = 1;
        private const int TEXTURE_HEIGHT = 4;
        private const float TEXTURE_SCALE_Y = 100f;
        private const float CAMERA_DISTANCE = 1f;

        /// <summary>
        /// Initialize the scanline effect
        /// </summary>
        public ScanlineEffect(Transform parent, float scanlineIntensity)
        {
            m_scanlineIntensity = scanlineIntensity;

            // Create overlay quad with scanline effect
            m_scanlineObject = new GameObject("Scanlines");
            m_scanlineObject.transform.SetParent(parent);

            // Position in front of camera
            var cam = Camera.main;
            if (cam != null)
            {
                m_scanlineObject.transform.position = cam.transform.position + cam.transform.forward * CAMERA_DISTANCE;
                m_scanlineObject.transform.rotation = cam.transform.rotation;
            }

            // Create a simple scanline texture
            Texture2D scanlineTex = CreateScanlineTexture();

            m_scanlineMaterial = new Material(Shader.Find("Sprites/Default"));
            m_scanlineMaterial.mainTexture = scanlineTex;
            m_scanlineMaterial.mainTextureScale = new Vector2(1, TEXTURE_SCALE_Y);

            // Note: Full implementation would need a screen-space quad or post-process effect
            // This creates the material but doesn't render it without additional setup
        }

        /// <summary>
        /// Create procedural scanline texture
        /// </summary>
        private Texture2D CreateScanlineTexture()
        {
            Texture2D scanlineTex = new Texture2D(TEXTURE_WIDTH, TEXTURE_HEIGHT, TextureFormat.RGBA32, false);
            scanlineTex.filterMode = FilterMode.Point;
            scanlineTex.wrapMode = TextureWrapMode.Repeat;

            // Create alternating scanline pattern
            Color darkLine = new Color(0, 0, 0, m_scanlineIntensity);
            Color clearLine = new Color(0, 0, 0, 0);

            scanlineTex.SetPixel(0, 0, darkLine);
            scanlineTex.SetPixel(0, 1, clearLine);
            scanlineTex.SetPixel(0, 2, darkLine);
            scanlineTex.SetPixel(0, 3, clearLine);
            scanlineTex.Apply();

            return scanlineTex;
        }

        /// <summary>
        /// Update scanline effect (currently static, but kept for future animation)
        /// </summary>
        public void UpdateScanlines(float deltaTime)
        {
            // Scanlines are static for now
            // Could add scrolling or flickering effects here if needed
        }

        /// <summary>
        /// Set scanline visibility
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (m_scanlineObject != null)
            {
                m_scanlineObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Set scanline intensity
        /// </summary>
        public void SetIntensity(float intensity)
        {
            // Would need to recreate texture or use shader properties
            // Left as placeholder for potential future enhancement
        }

        /// <summary>
        /// Clean up scanline resources
        /// </summary>
        public void Destroy()
        {
            if (m_scanlineObject != null)
            {
                Object.Destroy(m_scanlineObject);
            }

            if (m_scanlineMaterial != null)
            {
                Object.Destroy(m_scanlineMaterial.mainTexture);
                Object.Destroy(m_scanlineMaterial);
            }
        }
    }
}
