using UnityEngine;

namespace NeuralBreak.Entities
{
    /// <summary>
    /// Runtime fix for pickup physics issues.
    /// Ensures pickups don't have gravity or physics body that causes falling.
    ///
    /// NOTE: This is a runtime fix. The proper solution is to fix the prefab in Unity Editor:
    /// - Open MedPackPickup.prefab
    /// - Remove Rigidbody2D component OR set Body Type to Kinematic with Gravity Scale = 0
    /// </summary>
    [RequireComponent(typeof(PickupBase))]
    public class PickupPhysicsFix : MonoBehaviour
    {
        private void Awake()
        {
            FixPhysics();
        }

        private void FixPhysics()
        {
            // Get Rigidbody2D if present
            Rigidbody2D rb = GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                // Pickups should NOT use physics - they move via transform.position
                // Set to Kinematic (no physics simulation) and disable gravity
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;

                Debug.Log($"[PickupPhysicsFix] Fixed Rigidbody2D on {gameObject.name}: Set to Kinematic with no gravity");
            }
        }
    }
}
