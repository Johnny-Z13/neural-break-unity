using UnityEngine;
using System.Collections.Generic;
using NeuralBreak.Combat;
using NeuralBreak.Entities;

namespace NeuralBreak.Core
{
    // ============================================
    // GAME EVENTS - Neural Break specific events
    // ============================================

    #region Game State Events

    public struct GameStateChangedEvent
    {
        public GameStateType previousState;
        public GameStateType newState;
    }

    public struct GameStartedEvent
    {
        public GameMode mode;
    }

    public struct GameOverEvent
    {
        public GameStats finalStats;
    }

    public struct GamePausedEvent
    {
        public bool isPaused;
    }

    #endregion

    #region Player Events

    public struct PlayerDamagedEvent
    {
        public int damage;
        public int currentHealth;
        public int maxHealth;
        public Vector3 damageSource;
    }

    public struct PlayerHealedEvent
    {
        public int amount;
        public int currentHealth;
        public int maxHealth;
    }

    public struct PlayerDiedEvent
    {
        public Vector3 position;
    }

    public struct PlayerDashedEvent
    {
        public Vector3 direction;
    }

    public struct PlayerThrustStartedEvent { }

    public struct PlayerThrustEndedEvent { }

    public struct PlayerLevelUpEvent
    {
        public int newLevel;
        public int totalXP;
    }

    public struct ShieldChangedEvent
    {
        public int currentShields;
        public int maxShields;
    }

    #endregion

    #region Combat Events

    public struct EnemyKilledEvent
    {
        public EnemyType enemyType;
        public Vector3 position;
        public int scoreValue;
        public int xpValue;
    }

    public struct EnemyDamagedEvent
    {
        public EnemyType enemyType;
        public int damage;
        public int currentHealth;
        public Vector3 position;
    }

    public struct ProjectileFiredEvent
    {
        public Vector3 position;
        public Vector3 direction;
        public int powerLevel;
    }

    public struct ComboChangedEvent
    {
        public int comboCount;
        public float multiplier;
    }

    public struct ScoreChangedEvent
    {
        public int newScore;
        public int delta;
        public Vector3 worldPosition;
    }

    #endregion

    #region Level Events

    public struct LevelStartedEvent
    {
        public int levelNumber;
        public string levelName;
    }

    public struct LevelCompletedEvent
    {
        public int levelNumber;
        public string levelName;
        public float completionTime;
    }

    public struct GameCompletedEvent
    {
        public int finalLevel;
        public float totalTime;
    }

    public struct ObjectiveProgressEvent
    {
        public EnemyType enemyType;
        public int current;
        public int required;
    }

    public struct VictoryEvent
    {
        public GameStats finalStats;
    }

    #endregion

    #region Pickup Events

    public struct PickupCollectedEvent
    {
        public PickupType pickupType;
        public Vector3 position;
    }

    public struct PowerUpChangedEvent
    {
        public int newLevel;
    }

    public struct SpeedUpChangedEvent
    {
        public int newLevel;
    }

    #endregion

    #region Weapon Events

    public struct WeaponHeatChangedEvent
    {
        public float heat;
        public float maxHeat;
        public bool isOverheated;
    }

    public struct WeaponOverheatedEvent
    {
        public float cooldownDuration;
    }

    public struct ProjectileHitEvent
    {
        public Vector3 position;
        public bool hitEnemy;
    }

    #endregion

    #region Smart Bomb Events

    public struct SmartBombActivatedEvent
    {
        public Vector3 position;
    }

    public struct SmartBombCountChangedEvent
    {
        public int count;
        public int maxCount;
    }

    #endregion

    #region Spawn Events

    public struct EnemySpawnWarningEvent
    {
        public EnemyType enemyType;
        public Vector3 spawnPosition;
        public float warningDuration;
    }

    public struct EnemySpawnedEvent
    {
        public EnemyType enemyType;
        public Vector3 position;
    }

    public struct BossEncounterEvent
    {
        public bool isBossActive;
        public int bossHealth;
        public int bossMaxHealth;
        public float healthPercent;
    }

    public struct BossSpawnedEvent
    {
        public EnemyType bossType;
        public Vector3 position;
    }

    public struct BossDefeatedEvent
    {
        public EnemyType bossType;
        public int scoreAwarded;
    }

    #endregion

    #region Weapon Upgrade Events

    public struct WeaponUpgradeActivatedEvent
    {
        public PickupType upgradeType;
        public float duration;
    }

    public struct WeaponUpgradeExpiredEvent
    {
        public PickupType upgradeType;
    }

    // Permanent upgrade events
    public struct PermanentUpgradeAddedEvent
    {
        public UpgradeDefinition upgrade;
    }

    public struct PermanentUpgradeRemovedEvent
    {
        public string upgradeId;
    }

    public struct WeaponModifiersChangedEvent
    {
        public WeaponModifiers modifiers;
    }

    // Card selection events
    public struct UpgradeSelectionStartedEvent
    {
        public List<UpgradeDefinition> options;
    }

    public struct UpgradeSelectedEvent
    {
        public UpgradeDefinition selected;
    }

    public struct UpgradeSelectionCancelledEvent { }

    // Special weapon events
    public struct ExplosionTriggeredEvent
    {
        public Vector2 position;
        public float radius;
    }

    public struct ChainLightningEvent
    {
        public List<EnemyBase> targets;
    }

    #endregion

    #region UI Events

    public struct ScreenFlashRequestEvent
    {
        public Color color;
        public float duration;
    }

    public struct DamageFlashRequestEvent
    {
        public float intensity;
    }

    public struct HealFlashRequestEvent
    {
        public float intensity;
    }

    public struct PickupFlashRequestEvent
    {
        public float intensity;
    }

    #endregion

    // ============================================
    // ENUMS for event data
    // ============================================

    public enum EnemyType
    {
        DataMite,
        ScanDrone,
        ChaosWorm,
        VoidSphere,
        CrystalShard,
        Fizzer,
        UFO,
        Boss
    }

    public enum PickupType
    {
        MedPack,
        Health,         // Alias for MedPack
        PowerUp,
        SpeedUp,
        SpeedBoost,     // Alias for SpeedUp
        Shield,
        Invulnerable,
        XP,
        Score,
        SmartBomb,      // Adds a smart bomb to player inventory
        // Weapon upgrades
        SpreadShot,
        Piercing,
        RapidFire,
        Homing
    }
}
