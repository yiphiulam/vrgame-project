using UnityEngine;
using System.Collections;

namespace TheBreathlessStudyRoom.Interactables
{
    /// <summary>
    /// 控制黑板上隱藏的螢光文字平滑浮現。
    /// </summary>
    public class BlackboardTextFader : MonoBehaviour
    {
        public MeshRenderer textRenderer;
        public int materialIndex = 0;
        public float fadeDuration = 3.0f;
        
        [Tooltip("最終要達到的發光顏色強度")]
        [ColorUsage(true, true)] public Color finalEmissionColor = new Color(0f, 1f, 0f, 2f); // 綠色螢光

        private Material _textMaterial;

        private void Awake()
        {
            if (textRenderer != null)
            {
                _textMaterial = textRenderer.materials[materialIndex];
                // 初始設為全黑 (隱藏狀態)
                _textMaterial.EnableKeyword("_EMISSION");
                _textMaterial.SetColor("_EmissionColor", Color.black);
            }
        }

        /// <summary>
        /// 提供給手電筒的 UnityEvent 呼叫。
        /// </summary>
        public void StartFadingIn()
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(FadeInRoutine());
            }
        }

        private IEnumerator FadeInRoutine()
        {
            float elapsed = 0f;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration);
                
                if (_textMaterial != null)
                {
                    _textMaterial.SetColor("_EmissionColor", Color.Lerp(Color.black, finalEmissionColor, t));
                }

                yield return null;
            }
            
            if (_textMaterial != null)
            {
                _textMaterial.SetColor("_EmissionColor", finalEmissionColor);
            }
        }
    }
}
