using UnityEngine;
using System.Collections;
using NeuralBreak.Core;

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
        [SerializeField] private EliteType _eliteType = EliteType.None;
        [SerializeField] private bool _randomizeOnSpawn = true;
        [SerializeField] private float _eliteChance = 0.1f; // 10% chance to be elite

        [Header("Visual")]
        [SerializeField] private Color _eliteGlowColor = new Color(1f, 0.8f, 0f); // Gold
        [SerializeField] private float _glowPulseSpeed = 3f;

        // Type-specific settings
        [Header("Armored")]
        [SerializeField] private float _armoredHealthMultiplier = 2f;
        [SerializeField] private float _armoredSpeedMultiplier = 0.7f;

        [Header("Swift")]
        [SerializeField] private float _swiftSpeedMultiplier = 1.8f;

        [Header("Shielded")]
        [SerializeField] private int _shieldAmount = 3;
        [SerializeField] private float _shieldRegenTime = 5f;
        [SerializeField] private Color _shieldColor = new Color(0.3f, 0.8f, 1f, 0.5f);

        [Header("Berserker")]
        [SerializeField] private float _berserkerThreshold = 0.3f; // Enrage at 30% health
        [SerializeField] private float _berserkerSpeedMultiplier = 1.5f;
        [SerializeField] private float _berserkerDamageMultiplier = 1.5f;
        [SerializeField] private Color _berserkerColor = new Color(1f, 0.2f, 0.2f);

        [Header("Teleporter")]
        [SerializeField] private float _teleportCooldown = 3f;
        [SerializeField] private float _teleportRange = 5f;
        [SerializeField] private float _teleportChance = 0.3f; // Chance to teleport when hit

        [Header("Splitter")]
        [SerializeField] private int _splitCount = 2;

        // State
        private EnemyBase _enemy;
        private SpriteRenderer _spriteRenderer;
        private bool _isElite;
        private int _currentShield;
        private float _shieldRegenTimer;
        private bool _isBerserk;
        private float _teleportTimer;
        private float _originalSpeed;
        private int _originalDamage;
        private GameObject _shieldVisual;
        private GameObject _eliteGlow;

        public bool IsElite => _isElite;
        public EliteType Type => _eliteType;

        private void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Initialize elite status. Call after enemy Initialize.
        /// </summary>
        public void InitializeElite()
        {
            if (_randomizeOnSpawn)
            {
                // Random chance to become elite
                if (Random.value < _eliteChance)
                {
                    // Random elite type
                    _eliteType = (EliteType)Random.Range(1, System.Enum.GetValues(typeof(EliteType)).Length);
                    _isElite = true;
                }
                else
                {
                    _eliteType = EliteType.None;
                    _isElite = false;
                }
            }
            else
            {
                _isElite = _eliteType != EliteType.None;
            }

            if (!_isElite) return;

            CacheOriginalStats();
            ApplyEliteModifiers();
            CreateEliteVisuals();

            Debug.Log($"[EliteModifier] {_enemy.EnemyType} became {_eliteType} elite!");
        }

        private void CacheOriginalStats()
        {
            // Cache original values for berserker
            var speedField = typeof(EnemyBase).GetField("_speed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var damageField = typeof(EnemyBase).GetField("_damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (speedField != null) _originalSpeed = (float)speedField.GetValue(_enemy);
            if (damageField != null) _originalDamage = (int)damageField.GetValue(_enemy);
        }

        private void ApplyEliteModifiers()
        {
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var enemyType = typeof(EnemyBase);

            switch (_eliteType)
            {
                case EliteType.Armored:
                    // Increase health, decrease speed
                    var healthField = enemyType.GetField("_maxHealth", bindingFlags);
                    var currentHealthField = enemyType.GetField("_currentHealth", bindingFlags);
                    var speedField = enemyType.GetField("_speed", bindingFlags);

                    if (healthField != null)
                    {
                        int newHealth = Mathf.RoundToInt((int)healthField.GetValue(_enemy) * _armoredHealthMultiplier);
                        healthField.SetValue(_enemy, newHealth);
                        currentHealthField?.SetValue(_enemy, newHealth);
                    }
                    if (speedField != null)
                    {
                        speedField.SetValue(_enemy, (float)speedField.GetValue(_enemy) * _armoredSpeedMultiplier);
                    }
                    break;

                case EliteType.Swift:
                    var swiftSpeedField = enemyType.GetField("_speed", bindingFlags);
                    if (swiftSpeedField != null)
                    {
                        swiftSpeedField.SetValue(_enemy, (float)swiftSpeedField.GetValue(_enemy) * _swiftSpeedMultiplier);
                    }
                    break;

                case EliteType.Shielded:
                    _currentShield = _shieldAmount;
                    _shieldRegenTimer = 0f;
                    break;

                case EliteType.Berserker:
                    _isBerserk = false;
                    break;

                case EliteType.Teleporter:
                    _teleportTimer = _teleportCooldown;
                    break;
            }
        }

        private void CreateEliteVisuals()
        {
            // Create glow effect
            _eliteGlow = new GameObject("EliteGlow");
            _eliteGlow.transform.SetParent(transform);
            _eliteGlow.transform.localPosition = Vector3.zero;
            _eliteGlow.transform.localScale = Vector3.one * 1.5f;

            var glowSR = _eliteGlow.AddComponent<SpriteRenderer>();
            glowSR.sprite = Graphics.SpriteGenerator.CreateGlow(64, _eliteGlowColor, "EliteGlow");
            glowSR.color = new Color(_eliteGlowColor.r, _eliteGlowColor.g, _eliteGlowColor.b, 0.3f);
            glowSR.sortingOrder = _spriteRenderer != null ? _spriteRenderer.sortingOrder - 1 : 0;

            // Shield visual for shielded elites
            if (_eliteType == EliteType.Shielded)
            {
                _shieldVisual = new GameObject("ShieldVisual");
                _shieldVisual.transform.SetParent(transform);
                _shieldVisual.transform.localPosition = Vector3.zero;
                _shieldVisual.transform.localScale = Vector3.one * 1.3f;

                var shieldSR = _shieldVisual.AddComponent<SpriteRenderer>();
                shieldSR.sprite = Graphics.SpriteGenerator.CreateCircle(64, _shieldColor, "Shield");
                shieldSR.color = _shieldColor;
                shieldSR.sortingOrder = _spriteRenderer != null ? _spriteRenderer.sortingOrder + 1 : 1;
            }
        }

        private void Update()
        {
            if (!_isElite || !_enemy.IsAlive) return;

            UpdateEliteGlow();

            switch (_eliteType)
            {
                case EliteType.Shielded:
                    UpdateShieldRegen();
                    break;

                case EliteType.Berserker:
                    UpdateBerserker();
                    break;

                case EliteType.Teleporter:
                    _teleportTimer += Time.deltaTime;
                    break;
            }
        }

        private void UpdateEliteGlow()
        {
            if (_eliteGlow == null) return;

            var glowSR = _eliteGlow.GetComponent<SpriteRenderer>();
            if (glowSR != null)
            {
                float pulse = 0.2f + Mathf.Sin(Time.time * _glowPulseSpeed) * 0.1f;
                Color c = _eliteGlowColor;
                c.a = pulse;
                glowSR.color = c;
            }
        }

        private void UpdateShieldRegen()
        {
            if (_currentShield >= _shieldAmount) return;

            _shieldRegenTimer += Time.deltaTime;
            if (_shieldRegenTimer >= _shieldRegenTime)
            {
                _currentShield = Mathf.Min(_currentShield + 1, _shieldAmount);
                _shieldRegenTimer = 0f;
                UpdateShieldVisual();
            }
        }

        private void UpdateShieldVisual()
        {
            if (_shieldVisual == null) return;

            var sr = _shieldVisual.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = _shieldColor;
                c.a = _currentShield > 0 ? 0.5f : 0f;
                sr.color = c;
            }
        }

        private void UpdateBerserker()
        {
            if (_isBerserk) return;

            if (_enemy.HealthPercent <= _berserkerThreshold)
            {
                EnterBerserkMode();
            }
        }

        private void EnterBerserkMode()
        {
            _isBerserk = true;

            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var enemyType = typeof(EnemyBase);

            // Boost speed and damage
            var speedField = enemyType.GetField("_speed", bindingFlags);
            var damageField = enemyType.GetField("_damage", bindingFlags);

            if (speedField != null)
            {
                speedField.SetValue(_enemy, _originalSpeed * _berserkerSpeedMultiplier);
            }
            if (damageField != null)
            {
                damageField.SetValue(_enemy, Mathf.RoundToInt(_originalDamage * _berserkerDamageMultiplier));
            }

            // Visual feedback
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _berserkerColor;
            }
            if (_eliteGlow != null)
            {
                var glowSR = _eliteGlow.GetComponent<SpriteRenderer>();
                if (glowSR != null) glowSR.color = _berserkerColor;
            }

            Debug.Log($"[EliteModifier] {_enemy.EnemyType} entered BERSERK mode!");
        }

        /// <summary>
        /// Called when enemy takes damage. Returns true if damage should be blocked.
        /// </summary>
        public bool OnTakeDamage(int damage, Vector2 source)
        {
            if (!_isElite) return false;

            switch (_eliteType)
            {
                case EliteType.Shielded:
                    if (_currentShield > 0)
                    {
                        _currentShield--;
                        _shieldRegenTimer = 0f;
                        UpdateShieldVisual();
                        // Shield absorbs hit
                        return true;
                    }
                    break;

                case EliteType.Teleporter:
                    if (_teleportTimer >= _teleportCooldown && Random.value < _teleportChance)
                    {
                        Teleport();
                        _teleportTimer = 0f;
                    }
                    break;
            }

            return false;
        }

        private void Teleport()
        {
            // Find random position away from damage
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector2 newPos = (Vector2)transform.position + randomDir * _teleportRange;

            // Visual effect at old position
            StartCoroutine(TeleportEffect(transform.position, newPos));

            transform.position = newPos;
        }

        private IEnumerator TeleportEffect(Vector2 from, Vector2 to)
        {
            // Simple flash effect
            if (_spriteRenderer != null)
            {
                Color original = _spriteRenderer.color;
                _spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                _spriteRenderer.color = original;
            }
        }

        /// <summary>
        /// Called when enemy dies. For splitter type.
        /// </summary>
        public void OnDeath()
        {
            if (!_isElite) return;

            if (_eliteType == EliteType.Splitter)
            {
                // Spawn mini versions (handled by spawner)
                EventBus.Publish(new EliteSplitEvent
                {
                    position = transform.position,
                    enemyType = _enemy.EnemyType,
                    splitCount = _splitCount
                });
            }

            // Cleanup visuals
            if (_eliteGlow != null) Destroy(_eliteGlow);
            if (_shieldVisual != null) Destroy(_shieldVisual);
        }

        public void Reset()
        {
            _isElite = false;
            _eliteType = EliteType.None;
            _isBerserk = false;
            _currentShield = 0;

            if (_eliteGlow != null)
            {
                Destroy(_eliteGlow);
                _eliteGlow = null;
            }
            if (_shieldVisual != null)
            {
                Destroy(_shieldVisual);
                _shieldVisual = null;
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
