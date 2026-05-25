using UnityEngine;

namespace TheBreathlessStudyRoom.Core
{
    /// <summary>
    /// Controls the active/inactive state of the Classroom, Corridor, and Guard Room environments at runtime,
    /// and teleports the player's XR Origin rig to the correct coordinate starting point for each scene transition.
    /// </summary>
    [AddComponentMenu("The Breathless Study Room/Core/Environment State Controller")]
    public class EnvironmentStateController : MonoBehaviour
    {
        [Header("Environment Parents")]
        [Tooltip("The parent GameObject containing all elements of the Study Room / Classroom.")]
        public GameObject classroomParent;

        [Tooltip("The parent GameObject containing all elements of the Corridor.")]
        public GameObject corridorParent;

        [Tooltip("The parent GameObject containing all elements of the Guard Room.")]
        public GameObject guardRoomParent;

        [Header("Player Tracking Rig")]
        [Tooltip("The XR Origin or player camera rig transform to teleport on scene transitions.")]
        public Transform playerRig;

        [Header("Timeline Linkage")]
        [SerializeField] private TimelineManager _timelineManager;

        private void Start()
        {
            if (_timelineManager == null)
            {
                _timelineManager = GetComponent<TimelineManager>();
            }

            if (_timelineManager != null)
            {
                _timelineManager.OnStateTransition += HandleStateTransition;
            }

            // Set initial state based on timeline manager's current state
            if (_timelineManager != null)
            {
                HandleStateTransition(_timelineManager.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (_timelineManager != null)
            {
                _timelineManager.OnStateTransition -= HandleStateTransition;
            }
        }

        /// <summary>
        /// Responds to state changes from the Timeline Manager, enabling the active room and teleporting the player.
        /// </summary>
        public void HandleStateTransition(GameState newState)
        {
            switch (newState)
            {
                case GameState.Start:
                case GameState.Scene1StudyRoom:
                    SetActiveEnvironments(true, false, false);
                    TeleportPlayer(new Vector3(0f, 0f, 0f), Quaternion.identity);
                    break;

                case GameState.Scene2Corridor:
                    SetActiveEnvironments(false, true, false);
                    // A-Frame Z=4 corresponds to Unity Z=-4. Face towards positive Z (+Z is forward/exit door).
                    TeleportPlayer(new Vector3(0f, 0f, -4f), Quaternion.identity);
                    break;

                case GameState.Scene3GuardRoom:
                    SetActiveEnvironments(false, false, true);
                    // Face towards positive Z (CCTV screens are at Z=1.95).
                    TeleportPlayer(new Vector3(0f, 0f, 0f), Quaternion.identity);
                    break;

                case GameState.Ending:
                    // Keep the current environment active during the ending sequence
                    break;
            }
        }

        private void SetActiveEnvironments(bool classroomActive, bool corridorActive, bool guardRoomActive)
        {
            if (classroomParent != null) classroomParent.SetActive(classroomActive);
            if (corridorParent != null) corridorParent.SetActive(corridorActive);
            if (guardRoomParent != null) guardRoomParent.SetActive(guardRoomActive);
            
            Debug.Log($"[EnvironmentStateController] Environment visibility updated - Classroom: {classroomActive}, Corridor: {corridorActive}, GuardRoom: {guardRoomActive}");
        }

        private void TeleportPlayer(Vector3 targetPosition, Quaternion targetRotation)
        {
            if (playerRig == null)
            {
                // Auto-fallback search if not linked
                var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
                if (origin != null)
                {
                    playerRig = origin.transform;
                }
                else
                {
                    var mainCam = Camera.main;
                    if (mainCam != null) playerRig = mainCam.transform;
                }
            }

            if (playerRig != null)
            {
                playerRig.position = targetPosition;
                playerRig.rotation = targetRotation;
                Debug.Log($"[EnvironmentStateController] Teleported player rig to {targetPosition}");
            }
            else
            {
                Debug.LogWarning("[EnvironmentStateController] Cannot teleport player: Player Rig reference is null.");
            }
        }
    }
}
