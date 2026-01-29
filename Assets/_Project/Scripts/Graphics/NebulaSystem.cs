using UnityEngine;
using NeuralBreak.Config;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Manages nebula particle effects for starfield background.
    /// Creates drifting, pulsing nebula clouds with color gradients.
    /// Auto-sizes to match arena boundary.
    /// </summary>
    public class NebulaSystem
    {
        private readonly GameObject[] _nebulaObjects;
        private readonly StarfieldOptimizer _optimizer;
        private readonly int _nebulaCount;
        private readonly float _nebulaSize;
        private readonly float _nebulaIntensity;
        private readonly float _nebulaMoveSpeed;
        private readonly float _arenaRadius;

        private float _time;

        private const float DRIFT_X_MULTIPLIER = 0.5f;
        private const float DRIFT_Y_MULTIPLIER = 0.3f;
        private const float DRIFT_Y_FREQ = 0.8f;
        private const float PULSE_SPEED = 0.2f;
        private const float PULSE_AMPLITUDE = 0.1f;
        private const float HUE_ROTATION_SPEED = 0.02f;
        private const float HUE_SPACING = 0.33f;
        private const float DEPTH_FACTOR = 0.8f;

        /// <summary>
        /// Initialize the nebula system
        /// </summary>
        public NebulaSystem(
            Transform parent,
            int nebulaCount,
            float nebulaSize,
            float nebulaIntensity,
            float nebulaMoveSpeed,
            float starFieldDepth,
            StarfieldOptimizer optimizer)
        {
            _nebulaCount = nebulaCount;
            _nebulaSize = nebulaSize;
            _nebulaIntensity = nebulaIntensity;
            _nebulaMoveSpeed = nebulaMoveSpeed;
            _optimizer = optimizer;

            // Get arena radius for positioning
            _arenaRadius = ConfigProvider.Player?.arenaRadius ?? 30f;

            _nebulaObjects = new GameObject[_nebulaCount];

            CreateNebulae(parent, starFieldDepth);
        }

        /// <summary>
        /// Create nebula game objects
        /// </summary>
        private void CreateNebulae(Transform parent, float starFieldDepth)
        {
            // Scale nebula spread to arena size
            float spreadX = _arenaRadius * 1.2f;
            float spreadY = _arenaRadius * 0.6f;

            for (int i = 0; i < _nebulaCount; i++)
            {
                GameObject nebula = GameObject.CreatePrimitive(PrimitiveType.Quad);
                nebula.name = $"Nebula_{i}";
                nebula.transform.SetParent(parent);

                // Remove collider
                Object.Destroy(nebula.GetComponent<Collider>());

                // Position far in background, scaled to arena
                nebula.transform.localPosition = new Vector3(
                    Random.Range(-spreadX, spreadX),
                    Random.Range(-spreadY, spreadY),
                    starFieldDepth * DEPTH_FACTOR
                );
                nebula.transform.localScale = Vector3.one * _nebulaSize;

                // Create nebula material
                var renderer = nebula.GetComponent<MeshRenderer>();
                Material mat = new Material(Shader.Find("Sprites/Default"));

                // Generate nebula color
                float hue = (i * HUE_SPACING + Random.Range(-0.1f, 0.1f)) % 1f;
                Color nebulaColor = _optimizer.CalculateNebulaColor(hue, _nebulaIntensity);
                mat.color = nebulaColor;

                renderer.material = mat;
                renderer.sortingOrder = -100;

                _nebulaObjects[i] = nebula;
            }
        }

        /// <summary>
        /// Update nebula animations (allocation-free)
        /// </summary>
        public void UpdateNebulae(float deltaTime)
        {
            if (_nebulaObjects == null) return;

            _time += deltaTime;

            for (int i = 0; i < _nebulaObjects.Length; i++)
            {
                if (_nebulaObjects[i] == null) continue;

                // Slowly drift nebulae (allocation-free)
                Vector3 pos = _nebulaObjects[i].transform.localPosition;
                float xOffset = Mathf.Sin(_time * _nebulaMoveSpeed + i * 2f) * DRIFT_X_MULTIPLIER;
                float yOffset = Mathf.Cos(_time * _nebulaMoveSpeed * DRIFT_Y_FREQ + i * 2f) * DRIFT_Y_MULTIPLIER;
                Vector3 newPos = _optimizer.UpdateNebulaPosition(pos, xOffset, yOffset, deltaTime);
                _nebulaObjects[i].transform.localPosition = newPos;

                // Pulse size (allocation-free)
                float pulse = 1f + Mathf.Sin(_time * PULSE_SPEED + i) * PULSE_AMPLITUDE;
                Vector3 newScale = _optimizer.CalculateNebulaScale(_nebulaSize, pulse);
                _nebulaObjects[i].transform.localScale = newScale;

                // Rotate color hue slowly (allocation-free)
                var renderer = _nebulaObjects[i].GetComponent<MeshRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    float hue = ((i * HUE_SPACING) + _time * HUE_ROTATION_SPEED) % 1f;
                    Color nebulaColor = _optimizer.CalculateNebulaColor(hue, _nebulaIntensity);
                    renderer.material.color = nebulaColor;
                }
            }
        }

        /// <summary>
        /// Set nebula visibility
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_nebulaObjects == null) return;

            for (int i = 0; i < _nebulaObjects.Length; i++)
            {
                if (_nebulaObjects[i] != null)
                {
                    _nebulaObjects[i].SetActive(visible);
                }
            }
        }

        /// <summary>
        /// Clean up nebula resources
        /// </summary>
        public void Destroy()
        {
            if (_nebulaObjects == null) return;

            for (int i = 0; i < _nebulaObjects.Length; i++)
            {
                if (_nebulaObjects[i] != null)
                {
                    Object.Destroy(_nebulaObjects[i]);
                }
            }
        }
    }
}
