#if UNITY_EDITOR
namespace Nimrita.FlowUI.Editor.Playground
{
    /// <summary>
    /// States for the Playground FSM (Finite State Machine).
    ///
    /// State Flow:
    /// Idle → Editing → WaitingCompile → Compiling → Executing → Success/Error → Editing
    /// </summary>
    public enum PlaygroundState
    {
        /// <summary>
        /// No code, waiting for input
        /// </summary>
        Idle,

        /// <summary>
        /// User is actively typing
        /// </summary>
        Editing,

        /// <summary>
        /// User stopped typing, waiting for debounce delay
        /// </summary>
        WaitingCompile,

        /// <summary>
        /// Code is being compiled
        /// </summary>
        Compiling,

        /// <summary>
        /// Compiled code is being executed
        /// </summary>
        Executing,

        /// <summary>
        /// Execution succeeded
        /// </summary>
        Success,

        /// <summary>
        /// Compilation or execution failed
        /// </summary>
        Error
    }
}
#endif
