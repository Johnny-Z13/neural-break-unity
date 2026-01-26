using System.Collections.Generic;
using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;
using MoreMountains.Feedbacks;

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
        [SerializeField] private int _shardCount = 6;
        [SerializeField] private float _orbitRadius = 1.5f;
        [SerializeField] private float _orbitSpeed = 1.5f; // rotations per second
        [SerializeField] private float _shardScale = 0.4f;

        [Header("Attack")]
        [SerializeField] private int _shardsPerBurst = 2;

        [Header("Death Explosion")]
        [SerializeField] private float _deathDamageRadius = 5f;
        [SerializeField] private int _deathDamageAmount = 30;

        // Config-driven shooting values
        private float _burstCooldown => EnemyConfig?.fireRate ?? 3.5f;
        private int _shotsPerShard => EnemyConfig?.burstCount ?? 2;
        private float _shotDelay => EnemyConfig?.burstDelay ?? 0.2f;
        private float _projectileSpeed => EnemyConfig?.projectileSpeed ?? 8f;
        private int _projectileDamage => EnemyConfig?.projectileDamage ?? 10;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _coreRenderer;
        [SerializeField] private GameObject _shardPrefab;
        [SerializeField] private CrystalShardVisuals _visuals;
        [SerializeField] private Color _crystalColor = new Color(0.4f, 0.8f, 1f); // Ice blue
        [SerializeField] private Color _coreColor = new Color(0.2f, 0.4f, 0.8f); // Darker blue

        [Header("Feel Feedbacks")]
        [SerializeField] private MMF_Player _burstFeedback;
        [SerializeField] private MMF_Player _shardFireFeedback;
        [SerializeField] private MMF_Player _shatterFeedback;

        // Shards
        private List<Transform> _shards = new List<Transform>();
        private List<SpriteRenderer> _shardRenderers = new List<SpriteRenderer>();

        // State
        private float _orbitAngle;
        private float _burstTimer;
        private bool _isFiring;
        private int _nextFiringShard;
        private bool _visualsGenerated;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _orbitAngle = Random.Range(0f, 360f);
            _burstTimer = _burstCooldown * Random.Range(0.3f, 0.6f);
            _isFiring = false;
            _nextFiringShard = 0;

            CreateShards();

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
                _visuals = GetComponentInChildren<CrystalShardVisuals>();
            }

            if (_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                _visuals = visualsGO.AddComponent<CrystalShardVisuals>();
            }
        }

        private void CreateShards()
        {
            // Clear existing shards
            foreach (var shard in _shards)
            {
                if (shard != null)
                {
                    Destroy(shard.gameObject);
                }
            }
            _shards.Clear();
            _shardRenderers.Clear();

            // Create new shards
            if (_shardPrefab != null)
            {
                float angleStep = 360f / _shardCount;

                for (int i = 0; i < _shardCount; i++)
                {
                    GameObject shard = Instantiate(_shardPrefab, transform);
                    shard.name = $"Shard_{i}";
                    shard.transform.localScale = Vector3.one * _shardScale;

                    SpriteRenderer sr = shard.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        // Slight color variation
                        float hueShift = (i * 0.05f);
                        Color shardColor = Color.Lerp(_crystalColor, Color.white, hueShift);
                        sr.color = shardColor;
                        _shardRenderers.Add(sr);
                    }

                    _shards.Add(shard.transform);
                }
            }
            else
            {
                // Create simple placeholder shards
                for (int i = 0; i < _shardCount; i++)
                {
                    GameObject shard = new GameObject($"Shard_{i}");
                    shard.transform.SetParent(transform);
                    shard.transform.localScale = Vector3.one * _shardScale;

                    SpriteRenderer sr = shard.AddComponent<SpriteRenderer>();
                    sr.sprite = _coreRenderer?.sprite;
                    sr.color = _crystalColor;
                    _shardRenderers.Add(sr);

                    _shards.Add(shard.transform);
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
            transform.position = (Vector2)transform.position + direction * _speed * Time.deltaTime;
        }

        private void UpdateOrbit()
        {
            // Rotate orbit angle
            _orbitAngle += _orbitSpeed * 360f * Time.deltaTime;
            if (_orbitAngle >= 360f) _orbitAngle -= 360f;

            // Position shards in orbit
            float angleStep = 360f / _shardCount;

            for (int i = 0; i < _shards.Count; i++)
            {
                if (_shards[i] == null) continue;

                float shardAngle = (_orbitAngle + (angleStep * i)) * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(shardAngle), Mathf.Sin(shardAngle)) * _orbitRadius;

                _shards[i].localPosition = offset;

                // Rotate shard to point outward
                _shards[i].localRotation = Quaternion.Euler(0, 0, shardAngle * Mathf.Rad2Deg);
            }
        }

        private void UpdateAttack()
        {
            if (_isFiring) return;

            _burstTimer += Time.deltaTime;
            if (_burstTimer >= _burstCooldown)
            {
                StartCoroutine(FireBurst());
                _burstTimer = 0f;
            }
        }

        private System.Collections.IEnumerator FireBurst()
        {
            _isFiring = true;
            _burstFeedback?.PlayFeedbacks();

            // Fire from multiple shards
            for (int s = 0; s < _shardsPerBurst; s++)
            {
                // Safety check: avoid divide by zero
                if (_shards.Count == 0) break;

                int shardIndex = (_nextFiringShard + s) % _shards.Count;

                // Fire multiple shots from this shard
                for (int shot = 0; shot < _shotsPerShard; shot++)
                {
                    FireFromShard(shardIndex);

                    if (shot < _shotsPerShard - 1)
                    {
                        yield return new WaitForSeconds(_shotDelay);
                    }
                }
            }

            _nextFiringShard = (_nextFiringShard + _shardsPerBurst) % _shards.Count;
            _isFiring = false;
        }

        private void FireFromShard(int shardIndex)
        {
            if (shardIndex < 0 || shardIndex >= _shards.Count) return;
            if (_shards[shardIndex] == null) return;
            if (EnemyProjectilePool.Instance == null) return;

            Vector2 shardPos = _shards[shardIndex].position;
            Vector2 direction = GetDirectionToPlayer();

            EnemyProjectilePool.Instance.Fire(
                shardPos,
                direction,
                _projectileSpeed,
                _projectileDamage,
                _crystalColor
            );

            _shardFireFeedback?.PlayFeedbacks();

            // Flash the shard
            if (shardIndex < _shardRenderers.Count && _shardRenderers[shardIndex] != null)
            {
                StartCoroutine(FlashShard(shardIndex));
            }
        }

        private System.Collections.IEnumerator FlashShard(int index)
        {
            if (index >= _shardRenderers.Count || _shardRenderers[index] == null) yield break;

            Color original = _shardRenderers[index].color;
            _shardRenderers[index].color = Color.white;

            yield return new WaitForSeconds(0.1f);

            if (index < _shardRenderers.Count && _shardRenderers[index] != null)
            {
                _shardRenderers[index].color = original;
            }
        }

        public override void Kill()
        {
            _shatterFeedback?.PlayFeedbacks();
            DealDeathDamage();

            // Fire shards outward
            if (EnemyProjectilePool.Instance != null)
            {
                for (int i = 0; i < _shards.Count; i++)
                {
                    if (_shards[i] != null)
                    {
                        Vector2 direction = (_shards[i].position - transform.position).normalized;
                        EnemyProjectilePool.Instance.Fire(
                            _shards[i].position,
                            direction,
                            _projectileSpeed * 1.5f,
                            _projectileDamage,
                            _crystalColor
                        );
                    }
                }
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
            foreach (var shard in _shards)
            {
                if (shard != null)
                {
                    Destroy(shard.gameObject);
                }
            }
            _shards.Clear();
            _shardRenderers.Clear();
        }

        protected override void OnStateChanged(EnemyState newState)
        {
            base.OnStateChanged(newState);

            if (_coreRenderer == null) return;

            switch (newState)
            {
                case EnemyState.Spawning:
                    _coreRenderer.color = new Color(_coreColor.r, _coreColor.g, _coreColor.b, 0.5f);
                    SetShardsAlpha(0.5f);
                    break;
                case EnemyState.Alive:
                    _coreRenderer.color = _coreColor;
                    SetShardsAlpha(1f);
                    break;
                case EnemyState.Dying:
                    _coreRenderer.color = Color.white;
                    break;
            }
        }

        private void SetShardsAlpha(float alpha)
        {
            foreach (var sr in _shardRenderers)
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
            Gizmos.color = _crystalColor;
            Gizmos.DrawWireSphere(transform.position, _orbitRadius);

            // Death damage radius
            Gizmos.color = new Color(_crystalColor.r, _crystalColor.g, _crystalColor.b, 0.2f);
            Gizmos.DrawSphere(transform.position, _deathDamageRadius);
        }
    }
}
