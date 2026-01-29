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
        [SerializeField] private float _trailThreshold = 0.8f; // Stars closer than this get trails (0-1)
        [SerializeField] private float _minTrailLength = 0.1f;
        [SerializeField] private float _maxTrailLength = 1.5f;
        [SerializeField] private float _trailAlpha = 0.6f;
        [SerializeField] private float _trailWidthMultiplier = 0.4f;

        private Material _trailMaterial;
        private List<TrailData> _activeTrails = new List<TrailData>();
        private bool _isEnabled = true;

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

            _trailMaterial = new Material(shader);
            _trailMaterial.hideFlags = HideFlags.HideAndDontSave;
            _trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            _trailMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _trailMaterial.SetInt("_ZWrite", 0);
        }

        /// <summary>
        /// Clear all pending trails for this frame.
        /// </summary>
        public void ClearTrails()
        {
            _activeTrails.Clear();
        }

        /// <summary>
        /// Add a trail for a star if it meets the depth threshold.
        /// </summary>
        public void AddTrail(Vector3 currentPos, Vector3 previousPos, float depthFactor, Color color, float speed)
        {
            if (!_isEnabled) return;

            // Only draw trails for close stars (high depthFactor = close to camera)
            if (depthFactor < _trailThreshold) return;

            // Calculate trail length based on speed and depth
            float trailIntensity = (depthFactor - _trailThreshold) / (1f - _trailThreshold);
            trailIntensity = Mathf.Clamp01(trailIntensity);

            float trailLength = Mathf.Lerp(_minTrailLength, _maxTrailLength, trailIntensity * speed * 0.2f);

            // Calculate trail direction (from previous to current position)
            Vector3 direction = (currentPos - previousPos).normalized;
            Vector3 trailStart = currentPos;
            Vector3 trailEnd = currentPos - direction * trailLength;

            // Fade trail based on depth
            Color trailColor = color;
            trailColor.a = _trailAlpha * trailIntensity;

            _activeTrails.Add(new TrailData
            {
                startPos = trailStart,
                endPos = trailEnd,
                color = trailColor,
                width = Mathf.Max(0.01f, trailIntensity * _trailWidthMultiplier)
            });
        }

        /// <summary>
        /// Render all accumulated trails using GL.
        /// Call this in OnRenderObject or after camera render.
        /// </summary>
        private void OnRenderObject()
        {
            if (_activeTrails.Count == 0) return;
            if (_trailMaterial == null) return;

            _trailMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            GL.Begin(GL.LINES);

            foreach (var trail in _activeTrails)
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
            _isEnabled = enabled;
        }

        private void OnDestroy()
        {
            if (_trailMaterial != null)
            {
                DestroyImmediate(_trailMaterial);
            }
        }
    }
}
