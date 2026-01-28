using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Entities;
using NeuralBreak.Config;
using System.Collections.Generic;

namespace NeuralBreak.Combat
{
    /// <summary>
    /// Continuous beam weapon that deals damage over time to enemies in a line.
    /// Activated when beam weapon upgrade is selected.
    /// </summary>
    public class BeamWeapon : MonoBehaviour
    {
        [Header("Beam Settings")]
        [SerializeField] private float _maxRange = 15f;
        [SerializeField] private float _beamWidth = 0.3f;
        [SerializeField] private float _damagePerSecond = 50f;
        [SerializeField] private LayerMask _enemyLayer;

        [Header("Visual")]
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private ParticleSystem _impactParticles;

        private bool _isActive;
        private Vector2 _beamDirection;
        private float _damageMultiplier = 1f;
        private HashSet<EnemyBase> _enemiesInBeam = new HashSet<EnemyBase>();

        // Damage accumulator (to deal discrete damage chunks)
        private Dictionary<EnemyBase, float> _damageAccumulator = new Dictionary<EnemyBase, float>();

        private void Awake()
        {
            // Create line renderer if not assigned
            if (_lineRenderer == null)
            {
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
                SetupLineRenderer();
            }

            _isActive = false;
            _lineRenderer.enabled = false;
        }

        private void SetupLineRenderer()
        {
            _lineRenderer.startWidth = _beamWidth;
            _lineRenderer.endWidth = _beamWidth * 0.5f;
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = new Color(0.2f, 0.9f, 1f, 0.8f);
            _lineRenderer.endColor = new Color(0.2f, 0.9f, 1f, 0.2f);
            _lineRenderer.sortingOrder = 99;
            _lineRenderer.numCornerVertices = 5;
            _lineRenderer.numCapVertices = 5;
        }

        private void Update()
        {
            if (!_isActive) return;

            UpdateBeam();
            DealBeamDamage(Time.deltaTime);
        }

        /// <summary>
        /// Activate beam in a direction.
        /// </summary>
        public void Fire(Vector2 origin, Vector2 direction, float damageMultiplier = 1f)
        {
            _isActive = true;
            _beamDirection = direction.normalized;
            _damageMultiplier = damageMultiplier;

            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(0, origin);

            UpdateBeam();
        }

        /// <summary>
        /// Deactivate beam.
        /// </summary>
        public void Stop()
        {
            _isActive = false;
            _lineRenderer.enabled = false;
            _enemiesInBeam.Clear();
            _damageAccumulator.Clear();

            if (_impactParticles != null)
            {
                _impactParticles.Stop();
            }
        }

        private void UpdateBeam()
        {
            Vector2 origin = _lineRenderer.GetPosition(0);

            // Raycast to find hit point
            RaycastHit2D hit = Physics2D.Raycast(origin, _beamDirection, _maxRange, _enemyLayer);

            Vector2 endPoint;
            if (hit.collider != null)
            {
                endPoint = hit.point;
                ShowImpactEffect(hit.point);
            }
            else
            {
                endPoint = origin + _beamDirection * _maxRange;
                HideImpactEffect();
            }

            _lineRenderer.SetPosition(1, endPoint);

            // Find all enemies in beam
            FindEnemiesInBeam(origin, endPoint);
        }

        private void FindEnemiesInBeam(Vector2 start, Vector2 end)
        {
            _enemiesInBeam.Clear();

            // Box cast to find all enemies in beam
            Vector2 direction = (end - start).normalized;
            float distance = Vector2.Distance(start, end);

            RaycastHit2D[] hits = Physics2D.BoxCastAll(
                origin: start,
                size: new Vector2(_beamWidth, distance),
                angle: Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
                direction: direction,
                distance: distance,
                layerMask: _enemyLayer
            );

            foreach (var hit in hits)
            {
                var enemy = hit.collider.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    _enemiesInBeam.Add(enemy);
                }
            }
        }

        private void DealBeamDamage(float deltaTime)
        {
            foreach (var enemy in _enemiesInBeam)
            {
                if (enemy == null || !enemy.IsAlive) continue;

                // Accumulate damage
                float damageThisFrame = _damagePerSecond * _damageMultiplier * deltaTime;

                if (!_damageAccumulator.ContainsKey(enemy))
                {
                    _damageAccumulator[enemy] = 0f;
                }

                _damageAccumulator[enemy] += damageThisFrame;

                // Deal damage in chunks of 1
                if (_damageAccumulator[enemy] >= 1f)
                {
                    int damageToDeal = Mathf.FloorToInt(_damageAccumulator[enemy]);
                    enemy.TakeDamage(damageToDeal, enemy.transform.position);
                    _damageAccumulator[enemy] -= damageToDeal;
                }
            }

            // Clean up dead enemies from accumulator
            var toRemove = new List<EnemyBase>();
            foreach (var kvp in _damageAccumulator)
            {
                if (kvp.Key == null || !kvp.Key.IsAlive)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var enemy in toRemove)
            {
                _damageAccumulator.Remove(enemy);
            }
        }

        private void ShowImpactEffect(Vector2 position)
        {
            if (_impactParticles != null)
            {
                _impactParticles.transform.position = position;
                if (!_impactParticles.isPlaying)
                {
                    _impactParticles.Play();
                }
            }
        }

        private void HideImpactEffect()
        {
            if (_impactParticles != null && _impactParticles.isPlaying)
            {
                _impactParticles.Stop();
            }
        }

        public bool IsActive => _isActive;
    }
}
