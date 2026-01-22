using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using MoreMountains.Feedbacks;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// UFO - Erratic alien attacker with unpredictable movement!
    /// Features: teleport dashes, figure-8 patterns, strafing runs, and abduction beams.
    /// Highly mobile and hard to hit - classic arcade UFO behavior.
    ///
    /// Stats: HP=30, Speed=2.8, Damage=12, XP=25
    /// Fire Rate: 2.0s, Bullet Speed: 8.0, Bullet Damage: 14
    /// Death Damage: 25 in 3.0 radius
    /// </summary>
    public class UFO : EnemyBase
    {
        public override EnemyType EnemyType => EnemyType.UFO;

        [Header("UFO Movement")]
        [SerializeField] private float _dashSpeed = 12f;
        [SerializeField] private float _normalSpeed = 3f;
        [SerializeField] private float _strafeSpeed = 6f;
        [SerializeField] private float _preferredDistance = 7f;
        [SerializeField] private float _minDistance = 4f;
        [SerializeField] private float _maxDistance = 12f;

        [Header("Behavior Timings")]
        [SerializeField] private float _dashCooldown = 2f;
        [SerializeField] private float _dashDuration = 0.3f;
        [SerializeField] private float _hoverDuration = 1.5f;
        [SerializeField] private float _strafeDuration = 2f;

        [Header("Attack")]
        [SerializeField] private float _fireRate = 1.5f;
        [SerializeField] private float _burstCount = 3;
        [SerializeField] private float _burstDelay = 0.15f;
        [SerializeField] private float _projectileSpeed = 9f;
        [SerializeField] private int _projectileDamage = 12;

        [Header("Death Explosion")]
        [SerializeField] private float _deathDamageRadius = 3f;
        [SerializeField] private int _deathDamageAmount = 25;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _domeLight;
        [SerializeField] private UFOVisuals _visuals;
        [SerializeField] private Color _ufoColor = new Color(0.7f, 0.7f, 0.8f); // Silver
        [SerializeField] private Color _domeColor = new Color(0.3f, 0.8f, 1f); // Cyan dome
        [SerializeField] private float _wobbleSpeed = 3f;
        [SerializeField] private float _wobbleAmount = 8f;
        [SerializeField] private float _tiltAmount = 15f;

        [Header("Feel Feedbacks")]
        [SerializeField] private MMF_Player _dashFeedback;
        [SerializeField] private MMF_Player _fireFeedback;
        [SerializeField] private MMF_Player _hoverFeedback;

        // State
        private enum UFOState { Approaching, Hovering, Strafing, Dashing, FigureEight }
        private UFOState _ufoState = UFOState.Approaching;

        private float _fireTimer;
        private float _stateTimer;
        private float _dashCooldownTimer;
        private float _figure8Phase;
        private float _strafeDirection;
        private Vector2 _dashTarget;
        private Vector2 _lastPosition;
        private float _wobblePhase;
        private float _tiltPhase;
        private bool _visualsGenerated;
        private int _burstShotsFired;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _ufoState = UFOState.Approaching;
            _fireTimer = _fireRate * 0.5f;
            _stateTimer = 0f;
            _dashCooldownTimer = _dashCooldown;
            _figure8Phase = Random.Range(0f, Mathf.PI * 2f);
            _strafeDirection = Random.value > 0.5f ? 1f : -1f;
            _wobblePhase = Random.Range(0f, Mathf.PI * 2f);
            _tiltPhase = 0f;
            _lastPosition = transform.position;
            _burstShotsFired = 0;

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
                _visuals = GetComponentInChildren<UFOVisuals>();
            }

            if (_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                _visuals = visualsGO.AddComponent<UFOVisuals>();
            }
        }

        protected override void UpdateAI()
        {
            float distanceToPlayer = GetDistanceToPlayer();
            _lastPosition = transform.position;

            // Update dash cooldown
            _dashCooldownTimer -= Time.deltaTime;

            // State machine
            switch (_ufoState)
            {
                case UFOState.Approaching:
                    UpdateApproaching(distanceToPlayer);
                    break;

                case UFOState.Hovering:
                    UpdateHovering(distanceToPlayer);
                    break;

                case UFOState.Strafing:
                    UpdateStrafing(distanceToPlayer);
                    break;

                case UFOState.Dashing:
                    UpdateDashing();
                    break;

                case UFOState.FigureEight:
                    UpdateFigureEight(distanceToPlayer);
                    break;
            }

            // Update firing
            UpdateFiring();

            // Visual effects based on movement
            UpdateVisuals();
        }

        private void UpdateApproaching(float distanceToPlayer)
        {
            // Move toward preferred distance
            if (distanceToPlayer > _preferredDistance + 2f)
            {
                Vector2 direction = GetDirectionToPlayer();
                transform.position = (Vector2)transform.position + direction * _normalSpeed * Time.deltaTime;
            }
            else
            {
                // Pick a random behavior
                ChooseNextBehavior();
            }
        }

        private void ChooseNextBehavior()
        {
            float roll = Random.value;

            if (roll < 0.3f)
            {
                // Hover and shoot
                _ufoState = UFOState.Hovering;
                _stateTimer = _hoverDuration;
                _hoverFeedback?.PlayFeedbacks();
            }
            else if (roll < 0.6f)
            {
                // Strafe around player
                _ufoState = UFOState.Strafing;
                _stateTimer = _strafeDuration;
                _strafeDirection = Random.value > 0.5f ? 1f : -1f;
            }
            else
            {
                // Figure-8 pattern
                _ufoState = UFOState.FigureEight;
                _stateTimer = 4f; // Full figure-8 cycle
                _figure8Phase = 0f;
            }
        }

        private void UpdateHovering(float distanceToPlayer)
        {
            _stateTimer -= Time.deltaTime;

            // Gentle floating motion
            float hoverX = Mathf.Sin(Time.time * 2f) * 0.5f;
            float hoverY = Mathf.Cos(Time.time * 1.5f) * 0.3f;
            Vector2 hover = new Vector2(hoverX, hoverY);

            transform.position = (Vector2)transform.position + hover * Time.deltaTime;

            // Maintain distance
            MaintainDistance(distanceToPlayer);

            // Try to dash if cooldown ready
            if (_dashCooldownTimer <= 0 && Random.value < 0.03f)
            {
                StartDash();
                return;
            }

            if (_stateTimer <= 0)
            {
                ChooseNextBehavior();
            }
        }

        private void UpdateStrafing(float distanceToPlayer)
        {
            _stateTimer -= Time.deltaTime;

            // Circle-strafe around player
            Vector2 toPlayer = GetDirectionToPlayer();
            Vector2 perpendicular = new Vector2(-toPlayer.y, toPlayer.x) * _strafeDirection;

            // Move perpendicular to player
            Vector2 movement = perpendicular * _strafeSpeed * Time.deltaTime;

            // Also maintain distance
            float distanceError = distanceToPlayer - _preferredDistance;
            movement += toPlayer * distanceError * 2f * Time.deltaTime;

            transform.position = (Vector2)transform.position + movement;

            // Occasionally reverse direction
            if (Random.value < 0.01f)
            {
                _strafeDirection *= -1f;
            }

            // Try to dash
            if (_dashCooldownTimer <= 0 && Random.value < 0.02f)
            {
                StartDash();
                return;
            }

            if (_stateTimer <= 0)
            {
                ChooseNextBehavior();
            }
        }

        private void StartDash()
        {
            _ufoState = UFOState.Dashing;
            _stateTimer = _dashDuration;
            _dashCooldownTimer = _dashCooldown;

            // Pick a random dash target - either toward player, away, or to the side
            float roll = Random.value;
            Vector2 playerPos = _playerTarget != null ? (Vector2)_playerTarget.position : Vector2.zero;

            if (roll < 0.3f)
            {
                // Dash toward player (aggressive)
                _dashTarget = playerPos + GetDirectionToPlayer() * -_minDistance;
            }
            else if (roll < 0.6f)
            {
                // Dash away (evasive)
                _dashTarget = (Vector2)transform.position - GetDirectionToPlayer() * 6f;
            }
            else
            {
                // Dash to random position around player
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(_minDistance, _maxDistance);
                _dashTarget = playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            }

            _dashFeedback?.PlayFeedbacks();
        }

        private void UpdateDashing()
        {
            _stateTimer -= Time.deltaTime;

            // Quick teleport-like dash
            Vector2 direction = (_dashTarget - (Vector2)transform.position).normalized;
            transform.position = (Vector2)transform.position + direction * _dashSpeed * Time.deltaTime;

            // Check if reached target or time expired
            if (_stateTimer <= 0 || Vector2.Distance(transform.position, _dashTarget) < 0.5f)
            {
                ChooseNextBehavior();
            }
        }

        private void UpdateFigureEight(float distanceToPlayer)
        {
            _stateTimer -= Time.deltaTime;
            _figure8Phase += Time.deltaTime * 1.5f;

            // Figure-8 (lemniscate) parametric equations
            float t = _figure8Phase;
            float scale = 4f;
            float x = scale * Mathf.Cos(t) / (1f + Mathf.Sin(t) * Mathf.Sin(t));
            float y = scale * Mathf.Sin(t) * Mathf.Cos(t) / (1f + Mathf.Sin(t) * Mathf.Sin(t));

            // Offset from player position
            Vector2 playerPos = _playerTarget != null ? (Vector2)_playerTarget.position : Vector2.zero;
            Vector2 targetPos = playerPos + new Vector2(x, y) + Vector2.up * _preferredDistance * 0.5f;

            // Smooth movement to target
            transform.position = Vector2.Lerp(transform.position, targetPos, Time.deltaTime * 3f);

            // Try to dash
            if (_dashCooldownTimer <= 0 && Random.value < 0.015f)
            {
                StartDash();
                return;
            }

            if (_stateTimer <= 0)
            {
                ChooseNextBehavior();
            }
        }

        private void MaintainDistance(float distanceToPlayer)
        {
            if (distanceToPlayer < _minDistance)
            {
                // Too close, move away
                Vector2 away = -GetDirectionToPlayer();
                transform.position = (Vector2)transform.position + away * _normalSpeed * Time.deltaTime;
            }
            else if (distanceToPlayer > _maxDistance)
            {
                // Too far, move closer
                Vector2 toward = GetDirectionToPlayer();
                transform.position = (Vector2)transform.position + toward * _normalSpeed * Time.deltaTime;
            }
        }

        private void UpdateFiring()
        {
            _fireTimer += Time.deltaTime;

            if (_fireTimer >= _fireRate)
            {
                // Fire a burst
                StartCoroutine(FireBurst());
                _fireTimer = 0f;
            }
        }

        private System.Collections.IEnumerator FireBurst()
        {
            for (int i = 0; i < _burstCount; i++)
            {
                FireAtPlayer();
                if (i < _burstCount - 1)
                {
                    yield return new WaitForSeconds(_burstDelay);
                }
            }
        }

        private void FireAtPlayer()
        {
            if (EnemyProjectilePool.Instance == null) return;

            Vector2 direction = GetDirectionToPlayer();

            // Add slight spread for more interesting patterns
            float spread = Random.Range(-5f, 5f) * Mathf.Deg2Rad;
            direction = new Vector2(
                direction.x * Mathf.Cos(spread) - direction.y * Mathf.Sin(spread),
                direction.x * Mathf.Sin(spread) + direction.y * Mathf.Cos(spread)
            );

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

        private void UpdateVisuals()
        {
            _wobblePhase += Time.deltaTime * _wobbleSpeed;

            // Calculate movement direction for tilt
            Vector2 velocity = ((Vector2)transform.position - _lastPosition) / Time.deltaTime;
            float targetTilt = -velocity.x * _tiltAmount;
            _tiltPhase = Mathf.Lerp(_tiltPhase, targetTilt, Time.deltaTime * 5f);

            // Combine wobble and tilt
            float wobble = Mathf.Sin(_wobblePhase) * _wobbleAmount;
            transform.rotation = Quaternion.Euler(0, 0, wobble + _tiltPhase);
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

            // Preferred distance
            Gizmos.color = Color.cyan;
            if (_playerTarget != null)
            {
                Gizmos.DrawWireSphere(_playerTarget.position, _preferredDistance);
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_playerTarget.position, _minDistance);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_playerTarget.position, _maxDistance);
            }

            // Death damage radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, _deathDamageRadius);

            // Dash target
            if (_ufoState == UFOState.Dashing)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _dashTarget);
                Gizmos.DrawWireSphere(_dashTarget, 0.5f);
            }
        }
    }
}
