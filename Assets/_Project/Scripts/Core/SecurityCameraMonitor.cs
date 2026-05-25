using System;
using UnityEngine;

namespace TheBreathlessStudyRoom.Core
{
    /// <summary>
    /// Handles the closed-circuit television (CCTV) security monitoring system.
    /// Captures a secondary virtual camera's viewport into a dynamically created URP-compatible
    /// RenderTexture, maps it to a list of in-world monitor screen surfaces, and manages
    /// target paper rules elimination when the narrative reaches its ultimate climax.
    /// </summary>
    [AddComponentMenu("The Breathless Study Room/Core/Security Camera Monitor")]
    public class SecurityCameraMonitor : MonoBehaviour
    {
        [Header("CCTV Camera Setup")]
        [Tooltip("The secondary virtual camera observing the replica of the study room.")]
        [SerializeField] private Camera _cctvCamera;

        [Header("Render Texture Settings")]
        [Tooltip("Width resolution of the generated RenderTexture.")]
        [SerializeField] private int _textureWidth = 512;

        [Tooltip("Height resolution of the generated RenderTexture.")]
        [SerializeField] private int _textureHeight = 512;

        [Tooltip("Color depth bits of the generated RenderTexture.")]
        [SerializeField] private int _textureDepthBits = 16;

        [Tooltip("Format of the generated RenderTexture.")]
        [SerializeField] private RenderTextureFormat _textureFormat = RenderTextureFormat.Default;

        [Header("Monitor Screens")]
        [Tooltip("MeshRenderers of the in-world monitor screens displaying the CCTV feed.")]
        [SerializeField] private MeshRenderer[] _monitorRenderers;

        [Tooltip("URP shader main texture property name. Default is '_BaseMap' for URP Lit shaders.")]
        [SerializeField] private string _shaderTexturePropertyName = "_BaseMap";

        [Header("Narrative Climax & Rule Paper Elimination")]
        [Tooltip("Optional direct assignment of rule papers renderers to bypass tag search for high performance.")]
        [SerializeField] private Renderer[] _assignedRulePapers;

        [Tooltip("The exact tag applied to the rule paper GameObjects that will be erased if not assigned directly.")]
        [SerializeField] private string _rulePaperTag = "RulePaper";

        [Tooltip("The event tag from TimelineManager that triggers the rule paper erasure.")]
        [SerializeField] private string _climaxEventTag = "FINAL_CLIMAX_TRIGGER";

        [Header("Timeline Manager Hook")]
        [Tooltip("Linkage to the global TimelineManager to listen for events.")]
        [SerializeField] private TimelineManager _timelineManager;

        private RenderTexture _dynamicRenderTexture;

        /// <summary>
        /// Initializes the CCTV dynamic RenderTexture and maps it to target screens.
        /// </summary>
        private void Start()
        {
            InitializeCCTVFeed();

            if (_timelineManager != null)
            {
                _timelineManager.OnTimelineTrigger += HandleTimelineTrigger;
            }
        }

        /// <summary>
        /// Cleans up the dynamic RenderTexture allocation to prevent GPU/RAM memory leaks.
        /// </summary>
        private void OnDestroy()
        {
            if (_timelineManager != null)
            {
                _timelineManager.OnTimelineTrigger -= HandleTimelineTrigger;
            }

            ReleaseRenderTexture();
        }

        /// <summary>
        /// Generates the RenderTexture at runtime and assigns it as the target of the CCTV camera
        /// and main texture of the monitor materials.
        /// </summary>
        private void InitializeCCTVFeed()
        {
            if (_cctvCamera == null)
            {
                Debug.LogWarning("[SecurityCameraMonitor] CCTV Camera is not assigned. CCTV feed initialization aborted.");
                return;
            }

            // Dynamically instantiate the RenderTexture to isolate graphics memory
            _dynamicRenderTexture = new RenderTexture(_textureWidth, _textureHeight, _textureDepthBits, _textureFormat)
            {
                name = "CCTV_Dynamic_Feed",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            if (!_dynamicRenderTexture.Create())
            {
                Debug.LogError("[SecurityCameraMonitor] Failed to create dynamic RenderTexture.");
                return;
            }

            // Route camera output to texture
            _cctvCamera.targetTexture = _dynamicRenderTexture;

            // Apply texture to all monitor screen materials
            if (_monitorRenderers != null && _monitorRenderers.Length > 0)
            {
                foreach (MeshRenderer screenRenderer in _monitorRenderers)
                {
                    if (screenRenderer != null && screenRenderer.material != null)
                    {
                        // Handle standard URP Lit shader properties as well as legacy shaders
                        if (screenRenderer.material.HasProperty(_shaderTexturePropertyName))
                        {
                            screenRenderer.material.SetTexture(_shaderTexturePropertyName, _dynamicRenderTexture);
                        }
                        else
                        {
                            screenRenderer.material.mainTexture = _dynamicRenderTexture;
                        }
                    }
                }
            }

            Debug.Log($"[SecurityCameraMonitor] CCTV feed successfully initialized at {_textureWidth}x{_textureHeight}.");
        }

        /// <summary>
        /// Releases graphics resources allocated for the dynamic RenderTexture.
        /// </summary>
        private void ReleaseRenderTexture()
        {
            if (_dynamicRenderTexture != null)
            {
                if (_cctvCamera != null)
                {
                    _cctvCamera.targetTexture = null;
                }

                _dynamicRenderTexture.Release();
                Destroy(_dynamicRenderTexture);
                _dynamicRenderTexture = null;

                Debug.Log("[SecurityCameraMonitor] Dynamic RenderTexture released successfully.");
            }
        }

        /// <summary>
        /// Listeners to Timeline events and triggers reactions based on event tags.
        /// </summary>
        /// <param name="eventTag">The unique tag of the timeline trigger.</param>
        private void HandleTimelineTrigger(string eventTag)
        {
            if (string.Equals(eventTag, _climaxEventTag, StringComparison.OrdinalIgnoreCase))
            {
                EraseAllRules();
            }
        }

        /// <summary>
        /// Finds all GameObjects bearing the rule paper tag (or iterates over assigned renderers)
        /// and disables their Renderer components to simulate absolute text vanishing.
        /// </summary>
        public void EraseAllRules()
        {
            int deactivatedCount = 0;

            // Method A: Direct assignment (Recommended, High Performance, compliance with .cursorrules rules)
            if (_assignedRulePapers != null && _assignedRulePapers.Length > 0)
            {
                foreach (Renderer paperRenderer in _assignedRulePapers)
                {
                    if (paperRenderer != null)
                    {
                        paperRenderer.enabled = false;
                        deactivatedCount++;
                    }
                }
                Debug.Log($"[SecurityCameraMonitor] Climax reached. Successfully disabled Renderer on {deactivatedCount} assigned rule papers (Bypassed Find).");
                return;
            }

            // Method B: Dynamic Tag-based lookup (As requested by the PRD specification)
            if (string.IsNullOrEmpty(_rulePaperTag))
            {
                Debug.LogWarning("[SecurityCameraMonitor] Rule paper tag is null or empty. Tag lookup aborted.");
                return;
            }

            GameObject[] rulePapers = GameObject.FindGameObjectsWithTag(_rulePaperTag);

            if (rulePapers == null || rulePapers.Length == 0)
            {
                Debug.LogWarning($"[SecurityCameraMonitor] No GameObjects with tag '{_rulePaperTag}' found in the scene.");
                return;
            }

            foreach (GameObject paper in rulePapers)
            {
                if (paper != null)
                {
                    Renderer paperRenderer = paper.GetComponent<Renderer>();
                    if (paperRenderer != null)
                    {
                        paperRenderer.enabled = false;
                        deactivatedCount++;
                    }
                }
            }

            Debug.Log($"[SecurityCameraMonitor] Climax reached. Successfully disabled Renderer on {deactivatedCount} rule paper objects via tag '{_rulePaperTag}'.");
        }
    }
}
