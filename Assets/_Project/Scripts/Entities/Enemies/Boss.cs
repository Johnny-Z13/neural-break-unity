using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using Z13.Core;

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
        [SerializeField] private float m_phase2FireRateMultiplier = 0.8f; // Phase 2 is 80% of phase 1 rate

        [Header("Phase 3 Ring Attack")]
        [SerializeField] private float m_ringAttackCooldown = 3f;
        [SerializeField] private float m_ringExpandSpeed = 2f;
        [SerializeField] private float m_ringDuration = 3f;
        [SerializeField] private int m_ringDamage = 30;
        [SerializeField] private float m_ringWidth = 0.5f;

        [Header("Death Explosion")]
        [SerializeField] private float m_deathDamageRadius = 12f;
        [SerializeField] private int m_deathDamageAmount = 75;
        [SerializeField] private int m_deathBulletCount = 24;

        // Config-driven shooting values
        private float m_phase1FireRate => EnemyConfig?.fireRate ?? 1.5f;
        private float m_phase2FireRate => m_phase1FireRate * m_phase2FireRateMultiplier;
        private float m_projectileSpeed => EnemyConfig?.projectileSpeed ?? 6.5f;
        private int m_projectileDamage => EnemyConfig?.projectileDamage ?? 18;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer m_bodyRenderer;
        [SerializeField] private SpriteRenderer m_coreRenderer;
        [SerializeField] private Transform m_ringVisual;
        [SerializeField] private Color m_phase1Color = new Color(0.8f, 0.2f, 0.2f); // Red
        [SerializeField] private Color m_phase2Color = new Color(1f, 0.5f, 0f); // Orange
        [SerializeField] private Color m_phase3Color = new Color(1f, 0f, 0.5f); // Pink
        [SerializeField] private float m_pulseSpeed = 2f;
        [SerializeField] private float m_pulseAmount = 0.1f;

        // Note: MMFeedbacks removed

        // State
        private enum BossPhase { Phase1, Phase2, Phase3 }
        private BossPhase m_currentPhase = BossPhase.Phase1;

        private float m_fireTimer;
        private float m_ringTimer;
        private float m_pulsePhase;

        // Ring attack state
        private bool m_isRingActive;
        private float m_currentRingRadius;
        private float m_ringActiveTime;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            m_currentPhase = BossPhase.Phase1;
            m_fireTimer = 0f;
            m_ringTimer = 0f;
            m_pulsePhase = 0f;
            m_isRingActive = false;
            m_currentRingRadius = 0f;

            // Hide ring visual initially
            if (m_ringVisual != null)
            {
                m_ringVisual.gameObject.SetActive(false);
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

            switch (m_currentPhase)
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

            if (newPhase != m_currentPhase)
            {
                TransitionToPhase(newPhase);
            }
        }

        private void TransitionToPhase(BossPhase newPhase)
        {
            m_currentPhase = newPhase;
            m_fireTimer = 0f;
            m_ringTimer = 0f;

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
            transform.position = (Vector2)transform.position + direction * m_speed * Time.deltaTime;
        }

        private void UpdatePulse()
        {
            m_pulsePhase += Time.deltaTime * m_pulseSpeed;
            float pulse = 1f + Mathf.Sin(m_pulsePhase) * m_pulseAmount;
            transform.localScale = Vector3.one * pulse * 1.5f; // Boss is larger

            // Core glow intensity
            if (m_coreRenderer != null)
            {
                float glow = 0.5f + Mathf.Sin(m_pulsePhase * 2f) * 0.3f;
                Color coreColor = GetPhaseColor();
                coreColor.a = glow;
                m_coreRenderer.color = coreColor;
            }
        }

        private void UpdateProjectileAttack()
        {
            float fireRate = m_currentPhase == BossPhase.Phase1 ? m_phase1FireRate : m_phase2FireRate;

            m_fireTimer += Time.deltaTime;
            if (m_fireTimer >= fireRate)
            {
                FireAtPlayer();
                m_fireTimer = 0f;
            }
        }

        private void FireAtPlayer()
        {
            if (EnemyProjectilePool.Instance == null) return;

            Vector2 direction = GetDirectionToPlayer();

            // Fire spread based on phase
            int bulletCount = m_currentPhase == BossPhase.Phase1 ? 3 : 5;
            float spread = m_currentPhase == BossPhase.Phase1 ? 30f : 45f;

            EnemyProjectilePool.Instance.FireSpread(
                transform.position,
                direction,
                m_projectileSpeed,
                m_projectileDamage,
                bulletCount,
                spread,
                GetPhaseColor()
            );

            // Feedback (Feel removed)
        }

        private void UpdateRingAttack()
        {
            if (m_isRingActive)
            {
                UpdateActiveRing();
            }
            else
            {
                m_ringTimer += Time.deltaTime;
                if (m_ringTimer >= m_ringAttackCooldown)
                {
                    StartRingAttack();
                    m_ringTimer = 0f;
                }
            }
        }

        private void StartRingAttack()
        {
            m_isRingActive = true;
            m_currentRingRadius = 0f;
            m_ringActiveTime = 0f;

            if (m_ringVisual != null)
            {
                m_ringVisual.gameObject.SetActive(true);
                m_ringVisual.localScale = Vector3.zero;
            }

            // Feedback (Feel removed)
        }

        private void UpdateActiveRing()
        {
            m_ringActiveTime += Time.deltaTime;
            m_currentRingRadius += m_ringExpandSpeed * Time.deltaTime;

            // Update ring visual
            if (m_ringVisual != null)
            {
                m_ringVisual.localScale = Vector3.one * m_currentRingRadius * 2f;
            }

            // Check player collision with ring
            CheckRingCollision();

            // End ring attack
            if (m_ringActiveTime >= m_ringDuration)
            {
                EndRingAttack();
            }
        }

        private void CheckRingCollision()
        {
            if (m_playerTarget == null) return;

            float distanceToPlayer = GetDistanceToPlayer();
            float innerRadius = m_currentRingRadius - m_ringWidth;
            float outerRadius = m_currentRingRadius + m_ringWidth;

            // Check if player is within ring band
            if (distanceToPlayer >= innerRadius && distanceToPlayer <= outerRadius)
            {
                // Damage player
                PlayerHealth playerHealth = m_playerTarget.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(m_ringDamage, transform.position);
                }
            }
        }

        private void EndRingAttack()
        {
            m_isRingActive = false;

            if (m_ringVisual != null)
            {
                m_ringVisual.gameObject.SetActive(false);
            }
        }

        private void UpdatePhaseVisuals()
        {
            if (m_bodyRenderer != null)
            {
                m_bodyRenderer.color = GetPhaseColor();
            }
        }

        private Color GetPhaseColor()
        {
            switch (m_currentPhase)
            {
                case BossPhase.Phase1: return m_phase1Color;
                case BossPhase.Phase2: return m_phase2Color;
                case BossPhase.Phase3: return m_phase3Color;
                default: return m_phase1Color;
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
                    m_projectileSpeed * 1.5f,
                    m_projectileDamage,
                    m_deathBulletCount,
                    Color.red
                );
            }

            // Hide ring
            if (m_ringVisual != null)
            {
                m_ringVisual.gameObject.SetActive(false);
            }

            base.Kill();
        }

        private void DealDeathDamage()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, m_deathDamageRadius);

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(m_deathDamageAmount, transform.position);
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

            if (m_bodyRenderer == null) return;

            switch (newState)
            {
                case EnemyState.Spawning:
                    m_bodyRenderer.color = new Color(m_phase1Color.r, m_phase1Color.g, m_phase1Color.b, 0.5f);
                    break;
                case EnemyState.Alive:
                    UpdatePhaseVisuals();
                    break;
                case EnemyState.Dying:
                    m_bodyRenderer.color = Color.white;
                    break;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Death damage radius
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, m_deathDamageRadius);

            // Current ring radius (if active)
            if (m_isRingActive)
            {
                Gizmos.color = new Color(1f, 0f, 0.5f, 0.5f);
                Gizmos.DrawWireSphere(transform.position, m_currentRingRadius - m_ringWidth);
                Gizmos.DrawWireSphere(transform.position, m_currentRingRadius + m_ringWidth);
            }
        }
    }
}
