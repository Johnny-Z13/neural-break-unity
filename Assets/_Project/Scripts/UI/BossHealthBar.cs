using UnityEngine;

namespace NeuralBreak.UI
{
    /// <summary>
    /// DISABLED - Boss health bar removed from UI per user request.
    /// This component does nothing but exists to prevent null reference errors
    /// in SceneReferenceWiring which expects this type to exist.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        // Intentionally empty - boss health bar is disabled
        private void Start()
        {
            // Disable this GameObject entirely
            gameObject.SetActive(false);
        }
    }
}
