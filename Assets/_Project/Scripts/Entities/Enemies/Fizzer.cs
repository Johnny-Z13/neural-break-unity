using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Fizzer - Fast, erratic, high-speed chaos enemy.
    /// Very fast movement with unpredictable zigzag patterns.
    /// Based on TypeScript Fizzer.ts.
    ///
    /// Stats: HP=2, Speed=8.0 (VERY FAST), Damage=6, XP=15
    /// Burst Fire: 2 shots, 3.0s between bursts, 0.2s between shots
    /// Death Damage: 15 in 2.0 radius (electric explosion)
    /// </summary>
    public class Fizzer : EnemyBase
    {
        public override EnemyType EnemyType => EnemyType.Fizzer;

        [Header("Fizzer Settings")]
        [SerializeField] private float _directionChangeInterval = 0.15f;
        [SerializeField] private float _zigzagAmplitude = 3f;
        [SerializeField] private float _zigzagFrequency = 8f;

        [Header("Death Explosion")]
        [SerializeField] private float _deathDamageRadius = 2f;
        [SerializeField] private int _deathDamageAmount = 15;

        // Config-driven shooting values
        private float _burstCooldown => EnemyConfig?.fireRate ?? 3f;
        private int _burstCount => EnemyConfig?.burstCount ?? 2;
        private float _burstDelay => EnemyConfig?.burstDelay ?? 0.2f;
        private float _projectileSpeed => EnemyConfig?.projectileSpeed ?? 9f;
        private int _projectileDamage => EnemyConfig?.projectileDamage ?? 6;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private FizzerVisuals _visuals;
        [SerializeField] private Color _electricColor = new Color(0.2f, 0.8f, 1f); // Electric cyan-blue

        [Header("Vapor Trail")]
        [SerializeField] private bool _enableVaporTrail = true;
        private ParticleSystem _vaporParticles;

        // Note: MMFeedbacks removed

        // Movement state
        private Vector2 _currentDirection;
        private float _directionTimer;
        private float _zigzagOffset;
        private float _zigzagPhase;

        // Attack state
        private float _burstTimer;
        private bool _isFiringBurst;
        private bool _visualsGenerated;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _currentDirection = GetDirectionToPlayer();
            _directionTimer = 0f;
            _zigzagOffset = Random.Range(0f, Mathf.PI * 2f); // Random start phase
            _zigzagPhase = 0f;
            _burstTimer = _burstCooldown * Random.Range(0.3f, 0.7f); // Random initial delay
            _isFiringBurst = false;

            // Setup trail renderer with material and color
            if (_trailRenderer != null)
            {
                // Load trail material if not assigned
                if (_trailRenderer.sharedMaterial == null)
                {
                    var trailMaterial = Resources.Load<Material>("Materials/VFX/FizzerTrail");
                    if (trailMaterial != null)
                    {
                        _trailRenderer.sharedMaterial = trailMaterial;
                    }
                    else
                    {
                        Debug.LogWarning("[Fizzer] FizzerTrail material not found in Resources!");
                    }
                }

                _trailRenderer.startColor = _electricColor;
                _trailRenderer.endColor = new Color(_electricColor.r, _electricColor.g, _electricColor.b, 0f);
                _trailRenderer.time = 0.3f;
                _trailRenderer.widthMultiplier = 0.2f;
            }

            // Generate procedural visuals if not yet done
            if (!_visualsGenerated)
            {
                EnsureVisuals();
                _visualsGenerated = true;
            }

            // Setup vapor trail particles
            if (_enableVaporTrail)
            {
                EnsureVaporTrail();
            }
        }

        private void EnsureVaporTrail()
        {
            if (_vaporParticles != null) return;

            // Create vapor trail particle system
            var vaporGO = new GameObject("VaporTrail");
            vaporGO.transform.SetParent(transform, false);
            vaporGO.transform.localPosition = Vector3.zero;

            _vaporParticles = vaporGO.AddComponent<ParticleSystem>();

            var main = _vaporParticles.main;
            main.startLifetime = 0.4f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startColor = new Color(_electricColor.r, _electricColor.g, _electricColor.b, 0.4f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 50;

            var emission = _vaporParticles.emission;
            emission.rateOverTime = 30f;

            var shape = _vaporParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var colorOverLifetime = _vaporParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(_electricColor, 0f),
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(_electricColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.5f, 0f),
                    new GradientAlphaKey(0.3f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = _vaporParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0f)
            ));

            // Set up renderer with proper URP particle material
            var renderer = vaporGO.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Try to load existing particle material, or create one with proper shader
            var particleMat = Resources.Load<Material>("Materials/VFX/ParticleAdditive");
            if (particleMat != null)
            {
                renderer.material = particleMat;
            }
            else
            {
                // Fallback: try multiple shader names for URP compatibility
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                          ?? Shader.Find("Particles/Standard Unlit")
                          ?? Shader.Find("Sprites/Default");

                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.SetColor("_BaseColor", Color.white);
                    // Enable alpha blending
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.SetFloat("_Blend", 0);   // Alpha blend
                    mat.renderQueue = 3000;
                    renderer.material = mat;
                }
            }
            renderer.sortingOrder = -1;
        }

        private void EnsureVisuals()
        {
            if (_visuals == null)
            {
                _visuals = GetComponentInChildren<FizzerVisuals>();
            }

            if (_visuals == null)
            {
                var visualsGO = new GameObject("Visuals");
                visualsGO.transform.SetParent(transform, false);
                visualsGO.transform.localPosition = Vector3.zero;
                _visuals = visualsGO.AddComponent<FizzerVisuals>();
            }
        }

        protected override void UpdateAI()
        {
            UpdateMovement();
            UpdateAttack();
        }

        private void UpdateMovement()
        {
            // Change direction periodically for erratic behavior
            _directionTimer += Time.deltaTime;
            if (_directionTimer >= _directionChangeInterval)
            {
                UpdateDirection();
                _directionTimer = 0f;
            }

            // Calculate zigzag offset perpendicular to movement
            _zigzagPhase += Time.deltaTime * _zigzagFrequency;
            float zigzag = Mathf.Sin(_zigzagPhase + _zigzagOffset) * _zigzagAmplitude;

            // Perpendicular direction for zigzag
            Vector2 perpendicular = new Vector2(-_currentDirection.y, _currentDirection.x);

            // Combined movement: toward player + zigzag
            Vector2 movement = (_currentDirection * _speed + perpendicular * zigzag) * Time.deltaTime;
            transform.position = (Vector2)transform.position + movement;

            // Visual rotation based on velocity
            if (movement.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }
        }

        private void UpdateDirection()
        {
            // Mostly toward player, but with random variation
            Vector2 toPlayer = GetDirectionToPlayer();

            // Add random offset for unpredictability
            float randomAngle = Random.Range(-45f, 45f) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(randomAngle);
            float sin = Mathf.Sin(randomAngle);

            _currentDirection = new Vector2(
                toPlayer.x * cos - toPlayer.y * sin,
                toPlayer.x * sin + toPlayer.y * cos
            ).normalized;
        }

        private void UpdateAttack()
        {
            if (_isFiringBurst) return;

            _burstTimer += Time.deltaTime;
            if (_burstTimer >= _burstCooldown)
            {
                StartCoroutine(FireBurst());
                _burstTimer = 0f;
            }
        }

        private System.Collections.IEnumerator FireBurst()
        {
            _isFiringBurst = true;
            // Feedback (Feel removed)

            for (int i = 0; i < _burstCount; i++)
            {
                FireProjectile();

                if (i < _burstCount - 1)
                {
                    yield return new WaitForSeconds(_burstDelay);
                }
            }

            _isFiringBurst = false;
        }

        private void FireProjectile()
        {
            if (EnemyProjectilePool.Instance == null) return;

            Vector2 direction = GetDirectionToPlayer();
            Vector2 firePos = (Vector2)transform.position + direction * 0.3f;

            EnemyProjectilePool.Instance.Fire(
                firePos,
                direction,
                _projectileSpeed,
                _projectileDamage,
                _electricColor
            );
        }

        public override void Kill()
        {
            // Electric death explosion
            // Feedback (Feel removed)

            // Damage nearby enemies
            DealDeathDamage();

            base.Kill();
        }

        private void DealDeathDamage()
        {
            // Find all enemies in radius and damage them
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

            // Control vapor trail based on state
            if (_vaporParticles != null)
            {
                var emission = _vaporParticles.emission;
                emission.enabled = (newState == EnemyState.Alive);
            }

            if (_trailRenderer != null)
            {
                _trailRenderer.enabled = (newState == EnemyState.Alive);
            }

            if (_spriteRenderer == null) return;

            switch (newState)
            {
                case EnemyState.Spawning:
                    _spriteRenderer.color = new Color(_electricColor.r, _electricColor.g, _electricColor.b, 0.5f);
                    break;
                case EnemyState.Alive:
                    _spriteRenderer.color = _electricColor;
                    break;
                case EnemyState.Dying:
                    _spriteRenderer.color = Color.white;
                    break;
            }
        }

        public override void OnReturnToPool()
        {
            base.OnReturnToPool();

            // Clear vapor trail
            if (_vaporParticles != null)
            {
                _vaporParticles.Clear();
            }

            // Clear trail renderer
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Death damage radius
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, _deathDamageRadius);
        }
    }
}
