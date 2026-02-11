using UnityEngine;
using UnityEngine.SceneManagement;
using Z13.Core;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Controls singleton initialization order in the Boot scene.
    /// Components are initialized in the exact order they appear in the list,
    /// guaranteeing that dependencies are satisfied before dependents initialize.
    /// After initialization, loads the main game scene.
    /// </summary>
    public class BootManager : MonoBehaviour
    {
        [Header("Initialization Order")]
        [Tooltip("Singletons will be initialized in this exact order. Earlier items are guaranteed to exist when later items initialize.")]
        [SerializeField] private MonoBehaviour[] m_bootComponents;

        [Header("Scene Transition")]
        [SerializeField] private string m_mainSceneName = "main-neural-break";
        [SerializeField] private bool m_loadMainSceneOnBoot = true;

        private void Start()
        {
            InitializeAll();

            if (m_loadMainSceneOnBoot)
            {
                LoadMainScene();
            }
        }

        private void InitializeAll()
        {
            Debug.Log($"[BootManager] Starting initialization of {m_bootComponents.Length} components...");
            float startTime = Time.realtimeSinceStartup;

            for (int i = 0; i < m_bootComponents.Length; i++)
            {
                var component = m_bootComponents[i];
                if (component == null)
                {
                    Debug.LogError($"[BootManager] Null component at index {i} in boot list!");
                    continue;
                }

                // Mark as persistent - survives scene loads
                DontDestroyOnLoad(component.gameObject);

                // Initialize if bootable
                if (component is IBootable bootable)
                {
                    Debug.Log($"[BootManager] [{i}] Initializing {component.GetType().Name}...");
                    bootable.Initialize();
                }
                else
                {
                    Debug.LogWarning($"[BootManager] [{i}] {component.GetType().Name} does not implement IBootable - marked as persistent but not initialized");
                }
            }

            float elapsed = Time.realtimeSinceStartup - startTime;
            Debug.Log($"[BootManager] All {m_bootComponents.Length} components initialized in {elapsed:F3}s");
        }

        private void LoadMainScene()
        {
            Debug.Log($"[BootManager] Loading main scene: {m_mainSceneName}");
            SceneManager.LoadScene(m_mainSceneName);
        }

        /// <summary>
        /// Manually trigger main scene load (useful for testing)
        /// </summary>
        public void LoadMainSceneManual()
        {
            LoadMainScene();
        }

        #region Debug

        [ContextMenu("Debug: List Boot Components")]
        private void DebugListComponents()
        {
            for (int i = 0; i < m_bootComponents.Length; i++)
            {
                var c = m_bootComponents[i];
                string name = c != null ? c.GetType().Name : "NULL";
                string bootable = c is IBootable ? "IBootable" : "NOT Bootable";
                Debug.Log($"[BootManager] [{i}] {name} ({bootable})");
            }
        }

        [ContextMenu("Debug: Reinitialize All")]
        private void DebugReinitialize()
        {
            InitializeAll();
        }

        #endregion
    }
}
