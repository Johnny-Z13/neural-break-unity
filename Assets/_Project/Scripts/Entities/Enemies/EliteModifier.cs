using UnityEngine;
using System.Collections;
using NeuralBreak.Core;
using Z13.Core;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Elite modifier types that can be applied to enemies.
    /// </summary>
    public enum EliteType
    {
        None,
        Armored,    // Extra health, slower
        Swift,      // Faster movement
        Shielded,   // Regenerating shield
        Berserker,  // Enrages at low health
        Teleporter, // Blinks around
        Splitter    // Spawns mini copies on death
    }

    /// <summary>
    /// Component that modifies enemy behavior to make them elite variants.
    /// Attach to any EnemyBase to give them special abilities.
    /// </summary>
    public class EliteModifier : MonoBehaviour
    {
        [Header("Elite Settings")]
        [SerializeField] private EliteType m_eliteType = EliteType.None;
        [SerializeField] private bool m_randomizeOnSpawn = true;
        [SerializeField] private float m_eliteChance = 0.1f; // 10% chance to be elite

        [Header("Visual")]
        [SerializeField] private Color m_eliteGlowColor = new Color(1f, 0.8f, 0f); // Gold
        [SerializeField] private float m_glowPulseSpeed = 3f;

        // Type-specific settings
        [Header("Armored")]
        [SerializeField] private float m_armoredHealthMultiplier = 2f;
        [SerializeField] private float m_armoredSpeedMultiplier = 0.7f;

        [Header("Swift")]
        [SerializeField] private float m_swiftSpeedMultiplier = 1.8f;

        [Header("Shielded")]
        [SerializeField] private int m_shieldAmount = 3;
        [SerializeField] private float m_shieldRegenTime = 5f;
        [SerializeField] private Color m_shieldColor = new Color(0.3f, 0.8f, 1f, 0.5f);

        [Header("Berserker")]
        [SerializeField] private float m_berserkerThreshold = 0.3f; // Enrage at 30% health
        [SerializeField] private float m_berserkerSpeedMultiplier = 1.5f;
        [SerializeField] private float m_berserkerDamageMultiplier = 1.5f;
        [SerializeField] private Color m_berserkerColor = new Color(1f, 0.2f, 0.2f);

        [Header("Teleporter")]
        [SerializeField] private float m_teleportCooldown = 3f;
        [SerializeField] private float m_teleportRange = 5f;
        [SerializeField] private float m_teleportChance = 0.3f; // Chance to teleport when hit

        [Header("Splitter")]
        [SerializeField] private int m_splitCount = 2;

        // State
        private EnemyBase m_enemy;
        private SpriteRenderer m_spriteRenderer;
        private bool m_isElite;
        private int m_currentShield;
        private float m_shieldRegenTimer;
        private bool m_isBerserk;
        private float m_teleportTimer;
        private float m_originalSpeed;
        private int m_originalDamage;
        private GameObject m_shieldVisual;
        private GameObject m_eliteGlow;
        // Cached SpriteRenderers (avoids GetComponent per frame in Update)
        private SpriteRenderer m_eliteGlowSR;
        private SpriteRenderer m_shieldVisualSR;

        public bool IsElite => m_isElite;
        public EliteType Type => m_eliteType;

        private void Awake()
        {
            m_enemy = GetComponent<EnemyBase>();
            m_spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Initialize elite status. Call after enemy Initialize.
        /// </summary>
        public void InitializeElite()
        {
            if (m_randomizeOnSpawn)
            {
                // Random chance to become elite
                if (Random.value < m_eliteChance)
                {
                    // Random elite type
                    m_eliteType = (EliteType)Random.Range(1, System.Enum.GetValues(typeof(EliteType)).Length);
                    m_isElite = true;
                }
                else
                {
                    m_eliteType = EliteType.None;
                    m_isElite = false;
                }
            }
            else
            {
                m_isElite = m_eliteType != EliteType.None;
            }

            if (!m_isElite) return;

            CacheOriginalStats();
            ApplyEliteModifiers();
            CreateEliteVisuals();

            Debug.Log($"[EliteModifier] {m_enemy.EnemyType} became {m_eliteType} elite!");
        }

        private void CacheOriginalStats()
        {
            // Cache original values for berserker
            var speedField = typeof(EnemyBase).GetField("m_speed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var damageField = typeof(EnemyBase).GetField("m_damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (speedField != null) m_originalSpeed = (float)speedField.GetValue(m_enemy);
            if (damageField != null) m_originalDamage = (int)damageField.GetValue(m_enemy);
        }

        private void ApplyEliteModifiers()
        {
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var enemyType = typeof(EnemyBase);

            switch (m_eliteType)
            {
                case EliteType.Armored:
                    // Increase health, decrease speed
                    var healthField = enemyType.GetField("m_maxHealth", bindingFlags);
                    var currentHealthField = enemyType.GetField("m_currentHealth", bindingFlags);
                    var speedField = enemyType.GetField("m_speed", bindingFlags);

                    if (healthField != null)
                    {
                        int newHealth = Mathf.RoundToInt((int)healthField.GetValue(m_enemy) * m_armoredHealthMultiplier);
                        healthField.SetValue(m_enemy, newHealth);
                        currentHealthField?.SetValue(m_enemy, newHealth);
                    }
                    if (speedField != null)
                    {
                        speedField.SetValue(m_enemy, (float)speedField.GetValue(m_enemy) * m_armoredSpeedMultiplier);
                    }
                    break;

                case EliteType.Swift:
                    var swiftSpeedField = enemyType.GetField("m_speed", bindingFlags);
                    if (swiftSpeedField != null)
                    {
                        swiftSpeedField.SetValue(m_enemy, (float)swiftSpeedField.GetValue(m_enemy) * m_swiftSpeedMultiplier);
                    }
                    break;

                case EliteType.Shielded:
                    m_currentShield = m_shieldAmount;
                    m_shieldRegenTimer = 0f;
                    break;

                case EliteType.Berserker:
                    m_isBerserk = false;
                    break;

                case EliteType.Teleporter:
                    m_teleportTimer = m_teleportCooldown;
                    break;
            }
        }

        private void CreateEliteVisuals()
        {
            // Create glow effect
            m_eliteGlow = new GameObject("EliteGlow");
            m_eliteGlow.transform.SetParent(transform);
            m_eliteGlow.transform.localPosition = Vector3.zero;
            m_eliteGlow.transform.localScale = Vector3.one * 1.5f;

            m_eliteGlowSR = m_eliteGlow.AddComponent<SpriteRenderer>();
            m_eliteGlowSR.sprite = Graphics.SpriteGenerator.CreateGlow(64, m_eliteGlowColor, "EliteGlow");
            m_eliteGlowSR.color = new Color(m_eliteGlowColor.r, m_eliteGlowColor.g, m_eliteGlowColor.b, 0.3f);
            m_eliteGlowSR.sortingOrder = m_spriteRenderer != null ? m_spriteRenderer.sortingOrder - 1 : 0;

            // Shield visual for shielded elites
            if (m_eliteType == EliteType.Shielded)
            {
                m_shieldVisual = new GameObject("ShieldVisual");
                m_shieldVisual.transform.SetParent(transform);
                m_shieldVisual.transform.localPosition = Vector3.zero;
                m_shieldVisual.transform.localScale = Vector3.one * 1.3f;

                m_shieldVisualSR = m_shieldVisual.AddComponent<SpriteRenderer>();
                m_shieldVisualSR.sprite = Graphics.SpriteGenerator.CreateCircle(64, m_shieldColor, "Shield");
                m_shieldVisualSR.color = m_shieldColor;
                m_shieldVisualSR.sortingOrder = m_spriteRenderer != null ? m_spriteRenderer.sortingOrder + 1 : 1;
            }
        }

        private void Update()
        {
            if (!m_isElite || !m_enemy.IsAlive) return;

            UpdateEliteGlow();

            switch (m_eliteType)
            {
                case EliteType.Shielded:
                    UpdateShieldRegen();
                    break;

                case EliteType.Berserker:
                    UpdateBerserker();
                    break;

                case EliteType.Teleporter:
                    m_teleportTimer += Time.deltaTime;
                    break;
            }
        }

        private void UpdateEliteGlow()
        {
            if (m_eliteGlowSR == null) return;

            float pulse = 0.2f + Mathf.Sin(Time.time * m_glowPulseSpeed) * 0.1f;
            Color c = m_eliteGlowColor;
            c.a = pulse;
            m_eliteGlowSR.color = c;
        }

        private void UpdateShieldRegen()
        {
            if (m_currentShield >= m_shieldAmount) return;

            m_shieldRegenTimer += Time.deltaTime;
            if (m_shieldRegenTimer >= m_shieldRegenTime)
            {
                m_currentShield = Mathf.Min(m_currentShield + 1, m_shieldAmount);
                m_shieldRegenTimer = 0f;
                UpdateShieldVisual();
            }
        }

        private void UpdateShieldVisual()
        {
            if (m_shieldVisualSR == null) return;

            Color c = m_shieldColor;
            c.a = m_currentShield > 0 ? 0.5f : 0f;
            m_shieldVisualSR.color = c;
        }

        private void UpdateBerserker()
        {
            if (m_isBerserk) return;

            if (m_enemy.HealthPercent <= m_berserkerThreshold)
            {
                EnterBerserkMode();
            }
        }

        private void EnterBerserkMode()
        {
            m_isBerserk = true;

            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var enemyType = typeof(EnemyBase);

            // Boost speed and damage
            var speedField = enemyType.GetField("m_speed", bindingFlags);
            var damageField = enemyType.GetField("m_damage", bindingFlags);

            if (speedField != null)
            {
                speedField.SetValue(m_enemy, m_originalSpeed * m_berserkerSpeedMultiplier);
            }
            if (damageField != null)
            {
                damageField.SetValue(m_enemy, Mathf.RoundToInt(m_originalDamage * m_berserkerDamageMultiplier));
            }

            // Visual feedback
            if (m_spriteRenderer != null)
            {
                m_spriteRenderer.color = m_berserkerColor;
            }
            if (m_eliteGlowSR != null)
            {
                m_eliteGlowSR.color = m_berserkerColor;
            }

            Debug.Log($"[EliteModifier] {m_enemy.EnemyType} entered BERSERK mode!");
        }

        /// <summary>
        /// Called when enemy takes damage. Returns true if damage should be blocked.
        /// </summary>
        public bool OnTakeDamage(int damage, Vector2 source)
        {
            if (!m_isElite) return false;

            switch (m_eliteType)
            {
                case EliteType.Shielded:
                    if (m_currentShield > 0)
                    {
                        m_currentShield--;
                        m_shieldRegenTimer = 0f;
                        UpdateShieldVisual();
                        // Shield absorbs hit
                        return true;
                    }
                    break;

                case EliteType.Teleporter:
                    if (m_teleportTimer >= m_teleportCooldown && Random.value < m_teleportChance)
                    {
                        Teleport();
                        m_teleportTimer = 0f;
                    }
                    break;
            }

            return false;
        }

        private void Teleport()
        {
            // Find random position away from damage
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector2 newPos = (Vector2)transform.position + randomDir * m_teleportRange;

            // Visual effect at old position
            StartCoroutine(TeleportEffect(transform.position, newPos));

            transform.position = newPos;
        }

        private IEnumerator TeleportEffect(Vector2 from, Vector2 to)
        {
            // Simple flash effect
            if (m_spriteRenderer != null)
            {
                Color original = m_spriteRenderer.color;
                m_spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                m_spriteRenderer.color = original;
            }
        }

        /// <summary>
        /// Called when enemy dies. For splitter type.
        /// </summary>
        public void OnDeath()
        {
            if (!m_isElite) return;

            if (m_eliteType == EliteType.Splitter)
            {
                // Spawn mini versions (handled by spawner)
                EventBus.Publish(new EliteSplitEvent
                {
                    position = transform.position,
                    enemyType = m_enemy.EnemyType,
                    splitCount = m_splitCount
                });
            }

            // Cleanup visuals
            if (m_eliteGlow != null) Destroy(m_eliteGlow);
            if (m_shieldVisual != null) Destroy(m_shieldVisual);
        }

        public void Reset()
        {
            m_isElite = false;
            m_eliteType = EliteType.None;
            m_isBerserk = false;
            m_currentShield = 0;

            if (m_eliteGlow != null)
            {
                Destroy(m_eliteGlow);
                m_eliteGlow = null;
                m_eliteGlowSR = null;
            }
            if (m_shieldVisual != null)
            {
                Destroy(m_shieldVisual);
                m_shieldVisual = null;
                m_shieldVisualSR = null;
            }
        }
    }

    /// <summary>
    /// Event fired when a splitter elite dies
    /// </summary>
    public struct EliteSplitEvent
    {
        public Vector2 position;
        public EnemyType enemyType;
        public int splitCount;
    }
}
