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
        [SerializeField] private PlayerController _player;
        [SerializeField] private CameraController _cameraController;
        [SerializeField] private EnemySpawner _enemySpawner;
        [SerializeField] private EnemyProjectilePool _enemyProjectilePool;
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private LevelManager _levelManager;

        [Header("Optional System References (Leave empty to auto-create)")]
        [SerializeField] private SpawnWarningIndicator _spawnWarningIndicator;
        [SerializeField] private LowHealthVignette _lowHealthVignette;
        [SerializeField] private HighScoreManager _highScoreManager;
        [SerializeField] private BossHealthBar _bossHealthBar;
        [SerializeField] private ControlsOverlay _controlsOverlay;
        [SerializeField] private Combat.WeaponUpgradeManager _weaponUpgradeManager;
        [SerializeField] private ActiveUpgradesDisplay _activeUpgradesDisplay;
        [SerializeField] private PlayerLevelSystem _playerLevelSystem;
        [SerializeField] private UI.XPBarDisplay _xpBarDisplay;
        [SerializeField] private UI.LevelUpAnnouncement _levelUpAnnouncement;
        [SerializeField] private UI.DamageNumberPopup _damageNumberPopup;
        [SerializeField] private UI.WaveAnnouncement _waveAnnouncement;
        [SerializeField] private UI.StatisticsScreen _statisticsScreen;
        [SerializeField] private Graphics.ArenaManager _arenaManager;
        [SerializeField] private Input.GamepadRumble _gamepadRumble;
        [SerializeField] private UI.Minimap _minimap;
        [SerializeField] private AccessibilityManager _accessibilityManager;
        [SerializeField] private SaveSystem _saveSystem;
        [SerializeField] private Audio.MusicManager _musicManager;
        [SerializeField] private Graphics.EnvironmentParticles _environmentParticles;
        [SerializeField] private Entities.ShipCustomization _shipCustomization;
        [SerializeField] private Graphics.EnemyDeathVFX _enemyDeathVFX;
        [SerializeField] private UI.UIFeedbacks _uiFeedbacks;
        [SerializeField] private AchievementSystem _achievementSystem;

        [Header("Prefab References - Drag from Project")]
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private EnemyProjectile _enemyProjectilePrefab;
        [SerializeField] private DataMite _dataMitePrefab;
        [SerializeField] private ScanDrone _scanDronePrefab;
        [SerializeField] private Fizzer _fizzerPrefab;
        [SerializeField] private UFO _ufoPrefab;
        [SerializeField] private ChaosWorm _chaosWormPrefab;
        [SerializeField] private VoidSphere _voidSpherePrefab;
        [SerializeField] private CrystalShard _crystalShardPrefab;
        [SerializeField] private Boss _bossPrefab;

        /// <summary>
        /// Wires up all scene references using reflection where necessary.
        /// This method should be called during initialization.
        /// </summary>
        public void WireSceneReferences()
        {
            Debug.Log($"[SceneReferenceWiring] WireSceneReferences START at {Time.realtimeSinceStartup:F3}s");

            // Auto-find required references if not assigned in Inspector
            AutoFindRequiredReferences();

            if (_player == null)
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
            if (_cameraController == null)
            {
                Debug.LogWarning("[SceneReferenceWiring] CameraController reference is missing!");
                return;
            }

            _cameraController.SetTarget(_player.transform);
            _cameraController.SnapToTarget();
            Debug.Log("[SceneReferenceWiring] Camera target set to Player");
        }

        private void SetupEnemySpawner()
        {
            if (_enemySpawner == null)
            {
                Debug.LogWarning("[SceneReferenceWiring] EnemySpawner reference is missing!");
                return;
            }

            // Use reflection to set serialized fields
            var spawnerType = typeof(EnemySpawner);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // Set player target
            spawnerType.GetField("_playerTarget", bindingFlags)?.SetValue(_enemySpawner, _player.transform);

            // Set enemy prefabs
            if (_dataMitePrefab != null)
                spawnerType.GetField("_dataMitePrefab", bindingFlags)?.SetValue(_enemySpawner, _dataMitePrefab);
            if (_scanDronePrefab != null)
                spawnerType.GetField("_scanDronePrefab", bindingFlags)?.SetValue(_enemySpawner, _scanDronePrefab);
            if (_fizzerPrefab != null)
                spawnerType.GetField("_fizzerPrefab", bindingFlags)?.SetValue(_enemySpawner, _fizzerPrefab);
            if (_ufoPrefab != null)
                spawnerType.GetField("_ufoPrefab", bindingFlags)?.SetValue(_enemySpawner, _ufoPrefab);
            if (_chaosWormPrefab != null)
                spawnerType.GetField("_chaosWormPrefab", bindingFlags)?.SetValue(_enemySpawner, _chaosWormPrefab);
            if (_voidSpherePrefab != null)
                spawnerType.GetField("_voidSpherePrefab", bindingFlags)?.SetValue(_enemySpawner, _voidSpherePrefab);
            if (_crystalShardPrefab != null)
                spawnerType.GetField("_crystalShardPrefab", bindingFlags)?.SetValue(_enemySpawner, _crystalShardPrefab);
            if (_bossPrefab != null)
                spawnerType.GetField("_bossPrefab", bindingFlags)?.SetValue(_enemySpawner, _bossPrefab);

            Debug.Log("[SceneReferenceWiring] EnemySpawner configured");
        }

        private void SetupWeaponSystem()
        {
            var weapon = _player.GetComponent<WeaponSystem>();
            if (weapon == null)
            {
                Debug.LogWarning("[SceneReferenceWiring] WeaponSystem not found on Player!");
                return;
            }

            var weaponType = typeof(WeaponSystem);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            if (_projectilePrefab != null)
            {
                weaponType.GetField("_projectilePrefab", bindingFlags)?.SetValue(weapon, _projectilePrefab);
            }

            // Create projectile container if needed
            var containerField = weaponType.GetField("_projectileContainer", bindingFlags);
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
            if (_enemyProjectilePool == null)
            {
                Debug.LogWarning("[SceneReferenceWiring] EnemyProjectilePool reference is missing!");
                return;
            }

            var poolType = typeof(EnemyProjectilePool);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            if (_enemyProjectilePrefab != null)
            {
                poolType.GetField("_projectilePrefab", bindingFlags)?.SetValue(_enemyProjectilePool, _enemyProjectilePrefab);
            }

            Debug.Log("[SceneReferenceWiring] EnemyProjectilePool configured");
        }

        private void SetupGameManager()
        {
            if (_gameManager == null)
            {
                Debug.LogWarning("[SceneReferenceWiring] GameManager reference is missing!");
                return;
            }

            var gmType = typeof(GameManager);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            gmType.GetField("_player", bindingFlags)?.SetValue(_gameManager, _player);
            gmType.GetField("_enemySpawner", bindingFlags)?.SetValue(_gameManager, _enemySpawner);

            if (_levelManager != null)
            {
                gmType.GetField("_levelManager", bindingFlags)?.SetValue(_gameManager, _levelManager);
            }

            Debug.Log("[SceneReferenceWiring] GameManager configured");
        }

        private void SetupOptionalSystems()
        {
            // Create optional systems only if they don't exist in the scene
            // and no SerializeField reference was provided
            EnsureSystemExists(ref _spawnWarningIndicator, "SpawnWarningIndicator");
            EnsureSystemExists(ref _lowHealthVignette, "LowHealthVignette");
            EnsureSystemExists(ref _highScoreManager, "HighScoreManager");
            EnsureSystemExists(ref _bossHealthBar, "BossHealthBar");
            EnsureSystemExists(ref _controlsOverlay, "ControlsOverlay");
            EnsureSystemExists(ref _weaponUpgradeManager, "WeaponUpgradeManager");
            EnsureSystemExists(ref _activeUpgradesDisplay, "ActiveUpgradesDisplay");
            EnsureSystemExists(ref _playerLevelSystem, "PlayerLevelSystem");
            EnsureSystemExists(ref _xpBarDisplay, "XPBarDisplay");
            EnsureSystemExists(ref _levelUpAnnouncement, "LevelUpAnnouncement");
            EnsureSystemExists(ref _damageNumberPopup, "DamageNumberPopup");
            EnsureSystemExists(ref _waveAnnouncement, "WaveAnnouncement");
            EnsureSystemExists(ref _statisticsScreen, "StatisticsScreen");
            EnsureSystemExists(ref _arenaManager, "ArenaManager");
            EnsureSystemExists(ref _gamepadRumble, "GamepadRumble");
            EnsureSystemExists(ref _minimap, "Minimap");
            EnsureSystemExists(ref _accessibilityManager, "AccessibilityManager");
            EnsureSystemExists(ref _saveSystem, "SaveSystem");
            EnsureSystemExists(ref _musicManager, "MusicManager");
            EnsureSystemExists(ref _environmentParticles, "EnvironmentParticles");
            EnsureSystemExists(ref _shipCustomization, "ShipCustomization");
            EnsureSystemExists(ref _enemyDeathVFX, "EnemyDeathVFX");
            EnsureSystemExists(ref _uiFeedbacks, "UIFeedbacks");

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
            if (_achievementSystem == null)
            {
                var achievementGO = new GameObject("AchievementSystem");
                _achievementSystem = achievementGO.AddComponent<AchievementSystem>();
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
            if (_player == null) _player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (_cameraController == null) _cameraController = UnityEngine.Object.FindFirstObjectByType<CameraController>();
            if (_enemySpawner == null) _enemySpawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
            if (_enemyProjectilePool == null) _enemyProjectilePool = UnityEngine.Object.FindFirstObjectByType<EnemyProjectilePool>();
            if (_gameManager == null) _gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (_levelManager == null) _levelManager = UnityEngine.Object.FindFirstObjectByType<LevelManager>();

            // Log what was found
            if (_player != null) Debug.Log("[SceneReferenceWiring] Auto-found Player");
            if (_cameraController != null) Debug.Log("[SceneReferenceWiring] Auto-found CameraController");
            if (_enemySpawner != null) Debug.Log("[SceneReferenceWiring] Auto-found EnemySpawner");
            if (_enemyProjectilePool != null) Debug.Log("[SceneReferenceWiring] Auto-found EnemyProjectilePool");
            if (_gameManager != null) Debug.Log("[SceneReferenceWiring] Auto-found GameManager");
            if (_levelManager != null) Debug.Log("[SceneReferenceWiring] Auto-found LevelManager");
        }

        #region Editor Helper

        [ContextMenu("Auto-Find All Scene References")]
        private void AutoFindAllReferences()
        {
            // Find required scene objects
            if (_player == null) _player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (_cameraController == null) _cameraController = UnityEngine.Object.FindFirstObjectByType<CameraController>();
            if (_enemySpawner == null) _enemySpawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
            if (_enemyProjectilePool == null) _enemyProjectilePool = UnityEngine.Object.FindFirstObjectByType<EnemyProjectilePool>();
            if (_gameManager == null) _gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (_levelManager == null) _levelManager = UnityEngine.Object.FindFirstObjectByType<LevelManager>();

            // Find optional scene objects
            if (_spawnWarningIndicator == null) _spawnWarningIndicator = UnityEngine.Object.FindFirstObjectByType<SpawnWarningIndicator>();
            if (_lowHealthVignette == null) _lowHealthVignette = UnityEngine.Object.FindFirstObjectByType<LowHealthVignette>();
            if (_highScoreManager == null) _highScoreManager = UnityEngine.Object.FindFirstObjectByType<HighScoreManager>();
            if (_bossHealthBar == null) _bossHealthBar = UnityEngine.Object.FindFirstObjectByType<BossHealthBar>();
            if (_controlsOverlay == null) _controlsOverlay = UnityEngine.Object.FindFirstObjectByType<ControlsOverlay>();
            if (_weaponUpgradeManager == null) _weaponUpgradeManager = UnityEngine.Object.FindFirstObjectByType<Combat.WeaponUpgradeManager>();
            if (_activeUpgradesDisplay == null) _activeUpgradesDisplay = UnityEngine.Object.FindFirstObjectByType<ActiveUpgradesDisplay>();
            if (_playerLevelSystem == null) _playerLevelSystem = UnityEngine.Object.FindFirstObjectByType<PlayerLevelSystem>();
            if (_xpBarDisplay == null) _xpBarDisplay = UnityEngine.Object.FindFirstObjectByType<UI.XPBarDisplay>();
            if (_levelUpAnnouncement == null) _levelUpAnnouncement = UnityEngine.Object.FindFirstObjectByType<UI.LevelUpAnnouncement>();
            if (_damageNumberPopup == null) _damageNumberPopup = UnityEngine.Object.FindFirstObjectByType<UI.DamageNumberPopup>();
            if (_waveAnnouncement == null) _waveAnnouncement = UnityEngine.Object.FindFirstObjectByType<UI.WaveAnnouncement>();
            if (_statisticsScreen == null) _statisticsScreen = UnityEngine.Object.FindFirstObjectByType<UI.StatisticsScreen>();
            if (_arenaManager == null) _arenaManager = UnityEngine.Object.FindFirstObjectByType<Graphics.ArenaManager>();
            if (_gamepadRumble == null) _gamepadRumble = UnityEngine.Object.FindFirstObjectByType<Input.GamepadRumble>();
            if (_minimap == null) _minimap = UnityEngine.Object.FindFirstObjectByType<UI.Minimap>();
            if (_accessibilityManager == null) _accessibilityManager = UnityEngine.Object.FindFirstObjectByType<AccessibilityManager>();
            if (_saveSystem == null) _saveSystem = UnityEngine.Object.FindFirstObjectByType<SaveSystem>();
            if (_musicManager == null) _musicManager = UnityEngine.Object.FindFirstObjectByType<Audio.MusicManager>();
            if (_environmentParticles == null) _environmentParticles = UnityEngine.Object.FindFirstObjectByType<Graphics.EnvironmentParticles>();
            if (_shipCustomization == null) _shipCustomization = UnityEngine.Object.FindFirstObjectByType<Entities.ShipCustomization>();
            if (_enemyDeathVFX == null) _enemyDeathVFX = UnityEngine.Object.FindFirstObjectByType<Graphics.EnemyDeathVFX>();
            if (_uiFeedbacks == null) _uiFeedbacks = UnityEngine.Object.FindFirstObjectByType<UI.UIFeedbacks>();
            if (_achievementSystem == null) _achievementSystem = UnityEngine.Object.FindFirstObjectByType<AchievementSystem>();

            Debug.Log("[SceneReferenceWiring] Auto-find complete! Check Inspector for results.");
        }

        #endregion
    }
}
