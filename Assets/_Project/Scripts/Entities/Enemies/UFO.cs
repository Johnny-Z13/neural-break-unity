using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using MoreMountains.Feedbacks;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// UFO - Hit-and-run attacker with curved movement patterns.
    /// Dives at player, fires, then retreats. Uses bezier-curved paths.
    /// Based on TypeScript UFO.ts.
    ///
    /// Stats: HP=30, Speed=2.8, Damage=12, XP=25
    /// Fire Rate: 2.0s, Bullet Speed: 8.0, Bullet Damage: 14
    /// Death Damage: 25 in 3.0 radius
    /// </summary>
    public class UFO : EnemyBase
    {
        public override EnemyType EnemyType => EnemyType.UFO;

        [Header("UFO Settings")]
        [SerializeField] private float _diveSpeed = 5f;
        [SerializeField] private float _retreatSpeed = 3f;
        [SerializeField] private float _orbitRadius = 8f;
        [SerializeField] private float _diveDistance = 4f;
        [SerializeField] private float _retreatDistance = 12f;

        [Header("Attack")]
        [SerializeField] private float _fireRate = 2f;
        [SerializeField] private float _projectileSpeed = 8f;
        [SerializeField] private int _projectileDamage = 14;

        [Header("Death Explosion")]
        [SerializeField] private float _deathDamageRadius = 3f;
        [SerializeField] private int _deathDamageAmount = 25;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _domeLight;
        [SerializeField] private Color _ufoColor = new Color(0.7f, 0.7f, 0.8f); // Silver
        [SerializeField] private Color _domeColor = new Color(0.3f, 0.8f, 1f); // Cyan dome
        [SerializeField] private float _wobbleSpeed = 2f;
        [SerializeField] private float _wobbleAmount = 5f;

        [Header("Feel Feedbacks")]
        [SerializeField] private MMF_Player _diveFeedback;
        [SerializeField] private MMF_Player _fireFeedback;
        [SerializeField] private MMF_Player _retreatFeedback;

        // State
        private enum UFOState { Approaching, Orbiting, Diving, Retreating }
        private UFOState _ufoState = UFOState.Approaching;

        private float _fireTimer;
        private float _orbitAngle;
        private float _ufoStateTimer;
        private Vector2 _diveTarget;
        private Vector2 _retreatTarget;
        private float _wobblePhase;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _ufoState = UFOState.Approaching;
            _fireTimer = _fireRate * 0.5f;
            _orbitAngle = Random.Range(0f, 360f);
            _ufoStateTimer = 0f;
            _wobblePhase = Random.Range(0f, Mathf.PI * 2f);
        }

        protected override void UpdateAI()
        {
            float distanceToPlayer = GetDistanceToPlayer();

            // State machine
            switch (_ufoState)
            {
                case UFOState.Approaching:
                    UpdateApproaching(distanceToPlayer);
                    break;

                case UFOState.Orbiting:
                    UpdateOrbiting(distanceToPlayer);
                    break;

                case UFOState.Diving:
                    UpdateDiving();
                    break;

                case UFOState.Retreating:
                    UpdateRetreating(distanceToPlayer);
                    break;
            }

            // Visual wobble
            UpdateWobble();
        }

        private void UpdateApproaching(float distanceToPlayer)
        {
            // Move toward orbit distance
            if (distanceToPlayer > _orbitRadius + 2f)
            {
                Vector2 direction = GetDirectionToPlayer();
                transform.position = (Vector2)transform.position + direction * _speed * Time.deltaTime;
            }
            else
            {
                // Start orbiting
                _ufoState = UFOState.Orbiting;
                _ufoStateTimer = 0f;
            }
        }

        private void UpdateOrbiting(float distanceToPlayer)
        {
            // Orbit around player
            _orbitAngle += _speed * 20f * Time.deltaTime; // degrees per second
            if (_orbitAngle >= 360f) _orbitAngle -= 360f;

            Vector2 playerPos = _playerTarget != null ? (Vector2)_playerTarget.position : Vector2.zero;
            float rad = _orbitAngle * Mathf.Deg2Rad;
            Vector2 targetPos = playerPos + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _orbitRadius;

            transform.position = Vector2.MoveTowards(transform.position, targetPos, _speed * Time.deltaTime);

            // Fire while orbiting
            _fireTimer += Time.deltaTime;
            if (_fireTimer >= _fireRate)
            {
                FireAtPlayer();
                _fireTimer = 0f;
            }

            // Periodically dive
            _ufoStateTimer += Time.deltaTime;
            if (_ufoStateTimer > 3f && Random.value < 0.02f) // ~2% chance per frame after 3s
            {
                StartDive();
            }
        }

        private void StartDive()
        {
            _ufoState = UFOState.Diving;
            _diveTarget = _playerTarget != null ? (Vector2)_playerTarget.position : Vector2.zero;
            _diveFeedback?.PlayFeedbacks();
        }

        private void UpdateDiving()
        {
            // Dive toward player position (where they were when dive started)
            Vector2 direction = (_diveTarget - (Vector2)transform.position).normalized;
            transform.position = (Vector2)transform.position + direction * _diveSpeed * Time.deltaTime;

            // Fire during dive
            _fireTimer += Time.deltaTime;
            if (_fireTimer >= _fireRate * 0.5f) // Faster firing during dive
            {
                FireAtPlayer();
                _fireTimer = 0f;
            }

            // Check if close enough to target
            if (Vector2.Distance(transform.position, _diveTarget) < _diveDistance)
            {
                StartRetreat();
            }
        }

        private void StartRetreat()
        {
            _ufoState = UFOState.Retreating;

            // Retreat in opposite direction from player
            Vector2 awayFromPlayer = -GetDirectionToPlayer();
            _retreatTarget = (Vector2)transform.position + awayFromPlayer * _retreatDistance;
            _retreatFeedback?.PlayFeedbacks();
        }

        private void UpdateRetreating(float distanceToPlayer)
        {
            // Move to retreat position
            Vector2 direction = (_retreatTarget - (Vector2)transform.position).normalized;
            transform.position = (Vector2)transform.position + direction * _retreatSpeed * Time.deltaTime;

            // Check if reached retreat distance
            if (distanceToPlayer > _retreatDistance || Vector2.Distance(transform.position, _retreatTarget) < 1f)
            {
                _ufoState = UFOState.Approaching;
                _ufoStateTimer = 0f;
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
                _domeColor
            );

            _fireFeedback?.PlayFeedbacks();
        }

        private void UpdateWobble()
        {
            _wobblePhase += Time.deltaTime * _wobbleSpeed;
            float wobble = Mathf.Sin(_wobblePhase) * _wobbleAmount;
            transform.rotation = Quaternion.Euler(0, 0, wobble);
        }

        public override void Kill()
        {
            // Deal death damage
            DealDeathDamage();
            base.Kill();
        }

        private void DealDeathDamage()
        {
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
                    _spriteRenderer.color = new Color(_ufoColor.r, _ufoColor.g, _ufoColor.b, 0.5f);
                    break;
                case EnemyState.Alive:
                    _spriteRenderer.color = _ufoColor;
                    break;
                case EnemyState.Dying:
                    _spriteRenderer.color = Color.white;
                    break;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Orbit radius
            Gizmos.color = Color.cyan;
            if (_playerTarget != null)
            {
                Gizmos.DrawWireSphere(_playerTarget.position, _orbitRadius);
            }

            // Death damage radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, _deathDamageRadius);
        }
    }
}
