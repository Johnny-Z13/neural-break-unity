using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using MoreMountains.Feedbacks;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// VoidSphere - Massive tank boss enemy.
    /// Very slow but extremely tanky. Fires bursts of projectiles.
    /// Huge death explosion damages nearby enemies.
    /// Based on TypeScript VoidSphere.ts.
    ///
    /// Stats: HP=650 (MASSIVE), Speed=0.5 (very slow), Damage=40, XP=50
    /// Burst Fire: 4 shots, 3.0s between bursts, 0.25s between shots
    /// Death Damage: 50 in 8.0 radius (huge explosion)
    /// </summary>
    public class VoidSphere : EnemyBase
    {
        public override EnemyType EnemyType => EnemyType.VoidSphere;

        [Header("VoidSphere Settings")]
        [SerializeField] private float _pulsateSpeed = 1f;
        [SerializeField] private float _pulsateAmount = 0.1f;
        [SerializeField] private float _gravityPullRadius = 6f;
        [SerializeField] private float _gravityPullStrength = 2f;

        [Header("Burst Attack")]
        [SerializeField] private float _burstCooldown = 3f;
        [SerializeField] private int _burstCount = 4;
        [SerializeField] private float _burstDelay = 0.25f;
        [SerializeField] private float _projectileSpeed = 5f;
        [SerializeField] private int _projectileDamage = 20;
        [SerializeField] private float _spreadAngle = 30f;

        [Header("Death Explosion")]
        [SerializeField] private float _deathDamageRadius = 8f;
        [SerializeField] private int _deathDamageAmount = 50;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private SpriteRenderer _innerGlow;
        [SerializeField] private Color _voidColor = new Color(0.2f, 0f, 0.4f); // Deep purple
        [SerializeField] private Color _glowColor = new Color(0.6f, 0f, 1f); // Purple glow

        [Header("Feel Feedbacks")]
        [SerializeField] private MMF_Player _burstFeedback;
        [SerializeField] private MMF_Player _chargeFeedback;
        [SerializeField] private MMF_Player _implosionFeedback;

        // State
        private float _burstTimer;
        private bool _isFiringBurst;
        private float _pulsatePhase;
        private float _chargeTimer;
        private bool _isCharging;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _burstTimer = _burstCooldown * 0.5f;
            _isFiringBurst = false;
            _pulsatePhase = Random.Range(0f, Mathf.PI * 2f);
            _isCharging = false;
        }

        protected override void UpdateAI()
        {
            UpdateMovement();
            UpdatePulsate();
            UpdateAttack();
            ApplyGravityPull();
        }

        private void UpdateMovement()
        {
            // Slow, relentless advance toward player
            Vector2 direction = GetDirectionToPlayer();
            transform.position = (Vector2)transform.position + direction * _speed * Time.deltaTime;
        }

        private void UpdatePulsate()
        {
            _pulsatePhase += Time.deltaTime * _pulsateSpeed;
            float scale = 1f + Mathf.Sin(_pulsatePhase) * _pulsateAmount;
            transform.localScale = Vector3.one * scale;

            // Inner glow intensity
            if (_innerGlow != null)
            {
                float glowIntensity = 0.5f + Mathf.Sin(_pulsatePhase * 2f) * 0.3f;
                Color glow = _glowColor;
                glow.a = glowIntensity;
                _innerGlow.color = glow;
            }
        }

        private void UpdateAttack()
        {
            if (_isFiringBurst) return;

            _burstTimer += Time.deltaTime;

            // Start charging before burst
            if (!_isCharging && _burstTimer >= _burstCooldown - 0.5f)
            {
                _isCharging = true;
                _chargeFeedback?.PlayFeedbacks();
            }

            if (_burstTimer >= _burstCooldown)
            {
                StartCoroutine(FireBurst());
                _burstTimer = 0f;
                _isCharging = false;
            }
        }

        private System.Collections.IEnumerator FireBurst()
        {
            _isFiringBurst = true;
            _burstFeedback?.PlayFeedbacks();

            for (int i = 0; i < _burstCount; i++)
            {
                FireProjectiles();

                if (i < _burstCount - 1)
                {
                    yield return new WaitForSeconds(_burstDelay);
                }
            }

            _isFiringBurst = false;
        }

        private void FireProjectiles()
        {
            if (EnemyProjectilePool.Instance == null) return;

            Vector2 direction = GetDirectionToPlayer();

            // Fire spread of projectiles
            EnemyProjectilePool.Instance.FireSpread(
                transform.position,
                direction,
                _projectileSpeed,
                _projectileDamage,
                3, // 3 projectiles per shot
                _spreadAngle,
                _glowColor
            );
        }

        private void ApplyGravityPull()
        {
            if (_playerTarget == null) return;

            // Pull player slightly toward the void sphere when in range
            float distanceToPlayer = GetDistanceToPlayer();
            if (distanceToPlayer <= _gravityPullRadius && distanceToPlayer > 0.5f)
            {
                // Calculate pull strength (stronger when closer)
                float pullFactor = 1f - (distanceToPlayer / _gravityPullRadius);
                Vector2 pullDirection = ((Vector2)transform.position - (Vector2)_playerTarget.position).normalized;

                // Apply subtle pull (reduced by factor to not be too oppressive)
                var playerRb = _playerTarget.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.AddForce(pullDirection * _gravityPullStrength * pullFactor, ForceMode2D.Force);
                }
            }
        }

        public override void Kill()
        {
            // Massive implosion/explosion
            _implosionFeedback?.PlayFeedbacks();
            DealDeathDamage();

            // Fire death nova
            if (EnemyProjectilePool.Instance != null)
            {
                EnemyProjectilePool.Instance.FireRing(
                    transform.position,
                    _projectileSpeed * 1.5f,
                    _projectileDamage,
                    16, // Big ring of bullets
                    _glowColor
                );
            }

            base.Kill();
        }

        private void DealDeathDamage()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _deathDamageRadius);

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                // Damage enemies
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(_deathDamageAmount, transform.position);
                }

                // Could also push player back here
            }
        }

        protected override void OnStateChanged(EnemyState newState)
        {
            base.OnStateChanged(newState);

            if (_spriteRenderer == null) return;

            switch (newState)
            {
                case EnemyState.Spawning:
                    _spriteRenderer.color = new Color(_voidColor.r, _voidColor.g, _voidColor.b, 0.5f);
                    break;
                case EnemyState.Alive:
                    _spriteRenderer.color = _voidColor;
                    break;
                case EnemyState.Dying:
                    _spriteRenderer.color = _glowColor;
                    break;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Gravity pull radius
            Gizmos.color = new Color(0.6f, 0f, 1f, 0.2f);
            Gizmos.DrawSphere(transform.position, _gravityPullRadius);

            // Death damage radius
            Gizmos.color = new Color(1f, 0f, 0.5f, 0.2f);
            Gizmos.DrawSphere(transform.position, _deathDamageRadius);
        }
    }
}
