using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using MoreMountains.Feedbacks;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Fizzer - Fast, erratic, high-speed chaos enemy.
    /// Very fast movement with unpredictable zigzag patterns.
    /// Based on TypeScript Fizzer.ts.
    ///
    /// Stats: HP=2, Speed=8.0 (VERY FAST), Damage=6, XP=15
    /// Burst Fire: 2 shots, 3.0s between bursts, 0.2s between shots
    /// Death Damage: 15 in 2.0 radius (electric explosion)
    /// </summary>
    public class Fizzer : EnemyBase
    {
        public override EnemyType EnemyType => EnemyType.Fizzer;

        [Header("Fizzer Settings")]
        [SerializeField] private float _directionChangeInterval = 0.15f;
        [SerializeField] private float _zigzagAmplitude = 3f;
        [SerializeField] private float _zigzagFrequency = 8f;

        [Header("Burst Fire")]
        [SerializeField] private float _burstCooldown = 3f;
        [SerializeField] private int _burstCount = 2;
        [SerializeField] private float _burstDelay = 0.2f;
        [SerializeField] private float _projectileSpeed = 9f;
        [SerializeField] private int _projectileDamage = 6;

        [Header("Death Explosion")]
        [SerializeField] private float _deathDamageRadius = 2f;
        [SerializeField] private int _deathDamageAmount = 15;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private FizzerVisuals _visuals;
        [SerializeField] private Color _electricColor = new Color(0.2f, 0.8f, 1f); // Electric cyan-blue

        [Header("Feel Feedbacks")]
        [SerializeField] private MMF_Player _burstFeedback;
        [SerializeField] private MMF_Player _electricDeathFeedback;

        // Movement state
        private Vector2 _currentDirection;
        private float _directionTimer;
        private float _zigzagOffset;
        private float _zigzagPhase;

        // Attack state
        private float _burstTimer;
        private bool _isFiringBurst;
        private bool _visualsGenerated;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _currentDirection = GetDirectionToPlayer();
            _directionTimer = 0f;
            _zigzagOffset = Random.Range(0f, Mathf.PI * 2f); // Random start phase
            _zigzagPhase = 0f;
            _burstTimer = _burstCooldown * Random.Range(0.3f, 0.7f); // Random initial delay
            _isFiringBurst = false;

            // Set trail color
            if (_trailRenderer != null)
            {
                _trailRenderer.startColor = _electricColor;
                _trailRenderer.endColor = new Color(_electricColor.r, _electricColor.g, _electricColor.b, 0f);
            }

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
                _visuals = GetComponentInChildren<FizzerVisuals>();
            }

            if (_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                _visuals = visualsGO.AddComponent<FizzerVisuals>();
            }
        }

        protected override void UpdateAI()
        {
            UpdateMovement();
            UpdateAttack();
        }

        private void UpdateMovement()
        {
            // Change direction periodically for erratic behavior
            _directionTimer += Time.deltaTime;
            if (_directionTimer >= _directionChangeInterval)
            {
                UpdateDirection();
                _directionTimer = 0f;
            }

            // Calculate zigzag offset perpendicular to movement
            _zigzagPhase += Time.deltaTime * _zigzagFrequency;
            float zigzag = Mathf.Sin(_zigzagPhase + _zigzagOffset) * _zigzagAmplitude;

            // Perpendicular direction for zigzag
            Vector2 perpendicular = new Vector2(-_currentDirection.y, _currentDirection.x);

            // Combined movement: toward player + zigzag
            Vector2 movement = (_currentDirection * _speed + perpendicular * zigzag) * Time.deltaTime;
            transform.position = (Vector2)transform.position + movement;

            // Visual rotation based on velocity
            if (movement.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }
        }

        private void UpdateDirection()
        {
            // Mostly toward player, but with random variation
            Vector2 toPlayer = GetDirectionToPlayer();

            // Add random offset for unpredictability
            float randomAngle = Random.Range(-45f, 45f) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(randomAngle);
            float sin = Mathf.Sin(randomAngle);

            _currentDirection = new Vector2(
                toPlayer.x * cos - toPlayer.y * sin,
                toPlayer.x * sin + toPlayer.y * cos
            ).normalized;
        }

        private void UpdateAttack()
        {
            if (_isFiringBurst) return;

            _burstTimer += Time.deltaTime;
            if (_burstTimer >= _burstCooldown)
            {
                StartCoroutine(FireBurst());
                _burstTimer = 0f;
            }
        }

        private System.Collections.IEnumerator FireBurst()
        {
            _isFiringBurst = true;
            _burstFeedback?.PlayFeedbacks();

            for (int i = 0; i < _burstCount; i++)
            {
                FireProjectile();

                if (i < _burstCount - 1)
                {
                    yield return new WaitForSeconds(_burstDelay);
                }
            }

            _isFiringBurst = false;
        }

        private void FireProjectile()
        {
            if (EnemyProjectilePool.Instance == null) return;

            Vector2 direction = GetDirectionToPlayer();
            Vector2 firePos = (Vector2)transform.position + direction * 0.3f;

            EnemyProjectilePool.Instance.Fire(
                firePos,
                direction,
                _projectileSpeed,
                _projectileDamage,
                _electricColor
            );
        }

        public override void Kill()
        {
            // Electric death explosion
            _electricDeathFeedback?.PlayFeedbacks();

            // Damage nearby enemies
            DealDeathDamage();

            base.Kill();
        }

        private void DealDeathDamage()
        {
            // Find all enemies in radius and damage them
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _deathDamageRadius);

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(_deathDamageAmount, transform.position);
                }
            }
        }

        protected override void OnStateChanged(EnemyState newState)
        {
            base.OnStateChanged(newState);

            if (_spriteRenderer == null) return;

            switch (newState)
            {
                case EnemyState.Spawning:
                    _spriteRenderer.color = new Color(_electricColor.r, _electricColor.g, _electricColor.b, 0.5f);
                    break;
                case EnemyState.Alive:
                    _spriteRenderer.color = _electricColor;
                    break;
                case EnemyState.Dying:
                    _spriteRenderer.color = Color.white;
                    break;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Death damage radius
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, _deathDamageRadius);
        }
    }
}
