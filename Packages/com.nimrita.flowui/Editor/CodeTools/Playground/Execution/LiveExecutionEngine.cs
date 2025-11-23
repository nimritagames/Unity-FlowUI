#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Nimrita.FlowUI.Editor.Playground
{
    /// <summary>
    /// Handles live code execution with smart debouncing and automatic updates.
    /// This is where the magic happens - code changes trigger automatic UI updates!
    /// </summary>
    public class LiveExecutionEngine
    {
        // Configuration
        private const float DEFAULT_DEBOUNCE_DELAY = 0.5f; // 500ms - longer to reduce spam
        private const string GENERATED_NAMESPACE = "UIBuilderPlaygroundGenerated";
        private const string GENERATED_CLASS = "GeneratedUIBuilder";
        private const string EXECUTE_METHOD = "Execute";

        // Dependencies
        private readonly IPlaygroundCompiler compiler;
        private readonly ContainerTracker containerTracker;
        private readonly SmartDebouncer debouncer;
        private readonly PlaygroundStateManager stateManager;
        private readonly Action<ExecutionStatus> onStatusChanged;

        // State
        private double lastExecutionTime = 0;
        private bool liveModeEnabled = false;
        private UIManager targetUIManager;

        // Stats
        private int totalExecutions = 0;
        private int successfulExecutions = 0;
        private int failedExecutions = 0;

        public LiveExecutionEngine(
            IPlaygroundCompiler compiler,
            ContainerTracker containerTracker,
            Action<ExecutionStatus> onStatusChanged = null)
        {
            this.compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
            this.containerTracker = containerTracker ?? throw new ArgumentNullException(nameof(containerTracker));
            this.debouncer = new SmartDebouncer();
            this.stateManager = new PlaygroundStateManager();
            this.stateManager.LogTransitions = true; // Enable transition logging
            this.onStatusChanged = onStatusChanged;
        }

        /// <summary>
        /// Gets or sets whether live mode is enabled.
        /// </summary>
        public bool LiveModeEnabled
        {
            get => liveModeEnabled;
            set
            {
                if (liveModeEnabled != value)
                {
                    liveModeEnabled = value;
                    if (liveModeEnabled)
                    {
                        NotifyStatus(ExecutionStatus.CreateLiveMode(true));
                    }
                    else
                    {
                        NotifyStatus(ExecutionStatus.CreateLiveMode(false));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the debounce delay in seconds.
        /// </summary>
        public float DebounceDelay
        {
            get => debouncer.DebounceDelay;
            set => debouncer.DebounceDelay = value;
        }

        /// <summary>
        /// Sets the target UIManager for execution.
        /// </summary>
        public void SetTargetUIManager(UIManager uiManager)
        {
            targetUIManager = uiManager;
        }

        /// <summary>
        /// Call this from EditorApplication.update to check for code changes.
        /// </summary>
        public void Update(string currentCode)
        {
            if (!liveModeEnabled) return;
            if (targetUIManager == null) return;

            // Check if smart debouncer says we should execute
            if (debouncer.ShouldExecute(currentCode, out string reason))
            {
                Debug.Log($"[LiveEngine] ⚡ Auto-executing: {reason}");
                ExecuteLive(currentCode);
            }
        }

        /// <summary>
        /// Manually executes code (for manual Execute button).
        /// </summary>
        public ExecutionResult ExecuteManual(string code, UIManager uiManager)
        {
            targetUIManager = uiManager;
            return ExecuteInternal(code, isManual: true);
        }

        /// <summary>
        /// Executes code in live mode (automatic).
        /// </summary>
        private void ExecuteLive(string code)
        {
            ExecuteInternal(code, isManual: false);
        }

        private ExecutionResult ExecuteInternal(string code, bool isManual)
        {
            totalExecutions++;
            lastExecutionTime = EditorApplication.timeSinceStartup;

            // Validate
            if (targetUIManager == null)
            {
                Debug.LogError("[LiveEngine] No UIManager!");
                var error = ExecutionResult.CreateError("No UIManager assigned!");
                stateManager.TransitionTo(PlaygroundState.Error, error.ErrorMessage);
                NotifyStatus(ExecutionStatus.CreateError(error.ErrorMessage));
                failedExecutions++;
                return error;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                // Empty code - just cleanup
                stateManager.TransitionTo(PlaygroundState.Compiling);
                NotifyStatus(ExecutionStatus.CreateCompiling());
                containerTracker.CleanupPrevious();
                debouncer.MarkExecuted(code);
                stateManager.TransitionTo(PlaygroundState.Success);
                NotifyStatus(ExecutionStatus.CreateSuccess(0, 0));
                successfulExecutions++;
                return ExecutionResult.CreateSuccess(0);
            }

            // Compile
            stateManager.TransitionTo(PlaygroundState.Compiling);
            NotifyStatus(ExecutionStatus.CreateCompiling());

            if (!compiler.TryCompile(code, out CompilationResult compilationResult))
            {
                // Compilation failed
                string errorMsg = string.Join("\n", compilationResult.Errors);
                failedExecutions++;
                stateManager.TransitionTo(PlaygroundState.Error, errorMsg);
                NotifyStatus(ExecutionStatus.CreateError(errorMsg));
                return ExecutionResult.CreateError(errorMsg);
            }

            // Cleanup previous UI
            containerTracker.CleanupPrevious();

            // Execute
            try
            {
                stateManager.TransitionTo(PlaygroundState.Executing);
                NotifyStatus(ExecutionStatus.CreateExecuting());

                // Mark for undo
                Undo.RecordObject(targetUIManager, "Playground Execute");

                // Get the generated method
                Type generatedType = compilationResult.CompiledAssembly.GetType($"{GENERATED_NAMESPACE}.{GENERATED_CLASS}");
                MethodInfo executeMethod = generatedType.GetMethod(EXECUTE_METHOD);

                // Begin tracking (create container root)
                containerTracker.BeginExecution();

                // Execute! Pass UIManager AND root container transform
                executeMethod.Invoke(null, new object[] { targetUIManager, containerTracker.RootTransform });

                // End tracking (collect created objects from container)
                containerTracker.EndExecution();

                // Mark scene dirty
                EditorUtility.SetDirty(targetUIManager);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
                );

                // Success!
                debouncer.MarkExecuted(code);
                successfulExecutions++;

                float totalTime = compilationResult.CompilationTimeMs;
                int objectCount = containerTracker.TrackedObjectCount;

                stateManager.TransitionTo(PlaygroundState.Success);
                NotifyStatus(ExecutionStatus.CreateSuccess(totalTime, objectCount, compilationResult.WasCached));

                return ExecutionResult.CreateSuccess(totalTime, objectCount);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LiveEngine] Execution exception: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.LogError($"[LiveEngine] Inner exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                }
                failedExecutions++;
                string errorMsg = $"Execution failed: {ex.InnerException?.Message ?? ex.Message}";
                stateManager.TransitionTo(PlaygroundState.Error, errorMsg);
                NotifyStatus(ExecutionStatus.CreateError(errorMsg));
                return ExecutionResult.CreateError(errorMsg);
            }
        }

        /// <summary>
        /// Clears compilation cache, debouncer state, and resets FSM.
        /// </summary>
        public void ClearCache()
        {
            compiler.ClearCache();
            debouncer.Reset();
            stateManager.Reset();
        }

        /// <summary>
        /// Gets execution statistics.
        /// </summary>
        public ExecutionStats GetStats()
        {
            return new ExecutionStats
            {
                TotalExecutions = totalExecutions,
                SuccessfulExecutions = successfulExecutions,
                FailedExecutions = failedExecutions,
                LastExecutionTime = lastExecutionTime
            };
        }

        private void NotifyStatus(ExecutionStatus status)
        {
            onStatusChanged?.Invoke(status);
        }
    }

    /// <summary>
    /// Result of a code execution attempt.
    /// </summary>
    public class ExecutionResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public float ExecutionTimeMs { get; set; }
        public int ObjectsCreated { get; set; }

        public static ExecutionResult CreateSuccess(float timeMs, int objectCount = 0)
        {
            return new ExecutionResult
            {
                Success = true,
                ExecutionTimeMs = timeMs,
                ObjectsCreated = objectCount
            };
        }

        public static ExecutionResult CreateError(string error)
        {
            return new ExecutionResult
            {
                Success = false,
                ErrorMessage = error
            };
        }
    }

    /// <summary>
    /// Current status of the execution engine.
    /// </summary>
    public class ExecutionStatus
    {
        public enum State { Idle, Compiling, Executing, Success, Error, LiveModeChanged }

        public State CurrentState { get; set; }
        public string Message { get; set; }
        public float CompilationTimeMs { get; set; }
        public int ObjectsCreated { get; set; }
        public bool WasCached { get; set; }
        public bool LiveModeEnabled { get; set; }

        public static ExecutionStatus CreateCompiling()
        {
            return new ExecutionStatus { CurrentState = State.Compiling, Message = "Compiling..." };
        }

        public static ExecutionStatus CreateExecuting()
        {
            return new ExecutionStatus { CurrentState = State.Executing, Message = "Executing..." };
        }

        public static ExecutionStatus CreateSuccess(float timeMs, int objectCount, bool cached = false)
        {
            string cacheInfo = cached ? " (cached)" : "";
            return new ExecutionStatus
            {
                CurrentState = State.Success,
                Message = $"✓ Success in {timeMs:F1}ms{cacheInfo} - {objectCount} objects",
                CompilationTimeMs = timeMs,
                ObjectsCreated = objectCount,
                WasCached = cached
            };
        }

        public static ExecutionStatus CreateError(string error)
        {
            return new ExecutionStatus { CurrentState = State.Error, Message = error };
        }

        public static ExecutionStatus CreateLiveMode(bool enabled)
        {
            return new ExecutionStatus
            {
                CurrentState = State.LiveModeChanged,
                LiveModeEnabled = enabled,
                Message = enabled ? "Live Mode ON" : "Live Mode OFF"
            };
        }
    }

    /// <summary>
    /// Execution statistics.
    /// </summary>
    public class ExecutionStats
    {
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public double LastExecutionTime { get; set; }

        public float SuccessRate => TotalExecutions > 0 ? (float)SuccessfulExecutions / TotalExecutions * 100f : 0f;
    }
}
#endif
