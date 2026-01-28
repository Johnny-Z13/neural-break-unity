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
        [SerializeField] private AudioSource _audioSource;

        [Header("Audio Clips (Optional)")]
        [SerializeField] private AudioClip _screenOpenClip;
        [SerializeField] private AudioClip _hoverClip;
        [SerializeField] private AudioClip _selectClip;
        [SerializeField] private AudioClip _appliedClip;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 0.7f;

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D sound
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
            if (_screenOpenClip != null)
            {
                _audioSource.PlayOneShot(_screenOpenClip, _masterVolume);
            }
        }

        public void PlayHoverSound()
        {
            if (_hoverClip != null)
            {
                _audioSource.PlayOneShot(_hoverClip, _masterVolume * 0.3f);
            }
        }

        public void PlaySelectSound(UpgradeTier tier)
        {
            if (_selectClip != null)
            {
                float pitch = tier switch
                {
                    UpgradeTier.Common => 0.9f,
                    UpgradeTier.Rare => 1.0f,
                    UpgradeTier.Epic => 1.1f,
                    UpgradeTier.Legendary => 1.2f,
                    _ => 1.0f
                };

                _audioSource.pitch = pitch;
                _audioSource.PlayOneShot(_selectClip, _masterVolume);
                _audioSource.pitch = 1.0f;
            }
        }

        public void PlayAppliedSound()
        {
            if (_appliedClip != null)
            {
                _audioSource.PlayOneShot(_appliedClip, _masterVolume * 0.8f);
            }
        }
    }
}
