using System;
using UnityEngine;
using UnityEngine.Events;

namespace TheBreathlessStudyRoom.Core
{
    /// <summary>
    /// Represents the core game state nodes in the VR experience.
    /// </summary>
    public enum GameState
    {
        Start,
        Scene1StudyRoom,
        Scene2Corridor,
        Scene3GuardRoom,
        Ending
    }

    /// <summary>
    /// Coordinates the in-game clock, drives timed anomaly events,
    /// and logs the subject's compliance profile for final ending distribution.
    /// </summary>
    [AddComponentMenu("The Breathless Study Room/Core/Timeline Manager")]
    public class TimelineManager : MonoBehaviour
    {
        [Header("Time Calibration")]
        [Tooltip("The duration in real-world seconds that equals one in-game minute.")]
        [SerializeField] private float _secondsPerGameMinute = 40f;

        [Header("State Metrics")]
        [Range(0f, 100f)]
        [Tooltip("Current stress percentage of the player subject.")]
        [SerializeField] private float _stressLevel = 0f;

        [Header("State Transition Events")]
        [SerializeField] private UnityEvent<GameState> _onGameStateChanged;
        [SerializeField] private UnityEvent<string> _onTimelineEventTriggered;
        [SerializeField] private UnityEvent<string> _onTimeUpdated;

        private GameState _currentState = GameState.Start;
        private float _elapsedSeconds = 0f;
        private int _gameMinutes = 1;
        private int _gameSeconds = 0;
        private bool _isTimerRunning = false;

        // Compliance logs
        private int _complianceEventsCount = 0;
        private int _rebellionEventsCount = 0;
        private int _secretsDiscoveredCount = 0;

        // Event hooks
        public event Action<GameState> OnStateTransition;
        public event Action<string> OnTimelineTrigger;
        public event Action<float> OnStressModulated;

        public GameState CurrentState => _currentState;
        public float StressLevel => _stressLevel;
        public int ComplianceCount => _complianceEventsCount;
        public int RebellionCount => _rebellionEventsCount;
        public int SecretsFound => _secretsDiscoveredCount;

        /// <summary>
        /// Awake initialized values.
        /// </summary>
        private void Awake()
        {
            _currentState = GameState.Start;
        }

        private void Start()
        {
            // The game will wait for StartScreenManager to call StartTimeline()
            Debug.Log("[TimelineManager] Initialized. Waiting for start input in GameState.Start...");
        }

        private void Update()
        {
            if (!_isTimerRunning) return;

            _elapsedSeconds += Time.deltaTime;
            UpdateGameClock();
            EvaluateTimelineEvents();
        }

        /// <summary>
        /// Begins the timeline simulation.
        /// </summary>
        public void StartTimeline()
        {
            _isTimerRunning = true;
            SetGameState(GameState.Scene1StudyRoom);
        }

        public void PauseTimeline()
        {
            _isTimerRunning = false;
            Debug.Log("[TimelineManager] Timeline Paused (Tutorial Mode).");
        }

        public void ResumeTimeline()
        {
            _isTimerRunning = true;
            Debug.Log("[TimelineManager] Timeline Resumed.");
        }

        /// <summary>
        /// Transitions game states cleanly.
        /// </summary>
        public void SetGameState(GameState newState)
        {
            _currentState = newState;
            _onGameStateChanged?.Invoke(_currentState);
            OnStateTransition?.Invoke(_currentState);
            Debug.Log($"[TimelineManager] State transitioned to: {newState}");
        }

        /// <summary>
        /// Updates the clock display mathematically.
        /// </summary>
        private void UpdateGameClock()
        {
            if (_currentState != GameState.Scene1StudyRoom) return;

            int minutesPassed = Mathf.FloorToInt(_elapsedSeconds / _secondsPerGameMinute);
            float remainingSeconds = _elapsedSeconds % _secondsPerGameMinute;
            int secondsPassed = Mathf.FloorToInt(remainingSeconds * (60f / _secondsPerGameMinute));

            _gameMinutes = 1 + minutesPassed;
            _gameSeconds = secondsPassed;

            if (_gameMinutes >= 5)
            {
                _gameMinutes = 5;
                _gameSeconds = 0;
            }

            string formattedTime = $"24:{_gameMinutes:00}:{_gameSeconds:00}";
            _onTimeUpdated?.Invoke(formattedTime);
        }

        /// <summary>
        /// Processes linear MVP timed triggers in accordance with the PRD schedule.
        /// </summary>
        private void EvaluateTimelineEvents()
        {
            int elapsedInt = Mathf.FloorToInt(_elapsedSeconds);

            // EVENT 1 (25s): Window Tapping Starts
            if (elapsedInt == 25)
            {
                TriggerEvent("WINDOW_TAP_TRIGGER");
            }
            // EVENT 2 (55s): Blackboard rules shift to Red chalk
            else if (elapsedInt == 55)
            {
                TriggerEvent("BLACKBOARD_CONFLICT_TRIGGER");
            }
            // EVENT 3 (90s): Locker opens slightly, flashing Green chalkboard rules
            else if (elapsedInt == 90)
            {
                TriggerEvent("LOCKER_DECRYPT_TRIGGER");
            }
            // EVENT 4 (125s): Radio Broadcast announcement (Head down vs door check)
            else if (elapsedInt == 125)
            {
                TriggerEvent("BROADCAST_PATROL_TRIGGER");
            }
            // EVENT 5 (165s): Exit locks unlock, permitting corridor access
            else if (elapsedInt == 165)
            {
                TriggerEvent("DOOR_UNLOCK_TRIGGER");
                _isTimerRunning = false; // Clock freezes at 24:04
            }
        }

        private void TriggerEvent(string eventTag)
        {
            _onTimelineEventTriggered?.Invoke(eventTag);
            OnTimelineTrigger?.Invoke(eventTag);
            Debug.Log($"[TimelineManager] Timed Event triggered: {eventTag}");
        }

        /// <summary>
        /// Adjusts stress levels and updates bounds.
        /// </summary>
        public void ModulateStress(float amount)
        {
            _stressLevel = Mathf.Clamp(_stressLevel + amount, 0f, 100f);
            OnStressModulated?.Invoke(_stressLevel);

            if (_stressLevel >= 100f && _currentState != GameState.Ending)
            {
                TriggerEvent("SANITY_CRUSH_ENDING");
                SetGameState(GameState.Ending);
            }
        }

        /// <summary>
        /// Logs a compliance action with default weight (1).
        /// </summary>
        public void LogCompliance()
        {
            LogCompliance(1);
        }

        /// <summary>
        /// Logs a compliance action (player follows authoritative directives blindly).
        /// </summary>
        public void LogCompliance(int weight)
        {
            _complianceEventsCount += weight;
            Debug.Log($"[TimelineManager] Obedience logged. Total Compliance weight: {_complianceEventsCount}");
        }

        /// <summary>
        /// Logs a rebellion action (player relies on independent thinking).
        /// </summary>
        public void LogRebellion(int weight = 1)
        {
            _rebellionEventsCount += weight;
            Debug.Log($"[TimelineManager] Defiance logged. Total Rebellion weight: {_rebellionEventsCount}");
        }

        /// <summary>
        /// Registers key hidden clues retrieved.
        /// </summary>
        public void LogSecretFound()
        {
            _secretsDiscoveredCount++;
            Debug.Log($"[TimelineManager] Secrets discovered: {_secretsDiscoveredCount}");
        }
    }
}
