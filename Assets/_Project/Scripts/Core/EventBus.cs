using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Simple event bus for decoupled communication between systems.
    /// Provides type-safe events without tight coupling.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _events = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Subscribe to an event type
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
            {
                Debug.LogError($"[EventBus] Cannot subscribe to {typeof(T).Name} - handler is null!");
                return;
            }

            Type eventType = typeof(T);

            if (_events.TryGetValue(eventType, out Delegate existing))
            {
                _events[eventType] = Delegate.Combine(existing, handler);
            }
            else
            {
                _events[eventType] = handler;
            }
        }

        /// <summary>
        /// Unsubscribe from an event type
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
            {
                Debug.LogError($"[EventBus] Cannot unsubscribe from {typeof(T).Name} - handler is null!");
                return;
            }

            Type eventType = typeof(T);

            if (_events.TryGetValue(eventType, out Delegate existing))
            {
                Delegate newDelegate = Delegate.Remove(existing, handler);
                if (newDelegate == null)
                {
                    _events.Remove(eventType);
                }
                else
                {
                    _events[eventType] = newDelegate;
                }
            }
        }

        /// <summary>
        /// Publish an event to all subscribers
        /// </summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            Type eventType = typeof(T);

            if (_events.TryGetValue(eventType, out Delegate handler))
            {
                try
                {
                    (handler as Action<T>)?.Invoke(eventData);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[EventBus] Error invoking event {typeof(T).Name}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Clear all event subscriptions (call on scene unload)
        /// </summary>
        public static void Clear()
        {
            _events.Clear();
        }
    }

    // ============================================
    // GAME EVENTS - Add new event structs here
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
        // Weapon upgrades
        SpreadShot,
        Piercing,
        RapidFire,
        Homing
    }

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
}
