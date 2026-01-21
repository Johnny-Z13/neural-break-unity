using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Main HUD orchestrator. Routes events to child display components.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Display Components")]
        [SerializeField] private HealthDisplay _healthDisplay;
        [SerializeField] private ScoreDisplay _scoreDisplay;
        [SerializeField] private WeaponHeatDisplay _heatDisplay;
        [SerializeField] private LevelDisplay _levelDisplay;

        private void OnEnable()
        {
            // Subscribe to events
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Subscribe<ShieldChangedEvent>(OnShieldChanged);
            EventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Subscribe<WeaponHeatChangedEvent>(OnWeaponHeatChanged);
            EventBus.Subscribe<PowerUpChangedEvent>(OnPowerUpChanged);
            EventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<PlayerHealedEvent>(OnPlayerHealed);
            EventBus.Unsubscribe<ShieldChangedEvent>(OnShieldChanged);
            EventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            EventBus.Unsubscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Unsubscribe<WeaponHeatChangedEvent>(OnWeaponHeatChanged);
            EventBus.Unsubscribe<PowerUpChangedEvent>(OnPowerUpChanged);
            EventBus.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
        }

        private void OnGameStarted(GameStartedEvent evt)
        {
            // Reset all displays
            _healthDisplay?.ResetDisplay();
            _scoreDisplay?.ResetDisplay();
            _heatDisplay?.ResetDisplay();
            _levelDisplay?.SetLevel(1);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            _healthDisplay?.UpdateHealth(evt.currentHealth, evt.maxHealth);
        }

        private void OnPlayerHealed(PlayerHealedEvent evt)
        {
            _healthDisplay?.UpdateHealth(evt.currentHealth, evt.maxHealth);
        }

        private void OnShieldChanged(ShieldChangedEvent evt)
        {
            _healthDisplay?.UpdateShields(evt.currentShields, evt.maxShields);
        }

        private void OnScoreChanged(ScoreChangedEvent evt)
        {
            _scoreDisplay?.UpdateScore(evt.newScore, evt.delta, evt.worldPosition);
        }

        private void OnComboChanged(ComboChangedEvent evt)
        {
            _scoreDisplay?.UpdateCombo(evt.comboCount, evt.multiplier);
        }

        private void OnWeaponHeatChanged(WeaponHeatChangedEvent evt)
        {
            _heatDisplay?.UpdateHeat(evt.heat, evt.maxHeat, evt.isOverheated);
        }

        private void OnPowerUpChanged(PowerUpChangedEvent evt)
        {
            _heatDisplay?.UpdatePowerLevel(evt.newLevel, 10); // Max level is 10
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            _levelDisplay?.SetLevel(evt.levelNumber);
        }
    }
}
