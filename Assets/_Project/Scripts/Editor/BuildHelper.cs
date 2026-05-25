using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TheBreathlessStudyRoom.Editor
{
    /// <summary>
    /// Automates PCVR Windows standalone packaging directly via command line batchmode or Editor menu shortcuts.
    /// </summary>
    public static class BuildHelper
    {
        [MenuItem("The Breathless Study Room/Build Windows Executable", false, 50)]
        public static void BuildWindows()
        {
            Debug.Log("[BuildHelper] Initiating Windows PCVR Build sequence...");

            string buildDir = "Builds/Windows";
            string buildPath = Path.Combine(buildDir, "TheBreathlessStudyRoom.exe");

            // Ensure Builds/Windows directory structure exists
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            // Target the core MVP Scene file
            string scenePath = "Assets/_Project/Scenes/TheBreathlessStudyRoom_MVP.unity";
            if (!File.Exists(scenePath))
            {
                string[] scenesFound = Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories);
                if (scenesFound.Length > 0)
                {
                    scenePath = scenesFound[0];
                }
                else
                {
                    Debug.LogError("[BuildHelper] Build failed: TheBreathlessStudyRoom_MVP.unity scene was not generated yet. Please run Setup Complete MVP Scene first.");
                    if (!Application.isBatchMode)
                    {
                        EditorUtility.DisplayDialog("Build Error", "Please generate the MVP scene first by selecting 'The Breathless Study Room -> Setup Complete MVP Scene'!", "OK");
                    }
                    return;
                }
            }

            string[] scenes = new string[] { scenePath };

            BuildPlayerOptions opt = new BuildPlayerOptions();
            opt.scenes = scenes;
            opt.locationPathName = buildPath;
            opt.target = BuildTarget.StandaloneWindows64;
            opt.options = BuildOptions.None;

            Debug.Log($"[BuildHelper] Standalone Output Path: {buildPath}");
            Debug.Log($"[BuildHelper] Packaging Scene: {scenePath}");

            var report = BuildPipeline.BuildPlayer(opt);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[BuildHelper] Build Succeeded! Standalone Package Total Size: {summary.totalSize} bytes. Saved to: {buildPath}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Build Succeeded", $"Successfully packaged the game standalone Windows VR build!\n\nLocation: {buildPath}", "Open Folder");
                    EditorUtility.RevealInFinder(buildPath);
                }
            }
            else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
            {
                Debug.LogError($"[BuildHelper] Build Failed! Total compiled error count: {summary.totalErrors}");
            }
        }
        public static void BuildCompleteBatch()
        {
            Debug.Log("[BuildHelper] Running headless complete batch build...");
            SceneSetupHelper.SetupCompleteMVPScene();
            BuildWindows();
        }
    }
}
