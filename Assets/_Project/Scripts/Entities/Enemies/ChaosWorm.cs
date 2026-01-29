using System.Collections.Generic;
using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

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
        [SerializeField] private int _segmentCount = 12;
        [SerializeField] private float _segmentSpacing = 0.6f;
        [SerializeField] private float _undulationAmplitude = 2f;
        [SerializeField] private float _undulationFrequency = 2f;
        [SerializeField] private float _turnSpeed = 90f; // degrees per second

        [Header("Death Spray")]
        [SerializeField] private int _bulletsPerSegment = 6;
        [SerializeField] private int _finalNovaBullets = 16;
        [SerializeField] private float _deathBulletSpeed = 8f;
        [SerializeField] private int _deathBulletDamage = 15;
        [SerializeField] private float _deathSprayDuration = 2f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _headRenderer;
        [SerializeField] private GameObject _segmentPrefab;
        [SerializeField] private ChaosWormVisuals _visuals;
        [SerializeField] private Color _wormColor = new Color(0.8f, 0.2f, 0.5f); // Purple-pink

        // Note: MMFeedbacks removed

        // Segments
        private List<Transform> _segments = new List<Transform>();
        private List<Vector2> _positionHistory = new List<Vector2>();
        private int _historyLength;

        // Movement
        private float _currentAngle;
        private float _undulationPhase;
        private float _targetAngle;

        // Death animation
        private bool _isDeathAnimating;
        private float _deathTimer;
        private int _currentDeathSegment;
        private bool _visualsGenerated;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _currentAngle = Random.Range(0f, 360f);
            _undulationPhase = 0f;
            _isDeathAnimating = false;

            // Calculate history length needed
            _historyLength = _segmentCount * Mathf.CeilToInt(_segmentSpacing / (_speed * Time.fixedDeltaTime));
            _historyLength = Mathf.Max(_historyLength, _segmentCount * 10);

            // Initialize position history with current position
            _positionHistory.Clear();
            for (int i = 0; i < _historyLength; i++)
            {
                _positionHistory.Add(transform.position);
            }

            // Create segments
            CreateSegments();

            // Generate procedural visuals for head if not yet done
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
                _visuals = GetComponentInChildren<ChaosWormVisuals>();
            }

            if (_visuals == null)
            {
                var visualsGO = new GameObject("HeadVisuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                _visuals = visualsGO.AddComponent<ChaosWormVisuals>();
            }
        }

        private void CreateSegments()
        {
            // Clear existing segments
            foreach (var seg in _segments)
            {
                if (seg != null)
                {
                    Destroy(seg.gameObject);
                }
            }
            _segments.Clear();

            // Create new segments (runtime creation if no prefab)
            for (int i = 0; i < _segmentCount; i++)
            {
                GameObject seg;

                if (_segmentPrefab != null)
                {
                    seg = Instantiate(_segmentPrefab, transform.position, Quaternion.identity, transform.parent);
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
                    float t = (float)i / _segmentCount;
                    // Rainbow gradient from magenta through to red
                    Color segColor = Color.HSVToRGB(0.85f - t * 0.15f, 0.8f, 1f);
                    sr.color = segColor;
                }

                _segments.Add(seg.transform);
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
            sr.sprite = Graphics.SpriteGenerator.CreateCircle(32, _wormColor, "WormSegmentSprite");
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
            if (_isDeathAnimating)
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
            _targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

            // Smoothly turn toward target
            float angleDiff = Mathf.DeltaAngle(_currentAngle, _targetAngle);
            float turnAmount = Mathf.Sign(angleDiff) * Mathf.Min(Mathf.Abs(angleDiff), _turnSpeed * Time.deltaTime);
            _currentAngle += turnAmount;

            // Add undulation
            _undulationPhase += Time.deltaTime * _undulationFrequency;
            float undulation = Mathf.Sin(_undulationPhase) * _undulationAmplitude;
            float finalAngle = _currentAngle + undulation;

            // Move in current direction
            float rad = finalAngle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 newPos = (Vector2)transform.position + direction * _speed * Time.deltaTime;
            transform.position = newPos;

            // Rotate head to face direction
            transform.rotation = Quaternion.Euler(0, 0, finalAngle - 90f);

            // Record position in history
            _positionHistory.Insert(0, newPos);
            if (_positionHistory.Count > _historyLength)
            {
                _positionHistory.RemoveAt(_positionHistory.Count - 1);
            }
        }

        private void UpdateSegments()
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i] == null) continue;

                // Get position from history
                int historyIndex = Mathf.Min((i + 1) * 10, _positionHistory.Count - 1);
                _segments[i].position = _positionHistory[historyIndex];

                // Rotate segment to face next position
                if (historyIndex > 0)
                {
                    Vector2 dir = _positionHistory[historyIndex - 1] - _positionHistory[historyIndex];
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                        _segments[i].rotation = Quaternion.Euler(0, 0, angle - 90f);
                    }
                }
            }
        }

        public override void Kill()
        {
            // Prevent multiple kill calls
            if (_isDeathAnimating || _state == EnemyState.Dying || _state == EnemyState.Dead) return;

            // Start death animation instead of immediate death
            _isDeathAnimating = true;
            _deathTimer = 0f;
            _currentDeathSegment = _segments.Count - 1; // Start from tail

            // Transition to Dying state so we stop taking damage
            // but we'll handle the animation ourselves
            SetState(EnemyState.Dying);

            // Publish kill event immediately (for scoring)
            EventBus.Publish(new EnemyKilledEvent
            {
                enemyType = EnemyType,
                position = transform.position,
                scoreValue = _scoreValue,
                xpValue = _xpValue
            });
        }

        private void UpdateDeathAnimation()
        {
            _deathTimer += Time.deltaTime;
            float timePerSegment = _deathSprayDuration / (_segments.Count + 1);

            // Check if should destroy next segment
            int targetSegment = _segments.Count - 1 - Mathf.FloorToInt(_deathTimer / timePerSegment);

            while (_currentDeathSegment >= targetSegment && _currentDeathSegment >= 0)
            {
                DestroySegment(_currentDeathSegment);
                _currentDeathSegment--;
            }

            // Final head destruction
            if (_deathTimer >= _deathSprayDuration)
            {
                // Fire nova from head
                if (EnemyProjectilePool.Instance != null)
                {
                    EnemyProjectilePool.Instance.FireRing(
                        transform.position,
                        _deathBulletSpeed,
                        _deathBulletDamage,
                        _finalNovaBullets,
                        Color.red
                    );
                }

                _isDeathAnimating = false;

                // Finish dying - return to pool
                SetState(EnemyState.Dead);
                _returnToPool?.Invoke(this);
            }
        }

        private void DestroySegment(int index)
        {
            if (index < 0 || index >= _segments.Count) return;
            if (_segments[index] == null) return;

            Vector2 segmentPos = _segments[index].position;

            // Fire bullets from segment
            if (EnemyProjectilePool.Instance != null)
            {
                EnemyProjectilePool.Instance.FireRing(
                    segmentPos,
                    _deathBulletSpeed,
                    _deathBulletDamage,
                    _bulletsPerSegment,
                    _wormColor
                );
            }

            // Feedback (Feel removed)

            // Destroy segment
            Destroy(_segments[index].gameObject);
            _segments[index] = null;
        }

        public override void KillInstant()
        {
            // Clean up segments without animation
            foreach (var seg in _segments)
            {
                if (seg != null)
                {
                    Destroy(seg.gameObject);
                }
            }
            _segments.Clear();

            base.KillInstant();
        }

        public override void OnReturnToPool()
        {
            // Clean up segments
            foreach (var seg in _segments)
            {
                if (seg != null)
                {
                    Destroy(seg.gameObject);
                }
            }
            _segments.Clear();
            _positionHistory.Clear();

            base.OnReturnToPool();
        }

        protected override void OnStateChanged(EnemyState newState)
        {
            base.OnStateChanged(newState);

            if (_headRenderer == null) return;

            switch (newState)
            {
                case EnemyState.Spawning:
                    _headRenderer.color = new Color(_wormColor.r, _wormColor.g, _wormColor.b, 0.5f);
                    break;
                case EnemyState.Alive:
                    _headRenderer.color = _wormColor;
                    break;
                case EnemyState.Dying:
                    _headRenderer.color = Color.white;
                    break;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Draw segment positions
            Gizmos.color = _wormColor;
            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i] != null)
                {
                    Gizmos.DrawWireSphere(_segments[i].position, 0.3f);
                }
            }
        }
    }
}
