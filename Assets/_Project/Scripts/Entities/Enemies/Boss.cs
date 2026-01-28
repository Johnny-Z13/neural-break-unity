using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Boss - Multi-phase level boss enemy.
    /// Three distinct attack phases based on health percentage.
    /// Based on TypeScript Boss.ts.
    ///
    /// Stats: HP=180, Speed=0.3 (slow, menacing), Damage=25, XP=100
    /// Phase 1 (100-67%): Normal fire, 1.5s rate
    /// Phase 2 (66-34%): Faster fire, 1.2s rate
    /// Phase 3 (33-0%): Ring attacks instead of bullets
    /// Death Damage: 75 in 12.0 radius (epic explosion)
    /// </summary>
    public class Boss : EnemyBase
    {
        public override EnemyType EnemyType => EnemyType.Boss;

        [Header("Boss Settings")]
        [SerializeField] private float _phase2FireRateMultiplier = 0.8f; // Phase 2 is 80% of phase 1 rate

        [Header("Phase 3 Ring Attack")]
        [SerializeField] private float _ringAttackCooldown = 3f;
        [SerializeField] private float _ringExpandSpeed = 2f;
        [SerializeField] private float _ringDuration = 3f;
        [SerializeField] private int _ringDamage = 30;
        [SerializeField] private float _ringWidth = 0.5f;

        [Header("Death Explosion")]
        [SerializeField] private float _deathDamageRadius = 12f;
        [SerializeField] private int _deathDamageAmount = 75;
        [SerializeField] private int _deathBulletCount = 24;

        // Config-driven shooting values
        private float _phase1FireRate => EnemyConfig?.fireRate ?? 1.5f;
        private float _phase2FireRate => _phase1FireRate * _phase2FireRateMultiplier;
        private float _projectileSpeed => EnemyConfig?.projectileSpeed ?? 6.5f;
        private int _projectileDamage => EnemyConfig?.projectileDamage ?? 18;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private SpriteRenderer _coreRenderer;
        [SerializeField] private Transform _ringVisual;
        [SerializeField] private Color _phase1Color = new Color(0.8f, 0.2f, 0.2f); // Red
        [SerializeField] private Color _phase2Color = new Color(1f, 0.5f, 0f); // Orange
        [SerializeField] private Color _phase3Color = new Color(1f, 0f, 0.5f); // Pink
        [SerializeField] private float _pulseSpeed = 2f;
        [SerializeField] private float _pulseAmount = 0.1f;

        // Note: MMFeedbacks removed

        // State
        private enum BossPhase { Phase1, Phase2, Phase3 }
        private BossPhase _currentPhase = BossPhase.Phase1;

        private float _fireTimer;
        private float _ringTimer;
        private float _pulsePhase;

        // Ring attack state
        private bool _isRingActive;
        private float _currentRingRadius;
        private float _ringActiveTime;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _currentPhase = BossPhase.Phase1;
            _fireTimer = 0f;
            _ringTimer = 0f;
            _pulsePhase = 0f;
            _isRingActive = false;
            _currentRingRadius = 0f;

            // Hide ring visual initially
            if (_ringVisual != null)
            {
                _ringVisual.gameObject.SetActive(false);
            }

            UpdatePhaseVisuals();

            // Announce boss encounter
            PublishBossEvent(true);
        }

        protected override void UpdateAI()
        {
            UpdatePhase();
            UpdateMovement();
            UpdatePulse();

            switch (_currentPhase)
            {
                case BossPhase.Phase1:
                case BossPhase.Phase2:
                    UpdateProjectileAttack();
                    break;
                case BossPhase.Phase3:
                    UpdateRingAttack();
                    break;
            }
        }

        private void UpdatePhase()
        {
            float healthPercent = HealthPercent;
            BossPhase newPhase;

            if (healthPercent > 0.67f)
            {
                newPhase = BossPhase.Phase1;
            }
            else if (healthPercent > 0.34f)
            {
                newPhase = BossPhase.Phase2;
            }
            else
            {
                newPhase = BossPhase.Phase3;
            }

            if (newPhase != _currentPhase)
            {
                TransitionToPhase(newPhase);
            }
        }

        private void TransitionToPhase(BossPhase newPhase)
        {
            _currentPhase = newPhase;
            _fireTimer = 0f;
            _ringTimer = 0f;

            switch (newPhase)
            {
                case BossPhase.Phase1:
                    // Feedback (Feel removed)
                    break;
                case BossPhase.Phase2:
                    // Feedback (Feel removed)
                    Debug.Log("[Boss] Entering Phase 2 - Increased aggression!");
                    break;
                case BossPhase.Phase3:
                    // Feedback (Feel removed)
                    Debug.Log("[Boss] Entering Phase 3 - Ring attacks!");
                    break;
            }

            UpdatePhaseVisuals();
        }

        private void UpdateMovement()
        {
            // Slow, menacing approach
            Vector2 direction = GetDirectionToPlayer();
            transform.position = (Vector2)transform.position + direction * _speed * Time.deltaTime;
        }

        private void UpdatePulse()
        {
            _pulsePhase += Time.deltaTime * _pulseSpeed;
            float pulse = 1f + Mathf.Sin(_pulsePhase) * _pulseAmount;
            transform.localScale = Vector3.one * pulse * 1.5f; // Boss is larger

            // Core glow intensity
            if (_coreRenderer != null)
            {
                float glow = 0.5f + Mathf.Sin(_pulsePhase * 2f) * 0.3f;
                Color coreColor = GetPhaseColor();
                coreColor.a = glow;
                _coreRenderer.color = coreColor;
            }
        }

        private void UpdateProjectileAttack()
        {
            float fireRate = _currentPhase == BossPhase.Phase1 ? _phase1FireRate : _phase2FireRate;

            _fireTimer += Time.deltaTime;
            if (_fireTimer >= fireRate)
            {
                FireAtPlayer();
                _fireTimer = 0f;
            }
        }

        private void FireAtPlayer()
        {
            if (EnemyProjectilePool.Instance == null) return;

            Vector2 direction = GetDirectionToPlayer();

            // Fire spread based on phase
            int bulletCount = _currentPhase == BossPhase.Phase1 ? 3 : 5;
            float spread = _currentPhase == BossPhase.Phase1 ? 30f : 45f;

            EnemyProjectilePool.Instance.FireSpread(
                transform.position,
                direction,
                _projectileSpeed,
                _projectileDamage,
                bulletCount,
                spread,
                GetPhaseColor()
            );

            // Feedback (Feel removed)
        }

        private void UpdateRingAttack()
        {
            if (_isRingActive)
            {
                UpdateActiveRing();
            }
            else
            {
                _ringTimer += Time.deltaTime;
                if (_ringTimer >= _ringAttackCooldown)
                {
                    StartRingAttack();
                    _ringTimer = 0f;
                }
            }
        }

        private void StartRingAttack()
        {
            _isRingActive = true;
            _currentRingRadius = 0f;
            _ringActiveTime = 0f;

            if (_ringVisual != null)
            {
                _ringVisual.gameObject.SetActive(true);
                _ringVisual.localScale = Vector3.zero;
            }

            // Feedback (Feel removed)
        }

        private void UpdateActiveRing()
        {
            _ringActiveTime += Time.deltaTime;
            _currentRingRadius += _ringExpandSpeed * Time.deltaTime;

            // Update ring visual
            if (_ringVisual != null)
            {
                _ringVisual.localScale = Vector3.one * _currentRingRadius * 2f;
            }

            // Check player collision with ring
            CheckRingCollision();

            // End ring attack
            if (_ringActiveTime >= _ringDuration)
            {
                EndRingAttack();
            }
        }

        private void CheckRingCollision()
        {
            if (_playerTarget == null) return;

            float distanceToPlayer = GetDistanceToPlayer();
            float innerRadius = _currentRingRadius - _ringWidth;
            float outerRadius = _currentRingRadius + _ringWidth;

            // Check if player is within ring band
            if (distanceToPlayer >= innerRadius && distanceToPlayer <= outerRadius)
            {
                // Damage player
                PlayerHealth playerHealth = _playerTarget.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(_ringDamage, transform.position);
                }
            }
        }

        private void EndRingAttack()
        {
            _isRingActive = false;

            if (_ringVisual != null)
            {
                _ringVisual.gameObject.SetActive(false);
            }
        }

        private void UpdatePhaseVisuals()
        {
            if (_bodyRenderer != null)
            {
                _bodyRenderer.color = GetPhaseColor();
            }
        }

        private Color GetPhaseColor()
        {
            switch (_currentPhase)
            {
                case BossPhase.Phase1: return _phase1Color;
                case BossPhase.Phase2: return _phase2Color;
                case BossPhase.Phase3: return _phase3Color;
                default: return _phase1Color;
            }
        }

        public override void Kill()
        {
            // Announce boss defeated
            PublishBossEvent(false);

            // Feedback (Feel removed)
            DealDeathDamage();

            // Epic death bullet nova
            if (EnemyProjectilePool.Instance != null)
            {
                EnemyProjectilePool.Instance.FireRing(
                    transform.position,
                    _projectileSpeed * 1.5f,
                    _projectileDamage,
                    _deathBulletCount,
                    Color.red
                );
            }

            // Hide ring
            if (_ringVisual != null)
            {
                _ringVisual.gameObject.SetActive(false);
            }

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

        public override void TakeDamage(int damage, Vector2 damageSource)
        {
            base.TakeDamage(damage, damageSource);

            // Screen shake scales with boss damage
            EventBus.Publish(new EnemyDamagedEvent
            {
                enemyType = EnemyType.Boss,
                damage = damage,
                currentHealth = CurrentHealth,
                position = transform.position
            });

            // Update boss health UI
            PublishBossEvent(true);
        }

        private void PublishBossEvent(bool isActive)
        {
            EventBus.Publish(new BossEncounterEvent
            {
                isBossActive = isActive,
                bossHealth = CurrentHealth,
                bossMaxHealth = MaxHealth,
                healthPercent = HealthPercent
            });
        }

        protected override void OnStateChanged(EnemyState newState)
        {
            base.OnStateChanged(newState);

            if (_bodyRenderer == null) return;

            switch (newState)
            {
                case EnemyState.Spawning:
                    _bodyRenderer.color = new Color(_phase1Color.r, _phase1Color.g, _phase1Color.b, 0.5f);
                    break;
                case EnemyState.Alive:
                    UpdatePhaseVisuals();
                    break;
                case EnemyState.Dying:
                    _bodyRenderer.color = Color.white;
                    break;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Death damage radius
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, _deathDamageRadius);

            // Current ring radius (if active)
            if (_isRingActive)
            {
                Gizmos.color = new Color(1f, 0f, 0.5f, 0.5f);
                Gizmos.DrawWireSphere(transform.position, _currentRingRadius - _ringWidth);
                Gizmos.DrawWireSphere(transform.position, _currentRingRadius + _ringWidth);
            }
        }
    }
}
