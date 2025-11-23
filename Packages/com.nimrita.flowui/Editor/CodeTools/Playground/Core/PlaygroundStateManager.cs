#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nimrita.FlowUI.Editor.Playground
{
    /// <summary>
    /// Manages playground state transitions using FSM pattern.
    ///
    /// Benefits:
    /// - Predictable state flow
    /// - Easy debugging (log all transitions)
    /// - Clear error recovery paths
    /// - Prevents invalid state combinations
    /// </summary>
    public class PlaygroundStateManager
    {
        private PlaygroundState currentState = PlaygroundState.Idle;
        private PlaygroundState previousState = PlaygroundState.Idle;
        private double lastTransitionTime = 0;
        private string lastErrorMessage = "";
        private bool logTransitions = true;

        // Events
        public event Action<PlaygroundState, PlaygroundState> OnStateChanged;

        /// <summary>
        /// Gets the current state.
        /// </summary>
        public PlaygroundState CurrentState => currentState;

        /// <summary>
        /// Gets the previous state.
        /// </summary>
        public PlaygroundState PreviousState => previousState;

        /// <summary>
        /// Gets the last error message (if in Error state).
        /// </summary>
        public string LastErrorMessage => lastErrorMessage;

        /// <summary>
        /// Gets or sets whether to log state transitions.
        /// </summary>
        public bool LogTransitions
        {
            get => logTransitions;
            set => logTransitions = value;
        }

        /// <summary>
        /// Attempt to transition to a new state.
        /// Returns true if transition was valid and executed.
        /// </summary>
        public bool TransitionTo(PlaygroundState newState, string errorMessage = "")
        {
            // Check if transition is valid
            if (!IsValidTransition(currentState, newState))
            {
                Debug.LogWarning($"[PlaygroundFSM] Invalid transition: {currentState} → {newState}");
                return false;
            }

            // Store previous state
            previousState = currentState;
            currentState = newState;
            lastTransitionTime = UnityEditor.EditorApplication.timeSinceStartup;

            // Store error message if entering error state
            if (newState == PlaygroundState.Error)
            {
                lastErrorMessage = errorMessage;
            }
            else
            {
                lastErrorMessage = "";
            }

            // Log transition
            if (logTransitions)
            {
                string msg = $"[PlaygroundFSM] {previousState} → {currentState}";
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    msg += $" ({errorMessage})";
                }
                Debug.Log(msg);
            }

            // Notify listeners
            OnStateChanged?.Invoke(previousState, currentState);

            return true;
        }

        /// <summary>
        /// Reset to Idle state.
        /// </summary>
        public void Reset()
        {
            TransitionTo(PlaygroundState.Idle);
            lastErrorMessage = "";
        }

        /// <summary>
        /// Check if a state transition is valid.
        /// </summary>
        private bool IsValidTransition(PlaygroundState from, PlaygroundState to)
        {
            // Same state is always valid (no-op)
            if (from == to)
                return true;

            // Define valid transitions
            var validTransitions = new Dictionary<PlaygroundState, PlaygroundState[]>
            {
                { PlaygroundState.Idle, new[] { PlaygroundState.Editing } },
                { PlaygroundState.Editing, new[] { PlaygroundState.WaitingCompile, PlaygroundState.Idle } },
                { PlaygroundState.WaitingCompile, new[] { PlaygroundState.Compiling, PlaygroundState.Editing, PlaygroundState.Idle } },
                // Allow Compiling → Success for no-op/cleanup runs that skip execution
                { PlaygroundState.Compiling, new[] { PlaygroundState.Executing, PlaygroundState.Error, PlaygroundState.Success } },
                { PlaygroundState.Executing, new[] { PlaygroundState.Success, PlaygroundState.Error } },
                { PlaygroundState.Success, new[] { PlaygroundState.Editing, PlaygroundState.Idle } },
                { PlaygroundState.Error, new[] { PlaygroundState.Editing, PlaygroundState.Idle } }
            };

            // Check if transition exists in valid transitions
            if (validTransitions.TryGetValue(from, out PlaygroundState[] allowedStates))
            {
                return Array.Exists(allowedStates, state => state == to);
            }

            return false;
        }

        /// <summary>
        /// Get a user-friendly status message for current state.
        /// </summary>
        public string GetStatusMessage()
        {
            switch (currentState)
            {
                case PlaygroundState.Idle:
                    return "Ready - Start typing to see live updates";

                case PlaygroundState.Editing:
                    return "Editing...";

                case PlaygroundState.WaitingCompile:
                    return "Waiting for typing to complete...";

                case PlaygroundState.Compiling:
                    return "Compiling code...";

                case PlaygroundState.Executing:
                    return "Executing code...";

                case PlaygroundState.Success:
                    return "✓ Success!";

                case PlaygroundState.Error:
                    return $"✗ Error: {lastErrorMessage}";

                default:
                    return "Unknown state";
            }
        }

        /// <summary>
        /// Get statistics for debugging.
        /// </summary>
        public string GetStats()
        {
            double timeSinceTransition = UnityEditor.EditorApplication.timeSinceStartup - lastTransitionTime;
            return $"State: {currentState}, Time in state: {timeSinceTransition:F2}s";
        }
    }
}
#endif
