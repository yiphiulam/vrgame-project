using UnityEngine;
using System.Collections;

namespace TheBreathlessStudyRoom.Interactables
{
    /// <summary>
    /// 自動偵測玩家手電筒照射前方是否有帶有特定標籤（例如 "Clue"）的物件。
    /// 若有，則自動將聚光燈強度平滑降低、邊緣稍微放大，避免紙張反光過曝。
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class FlashlightAntiGlare : MonoBehaviour
    {
        [Header("Raycast 偵測設定")]
        [Tooltip("射線的起點，通常是攝影機或是手電筒本身的 Transform。")]
        public Transform rayOrigin;
        [Tooltip("射線最遠能偵測的距離。")]
        public float maxRayDistance = 10f;
        [Tooltip("射線只會偵測特定圖層，可設定以優化效能。")]
        public LayerMask interactableLayer = ~0;
        [Tooltip("需要觸發防眩光效果的物件標籤。")]
        public string clueTag = "Clue";

        [Header("燈光漸變設定")]
        [Tooltip("燈光漸變所需的平滑時間（秒）。")]
        public float transitionDuration = 0.5f;
        [Tooltip("要降低的強度比例（0.3 = 減少 30%）。")]
        [Range(0f, 1f)] public float intensityReductionPct = 0.3f;
        [Tooltip("當照到紙條時，聚光燈角度放大的度數。")]
        public float angleIncreaseAmount = 10f;

        private Light _flashlight;
        private float _originalIntensity;
        private float _originalSpotAngle;

        private float _targetIntensity;
        private float _targetSpotAngle;
        
        private Coroutine _transitionCoroutine;
        private bool _isLookingAtClue = false;

        private void Awake()
        {
            _flashlight = GetComponent<Light>();
            // 記錄手電筒初始的強度與角度
            _originalIntensity = _flashlight.intensity;
            _originalSpotAngle = _flashlight.spotAngle;

            _targetIntensity = _originalIntensity;
            _targetSpotAngle = _originalSpotAngle;

            if (rayOrigin == null) rayOrigin = transform;
        }

        private void Update()
        {
            CheckForClue();
        }

        private void CheckForClue()
        {
            bool hitClue = false;

            // 從起點發射射線
            if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, maxRayDistance, interactableLayer))
            {
                // 如果打到帶有特定標籤的碰撞體
                if (hit.collider.CompareTag(clueTag))
                {
                    hitClue = true;
                }
            }

            // 狀態切換判斷
            if (hitClue && !_isLookingAtClue)
            {
                _isLookingAtClue = true;
                // 目標亮度減少 30%，目標角度增加
                float newIntensity = _originalIntensity * (1f - intensityReductionPct);
                float newAngle = _originalSpotAngle + angleIncreaseAmount;
                StartLightTransition(newIntensity, newAngle);
            }
            else if (!hitClue && _isLookingAtClue)
            {
                _isLookingAtClue = false;
                // 移開視線，恢復原始數值
                StartLightTransition(_originalIntensity, _originalSpotAngle);
            }
        }

        private void StartLightTransition(float targetIntensity, float targetAngle)
        {
            // 如果已經有漸變正在進行，先停止它
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }
            
            _targetIntensity = targetIntensity;
            _targetSpotAngle = targetAngle;
            _transitionCoroutine = StartCoroutine(TransitionLightRoutine());
        }

        private IEnumerator TransitionLightRoutine()
        {
            float startIntensity = _flashlight.intensity;
            float startAngle = _flashlight.spotAngle;
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                // 使用 SmoothStep 讓過渡在起點與終點更為柔和
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

                _flashlight.intensity = Mathf.Lerp(startIntensity, _targetIntensity, t);
                _flashlight.spotAngle = Mathf.Lerp(startAngle, _targetSpotAngle, t);

                yield return null;
            }

            // 確保最終精準對齊目標數值
            _flashlight.intensity = _targetIntensity;
            _flashlight.spotAngle = _targetSpotAngle;
        }
    }
}
