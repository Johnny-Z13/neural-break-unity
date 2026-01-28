using UnityEngine;
using UnityEditor;
using NeuralBreak.Combat;
using NeuralBreak.UI;

public class SetupSmartBomb
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();
        
        // 1. Add SmartBombSystem to Player
        var player = GameObject.Find("Player");
        if (player == null)
        {
            return "❌ Player GameObject not found!";
        }
        
        var smartBombSystem = player.GetComponent<SmartBombSystem>();
        if (smartBombSystem == null)
        {
            smartBombSystem = player.AddComponent<SmartBombSystem>();
            results.AppendLine("✅ Added SmartBombSystem to Player");
        }
        else
        {
            results.AppendLine("ℹ️ SmartBombSystem already exists on Player");
        }
        
        // 2. Create explosion particle system
        var explosionGO = new GameObject("SmartBombExplosion");
        explosionGO.transform.SetParent(player.transform);
        explosionGO.transform.localPosition = Vector3.zero;
        
        var particleSystem = explosionGO.AddComponent<ParticleSystem>();
        
        // Configure particle system for epic explosion
        var main = particleSystem.main;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.8f, 0.2f, 1f),
            new Color(1f, 0.3f, 0.1f, 0f)
        );
        main.startLifetime = 1.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(15f, 30f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 1.5f);
        main.maxParticles = 500;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = particleSystem.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 500) });
        
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        
        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.5f),
                new GradientColorKey(new Color(0.8f, 0.2f, 0.1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));
        
        // Add particle renderer settings
        var renderer = explosionGO.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 200;
        
        // Create a simple material for particles
        var mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.SetColor("_Color", Color.white);
        renderer.material = mat;
        
        results.AppendLine("✅ Created SmartBombExplosion particle system");
        
        // Assign particle system to SmartBombSystem
        var serializedObject = new SerializedObject(smartBombSystem);
        var explosionProp = serializedObject.FindProperty("_explosionParticles");
        if (explosionProp != null)
        {
            explosionProp.objectReferenceValue = particleSystem;
            serializedObject.ApplyModifiedProperties();
            results.AppendLine("✅ Assigned explosion particles to SmartBombSystem");
        }
        
        // 3. Add SmartBombDisplay to UI
        var mainCanvas = GameObject.Find("MainCanvas");
        if (mainCanvas == null)
        {
            results.AppendLine("⚠️ MainCanvas not found - SmartBombDisplay not added to UI");
        }
        else
        {
            // Check if SmartBombDisplay already exists
            var existingDisplay = mainCanvas.GetComponentInChildren<SmartBombDisplay>(true);
            if (existingDisplay == null)
            {
                // Create SmartBombDisplay container
                var displayGO = new GameObject("SmartBombDisplay");
                displayGO.transform.SetParent(mainCanvas.transform, false);
                
                var rectTransform = displayGO.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(0, 0);
                rectTransform.pivot = new Vector2(0, 0);
                rectTransform.anchoredPosition = new Vector2(20, 20);
                rectTransform.sizeDelta = new Vector2(150, 50);
                
                var display = displayGO.AddComponent<SmartBombDisplay>();
                results.AppendLine("✅ Added SmartBombDisplay to MainCanvas");
            }
            else
            {
                results.AppendLine("ℹ️ SmartBombDisplay already exists in UI");
            }
        }
        
        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
        
        results.AppendLine("\n📋 MANUAL SETUP NEEDED:");
        results.AppendLine("   1. Assign epic explosion AudioClip to SmartBombSystem");
        results.AppendLine("   2. Add 'SmartBomb' action to Input Actions asset");
        results.AppendLine("      - Keyboard: B key");
        results.AppendLine("      - Gamepad: Left Trigger or Y button");
        results.AppendLine("   Note: Feel/MMFeedbacks removed - using native Unity feedback");
        
        return results.ToString();
    }
}
