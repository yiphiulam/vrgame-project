using UnityEngine;
using System.Collections;

namespace TheBreathlessStudyRoom.Interactables
{
    /// <summary>
    /// 環境驚嚇邏輯：當玩家背對門口且手電筒開啟時，有機率觸發門牌掉落、音效與走廊燈光閃爍。
    /// 觸發後具備冷卻時間機制。
    /// </summary>
    public class DoorSignScare : MonoBehaviour
    {
        [Header("物件參考 (References)")]
        [Tooltip("玩家的攝影機 (XR Origin -> Main Camera)")]
        public Transform playerCamera;
        [Tooltip("門的 Transform (用來計算 Forward 向量)")]
        public Transform doorTransform;
        [Tooltip("玩家的手電筒光源 (用來判定是否開啟)")]
        public Light playerFlashlight;
        [Tooltip("門牌的 Rigidbody (需提前掛載並設為 isKinematic = true)")]
        public Rigidbody doorSignRigidbody;
        [Tooltip("播放掉落撞擊音效的 AudioSource")]
        public AudioSource impactAudioSource;
        [Tooltip("撞擊音效檔案")]
        public AudioClip dropCrashSound;
        [Tooltip("走廊上需要閃爍的燈光陣列")]
        public Light[] corridorLights;

        [Header("觸發設定 (Settings)")]
        [Tooltip("觸發機率 (0.0 到 1.0 之間，0.3 代表 30%)")]
        [Range(0f, 1f)] public float triggerProbability = 0.3f;
        [Tooltip("冷卻時間 (秒)，5 分鐘 = 300 秒")]
        public float cooldownTime = 300f;
        [Tooltip("背對門口的角度閾值 (大於 120 度視為背對)")]
        public float backFacingAngleThreshold = 120f;

        // 紀錄最後一次觸發的時間 (初始設為一個很小的值確保遊戲一開始可以觸發)
        private float _lastTriggerTime = -9999f;
        // 記錄上一幀是否處於背對狀態，確保只有在"轉身"的瞬間判定一次機率
        private bool _wasFacingBack = false;

        private void Update()
        {
            if (playerCamera == null || doorTransform == null) return;

            // 1. 檢查是否在冷卻時間內
            if (Time.time - _lastTriggerTime < cooldownTime) return;

            // 2. 檢查手電筒是否開啟
            if (playerFlashlight != null && !playerFlashlight.enabled)
            {
                _wasFacingBack = false; // 沒開手電筒就不算數
                return;
            }

            // 3. 計算攝影機前方與門的前方夾角
            float angle = Vector3.Angle(playerCamera.forward, doorTransform.forward);
            bool isFacingBack = angle > backFacingAngleThreshold;

            // 4. 邊緣觸發判定 (只有在剛轉過身背對的那個瞬間，才擲骰子判定機率)
            if (isFacingBack && !_wasFacingBack)
            {
                RollForScare();
            }

            _wasFacingBack = isFacingBack;
        }

        private void RollForScare()
        {
            // 產生 0.0 到 1.0 的隨機數
            float roll = Random.value;
            if (roll <= triggerProbability)
            {
                TriggerScareEvent();
            }
        }

        private void TriggerScareEvent()
        {
            // 進入冷卻
            _lastTriggerTime = Time.time;

            // 掉落門牌
            if (doorSignRigidbody != null)
            {
                doorSignRigidbody.isKinematic = false;
                doorSignRigidbody.useGravity = true;
                // 給予一點向外推的微小力量，讓掉落更自然
                doorSignRigidbody.AddForce(doorTransform.forward * 0.5f, ForceMode.Impulse);
                // 給予一點隨機旋轉
                doorSignRigidbody.AddTorque(Random.insideUnitSphere * 1f, ForceMode.Impulse);
            }

            // 播放音效
            if (impactAudioSource != null && dropCrashSound != null)
            {
                impactAudioSource.PlayOneShot(dropCrashSound);
            }

            // 執行燈光閃爍協程
            if (corridorLights != null && corridorLights.Length > 0)
            {
                StartCoroutine(BlinkLightsRoutine());
            }
        }

        private IEnumerator BlinkLightsRoutine()
        {
            // 記錄所有燈光原始的開關狀態
            bool[] originalStates = new bool[corridorLights.Length];
            for (int i = 0; i < corridorLights.Length; i++)
            {
                if (corridorLights[i] != null)
                    originalStates[i] = corridorLights[i].enabled;
            }

            // 閃爍兩次
            for (int blink = 0; blink < 2; blink++)
            {
                // 燈光瞬間全滅
                SetLightsEnabled(false);
                yield return new WaitForSeconds(0.1f);
                
                // 燈光瞬間全亮
                SetLightsEnabled(true);
                yield return new WaitForSeconds(0.15f);
            }

            // 恢復所有燈光原始狀態
            for (int i = 0; i < corridorLights.Length; i++)
            {
                if (corridorLights[i] != null)
                    corridorLights[i].enabled = originalStates[i];
            }
        }

        private void SetLightsEnabled(bool state)
        {
            foreach (var l in corridorLights)
            {
                if (l != null) l.enabled = state;
            }
        }
    }
}
