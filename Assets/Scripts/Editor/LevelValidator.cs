using UnityEngine;
using UnityEditor;
using NeuralBreak.Core;

namespace NeuralBreak.Editor
{
    /// <summary>
    /// Editor tool to validate all 99 levels are completable.
    /// Checks that objectives have matching spawn rates.
    /// </summary>
    public class LevelValidator : EditorWindow
    {
        private Vector2 m_scrollPos;
        private string m_report = "";

        [MenuItem("Neural Break/Validate Levels")]
        public static void ShowWindow()
        {
            GetWindow<LevelValidator>("Level Validator");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Level Validation Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Validate All 99 Levels"))
            {
                ValidateAllLevels();
            }

            if (GUILayout.Button("Print Level 1-10 Details"))
            {
                PrintLevelDetails(1, 10);
            }

            if (GUILayout.Button("Print Level 50 Details"))
            {
                PrintLevelDetails(50, 50);
            }

            if (GUILayout.Button("Print Level 99 Details"))
            {
                PrintLevelDetails(99, 99);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Report:", EditorStyles.boldLabel);

            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos, GUILayout.Height(400));
            EditorGUILayout.TextArea(m_report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void ValidateAllLevels()
        {
            m_report = "=== LEVEL VALIDATION REPORT ===\n\n";
            int issueCount = 0;

            for (int level = 1; level <= 99; level++)
            {
                var config = LevelGenerator.GetLevelConfig(level);
                var issues = ValidateLevel(config);

                if (issues.Length > 0)
                {
                    m_report += $"LEVEL {level} ({config.name}):\n";
                    foreach (var issue in issues)
                    {
                        m_report += $"  - {issue}\n";
                        issueCount++;
                    }
                    m_report += "\n";
                }
            }

            if (issueCount == 0)
            {
                m_report += "All 99 levels are valid and completable!\n";
            }
            else
            {
                m_report = $"FOUND {issueCount} ISSUES:\n\n" + m_report;
            }

            Debug.Log(m_report);
        }

        private string[] ValidateLevel(LevelConfig config)
        {
            var issues = new System.Collections.Generic.List<string>();

            // Check: If objective requires enemy type, spawn rate must not be Infinity
            if (config.objectives.dataMites > 0 && float.IsPositiveInfinity(config.spawnRates.dataMiteRate))
            {
                issues.Add($"Requires {config.objectives.dataMites} DataMites but spawn rate is INFINITY");
            }

            if (config.objectives.scanDrones > 0 && float.IsPositiveInfinity(config.spawnRates.scanDroneRate))
            {
                issues.Add($"Requires {config.objectives.scanDrones} ScanDrones but spawn rate is INFINITY");
            }

            if (config.objectives.chaosWorms > 0 && float.IsPositiveInfinity(config.spawnRates.chaosWormRate))
            {
                issues.Add($"Requires {config.objectives.chaosWorms} ChaosWorms but spawn rate is INFINITY");
            }

            if (config.objectives.voidSpheres > 0 && float.IsPositiveInfinity(config.spawnRates.voidSphereRate))
            {
                issues.Add($"Requires {config.objectives.voidSpheres} VoidSpheres but spawn rate is INFINITY");
            }

            if (config.objectives.crystalShards > 0 && float.IsPositiveInfinity(config.spawnRates.crystalShardRate))
            {
                issues.Add($"Requires {config.objectives.crystalShards} CrystalShards but spawn rate is INFINITY");
            }

            if (config.objectives.fizzers > 0 && float.IsPositiveInfinity(config.spawnRates.fizzerRate))
            {
                issues.Add($"Requires {config.objectives.fizzers} Fizzers but spawn rate is INFINITY");
            }

            if (config.objectives.ufos > 0 && float.IsPositiveInfinity(config.spawnRates.ufoRate))
            {
                issues.Add($"Requires {config.objectives.ufos} UFOs but spawn rate is INFINITY");
            }

            if (config.objectives.bosses > 0 && float.IsPositiveInfinity(config.spawnRates.bossRate))
            {
                issues.Add($"Requires {config.objectives.bosses} Bosses but spawn rate is INFINITY");
            }

            // Check: Total objectives shouldn't be 0
            if (config.objectives.TotalKillsRequired == 0)
            {
                issues.Add("No objectives defined - level has nothing to complete");
            }

            // Check: Spawn rates shouldn't be negative or zero (would cause instant/infinite spawns)
            if (config.spawnRates.dataMiteRate <= 0 && !float.IsPositiveInfinity(config.spawnRates.dataMiteRate))
            {
                issues.Add($"DataMite spawn rate is {config.spawnRates.dataMiteRate} (should be > 0)");
            }

            return issues.ToArray();
        }

        private void PrintLevelDetails(int startLevel, int endLevel)
        {
            m_report = "";

            for (int level = startLevel; level <= endLevel; level++)
            {
                var config = LevelGenerator.GetLevelConfig(level);

                m_report += $"=== LEVEL {level}: {config.name} ===\n";
                m_report += $"OBJECTIVES:\n";
                m_report += $"  DataMites: {config.objectives.dataMites}\n";
                m_report += $"  ScanDrones: {config.objectives.scanDrones}\n";
                m_report += $"  ChaosWorms: {config.objectives.chaosWorms}\n";
                m_report += $"  VoidSpheres: {config.objectives.voidSpheres}\n";
                m_report += $"  CrystalShards: {config.objectives.crystalShards}\n";
                m_report += $"  Fizzers: {config.objectives.fizzers}\n";
                m_report += $"  UFOs: {config.objectives.ufos}\n";
                m_report += $"  Bosses: {config.objectives.bosses}\n";
                m_report += $"  TOTAL: {config.objectives.TotalKillsRequired}\n";
                m_report += $"\nSPAWN RATES (seconds between spawns):\n";
                m_report += $"  DataMite: {FormatRate(config.spawnRates.dataMiteRate)}\n";
                m_report += $"  ScanDrone: {FormatRate(config.spawnRates.scanDroneRate)}\n";
                m_report += $"  ChaosWorm: {FormatRate(config.spawnRates.chaosWormRate)}\n";
                m_report += $"  VoidSphere: {FormatRate(config.spawnRates.voidSphereRate)}\n";
                m_report += $"  CrystalShard: {FormatRate(config.spawnRates.crystalShardRate)}\n";
                m_report += $"  Fizzer: {FormatRate(config.spawnRates.fizzerRate)}\n";
                m_report += $"  UFO: {FormatRate(config.spawnRates.ufoRate)}\n";
                m_report += $"  Boss: {FormatRate(config.spawnRates.bossRate)}\n";

                // Estimate time to complete
                float estimatedTime = EstimateCompletionTime(config);
                m_report += $"\nESTIMATED TIME: {estimatedTime:F0} seconds ({estimatedTime / 60f:F1} minutes)\n";
                m_report += "\n";
            }

            Debug.Log(m_report);
        }

        private string FormatRate(float rate)
        {
            if (float.IsPositiveInfinity(rate)) return "DISABLED";
            return $"{rate:F2}s";
        }

        private float EstimateCompletionTime(LevelConfig config)
        {
            float time = 0;

            // Rough estimate: time = objectives * spawn rate (assuming player kills instantly)
            if (config.objectives.dataMites > 0 && !float.IsPositiveInfinity(config.spawnRates.dataMiteRate))
                time = Mathf.Max(time, config.objectives.dataMites * config.spawnRates.dataMiteRate);

            if (config.objectives.scanDrones > 0 && !float.IsPositiveInfinity(config.spawnRates.scanDroneRate))
                time = Mathf.Max(time, config.objectives.scanDrones * config.spawnRates.scanDroneRate);

            if (config.objectives.chaosWorms > 0 && !float.IsPositiveInfinity(config.spawnRates.chaosWormRate))
                time = Mathf.Max(time, config.objectives.chaosWorms * config.spawnRates.chaosWormRate);

            if (config.objectives.voidSpheres > 0 && !float.IsPositiveInfinity(config.spawnRates.voidSphereRate))
                time = Mathf.Max(time, config.objectives.voidSpheres * config.spawnRates.voidSphereRate);

            if (config.objectives.crystalShards > 0 && !float.IsPositiveInfinity(config.spawnRates.crystalShardRate))
                time = Mathf.Max(time, config.objectives.crystalShards * config.spawnRates.crystalShardRate);

            if (config.objectives.fizzers > 0 && !float.IsPositiveInfinity(config.spawnRates.fizzerRate))
                time = Mathf.Max(time, config.objectives.fizzers * config.spawnRates.fizzerRate);

            if (config.objectives.ufos > 0 && !float.IsPositiveInfinity(config.spawnRates.ufoRate))
                time = Mathf.Max(time, config.objectives.ufos * config.spawnRates.ufoRate);

            if (config.objectives.bosses > 0 && !float.IsPositiveInfinity(config.spawnRates.bossRate))
                time = Mathf.Max(time, config.objectives.bosses * config.spawnRates.bossRate);

            return time;
        }
    }
}
