using UnityEngine;

namespace TheBreathlessStudyRoom.Core
{
    /// <summary>
    /// 當玩家卡關(一定時間沒有進展)時，透過音效與微弱的燈光閃爍引導玩家看向當前目標。
    /// </summary>
    public class HintSystem : MonoBehaviour
    {
        public static HintSystem Instance;

        public Transform currentTarget;
        [Tooltip("閒置多久後觸發提示 (秒)")]
        public float timeToHint = 30f;
        
        [Header("Hint Cues")]
        public AudioSource hintAudioSource;
        [Tooltip("提示音效，建議使用微弱的電流滋滋聲")]
        public AudioClip hintStaticClip;
        public Color hintLightColor = new Color(0.8f, 0.9f, 1f); // 幽微的冷白光
        
        private float _idleTimer = 0f;

        [Header("目標節點 (Sequence Targets)")]
        public Transform flashlightTarget;
        public Transform blackboardTarget;
        public Transform doorTarget;

        private void Awake()
        {
            Instance = this;
            SetTarget(flashlightTarget); // 一開始的目標是手電筒
        }

        private void Update()
        {
            if (currentTarget == null) return;

            _idleTimer += Time.deltaTime;

            if (_idleTimer >= timeToHint)
            {
                TriggerHint();
                _idleTimer = 0f; // 重新計時，避免瘋狂閃爍
            }
        }

        public void SetTarget(Transform newTarget)
        {
            currentTarget = newTarget;
            _idleTimer = 0f; 
            Debug.Log($"[HintSystem] 當前提示目標已更新為: {(newTarget != null ? newTarget.name : "無")}");
        }

        public void ResetTimer()
        {
            _idleTimer = 0f;
        }

        private void TriggerHint()
        {
            if (currentTarget == null) return;

            // 1. 播放空間音效吸引轉頭
            if (hintAudioSource != null && hintStaticClip != null)
            {
                hintAudioSource.transform.position = currentTarget.position;
                hintAudioSource.PlayOneShot(hintStaticClip);
            }
            
            // 2. 視覺提示：在目標處閃爍微光
            Light existingLight = currentTarget.GetComponentInChildren<Light>();
            if (existingLight == null)
            {
                StartCoroutine(BlinkHintLight(currentTarget.position + Vector3.up * 0.3f));
            }
            else
            {
                // 如果目標本來就會發光（例如手電筒），直接讓它閃爍
                StartCoroutine(BlinkExistingLight(existingLight));
            }
        }

        private System.Collections.IEnumerator BlinkHintLight(Vector3 pos)
        {
            GameObject lightObj = new GameObject("HintTempLight");
            lightObj.transform.position = pos;
            Light hintLight = lightObj.AddComponent<Light>();
            hintLight.type = LightType.Point;
            hintLight.range = 3f;
            hintLight.intensity = 0f;
            hintLight.color = hintLightColor;

            // 淡入
            float duration = 0.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                hintLight.intensity = Mathf.Lerp(0f, 1.5f, elapsed / duration);
                yield return null;
            }

            // 急促閃爍三次
            for(int i=0; i<3; i++)
            {
                hintLight.intensity = 0.2f;
                yield return new WaitForSeconds(0.1f);
                hintLight.intensity = 1.5f;
                yield return new WaitForSeconds(0.1f);
            }

            // 淡出並銷毀
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                hintLight.intensity = Mathf.Lerp(1.5f, 0f, elapsed / duration);
                yield return null;
            }

            Destroy(lightObj);
        }

        private System.Collections.IEnumerator BlinkExistingLight(Light targetLight)
        {
            float originalIntensity = targetLight.intensity;
            for(int i=0; i<3; i++)
            {
                targetLight.intensity = originalIntensity * 0.2f;
                yield return new WaitForSeconds(0.1f);
                targetLight.intensity = originalIntensity * 1.5f;
                yield return new WaitForSeconds(0.1f);
            }
            targetLight.intensity = originalIntensity;
        }
    }
}
