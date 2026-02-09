using UnityEngine;
using System.Collections;
using Z13.Core;
using NeuralBreak.Entities;
using NeuralBreak.Config;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Scene-specific gameplay manager - handles scoring, combo, enemies, and level transitions.
    /// Global state (GameStateType, GameMode, pause/resume) is handled by GameStateManager.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private PlayerController m_player;
        [SerializeField] private EnemySpawner m_enemySpawner;
        [SerializeField] private LevelManager m_levelManager;

        [Header("Settings")]
        [SerializeField] private bool m_autoStartOnPlay = false;

        public GameStats Stats { get; private set; } = new GameStats();

        public GameStateType CurrentState => GameStateManager.Instance?.CurrentState ?? GameStateType.StartScreen;
        public GameMode CurrentMode => GameStateManager.Instance?.CurrentMode ?? GameMode.Arcade;
        public bool IsPaused => GameStateManager.Instance?.IsPaused ?? false;
        public bool IsPlaying => GameStateManager.Instance?.IsPlaying ?? false;

        private int m_currentCombo;
        private float m_currentMultiplier = 1f;
        private float m_comboTimer;

        private float ComboDecayTime => ConfigProvider.Combo?.comboDecayTime ?? 3f;
        private float MultiplierPerKill => ConfigProvider.Combo?.multiplierPerKill ?? 0.1f;
        private float MaxMultiplier => ConfigProvider.Combo?.maxMultiplier ?? 10f;
        private float MultiplierDecayRate => ConfigProvider.Combo?.multiplierDecayRate ?? 2f;
        private float BossKillMultiplier => ConfigProvider.Combo?.bossKillMultiplier ?? 2f;

        private bool m_upgradeSelected;
        private Coroutine m_levelTransitionCoroutine;
        private bool m_isPlayerDead;

        private void Awake()
        {
            Instance = this;

            // Ensure GameStateManager exists (creates one if not present)
            EnsureGameStateManager();

            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Subscribe<UpgradeSelectedEvent>(OnUpgradeSelected);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            EventBus.Unsubscribe<UpgradeSelectedEvent>(OnUpgradeSelected);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (m_autoStartOnPlay && CurrentState == GameStateType.StartScreen)
            {
                StartGame(CurrentMode);
            }
        }

        private void Update()
        {
            if (!IsPlaying) return;

            Stats.survivedTime += Time.deltaTime;
            UpdateComboDecay();
        }

        #region Game Control

        public void StartGame(GameMode mode)
        {
            LogHelper.Log($"[GameManager] STARTING GAME IN {mode} MODE");

            Stats.Reset();
            m_currentCombo = 0;
            m_currentMultiplier = 1f;
            m_isPlayerDead = false;
            m_levelTransitionCoroutine = null;

            GameStateManager.Instance.StartGame(mode);
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            Stats.Reset();
            m_currentCombo = 0;
            m_currentMultiplier = 1f;
            m_isPlayerDead = false;
            m_levelTransitionCoroutine = null;
        }

        public void SetState(GameStateType newState) => GameStateManager.Instance.SetState(newState);
        public void PauseGame() => GameStateManager.Instance.PauseGame();
        public void ResumeGame() => GameStateManager.Instance.ResumeGame();
        public void ReturnToMenu() => GameStateManager.Instance.ReturnToMenu();

        public void GameOver()
        {
            GameStateManager.Instance.GameOver(Stats);
        }

        public void Victory()
        {
            GameStateManager.Instance.Victory(Stats);
        }

        #endregion

        #region Scoring & Combo

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (evt.scoreValue < 0 || evt.xpValue < 0) return;

            Stats.enemiesKilled++;
            UpdateKillCount(evt.enemyType);

            m_currentCombo++;
            m_comboTimer = ComboDecayTime;

            if (m_currentCombo > Stats.highestCombo)
            {
                Stats.highestCombo = m_currentCombo;
            }

            if (m_comboTimer > 0)
            {
                m_currentMultiplier = Mathf.Min(m_currentMultiplier + MultiplierPerKill, MaxMultiplier);
                if (m_currentMultiplier > Stats.highestMultiplier)
                {
                    Stats.highestMultiplier = m_currentMultiplier;
                }
            }

            int baseScore = evt.scoreValue;
            float scoreMultiplier = m_currentMultiplier;
            if (evt.enemyType == EnemyType.Boss)
            {
                scoreMultiplier *= BossKillMultiplier;
            }
            int finalScore = Mathf.RoundToInt(baseScore * scoreMultiplier);
            Stats.score += finalScore;
            Stats.totalXP += evt.xpValue;

            EventBus.Publish(new ComboChangedEvent
            {
                comboCount = m_currentCombo,
                multiplier = m_currentMultiplier
            });

            EventBus.Publish(new ScoreChangedEvent
            {
                newScore = Stats.score,
                delta = finalScore,
                worldPosition = evt.position
            });
        }

        private void UpdateKillCount(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.DataMite: Stats.dataMinersKilled++; break;
                case EnemyType.ScanDrone: Stats.scanDronesKilled++; break;
                case EnemyType.ChaosWorm: Stats.chaosWormsKilled++; break;
                case EnemyType.VoidSphere: Stats.voidSpheresKilled++; break;
                case EnemyType.CrystalShard: Stats.crystalSwarmsKilled++; break;
                case EnemyType.Fizzer: Stats.fizzersKilled++; break;
                case EnemyType.UFO: Stats.ufosKilled++; break;
                case EnemyType.Boss: Stats.bossesKilled++; break;
            }
        }

        private void UpdateComboDecay()
        {
            if (m_currentCombo > 0)
            {
                m_comboTimer -= Time.deltaTime;
                if (m_comboTimer <= 0)
                {
                    m_currentCombo = 0;
                    EventBus.Publish(new ComboChangedEvent
                    {
                        comboCount = 0,
                        multiplier = m_currentMultiplier
                    });
                }
            }

            if (m_currentMultiplier > 1f && m_comboTimer <= 0)
            {
                float decayRate = MultiplierDecayRate > 0 ? 1f / MultiplierDecayRate : 0.5f;
                m_currentMultiplier = Mathf.Max(1f, m_currentMultiplier - Time.deltaTime * decayRate);
            }
        }

        public void ResetCombo()
        {
            m_currentCombo = 0;
            EventBus.Publish(new ComboChangedEvent
            {
                comboCount = 0,
                multiplier = m_currentMultiplier
            });
        }

        #endregion

        #region Event Handlers

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            m_isPlayerDead = true;

            if (m_levelTransitionCoroutine != null)
            {
                StopCoroutine(m_levelTransitionCoroutine);
                m_levelTransitionCoroutine = null;
            }

            StartCoroutine(DelayedGameOver(1.5f));
        }

        private IEnumerator DelayedGameOver(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameOver();
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            if (m_isPlayerDead) return;

            Stats.level = evt.levelNumber + 1;

            if (evt.levelNumber >= 99)
            {
                Victory();
            }
            else
            {
                m_levelTransitionCoroutine = StartCoroutine(LevelTransition());
            }
        }

        private IEnumerator LevelTransition()
        {
            LogHelper.Log("[GameManager] Level completed - transitioning...");

            if (m_isPlayerDead)
            {
                m_levelTransitionCoroutine = null;
                yield break;
            }

            // Randomized firework death sequence over 1.5 seconds!
            if (m_enemySpawner != null)
            {
                // Each enemy gets a random death time between 0 and 1.5 seconds
                yield return StartCoroutine(m_enemySpawner.KillAllEnemiesFireworks(1.5f));
            }

            // Extra second delay before showing upgrade menu (let fireworks finish)
            yield return new WaitForSeconds(1.0f);

            bool showUpgradeSelection = ShouldShowUpgradeSelection();

            if (showUpgradeSelection && !m_isPlayerDead)
            {
                GameStateManager.Instance.EnterRogueChoice();

                EventBus.Publish(new UpgradeSelectionStartedEvent
                {
                    options = new System.Collections.Generic.List<Combat.UpgradeDefinition>()
                });

                m_upgradeSelected = false;
                float upgradeTimeout = 60f;
                float upgradeWaitTime = 0f;

                while (!m_upgradeSelected && upgradeWaitTime < upgradeTimeout)
                {
                    yield return null;
                    upgradeWaitTime += Time.unscaledDeltaTime;
                }
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }

            if (m_levelManager != null)
            {
                m_levelManager.AdvanceLevel();
            }

            GameStateManager.Instance.ExitRogueChoice();

            if (m_enemySpawner != null)
            {
                m_enemySpawner.StartSpawning();
            }

            m_levelTransitionCoroutine = null;
        }

        private bool ShouldShowUpgradeSelection()
        {
            if (CurrentMode == GameMode.Rogue)
            {
                return true;
            }

            if (CurrentMode == GameMode.Arcade)
            {
                int interval = ConfigProvider.Balance?.upgradeSystem?.showUpgradeEveryNLevels ?? 5;
                return Stats.level % interval == 0;
            }

            return false;
        }

        private void OnUpgradeSelected(UpgradeSelectedEvent evt)
        {
            m_upgradeSelected = true;
        }

        #endregion

        #region Initialization Helpers

        /// <summary>
        /// Ensures GameStateManager exists. Creates one if not present in scene.
        /// This allows the main scene to run directly without the Boot scene.
        /// </summary>
        private void EnsureGameStateManager()
        {
            if (GameStateManager.Instance != null) return;

            // Try to find existing in scene
            var existing = FindFirstObjectByType<GameStateManager>();
            if (existing != null)
            {
                existing.Initialize();
                return;
            }

            // Create one if none exists
            var go = new GameObject("GameStateManager (Auto-Created)");
            var gsm = go.AddComponent<GameStateManager>();
            gsm.Initialize();
            Debug.Log("[GameManager] Created GameStateManager automatically - add it to scene for production");
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Start Arcade")]
        private void DebugStartArcade() => StartGame(GameMode.Arcade);

        [ContextMenu("Debug: Game Over")]
        private void DebugGameOver() => GameOver();

        [ContextMenu("Debug: Victory")]
        private void DebugVictory() => Victory();

        #endregion
    }
}
