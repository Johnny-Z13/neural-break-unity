using UnityEngine;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// 80s-style perspective grid floor effect for starfield background.
    /// Manages grid line rendering and scrolling animation.
    /// </summary>
    public class StarGridRenderer
    {
        private readonly LineRenderer _gridRenderer;
        private readonly StarfieldOptimizer _optimizer;
        private readonly Transform _gridTransform;

        private Color _gridColor;
        private float _speed;
        private float _time;
        private readonly float _gridHorizonY;

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
            _gridColor = gridColor;
            _gridHorizonY = gridHorizonY;
            _optimizer = optimizer;

            GameObject gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(parent);
            gridObj.transform.localPosition = new Vector3(0, _gridHorizonY - 15f, BASE_Z_OFFSET);
            gridObj.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);

            _gridTransform = gridObj.transform;
            _gridRenderer = gridObj.AddComponent<LineRenderer>();
            _gridRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _gridRenderer.startColor = _gridColor;
            _gridRenderer.endColor = _gridColor;
            _gridRenderer.startWidth = 0.05f;
            _gridRenderer.endWidth = 0.05f;
            _gridRenderer.sortingOrder = -50;

            CreateGridLines();
        }

        /// <summary>
        /// Create the grid line geometry
        /// </summary>
        private void CreateGridLines()
        {
            const int gridLines = 20;
            const float gridSize = 40f;
            int totalPoints = (gridLines * 2 + 1) * 4;
            _gridRenderer.positionCount = totalPoints;

            int index = 0;
            float spacing = gridSize / gridLines;

            // Horizontal lines (perpendicular to camera)
            for (int i = -gridLines; i <= gridLines; i++)
            {
                _gridRenderer.SetPosition(index++, new Vector3(-gridSize, 0, i * spacing));
                _gridRenderer.SetPosition(index++, new Vector3(gridSize, 0, i * spacing));
            }

            // Vertical lines (going into distance)
            for (int i = -gridLines; i <= gridLines; i++)
            {
                _gridRenderer.SetPosition(index++, new Vector3(i * spacing, 0, -gridSize));
                _gridRenderer.SetPosition(index++, new Vector3(i * spacing, 0, gridSize));
            }
        }

        /// <summary>
        /// Update grid animation (allocation-free)
        /// </summary>
        public void UpdateGrid(float deltaTime, float speed)
        {
            if (_gridRenderer == null) return;

            _time += deltaTime;
            _speed = speed;

            // Scroll grid based on speed for motion effect (allocation-free)
            float newZ = BASE_Z_OFFSET + ((_time * _speed) % 4f);
            Vector3 newPos = _optimizer.UpdateGridPosition(_gridTransform.localPosition, newZ);
            _gridTransform.localPosition = newPos;

            // Fade grid color based on time (allocation-free)
            float alphaMultiplier = BASE_ALPHA + Mathf.Sin(_time * PULSE_SPEED) * PULSE_AMPLITUDE;
            Color fadedColor = _optimizer.CalculateGridColor(_gridColor, alphaMultiplier);
            _gridRenderer.startColor = fadedColor;
            _gridRenderer.endColor = fadedColor;
        }

        /// <summary>
        /// Set grid color
        /// </summary>
        public void SetGridColor(Color color)
        {
            _gridColor = color;
        }

        /// <summary>
        /// Set grid visibility
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_gridRenderer != null && _gridRenderer.gameObject != null)
            {
                _gridRenderer.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Clean up grid resources
        /// </summary>
        public void Destroy()
        {
            if (_gridRenderer != null && _gridRenderer.gameObject != null)
            {
                Object.Destroy(_gridRenderer.gameObject);
            }
        }
    }
}
