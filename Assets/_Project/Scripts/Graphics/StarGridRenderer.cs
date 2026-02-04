using UnityEngine;
using NeuralBreak.Config;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// 80s-style perspective grid floor effect for starfield background.
    /// Manages grid line rendering and scrolling animation.
    /// Auto-sizes to match arena boundary.
    /// </summary>
    public class StarGridRenderer
    {
        private readonly LineRenderer m_gridRenderer;
        private readonly StarfieldOptimizer m_optimizer;
        private readonly Transform m_gridTransform;
        private readonly float m_gridSize;

        private Color m_gridColor;
        private float m_speed;
        private float m_time;
        private readonly float m_gridHorizonY;

        private const float BASE_Z_OFFSET = 30f;
        private const float SCROLL_SPEED_MULTIPLIER = 2f;
        private const float PULSE_SPEED = 0.5f;
        private const float PULSE_AMPLITUDE = 0.2f;
        private const float BASE_ALPHA = 0.5f;

        /// <summary>
        /// Initialize the grid renderer system
        /// </summary>
        public StarGridRenderer(Transform parent, Color gridColor, float gridHorizonY, StarfieldOptimizer optimizer)
        {
            m_gridColor = gridColor;
            m_gridHorizonY = gridHorizonY;
            m_optimizer = optimizer;

            // Size grid to match arena boundary
            float arenaRadius = ConfigProvider.Player?.arenaRadius ?? 30f;
            m_gridSize = arenaRadius * 1.5f;

            GameObject gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(parent);
            gridObj.transform.localPosition = new Vector3(0, m_gridHorizonY - 15f, BASE_Z_OFFSET);
            gridObj.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);

            m_gridTransform = gridObj.transform;
            m_gridRenderer = gridObj.AddComponent<LineRenderer>();
            m_gridRenderer.material = new Material(Shader.Find("Sprites/Default"));
            m_gridRenderer.startColor = m_gridColor;
            m_gridRenderer.endColor = m_gridColor;
            m_gridRenderer.startWidth = 0.05f;
            m_gridRenderer.endWidth = 0.05f;
            m_gridRenderer.sortingOrder = -50;

            CreateGridLines();
        }

        /// <summary>
        /// Create the grid line geometry
        /// </summary>
        private void CreateGridLines()
        {
            const int gridLines = 20;
            int totalPoints = (gridLines * 2 + 1) * 4;
            m_gridRenderer.positionCount = totalPoints;

            int index = 0;
            float spacing = m_gridSize / gridLines;

            // Horizontal lines (perpendicular to camera)
            for (int i = -gridLines; i <= gridLines; i++)
            {
                m_gridRenderer.SetPosition(index++, new Vector3(-m_gridSize, 0, i * spacing));
                m_gridRenderer.SetPosition(index++, new Vector3(m_gridSize, 0, i * spacing));
            }

            // Vertical lines (going into distance)
            for (int i = -gridLines; i <= gridLines; i++)
            {
                m_gridRenderer.SetPosition(index++, new Vector3(i * spacing, 0, -m_gridSize));
                m_gridRenderer.SetPosition(index++, new Vector3(i * spacing, 0, m_gridSize));
            }
        }

        /// <summary>
        /// Update grid animation (allocation-free)
        /// </summary>
        public void UpdateGrid(float deltaTime, float speed)
        {
            if (m_gridRenderer == null) return;

            m_time += deltaTime;
            m_speed = speed;

            // Scroll grid based on speed for motion effect (allocation-free)
            float newZ = BASE_Z_OFFSET + ((m_time * m_speed) % 4f);
            Vector3 newPos = m_optimizer.UpdateGridPosition(m_gridTransform.localPosition, newZ);
            m_gridTransform.localPosition = newPos;

            // Fade grid color based on time (allocation-free)
            float alphaMultiplier = BASE_ALPHA + Mathf.Sin(m_time * PULSE_SPEED) * PULSE_AMPLITUDE;
            Color fadedColor = m_optimizer.CalculateGridColor(m_gridColor, alphaMultiplier);
            m_gridRenderer.startColor = fadedColor;
            m_gridRenderer.endColor = fadedColor;
        }

        /// <summary>
        /// Set grid color
        /// </summary>
        public void SetGridColor(Color color)
        {
            m_gridColor = color;
        }

        /// <summary>
        /// Set grid visibility
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (m_gridRenderer != null && m_gridRenderer.gameObject != null)
            {
                m_gridRenderer.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Clean up grid resources
        /// </summary>
        public void Destroy()
        {
            if (m_gridRenderer != null && m_gridRenderer.gameObject != null)
            {
                Object.Destroy(m_gridRenderer.gameObject);
            }
        }
    }
}
