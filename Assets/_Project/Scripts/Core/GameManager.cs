using UnityEngine;
using MoreMountains.Feedbacks;
using NeuralBreak.Entities;
using System.Collections;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Central game coordinator - manages game state, systems, and flow.
    /// Singleton pattern for easy access from other systems.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameStateType _currentState = GameStateType.StartScreen;
        [SerializeField] private GameMode _currentMode = GameMode.Arcade;
        [SerializeField] private bool _isPaused;
        [SerializeField] private bool _autoStartOnPlay = true;

        [Header("References")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private EnemySpawner _enemySpawner;
        [SerializeField] private LevelManager _levelManager;

        [Header("Feel Feedbacks")]
        [SerializeField] private MMF_Player _gameStartFeedback;
        [SerializeField] private MMF_Player _gameOverFeedback;
        [SerializeField] private MMF_Player _levelCompleteFeedback;
        [SerializeField] private MMF_Player _victoryFeedback;

        // Game stats
        public GameStats Stats { get; private set; } = new GameStats();

        // Public accessors
        public GameStateType CurrentState => _currentState;
        public GameMode CurrentMode => _currentMode;
        public bool IsPaused => _isPaused;
        public bool IsPlaying => _currentState == GameStateType.Playing && !_isPaused;

        // Combo/Multiplier system
        private int _currentCombo;
        private float _currentMultiplier = 1f;
        private float _comboTimer;
        private const float COMBO_DECAY_TIME = 1.5f;
        private const float MULTIPLIER_DECAY_TIME = 2f;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Subscribe to events
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);

            // Ensure FeedbackSetup exists for runtime juiciness
            EnsureFeedbackSetup();
        }

        private void EnsureFeedbackSetup()
        {
            if (FeedbackSetup.Instance == null)
            {
                var feedbackGO = new GameObject("FeedbackSetup");
                feedbackGO.AddComponent<FeedbackSetup>();
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (_autoStartOnPlay && _currentState == GameStateType.StartScreen)
            {
                StartGame(_currentMode);
            }
        }

        private void Update()
        {
            if (!IsPlaying) return;

            // Update survived time
            Stats.survivedTime += Time.deltaTime;

            // Combo decay
            UpdateComboDecay();
        }

        #region Game State Management

        public void SetState(GameStateType newState)
        {
            if (_currentState == newState) return;

            GameStateType previousState = _currentState;
            _currentState = newState;

            EventBus.Publish(new GameStateChangedEvent
            {
                previousState = previousState,
                newState = newState
            });

            Debug.Log($"[GameManager] State changed: {previousState} -> {newState}");
        }

        public void StartGame(GameMode mode)
        {
            _currentMode = mode;
            Stats.Reset();
            _currentCombo = 0;
            _currentMultiplier = 1f;

            SetState(GameStateType.Playing);

            _gameStartFeedback?.PlayFeedbacks();

            EventBus.Publish(new GameStartedEvent { mode = mode });

            Debug.Log($"[GameManager] Game started in {mode} mode");
        }

        public void PauseGame()
        {
            if (_currentState != GameStateType.Playing) return;

            _isPaused = true;
            Time.timeScale = 0f;

            SetState(GameStateType.Paused);
            EventBus.Publish(new GamePausedEvent { isPaused = true });
        }

        public void ResumeGame()
        {
            if (_currentState != GameStateType.Paused) return;

            _isPaused = false;
            Time.timeScale = 1f;

            SetState(GameStateType.Playing);
            EventBus.Publish(new GamePausedEvent { isPaused = false });
        }

        public void GameOver()
        {
            SetState(GameStateType.GameOver);
            Time.timeScale = 1f;

            _gameOverFeedback?.PlayFeedbacks();

            EventBus.Publish(new GameOverEvent { finalStats = Stats });

            Debug.Log($"[GameManager] Game Over! Score: {Stats.score}, Level: {Stats.level}");
        }

        public void Victory()
        {
            Stats.gameCompleted = true;
            SetState(GameStateType.Victory);

            _victoryFeedback?.PlayFeedbacks();

            EventBus.Publish(new VictoryEvent { finalStats = Stats });

            Debug.Log("[GameManager] VICTORY! All 99 levels completed!");
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            _isPaused = false;
            SetState(GameStateType.StartScreen);
        }

        #endregion

        #region Scoring & Combo

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            // Update kill counts
            Stats.enemiesKilled++;
            UpdateKillCount(evt.enemyType);

            // Update combo
            _currentCombo++;
            _comboTimer = COMBO_DECAY_TIME;

            if (_currentCombo > Stats.highestCombo)
            {
                Stats.highestCombo = _currentCombo;
            }

            // Update multiplier (increases with quick kills)
            if (_comboTimer > 0)
            {
                _currentMultiplier = Mathf.Min(_currentMultiplier + 0.1f, 10f);
                if (_currentMultiplier > Stats.highestMultiplier)
                {
                    Stats.highestMultiplier = _currentMultiplier;
                }
            }

            // Calculate score with multiplier
            int baseScore = evt.scoreValue;
            int finalScore = Mathf.RoundToInt(baseScore * _currentMultiplier);
            Stats.score += finalScore;

            // Add XP
            Stats.totalXP += evt.xpValue;

            // Publish events
            EventBus.Publish(new ComboChangedEvent
            {
                comboCount = _currentCombo,
                multiplier = _currentMultiplier
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
            if (_currentCombo > 0)
            {
                _comboTimer -= Time.deltaTime;
                if (_comboTimer <= 0)
                {
                    _currentCombo = 0;
                    EventBus.Publish(new ComboChangedEvent
                    {
                        comboCount = 0,
                        multiplier = _currentMultiplier
                    });
                }
            }

            // Multiplier decays more slowly
            if (_currentMultiplier > 1f && _comboTimer <= 0)
            {
                _currentMultiplier = Mathf.Max(1f, _currentMultiplier - Time.deltaTime * 0.5f);
            }
        }

        public void ResetCombo()
        {
            _currentCombo = 0;
            EventBus.Publish(new ComboChangedEvent
            {
                comboCount = 0,
                multiplier = _currentMultiplier
            });
        }

        #endregion

        #region Event Handlers

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            GameOver();
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            Stats.level = evt.levelNumber + 1;

            _levelCompleteFeedback?.PlayFeedbacks();

            // Check for victory
            if (evt.levelNumber >= 99)
            {
                Victory();
            }
            else
            {
                // Start transition to next level
                StartCoroutine(LevelTransition());
            }
        }

        private IEnumerator LevelTransition()
        {
            // Brief pause before next level
            yield return new WaitForSeconds(2f);

            // Clear remaining enemies
            if (_enemySpawner != null)
            {
                _enemySpawner.ClearAllEnemies();
            }

            // Advance to next level
            if (_levelManager != null)
            {
                _levelManager.AdvanceLevel();
            }
            else if (LevelManager.Instance != null)
            {
                LevelManager.Instance.AdvanceLevel();
            }

            // Resume spawning
            if (_enemySpawner != null)
            {
                _enemySpawner.StartSpawning();
            }
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
