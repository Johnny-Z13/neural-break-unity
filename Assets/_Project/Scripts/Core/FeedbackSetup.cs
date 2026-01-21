using UnityEngine;
using MoreMountains.Feedbacks;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Sets up MMFeedbacks at runtime for entities that don't have them assigned.
    /// This provides "juicy" game feel effects using the Feel/MMFeedbacks package.
    /// Simplified version that creates basic feedback players.
    /// </summary>
    public class FeedbackSetup : MonoBehaviour
    {
        public static FeedbackSetup Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private MMF_Player CreateFeedbackPlayer(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            var player = go.AddComponent<MMF_Player>();
            player.InitializationMode = MMF_Player.InitializationModes.Script;
            return player;
        }

        /// <summary>
        /// Create a hit feedback for a specific target (simple flash/scale effect)
        /// </summary>
        public MMF_Player CreateHitFeedback(Transform target)
        {
            var player = CreateFeedbackPlayer($"{target.name}_HitFB", target);

            // Add a simple flash feedback
            var flash = new MMF_Flash();
            flash.Label = "Hit Flash";
            flash.FlashColor = Color.white;
            flash.FlashDuration = 0.05f;
            player.AddFeedback(flash);

            player.Initialization();
            return player;
        }

        /// <summary>
        /// Create a spawn feedback for a specific target
        /// </summary>
        public MMF_Player CreateSpawnFeedback(Transform target)
        {
            var player = CreateFeedbackPlayer($"{target.name}_SpawnFB", target);

            // Add a simple flash for spawn
            var flash = new MMF_Flash();
            flash.Label = "Spawn Flash";
            flash.FlashColor = new Color(1f, 1f, 1f, 0.5f);
            flash.FlashDuration = 0.15f;
            player.AddFeedback(flash);

            player.Initialization();
            return player;
        }

        /// <summary>
        /// Create a death feedback for a specific target
        /// </summary>
        public MMF_Player CreateDeathFeedback(Transform target)
        {
            var player = CreateFeedbackPlayer($"{target.name}_DeathFB", target);

            // Add a flash for death
            var flash = new MMF_Flash();
            flash.Label = "Death Flash";
            flash.FlashColor = Color.red;
            flash.FlashDuration = 0.1f;
            player.AddFeedback(flash);

            player.Initialization();
            return player;
        }
    }
}
