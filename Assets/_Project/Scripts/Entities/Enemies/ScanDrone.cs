using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using MoreMountains.Feedbacks;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// ScanDrone - Ranged patrolling enemy.
    /// Patrols until player enters detection range, then pursues and fires.
    /// Based on TypeScript ScanDrone.ts.
    ///
    /// Stats: HP=30, Speed=1.2, Damage=15, XP=6
    /// Fire Rate: 2.0s, Bullet Speed: 7.0, Bullet Damage: 15
    /// </summary>
    public class ScanDrone : EnemyBase
    {
        public override EnemyType EnemyType => EnemyType.ScanDrone;

        [Header("ScanDrone Settings")]
        [SerializeField] private float _detectionRange = 15f;
        [SerializeField] private float _fireRange = 12f;
        [SerializeField] private float _fireRate = 2f;
        [SerializeField] private float _projectileSpeed = 7f;
        [SerializeField] private int _projectileDamage = 15;

        [Header("Patrol Settings")]
        [SerializeField] private float _patrolRadius = 10f;
        [SerializeField] private float _patrolSpeed = 0.8f;
        [SerializeField] private float _chaseSpeedMultiplier = 1.5f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private ScanDroneVisuals _visuals;
#pragma warning disable CS0414 // Reserved for rotation animation feature
        [SerializeField] private float _rotationSpeed = 90f; // degrees per second
#pragma warning restore CS0414

        [Header("Feel Feedbacks")]
        [SerializeField] private MMF_Player _fireFeedback;
        [SerializeField] private MMF_Player _detectFeedback;

        // State
        private enum DroneState { Patrolling, Alerted, Attacking }
        private DroneState _droneState = DroneState.Patrolling;

        private Vector2 _patrolTarget;
        private float _fireTimer;
        private float _currentRotation;
        private bool _wasPlayerInRange;
        private bool _visualsGenerated;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _droneState = DroneState.Patrolling;
            _patrolTarget = GetNewPatrolTarget();
            _fireTimer = _fireRate * 0.5f; // Start partially ready
            _currentRotation = Random.Range(0f, 360f);
            _wasPlayerInRange = false;

            // Generate procedural visuals if not yet done
            if (!_visualsGenerated)
            {
                EnsureVisuals();
                _visualsGenerated = true;
            }
        }

        private void EnsureVisuals()
        {
            // Add ScanDroneVisuals component if not present
            if (_visuals == null)
            {
                _visuals = GetComponentInChildren<ScanDroneVisuals>();
            }

            if (_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                _visuals = visualsGO.AddComponent<ScanDroneVisuals>();
            }
        }

        protected override void UpdateAI()
        {
            float distanceToPlayer = GetDistanceToPlayer();
            bool playerInRange = distanceToPlayer < _detectionRange;

            // State transitions
            switch (_droneState)
            {
                case DroneState.Patrolling:
                    if (playerInRange)
                    {
                        _droneState = DroneState.Alerted;
                        _detectFeedback?.PlayFeedbacks();
                        _visuals?.SetAlerted(true);

                        if (!_wasPlayerInRange)
                        {
                            Debug.Log("[ScanDrone] Player detected!");
                        }
                    }
                    break;

                case DroneState.Alerted:
                    if (!playerInRange)
                    {
                        _droneState = DroneState.Patrolling;
                        _patrolTarget = GetNewPatrolTarget();
                        _visuals?.SetAlerted(false);
                    }
                    else if (distanceToPlayer < _fireRange)
                    {
                        _droneState = DroneState.Attacking;
                    }
                    break;

                case DroneState.Attacking:
                    if (!playerInRange)
                    {
                        _droneState = DroneState.Patrolling;
                        _patrolTarget = GetNewPatrolTarget();
                        _visuals?.SetAlerted(false);
                    }
                    else if (distanceToPlayer > _fireRange)
                    {
                        _droneState = DroneState.Alerted;
                    }
                    break;
            }

            _wasPlayerInRange = playerInRange;

            // Behavior based on state
            switch (_droneState)
            {
                case DroneState.Patrolling:
                    UpdatePatrol();
                    break;

                case DroneState.Alerted:
                    UpdateChase();
                    break;

                case DroneState.Attacking:
                    UpdateAttack();
                    break;
            }

        }

        private void UpdatePatrol()
        {
            // Move toward patrol target
            Vector2 currentPos = transform.position;
            Vector2 direction = (_patrolTarget - currentPos).normalized;

            transform.position = currentPos + direction * _patrolSpeed * Time.deltaTime;

            // Check if reached patrol target
            if (Vector2.Distance(currentPos, _patrolTarget) < 0.5f)
            {
                _patrolTarget = GetNewPatrolTarget();
            }
        }

        private void UpdateChase()
        {
            // Move toward player
            Vector2 direction = GetDirectionToPlayer();
            float chaseSpeed = _speed * _chaseSpeedMultiplier;

            transform.position = (Vector2)transform.position + direction * chaseSpeed * Time.deltaTime;
        }

        private void UpdateAttack()
        {
            // Slow movement while attacking
            Vector2 direction = GetDirectionToPlayer();
            transform.position = (Vector2)transform.position + direction * _speed * 0.3f * Time.deltaTime;

            // Fire at player
            _fireTimer += Time.deltaTime;
            if (_fireTimer >= _fireRate)
            {
                FireAtPlayer();
                _fireTimer = 0f;
            }
        }

        private void FireAtPlayer()
        {
            if (EnemyProjectilePool.Instance == null) return;

            Vector2 direction = GetDirectionToPlayer();
            Vector2 firePos = (Vector2)transform.position + direction * 0.5f;

            EnemyProjectilePool.Instance.Fire(
                firePos,
                direction,
                _projectileSpeed,
                _projectileDamage,
                new Color(1f, 0.5f, 0f) // Orange
            );

            _fireFeedback?.PlayFeedbacks();
        }

        private Vector2 GetNewPatrolTarget()
        {
            // Random point within patrol radius of spawn position
            Vector2 offset = Random.insideUnitCircle * _patrolRadius;
            return (Vector2)transform.position + offset;
        }

        protected override void OnStateChanged(EnemyState newState)
        {
            base.OnStateChanged(newState);

            if (_spriteRenderer == null) return;

            switch (newState)
            {
                case EnemyState.Spawning:
                    _spriteRenderer.color = new Color(0.5f, 0.8f, 1f, 0.5f); // Light blue, transparent
                    break;
                case EnemyState.Alive:
                    _spriteRenderer.color = new Color(0.3f, 0.7f, 1f, 1f); // Cyan-blue
                    break;
                case EnemyState.Dying:
                    _spriteRenderer.color = Color.white;
                    break;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            // Fire range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _fireRange);

            // Patrol target
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _patrolTarget);
                Gizmos.DrawWireSphere(_patrolTarget, 0.3f);
            }
        }
    }
}
