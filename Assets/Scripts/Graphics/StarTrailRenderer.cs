using UnityEngine;
using System.Collections.Generic;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Renders motion trails for close/fast-moving stars.
    /// Creates the "flying through space" effect from TS Starfield.ts.
    /// Uses GL.Lines for efficient trail rendering.
    /// </summary>
    public class StarTrailRenderer : MonoBehaviour
    {
        [Header("Trail Settings")]
        [SerializeField] private float m_trailThreshold = 0.8f; // Stars closer than this get trails (0-1)
        [SerializeField] private float m_minTrailLength = 0.1f;
        [SerializeField] private float m_maxTrailLength = 1.5f;
        [SerializeField] private float m_trailAlpha = 0.6f;
        [SerializeField] private float m_trailWidthMultiplier = 0.4f;

        private Material m_trailMaterial;
        private List<TrailData> m_activeTrails = new List<TrailData>();
        private bool m_isEnabled = true;

        private struct TrailData
        {
            public Vector3 startPos;
            public Vector3 endPos;
            public Color color;
            public float width;
        }

        private void Awake()
        {
            CreateTrailMaterial();
        }

        private void CreateTrailMaterial()
        {
            // Simple unlit line material with additive blending
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            m_trailMaterial = new Material(shader);
            m_trailMaterial.hideFlags = HideFlags.HideAndDontSave;
            m_trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m_trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m_trailMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            m_trailMaterial.SetInt("_ZWrite", 0);
        }

        /// <summary>
        /// Clear all pending trails for this frame.
        /// </summary>
        public void ClearTrails()
        {
            m_activeTrails.Clear();
        }

        /// <summary>
        /// Add a trail for a star if it meets the depth threshold.
        /// </summary>
        public void AddTrail(Vector3 currentPos, Vector3 previousPos, float depthFactor, Color color, float speed)
        {
            if (!m_isEnabled) return;

            // Only draw trails for close stars (high depthFactor = close to camera)
            if (depthFactor < m_trailThreshold) return;

            // Calculate trail length based on speed and depth
            float trailIntensity = (depthFactor - m_trailThreshold) / (1f - m_trailThreshold);
            trailIntensity = Mathf.Clamp01(trailIntensity);

            float trailLength = Mathf.Lerp(m_minTrailLength, m_maxTrailLength, trailIntensity * speed * 0.2f);

            // Calculate trail direction (from previous to current position)
            Vector3 direction = (currentPos - previousPos).normalized;
            Vector3 trailStart = currentPos;
            Vector3 trailEnd = currentPos - direction * trailLength;

            // Fade trail based on depth
            Color trailColor = color;
            trailColor.a = m_trailAlpha * trailIntensity;

            m_activeTrails.Add(new TrailData
            {
                startPos = trailStart,
                endPos = trailEnd,
                color = trailColor,
                width = Mathf.Max(0.01f, trailIntensity * m_trailWidthMultiplier)
            });
        }

        /// <summary>
        /// Render all accumulated trails using GL.
        /// Call this in OnRenderObject or after camera render.
        /// </summary>
        private void OnRenderObject()
        {
            if (m_activeTrails.Count == 0) return;
            if (m_trailMaterial == null) return;

            m_trailMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            GL.Begin(GL.LINES);

            foreach (var trail in m_activeTrails)
            {
                // Bright at star, fading toward tail
                GL.Color(trail.color);
                GL.Vertex(trail.startPos);

                Color fadeColor = trail.color;
                fadeColor.a *= 0.1f;
                GL.Color(fadeColor);
                GL.Vertex(trail.endPos);
            }

            GL.End();
            GL.PopMatrix();
        }

        public void SetEnabled(bool enabled)
        {
            m_isEnabled = enabled;
        }

        private void OnDestroy()
        {
            if (m_trailMaterial != null)
            {
                DestroyImmediate(m_trailMaterial);
            }
        }
    }
}
