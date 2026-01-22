using UnityEngine;
using NeuralBreak.Audio;
using NeuralBreak.Entities;
using NeuralBreak.Combat;
using NeuralBreak.Graphics;
using NeuralBreak.Input;
using NeuralBreak.UI;
using NeuralBreak.Utils;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Auto-wires scene references at runtime.
    /// This helps when prefab references can't be set in editor.
    /// </summary>
    public class GameSetup : MonoBehaviour
    {
        [Header("Prefabs - Drag these in Inspector")]
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

        [Header("Auto-Find Settings")]
        [SerializeField] private bool _autoFindOnAwake = true;
        [SerializeField] private bool _autoStartGame = true;

        // Cached sprites for runtime generation
        private static Sprite _circleSprite;
        private static Sprite _squareSprite;

        public static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite == null)
                    _circleSprite = RuntimeSpriteGenerator.CreateCircleSprite(64);
                return _circleSprite;
            }
        }

        public static Sprite SquareSprite
        {
            get
            {
                if (_squareSprite == null)
                    _squareSprite = RuntimeSpriteGenerator.CreateSquareSprite(64);
                return _squareSprite;
            }
        }

        private void Awake()
        {
            if (_autoFindOnAwake)
            {
                SetupReferences();
                SetupSprites();
            }
        }

        private void Start()
        {
            Debug.Log("[GameSetup] Start called");

            if (_autoStartGame)
            {
                // Give a frame for everything to initialize
                StartCoroutine(AutoStartGame());
            }
        }

        private System.Collections.IEnumerator AutoStartGame()
        {
            Debug.Log("[GameSetup] AutoStartGame coroutine starting...");

            // Wait for GameManager to exist
            float timeout = 3f;
            while (GameManager.Instance == null && timeout > 0)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("[GameSetup] GameManager.Instance is null after waiting!");
                yield break;
            }

            // Wait one more frame for safety
            yield return null;

            if (!GameManager.Instance.IsPlaying)
            {
                Debug.Log("[GameSetup] Starting game...");
                GameManager.Instance.StartGame(GameMode.Arcade);
                Debug.Log("[GameSetup] Auto-started game in Arcade mode");
            }
        }

        private void SetupSprites()
        {
            // Setup Player sprite
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                var sr = player.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite == null)
                {
                    sr.sprite = CircleSprite;
                    Debug.Log("[GameSetup] Player sprite generated");
                }
            }

            // Setup projectile prefab sprites
            if (_projectilePrefab != null)
            {
                var sr = _projectilePrefab.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite == null)
                {
                    sr.sprite = CircleSprite;
                }
            }

            if (_enemyProjectilePrefab != null)
            {
                var sr = _enemyProjectilePrefab.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite == null)
                {
                    sr.sprite = CircleSprite;
                }
            }

            // Setup enemy prefab sprites
            SetupEnemyPrefabSprite(_dataMitePrefab);
            SetupEnemyPrefabSprite(_scanDronePrefab);
            SetupEnemyPrefabSprite(_fizzerPrefab);
            SetupEnemyPrefabSprite(_ufoPrefab);
            SetupEnemyPrefabSprite(_chaosWormPrefab);
            SetupEnemyPrefabSprite(_voidSpherePrefab);
            SetupEnemyPrefabSprite(_crystalShardPrefab);
            SetupEnemyPrefabSprite(_bossPrefab);

            Debug.Log("[GameSetup] All sprites configured");
        }

        private void SetupEnemyPrefabSprite(EnemyBase enemy)
        {
            if (enemy == null) return;

            var sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
            {
                sr.sprite = CircleSprite;
            }
        }

        [ContextMenu("Setup References")]
        public void SetupReferences()
        {
            // Find Player
            var player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[GameSetup] No PlayerController found in scene!");
                return;
            }

            // Setup Camera
            var cameraController = FindFirstObjectByType<CameraController>();
            if (cameraController != null)
            {
                cameraController.SetTarget(player.transform);
                cameraController.SnapToTarget();
                Debug.Log("[GameSetup] Camera target set to Player");
            }

            // Setup EnemySpawner
            var spawner = FindFirstObjectByType<EnemySpawner>();
            if (spawner != null)
            {
                SetupEnemySpawner(spawner, player.transform);
            }

            // Setup WeaponSystem
            var weapon = player.GetComponent<WeaponSystem>();
            if (weapon != null)
            {
                SetupWeaponSystem(weapon);
            }

            // Setup EnemyProjectilePool
            var enemyPool = FindFirstObjectByType<EnemyProjectilePool>();
            if (enemyPool != null)
            {
                SetupEnemyProjectilePool(enemyPool);
            }

            // Setup GameManager
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                SetupGameManager(gameManager, player, spawner);
            }

            // Setup SpawnWarningIndicator
            SetupSpawnWarningIndicator();

            // Setup LowHealthVignette
            SetupLowHealthVignette();

            // Setup HighScoreManager
            SetupHighScoreManager();

            // Setup BossHealthBar
            SetupBossHealthBar();

            // Setup ControlsOverlay
            SetupControlsOverlay();

            // Setup WeaponUpgradeManager
            SetupWeaponUpgradeManager();

            // Setup ActiveUpgradesDisplay
            SetupActiveUpgradesDisplay();

            // Setup PlayerLevelSystem
            SetupPlayerLevelSystem();

            // Setup XPBarDisplay
            SetupXPBarDisplay();

            // Setup LevelUpAnnouncement
            SetupLevelUpAnnouncement();

            // Setup DamageNumberPopup
            SetupDamageNumberPopup();

            // Setup WaveAnnouncement
            SetupWaveAnnouncement();

            // Setup AchievementSystem
            SetupAchievementSystem();

            // Setup StatisticsScreen
            SetupStatisticsScreen();

            // Setup ArenaManager
            SetupArenaManager();

            // Setup GamepadRumble
            SetupGamepadRumble();

            // Setup Minimap
            SetupMinimap();

            // Setup AccessibilityManager
            SetupAccessibilityManager();

            // Setup SaveSystem
            SetupSaveSystem();

            // Setup MusicManager
            SetupMusicManager();

            // Setup EnvironmentParticles
            SetupEnvironmentParticles();

            // Setup ShipCustomization
            SetupShipCustomization();

            // Setup EnemyDeathVFX
            SetupEnemyDeathVFX();

            // Setup UIFeedbacks (FEEL integration)
            SetupUIFeedbacks();

            Debug.Log("[GameSetup] Scene references configured!");
        }

        private void SetupGameManager(GameManager gm, PlayerController player, EnemySpawner spawner)
        {
            var gmType = typeof(GameManager);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            if (player != null)
            {
                gmType.GetField("_player", bindingFlags)?.SetValue(gm, player);
            }

            if (spawner != null)
            {
                gmType.GetField("_enemySpawner", bindingFlags)?.SetValue(gm, spawner);
            }

            var levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
            {
                gmType.GetField("_levelManager", bindingFlags)?.SetValue(gm, levelManager);
            }

            Debug.Log("[GameSetup] GameManager configured");
        }

        private void SetupEnemySpawner(EnemySpawner spawner, Transform player)
        {
            // Use reflection to set serialized fields
            var spawnerType = typeof(EnemySpawner);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // Set player target
            var playerField = spawnerType.GetField("_playerTarget", bindingFlags);
            playerField?.SetValue(spawner, player);

            // Set prefabs if we have them
            if (_dataMitePrefab != null)
            {
                spawnerType.GetField("_dataMitePrefab", bindingFlags)?.SetValue(spawner, _dataMitePrefab);
            }
            if (_scanDronePrefab != null)
            {
                spawnerType.GetField("_scanDronePrefab", bindingFlags)?.SetValue(spawner, _scanDronePrefab);
            }
            if (_fizzerPrefab != null)
            {
                spawnerType.GetField("_fizzerPrefab", bindingFlags)?.SetValue(spawner, _fizzerPrefab);
            }
            if (_ufoPrefab != null)
            {
                spawnerType.GetField("_ufoPrefab", bindingFlags)?.SetValue(spawner, _ufoPrefab);
            }
            if (_chaosWormPrefab != null)
            {
                spawnerType.GetField("_chaosWormPrefab", bindingFlags)?.SetValue(spawner, _chaosWormPrefab);
            }
            if (_voidSpherePrefab != null)
            {
                spawnerType.GetField("_voidSpherePrefab", bindingFlags)?.SetValue(spawner, _voidSpherePrefab);
            }
            if (_crystalShardPrefab != null)
            {
                spawnerType.GetField("_crystalShardPrefab", bindingFlags)?.SetValue(spawner, _crystalShardPrefab);
            }
            if (_bossPrefab != null)
            {
                spawnerType.GetField("_bossPrefab", bindingFlags)?.SetValue(spawner, _bossPrefab);
            }

            Debug.Log("[GameSetup] EnemySpawner configured");
        }

        private void SetupWeaponSystem(WeaponSystem weapon)
        {
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

            Debug.Log("[GameSetup] WeaponSystem configured");
        }

        private void SetupEnemyProjectilePool(EnemyProjectilePool pool)
        {
            var poolType = typeof(EnemyProjectilePool);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            if (_enemyProjectilePrefab != null)
            {
                poolType.GetField("_projectilePrefab", bindingFlags)?.SetValue(pool, _enemyProjectilePrefab);
            }

            Debug.Log("[GameSetup] EnemyProjectilePool configured");
        }

        private void SetupSpawnWarningIndicator()
        {
            // Create SpawnWarningIndicator if not exists
            var existing = FindFirstObjectByType<SpawnWarningIndicator>();
            if (existing == null)
            {
                var warningGO = new GameObject("SpawnWarningIndicator");
                warningGO.AddComponent<SpawnWarningIndicator>();
                Debug.Log("[GameSetup] SpawnWarningIndicator created");
            }
        }

        private void SetupLowHealthVignette()
        {
            // Create LowHealthVignette if not exists
            var existing = FindFirstObjectByType<LowHealthVignette>();
            if (existing == null)
            {
                var vignetteGO = new GameObject("LowHealthVignette");
                vignetteGO.AddComponent<LowHealthVignette>();
                Debug.Log("[GameSetup] LowHealthVignette created");
            }
        }

        private void SetupHighScoreManager()
        {
            // Create HighScoreManager if not exists
            var existing = FindFirstObjectByType<HighScoreManager>();
            if (existing == null)
            {
                var highScoreGO = new GameObject("HighScoreManager");
                highScoreGO.AddComponent<HighScoreManager>();
                Debug.Log("[GameSetup] HighScoreManager created");
            }
        }

        private void SetupBossHealthBar()
        {
            // Create BossHealthBar if not exists
            var existing = FindFirstObjectByType<BossHealthBar>();
            if (existing == null)
            {
                var bossBarGO = new GameObject("BossHealthBar");
                bossBarGO.AddComponent<BossHealthBar>();
                Debug.Log("[GameSetup] BossHealthBar created");
            }
        }

        private void SetupControlsOverlay()
        {
            // Create ControlsOverlay if not exists
            var existing = FindFirstObjectByType<ControlsOverlay>();
            if (existing == null)
            {
                var overlayGO = new GameObject("ControlsOverlay");
                overlayGO.AddComponent<ControlsOverlay>();
                Debug.Log("[GameSetup] ControlsOverlay created");
            }
        }

        private void SetupWeaponUpgradeManager()
        {
            // Create WeaponUpgradeManager if not exists
            var existing = FindFirstObjectByType<Combat.WeaponUpgradeManager>();
            if (existing == null)
            {
                var managerGO = new GameObject("WeaponUpgradeManager");
                managerGO.AddComponent<Combat.WeaponUpgradeManager>();
                Debug.Log("[GameSetup] WeaponUpgradeManager created");
            }
        }

        private void SetupActiveUpgradesDisplay()
        {
            // Create ActiveUpgradesDisplay if not exists
            var existing = FindFirstObjectByType<ActiveUpgradesDisplay>();
            if (existing == null)
            {
                var displayGO = new GameObject("ActiveUpgradesDisplay");
                displayGO.AddComponent<ActiveUpgradesDisplay>();
                Debug.Log("[GameSetup] ActiveUpgradesDisplay created");
            }
        }

        private void SetupPlayerLevelSystem()
        {
            // Create PlayerLevelSystem if not exists
            var existing = FindFirstObjectByType<PlayerLevelSystem>();
            if (existing == null)
            {
                var levelSystemGO = new GameObject("PlayerLevelSystem");
                levelSystemGO.AddComponent<PlayerLevelSystem>();
                Debug.Log("[GameSetup] PlayerLevelSystem created");
            }
        }

        private void SetupXPBarDisplay()
        {
            // Create XPBarDisplay if not exists
            var existing = FindFirstObjectByType<UI.XPBarDisplay>();
            if (existing == null)
            {
                var xpBarGO = new GameObject("XPBarDisplay");
                xpBarGO.AddComponent<UI.XPBarDisplay>();
                Debug.Log("[GameSetup] XPBarDisplay created");
            }
        }

        private void SetupLevelUpAnnouncement()
        {
            // Create LevelUpAnnouncement if not exists
            var existing = FindFirstObjectByType<UI.LevelUpAnnouncement>();
            if (existing == null)
            {
                var announcementGO = new GameObject("LevelUpAnnouncement");
                announcementGO.AddComponent<UI.LevelUpAnnouncement>();
                Debug.Log("[GameSetup] LevelUpAnnouncement created");
            }
        }

        private void SetupDamageNumberPopup()
        {
            // Create DamageNumberPopup if not exists
            var existing = FindFirstObjectByType<UI.DamageNumberPopup>();
            if (existing == null)
            {
                var popupGO = new GameObject("DamageNumberPopup");
                popupGO.AddComponent<UI.DamageNumberPopup>();
                Debug.Log("[GameSetup] DamageNumberPopup created");
            }
        }

        private void SetupWaveAnnouncement()
        {
            // Create WaveAnnouncement if not exists
            var existing = FindFirstObjectByType<UI.WaveAnnouncement>();
            if (existing == null)
            {
                var waveGO = new GameObject("WaveAnnouncement");
                waveGO.AddComponent<UI.WaveAnnouncement>();
                Debug.Log("[GameSetup] WaveAnnouncement created");
            }
        }

        private void SetupAchievementSystem()
        {
            // Create AchievementSystem if not exists
            var existing = FindFirstObjectByType<AchievementSystem>();
            if (existing == null)
            {
                var achievementGO = new GameObject("AchievementSystem");
                achievementGO.AddComponent<AchievementSystem>();
                achievementGO.AddComponent<UI.AchievementPopup>();
                Debug.Log("[GameSetup] AchievementSystem created");
            }
        }

        private void SetupStatisticsScreen()
        {
            // Create StatisticsScreen if not exists
            var existing = FindFirstObjectByType<UI.StatisticsScreen>();
            if (existing == null)
            {
                var statsGO = new GameObject("StatisticsScreen");
                statsGO.AddComponent<UI.StatisticsScreen>();
                Debug.Log("[GameSetup] StatisticsScreen created");
            }
        }

        private void SetupArenaManager()
        {
            // Create ArenaManager if not exists
            var existing = FindFirstObjectByType<Graphics.ArenaManager>();
            if (existing == null)
            {
                var arenaGO = new GameObject("ArenaManager");
                arenaGO.AddComponent<Graphics.ArenaManager>();
                Debug.Log("[GameSetup] ArenaManager created");
            }
        }

        private void SetupGamepadRumble()
        {
            // Create GamepadRumble if not exists
            var existing = FindFirstObjectByType<Input.GamepadRumble>();
            if (existing == null)
            {
                var rumbleGO = new GameObject("GamepadRumble");
                rumbleGO.AddComponent<Input.GamepadRumble>();
                Debug.Log("[GameSetup] GamepadRumble created");
            }
        }

        private void SetupMinimap()
        {
            // Create Minimap if not exists
            var existing = FindFirstObjectByType<UI.Minimap>();
            if (existing == null)
            {
                var minimapGO = new GameObject("Minimap");
                minimapGO.AddComponent<UI.Minimap>();
                Debug.Log("[GameSetup] Minimap created");
            }
        }

        private void SetupAccessibilityManager()
        {
            // Create AccessibilityManager if not exists
            var existing = FindFirstObjectByType<AccessibilityManager>();
            if (existing == null)
            {
                var accessGO = new GameObject("AccessibilityManager");
                accessGO.AddComponent<AccessibilityManager>();
                Debug.Log("[GameSetup] AccessibilityManager created");
            }
        }

        private void SetupSaveSystem()
        {
            // Create SaveSystem if not exists
            var existing = FindFirstObjectByType<SaveSystem>();
            if (existing == null)
            {
                var saveGO = new GameObject("SaveSystem");
                saveGO.AddComponent<SaveSystem>();
                Debug.Log("[GameSetup] SaveSystem created");
            }
        }

        private void SetupMusicManager()
        {
            // Create MusicManager if not exists
            var existing = FindFirstObjectByType<Audio.MusicManager>();
            if (existing == null)
            {
                var musicGO = new GameObject("MusicManager");
                musicGO.AddComponent<Audio.MusicManager>();
                Debug.Log("[GameSetup] MusicManager created");
            }
        }

        private void SetupEnvironmentParticles()
        {
            // Create EnvironmentParticles if not exists
            var existing = FindFirstObjectByType<Graphics.EnvironmentParticles>();
            if (existing == null)
            {
                var particlesGO = new GameObject("EnvironmentParticles");
                particlesGO.AddComponent<Graphics.EnvironmentParticles>();
                Debug.Log("[GameSetup] EnvironmentParticles created");
            }
        }

        private void SetupShipCustomization()
        {
            // Create ShipCustomization if not exists
            var existing = FindFirstObjectByType<Entities.ShipCustomization>();
            if (existing == null)
            {
                var customGO = new GameObject("ShipCustomization");
                customGO.AddComponent<Entities.ShipCustomization>();
                Debug.Log("[GameSetup] ShipCustomization created");
            }
        }

        private void SetupEnemyDeathVFX()
        {
            // Create EnemyDeathVFX if not exists
            var existing = FindFirstObjectByType<Graphics.EnemyDeathVFX>();
            if (existing == null)
            {
                var vfxGO = new GameObject("EnemyDeathVFX");
                vfxGO.AddComponent<Graphics.EnemyDeathVFX>();
                Debug.Log("[GameSetup] EnemyDeathVFX created");
            }
        }

        private void SetupUIFeedbacks()
        {
            // Create UIFeedbacks if not exists (FEEL integration for UI juice)
            var existing = FindFirstObjectByType<UI.UIFeedbacks>();
            if (existing == null)
            {
                var feedbacksGO = new GameObject("UIFeedbacks");
                feedbacksGO.AddComponent<UI.UIFeedbacks>();
                Debug.Log("[GameSetup] UIFeedbacks created (FEEL integration)");
            }
        }
    }
}
