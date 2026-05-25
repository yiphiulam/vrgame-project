using UnityEngine;

namespace TheBreathlessStudyRoom.Interactables
{
    /// <summary>
    /// Fallback controller for gaze calculations in desktop simulation play mode.
    /// </summary>
    [AddComponentMenu("The Breathless Study Room/Interactables/XR RayCast Gaze Fallback")]
    public class XRRayCastGazeFallback : MonoBehaviour
    {
        private void Update()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            {
                var target = hit.collider.GetComponent<GazeDwellSelector>();
                if (target != null)
                {
                    // Simulated hover trigger
                }
            }
        }
    }
}
