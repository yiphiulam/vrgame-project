using UnityEngine;
using System.Collections;
using TheBreathlessStudyRoom.Core;

namespace TheBreathlessStudyRoom.Interactables
{
    /// <summary>
    /// Runtime component to pivot open the locker door on gaze.
    /// </summary>
    [AddComponentMenu("The Breathless Study Room/Interactables/Locker Door Opener")]
    public class LockerDoorOpener : MonoBehaviour
    {
        public Transform targetPivot;
        public GameObject secretNote;
        private bool _isOpened = false;

        public void OpenLockerDoor()
        {
            if (_isOpened || targetPivot == null) return;
            _isOpened = true;
            
            StartCoroutine(RotatePivotCoroutine());
            
            var audioMgr = FindObjectOfType<AudioManager>();
            if (audioMgr != null)
            {
                audioMgr.PlayDoorSqueak();
            }

            Debug.Log("[LockerDoorOpener] Locker door opened by player gaze!");
        }

        private IEnumerator RotatePivotCoroutine()
        {
            float elapsed = 0f;
            float duration = 1.2f;
            Quaternion startRot = targetPivot.localRotation;
            Quaternion endRot = Quaternion.Euler(0f, 110f, 0f); 

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                targetPivot.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
                yield return null;
            }
            targetPivot.localRotation = endRot;
        }
    }
}
