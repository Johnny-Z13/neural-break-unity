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
        [SerializeField] private float m_maxRange = 15f;
        [SerializeField] private float m_beamWidth = 0.3f;
        [SerializeField] private float m_damagePerSecond = 50f;
        [SerializeField] private LayerMask m_enemyLayer;

        [Header("Visual")]
        [SerializeField] private LineRenderer m_lineRenderer;
        [SerializeField] private ParticleSystem m_impactParticles;

        private bool m_isActive;
        private Vector2 m_beamDirection;
        private float m_damageMultiplier = 1f;
        private HashSet<EnemyBase> m_enemiesInBeam = new HashSet<EnemyBase>();

        // Damage accumulator (to deal discrete damage chunks)
        private Dictionary<EnemyBase, float> m_damageAccumulator = new Dictionary<EnemyBase, float>();

        private void Awake()
        {
            // Create line renderer if not assigned
            if (m_lineRenderer == null)
            {
                m_lineRenderer = gameObject.AddComponent<LineRenderer>();
                SetupLineRenderer();
            }

            m_isActive = false;
            m_lineRenderer.enabled = false;
        }

        private void SetupLineRenderer()
        {
            m_lineRenderer.startWidth = m_beamWidth;
            m_lineRenderer.endWidth = m_beamWidth * 0.5f;
            m_lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            m_lineRenderer.startColor = new Color(0.2f, 0.9f, 1f, 0.8f);
            m_lineRenderer.endColor = new Color(0.2f, 0.9f, 1f, 0.2f);
            m_lineRenderer.sortingOrder = 99;
            m_lineRenderer.numCornerVertices = 5;
            m_lineRenderer.numCapVertices = 5;
        }

        private void Update()
        {
            if (!m_isActive) return;

            UpdateBeam();
            DealBeamDamage(Time.deltaTime);
        }

        /// <summary>
        /// Activate beam in a direction.
        /// </summary>
        public void Fire(Vector2 origin, Vector2 direction, float damageMultiplier = 1f)
        {
            m_isActive = true;
            m_beamDirection = direction.normalized;
            m_damageMultiplier = damageMultiplier;

            m_lineRenderer.enabled = true;
            m_lineRenderer.SetPosition(0, origin);

            UpdateBeam();
        }

        /// <summary>
        /// Deactivate beam.
        /// </summary>
        public void Stop()
        {
            m_isActive = false;
            m_lineRenderer.enabled = false;
            m_enemiesInBeam.Clear();
            m_damageAccumulator.Clear();

            if (m_impactParticles != null)
            {
                m_impactParticles.Stop();
            }
        }

        private void UpdateBeam()
        {
            Vector2 origin = m_lineRenderer.GetPosition(0);

            // Raycast to find hit point
            RaycastHit2D hit = Physics2D.Raycast(origin, m_beamDirection, m_maxRange, m_enemyLayer);

            Vector2 endPoint;
            if (hit.collider != null)
            {
                endPoint = hit.point;
                ShowImpactEffect(hit.point);
            }
            else
            {
                endPoint = origin + m_beamDirection * m_maxRange;
                HideImpactEffect();
            }

            m_lineRenderer.SetPosition(1, endPoint);

            // Find all enemies in beam
            FindEnemiesInBeam(origin, endPoint);
        }

        private void FindEnemiesInBeam(Vector2 start, Vector2 end)
        {
            m_enemiesInBeam.Clear();

            // Box cast to find all enemies in beam
            Vector2 direction = (end - start).normalized;
            float distance = Vector2.Distance(start, end);

            RaycastHit2D[] hits = Physics2D.BoxCastAll(
                origin: start,
                size: new Vector2(m_beamWidth, distance),
                angle: Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
                direction: direction,
                distance: distance,
                layerMask: m_enemyLayer
            );

            foreach (var hit in hits)
            {
                var enemy = hit.collider.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    m_enemiesInBeam.Add(enemy);
                }
            }
        }

        private void DealBeamDamage(float deltaTime)
        {
            foreach (var enemy in m_enemiesInBeam)
            {
                if (enemy == null || !enemy.IsAlive) continue;

                // Accumulate damage
                float damageThisFrame = m_damagePerSecond * m_damageMultiplier * deltaTime;

                if (!m_damageAccumulator.ContainsKey(enemy))
                {
                    m_damageAccumulator[enemy] = 0f;
                }

                m_damageAccumulator[enemy] += damageThisFrame;

                // Deal damage in chunks of 1
                if (m_damageAccumulator[enemy] >= 1f)
                {
                    int damageToDeal = Mathf.FloorToInt(m_damageAccumulator[enemy]);
                    enemy.TakeDamage(damageToDeal, enemy.transform.position);
                    m_damageAccumulator[enemy] -= damageToDeal;
                }
            }

            // Clean up dead enemies from accumulator
            var toRemove = new List<EnemyBase>();
            foreach (var kvp in m_damageAccumulator)
            {
                if (kvp.Key == null || !kvp.Key.IsAlive)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var enemy in toRemove)
            {
                m_damageAccumulator.Remove(enemy);
            }
        }

        private void ShowImpactEffect(Vector2 position)
        {
            if (m_impactParticles != null)
            {
                m_impactParticles.transform.position = position;
                if (!m_impactParticles.isPlaying)
                {
                    m_impactParticles.Play();
                }
            }
        }

        private void HideImpactEffect()
        {
            if (m_impactParticles != null && m_impactParticles.isPlaying)
            {
                m_impactParticles.Stop();
            }
        }

        public bool IsActive => m_isActive;
    }
}
