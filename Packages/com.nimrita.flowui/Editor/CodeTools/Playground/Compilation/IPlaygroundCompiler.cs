#if UNITY_EDITOR
using System;
using System.Reflection;

namespace Nimrita.FlowUI.Editor.Playground
{
    /// <summary>
    /// Interface for playground code compilers.
    /// Allows swapping between CodeDOM, Roslyn, or other compilation strategies.
    /// </summary>
    public interface IPlaygroundCompiler
    {
        /// <summary>
        /// Compiles the given C# code into an executable assembly.
        /// </summary>
        /// <param name="code">The C# code to compile</param>
        /// <param name="result">The compilation result containing assembly or errors</param>
        /// <returns>True if compilation succeeded</returns>
        bool TryCompile(string code, out CompilationResult result);

        /// <summary>
        /// Clears any cached compilation data.
        /// </summary>
        void ClearCache();
    }

    /// <summary>
    /// Result of a compilation attempt.
    /// </summary>
    public class CompilationResult
    {
        public bool Success { get; set; }
        public Assembly CompiledAssembly { get; set; }
        public string[] Errors { get; set; }
        public float CompilationTimeMs { get; set; }
        public bool WasCached { get; set; }

        public static CompilationResult CreateSuccess(Assembly assembly, float timeMs, bool cached = false)
        {
            return new CompilationResult
            {
                Success = true,
                CompiledAssembly = assembly,
                Errors = Array.Empty<string>(),
                CompilationTimeMs = timeMs,
                WasCached = cached
            };
        }

        public static CompilationResult CreateFailure(string[] errors, float timeMs)
        {
            return new CompilationResult
            {
                Success = false,
                Errors = errors ?? Array.Empty<string>(),
                CompilationTimeMs = timeMs,
                WasCached = false
            };
        }
    }
}
#endif
