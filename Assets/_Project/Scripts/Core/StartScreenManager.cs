using UnityEngine;
using TheBreathlessStudyRoom.Core;

namespace TheBreathlessStudyRoom.Core
{
    /// <summary>
    /// Manages the Start Screen billboard, handling player transition from the start menu to the classroom (Scene 1).
    /// </summary>
    [AddComponentMenu("The Breathless Study Room/Core/Start Screen Manager")]
    public class StartScreenManager : MonoBehaviour
    {
        [Header("Start Screen Panel UI")]
        [Tooltip("The parent GameObject of the start screen billboard to disable when entering the classroom.")]
        public GameObject startScreenBillboardPanel;

        [Header("Timeline Linkage")]
        [Tooltip("Reference to the TimelineManager to begin the game clock and mechanics.")]
        public TimelineManager timelineManager;

        private void Start()
        {
            if (timelineManager == null)
            {
                timelineManager = FindObjectOfType<TimelineManager>();
            }

            // Ensure start screen is active initially
            if (startScreenBillboardPanel != null)
            {
                startScreenBillboardPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Starts the game timeline and deactivates the start billboard.
        /// </summary>
        public void EnterClassroom()
        {
            // Play sound feedback
            var audioManager = FindObjectOfType<AudioManager>();
            if (audioManager != null)
            {
                audioManager.PlayDoorSqueak(); // Reuse audio source click feedback
            }

            if (timelineManager != null)
            {
                timelineManager.StartTimeline();
            }

            if (startScreenBillboardPanel != null)
            {
                startScreenBillboardPanel.SetActive(false);
            }

            Debug.Log("[StartScreenManager] EnterClassroom triggered. Timeline started.");
        }
    }
}
