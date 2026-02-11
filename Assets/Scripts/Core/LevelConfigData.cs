using UnityEngine;
using System;

namespace NeuralBreak.Core
{
    /// <summary>
    /// ScriptableObject containing level configuration.
    /// Can be used for individual level definitions or generated dynamically.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Neural Break/Level Config")]
    public class LevelConfigData : ScriptableObject
    {
        [Header("Level Info")]
        public int levelNumber = 1;
        public string levelName = "NEURAL INITIALIZATION";

        [Header("Kill Objectives")]
        public LevelObjectives objectives;

        [Header("Spawn Rates (seconds between spawns)")]
        public SpawnRates spawnRates;
    }

    /// <summary>
    /// Kill objectives for a level - how many of each enemy must be killed
    /// </summary>
    [Serializable]
    public struct LevelObjectives
    {
        [Tooltip("Number of DataMites to kill")]
        public int dataMites;

        [Tooltip("Number of ScanDrones to kill")]
        public int scanDrones;

        [Tooltip("Number of ChaosWorms to kill")]
        public int chaosWorms;

        [Tooltip("Number of VoidSpheres to kill")]
        public int voidSpheres;

        [Tooltip("Number of CrystalShards to kill")]
        public int crystalShards;

        [Tooltip("Number of Fizzers to kill")]
        public int fizzers;

        [Tooltip("Number of UFOs to kill")]
        public int ufos;

        [Tooltip("Number of Bosses to kill")]
        public int bosses;

        public int TotalKillsRequired =>
            dataMites + scanDrones + chaosWorms + voidSpheres +
            crystalShards + fizzers + ufos + bosses;

        public static LevelObjectives Empty => new LevelObjectives();
    }

    /// <summary>
    /// Spawn rates for each enemy type (seconds between spawns)
    /// Use Infinity to disable a spawn type
    /// </summary>
    [Serializable]
    public struct SpawnRates
    {
        [Tooltip("Seconds between DataMite spawns")]
        public float dataMiteRate;

        [Tooltip("Seconds between ScanDrone spawns")]
        public float scanDroneRate;

        [Tooltip("Seconds between ChaosWorm spawns")]
        public float chaosWormRate;

        [Tooltip("Seconds between VoidSphere spawns")]
        public float voidSphereRate;

        [Tooltip("Seconds between CrystalShard spawns")]
        public float crystalShardRate;

        [Tooltip("Seconds between Fizzer spawns")]
        public float fizzerRate;

        [Tooltip("Seconds between UFO spawns")]
        public float ufoRate;

        [Tooltip("Seconds between Boss spawns")]
        public float bossRate;

        public static SpawnRates Default => new SpawnRates
        {
            dataMiteRate = 2.0f,
            scanDroneRate = 8.0f,
            chaosWormRate = 15.0f,
            voidSphereRate = 20.0f,
            crystalShardRate = 25.0f,
            fizzerRate = 12.0f,
            ufoRate = 50.0f,
            bossRate = 120.0f
        };

        public static SpawnRates Disabled => new SpawnRates
        {
            dataMiteRate = float.PositiveInfinity,
            scanDroneRate = float.PositiveInfinity,
            chaosWormRate = float.PositiveInfinity,
            voidSphereRate = float.PositiveInfinity,
            crystalShardRate = float.PositiveInfinity,
            fizzerRate = float.PositiveInfinity,
            ufoRate = float.PositiveInfinity,
            bossRate = float.PositiveInfinity
        };
    }

    /// <summary>
    /// Level progress tracking - how many of each enemy have been killed
    /// </summary>
    [Serializable]
    public struct LevelProgress
    {
        public int dataMites;
        public int scanDrones;
        public int chaosWorms;
        public int voidSpheres;
        public int crystalShards;
        public int fizzers;
        public int ufos;
        public int bosses;

        public int TotalKills =>
            dataMites + scanDrones + chaosWorms + voidSpheres +
            crystalShards + fizzers + ufos + bosses;

        public static LevelProgress Empty => new LevelProgress();

        /// <summary>
        /// Check if all objectives are met
        /// </summary>
        public bool MeetsObjectives(LevelObjectives objectives)
        {
            return dataMites >= objectives.dataMites &&
                   scanDrones >= objectives.scanDrones &&
                   chaosWorms >= objectives.chaosWorms &&
                   voidSpheres >= objectives.voidSpheres &&
                   crystalShards >= objectives.crystalShards &&
                   fizzers >= objectives.fizzers &&
                   ufos >= objectives.ufos &&
                   bosses >= objectives.bosses;
        }

        /// <summary>
        /// Get progress percentage towards objectives
        /// </summary>
        public float GetProgressPercent(LevelObjectives objectives)
        {
            int totalNeeded = objectives.TotalKillsRequired;
            if (totalNeeded == 0) return 100f;

            int totalAchieved =
                Mathf.Min(dataMites, objectives.dataMites) +
                Mathf.Min(scanDrones, objectives.scanDrones) +
                Mathf.Min(chaosWorms, objectives.chaosWorms) +
                Mathf.Min(voidSpheres, objectives.voidSpheres) +
                Mathf.Min(crystalShards, objectives.crystalShards) +
                Mathf.Min(fizzers, objectives.fizzers) +
                Mathf.Min(ufos, objectives.ufos) +
                Mathf.Min(bosses, objectives.bosses);

            return Mathf.Min(100f, (float)totalAchieved / totalNeeded * 100f);
        }
    }
}
