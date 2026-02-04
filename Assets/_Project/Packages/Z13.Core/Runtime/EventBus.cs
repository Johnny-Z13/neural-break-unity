using System;
using System.Collections.Generic;
using UnityEngine;

namespace Z13.Core
{
    /// <summary>
    /// Simple event bus for decoupled communication between systems.
    /// Provides type-safe events without tight coupling.
    ///
    /// Usage:
    /// - Define event structs in your game project
    /// - Subscribe: EventBus.Subscribe&lt;MyEvent&gt;(handler)
    /// - Publish: EventBus.Publish(new MyEvent { ... })
    /// - Unsubscribe: EventBus.Unsubscribe&lt;MyEvent&gt;(handler)
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> s_events = new Dictionary<Type, Delegate>();

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

            Type eventType = typeof(T);

            if (s_events.TryGetValue(eventType, out Delegate existing))
            {
                s_events[eventType] = Delegate.Combine(existing, handler);
            }
            else
            {
                s_events[eventType] = handler;
            }
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

            Type eventType = typeof(T);

            if (s_events.TryGetValue(eventType, out Delegate existing))
            {
                Delegate newDelegate = Delegate.Remove(existing, handler);
                if (newDelegate == null)
                {
                    s_events.Remove(eventType);
                }
                else
                {
                    s_events[eventType] = newDelegate;
                }
            }
        }

        /// <summary>
        /// Publish an event to all subscribers
        /// </summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            Type eventType = typeof(T);

            if (s_events.TryGetValue(eventType, out Delegate handler))
            {
                try
                {
                    (handler as Action<T>)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Error invoking event {typeof(T).Name}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Clear all event subscriptions (call on scene unload)
        /// </summary>
        public static void Clear()
        {
            s_events.Clear();
        }

        /// <summary>
        /// Get count of subscribers for an event type (useful for debugging)
        /// </summary>
        public static int GetSubscriberCount<T>() where T : struct
        {
            if (s_events.TryGetValue(typeof(T), out Delegate handler))
            {
                return handler?.GetInvocationList()?.Length ?? 0;
            }
            return 0;
        }

        /// <summary>
        /// Check if any subscribers exist for an event type
        /// </summary>
        public static bool HasSubscribers<T>() where T : struct
        {
            return s_events.ContainsKey(typeof(T));
        }
    }
}
