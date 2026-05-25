using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TheBreathlessStudyRoom.Core;

namespace TheBreathlessStudyRoom.Interactables
{
    /// <summary>
    /// 當玩家視線掃過互動物件或閱讀紙張時，為其添加平滑的高光，以增強文字可讀性。
    /// 支援「紅色警告」高對比閃爍模式。
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class InteractableHighlight : MonoBehaviour
    {
        public MeshRenderer targetRenderer;
        public int materialIndex = 0;
        
        [Header("Highlight Settings")]
        [Tooltip("普通物件或規則的高光顏色，建議使用冷白色或淺綠色提高可讀性")]
        [ColorUsage(true, true)] public Color highlightColor = new Color(0.8f, 0.9f, 1f, 1f);
        public float pulseSpeed = 2f;
        
        [Tooltip("如果是重要或致命的規則，打勾此項，高光會變成極高對比的紅色閃爍警告！")]
        public bool isImportantRedWarning = false;
        
        private Material _mat;
        private Color _originalEmission;
        private bool _isHovered = false;
        private XRBaseInteractable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<XRBaseInteractable>();
            if (targetRenderer != null && targetRenderer.materials.Length > materialIndex)
            {
                _mat = targetRenderer.materials[materialIndex];
                _mat.EnableKeyword("_EMISSION");
                _originalEmission = _mat.GetColor("_EmissionColor");
            }

            _interactable.hoverEntered.AddListener(OnHoverEnter);
            _interactable.hoverExited.AddListener(OnHoverExit);
        }

        private void Update()
        {
            if (_isHovered && _mat != null)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
                
                // 若為重要警告，強制轉為 2 倍亮度的紅色脈衝
                Color targetCol = isImportantRedWarning ? Color.red * 2f : highlightColor;
                
                _mat.SetColor("_EmissionColor", Color.Lerp(_originalEmission, targetCol, t));
            }
        }

        private void OnHoverEnter(HoverEnterEventArgs args)
        {
            _isHovered = true;
            
            // 通知 HintSystem 重置防卡關計時器 (因為玩家正在閱讀/互動)
            if (HintSystem.Instance != null)
            {
                HintSystem.Instance.ResetTimer();
            }
        }

        private void OnHoverExit(HoverExitEventArgs args)
        {
            _isHovered = false;
            if (_mat != null) _mat.SetColor("_EmissionColor", _originalEmission);
        }

        private void OnDestroy()
        {
            if (_interactable != null)
            {
                _interactable.hoverEntered.RemoveListener(OnHoverEnter);
                _interactable.hoverExited.RemoveListener(OnHoverExit);
            }
        }
    }
}
