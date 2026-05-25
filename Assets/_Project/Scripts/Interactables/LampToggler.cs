using UnityEngine;
using TheBreathlessStudyRoom.Core;

namespace TheBreathlessStudyRoom.Interactables
{
    /// <summary>
    /// Lightweight runtime behavior that listens to look events on the desk lamp,
    /// toggles visual lights and registers compliance vs rebellion status logs.
    /// </summary>
    [AddComponentMenu("The Breathless Study Room/Interactables/Lamp Toggler")]
    public class LampToggler : MonoBehaviour
    {
        public Light TargetLight;
        public TimelineManager TimelineManager;

        public void ToggleDeskLamp()
        {
            if (TargetLight == null) return;
            
            TargetLight.enabled = !TargetLight.enabled;
            Debug.Log($"[LampToggler] Desk Lamp toggled! Now: {(TargetLight.enabled ? "ON" : "OFF")}");

            // Play light toggle click
            var audioMgr = FindObjectOfType<AudioManager>();
            if (audioMgr != null)
            {
                audioMgr.PlayBroadcastChime(); // Click indicator
            }

            if (TimelineManager != null)
            {
                if (TimelineManager.CurrentState == GameState.Scene1StudyRoom)
                {
                    if (TargetLight.enabled)
                    {
                        TimelineManager.LogRebellion(2);
                        TimelineManager.ModulateStress(-5f); // Calms down under light
                    }
                    else
                    {
                        TimelineManager.LogCompliance(1);
                        TimelineManager.ModulateStress(10f); // Going dark induces panic
                    }
                }
            }
        }
    }
}
