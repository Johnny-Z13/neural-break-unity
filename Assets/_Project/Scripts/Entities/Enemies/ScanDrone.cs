using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

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
        [SerializeField] private float m_detectionRange = 15f;
        [SerializeField] private float m_fireRange = 12f;

        [Header("Patrol Settings")]
        [SerializeField] private float m_patrolRadius = 10f;
        [SerializeField] private float m_patrolSpeed = 0.8f;
        [SerializeField] private float m_chaseSpeedMultiplier = 1.5f;
        [SerializeField] private float m_chargeSpeedMultiplier = 2.5f;  // Aggressive charge when alerted
        [SerializeField] private float m_chargeDuration = 1.5f;  // How long to charge before attacking

        [Header("Alert Behavior")]
        [SerializeField] private Color m_normalColor = new Color(0.3f, 0.7f, 1f);  // Cyan-blue
        [SerializeField] private Color m_alertColor = Color.red;  // Red when charging
        private float m_chargeTimer;

        // Config-driven shooting values
        private float m_fireRate => EnemyConfig?.fireRate ?? 2f;
        private float m_projectileSpeed => EnemyConfig?.projectileSpeed ?? 7f;
        private int m_projectileDamage => EnemyConfig?.projectileDamage ?? 15;

        [Header("Visual")]
        // m_spriteRenderer inherited from EnemyBase (protected field)
        [SerializeField] private ScanDroneVisuals m_visuals;
#pragma warning disable CS0414 // Reserved for rotation animation feature
        [SerializeField] private float m_rotationSpeed = 90f; // degrees per second
#pragma warning restore CS0414

        // Note: MMFeedbacks removed

        // State
        private enum DroneState { Patrolling, Alerted, Attacking }
        private DroneState m_droneState = DroneState.Patrolling;

        private Vector2 m_patrolTarget;
        private float m_fireTimer;
        private float m_currentRotation;
        private bool m_wasPlayerInRange;
        private bool m_visualsGenerated;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            m_droneState = DroneState.Patrolling;
            m_patrolTarget = GetNewPatrolTarget();
            m_fireTimer = m_fireRate * 0.5f; // Start partially ready
            m_currentRotation = Random.Range(0f, 360f);
            m_wasPlayerInRange = false;
            m_chargeTimer = 0f;

            // Generate procedural visuals if not yet done
            if (!m_visualsGenerated)
            {
                EnsureVisuals();
                m_visualsGenerated = true;
            }
        }

        private void EnsureVisuals()
        {
            // Add ScanDroneVisuals component if not present
            if (m_visuals == null)
            {
                m_visuals = GetComponentInChildren<ScanDroneVisuals>();
            }

            if (m_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                m_visuals = visualsGO.AddComponent<ScanDroneVisuals>();
            }
        }

        protected override void UpdateAI()
        {
            float distanceToPlayer = GetDistanceToPlayer();
            bool playerInRange = distanceToPlayer < m_detectionRange;

            // State transitions
            switch (m_droneState)
            {
                case DroneState.Patrolling:
                    if (playerInRange)
                    {
                        m_droneState = DroneState.Alerted;
                        m_chargeTimer = 0f;  // Start charge timer
                        // Feedback (Feel removed)
                        m_visuals?.SetAlerted(true);

                        // Turn red when alerted!
                        if (m_spriteRenderer != null)
                        {
                            m_spriteRenderer.color = m_alertColor;
                        }

                        if (!m_wasPlayerInRange)
                        {
                            Debug.Log("[ScanDrone] Player detected! Charging!");
                        }
                    }
                    break;

                case DroneState.Alerted:
                    if (!playerInRange)
                    {
                        m_droneState = DroneState.Patrolling;
                        m_patrolTarget = GetNewPatrolTarget();
                        m_visuals?.SetAlerted(false);

                        // Return to normal color
                        if (m_spriteRenderer != null)
                        {
                            m_spriteRenderer.color = m_normalColor;
                        }
                    }
                    else
                    {
                        // Charge for duration, then attack
                        m_chargeTimer += Time.deltaTime;
                        if (m_chargeTimer >= m_chargeDuration || distanceToPlayer < m_fireRange)
                        {
                            m_droneState = DroneState.Attacking;
                        }
                    }
                    break;

                case DroneState.Attacking:
                    if (!playerInRange)
                    {
                        m_droneState = DroneState.Patrolling;
                        m_patrolTarget = GetNewPatrolTarget();
                        m_visuals?.SetAlerted(false);

                        // Return to normal color
                        if (m_spriteRenderer != null)
                        {
                            m_spriteRenderer.color = m_normalColor;
                        }
                    }
                    else if (distanceToPlayer > m_fireRange)
                    {
                        m_droneState = DroneState.Alerted;
                        m_chargeTimer = 0f;  // Reset charge timer

                        // Turn red again when re-alerted
                        if (m_spriteRenderer != null)
                        {
                            m_spriteRenderer.color = m_alertColor;
                        }
                    }
                    break;
            }

            m_wasPlayerInRange = playerInRange;

            // Behavior based on state
            switch (m_droneState)
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
            Vector2 direction = (m_patrolTarget - currentPos).normalized;

            transform.position = currentPos + direction * m_patrolSpeed * Time.deltaTime;

            // Check if reached patrol target
            if (Vector2.Distance(currentPos, m_patrolTarget) < 0.5f)
            {
                m_patrolTarget = GetNewPatrolTarget();
            }
        }

        private void UpdateChase()
        {
            // AGGRESSIVE CHARGE toward player when alerted (red state)
            Vector2 direction = GetDirectionToPlayer();
            float chargeSpeed = m_speed * m_chargeSpeedMultiplier;  // 2.5x speed (FAST!)

            transform.position = (Vector2)transform.position + direction * chargeSpeed * Time.deltaTime;
        }

        private void UpdateAttack()
        {
            // Slow movement while attacking
            Vector2 direction = GetDirectionToPlayer();
            transform.position = (Vector2)transform.position + direction * m_speed * 0.3f * Time.deltaTime;

            // Fire at player
            m_fireTimer += Time.deltaTime;
            if (m_fireTimer >= m_fireRate)
            {
                FireAtPlayer();
                m_fireTimer = 0f;
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
                m_projectileSpeed,
                m_projectileDamage,
                new Color(1f, 0.5f, 0f) // Orange
            );

            // Feedback (Feel removed)
        }

        private Vector2 GetNewPatrolTarget()
        {
            // Random point within patrol radius of spawn position
            Vector2 offset = Random.insideUnitCircle * m_patrolRadius;
            return (Vector2)transform.position + offset;
        }

        protected override void OnStateChanged(EnemyState newState)
        {
            base.OnStateChanged(newState);

            if (m_spriteRenderer == null) return;

            switch (newState)
            {
                case EnemyState.Spawning:
                    m_spriteRenderer.color = new Color(0.5f, 0.8f, 1f, 0.5f); // Light blue, transparent
                    break;
                case EnemyState.Alive:
                    // Start with normal color (will turn red when alerted)
                    m_spriteRenderer.color = m_normalColor;
                    break;
                case EnemyState.Dying:
                    m_spriteRenderer.color = Color.white;
                    break;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_detectionRange);

            // Fire range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, m_fireRange);

            // Patrol target
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, m_patrolTarget);
                Gizmos.DrawWireSphere(m_patrolTarget, 0.3f);
            }
        }
    }
}
