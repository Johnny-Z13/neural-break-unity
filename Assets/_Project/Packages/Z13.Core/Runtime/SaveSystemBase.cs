using UnityEngine;
using System;
using System.IO;

namespace Z13.Core
{
    /// <summary>
    /// Base class for save systems. Handles file I/O and JSON serialization.
    /// Inherit from this class and implement game-specific save logic.
    /// </summary>
    /// <typeparam name="T">The save data type (must be serializable)</typeparam>
    public abstract class SaveSystemBase<T> : MonoBehaviour where T : class, new()
    {
        [Header("Save Settings")]
        [SerializeField] protected bool m_autoSaveEnabled = true;
        [SerializeField] protected float m_autoSaveInterval = 60f;

        public T CurrentSave { get; protected set; }
        public bool HasSaveData => CurrentSave != null;

        public event Action<T> OnSaveLoaded;
        public event Action OnSaveCleared;

        protected float m_lastAutoSave;

        /// <summary>
        /// Override to provide the save file name (e.g., "my_game_save.json")
        /// </summary>
        protected abstract string SaveFileName { get; }

        /// <summary>
        /// Override to determine if auto-save should run this frame
        /// </summary>
        protected virtual bool ShouldAutoSave => false;

        protected virtual void Awake()
        {
            LoadGame();
        }

        protected virtual void Update()
        {
            if (m_autoSaveEnabled && ShouldAutoSave)
            {
                if (Time.time - m_lastAutoSave > m_autoSaveInterval)
                {
                    SaveGame();
                    m_lastAutoSave = Time.time;
                }
            }
        }

        /// <summary>
        /// Save current game state to file
        /// </summary>
        public virtual void SaveGame()
        {
            if (CurrentSave == null)
            {
                CurrentSave = new T();
            }

            OnBeforeSave();

            try
            {
                string json = JsonUtility.ToJson(CurrentSave, true);
                string path = GetSavePath();
                File.WriteAllText(path, json);
                Debug.Log($"[SaveSystem] Game saved to {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
            }
        }

        /// <summary>
        /// Override to perform actions before save (e.g., update timestamps)
        /// </summary>
        protected virtual void OnBeforeSave() { }

        /// <summary>
        /// Load game state from file
        /// </summary>
        public virtual void LoadGame()
        {
            string path = GetSavePath();

            if (!File.Exists(path))
            {
                Debug.Log("[SaveSystem] No save file found, creating new save");
                CurrentSave = new T();
                OnNewSaveCreated();
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                CurrentSave = JsonUtility.FromJson<T>(json);
                Debug.Log($"[SaveSystem] Game loaded from {path}");

                OnAfterLoad();
                OnSaveLoaded?.Invoke(CurrentSave);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to load: {e.Message}");
                CurrentSave = new T();
                OnNewSaveCreated();
            }
        }

        /// <summary>
        /// Override to perform actions after load (e.g., migration)
        /// </summary>
        protected virtual void OnAfterLoad() { }

        /// <summary>
        /// Override to perform actions when a new save is created
        /// </summary>
        protected virtual void OnNewSaveCreated() { }

        /// <summary>
        /// Delete save data
        /// </summary>
        public virtual void ClearSave()
        {
            string path = GetSavePath();

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            CurrentSave = new T();
            OnSaveCleared?.Invoke();

            Debug.Log("[SaveSystem] Save data cleared");
        }

        /// <summary>
        /// Get full path to save file
        /// </summary>
        protected virtual string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }

        /// <summary>
        /// Export save as JSON string (for cloud backup)
        /// </summary>
        public string ExportSave()
        {
            if (CurrentSave == null) return "";
            return JsonUtility.ToJson(CurrentSave);
        }

        /// <summary>
        /// Import save from JSON string (for cloud restore)
        /// </summary>
        public bool ImportSave(string json)
        {
            try
            {
                CurrentSave = JsonUtility.FromJson<T>(json);
                SaveGame();
                OnSaveLoaded?.Invoke(CurrentSave);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Import failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if save file exists
        /// </summary>
        public bool SaveFileExists()
        {
            return File.Exists(GetSavePath());
        }

        /// <summary>
        /// Get save file info (size, last modified)
        /// </summary>
        public (long size, DateTime lastModified)? GetSaveFileInfo()
        {
            string path = GetSavePath();
            if (!File.Exists(path)) return null;

            var info = new FileInfo(path);
            return (info.Length, info.LastWriteTime);
        }
    }
}
