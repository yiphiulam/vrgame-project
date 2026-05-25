using System.Collections;
using UnityEngine;

namespace TheBreathlessStudyRoom.Core
{
    /// <summary>
    /// Governs visual anomalies in the environment, handling dynamic blackboard material swappings
    /// and random URP light intensity fluctuations (fluorescent bulb flickers) based on timeline triggers.
    /// </summary>
    [AddComponentMenu("The Breathless Study Room/Core/Anomaly Controller")]
    public class AnomalyController : MonoBehaviour
    {
        [Header("Blackboard Configuration")]
        [SerializeField] private MeshRenderer _blackboardMeshRenderer;
        [SerializeField] private Material _defaultRulesMaterial;
        [SerializeField] private Material _redConflictRulesMaterial;
        [SerializeField] private Material _greenDecryptRulesMaterial;
        [SerializeField] private Material _greenExitRulesMaterial;
        [SerializeField] private Material _windowAnomalyFaceMaterial;

        [Header("Window Configuration")]
        [SerializeField] private MeshRenderer _windowGlassRenderer;
        [SerializeField] private GameObject _windowAnomalyPlane;

        [Header("URP Environmental Lights")]
        [SerializeField] private Light[] _overheadFluorescents;
        [SerializeField] private Light _deskLamp;

        [Header("Timeline Manager Hook")]
        [SerializeField] private TimelineManager _timelineManager;

        private bool _shouldFlicker = true;
        private float[] _baseLightIntensities;

        private void Start()
        {
            // Record original light intensities
            if (_overheadFluorescents != null && _overheadFluorescents.Length > 0)
            {
                _baseLightIntensities = new float[_overheadFluorescents.Length];
                for (int i = 0; i < _overheadFluorescents.Length; i++)
                {
                    if (_overheadFluorescents[i] != null)
                    {
                        _baseLightIntensities[i] = _overheadFluorescents[i].intensity;
                    }
                }
            }

            if (_timelineManager != null)
            {
                _timelineManager.OnTimelineTrigger += HandleTimelineVisualTrigger;
            }

            // Draw default states
            ResetBlackboard();
            StartCoroutine(OverheadLightFlickerLoop());
        }

        private void OnDestroy()
        {
            if (_timelineManager != null)
            {
                _timelineManager.OnTimelineTrigger -= HandleTimelineVisualTrigger;
            }
        }

        /// <summary>
        /// Resets chalkboard rules back to initial safety instructions.
        /// </summary>
        public void ResetBlackboard()
        {
            if (_blackboardMeshRenderer != null && _defaultRulesMaterial != null)
            {
                _blackboardMeshRenderer.material = _defaultRulesMaterial;
            }
            if (_windowAnomalyPlane != null)
            {
                _windowAnomalyPlane.SetActive(false);
            }
        }

        /// <summary>
        /// Swaps materials in accordance with structural narrative triggers.
        /// </summary>
        private void HandleTimelineVisualTrigger(string eventTag)
        {
            switch (eventTag)
            {
                case "WINDOW_TAP_TRIGGER":
                    // Start window tapping visuals
                    StartCoroutine(TriggerWindowFaceSequence(12f));
                    break;

                case "BLACKBOARD_CONFLICT_TRIGGER":
                    // Switch blackboard to red rules
                    if (_blackboardMeshRenderer != null && _redConflictRulesMaterial != null)
                    {
                        _blackboardMeshRenderer.material = _redConflictRulesMaterial;
                    }
                    StartCoroutine(TriggerIntenseFlicker(2f));
                    break;

                case "LOCKER_DECRYPT_TRIGGER":
                    // Switch blackboard to locker guidance green rules
                    if (_blackboardMeshRenderer != null && _greenDecryptRulesMaterial != null)
                    {
                        _blackboardMeshRenderer.material = _greenDecryptRulesMaterial;
                    }
                    break;

                case "DOOR_UNLOCK_TRIGGER":
                    // Switch blackboard to corridor green escape path
                    if (_blackboardMeshRenderer != null && _greenExitRulesMaterial != null)
                    {
                        _blackboardMeshRenderer.material = _greenExitRulesMaterial;
                    }
                    break;
            }
        }

        /// <summary>
        /// Fades window shadow face in and out during tapping sequence.
        /// </summary>
        private IEnumerator TriggerWindowFaceSequence(float duration)
        {
            if (_windowAnomalyPlane == null) yield break;
            
            _windowAnomalyPlane.SetActive(true);
            MeshRenderer faceRenderer = _windowAnomalyPlane.GetComponent<MeshRenderer>();
            
            if (faceRenderer != null)
            {
                Material faceMat = faceRenderer.material;
                Color baseColor = faceMat.color;
                
                // Fade In (alpha from 0 to 0.85)
                float elapsed = 0f;
                while (elapsed < 1.5f)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(0f, 0.85f, elapsed / 1.5f);
                    faceMat.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                    yield return null;
                }

                yield return new WaitForSeconds(duration - 3f);

                // Fade Out
                elapsed = 0f;
                while (elapsed < 1.5f)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(0.85f, 0f, elapsed / 1.5f);
                    faceMat.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                    yield return null;
                }
            }

            _windowAnomalyPlane.SetActive(false);
        }

        /// <summary>
        /// Overhead neon fluorescent light humming and flickering loop.
        /// </summary>
        private IEnumerator OverheadLightFlickerLoop()
        {
            while (true)
            {
                if (_shouldFlicker && _overheadFluorescents != null)
                {
                    // Ambient flicker
                    float delay = Random.Range(1f, 8f);
                    yield return new WaitForSeconds(delay);

                    int flickers = Random.Range(2, 6);
                    for (int i = 0; i < flickers; i++)
                    {
                        SetOverheadLightsActive(false);
                        yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
                        SetOverheadLightsActive(true);
                        yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
                    }
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }
            }
        }

        /// <summary>
        /// Instantly runs short intense electrical storm flickers.
        /// </summary>
        private IEnumerator TriggerIntenseFlicker(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetOverheadLightsIntensity(Random.Range(0f, 0.2f));
                yield return new WaitForSeconds(Random.Range(0.02f, 0.08f));
            }
            ResetOverheadLightsIntensity();
        }

        private void SetOverheadLightsActive(bool isActive)
        {
            if (_overheadFluorescents == null) return;
            
            for (int i = 0; i < _overheadFluorescents.Length; i++)
            {
                if (_overheadFluorescents[i] != null)
                {
                    _overheadFluorescents[i].enabled = isActive;
                }
            }
        }

        private void SetOverheadLightsIntensity(float multiplier)
        {
            if (_overheadFluorescents == null || _baseLightIntensities == null) return;
            
            for (int i = 0; i < _overheadFluorescents.Length; i++)
            {
                if (_overheadFluorescents[i] != null && i < _baseLightIntensities.Length)
                {
                    _overheadFluorescents[i].intensity = _baseLightIntensities[i] * multiplier;
                }
            }
        }

        private void ResetOverheadLightsIntensity()
        {
            if (_overheadFluorescents == null || _baseLightIntensities == null) return;
            
            for (int i = 0; i < _overheadFluorescents.Length; i++)
            {
                if (_overheadFluorescents[i] != null && i < _baseLightIntensities.Length)
                {
                    _overheadFluorescents[i].intensity = _baseLightIntensities[i];
                    _overheadFluorescents[i].enabled = true;
                }
            }
        }
    }
}
