using System.Collections.Generic;
using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using Z13.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// ChaosWorm - Large segmented serpent enemy.
    /// Undulating movement pattern with multiple body segments.
    /// Death triggers bullet spray from each segment.
    /// Based on TypeScript ChaosWorm.ts.
    ///
    /// Stats: HP=100, Speed=1.5, Damage=15, XP=35
    /// Segments: 12 body parts
    /// Death: 6 bullets per segment + 16 bullet nova from head
    /// </summary>
    public class ChaosWorm : EnemyBase
    {
        public override EnemyType EnemyType => EnemyType.ChaosWorm;

        [Header("Worm Settings")]
        [SerializeField] private int m_segmentCount = 12;
        [SerializeField] private float m_segmentSpacing = 0.6f;
        [SerializeField] private float m_undulationAmplitude = 2f;
        [SerializeField] private float m_undulationFrequency = 2f;
        [SerializeField] private float m_turnSpeed = 90f; // degrees per second

        [Header("Death Spray")]
        [SerializeField] private int m_bulletsPerSegment = 6;
        [SerializeField] private int m_finalNovaBullets = 16;
        [SerializeField] private float m_deathBulletSpeed = 8f;
        [SerializeField] private int m_deathBulletDamage = 15;
        [SerializeField] private float m_deathSprayDuration = 2f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer m_headRenderer;
        [SerializeField] private GameObject m_segmentPrefab;
        [SerializeField] private ChaosWormVisuals m_visuals;
        [SerializeField] private Color m_wormColor = new Color(0.8f, 0.2f, 0.5f); // Purple-pink

        // Note: MMFeedbacks removed

        // Segments
        private List<Transform> m_segments = new List<Transform>();
        private List<Vector2> m_positionHistory = new List<Vector2>();
        private int m_historyLength;

        // Movement
        private float m_currentAngle;
        private float m_undulationPhase;
        private float m_targetAngle;

        // Death animation
        private bool m_isDeathAnimating;
        private float m_deathTimer;
        private int m_currentDeathSegment;
        private bool m_visualsGenerated;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            m_currentAngle = Random.Range(0f, 360f);
            m_undulationPhase = 0f;
            m_isDeathAnimating = false;

            // Calculate history length needed
            m_historyLength = m_segmentCount * Mathf.CeilToInt(m_segmentSpacing / (m_speed * Time.fixedDeltaTime));
            m_historyLength = Mathf.Max(m_historyLength, m_segmentCount * 10);

            // Initialize position history with current position
            m_positionHistory.Clear();
            for (int i = 0; i < m_historyLength; i++)
            {
                m_positionHistory.Add(transform.position);
            }

            // Create segments
            CreateSegments();

            // Generate procedural visuals for head if not yet done
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
                m_visuals = GetComponentInChildren<ChaosWormVisuals>();
            }

            if (m_visuals == null)
            {
                var visualsGO = new GameObject("HeadVisuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                m_visuals = visualsGO.AddComponent<ChaosWormVisuals>();
            }
        }

        private void CreateSegments()
        {
            // Clear existing segments
            foreach (var seg in m_segments)
            {
                if (seg != null)
                {
                    Destroy(seg.gameObject);
                }
            }
            m_segments.Clear();

            // Create new segments (runtime creation if no prefab)
            for (int i = 0; i < m_segmentCount; i++)
            {
                GameObject seg;

                if (m_segmentPrefab != null)
                {
                    seg = Instantiate(m_segmentPrefab, transform.position, Quaternion.identity, transform.parent);
                }
                else
                {
                    // Create segment at runtime
                    seg = CreateRuntimeSegment();
                }

                seg.name = $"WormSegment_{i}";

                // Add WormSegment component to forward damage to parent
                var wormSegment = seg.GetComponent<WormSegment>();
                if (wormSegment == null)
                {
                    wormSegment = seg.AddComponent<WormSegment>();
                }
                wormSegment.Initialize(this);

                // Scale segments (smaller toward tail)
                float scale = 1f - (i * 0.05f);
                seg.transform.localScale = Vector3.one * Mathf.Max(scale, 0.4f);

                // Color gradient (rainbow effect like TS version)
                SpriteRenderer sr = seg.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float t = (float)i / m_segmentCount;
                    // Rainbow gradient from magenta through to red
                    Color segColor = Color.HSVToRGB(0.85f - t * 0.15f, 0.8f, 1f);
                    sr.color = segColor;
                }

                m_segments.Add(seg.transform);
            }
        }

        /// <summary>
        /// Create a worm segment at runtime with sprite and collider.
        /// </summary>
        private GameObject CreateRuntimeSegment()
        {
            var seg = new GameObject("WormSegment");
            seg.tag = "Enemy";

            // Add sprite renderer with circle sprite
            var sr = seg.AddComponent<SpriteRenderer>();
            sr.sprite = Graphics.SpriteGenerator.CreateCircle(32, m_wormColor, "WormSegmentSprite");
            sr.sortingOrder = 5;

            // Add collider for projectile detection
            var col = seg.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            return seg;
        }

        protected override void UpdateAI()
        {
            UpdateMovement();
            UpdateSegments();
        }

        /// <summary>
        /// Override dying state to handle our custom death animation
        /// </summary>
        protected override void UpdateDying()
        {
            if (m_isDeathAnimating)
            {
                UpdateDeathAnimation();
            }
            else
            {
                // Fallback to base behavior if not animating
                base.UpdateDying();
            }
        }

        private void UpdateMovement()
        {
            // Calculate target angle toward player
            Vector2 toPlayer = GetDirectionToPlayer();
            m_targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

            // Smoothly turn toward target
            float angleDiff = Mathf.DeltaAngle(m_currentAngle, m_targetAngle);
            float turnAmount = Mathf.Sign(angleDiff) * Mathf.Min(Mathf.Abs(angleDiff), m_turnSpeed * Time.deltaTime);
            m_currentAngle += turnAmount;

            // Add undulation
            m_undulationPhase += Time.deltaTime * m_undulationFrequency;
            float undulation = Mathf.Sin(m_undulationPhase) * m_undulationAmplitude;
            float finalAngle = m_currentAngle + undulation;

            // Move in current direction
            float rad = finalAngle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 newPos = (Vector2)transform.position + direction * m_speed * Time.deltaTime;
            transform.position = newPos;

            // Rotate head to face direction
            transform.rotation = Quaternion.Euler(0, 0, finalAngle - 90f);

            // Record position in history
            m_positionHistory.Insert(0, newPos);
            if (m_positionHistory.Count > m_historyLength)
            {
                m_positionHistory.RemoveAt(m_positionHistory.Count - 1);
            }
        }

        private void UpdateSegments()
        {
            for (int i = 0; i < m_segments.Count; i++)
            {
                if (m_segments[i] == null) continue;

                // Get position from history
                int historyIndex = Mathf.Min((i + 1) * 10, m_positionHistory.Count - 1);
                m_segments[i].position = m_positionHistory[historyIndex];

                // Rotate segment to face next position
                if (historyIndex > 0)
                {
                    Vector2 dir = m_positionHistory[historyIndex - 1] - m_positionHistory[historyIndex];
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                        m_segments[i].rotation = Quaternion.Euler(0, 0, angle - 90f);
                    }
                }
            }
        }

        public override void Kill()
        {
            // Prevent multiple kill calls
            if (m_isDeathAnimating || m_state == EnemyState.Dying || m_state == EnemyState.Dead) return;

            // Start death animation instead of immediate death
            m_isDeathAnimating = true;
            m_deathTimer = 0f;
            m_currentDeathSegment = m_segments.Count - 1; // Start from tail

            // Transition to Dying state so we stop taking damage
            // but we'll handle the animation ourselves
            SetState(EnemyState.Dying);

            // Publish kill event immediately (for scoring)
            EventBus.Publish(new EnemyKilledEvent
            {
                enemyType = EnemyType,
                position = transform.position,
                scoreValue = m_scoreValue,
                xpValue = m_xpValue
            });
        }

        private void UpdateDeathAnimation()
        {
            m_deathTimer += Time.deltaTime;
            float timePerSegment = m_deathSprayDuration / (m_segments.Count + 1);

            // Check if should destroy next segment
            int targetSegment = m_segments.Count - 1 - Mathf.FloorToInt(m_deathTimer / timePerSegment);

            while (m_currentDeathSegment >= targetSegment && m_currentDeathSegment >= 0)
            {
                DestroySegment(m_currentDeathSegment);
                m_currentDeathSegment--;
            }

            // Final head destruction
            if (m_deathTimer >= m_deathSprayDuration)
            {
                // Fire nova from head
                if (EnemyProjectilePool.Instance != null)
                {
                    EnemyProjectilePool.Instance.FireRing(
                        transform.position,
                        m_deathBulletSpeed,
                        m_deathBulletDamage,
                        m_finalNovaBullets,
                        Color.red
                    );
                }

                m_isDeathAnimating = false;

                // Finish dying - return to pool
                SetState(EnemyState.Dead);
                m_returnToPool?.Invoke(this);
            }
        }

        private void DestroySegment(int index)
        {
            if (index < 0 || index >= m_segments.Count) return;
            if (m_segments[index] == null) return;

            Vector2 segmentPos = m_segments[index].position;

            // Fire bullets from segment
            if (EnemyProjectilePool.Instance != null)
            {
                EnemyProjectilePool.Instance.FireRing(
                    segmentPos,
                    m_deathBulletSpeed,
                    m_deathBulletDamage,
                    m_bulletsPerSegment,
                    m_wormColor
                );
            }

            // Feedback (Feel removed)

            // Destroy segment
            Destroy(m_segments[index].gameObject);
            m_segments[index] = null;
        }

        public override void KillInstant()
        {
            // Clean up segments without animation
            foreach (var seg in m_segments)
            {
                if (seg != null)
                {
                    Destroy(seg.gameObject);
                }
            }
            m_segments.Clear();

            base.KillInstant();
        }

        public override void OnReturnToPool()
        {
            // Clean up segments
            foreach (var seg in m_segments)
            {
                if (seg != null)
                {
                    Destroy(seg.gameObject);
                }
            }
            m_segments.Clear();
            m_positionHistory.Clear();

            base.OnReturnToPool();
        }

        protected override void OnStateChanged(EnemyState newState)
        {
            base.OnStateChanged(newState);

            if (m_headRenderer == null) return;

            switch (newState)
            {
                case EnemyState.Spawning:
                    m_headRenderer.color = new Color(m_wormColor.r, m_wormColor.g, m_wormColor.b, 0.5f);
                    break;
                case EnemyState.Alive:
                    m_headRenderer.color = m_wormColor;
                    break;
                case EnemyState.Dying:
                    m_headRenderer.color = Color.white;
                    break;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Draw segment positions
            Gizmos.color = m_wormColor;
            for (int i = 0; i < m_segments.Count; i++)
            {
                if (m_segments[i] != null)
                {
                    Gizmos.DrawWireSphere(m_segments[i].position, 0.3f);
                }
            }
        }
    }
}
