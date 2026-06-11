using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace TheBreathlessStudyRoom.Core
{
    /// <summary>
    /// Provides helper methods to recenter the VR camera and recalibrate the floor tracking origin.
    /// </summary>
    [AddComponentMenu("The Breathless Study Room/Core/VR Origin Recenterer")]
    public class VROriginRecenterer : MonoBehaviour
    {
        private void Start()
        {
            Recenter();
        }

        private void Update()
        {
            // Allow manual recenter with keyboard 'R' for easy testing
            if (Input.GetKeyDown(KeyCode.R))
            {
                Recenter();
            }
        }

        /// <summary>
        /// Attempts to recenter the XR tracking origin and floor boundary.
        /// </summary>
        public void Recenter()
        {
            var inputSubsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(inputSubsystems);
            
            bool success = false;
            foreach (var subsystem in inputSubsystems)
            {
                if (subsystem.TryRecenter())
                {
                    success = true;
                }
            }

            if (success)
            {
                Debug.Log("[VROriginRecenterer] Successfully recentered XR floor tracking origin.");
            }
            else
            {
                Debug.LogWarning("[VROriginRecenterer] Could not recenter XR floor tracking origin (no active XR input subsystems found).");
            }
        }
    }
}
