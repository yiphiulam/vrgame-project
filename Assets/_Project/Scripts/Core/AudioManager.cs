using System.Collections;
using UnityEngine;

namespace TheBreathlessStudyRoom.Core
{
    /// <summary>
    /// Handles 3D spatialized horror sound assets, procedural scares,
    /// and dynamic heartbeat pacing tied directly to player stress metrics.
    /// </summary>
    [AddComponentMenu("The Breathless Study Room/Core/Audio Manager")]
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Asset Clips")]
        [SerializeField] private AudioClip _ambientDroneClip;
        [SerializeField] private AudioClip _heartbeatClip;
        [SerializeField] private AudioClip _clockTickClip;
        [SerializeField] private AudioClip _windowTapClip;
        [SerializeField] private AudioClip _scareChordClip;
        [SerializeField] private AudioClip _broadcastBeepClip;
        [SerializeField] private AudioClip _radioStaticClip;
        [SerializeField] private AudioClip _doorSqueakClip;

        [Header("Spatial Direct Source Nodes")]
        [SerializeField] private AudioSource _headAudioSource; // Connected directly to player head (non-spatialized)
        [SerializeField] private AudioSource _ambientSource; // Non-spatial background ambient source
        [SerializeField] private AudioSource _windowSource; // Spatial source placed outside left window
        [SerializeField] private AudioSource _doorSource; // Spatial source placed at back classroom door
        [SerializeField] private AudioSource _broadcastSource; // Spatial source placed at PA speaker
        
        [Header("Timeline Linkage")]
        [SerializeField] private TimelineManager _timelineManager;

        private float _heartbeatTimer = 0f;
        private bool _isHeartbeatActive = false;

        private void Start()
        {
            if (_timelineManager != null)
            {
                _timelineManager.OnStateTransition += HandleStateAudioUpdate;
                _timelineManager.OnTimelineTrigger += HandleTimelineAudioTrigger;
            }

            PlayAmbientHum();
            StartHeartbeat();
        }

        private void Update()
        {
            HandleDynamicHeartbeat();
        }

        private void OnDestroy()
        {
            if (_timelineManager != null)
            {
                _timelineManager.OnStateTransition -= HandleStateAudioUpdate;
                _timelineManager.OnTimelineTrigger -= HandleTimelineAudioTrigger;
            }
        }

        /// <summary>
        /// Initiates the sub-bass drone loop.
        /// </summary>
        public void PlayAmbientHum()
        {
            if (_ambientSource == null || _ambientDroneClip == null) return;
            
            _ambientSource.clip = _ambientDroneClip;
            _ambientSource.loop = true;
            _ambientSource.volume = 0.35f;
            _ambientSource.Play();
        }

        /// <summary>
        /// Unlocks the heartbeat loop.
        /// </summary>
        public void StartHeartbeat()
        {
            _isHeartbeatActive = true;
            _heartbeatTimer = 0f;
        }

        /// <summary>
        /// Computes interval delay to dynamically scale heartbeat beat speed with player stress:
        /// bpm = 65 + (stress * 0.95).
        /// </summary>
        private void HandleDynamicHeartbeat()
        {
            if (!_isHeartbeatActive || _headAudioSource == null || _heartbeatClip == null) return;

            float currentStress = _timelineManager != null ? _timelineManager.StressLevel : 0f;
            float bpm = 65f + (currentStress * 0.95f);
            float beatInterval = 60f / bpm;

            _heartbeatTimer += Time.deltaTime;

            if (_heartbeatTimer >= beatInterval)
            {
                _heartbeatTimer = 0f;
                PlayDoubleHeartbeat(currentStress);
            }
        }

        /// <summary>
        /// Emulates real double-beat (lub-dub) audio trigger pattern.
        /// </summary>
        private void PlayDoubleHeartbeat(float stress)
        {
            float maxVolume = 0.2f + (stress / 100f) * 0.6f;
            
            // Play "lub" beat
            _headAudioSource.PlayOneShot(_heartbeatClip, maxVolume);
            
            // Queue "dub" beat shortly after (180ms delay)
            StartCoroutine(TriggerDelayedHeartbeat(0.18f, maxVolume * 0.7f));
        }

        private IEnumerator TriggerDelayedHeartbeat(float delay, float volume)
        {
            yield return new WaitForSeconds(delay);
            if (_isHeartbeatActive && _headAudioSource != null && _heartbeatClip != null)
            {
                _headAudioSource.PlayOneShot(_heartbeatClip, volume);
            }
        }

        /// <summary>
        /// Coordinates sound triggers dynamically.
        /// </summary>
        private void HandleTimelineAudioTrigger(string eventTag)
        {
            switch (eventTag)
            {
                case "WINDOW_TAP_TRIGGER":
                    PlayWindowTaps();
                    break;
                case "BLACKBOARD_CONFLICT_TRIGGER":
                    PlayStaticBurst(1.2f, 0.4f);
                    break;
                case "BROADCAST_PATROL_TRIGGER":
                    PlayBroadcastChime();
                    break;
                case "DOOR_UNLOCK_TRIGGER":
                    PlayDoorSqueak();
                    break;
                case "SANITY_CRUSH_ENDING":
                    PlayJumpscareChord();
                    break;
            }
        }

        private void HandleStateAudioUpdate(GameState state)
        {
            if (state == GameState.Ending)
            {
                _isHeartbeatActive = false;
                if (_ambientSource != null) _ambientSource.Stop();
            }
        }

        /// <summary>
        /// Triggers spatial window knock knocks.
        /// </summary>
        public void PlayWindowTaps()
        {
            if (_windowSource != null && _windowTapClip != null)
            {
                StartCoroutine(TriggerTripleTaps());
            }
        }

        private IEnumerator TriggerTripleTaps()
        {
            for (int i = 0; i < 3; i++)
            {
                _windowSource.PlayOneShot(_windowTapClip, 0.75f);
                yield return new WaitForSeconds(0.14f);
            }
        }

        /// <summary>
        /// Plays spatial door unlocking squeak.
        /// </summary>
        public void PlayDoorSqueak()
        {
            if (_doorSource != null && _doorSqueakClip != null)
            {
                _doorSource.PlayOneShot(_doorSqueakClip, 0.65f);
            }
        }

        /// <summary>
        /// Broadcasts high frequency notifications.
        /// </summary>
        public void PlayBroadcastChime()
        {
            if (_broadcastSource != null && _broadcastBeepClip != null)
            {
                _broadcastSource.PlayOneShot(_broadcastBeepClip, 0.8f);
            }
        }

        /// <summary>
        /// Fires discordant jumpscare feedback chords.
        /// </summary>
        public void PlayJumpscareChord()
        {
            if (_headAudioSource != null && _scareChordClip != null)
            {
                _headAudioSource.PlayOneShot(_scareChordClip, 0.9f);
            }
            PlayStaticBurst(2.0f, 0.6f);
        }

        /// <summary>
        /// Triggers crackly radio noise burst.
        /// </summary>
        public void PlayStaticBurst(float duration, float volume)
        {
            if (_headAudioSource != null && _radioStaticClip != null)
            {
                StartCoroutine(TriggerStaticNoiseLoop(duration, volume));
            }
        }

        private IEnumerator TriggerStaticNoiseLoop(float duration, float volume)
        {
            // Use secondary source to loop static cleanly
            GameObject tempStatic = new GameObject("TempStaticAudio");
            AudioSource source = tempStatic.AddComponent<AudioSource>();
            source.clip = _radioStaticClip;
            source.spatialBlend = 0f; // 2D flat
            source.volume = volume;
            source.loop = true;
            source.Play();

            yield return new WaitForSeconds(duration);

            source.Stop();
            Destroy(tempStatic);
        }
    }
}
