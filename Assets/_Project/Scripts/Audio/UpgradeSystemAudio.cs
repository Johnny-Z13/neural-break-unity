using UnityEngine;
using NeuralBreak.Core;
using NeuralBreak.Combat;

namespace NeuralBreak.Audio
{
    /// <summary>
    /// Audio manager for upgrade system sounds.
    /// NOTE: Temporarily simplified - uses basic AudioSource.PlayOneShot
    /// </summary>
    public class UpgradeSystemAudio : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioSource m_audioSource;

        [Header("Audio Clips (Optional)")]
        [SerializeField] private AudioClip m_screenOpenClip;
        [SerializeField] private AudioClip m_hoverClip;
        [SerializeField] private AudioClip m_selectClip;
        [SerializeField] private AudioClip m_appliedClip;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float m_masterVolume = 0.7f;

        private void Awake()
        {
            if (m_audioSource == null)
            {
                m_audioSource = GetComponent<AudioSource>();
                if (m_audioSource == null)
                {
                    m_audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            m_audioSource.playOnAwake = false;
            m_audioSource.spatialBlend = 0f; // 2D sound
        }

        private void Start()
        {
            // Subscribe to events
            EventBus.Subscribe<UpgradeSelectionStartedEvent>(OnUpgradeSelectionStarted);
            EventBus.Subscribe<UpgradeSelectedEvent>(OnUpgradeSelected);
            EventBus.Subscribe<PermanentUpgradeAddedEvent>(OnPermanentUpgradeAdded);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<UpgradeSelectionStartedEvent>(OnUpgradeSelectionStarted);
            EventBus.Unsubscribe<UpgradeSelectedEvent>(OnUpgradeSelected);
            EventBus.Unsubscribe<PermanentUpgradeAddedEvent>(OnPermanentUpgradeAdded);
        }

        private void OnUpgradeSelectionStarted(UpgradeSelectionStartedEvent evt)
        {
            PlayScreenOpenSound();
        }

        private void OnUpgradeSelected(UpgradeSelectedEvent evt)
        {
            PlaySelectSound(evt.selected.tier);
        }

        private void OnPermanentUpgradeAdded(PermanentUpgradeAddedEvent evt)
        {
            PlayAppliedSound();
        }

        public void PlayScreenOpenSound()
        {
            if (m_screenOpenClip != null)
            {
                m_audioSource.PlayOneShot(m_screenOpenClip, m_masterVolume);
            }
        }

        public void PlayHoverSound()
        {
            if (m_hoverClip != null)
            {
                m_audioSource.PlayOneShot(m_hoverClip, m_masterVolume * 0.3f);
            }
        }

        public void PlaySelectSound(UpgradeTier tier)
        {
            if (m_selectClip != null)
            {
                float pitch = tier switch
                {
                    UpgradeTier.Common => 0.9f,
                    UpgradeTier.Rare => 1.0f,
                    UpgradeTier.Epic => 1.1f,
                    UpgradeTier.Legendary => 1.2f,
                    _ => 1.0f
                };

                m_audioSource.pitch = pitch;
                m_audioSource.PlayOneShot(m_selectClip, m_masterVolume);
                m_audioSource.pitch = 1.0f;
            }
        }

        public void PlayAppliedSound()
        {
            if (m_appliedClip != null)
            {
                m_audioSource.PlayOneShot(m_appliedClip, m_masterVolume * 0.8f);
            }
        }
    }
}
