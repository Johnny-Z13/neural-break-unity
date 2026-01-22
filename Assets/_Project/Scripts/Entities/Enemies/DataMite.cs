using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// DataMite - Basic entry-level enemy that swarms the player.
    /// Simple AI: Move toward player with slight sway oscillation.
    /// Based on TypeScript DataMite.ts.
    /// </summary>
    public class DataMite : EnemyBase
    {
        [Header("DataMite Settings")]
        [SerializeField] private float _swayAmplitude = 0.5f;
        [SerializeField] private float _swayFrequency = 3f;

        [Header("Visual")]
        [SerializeField] private DataMiteVisuals _visuals;

        // Sway oscillation
        private float _swayOffset;
        private float _swayTimer;
        private bool _visualsGenerated;

        public override EnemyType EnemyType => EnemyType.DataMite;

        protected override void OnInitialize()
        {
            // Randomize sway offset so mites don't all sway in sync
            _swayOffset = Random.Range(0f, Mathf.PI * 2f);
            _swayTimer = 0f;

            // Generate procedural visuals if not yet done
            if (!_visualsGenerated)
            {
                EnsureVisuals();
                _visualsGenerated = true;
            }
        }

        private void EnsureVisuals()
        {
            if (_visuals == null)
            {
                _visuals = GetComponentInChildren<DataMiteVisuals>();
            }

            if (_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                _visuals = visualsGO.AddComponent<DataMiteVisuals>();
            }
        }

        protected override void UpdateAI()
        {
            if (_playerTarget == null) return;

            // Get direction to player
            Vector2 toPlayer = GetDirectionToPlayer();

            // Calculate sway perpendicular to movement direction
            _swayTimer += Time.deltaTime * _swayFrequency;
            float swayValue = Mathf.Sin(_swayTimer + _swayOffset) * _swayAmplitude;

            // Perpendicular vector for sway
            Vector2 perpendicular = new Vector2(-toPlayer.y, toPlayer.x);
            Vector2 swayOffset = perpendicular * swayValue;

            // Final movement direction
            Vector2 moveDirection = (toPlayer + swayOffset * 0.3f).normalized;

            // Move toward player
            transform.Translate(moveDirection * _speed * Time.deltaTime, Space.World);

            // Face movement direction
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        protected override void OnStateChanged(EnemyState newState)
        {
            base.OnStateChanged(newState);
            // Visuals handle their own appearance via DataMiteVisuals
        }
    }
}
