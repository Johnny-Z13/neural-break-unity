using UnityEngine;
using NeuralBreak.Audio;
using NeuralBreak.Entities;
using NeuralBreak.Combat;
using NeuralBreak.Graphics;
using NeuralBreak.Input;
using NeuralBreak.UI;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Auto-wires scene references at runtime using reflection.
    /// Uses SerializeField references to avoid FindFirstObjectByType calls where possible.
    /// Optional systems are created at runtime if they don't exist in the scene.
    /// </summary>
    public class SceneReferenceWiring : MonoBehaviour
    {
        [Header("Required Scene References - Drag from Hierarchy")]
        [SerializeField] private PlayerController m_player;
        [SerializeField] private CameraController m_cameraController;
        [SerializeField] private EnemySpawner m_enemySpawner;
        [SerializeField] private EnemyProjectilePool m_enemyProjectilePool;
        [SerializeField] private GameManager m_gameManager;
        [SerializeField] private LevelManager m_levelManager;

        [Header("Optional System References (Leave empty to auto-create)")]
        [SerializeField] private SpawnWarningIndicator m_spawnWarningIndicator;
        [SerializeField] private LowHealthVignette m_lowHealthVignette;
        [SerializeField] private HighScoreManager m_highScoreManager;
        [SerializeField] private BossHealthBar m_bossHealthBar;
        [SerializeField] private ControlsOverlay m_controlsOverlay;
        [SerializeField] private Combat.WeaponUpgradeManager m_weaponUpgradeManager;
        [SerializeField] private ActiveUpgradesDisplay m_activeUpgradesDisplay;
        [SerializeField] private PlayerLevelSystem m_playerLevelSystem;
        [SerializeField] private UI.XPBarDisplay m_xpBarDisplay;
        [SerializeField] private UI.LevelUpAnnouncement m_levelUpAnnouncement;
        [SerializeField] private UI.DamageNumberPopup m_damageNumberPopup;
        [SerializeField] private UI.WaveAnnouncement m_waveAnnouncement;
        [SerializeField] private UI.StatisticsScreen m_statisticsScreen;
        [SerializeField] private Graphics.ArenaManager m_arenaManager;
        [SerializeField] private Input.GamepadRumble m_gamepadRumble;
        [SerializeField] private UI.Minimap m_minimap;
        [SerializeField] private AccessibilityManager m_accessibilityManager;
        [SerializeField] private SaveSystem m_saveSystem;
        [SerializeField] private Audio.MusicManager m_musicManager;
        [SerializeField] private Graphics.EnvironmentParticles m_environmentParticles;
        [SerializeField] private Entities.ShipCustomization m_shipCustomization;
        [SerializeField] private Graphics.EnemyDeathVFX m_enemyDeathVFX;
        [SerializeField] private UI.UIFeedbacks m_uiFeedbacks;
        [SerializeField] private AchievementSystem m_achievementSystem;

        [Header("Prefab References - Drag from Project")]
        [SerializeField] private Projectile m_projectilePrefab;
        [SerializeField] private EnemyProjectile m_enemyProjectilePrefab;
        [SerializeField] private DataMite m_dataMitePrefab;
        [SerializeField] private ScanDrone m_scanDronePrefab;
        [SerializeField] private Fizzer m_fizzerPrefab;
        [SerializeField] private UFO m_ufoPrefab;
        [SerializeField] private ChaosWorm m_chaosWormPrefab;
        [SerializeField] private VoidSphere m_voidSpherePrefab;
        [SerializeField] private CrystalShard m_crystalShardPrefab;
        [SerializeField] private Boss m_bossPrefab;

        /// <summary>
        /// Wires up all scene references using reflection where necessary.
        /// This method should be called during initialization.
        /// </summary>
        public void WireSceneReferences()
        {
            Debug.Log($"[SceneReferenceWiring] WireSceneReferences START at {Time.realtimeSinceStartup:F3}s");

            // Auto-find required references if not assigned in Inspector
            AutoFindRequiredReferences();

            if (m_player == null)
            {
                Debug.LogError("[SceneReferenceWiring] Player reference is missing! Could not find PlayerController in scene.");
                return;
            }

            // Setup Camera
            SetupCamera();

            // Setup EnemySpawner
            SetupEnemySpawner();

            // Setup WeaponSystem
            SetupWeaponSystem();

            // Setup EnemyProjectilePool
            SetupEnemyProjectilePool();

            // Setup GameManager
            SetupGameManager();

            Debug.Log($"[SceneReferenceWiring] Core setup done at {Time.realtimeSinceStartup:F3}s, starting optional systems...");

            // Setup all optional systems (create if missing)
            SetupOptionalSystems();

            Debug.Log($"[SceneReferenceWiring] Scene references configured at {Time.realtimeSinceStartup:F3}s!");
        }

        private void SetupCamera()
        {
            if (m_cameraController == null)
            {
                Debug.LogWarning("[SceneReferenceWiring] CameraController reference is missing!");
                return;
            }

            m_cameraController.SetTarget(m_player.transform);
            m_cameraController.SnapToTarget();
            Debug.Log("[SceneReferenceWiring] Camera target set to Player");
        }

        private void SetupEnemySpawner()
        {
            if (m_enemySpawner == null)
            {
                Debug.LogWarning("[SceneReferenceWiring] EnemySpawner reference is missing!");
                return;
            }

            // Use reflection to set serialized fields
            var spawnerType = typeof(EnemySpawner);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // Set player target
            spawnerType.GetField("m_playerTarget", bindingFlags)?.SetValue(m_enemySpawner, m_player.transform);

            // Set enemy prefabs
            if (m_dataMitePrefab != null)
                spawnerType.GetField("m_dataMitePrefab", bindingFlags)?.SetValue(m_enemySpawner, m_dataMitePrefab);
            if (m_scanDronePrefab != null)
                spawnerType.GetField("m_scanDronePrefab", bindingFlags)?.SetValue(m_enemySpawner, m_scanDronePrefab);
            if (m_fizzerPrefab != null)
                spawnerType.GetField("m_fizzerPrefab", bindingFlags)?.SetValue(m_enemySpawner, m_fizzerPrefab);
            if (m_ufoPrefab != null)
                spawnerType.GetField("m_ufoPrefab", bindingFlags)?.SetValue(m_enemySpawner, m_ufoPrefab);
            if (m_chaosWormPrefab != null)
                spawnerType.GetField("m_chaosWormPrefab", bindingFlags)?.SetValue(m_enemySpawner, m_chaosWormPrefab);
            if (m_voidSpherePrefab != null)
                spawnerType.GetField("m_voidSpherePrefab", bindingFlags)?.SetValue(m_enemySpawner, m_voidSpherePrefab);
            if (m_crystalShardPrefab != null)
                spawnerType.GetField("m_crystalShardPrefab", bindingFlags)?.SetValue(m_enemySpawner, m_crystalShardPrefab);
            if (m_bossPrefab != null)
                spawnerType.GetField("m_bossPrefab", bindingFlags)?.SetValue(m_enemySpawner, m_bossPrefab);

            Debug.Log("[SceneReferenceWiring] EnemySpawner configured");
        }

        private void SetupWeaponSystem()
        {
            var weapon = m_player.GetComponent<WeaponSystem>();
            if (weapon == null)
            {
                Debug.LogWarning("[SceneReferenceWiring] WeaponSystem not found on Player!");
                return;
            }

            var weaponType = typeof(WeaponSystem);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            if (m_projectilePrefab != null)
            {
                weaponType.GetField("m_projectilePrefab", bindingFlags)?.SetValue(weapon, m_projectilePrefab);
            }

            // Create projectile container if needed
            var containerField = weaponType.GetField("m_projectileContainer", bindingFlags);
            if (containerField != null)
            {
                var existingContainer = containerField.GetValue(weapon) as Transform;
                if (existingContainer == null)
                {
                    var container = new GameObject("PlayerProjectiles").transform;
                    container.SetParent(weapon.transform);
                    containerField.SetValue(weapon, container);
                }
            }

            Debug.Log("[SceneReferenceWiring] WeaponSystem configured");
        }

        private void SetupEnemyProjectilePool()
        {
            if (m_enemyProjectilePool == null)
            {
                Debug.LogWarning("[SceneReferenceWiring] EnemyProjectilePool reference is missing!");
                return;
            }

            var poolType = typeof(EnemyProjectilePool);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            if (m_enemyProjectilePrefab != null)
            {
                poolType.GetField("m_projectilePrefab", bindingFlags)?.SetValue(m_enemyProjectilePool, m_enemyProjectilePrefab);
            }

            Debug.Log("[SceneReferenceWiring] EnemyProjectilePool configured");
        }

        private void SetupGameManager()
        {
            if (m_gameManager == null)
            {
                Debug.LogWarning("[SceneReferenceWiring] GameManager reference is missing!");
                return;
            }

            var gmType = typeof(GameManager);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            gmType.GetField("m_player", bindingFlags)?.SetValue(m_gameManager, m_player);
            gmType.GetField("m_enemySpawner", bindingFlags)?.SetValue(m_gameManager, m_enemySpawner);

            if (m_levelManager != null)
            {
                gmType.GetField("m_levelManager", bindingFlags)?.SetValue(m_gameManager, m_levelManager);
            }

            Debug.Log("[SceneReferenceWiring] GameManager configured");
        }

        private void SetupOptionalSystems()
        {
            // Create optional systems only if they don't exist in the scene
            // and no SerializeField reference was provided
            EnsureSystemExists(ref m_spawnWarningIndicator, "SpawnWarningIndicator");
            EnsureSystemExists(ref m_lowHealthVignette, "LowHealthVignette");
            EnsureSystemExists(ref m_highScoreManager, "HighScoreManager");
            EnsureSystemExists(ref m_bossHealthBar, "BossHealthBar");
            EnsureSystemExists(ref m_controlsOverlay, "ControlsOverlay");
            EnsureSystemExists(ref m_weaponUpgradeManager, "WeaponUpgradeManager");
            EnsureSystemExists(ref m_activeUpgradesDisplay, "ActiveUpgradesDisplay");
            EnsureSystemExists(ref m_playerLevelSystem, "PlayerLevelSystem");
            EnsureSystemExists(ref m_xpBarDisplay, "XPBarDisplay");
            EnsureSystemExists(ref m_levelUpAnnouncement, "LevelUpAnnouncement");
            EnsureSystemExists(ref m_damageNumberPopup, "DamageNumberPopup");
            EnsureSystemExists(ref m_waveAnnouncement, "WaveAnnouncement");
            EnsureSystemExists(ref m_statisticsScreen, "StatisticsScreen");
            EnsureSystemExists(ref m_arenaManager, "ArenaManager");
            EnsureSystemExists(ref m_gamepadRumble, "GamepadRumble");
            EnsureSystemExists(ref m_minimap, "Minimap");
            EnsureSystemExists(ref m_accessibilityManager, "AccessibilityManager");
            EnsureSystemExists(ref m_saveSystem, "SaveSystem");
            EnsureSystemExists(ref m_musicManager, "MusicManager");
            EnsureSystemExists(ref m_environmentParticles, "EnvironmentParticles");
            EnsureSystemExists(ref m_shipCustomization, "ShipCustomization");
            EnsureSystemExists(ref m_enemyDeathVFX, "EnemyDeathVFX");
            EnsureSystemExists(ref m_uiFeedbacks, "UIFeedbacks");

            // Special case for AchievementSystem - needs AchievementPopup too
            EnsureAchievementSystemExists();
        }

        /// <summary>
        /// Ensures a system component exists. If reference is null, creates a new GameObject with the component.
        /// </summary>
        private void EnsureSystemExists<T>(ref T systemReference, string gameObjectName) where T : Component
        {
            if (systemReference == null)
            {
                var go = new GameObject(gameObjectName);
                systemReference = go.AddComponent<T>();
                Debug.Log($"[SceneReferenceWiring] {gameObjectName} created");
            }
        }

        private void EnsureAchievementSystemExists()
        {
            if (m_achievementSystem == null)
            {
                var achievementGO = new GameObject("AchievementSystem");
                m_achievementSystem = achievementGO.AddComponent<AchievementSystem>();
                achievementGO.AddComponent<UI.AchievementPopup>();
                Debug.Log("[SceneReferenceWiring] AchievementSystem created");
            }
        }

        /// <summary>
        /// Auto-finds required references at runtime if not assigned in Inspector.
        /// </summary>
        private void AutoFindRequiredReferences()
        {
            // Find required scene objects
            if (m_player == null) m_player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (m_cameraController == null) m_cameraController = UnityEngine.Object.FindFirstObjectByType<CameraController>();
            if (m_enemySpawner == null) m_enemySpawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
            if (m_enemyProjectilePool == null) m_enemyProjectilePool = UnityEngine.Object.FindFirstObjectByType<EnemyProjectilePool>();
            if (m_gameManager == null) m_gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (m_levelManager == null) m_levelManager = UnityEngine.Object.FindFirstObjectByType<LevelManager>();

            // Log what was found
            if (m_player != null) Debug.Log("[SceneReferenceWiring] Auto-found Player");
            if (m_cameraController != null) Debug.Log("[SceneReferenceWiring] Auto-found CameraController");
            if (m_enemySpawner != null) Debug.Log("[SceneReferenceWiring] Auto-found EnemySpawner");
            if (m_enemyProjectilePool != null) Debug.Log("[SceneReferenceWiring] Auto-found EnemyProjectilePool");
            if (m_gameManager != null) Debug.Log("[SceneReferenceWiring] Auto-found GameManager");
            if (m_levelManager != null) Debug.Log("[SceneReferenceWiring] Auto-found LevelManager");
        }

        #region Editor Helper

        [ContextMenu("Auto-Find All Scene References")]
        private void AutoFindAllReferences()
        {
            // Find required scene objects
            if (m_player == null) m_player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (m_cameraController == null) m_cameraController = UnityEngine.Object.FindFirstObjectByType<CameraController>();
            if (m_enemySpawner == null) m_enemySpawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
            if (m_enemyProjectilePool == null) m_enemyProjectilePool = UnityEngine.Object.FindFirstObjectByType<EnemyProjectilePool>();
            if (m_gameManager == null) m_gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (m_levelManager == null) m_levelManager = UnityEngine.Object.FindFirstObjectByType<LevelManager>();

            // Find optional scene objects
            if (m_spawnWarningIndicator == null) m_spawnWarningIndicator = UnityEngine.Object.FindFirstObjectByType<SpawnWarningIndicator>();
            if (m_lowHealthVignette == null) m_lowHealthVignette = UnityEngine.Object.FindFirstObjectByType<LowHealthVignette>();
            if (m_highScoreManager == null) m_highScoreManager = UnityEngine.Object.FindFirstObjectByType<HighScoreManager>();
            if (m_bossHealthBar == null) m_bossHealthBar = UnityEngine.Object.FindFirstObjectByType<BossHealthBar>();
            if (m_controlsOverlay == null) m_controlsOverlay = UnityEngine.Object.FindFirstObjectByType<ControlsOverlay>();
            if (m_weaponUpgradeManager == null) m_weaponUpgradeManager = UnityEngine.Object.FindFirstObjectByType<Combat.WeaponUpgradeManager>();
            if (m_activeUpgradesDisplay == null) m_activeUpgradesDisplay = UnityEngine.Object.FindFirstObjectByType<ActiveUpgradesDisplay>();
            if (m_playerLevelSystem == null) m_playerLevelSystem = UnityEngine.Object.FindFirstObjectByType<PlayerLevelSystem>();
            if (m_xpBarDisplay == null) m_xpBarDisplay = UnityEngine.Object.FindFirstObjectByType<UI.XPBarDisplay>();
            if (m_levelUpAnnouncement == null) m_levelUpAnnouncement = UnityEngine.Object.FindFirstObjectByType<UI.LevelUpAnnouncement>();
            if (m_damageNumberPopup == null) m_damageNumberPopup = UnityEngine.Object.FindFirstObjectByType<UI.DamageNumberPopup>();
            if (m_waveAnnouncement == null) m_waveAnnouncement = UnityEngine.Object.FindFirstObjectByType<UI.WaveAnnouncement>();
            if (m_statisticsScreen == null) m_statisticsScreen = UnityEngine.Object.FindFirstObjectByType<UI.StatisticsScreen>();
            if (m_arenaManager == null) m_arenaManager = UnityEngine.Object.FindFirstObjectByType<Graphics.ArenaManager>();
            if (m_gamepadRumble == null) m_gamepadRumble = UnityEngine.Object.FindFirstObjectByType<Input.GamepadRumble>();
            if (m_minimap == null) m_minimap = UnityEngine.Object.FindFirstObjectByType<UI.Minimap>();
            if (m_accessibilityManager == null) m_accessibilityManager = UnityEngine.Object.FindFirstObjectByType<AccessibilityManager>();
            if (m_saveSystem == null) m_saveSystem = UnityEngine.Object.FindFirstObjectByType<SaveSystem>();
            if (m_musicManager == null) m_musicManager = UnityEngine.Object.FindFirstObjectByType<Audio.MusicManager>();
            if (m_environmentParticles == null) m_environmentParticles = UnityEngine.Object.FindFirstObjectByType<Graphics.EnvironmentParticles>();
            if (m_shipCustomization == null) m_shipCustomization = UnityEngine.Object.FindFirstObjectByType<Entities.ShipCustomization>();
            if (m_enemyDeathVFX == null) m_enemyDeathVFX = UnityEngine.Object.FindFirstObjectByType<Graphics.EnemyDeathVFX>();
            if (m_uiFeedbacks == null) m_uiFeedbacks = UnityEngine.Object.FindFirstObjectByType<UI.UIFeedbacks>();
            if (m_achievementSystem == null) m_achievementSystem = UnityEngine.Object.FindFirstObjectByType<AchievementSystem>();

            Debug.Log("[SceneReferenceWiring] Auto-find complete! Check Inspector for results.");
        }

        #endregion
    }
}
