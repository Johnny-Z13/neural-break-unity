using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuralBreak.Core
{
    /// <summary>
    /// Generic object pool for zero-allocation spawning.
    /// Use this for projectiles, enemies, particles, etc.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly Queue<T> _pool = new Queue<T>();
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly int _initialSize;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;

        public int CountActive { get; private set; }
        public int CountInPool => _pool.Count;
        public int CountTotal => CountActive + CountInPool;

        /// <summary>
        /// Create a new object pool
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="parent">Parent transform for pooled objects</param>
        /// <param name="initialSize">Number of objects to pre-instantiate</param>
        /// <param name="onGet">Called when object is retrieved from pool</param>
        /// <param name="onReturn">Called when object is returned to pool</param>
        public ObjectPool(T prefab, Transform parent, int initialSize = 10,
            Action<T> onGet = null, Action<T> onReturn = null)
        {
            _prefab = prefab;
            _parent = parent;
            _initialSize = initialSize;
            _onGet = onGet;
            _onReturn = onReturn;

            Prewarm();
        }

        /// <summary>
        /// Pre-instantiate objects to avoid runtime allocation spikes
        /// </summary>
        public void Prewarm()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                T obj = CreateNew();
                obj.gameObject.SetActive(false);
                _pool.Enqueue(obj);
            }
        }

        /// <summary>
        /// Get an object from the pool (or create new if empty)
        /// </summary>
        public T Get()
        {
            T obj;

            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else
            {
                obj = CreateNew();
            }

            obj.gameObject.SetActive(true);
            CountActive++;
            _onGet?.Invoke(obj);

            return obj;
        }

        /// <summary>
        /// Get an object and set its position/rotation
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = Get();
            obj.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }

        /// <summary>
        /// Return an object to the pool
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null) return;

            _onReturn?.Invoke(obj);
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
            CountActive--;
        }

        /// <summary>
        /// Return all active objects to the pool
        /// </summary>
        public void ReturnAll(List<T> activeObjects)
        {
            foreach (var obj in activeObjects)
            {
                Return(obj);
            }
            activeObjects.Clear();
        }

        /// <summary>
        /// Clear the pool and destroy all objects
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                T obj = _pool.Dequeue();
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }
            CountActive = 0;
        }

        private T CreateNew()
        {
            T obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            return obj;
        }
    }

    /// <summary>
    /// Non-generic pool manager for use in inspector/prefab references
    /// </summary>
    public class GameObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _initialSize = 20;

        private ObjectPool<Transform> _pool;

        private void Awake()
        {
            _pool = new ObjectPool<Transform>(
                _prefab.transform,
                transform,
                _initialSize
            );
        }

        public GameObject Get()
        {
            return _pool.Get().gameObject;
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            return _pool.Get(position, rotation).gameObject;
        }

        public void Return(GameObject obj)
        {
            _pool.Return(obj.transform);
        }
    }
}
