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
        private Vector3 _cachedStarPosition;
        private Vector3 _cachedWorldPos;
        private Vector3 _cachedNebulaPos;
        private Vector3 _cachedGridPos;
        private Vector3 _cachedScale;

        // Cached colors for particle updates
        private Color _cachedParticleColor;
        private Color _cachedNebulaColor;
        private Color _cachedGridColor;

        // Cached vector3.zero for comparisons
        private readonly Vector3 _zero = Vector3.zero;

        /// <summary>
        /// Calculate world position for star particle (allocation-free)
        /// Locked to world origin (0,0,0) - does not follow camera
        /// </summary>
        public Vector3 CalculateWorldPosition(Vector3 starPos, float z, float starFieldDepth, Vector3 camPos)
        {
            float scale = starFieldDepth / Mathf.Max(z, 1f);

            // Lock to world origin - stars don't follow camera
            _cachedWorldPos.x = starPos.x * scale * 0.1f;
            _cachedWorldPos.y = starPos.y * scale * 0.1f;
            _cachedWorldPos.z = z * 0.5f - 10f; // Push behind the play area

            return _cachedWorldPos;
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
            _cachedParticleColor.r = baseColor.r * brightness;
            _cachedParticleColor.g = baseColor.g * brightness;
            _cachedParticleColor.b = baseColor.b * brightness;
            _cachedParticleColor.a = brightness;

            return _cachedParticleColor;
        }

        /// <summary>
        /// Calculate nebula color from HSV (allocation-free)
        /// </summary>
        public Color CalculateNebulaColor(float hue, float alpha)
        {
            // HSVToRGB creates allocation, but we do it once and cache
            _cachedNebulaColor = Color.HSVToRGB(hue, 1f, 0.5f);
            _cachedNebulaColor.a = alpha;

            return _cachedNebulaColor;
        }

        /// <summary>
        /// Update nebula position (allocation-free)
        /// </summary>
        public Vector3 UpdateNebulaPosition(Vector3 currentPos, float xOffset, float yOffset, float deltaTime)
        {
            _cachedNebulaPos.x = currentPos.x + xOffset * deltaTime;
            _cachedNebulaPos.y = currentPos.y + yOffset * deltaTime;
            _cachedNebulaPos.z = currentPos.z;

            return _cachedNebulaPos;
        }

        /// <summary>
        /// Calculate nebula scale with pulse (allocation-free)
        /// </summary>
        public Vector3 CalculateNebulaScale(float baseSize, float pulse)
        {
            float scale = baseSize * pulse;
            _cachedScale.x = scale;
            _cachedScale.y = scale;
            _cachedScale.z = scale;

            return _cachedScale;
        }

        /// <summary>
        /// Update grid position (allocation-free)
        /// </summary>
        public Vector3 UpdateGridPosition(Vector3 currentPos, float newZ)
        {
            _cachedGridPos.x = currentPos.x;
            _cachedGridPos.y = currentPos.y;
            _cachedGridPos.z = newZ;

            return _cachedGridPos;
        }

        /// <summary>
        /// Calculate grid color with fade (allocation-free)
        /// </summary>
        public Color CalculateGridColor(Color baseColor, float alphaMultiplier)
        {
            _cachedGridColor.r = baseColor.r;
            _cachedGridColor.g = baseColor.g;
            _cachedGridColor.b = baseColor.b;
            _cachedGridColor.a = baseColor.a * alphaMultiplier;

            return _cachedGridColor;
        }

        /// <summary>
        /// Create star position at spawn (allocation-free)
        /// </summary>
        public Vector3 CreateStarPosition(float angle, float distance, float z)
        {
            _cachedStarPosition.x = Mathf.Cos(angle) * distance;
            _cachedStarPosition.y = Mathf.Sin(angle) * distance;
            _cachedStarPosition.z = z;

            return _cachedStarPosition;
        }
    }
}
