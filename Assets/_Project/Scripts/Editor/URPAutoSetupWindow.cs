using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TheBreathlessStudyRoom.Editor
{
    /// <summary>
    /// 自動尋找或建立 URP Asset，並自動綁定到 GraphicsSettings 中，用以一鍵修復紫屏問題。
    /// </summary>
    public class URPAutoSetupWindow : EditorWindow
    {
        [MenuItem("The Breathless Study Room/Fix Purple Screen (URP Setup)", false, 1)]
        public static void FixPurpleScreen()
        {
            // 尋找專案中是否已經有 URP Asset
            string[] guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
            
            if (guids.Length > 0)
            {
                // 如果已經有了，直接套用到全域設定中
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                
                GraphicsSettings.defaultRenderPipeline = asset;
                QualitySettings.renderPipeline = asset;
                
                Debug.Log($"[URPFixer] 已成功綁定 URP Asset: {path}");
                EditorUtility.DisplayDialog("修復成功", "已為您自動套用 URP 渲染管線設定！\n場景的紫屏異常應該已經恢復正常了。", "太棒了");
            }
            else
            {
                // 如果沒有，則呼叫 Unity 內建的選單指令來安全地建立一個
                Debug.Log("[URPFixer] 未找到 URP Asset，正在自動建立...");
                
                // 強制執行 URP 建立指令
                EditorApplication.ExecuteMenuItem("Assets/Create/Rendering/URP Asset (with Universal Renderer)");
                
                EditorUtility.DisplayDialog("URP 設定檔建立中", 
                    "我已自動幫您呼叫了 URP 設定檔的建立指令！\n\n" +
                    "請在下方 Project 視窗按下 Enter 完成檔案命名。\n" +
                    "完成後，請【再次點擊一次】上方的 Fix Purple Screen 按鈕，我就會自動幫您套用到系統中！", 
                    "我知道了");
            }
        }
    }
}
