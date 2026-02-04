// EventBus is now provided by Z13.Core package.
// This file provides a namespace alias for backward compatibility.
// All event structs have been moved to GameEvents.cs

namespace NeuralBreak.Core
{
    /// <summary>
    /// EventBus alias for backward compatibility.
    /// The actual implementation is in Z13.Core.EventBus.
    ///
    /// Usage remains the same:
    /// - EventBus.Subscribe&lt;MyEvent&gt;(handler)
    /// - EventBus.Publish(new MyEvent { ... })
    /// - EventBus.Unsubscribe&lt;MyEvent&gt;(handler)
    /// </summary>
    public static class EventBus
    {
        public static void Subscribe<T>(System.Action<T> handler) where T : struct
            => Z13.Core.EventBus.Subscribe(handler);

        public static void Unsubscribe<T>(System.Action<T> handler) where T : struct
            => Z13.Core.EventBus.Unsubscribe(handler);

        public static void Publish<T>(T eventData) where T : struct
            => Z13.Core.EventBus.Publish(eventData);

        public static void Clear()
            => Z13.Core.EventBus.Clear();

        public static int GetSubscriberCount<T>() where T : struct
            => Z13.Core.EventBus.GetSubscriberCount<T>();

        public static bool HasSubscribers<T>() where T : struct
            => Z13.Core.EventBus.HasSubscribers<T>();
    }
}
