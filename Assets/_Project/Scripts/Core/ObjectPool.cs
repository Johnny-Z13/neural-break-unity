// ObjectPool is now provided by Z13.Core package.
// This file provides namespace aliases for backward compatibility.

namespace NeuralBreak.Core
{
    /// <summary>
    /// ObjectPool alias for backward compatibility.
    /// The actual implementation is in Z13.Core.ObjectPool.
    /// </summary>
    public class ObjectPool<T> : Z13.Core.ObjectPool<T> where T : UnityEngine.Component
    {
        public ObjectPool(T prefab, UnityEngine.Transform parent, int initialSize = 10,
            System.Action<T> onGet = null, System.Action<T> onReturn = null)
            : base(prefab, parent, initialSize, onGet, onReturn)
        {
        }
    }

    /// <summary>
    /// GameObjectPool alias for backward compatibility.
    /// The actual implementation is in Z13.Core.GameObjectPool.
    /// </summary>
    public class GameObjectPool : Z13.Core.GameObjectPool
    {
    }
}
