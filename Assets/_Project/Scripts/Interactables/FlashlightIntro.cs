using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TheBreathlessStudyRoom.Interactables
{
    /// <summary>
    /// 控制遊戲開場時手電筒的呼吸燈效果，以及注視與拿取時的互動邏輯。
    /// </summary>
    public class FlashlightIntro : MonoBehaviour
    {
        [Header("呼吸燈設定 (Breathing Light)")]
        [Tooltip("手電筒的 Mesh Renderer")]
        public MeshRenderer flashlightRenderer;
        [Tooltip("發光材質在 Renderer 陣列中的索引")]
        public int materialIndex = 0;
        [Tooltip("呼吸燈的基礎發光顏色")]
        [ColorUsage(true, true)] public Color emissionBaseColor = new Color(1f, 0.8f, 0.2f);
        public float breathingSpeed = 2f;
        public float minEmission = 0.1f;
        public float maxEmission = 1.0f;

        [Header("音效設定 (Audio)")]
        [Tooltip("播放電流聲的 AudioSource")]
        public AudioSource electricSoundSource;
        public AudioClip electricHumClip;

        [Header("事件觸發 (Events)")]
        [Tooltip("當手電筒被拿起時，會觸發此事件 (可連接到黑板文字浮現)")]
        public UnityEvent onFlashlightGrabbedAction;

        private Material _flashlightMaterial;
        private bool _isBreathing = true;
        private XRGrabInteractable _grabInteractable;

        private void Awake()
        {
            if (flashlightRenderer != null)
            {
                // 實例化材質以避免影響全域資源
                _flashlightMaterial = flashlightRenderer.materials[materialIndex];
                _flashlightMaterial.EnableKeyword("_EMISSION");
            }

            // 綁定 XRI 抓取事件
            _grabInteractable = GetComponent<XRGrabInteractable>();
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.AddListener(OnFlashlightGrabbed);
            }
        }

        private void Update()
        {
            // 如果還沒被拿起，持續播放平滑的呼吸燈效果
            if (_isBreathing && _flashlightMaterial != null)
            {
                // 使用 Sin 波產生平滑的 0 ~ 1 循環
                float t = (Mathf.Sin(Time.time * breathingSpeed) + 1f) / 2f;
                float emissionStrength = Mathf.Lerp(minEmission, maxEmission, t);
                _flashlightMaterial.SetColor("_EmissionColor", emissionBaseColor * emissionStrength);
            }
        }

        /// <summary>
        /// 提供給 GazeDwellSelector 在注視超過 2 秒時呼叫的方法。
        /// </summary>
        public void PlayElectricHum()
        {
            // 只有在還沒拿起手電筒的時候才會有這個效果
            if (_isBreathing && electricSoundSource != null && electricHumClip != null)
            {
                if (!electricSoundSource.isPlaying)
                {
                    electricSoundSource.PlayOneShot(electricHumClip);
                }
            }
        }

        private void OnFlashlightGrabbed(SelectEnterEventArgs args)
        {
            if (_isBreathing)
            {
                _isBreathing = false;
                
                // 關閉發光效果
                if (_flashlightMaterial != null)
                {
                    _flashlightMaterial.SetColor("_EmissionColor", Color.black);
                    _flashlightMaterial.DisableKeyword("_EMISSION");
                }

                // 若電流聲還在播放則關閉
                if (electricSoundSource != null && electricSoundSource.isPlaying)
                {
                    electricSoundSource.Stop();
                }

                // 呼叫黑板字跡浮現事件
                onFlashlightGrabbedAction?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.RemoveListener(OnFlashlightGrabbed);
            }
        }
    }
}
