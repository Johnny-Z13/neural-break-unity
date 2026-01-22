using UnityEngine;
using NeuralBreak.Core;

namespace NeuralBreak.Graphics
{
    /// <summary>
    /// Manages enemy death visual effects using procedural particle systems.
    /// Each enemy type has unique death particles matching their visual style.
    /// </summary>
    public class EnemyDeathVFX : MonoBehaviour
    {
        public static EnemyDeathVFX Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool _enabled = true;
        [SerializeField] private int _maxVFXPerFrame = 5;

        [Header("Particle Settings")]
        [SerializeField] private int _particleCount = 20;
        [SerializeField] private float _particleLifetime = 0.8f;
        [SerializeField] private float _particleSpeed = 5f;
        [SerializeField] private float _particleSize = 0.15f;
        [SerializeField] private float _emissionIntensity = 3f;

        private int _vfxThisFrame;
        private Material _particleMaterial;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateParticleMaterial();
        }

        private void Start()
        {
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);

            if (_particleMaterial != null)
            {
                Destroy(_particleMaterial);
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void LateUpdate()
        {
            _vfxThisFrame = 0;
        }

        private void CreateParticleMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader != null)
            {
                _particleMaterial = new Material(shader);
                _particleMaterial.SetColor("_BaseColor", Color.white);
                _particleMaterial.SetColor("_Color", Color.white);
                _particleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _particleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                _particleMaterial.SetInt("_ZWrite", 0);
                _particleMaterial.renderQueue = 3000;
                _particleMaterial.EnableKeyword("_EMISSION");

                if (_particleMaterial.HasProperty("_Surface"))
                {
                    _particleMaterial.SetFloat("_Surface", 1);
                    _particleMaterial.SetFloat("_Blend", 1);
                }
            }
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (!_enabled) return;
            if (_vfxThisFrame >= _maxVFXPerFrame) return;

            SpawnDeathEffect(evt.position, evt.enemyType);
            _vfxThisFrame++;
        }

        public void SpawnDeathEffect(Vector3 position, EnemyType enemyType)
        {
            if (!_enabled) return;

            GameObject vfxGO = enemyType switch
            {
                EnemyType.DataMite => CreateDataMiteDeath(position),
                EnemyType.ScanDrone => CreateScanDroneDeath(position),
                EnemyType.Fizzer => CreateFizzerDeath(position),
                EnemyType.UFO => CreateUFODeath(position),
                EnemyType.ChaosWorm => CreateChaosWormDeath(position),
                EnemyType.VoidSphere => CreateVoidSphereDeath(position),
                EnemyType.CrystalShard => CreateCrystalShardDeath(position),
                EnemyType.Boss => CreateBossDeath(position),
                _ => CreateDefaultDeath(position)
            };

            Destroy(vfxGO, 2f);
        }

        #region Enemy-Specific Death Effects

        /// <summary>
        /// DataMite: Quick digital dissolve with cyan/blue data fragments
        /// </summary>
        private GameObject CreateDataMiteDeath(Vector3 position)
        {
            var go = new GameObject("DeathVFX_DataMite");
            go.transform.position = position;

            Color primaryColor = new Color(0f, 1f, 0.8f);
            Color secondaryColor = new Color(0f, 0.6f, 1f);

            // Main burst - small fast particles
            var mainPS = CreateBaseParticleSystem(go, "Main");
            var main = mainPS.main;
            main.startLifetime = 0.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = primaryColor * _emissionIntensity;
            main.maxParticles = 30;

            var emission = mainPS.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

            var shape = mainPS.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;

            SetupColorFade(mainPS, primaryColor, secondaryColor);
            SetupShrink(mainPS);
            SetupRenderer(mainPS, primaryColor);

            // Binary/glitch particles - tiny square-like particles
            var glitchPS = CreateBaseParticleSystem(go, "Glitch");
            var glitchMain = glitchPS.main;
            glitchMain.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            glitchMain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            glitchMain.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            glitchMain.startColor = secondaryColor * _emissionIntensity;
            glitchMain.maxParticles = 15;

            var glitchEmission = glitchPS.emission;
            glitchEmission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 8),
                new ParticleSystem.Burst(0.1f, 5)
            });

            SetupColorFade(glitchPS, secondaryColor, primaryColor);
            SetupRenderer(glitchPS, secondaryColor);

            mainPS.Play();
            glitchPS.Play();
            return go;
        }

        /// <summary>
        /// ScanDrone: Mechanical explosion with orange sparks and debris
        /// </summary>
        private GameObject CreateScanDroneDeath(Vector3 position)
        {
            var go = new GameObject("DeathVFX_ScanDrone");
            go.transform.position = position;

            Color primaryColor = new Color(1f, 0.7f, 0f);
            Color secondaryColor = new Color(1f, 0.3f, 0f);
            Color sparkColor = new Color(1f, 1f, 0.5f);

            // Core explosion
            var mainPS = CreateBaseParticleSystem(go, "Explosion");
            var main = mainPS.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            main.startColor = primaryColor * _emissionIntensity;
            main.gravityModifier = 0.3f;
            main.maxParticles = 35;

            var emission = mainPS.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var shape = mainPS.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            SetupColorFade(mainPS, primaryColor, secondaryColor);
            SetupShrink(mainPS);
            SetupRenderer(mainPS, primaryColor);

            // Hot sparks
            var sparkPS = CreateBaseParticleSystem(go, "Sparks");
            var sparkMain = sparkPS.main;
            sparkMain.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(8f, 15f);
            sparkMain.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            sparkMain.startColor = sparkColor * _emissionIntensity;
            sparkMain.gravityModifier = 0.5f;
            sparkMain.maxParticles = 20;

            var sparkEmission = sparkPS.emission;
            sparkEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

            var sparkShape = sparkPS.shape;
            sparkShape.shapeType = ParticleSystemShapeType.Sphere;
            sparkShape.radius = 0.15f;

            SetupColorFade(sparkPS, sparkColor, secondaryColor);
            SetupRenderer(sparkPS, sparkColor);

            // Flash
            AddFlash(go, primaryColor, 0.8f);

            mainPS.Play();
            sparkPS.Play();
            return go;
        }

        /// <summary>
        /// Fizzer: Electric discharge with pink/magenta lightning
        /// </summary>
        private GameObject CreateFizzerDeath(Vector3 position)
        {
            var go = new GameObject("DeathVFX_Fizzer");
            go.transform.position = position;

            Color primaryColor = new Color(1f, 0f, 0.6f);
            Color secondaryColor = new Color(1f, 0.4f, 1f);
            Color electricColor = new Color(1f, 0.8f, 1f);

            // Electric burst - fast chaotic particles
            var mainPS = CreateBaseParticleSystem(go, "Electric");
            var main = mainPS.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = primaryColor * _emissionIntensity;
            main.maxParticles = 50;

            var emission = mainPS.emission;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 30),
                new ParticleSystem.Burst(0.05f, 15)
            });

            var shape = mainPS.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            // Add noise for chaotic movement
            var noise = mainPS.noise;
            noise.enabled = true;
            noise.strength = 3f;
            noise.frequency = 2f;
            noise.scrollSpeed = 1f;

            SetupColorFade(mainPS, electricColor, primaryColor);
            SetupShrink(mainPS);
            SetupRenderer(mainPS, primaryColor);

            // Secondary glow particles
            var glowPS = CreateBaseParticleSystem(go, "Glow");
            var glowMain = glowPS.main;
            glowMain.startLifetime = 0.5f;
            glowMain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            glowMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            glowMain.startColor = secondaryColor * _emissionIntensity * 0.5f;
            glowMain.maxParticles = 10;

            var glowEmission = glowPS.emission;
            glowEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8) });

            SetupColorFade(glowPS, secondaryColor, primaryColor);
            SetupShrink(glowPS);
            SetupRenderer(glowPS, secondaryColor);

            // Bright flash
            AddFlash(go, electricColor, 0.6f);

            mainPS.Play();
            glowPS.Play();
            return go;
        }

        /// <summary>
        /// UFO: Alien green implosion then explosion
        /// </summary>
        private GameObject CreateUFODeath(Vector3 position)
        {
            var go = new GameObject("DeathVFX_UFO");
            go.transform.position = position;

            Color primaryColor = new Color(0.3f, 1f, 0.3f);
            Color secondaryColor = new Color(0f, 1f, 0.5f);
            Color coreColor = new Color(0.8f, 1f, 0.8f);

            // Expanding ring
            var ringPS = CreateBaseParticleSystem(go, "Ring");
            var ringMain = ringPS.main;
            ringMain.startLifetime = 0.5f;
            ringMain.startSpeed = 12f;
            ringMain.startSize = 0.08f;
            ringMain.startColor = primaryColor * _emissionIntensity;
            ringMain.maxParticles = 24;

            var ringEmission = ringPS.emission;
            ringEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

            var ringShape = ringPS.shape;
            ringShape.shapeType = ParticleSystemShapeType.Circle;
            ringShape.radius = 0.1f;
            ringShape.arc = 360f;

            SetupColorFade(ringPS, primaryColor, secondaryColor);
            SetupShrink(ringPS);
            SetupRenderer(ringPS, primaryColor);

            // Central glow that expands
            var corePS = CreateBaseParticleSystem(go, "Core");
            var coreMain = corePS.main;
            coreMain.startLifetime = 0.6f;
            coreMain.startSpeed = 0f;
            coreMain.startSize = 0.5f;
            coreMain.startColor = coreColor * _emissionIntensity * 0.7f;
            coreMain.maxParticles = 1;

            var coreEmission = corePS.emission;
            coreEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

            var coreShape = corePS.shape;
            coreShape.enabled = false;

            var coreSize = corePS.sizeOverLifetime;
            coreSize.enabled = true;
            var expandCurve = new AnimationCurve();
            expandCurve.AddKey(0f, 0.3f);
            expandCurve.AddKey(0.2f, 1.2f);
            expandCurve.AddKey(1f, 0f);
            coreSize.size = new ParticleSystem.MinMaxCurve(1f, expandCurve);

            SetupColorFade(corePS, coreColor, primaryColor);
            SetupRenderer(corePS, coreColor);

            // Debris particles
            var debrisPS = CreateBaseParticleSystem(go, "Debris");
            var debrisMain = debrisPS.main;
            debrisMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
            debrisMain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            debrisMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            debrisMain.startColor = secondaryColor * _emissionIntensity;
            debrisMain.gravityModifier = 0.2f;
            debrisMain.maxParticles = 20;

            var debrisEmission = debrisPS.emission;
            debrisEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.1f, 15) });

            var debrisShape = debrisPS.shape;
            debrisShape.shapeType = ParticleSystemShapeType.Sphere;
            debrisShape.radius = 0.4f;

            SetupColorFade(debrisPS, secondaryColor, primaryColor);
            SetupShrink(debrisPS);
            SetupRenderer(debrisPS, secondaryColor);

            ringPS.Play();
            corePS.Play();
            debrisPS.Play();
            return go;
        }

        /// <summary>
        /// ChaosWorm: Chaotic purple/pink energy dispersal with swirling particles
        /// </summary>
        private GameObject CreateChaosWormDeath(Vector3 position)
        {
            var go = new GameObject("DeathVFX_ChaosWorm");
            go.transform.position = position;

            Color primaryColor = new Color(0.8f, 0f, 1f);
            Color secondaryColor = new Color(1f, 0f, 0.7f);
            Color coreColor = new Color(1f, 0.5f, 1f);

            // Swirling chaos particles
            var mainPS = CreateBaseParticleSystem(go, "Chaos");
            var main = mainPS.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            main.startColor = primaryColor * _emissionIntensity;
            main.maxParticles = 45;

            var emission = mainPS.emission;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 25),
                new ParticleSystem.Burst(0.15f, 15)
            });

            var shape = mainPS.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            // Strong swirling noise
            var noise = mainPS.noise;
            noise.enabled = true;
            noise.strength = 5f;
            noise.frequency = 1.5f;
            noise.scrollSpeed = 2f;
            noise.damping = true;

            // Orbital velocity for swirl effect
            var velocity = mainPS.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(-3f, 3f);

            SetupColorFade(mainPS, coreColor, primaryColor);
            SetupShrink(mainPS);
            SetupRenderer(mainPS, primaryColor);

            // Energy wisps
            var wispPS = CreateBaseParticleSystem(go, "Wisps");
            var wispMain = wispPS.main;
            wispMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            wispMain.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f);
            wispMain.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            wispMain.startColor = secondaryColor * _emissionIntensity;
            wispMain.maxParticles = 25;

            var wispEmission = wispPS.emission;
            wispEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.05f, 20) });

            var wispNoise = wispPS.noise;
            wispNoise.enabled = true;
            wispNoise.strength = 4f;
            wispNoise.frequency = 3f;

            SetupColorFade(wispPS, secondaryColor, primaryColor);
            SetupRenderer(wispPS, secondaryColor);

            AddFlash(go, coreColor, 1f);

            mainPS.Play();
            wispPS.Play();
            return go;
        }

        /// <summary>
        /// VoidSphere: Dark implosion with void energy and slow particles
        /// </summary>
        private GameObject CreateVoidSphereDeath(Vector3 position)
        {
            var go = new GameObject("DeathVFX_VoidSphere");
            go.transform.position = position;

            Color primaryColor = new Color(0.3f, 0f, 0.6f);
            Color secondaryColor = new Color(0.6f, 0f, 1f);
            Color voidColor = new Color(0.1f, 0f, 0.2f);

            // Implosion effect - particles moving inward first
            var implodePS = CreateBaseParticleSystem(go, "Implode");
            var implodeMain = implodePS.main;
            implodeMain.startLifetime = 0.4f;
            implodeMain.startSpeed = -8f; // Negative speed = implode
            implodeMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            implodeMain.startColor = secondaryColor * _emissionIntensity;
            implodeMain.maxParticles = 30;

            var implodeEmission = implodePS.emission;
            implodeEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

            var implodeShape = implodePS.shape;
            implodeShape.shapeType = ParticleSystemShapeType.Sphere;
            implodeShape.radius = 1.2f;

            SetupColorFade(implodePS, secondaryColor, voidColor);
            SetupRenderer(implodePS, secondaryColor);

            // Slow expanding void particles
            var voidPS = CreateBaseParticleSystem(go, "Void");
            var voidMain = voidPS.main;
            voidMain.startDelay = 0.3f;
            voidMain.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.8f);
            voidMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            voidMain.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            voidMain.startColor = primaryColor * _emissionIntensity;
            voidMain.maxParticles = 25;

            var voidEmission = voidPS.emission;
            voidEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

            var voidShape = voidPS.shape;
            voidShape.shapeType = ParticleSystemShapeType.Sphere;
            voidShape.radius = 0.2f;

            SetupColorFade(voidPS, primaryColor, voidColor);
            SetupShrink(voidPS);
            SetupRenderer(voidPS, primaryColor);

            // Dark flash - use brighter purple for visibility
            Color flashColor = new Color(0.8f, 0.2f, 1f); // Bright purple
            var flashGO = new GameObject("DarkFlash");
            flashGO.transform.SetParent(go.transform);
            flashGO.transform.localPosition = Vector3.zero;
            var flashPS = flashGO.AddComponent<ParticleSystem>();
            flashPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var flashMain = flashPS.main;
            flashMain.duration = 0.5f;
            flashMain.loop = false;
            flashMain.startDelay = 0.25f;
            flashMain.startLifetime = 0.3f;
            flashMain.startSpeed = 0f;
            flashMain.startSize = 1.5f;
            flashMain.startColor = flashColor * _emissionIntensity;
            flashMain.simulationSpace = ParticleSystemSimulationSpace.World;
            flashMain.playOnAwake = false;
            flashMain.maxParticles = 1;

            var flashEmission = flashPS.emission;
            flashEmission.enabled = true;
            flashEmission.rateOverTime = 0;
            flashEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

            var flashShape = flashPS.shape;
            flashShape.enabled = false;

            var flashSize = flashPS.sizeOverLifetime;
            flashSize.enabled = true;
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0f);
            curve.AddKey(0.15f, 1.2f);
            curve.AddKey(1f, 0f);
            flashSize.size = new ParticleSystem.MinMaxCurve(1f, curve);

            SetupRenderer(flashPS, flashColor);

            implodePS.Play();
            voidPS.Play();
            flashPS.Play();
            return go;
        }

        /// <summary>
        /// CrystalShard: Sharp crystalline shatter with ice-blue shards
        /// </summary>
        private GameObject CreateCrystalShardDeath(Vector3 position)
        {
            var go = new GameObject("DeathVFX_CrystalShard");
            go.transform.position = position;

            Color primaryColor = new Color(0.4f, 0.9f, 1f);
            Color secondaryColor = new Color(0.8f, 1f, 1f);
            Color sparkleColor = Color.white;

            // Fast sharp shards
            var shardPS = CreateBaseParticleSystem(go, "Shards");
            var shardMain = shardPS.main;
            shardMain.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            shardMain.startSpeed = new ParticleSystem.MinMaxCurve(12f, 20f);
            shardMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            shardMain.startColor = primaryColor * _emissionIntensity;
            shardMain.gravityModifier = 0.4f;
            shardMain.maxParticles = 35;

            // Rotation for tumbling shards
            shardMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var shardEmission = shardPS.emission;
            shardEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var shardShape = shardPS.shape;
            shardShape.shapeType = ParticleSystemShapeType.Sphere;
            shardShape.radius = 0.15f;

            var shardRotation = shardPS.rotationOverLifetime;
            shardRotation.enabled = true;
            shardRotation.z = new ParticleSystem.MinMaxCurve(-5f, 5f);

            SetupColorFade(shardPS, secondaryColor, primaryColor);
            SetupRenderer(shardPS, primaryColor);

            // Sparkle dust
            var sparklePS = CreateBaseParticleSystem(go, "Sparkles");
            var sparkleMain = sparklePS.main;
            sparkleMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            sparkleMain.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            sparkleMain.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            sparkleMain.startColor = sparkleColor * _emissionIntensity;
            sparkleMain.maxParticles = 25;

            var sparkleEmission = sparklePS.emission;
            sparkleEmission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 15),
                new ParticleSystem.Burst(0.1f, 10)
            });

            var sparkleShape = sparklePS.shape;
            sparkleShape.shapeType = ParticleSystemShapeType.Sphere;
            sparkleShape.radius = 0.25f;

            SetupColorFade(sparklePS, sparkleColor, primaryColor);
            SetupRenderer(sparklePS, sparkleColor);

            AddFlash(go, secondaryColor, 0.5f);

            shardPS.Play();
            sparklePS.Play();
            return go;
        }

        /// <summary>
        /// Boss: Massive multi-stage explosion with screen-filling particles
        /// </summary>
        private GameObject CreateBossDeath(Vector3 position)
        {
            var go = new GameObject("DeathVFX_Boss");
            go.transform.position = position;

            Color primaryColor = new Color(1f, 0.2f, 0f);
            Color secondaryColor = new Color(1f, 0.6f, 0f);
            Color coreColor = new Color(1f, 1f, 0.5f);

            // Massive core explosion
            var corePS = CreateBaseParticleSystem(go, "Core");
            var coreMain = corePS.main;
            coreMain.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            coreMain.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
            coreMain.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            coreMain.startColor = primaryColor * _emissionIntensity;
            coreMain.maxParticles = 80;

            var coreEmission = corePS.emission;
            coreEmission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 40),
                new ParticleSystem.Burst(0.15f, 25),
                new ParticleSystem.Burst(0.3f, 15)
            });

            var coreShape = corePS.shape;
            coreShape.shapeType = ParticleSystemShapeType.Sphere;
            coreShape.radius = 0.8f;

            SetupColorFade(corePS, coreColor, primaryColor);
            SetupShrink(corePS);
            SetupRenderer(corePS, primaryColor);

            // Secondary fire particles
            var firePS = CreateBaseParticleSystem(go, "Fire");
            var fireMain = firePS.main;
            fireMain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            fireMain.startSpeed = new ParticleSystem.MinMaxCurve(8f, 15f);
            fireMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            fireMain.startColor = secondaryColor * _emissionIntensity;
            fireMain.gravityModifier = -0.3f;
            fireMain.maxParticles = 50;

            var fireEmission = firePS.emission;
            fireEmission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0.1f, 30),
                new ParticleSystem.Burst(0.25f, 20)
            });

            var fireShape = firePS.shape;
            fireShape.shapeType = ParticleSystemShapeType.Sphere;
            fireShape.radius = 0.6f;

            SetupColorFade(firePS, secondaryColor, primaryColor);
            SetupShrink(firePS);
            SetupRenderer(firePS, secondaryColor);

            // Expanding shockwave ring
            var ringPS = CreateBaseParticleSystem(go, "Ring");
            var ringMain = ringPS.main;
            ringMain.startLifetime = 0.6f;
            ringMain.startSpeed = 20f;
            ringMain.startSize = 0.1f;
            ringMain.startColor = coreColor * _emissionIntensity;
            ringMain.maxParticles = 36;

            var ringEmission = ringPS.emission;
            ringEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 32) });

            var ringShape = ringPS.shape;
            ringShape.shapeType = ParticleSystemShapeType.Circle;
            ringShape.radius = 0.2f;
            ringShape.arc = 360f;

            SetupColorFade(ringPS, coreColor, secondaryColor);
            SetupShrink(ringPS);
            SetupRenderer(ringPS, coreColor);

            // Smoke/debris
            var smokePS = CreateBaseParticleSystem(go, "Smoke");
            var smokeMain = smokePS.main;
            smokeMain.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
            smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            smokeMain.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            smokeMain.startColor = new Color(0.3f, 0.15f, 0f) * _emissionIntensity * 0.5f;
            smokeMain.gravityModifier = -0.1f;
            smokeMain.maxParticles = 30;

            var smokeEmission = smokePS.emission;
            smokeEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.2f, 20) });

            var smokeShape = smokePS.shape;
            smokeShape.shapeType = ParticleSystemShapeType.Sphere;
            smokeShape.radius = 0.5f;

            var smokeNoise = smokePS.noise;
            smokeNoise.enabled = true;
            smokeNoise.strength = 2f;
            smokeNoise.frequency = 0.5f;

            SetupColorFade(smokePS, new Color(0.4f, 0.2f, 0f), new Color(0.1f, 0.05f, 0f));
            SetupRenderer(smokePS, new Color(0.3f, 0.15f, 0f));

            // Big flash
            AddFlash(go, coreColor, 2f);

            corePS.Play();
            firePS.Play();
            ringPS.Play();
            smokePS.Play();
            return go;
        }

        /// <summary>
        /// Default death effect
        /// </summary>
        private GameObject CreateDefaultDeath(Vector3 position)
        {
            var go = new GameObject("DeathVFX_Default");
            go.transform.position = position;

            Color primaryColor = Color.white;

            var mainPS = CreateBaseParticleSystem(go, "Main");
            var main = mainPS.main;
            main.startLifetime = _particleLifetime;
            main.startSpeed = _particleSpeed;
            main.startSize = _particleSize;
            main.startColor = primaryColor * _emissionIntensity;
            main.maxParticles = _particleCount * 2;

            var emission = mainPS.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, _particleCount) });

            var shape = mainPS.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            SetupColorFade(mainPS, primaryColor, Color.gray);
            SetupShrink(mainPS);
            SetupRenderer(mainPS, primaryColor);

            mainPS.Play();
            return go;
        }

        #endregion

        #region Helper Methods

        private ParticleSystem CreateBaseParticleSystem(GameObject parent, string name)
        {
            var psGO = new GameObject(name);
            psGO.transform.SetParent(parent.transform);
            psGO.transform.localPosition = Vector3.zero;

            var ps = psGO.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;

            var shape = ps.shape;
            shape.enabled = true;
            shape.rotation = Vector3.zero; // Emit in XY plane for 2D

            return ps;
        }

        private void SetupColorFade(ParticleSystem ps, Color startColor, Color endColor)
        {
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(startColor, 0.4f),
                    new GradientColorKey(endColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private void SetupShrink(ParticleSystem ps)
        {
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        }

        private void SetupRenderer(ParticleSystem ps, Color color)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateMaterialForColor(color);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 100;
        }

        private void AddFlash(GameObject parent, Color color, float size)
        {
            var flashGO = new GameObject("Flash");
            flashGO.transform.SetParent(parent.transform);
            flashGO.transform.localPosition = Vector3.zero;

            var flashPS = flashGO.AddComponent<ParticleSystem>();
            flashPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = flashPS.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = 0.15f;
            main.startSpeed = 0f;
            main.startSize = size;
            main.startColor = color * _emissionIntensity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 1;

            var emission = flashPS.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

            var shape = flashPS.shape;
            shape.enabled = false;

            var colorOverLifetime = flashPS.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.5f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = flashPS.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var curve = new AnimationCurve();
            curve.AddKey(0f, 0.8f);
            curve.AddKey(0.2f, 1.2f);
            curve.AddKey(1f, 0.5f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            SetupRenderer(flashPS, color);
            flashPS.Play();
        }

        private Material CreateMaterialForColor(Color color)
        {
            if (_particleMaterial == null)
            {
                CreateParticleMaterial();
            }

            var mat = new Material(_particleMaterial);
            Color emissiveColor = color * _emissionIntensity;

            mat.SetColor("_BaseColor", emissiveColor);
            mat.SetColor("_Color", emissiveColor);
            mat.SetColor("_EmissionColor", emissiveColor);

            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 1);
            }

            return mat;
        }

        #endregion

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }
    }
}
