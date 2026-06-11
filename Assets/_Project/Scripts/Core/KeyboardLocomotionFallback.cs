using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace TheBreathlessStudyRoom.Core
{
    /// <summary>
    /// Provides keyboard (WASD) movement and mouse-look fallbacks when playing in the Unity Editor or Desktop build without a VR headset.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("The Breathless Study Room/Core/Keyboard Locomotion Fallback")]
    public class KeyboardLocomotionFallback : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Walking speed on keyboard.")]
        public float moveSpeed = 3f;

        [Tooltip("Sensitivity of mouse look.")]
        public float mouseSensitivity = 2f;

        [Header("Camera Linkage")]
        [Tooltip("Reference to the main camera transform for orientation direction.")]
        public Transform cameraTransform;

        private CharacterController _characterController;
        private float _rotationX = 0f;
        private float _rotationY = 0f;

        private void Start()
        {
            _characterController = GetComponent<CharacterController>();
            
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main != null ? Camera.main.transform : null;
            }

            // Sync initial rotations if camera is available
            if (cameraTransform != null)
            {
                _rotationY = transform.localEulerAngles.y;
                _rotationX = cameraTransform.localEulerAngles.x;
            }
        }

        private void Update()
        {
            // Only enable keyboard/mouse controls if VR headset is NOT connected or active
            if (IsXRHeadsetActive())
            {
                return;
            }

            HandleMouseLook();
            HandleKeyboardMovement();
        }

        private bool IsXRHeadsetActive()
        {
            var xrDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, xrDevices);
            
            foreach (var device in xrDevices)
            {
                if (device.isValid)
                {
                    // If a head-mounted device is valid, defer to VR HMD and joysticks
                    return true;
                }
            }
            return false;
        }

        private void HandleKeyboardMovement()
        {
            float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right
            float vertical = Input.GetAxis("Vertical");     // W/S or Up/Down

            if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
            {
                // Move relative to the camera direction
                Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
                Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;

                // Keep movement on the horizontal plane
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;
                
                // Add gravity fallback so player stays on floor
                float gravity = -9.81f;
                moveDirection.y = gravity * Time.deltaTime;

                _characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
            }
            else
            {
                // Apply gravity even when static
                _characterController.Move(new Vector3(0f, -9.81f * Time.deltaTime, 0f));
            }
        }

        private void HandleMouseLook()
        {
            // Rotate camera when holding Left Mouse Button or Right Mouse Button
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

                _rotationY += mouseX;
                _rotationX -= mouseY;
                _rotationX = Mathf.Clamp(_rotationX, -80f, 80f);

                // Rotate the player body horizontally
                transform.localRotation = Quaternion.Euler(0f, _rotationY, 0f);

                // Rotate the camera vertically
                if (cameraTransform != null)
                {
                    cameraTransform.localRotation = Quaternion.Euler(_rotationX, 0f, 0f);
                }
            }
        }
    }
}
