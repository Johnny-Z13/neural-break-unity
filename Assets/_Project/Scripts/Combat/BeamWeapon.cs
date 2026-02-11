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
        private List<EnemyBase> m_enemiesInBeam = new List<EnemyBase>(16);

        // Damage accumulator (to deal discrete damage chunks)
        private Dictionary<EnemyBase, float> m_damageAccumulator = new Dictionary<EnemyBase, float>();
        private List<EnemyBase> m_toRemoveBuffer = new List<EnemyBase>(16);  // Reusable buffer
        private List<EnemyBase> m_damageKeysBuffer = new List<EnemyBase>(16);  // Buffer for dict iteration

        // Static buffer for BoxCast (zero allocation)
        private static RaycastHit2D[] s_boxCastBuffer = new RaycastHit2D[32];
        private ContactFilter2D m_enemyContactFilter;

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

            // Setup contact filter using the enemy layer mask
            m_enemyContactFilter = new ContactFilter2D();
            m_enemyContactFilter.SetLayerMask(m_enemyLayer);
            m_enemyContactFilter.useLayerMask = true;
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

            // Box cast to find all enemies in beam (NonAlloc - zero GC)
            Vector2 direction = (end - start).normalized;
            float distance = Vector2.Distance(start, end);

            int hitCount = Physics2D.BoxCast(
                start,
                new Vector2(m_beamWidth, distance),
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
                direction,
                m_enemyContactFilter,
                s_boxCastBuffer,
                distance
            );

            for (int i = 0; i < hitCount; i++)
            {
                var enemy = s_boxCastBuffer[i].collider.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    m_enemiesInBeam.Add(enemy);
                }
            }
        }

        private void DealBeamDamage(float deltaTime)
        {
            // Indexed for loop on List (zero allocation - List has struct enumerator)
            for (int i = 0; i < m_enemiesInBeam.Count; i++)
            {
                var enemy = m_enemiesInBeam[i];
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

            // Clean up dead enemies from accumulator (zero allocation)
            // Copy dict keys to buffer first, then iterate buffer
            m_toRemoveBuffer.Clear();
            m_damageKeysBuffer.Clear();
            var enumerator = m_damageAccumulator.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Key == null || !enumerator.Current.Key.IsAlive)
                {
                    m_toRemoveBuffer.Add(enumerator.Current.Key);
                }
            }
            enumerator.Dispose();

            for (int i = 0; i < m_toRemoveBuffer.Count; i++)
            {
                m_damageAccumulator.Remove(m_toRemoveBuffer[i]);
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
