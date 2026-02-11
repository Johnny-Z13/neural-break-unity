using UnityEngine;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.UI
{
    /// <summary>
    /// Main HUD orchestrator. Routes events to child display components.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Display Components")]
        [SerializeField] private HealthDisplay m_healthDisplay;
        [SerializeField] private ScoreDisplay m_scoreDisplay;
        [SerializeField] private WeaponHeatDisplay m_heatDisplay;
        [SerializeField] private LevelDisplay m_levelDisplay;

        private void Awake()
        {
            // Subscribe to events in Awake so we receive events even when disabled
            // (HUD starts disabled and is enabled by UIManager when game starts)
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

        private void OnDestroy()
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
            m_healthDisplay?.ResetDisplay();
            m_scoreDisplay?.ResetDisplay();
            m_heatDisplay?.ResetDisplay();
            m_levelDisplay?.SetLevel(1);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            m_healthDisplay?.UpdateHealth(evt.currentHealth, evt.maxHealth);
        }

        private void OnPlayerHealed(PlayerHealedEvent evt)
        {
            m_healthDisplay?.UpdateHealth(evt.currentHealth, evt.maxHealth);
        }

        private void OnShieldChanged(ShieldChangedEvent evt)
        {
            m_healthDisplay?.UpdateShields(evt.currentShields, evt.maxShields);
        }

        private void OnScoreChanged(ScoreChangedEvent evt)
        {
            m_scoreDisplay?.UpdateScore(evt.newScore, evt.delta, evt.worldPosition);
        }

        private void OnComboChanged(ComboChangedEvent evt)
        {
            m_scoreDisplay?.UpdateCombo(evt.comboCount, evt.multiplier);
        }

        private void OnWeaponHeatChanged(WeaponHeatChangedEvent evt)
        {
            m_heatDisplay?.UpdateHeat(evt.heat, evt.maxHeat, evt.isOverheated);
        }

        private void OnPowerUpChanged(PowerUpChangedEvent evt)
        {
            m_heatDisplay?.UpdatePowerLevel(evt.newLevel, 10); // Max level is 10
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            m_levelDisplay?.SetLevel(evt.levelNumber);
        }
    }
}
