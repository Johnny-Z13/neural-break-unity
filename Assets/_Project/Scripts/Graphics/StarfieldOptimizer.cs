using UnityEngine;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Caches Vector3/Color allocations to eliminate per-frame GC allocations.
    /// All starfield sub-systems should use this for allocation-free updates.
    /// </summary>
    public class StarfieldOptimizer
    {
        // Cached vectors for star updates
        private Vector3 m_cachedStarPosition;
        private Vector3 m_cachedWorldPos;
        private Vector3 m_cachedNebulaPos;
        private Vector3 m_cachedGridPos;
        private Vector3 m_cachedScale;

        // Cached colors for particle updates
        private Color m_cachedParticleColor;
        private Color m_cachedNebulaColor;
        private Color m_cachedGridColor;

        // Cached vector3.zero for comparisons
        private readonly Vector3 m_zero = Vector3.zero;

        /// <summary>
        /// Calculate world position for star particle (allocation-free)
        /// Locked to world origin (0,0,0) - does not follow camera
        /// </summary>
        public Vector3 CalculateWorldPosition(Vector3 starPos, float z, float starFieldDepth, Vector3 camPos)
        {
            float scale = starFieldDepth / Mathf.Max(z, 1f);

            // Lock to world origin - stars don't follow camera
            m_cachedWorldPos.x = starPos.x * scale * 0.1f;
            m_cachedWorldPos.y = starPos.y * scale * 0.1f;
            m_cachedWorldPos.z = z * 0.5f - 10f; // Push behind the play area

            return m_cachedWorldPos;
        }

        /// <summary>
        /// Update star position based on z-depth (allocation-free)
        /// </summary>
        public void UpdateStarPosition(ref Vector3 position, float x, float y, float z)
        {
            position.x = x;
            position.y = y;
            position.z = z;
        }

        /// <summary>
        /// Calculate particle color with brightness (allocation-free)
        /// </summary>
        public Color CalculateParticleColor(Color baseColor, float brightness)
        {
            m_cachedParticleColor.r = baseColor.r * brightness;
            m_cachedParticleColor.g = baseColor.g * brightness;
            m_cachedParticleColor.b = baseColor.b * brightness;
            m_cachedParticleColor.a = brightness;

            return m_cachedParticleColor;
        }

        /// <summary>
        /// Calculate nebula color from HSV (allocation-free)
        /// </summary>
        public Color CalculateNebulaColor(float hue, float alpha)
        {
            // HSVToRGB creates allocation, but we do it once and cache
            m_cachedNebulaColor = Color.HSVToRGB(hue, 1f, 0.5f);
            m_cachedNebulaColor.a = alpha;

            return m_cachedNebulaColor;
        }

        /// <summary>
        /// Update nebula position (allocation-free)
        /// </summary>
        public Vector3 UpdateNebulaPosition(Vector3 currentPos, float xOffset, float yOffset, float deltaTime)
        {
            m_cachedNebulaPos.x = currentPos.x + xOffset * deltaTime;
            m_cachedNebulaPos.y = currentPos.y + yOffset * deltaTime;
            m_cachedNebulaPos.z = currentPos.z;

            return m_cachedNebulaPos;
        }

        /// <summary>
        /// Calculate nebula scale with pulse (allocation-free)
        /// </summary>
        public Vector3 CalculateNebulaScale(float baseSize, float pulse)
        {
            float scale = baseSize * pulse;
            m_cachedScale.x = scale;
            m_cachedScale.y = scale;
            m_cachedScale.z = scale;

            return m_cachedScale;
        }

        /// <summary>
        /// Update grid position (allocation-free)
        /// </summary>
        public Vector3 UpdateGridPosition(Vector3 currentPos, float newZ)
        {
            m_cachedGridPos.x = currentPos.x;
            m_cachedGridPos.y = currentPos.y;
            m_cachedGridPos.z = newZ;

            return m_cachedGridPos;
        }

        /// <summary>
        /// Calculate grid color with fade (allocation-free)
        /// </summary>
        public Color CalculateGridColor(Color baseColor, float alphaMultiplier)
        {
            m_cachedGridColor.r = baseColor.r;
            m_cachedGridColor.g = baseColor.g;
            m_cachedGridColor.b = baseColor.b;
            m_cachedGridColor.a = baseColor.a * alphaMultiplier;

            return m_cachedGridColor;
        }

        /// <summary>
        /// Create star position at spawn (allocation-free)
        /// </summary>
        public Vector3 CreateStarPosition(float angle, float distance, float z)
        {
            m_cachedStarPosition.x = Mathf.Cos(angle) * distance;
            m_cachedStarPosition.y = Mathf.Sin(angle) * distance;
            m_cachedStarPosition.z = z;

            return m_cachedStarPosition;
        }
    }
}
