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
        private readonly GameObject[] m_nebulaObjects;
        private readonly StarfieldOptimizer m_optimizer;
        private readonly int m_nebulaCount;
        private readonly float m_nebulaSize;
        private readonly float m_nebulaIntensity;
        private readonly float m_nebulaMoveSpeed;
        private readonly float m_arenaRadius;

        private float m_time;

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
            m_nebulaCount = nebulaCount;
            m_nebulaSize = nebulaSize;
            m_nebulaIntensity = nebulaIntensity;
            m_nebulaMoveSpeed = nebulaMoveSpeed;
            m_optimizer = optimizer;

            // Get arena radius for positioning
            m_arenaRadius = ConfigProvider.Player?.arenaRadius ?? 30f;

            m_nebulaObjects = new GameObject[m_nebulaCount];

            CreateNebulae(parent, starFieldDepth);
        }

        /// <summary>
        /// Create nebula game objects
        /// </summary>
        private void CreateNebulae(Transform parent, float starFieldDepth)
        {
            // Scale nebula spread to arena size
            float spreadX = m_arenaRadius * 1.2f;
            float spreadY = m_arenaRadius * 0.6f;

            for (int i = 0; i < m_nebulaCount; i++)
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
                nebula.transform.localScale = Vector3.one * m_nebulaSize;

                // Create nebula material
                var renderer = nebula.GetComponent<MeshRenderer>();
                Material mat = new Material(Shader.Find("Sprites/Default"));

                // Generate nebula color
                float hue = (i * HUE_SPACING + Random.Range(-0.1f, 0.1f)) % 1f;
                Color nebulaColor = m_optimizer.CalculateNebulaColor(hue, m_nebulaIntensity);
                mat.color = nebulaColor;

                renderer.material = mat;
                renderer.sortingOrder = -100;

                m_nebulaObjects[i] = nebula;
            }
        }

        /// <summary>
        /// Update nebula animations (allocation-free)
        /// </summary>
        public void UpdateNebulae(float deltaTime)
        {
            if (m_nebulaObjects == null) return;

            m_time += deltaTime;

            for (int i = 0; i < m_nebulaObjects.Length; i++)
            {
                if (m_nebulaObjects[i] == null) continue;

                // Slowly drift nebulae (allocation-free)
                Vector3 pos = m_nebulaObjects[i].transform.localPosition;
                float xOffset = Mathf.Sin(m_time * m_nebulaMoveSpeed + i * 2f) * DRIFT_X_MULTIPLIER;
                float yOffset = Mathf.Cos(m_time * m_nebulaMoveSpeed * DRIFT_Y_FREQ + i * 2f) * DRIFT_Y_MULTIPLIER;
                Vector3 newPos = m_optimizer.UpdateNebulaPosition(pos, xOffset, yOffset, deltaTime);
                m_nebulaObjects[i].transform.localPosition = newPos;

                // Pulse size (allocation-free)
                float pulse = 1f + Mathf.Sin(m_time * PULSE_SPEED + i) * PULSE_AMPLITUDE;
                Vector3 newScale = m_optimizer.CalculateNebulaScale(m_nebulaSize, pulse);
                m_nebulaObjects[i].transform.localScale = newScale;

                // Rotate color hue slowly (allocation-free)
                var renderer = m_nebulaObjects[i].GetComponent<MeshRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    float hue = ((i * HUE_SPACING) + m_time * HUE_ROTATION_SPEED) % 1f;
                    Color nebulaColor = m_optimizer.CalculateNebulaColor(hue, m_nebulaIntensity);
                    renderer.material.color = nebulaColor;
                }
            }
        }

        /// <summary>
        /// Set nebula visibility
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (m_nebulaObjects == null) return;

            for (int i = 0; i < m_nebulaObjects.Length; i++)
            {
                if (m_nebulaObjects[i] != null)
                {
                    m_nebulaObjects[i].SetActive(visible);
                }
            }
        }

        /// <summary>
        /// Clean up nebula resources
        /// </summary>
        public void Destroy()
        {
            if (m_nebulaObjects == null) return;

            for (int i = 0; i < m_nebulaObjects.Length; i++)
            {
                if (m_nebulaObjects[i] != null)
                {
                    Object.Destroy(m_nebulaObjects[i]);
                }
            }
        }
    }
}
