using System.Collections.Generic;
using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// CrystalShardSwarm - Orbital attacker with rotating crystal shards.
    /// Central body with multiple orbiting crystals that fire at player.
    /// Based on TypeScript CrystalShardSwarm.ts.
    ///
    /// Stats: HP=250, Speed=1.8, Damage=25, XP=45
    /// Shards: 6 orbiting crystals, Orbit Speed: 1.5 rotation/sec
    /// Burst Fire: 2 shots from 2 shards, 3.5s between bursts
    /// Death Damage: 30 in 5.0 radius
    /// </summary>
    public class CrystalShard : EnemyBase
    {
        public override EnemyType EnemyType => EnemyType.CrystalShard;

        [Header("Crystal Settings")]
        [SerializeField] private int m_shardCount = 6;
        [SerializeField] private float m_orbitRadius = 1.5f;
        [SerializeField] private float m_orbitSpeed = 1.5f; // rotations per second
        [SerializeField] private float m_shardScale = 0.4f;

        [Header("Attack")]
        [SerializeField] private int m_shardsPerBurst = 2;

        [Header("Death Explosion")]
        [SerializeField] private float m_deathDamageRadius = 5f;
        [SerializeField] private int m_deathDamageAmount = 30;

        // Config-driven shooting values
        private float m_burstCooldown => EnemyConfig?.fireRate ?? 3.5f;
        private int m_shotsPerShard => EnemyConfig?.burstCount ?? 2;
        private float m_shotDelay => EnemyConfig?.burstDelay ?? 0.2f;
        private float m_projectileSpeed => EnemyConfig?.projectileSpeed ?? 8f;
        private int m_projectileDamage => EnemyConfig?.projectileDamage ?? 10;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer m_coreRenderer;
        [SerializeField] private GameObject m_shardPrefab;
        [SerializeField] private CrystalShardVisuals m_visuals;
        [SerializeField] private Color m_crystalColor = new Color(0.4f, 0.8f, 1f); // Ice blue
        [SerializeField] private Color m_coreColor = new Color(0.2f, 0.4f, 0.8f); // Darker blue

        // Note: MMFeedbacks removed

        // Shards
        private List<Transform> m_shards = new List<Transform>();
        private List<SpriteRenderer> m_shardRenderers = new List<SpriteRenderer>();

        // State
        private float m_orbitAngle;
        private float m_burstTimer;
        private bool m_isFiring;
        private int m_nextFiringShard;
        private bool m_visualsGenerated;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            m_orbitAngle = Random.Range(0f, 360f);
            m_burstTimer = m_burstCooldown * Random.Range(0.3f, 0.6f);
            m_isFiring = false;
            m_nextFiringShard = 0;

            CreateShards();

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
                m_visuals = GetComponentInChildren<CrystalShardVisuals>();
            }

            if (m_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                m_visuals = visualsGO.AddComponent<CrystalShardVisuals>();
            }
        }

        private void CreateShards()
        {
            // Clear existing shards
            foreach (var shard in m_shards)
            {
                if (shard != null)
                {
                    Destroy(shard.gameObject);
                }
            }
            m_shards.Clear();
            m_shardRenderers.Clear();

            // Create new shards
            if (m_shardPrefab != null)
            {
                float angleStep = 360f / m_shardCount;

                for (int i = 0; i < m_shardCount; i++)
                {
                    GameObject shard = Instantiate(m_shardPrefab, transform);
                    shard.name = $"Shard_{i}";
                    shard.transform.localScale = Vector3.one * m_shardScale;

                    SpriteRenderer sr = shard.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        // Slight color variation
                        float hueShift = (i * 0.05f);
                        Color shardColor = Color.Lerp(m_crystalColor, Color.white, hueShift);
                        sr.color = shardColor;
                        m_shardRenderers.Add(sr);
                    }

                    m_shards.Add(shard.transform);
                }
            }
            else
            {
                // Create simple placeholder shards - visuals handled by CrystalShardVisuals
                // We just need transform references for attack positions
                for (int i = 0; i < m_shardCount; i++)
                {
                    GameObject shard = new GameObject($"Shard_{i}");
                    shard.transform.SetParent(transform);
                    shard.transform.localScale = Vector3.one * m_shardScale;

                    // Create a simple diamond sprite if no prefab
                    SpriteRenderer sr = shard.AddComponent<SpriteRenderer>();
                    sr.sprite = Graphics.SpriteGenerator.CreateDiamond(32, m_crystalColor, $"CrystalShard_{i}");
                    sr.color = m_crystalColor;
                    m_shardRenderers.Add(sr);

                    m_shards.Add(shard.transform);
                }
            }
        }

        protected override void UpdateAI()
        {
            UpdateMovement();
            UpdateOrbit();
            UpdateAttack();
        }

        private void UpdateMovement()
        {
            // Move toward player
            Vector2 direction = GetDirectionToPlayer();
            transform.position = (Vector2)transform.position + direction * m_speed * Time.deltaTime;
        }

        private void UpdateOrbit()
        {
            // Rotate orbit angle
            m_orbitAngle += m_orbitSpeed * 360f * Time.deltaTime;
            if (m_orbitAngle >= 360f) m_orbitAngle -= 360f;

            // Position shards in orbit
            float angleStep = 360f / m_shardCount;

            for (int i = 0; i < m_shards.Count; i++)
            {
                if (m_shards[i] == null) continue;

                float shardAngle = (m_orbitAngle + (angleStep * i)) * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(shardAngle), Mathf.Sin(shardAngle)) * m_orbitRadius;

                m_shards[i].localPosition = offset;

                // Rotate shard to point outward
                m_shards[i].localRotation = Quaternion.Euler(0, 0, shardAngle * Mathf.Rad2Deg);
            }
        }

        private void UpdateAttack()
        {
            if (m_isFiring) return;

            m_burstTimer += Time.deltaTime;
            if (m_burstTimer >= m_burstCooldown)
            {
                StartCoroutine(FireBurst());
                m_burstTimer = 0f;
            }
        }

        private System.Collections.IEnumerator FireBurst()
        {
            m_isFiring = true;
            // Feedback (Feel removed)

            // Fire from multiple shards
            for (int s = 0; s < m_shardsPerBurst; s++)
            {
                // Safety check: avoid divide by zero
                if (m_shards.Count == 0) break;

                int shardIndex = (m_nextFiringShard + s) % m_shards.Count;

                // Fire multiple shots from this shard
                for (int shot = 0; shot < m_shotsPerShard; shot++)
                {
                    FireFromShard(shardIndex);

                    if (shot < m_shotsPerShard - 1)
                    {
                        yield return new WaitForSeconds(m_shotDelay);
                    }
                }
            }

            if (m_shards.Count > 0)
            {
                m_nextFiringShard = (m_nextFiringShard + m_shardsPerBurst) % m_shards.Count;
            }
            m_isFiring = false;
        }

        private void FireFromShard(int shardIndex)
        {
            if (shardIndex < 0 || shardIndex >= m_shards.Count) return;
            if (m_shards[shardIndex] == null) return;
            if (EnemyProjectilePool.Instance == null) return;

            Vector2 shardPos = m_shards[shardIndex].position;
            Vector2 direction = GetDirectionToPlayer();

            EnemyProjectilePool.Instance.Fire(
                shardPos,
                direction,
                m_projectileSpeed,
                m_projectileDamage,
                m_crystalColor
            );

            // Feedback (Feel removed)

            // Flash the shard
            if (shardIndex < m_shardRenderers.Count && m_shardRenderers[shardIndex] != null)
            {
                StartCoroutine(FlashShard(shardIndex));
            }
        }

        private System.Collections.IEnumerator FlashShard(int index)
        {
            if (index >= m_shardRenderers.Count || m_shardRenderers[index] == null) yield break;

            Color original = m_shardRenderers[index].color;
            m_shardRenderers[index].color = Color.white;

            yield return new WaitForSeconds(0.1f);

            if (index < m_shardRenderers.Count && m_shardRenderers[index] != null)
            {
                m_shardRenderers[index].color = original;
            }
        }

        public override void Kill()
        {
            // Feedback (Feel removed)
            DealDeathDamage();

            // Fire shards outward
            if (EnemyProjectilePool.Instance != null)
            {
                for (int i = 0; i < m_shards.Count; i++)
                {
                    if (m_shards[i] != null)
                    {
                        Vector2 direction = (m_shards[i].position - transform.position).normalized;
                        EnemyProjectilePool.Instance.Fire(
                            m_shards[i].position,
                            direction,
                            m_projectileSpeed * 1.5f,
                            m_projectileDamage,
                            m_crystalColor
                        );
                    }
                }
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

        public override void KillInstant()
        {
            CleanupShards();
            base.KillInstant();
        }

        public override void OnReturnToPool()
        {
            CleanupShards();
            base.OnReturnToPool();
        }

        private void CleanupShards()
        {
            foreach (var shard in m_shards)
            {
                if (shard != null)
                {
                    Destroy(shard.gameObject);
                }
            }
            m_shards.Clear();
            m_shardRenderers.Clear();
        }

        protected override void OnStateChanged(EnemyState newState)
        {
            base.OnStateChanged(newState);

            switch (newState)
            {
                case EnemyState.Spawning:
                    if (m_coreRenderer != null)
                        m_coreRenderer.color = new Color(m_coreColor.r, m_coreColor.g, m_coreColor.b, 0.5f);
                    SetShardsAlpha(0.5f);
                    break;
                case EnemyState.Alive:
                    if (m_coreRenderer != null)
                        m_coreRenderer.color = m_coreColor;
                    SetShardsAlpha(1f);
                    break;
                case EnemyState.Dying:
                    if (m_coreRenderer != null)
                        m_coreRenderer.color = Color.white;
                    break;
            }
        }

        private void SetShardsAlpha(float alpha)
        {
            foreach (var sr in m_shardRenderers)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = alpha;
                    sr.color = c;
                }
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Orbit radius
            Gizmos.color = m_crystalColor;
            Gizmos.DrawWireSphere(transform.position, m_orbitRadius);

            // Death damage radius
            Gizmos.color = new Color(m_crystalColor.r, m_crystalColor.g, m_crystalColor.b, 0.2f);
            Gizmos.DrawSphere(transform.position, m_deathDamageRadius);
        }
    }
}
