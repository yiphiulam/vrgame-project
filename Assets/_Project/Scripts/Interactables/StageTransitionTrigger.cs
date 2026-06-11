using UnityEngine;
using TheBreathlessStudyRoom.Core;

namespace TheBreathlessStudyRoom.Interactables
{
    /// <summary>
    /// Handles transitioning the player to a target GameState when interacted with,
    /// optionally checking if the classroom exit is unlocked.
    /// </summary>
    [AddComponentMenu("The Breathless Study Room/Interactables/Stage Transition Trigger")]
    public class StageTransitionTrigger : MonoBehaviour
    {
        [Header("Transition Target")]
        [Tooltip("The state to transition to when triggered.")]
        public GameState targetState;

        [Header("Classroom Locking Rules")]
        [Tooltip("If true, checks if the door is unlocked via DOOR_UNLOCK_TRIGGER before transitioning.")]
        public bool checkClassroomLock = false;

        [Header("Timeline Linkage")]
        [SerializeField] private TimelineManager _timelineManager;

        private bool _isClassroomUnlocked = false;

        private void Start()
        {
            if (_timelineManager == null)
            {
                _timelineManager = FindObjectOfType<TimelineManager>();
            }

            if (_timelineManager != null)
            {
                _timelineManager.OnTimelineTrigger += HandleTimelineTrigger;
            }
        }

        private void OnDestroy()
        {
            if (_timelineManager != null)
            {
                _timelineManager.OnTimelineTrigger -= HandleTimelineTrigger;
            }
        }

        private void HandleTimelineTrigger(string eventTag)
        {
            if (eventTag == "DOOR_UNLOCK_TRIGGER")
            {
                _isClassroomUnlocked = true;
                Debug.Log("[StageTransitionTrigger] Classroom exit is now UNLOCKED.");
            }
        }

        /// <summary>
        /// Call this method to execute the scene state transition.
        /// Can be hooked up to GazeDwellSelector.OnDwellSelected or XRI Select events.
        /// </summary>
        public void Transition()
        {
            if (_timelineManager == null)
            {
                _timelineManager = FindObjectOfType<TimelineManager>();
                if (_timelineManager == null)
                {
                    Debug.LogError("[StageTransitionTrigger] Cannot transition: TimelineManager not found!");
                    return;
                }
            }

            // Check classroom lock condition
            if (checkClassroomLock && _timelineManager.CurrentState == GameState.Scene1StudyRoom && !_isClassroomUnlocked)
            {
                Debug.LogWarning("[StageTransitionTrigger] Classroom exit door is still locked. You must wait for the door unlock event!");
                return;
            }

            Debug.Log($"[StageTransitionTrigger] Transitioning from {_timelineManager.CurrentState} to {targetState}...");
            
            // Play door squeak sound via AudioManager
            var audioManager = FindObjectOfType<AudioManager>();
            if (audioManager != null)
            {
                audioManager.PlayDoorSqueak();
            }

            _timelineManager.SetGameState(targetState);
        }
    }
}
