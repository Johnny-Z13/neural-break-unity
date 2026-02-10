using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

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
        [SerializeField] private float m_dashSpeed = 12f;
        [SerializeField] private float m_normalSpeed = 3f;
        [SerializeField] private float m_strafeSpeed = 6f;
        [SerializeField] private float m_preferredDistance = 7f;
        [SerializeField] private float m_minDistance = 4f;
        [SerializeField] private float m_maxDistance = 12f;

        [Header("Behavior Timings")]
        [SerializeField] private float m_dashCooldown = 2f;
        [SerializeField] private float m_dashDuration = 0.3f;
        [SerializeField] private float m_hoverDuration = 1.5f;
        [SerializeField] private float m_strafeDuration = 2f;

        [Header("Death Explosion")]
        [SerializeField] private float m_deathDamageRadius = 3f;
        [SerializeField] private int m_deathDamageAmount = 25;

        // Config-driven shooting values
        private float m_fireRate => EnemyConfig?.fireRate ?? 2f;
        private int m_burstCount => EnemyConfig?.burstCount ?? 3;
        private float m_burstDelay => EnemyConfig?.burstDelay ?? 0.15f;
        private float m_projectileSpeed => EnemyConfig?.projectileSpeed ?? 8f;
        private int m_projectileDamage => EnemyConfig?.projectileDamage ?? 14;

        [Header("Visual")]
        // m_spriteRenderer inherited from EnemyBase (protected field)
        [SerializeField] private Transform m_domeLight;
        [SerializeField] private UFOVisuals m_visuals;
        [SerializeField] private Color m_ufoColor = new Color(0.7f, 0.7f, 0.8f); // Silver
        [SerializeField] private Color m_domeColor = new Color(0.3f, 0.8f, 1f); // Cyan dome
        [SerializeField] private float m_wobbleSpeed = 3f;
        [SerializeField] private float m_wobbleAmount = 8f;
        [SerializeField] private float m_tiltAmount = 15f;

        // Note: MMFeedbacks removed

        // State
        private enum UFOState { Approaching, Hovering, Strafing, Dashing, FigureEight }
        private UFOState m_ufoState = UFOState.Approaching;

        private float m_fireTimer;
        private float m_dashCooldownTimer;
        private float m_figure8Phase;
        private float m_strafeDirection;
        private Vector2 m_dashTarget;
        private Vector2 m_lastPosition;
        private float m_wobblePhase;
        private float m_tiltPhase;
        private bool m_visualsGenerated;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            m_ufoState = UFOState.Approaching;
            m_fireTimer = m_fireRate * 0.5f;
            m_stateTimer = 0f;
            m_dashCooldownTimer = m_dashCooldown;
            m_figure8Phase = Random.Range(0f, Mathf.PI * 2f);
            m_strafeDirection = Random.value > 0.5f ? 1f : -1f;
            m_wobblePhase = Random.Range(0f, Mathf.PI * 2f);
            m_tiltPhase = 0f;
            m_lastPosition = transform.position;

            // Generate procedural visuals if not yet done
            if (!m_visualsGenerated)
            {
                EnsureVisuals();
                m_visualsGenerated = true;
            }
        }

        private void EnsureVisuals()
        {
            if (m_visuals == null)
            {
                m_visuals = GetComponentInChildren<UFOVisuals>();
            }

            if (m_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                m_visuals = visualsGO.AddComponent<UFOVisuals>();
            }
        }

        protected override void UpdateAI()
        {
            float distanceToPlayer = GetDistanceToPlayer();
            m_lastPosition = transform.position;

            // Update dash cooldown
            m_dashCooldownTimer -= Time.deltaTime;

            // State machine
            switch (m_ufoState)
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
            if (distanceToPlayer > m_preferredDistance + 2f)
            {
                Vector2 direction = GetDirectionToPlayer();
                transform.position = (Vector2)transform.position + direction * m_normalSpeed * Time.deltaTime;
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
                m_ufoState = UFOState.Hovering;
                m_stateTimer = m_hoverDuration;
                // Feedback (Feel removed)
            }
            else if (roll < 0.6f)
            {
                // Strafe around player
                m_ufoState = UFOState.Strafing;
                m_stateTimer = m_strafeDuration;
                m_strafeDirection = Random.value > 0.5f ? 1f : -1f;
            }
            else
            {
                // Figure-8 pattern
                m_ufoState = UFOState.FigureEight;
                m_stateTimer = 4f; // Full figure-8 cycle
                m_figure8Phase = 0f;
            }
        }

        private void UpdateHovering(float distanceToPlayer)
        {
            m_stateTimer -= Time.deltaTime;

            // Gentle floating motion
            float hoverX = Mathf.Sin(Time.time * 2f) * 0.5f;
            float hoverY = Mathf.Cos(Time.time * 1.5f) * 0.3f;
            Vector2 hover = new Vector2(hoverX, hoverY);

            transform.position = (Vector2)transform.position + hover * Time.deltaTime;

            // Maintain distance
            MaintainDistance(distanceToPlayer);

            // Try to dash if cooldown ready
            if (m_dashCooldownTimer <= 0 && Random.value < 0.03f)
            {
                StartDash();
                return;
            }

            if (m_stateTimer <= 0)
            {
                ChooseNextBehavior();
            }
        }

        private void UpdateStrafing(float distanceToPlayer)
        {
            m_stateTimer -= Time.deltaTime;

            // Circle-strafe around player
            Vector2 toPlayer = GetDirectionToPlayer();
            Vector2 perpendicular = new Vector2(-toPlayer.y, toPlayer.x) * m_strafeDirection;

            // Move perpendicular to player
            Vector2 movement = perpendicular * m_strafeSpeed * Time.deltaTime;

            // Also maintain distance
            float distanceError = distanceToPlayer - m_preferredDistance;
            movement += toPlayer * distanceError * 2f * Time.deltaTime;

            transform.position = (Vector2)transform.position + movement;

            // Occasionally reverse direction
            if (Random.value < 0.01f)
            {
                m_strafeDirection *= -1f;
            }

            // Try to dash
            if (m_dashCooldownTimer <= 0 && Random.value < 0.02f)
            {
                StartDash();
                return;
            }

            if (m_stateTimer <= 0)
            {
                ChooseNextBehavior();
            }
        }

        private void StartDash()
        {
            m_ufoState = UFOState.Dashing;
            m_stateTimer = m_dashDuration;
            m_dashCooldownTimer = m_dashCooldown;

            // Pick a random dash target - either toward player, away, or to the side
            float roll = Random.value;
            Vector2 playerPos = m_playerTarget != null ? (Vector2)m_playerTarget.position : Vector2.zero;

            if (roll < 0.3f)
            {
                // Dash toward player (aggressive)
                m_dashTarget = playerPos + GetDirectionToPlayer() * -m_minDistance;
            }
            else if (roll < 0.6f)
            {
                // Dash away (evasive)
                m_dashTarget = (Vector2)transform.position - GetDirectionToPlayer() * 6f;
            }
            else
            {
                // Dash to random position around player
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(m_minDistance, m_maxDistance);
                m_dashTarget = playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            }

            // Feedback (Feel removed)
        }

        private void UpdateDashing()
        {
            m_stateTimer -= Time.deltaTime;

            // Quick teleport-like dash
            Vector2 direction = (m_dashTarget - (Vector2)transform.position).normalized;
            transform.position = (Vector2)transform.position + direction * m_dashSpeed * Time.deltaTime;

            // Check if reached target or time expired
            if (m_stateTimer <= 0 || Vector2.Distance(transform.position, m_dashTarget) < 0.5f)
            {
                ChooseNextBehavior();
            }
        }

        private void UpdateFigureEight(float distanceToPlayer)
        {
            m_stateTimer -= Time.deltaTime;
            m_figure8Phase += Time.deltaTime * 1.5f;

            // Figure-8 (lemniscate) parametric equations
            float t = m_figure8Phase;
            float scale = 4f;
            float x = scale * Mathf.Cos(t) / (1f + Mathf.Sin(t) * Mathf.Sin(t));
            float y = scale * Mathf.Sin(t) * Mathf.Cos(t) / (1f + Mathf.Sin(t) * Mathf.Sin(t));

            // Offset from player position
            Vector2 playerPos = m_playerTarget != null ? (Vector2)m_playerTarget.position : Vector2.zero;
            Vector2 targetPos = playerPos + new Vector2(x, y) + Vector2.up * m_preferredDistance * 0.5f;

            // Smooth movement to target
            transform.position = Vector2.Lerp(transform.position, targetPos, Time.deltaTime * 3f);

            // Try to dash
            if (m_dashCooldownTimer <= 0 && Random.value < 0.015f)
            {
                StartDash();
                return;
            }

            if (m_stateTimer <= 0)
            {
                ChooseNextBehavior();
            }
        }

        private void MaintainDistance(float distanceToPlayer)
        {
            if (distanceToPlayer < m_minDistance)
            {
                // Too close, move away
                Vector2 away = -GetDirectionToPlayer();
                transform.position = (Vector2)transform.position + away * m_normalSpeed * Time.deltaTime;
            }
            else if (distanceToPlayer > m_maxDistance)
            {
                // Too far, move closer
                Vector2 toward = GetDirectionToPlayer();
                transform.position = (Vector2)transform.position + toward * m_normalSpeed * Time.deltaTime;
            }
        }

        private void UpdateFiring()
        {
            m_fireTimer += Time.deltaTime;

            if (m_fireTimer >= m_fireRate)
            {
                // Fire a burst
                StartCoroutine(FireBurst());
                m_fireTimer = 0f;
            }
        }

        private System.Collections.IEnumerator FireBurst()
        {
            for (int i = 0; i < m_burstCount; i++)
            {
                FireAtPlayer();
                if (i < m_burstCount - 1)
                {
                    yield return new WaitForSeconds(m_burstDelay);
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
                m_projectileSpeed,
                m_projectileDamage,
                m_domeColor
            );

            // Feedback (Feel removed)
        }

        private void UpdateVisuals()
        {
            m_wobblePhase += Time.deltaTime * m_wobbleSpeed;

            // Calculate movement direction for tilt (with safety check)
            if (Time.deltaTime > 0.0001f)
            {
                Vector2 velocity = ((Vector2)transform.position - m_lastPosition) / Time.deltaTime;
                float targetTilt = Mathf.Clamp(-velocity.x * m_tiltAmount, -45f, 45f); // Clamp to reasonable angle
                m_tiltPhase = Mathf.Lerp(m_tiltPhase, targetTilt, Time.deltaTime * 5f);
            }

            // Combine wobble and tilt
            float wobble = Mathf.Sin(m_wobblePhase) * m_wobbleAmount;
            float finalAngle = Mathf.Clamp(wobble + m_tiltPhase, -90f, 90f); // Clamp final angle
            transform.rotation = Quaternion.Euler(0, 0, finalAngle);
        }

        public override void Kill()
        {
            // Deal death damage
            DealDeathDamage();
            base.Kill();
        }

        // Cached array for overlap checks (zero allocation)
        private static Collider2D[] s_hitBuffer = new Collider2D[32];
        private static readonly ContactFilter2D s_noFilter = ContactFilter2D.noFilter;

        private void DealDeathDamage()
        {
            int hitCount = Physics2D.OverlapCircle(transform.position, m_deathDamageRadius, s_noFilter, s_hitBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                if (s_hitBuffer[i].gameObject == gameObject) continue;

                EnemyBase enemy = s_hitBuffer[i].GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(m_deathDamageAmount, transform.position);
                }
            }
        }

        protected override void OnStateChanged(EnemyState newState)
        {
            base.OnStateChanged(newState);

            if (m_spriteRenderer == null) return;

            switch (newState)
            {
                case EnemyState.Spawning:
                    m_spriteRenderer.color = new Color(m_ufoColor.r, m_ufoColor.g, m_ufoColor.b, 0.5f);
                    break;
                case EnemyState.Alive:
                    m_spriteRenderer.color = m_ufoColor;
                    break;
                case EnemyState.Dying:
                    m_spriteRenderer.color = Color.white;
                    break;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Preferred distance
            Gizmos.color = Color.cyan;
            if (m_playerTarget != null)
            {
                Gizmos.DrawWireSphere(m_playerTarget.position, m_preferredDistance);
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(m_playerTarget.position, m_minDistance);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(m_playerTarget.position, m_maxDistance);
            }

            // Death damage radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, m_deathDamageRadius);

            // Dash target
            if (m_ufoState == UFOState.Dashing)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, m_dashTarget);
                Gizmos.DrawWireSphere(m_dashTarget, 0.5f);
            }
        }
    }
}
