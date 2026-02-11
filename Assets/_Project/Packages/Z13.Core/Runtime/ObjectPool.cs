using System;
using System.Collections.Generic;
using UnityEngine;

namespace Z13.Core
{
    /// <summary>
    /// Generic object pool for zero-allocation spawning.
    /// Use this for projectiles, enemies, particles, etc.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly Queue<T> m_pool = new Queue<T>();
        private readonly T m_prefab;
        private readonly Transform m_parent;
        private readonly int m_initialSize;
        private readonly Action<T> m_onGet;
        private readonly Action<T> m_onReturn;

        public int CountActive { get; private set; }
        public int CountInPool => m_pool.Count;
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
            if (prefab == null)
            {
                Debug.LogError("[ObjectPool] Cannot create pool - prefab is null!");
                return;
            }

            if (initialSize < 0)
            {
                Debug.LogWarning($"[ObjectPool] Invalid initialSize: {initialSize}. Using default of 10.");
                initialSize = 10;
            }

            m_prefab = prefab;
            m_parent = parent;
            m_initialSize = initialSize;
            m_onGet = onGet;
            m_onReturn = onReturn;

            Prewarm();
        }

        /// <summary>
        /// Pre-instantiate objects to avoid runtime allocation spikes
        /// </summary>
        public void Prewarm()
        {
            if (m_prefab == null)
            {
                Debug.LogError("[ObjectPool] Cannot prewarm - prefab is null!");
                return;
            }

            for (int i = 0; i < m_initialSize; i++)
            {
                T obj = CreateNew();
                if (obj != null)
                {
                    obj.gameObject.SetActive(false);
                    m_pool.Enqueue(obj);
                }
            }
        }

        /// <summary>
        /// Get an object from the pool (or create new if empty)
        /// </summary>
        public T Get()
        {
            if (m_prefab == null)
            {
                Debug.LogError("[ObjectPool] Cannot get object - prefab is null!");
                return null;
            }

            T obj;

            if (m_pool.Count > 0)
            {
                obj = m_pool.Dequeue();
            }
            else
            {
                obj = CreateNew();
            }

            if (obj == null)
            {
                Debug.LogError("[ObjectPool] Failed to get object from pool!");
                return null;
            }

            obj.gameObject.SetActive(true);
            CountActive++;

            try
            {
                m_onGet?.Invoke(obj);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ObjectPool] Error in onGet callback: {ex.Message}");
            }

            return obj;
        }

        /// <summary>
        /// Get an object and set its position/rotation
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = Get();
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            return obj;
        }

        /// <summary>
        /// Return an object to the pool
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[ObjectPool] Attempted to return null object to pool!");
                return;
            }

            try
            {
                m_onReturn?.Invoke(obj);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ObjectPool] Error in onReturn callback: {ex.Message}");
            }

            obj.gameObject.SetActive(false);
            m_pool.Enqueue(obj);
            CountActive--;
        }

        /// <summary>
        /// Return all active objects to the pool
        /// </summary>
        public void ReturnAll(List<T> activeObjects)
        {
            if (activeObjects == null)
            {
                Debug.LogWarning("[ObjectPool] Cannot return all - activeObjects list is null!");
                return;
            }

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
            while (m_pool.Count > 0)
            {
                T obj = m_pool.Dequeue();
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }
            CountActive = 0;
        }

        private T CreateNew()
        {
            T obj = UnityEngine.Object.Instantiate(m_prefab, m_parent);
            return obj;
        }
    }
}
