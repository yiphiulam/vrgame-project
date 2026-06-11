using UnityEngine;
using UnityEngine.InputSystem;
using TheBreathlessStudyRoom.Core;

namespace TheBreathlessStudyRoom.Interactables
{
    /// <summary>
    /// Handles the VR 'Close Eyes' action. Pressing any controller auxiliary button (A/B/X/Y)
    /// or Keyboard Spacebar covers the player's camera with a black quad, blacking out their vision
    /// and logging compliance/rebellion states during the administrator patrol broadcast.
    /// </summary>
    [AddComponentMenu("The Breathless Study Room/Interactables/Eye Close Manager")]
    public class EyeCloseManager : MonoBehaviour
    {
        [Header("Blackout Visuals")]
        [Tooltip("The mesh quad parented to the camera that is enabled to turn vision black.")]
        public GameObject blackoutQuad;

        [Header("Timeline Linkage")]
        [Tooltip("Reference to the global TimelineManager.")]
        public TimelineManager timelineManager;
        
        private InputAction _leftPrimary;
        private InputAction _leftSecondary;
        private InputAction _rightPrimary;
        private InputAction _rightSecondary;
        private InputAction _keyboardSpace;

        private bool _isEyesClosed = false;
        private bool _isPatrolActive = false;
        private bool _patrolChoiceLogged = false;

        private void Start()
        {
            if (timelineManager == null)
            {
                timelineManager = FindObjectOfType<TimelineManager>();
            }

            if (timelineManager != null)
            {
                timelineManager.OnTimelineTrigger += HandleTimelineTrigger;
            }

            // Configure InputSystem bindings for standard VR controller auxiliary buttons
            _leftPrimary = new InputAction(binding: "<XRController>{LeftHand}/primaryButton"); // X button
            _leftSecondary = new InputAction(binding: "<XRController>{LeftHand}/secondaryButton"); // Y button
            _rightPrimary = new InputAction(binding: "<XRController>{RightHand}/primaryButton"); // A button
            _rightSecondary = new InputAction(binding: "<XRController>{RightHand}/secondaryButton"); // B button
            _keyboardSpace = new InputAction(binding: "<Keyboard>/space"); // Desktop spacebar testing

            _leftPrimary.Enable();
            _leftSecondary.Enable();
            _rightPrimary.Enable();
            _rightSecondary.Enable();
            _keyboardSpace.Enable();

            if (blackoutQuad != null)
            {
                blackoutQuad.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (timelineManager != null)
            {
                timelineManager.OnTimelineTrigger -= HandleTimelineTrigger;
            }

            _leftPrimary?.Disable();
            _leftSecondary?.Disable();
            _rightPrimary?.Disable();
            _rightSecondary?.Disable();
            _keyboardSpace?.Disable();
        }

        private void HandleTimelineTrigger(string eventTag)
        {
            if (string.Equals(eventTag, "BROADCAST_PATROL_TRIGGER", System.StringComparison.OrdinalIgnoreCase))
            {
                _isPatrolActive = true;
                _patrolChoiceLogged = false;
                Debug.Log("[EyeCloseManager] Patrol Broadcast warning active. Stare at door or close eyes!");
                
                // If they don't close their eyes, we evaluate after the warning duration (10 seconds)
                Invoke("CheckPatrolChoiceTimeout", 10f);
            }
        }

        private void Update()
        {
            // Detect if any of the mapped buttons are currently held down
            bool shouldClose = IsActionPressed(_leftPrimary) || 
                               IsActionPressed(_leftSecondary) ||
                               IsActionPressed(_rightPrimary) || 
                               IsActionPressed(_rightSecondary) ||
                               IsActionPressed(_keyboardSpace);

            if (shouldClose && !_isEyesClosed)
            {
                _isEyesClosed = true;
                if (blackoutQuad != null) blackoutQuad.SetActive(true);
                Debug.Log("[EyeCloseManager] Eyes CLOSED.");

                if (_isPatrolActive && !_patrolChoiceLogged)
                {
                    // Player closed eyes -> obeyed the broadcast rule, rebelled against blackboard rules
                    _patrolChoiceLogged = true;
                    if (timelineManager != null)
                    {
                        timelineManager.LogCompliance(1);
                        Debug.Log("[EyeCloseManager] Climax Choice: Player closed eyes (followed broadcast instructions).");
                    }
                }
            }
            else if (!shouldClose && _isEyesClosed)
            {
                _isEyesClosed = false;
                if (blackoutQuad != null) blackoutQuad.SetActive(false);
                Debug.Log("[EyeCloseManager] Eyes OPENED.");
            }
        }

        private void CheckPatrolChoiceTimeout()
        {
            if (_isPatrolActive && !_patrolChoiceLogged)
            {
                _isPatrolActive = false;
                _patrolChoiceLogged = true;
                
                // If they didn't close eyes, they kept them open (rebelled against broadcast)
                if (timelineManager != null)
                {
                    timelineManager.LogRebellion(1);
                    Debug.Log("[EyeCloseManager] Climax Choice: Player kept eyes open (rebelled against broadcast instructions).");
                }
            }
        }

        private bool IsActionPressed(InputAction action)
        {
            if (action == null) return false;
            try
            {
                return action.ReadValue<float>() > 0.5f;
            }
            catch
            {
                return false;
            }
        }
    }
}
