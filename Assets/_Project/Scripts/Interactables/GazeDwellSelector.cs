using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TheBreathlessStudyRoom.Interactables
{
    /// <summary>
    /// Custom XRI 3.x interactable component that detects visual gaze hover,
    /// triggers a selection event after a specified dwell duration,
    /// and mathematically scales the collider bounds to ease long-distance interaction.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("The Breathless Study Room/Interactables/Gaze Dwell Selector")]
    public class GazeDwellSelector : XRSimpleInteractable
    {
        [Header("Gaze Dwell Settings")]
        [Tooltip("The time in seconds a player must stare at this object to select it.")]
        [SerializeField] private float _dwellThreshold = 1.2f;

        [Tooltip("Standard event broadcast once the visual dwell duration is achieved.")]
        [SerializeField] private UnityEvent _onDwellSelected = new UnityEvent();

        /// <summary>
        /// Exposes the dwell selection event publicly for Editor scripting and runtime hookups.
        /// </summary>
        public UnityEvent OnDwellSelected => _onDwellSelected;

        [Header("Gaze Assistance (Distance Scaling)")]
        [Tooltip("Allows the interactable to expand its physical collider bounds when situated far from the camera.")]
        [SerializeField] private bool _enableGazeAssistance = true;

        [Tooltip("The camera transform representing the player's head. If left null, Camera.main will be queried.")]
        [SerializeField] private Transform _playerCameraTransform;

        private float _dwellTimer = 0f;
        private bool _isGazed = false;
        
        // Physics collision caching
        private BoxCollider _boxCollider;
        private SphereCollider _sphereCollider;
        private Vector3 _baseBoxSize = Vector3.one;
        private float _baseSphereRadius = 1f;
        private bool _hasBoxCollider = false;
        private bool _hasSphereCollider = false;

        /// <summary>
        /// Gets the current progress of the dwell countdown (from 0.0 to 1.0).
        /// </summary>
        public float DwellProgress => _dwellThreshold > 0 ? Mathf.Clamp01(_dwellTimer / _dwellThreshold) : 0f;

        protected override void Awake()
        {
            base.Awake();
            
            // Cache physical colliders
            if (TryGetComponent(out _boxCollider))
            {
                _baseBoxSize = _boxCollider.size;
                _hasBoxCollider = true;
            }
            else if (TryGetComponent(out _sphereCollider))
            {
                _baseSphereRadius = _sphereCollider.radius;
                _hasSphereCollider = true;
            }
        }

        protected virtual void Start()
        {
            if (_playerCameraTransform == null && Camera.main != null)
            {
                _playerCameraTransform = Camera.main.transform;
            }
        }

        protected virtual void Update()
        {
            HandleDwellTimer();
            HandleGazeAssistanceScale();
        }

        /// <summary>
        /// Accumulates active gaze look duration and fires triggers.
        /// </summary>
        private void HandleDwellTimer()
        {
            if (!_isGazed) return;

            _dwellTimer += Time.deltaTime;

            if (_dwellTimer >= _dwellThreshold)
            {
                _isGazed = false;
                _dwellTimer = 0f;
                TriggerDwellSelection();
            }
        }

        /// <summary>
        /// Implementation of the mathematical gaze assistance helper:
        /// Scales the collider bounding box by the formula: AssistedRadius = BaseRadius * Clamp(0.2 * Distance, 1.0, 3.5)
        /// to ensure easy focusing on distant environment nodes.
        /// </summary>
        private void HandleGazeAssistanceScale()
        {
            if (!_enableGazeAssistance || _playerCameraTransform == null) return;

            float distance = Vector3.Distance(transform.position, _playerCameraTransform.position);
            float assistanceMultiplier = Mathf.Clamp(0.2f * distance, 1.0f, 3.5f);

            if (_hasBoxCollider)
            {
                _boxCollider.size = _baseBoxSize * assistanceMultiplier;
            }
            else if (_hasSphereCollider)
            {
                _sphereCollider.radius = _baseSphereRadius * assistanceMultiplier;
            }
        }

        /// <summary>
        /// Broadcasts selection notifications dynamically.
        /// </summary>
        private void TriggerDwellSelection()
        {
            _onDwellSelected?.Invoke();
            Debug.Log($"[GazeDwellSelector] Dwell threshold achieved on object: {gameObject.name}");
        }

        /// <summary>
        /// Overridden method triggered when the XRI 3.x Gaze Interactor begins hovering.
        /// </summary>
        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);
            
            // Check if hover is triggered by a Gaze Interactor
            if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRGazeInteractor)
            {
                _isGazed = true;
                _dwellTimer = 0f;
            }
        }

        /// <summary>
        /// Overridden method triggered when the XRI 3.x Gaze Interactor leaves bounds.
        /// </summary>
        protected override void OnHoverExited(HoverExitEventArgs args)
        {
            base.OnHoverExited(args);
            
            if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRGazeInteractor)
            {
                _isGazed = false;
                _dwellTimer = 0f;
            }
        }
    }
}
