using System;
using UnityEngine;

namespace Z13.Core
{
    /// <summary>
    /// Simple event bus for decoupled communication between systems.
    /// Provides type-safe events without tight coupling.
    ///
    /// Uses the static generic class pattern: each T gets its own static field.
    /// No Dictionary, no typeof(T) lookups, no boxing - zero allocation on Publish.
    ///
    /// Usage:
    /// - Define event structs in your game project
    /// - Subscribe: EventBus.Subscribe&lt;MyEvent&gt;(handler)
    /// - Publish: EventBus.Publish(new MyEvent { ... })
    /// - Unsubscribe: EventBus.Unsubscribe&lt;MyEvent&gt;(handler)
    /// </summary>
    public static class EventBus
    {
        /// <summary>
        /// Static generic class pattern: each T gets its own static field.
        /// CLR guarantees separate storage per type argument - no dictionary needed.
        /// </summary>
        private static class EventHolder<T> where T : struct
        {
            public static Action<T> handlers;
        }

        /// <summary>
        /// Subscribe to an event type
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
            {
                Debug.LogError($"[EventBus] Cannot subscribe to {typeof(T).Name} - handler is null!");
                return;
            }

            EventHolder<T>.handlers += handler;
        }

        /// <summary>
        /// Unsubscribe from an event type
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
            {
                Debug.LogError($"[EventBus] Cannot unsubscribe from {typeof(T).Name} - handler is null!");
                return;
            }

            EventHolder<T>.handlers -= handler;
        }

        /// <summary>
        /// Publish an event to all subscribers.
        /// Zero allocation: no dictionary lookup, no typeof, no boxing.
        /// </summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            var handler = EventHolder<T>.handlers;
            if (handler != null)
            {
                try
                {
                    handler.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Error invoking event {typeof(T).Name}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Clear all event subscriptions for a specific type.
        /// Call ClearAll&lt;T&gt;() for each event type, or let subscribers manage
        /// their own lifecycle via OnEnable/OnDisable.
        /// </summary>
        public static void Clear<T>() where T : struct
        {
            EventHolder<T>.handlers = null;
        }

        /// <summary>
        /// Clear all event subscriptions.
        /// Note: With the static generic pattern, a full clear requires clearing each
        /// type individually. This method is provided for API compatibility but callers
        /// should prefer Clear&lt;T&gt;() or proper Subscribe/Unsubscribe lifecycle.
        /// In practice, subscribers manage their own lifecycle via OnEnable/OnDisable.
        /// </summary>
        public static void Clear()
        {
            // With static generic pattern, we cannot enumerate all T types at runtime.
            // Subscribers should manage their own lifecycle (OnEnable/OnDisable).
            // This is a no-op for API compatibility. Use Clear<T>() for specific types.
        }

        /// <summary>
        /// Get count of subscribers for an event type (useful for debugging)
        /// </summary>
        public static int GetSubscriberCount<T>() where T : struct
        {
            var handler = EventHolder<T>.handlers;
            return handler?.GetInvocationList()?.Length ?? 0;
        }

        /// <summary>
        /// Check if any subscribers exist for an event type
        /// </summary>
        public static bool HasSubscribers<T>() where T : struct
        {
            return EventHolder<T>.handlers != null;
        }
    }
}
