using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NeuralBreak.Editor
{
    /// <summary>
    /// Utility to fix "Particle Orbital Velocity curves must all be in the same mode" warnings.
    /// Run from: Tools > Neural Break > Fix Particle Systems
    /// </summary>
    public static class ParticleSystemFixer
    {
        [MenuItem("Neural Break/Fix Particle Systems")]
        public static void FixAllParticleSystems()
        {
            int fixedCount = 0;
            List<string> fixedPaths = new List<string>();

            // Find all prefabs
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" });

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    bool modified = false;
                    ParticleSystem[] particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);

                    foreach (ParticleSystem ps in particleSystems)
                    {
                        if (FixParticleSystem(ps))
                        {
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        EditorUtility.SetDirty(prefab);
                        fixedPaths.Add(path);
                        fixedCount++;
                    }
                }
            }

            if (fixedCount > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[ParticleSystemFixer] Fixed {fixedCount} prefabs:");
                foreach (string path in fixedPaths)
                {
                    Debug.Log($"  - {path}");
                }
            }
            else
            {
                Debug.Log("[ParticleSystemFixer] No particle systems needed fixing!");
            }
        }

        private static bool FixParticleSystem(ParticleSystem ps)
        {
            bool modified = false;

            // Fix Orbital Velocity module
            var orbitalVelocity = ps.velocityOverLifetime;
            if (orbitalVelocity.enabled && orbitalVelocity.orbitalOffsetX.mode != orbitalVelocity.orbitalOffsetY.mode)
            {
                // Get the most common mode or default to Constant
                var targetMode = ParticleSystemCurveMode.Constant;

                // Set all three axes to the same mode
                var x = orbitalVelocity.orbitalOffsetX;
                var y = orbitalVelocity.orbitalOffsetY;
                var z = orbitalVelocity.orbitalOffsetZ;

                // Use the mode of X as the target
                targetMode = x.mode;

                // Create new curves with matching modes
                orbitalVelocity.orbitalOffsetX = new ParticleSystem.MinMaxCurve(x.constant, x.curve);
                orbitalVelocity.orbitalOffsetY = new ParticleSystem.MinMaxCurve(y.constant, y.curve);
                orbitalVelocity.orbitalOffsetZ = new ParticleSystem.MinMaxCurve(z.constant, z.curve);

                modified = true;
                Debug.Log($"[ParticleSystemFixer] Fixed orbital velocity on: {ps.gameObject.name}");
            }

            return modified;
        }

        [MenuItem("Neural Break/Disable Orbital Velocity in All Particle Systems")]
        public static void DisableOrbitalVelocityAll()
        {
            int disabledCount = 0;

            // Find all prefabs
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" });

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    bool modified = false;
                    ParticleSystem[] particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);

                    foreach (ParticleSystem ps in particleSystems)
                    {
                        var velocityOverLifetime = ps.velocityOverLifetime;

                        // Check if any orbital velocity is enabled
                        if (velocityOverLifetime.enabled)
                        {
                            // Check if orbital offsets are being used (non-zero)
                            bool hasOrbital = velocityOverLifetime.orbitalOffsetX.constant != 0 ||
                                            velocityOverLifetime.orbitalOffsetY.constant != 0 ||
                                            velocityOverLifetime.orbitalOffsetZ.constant != 0;

                            if (hasOrbital)
                            {
                                // Set all orbital offsets to zero
                                velocityOverLifetime.orbitalOffsetX = new ParticleSystem.MinMaxCurve(0);
                                velocityOverLifetime.orbitalOffsetY = new ParticleSystem.MinMaxCurve(0);
                                velocityOverLifetime.orbitalOffsetZ = new ParticleSystem.MinMaxCurve(0);

                                modified = true;
                                disabledCount++;
                            }
                        }
                    }

                    if (modified)
                    {
                        EditorUtility.SetDirty(prefab);
                    }
                }
            }

            if (disabledCount > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[ParticleSystemFixer] Disabled orbital velocity in {disabledCount} particle systems");
            }
            else
            {
                Debug.Log("[ParticleSystemFixer] No orbital velocity found to disable");
            }
        }

        [MenuItem("Neural Break/List Particle Systems Using Orbital Velocity")]
        public static void ListParticleSystemsWithOrbitalVelocity()
        {
            List<string> results = new List<string>();

            // Find all prefabs
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" });

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    ParticleSystem[] particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);

                    foreach (ParticleSystem ps in particleSystems)
                    {
                        var velocityOverLifetime = ps.velocityOverLifetime;

                        if (velocityOverLifetime.enabled)
                        {
                            bool hasOrbital = velocityOverLifetime.orbitalOffsetX.constant != 0 ||
                                            velocityOverLifetime.orbitalOffsetY.constant != 0 ||
                                            velocityOverLifetime.orbitalOffsetZ.constant != 0;

                            if (hasOrbital)
                            {
                                results.Add($"{path} > {ps.gameObject.name}");
                            }
                        }
                    }
                }
            }

            if (results.Count > 0)
            {
                Debug.Log($"[ParticleSystemFixer] Found {results.Count} particle systems using orbital velocity:");
                foreach (string result in results)
                {
                    Debug.Log($"  - {result}");
                }
            }
            else
            {
                Debug.Log("[ParticleSystemFixer] No particle systems using orbital velocity found");
            }
        }
    }
}
