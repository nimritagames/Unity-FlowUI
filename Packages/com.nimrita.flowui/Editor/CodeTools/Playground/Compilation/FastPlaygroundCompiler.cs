#if UNITY_EDITOR
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using UnityEngine;

namespace Nimrita.FlowUI.Editor.Playground
{
    /// <summary>
    /// Fast compiler with caching and incremental compilation support.
    /// Uses CodeDOM for now, will be replaced with Roslyn for even better performance.
    /// </summary>
    public class FastPlaygroundCompiler : IPlaygroundCompiler
    {
        private const string NAMESPACE = "UIBuilderPlaygroundGenerated";
        private const string CLASS_NAME = "GeneratedUIBuilder";
        private const string METHOD_NAME = "Execute";
        private const int WRAPPER_LINE_OFFSET = 12; // Lines before user code starts

        // Caching
        private string lastCompiledCode = "";
        private Assembly cachedAssembly = null;
        private CompilerParameters cachedParameters = null;

        public bool TryCompile(string userCode, out CompilationResult result)
        {
            float startTime = Time.realtimeSinceStartup;

            try
            {
                // Check cache first
                if (!string.IsNullOrEmpty(lastCompiledCode) && lastCompiledCode == userCode && cachedAssembly != null)
                {
                    float cacheTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                    result = CompilationResult.CreateSuccess(cachedAssembly, cacheTime, cached: true);
                    return true;
                }

                // Wrap user code
                string wrappedCode = WrapCodeForCompilation(userCode);

                // Compile
                CompilerResults compilerResults = CompileCode(wrappedCode);

                // Check for errors
                if (compilerResults.Errors.HasErrors)
                {
                    string[] errors = ExtractErrors(compilerResults.Errors);
                    float errorTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                    result = CompilationResult.CreateFailure(errors, errorTime);
                    return false;
                }

                // Success - cache it!
                lastCompiledCode = userCode;
                cachedAssembly = compilerResults.CompiledAssembly;

                float compileTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                result = CompilationResult.CreateSuccess(cachedAssembly, compileTime, cached: false);
                return true;
            }
            catch (Exception ex)
            {
                float errorTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                result = CompilationResult.CreateFailure(new[] { $"Compilation exception: {ex.Message}" }, errorTime);
                return false;
            }
        }

        public void ClearCache()
        {
            lastCompiledCode = "";
            cachedAssembly = null;
            cachedParameters = null;
        }

        private string WrapCodeForCompilation(string userCode)
        {
            StringBuilder sb = new StringBuilder();

            // Using statements
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine("using TMPro;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using UnityEngine.Events;");
            sb.AppendLine();

            // Namespace and class wrapper
            sb.AppendLine($"namespace {NAMESPACE}");
            sb.AppendLine("{");
            sb.AppendLine($"    public static class {CLASS_NAME}");
            sb.AppendLine("    {");
            sb.AppendLine($"        public static void {METHOD_NAME}(UIManager uiManager, Transform playgroundRoot)");
            sb.AppendLine("        {");

            // User code (indented)
            sb.AppendLine(IndentCode(userCode, 12));

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string IndentCode(string code, int spaces)
        {
            if (string.IsNullOrEmpty(code)) return "";

            string indent = new string(' ', spaces);
            string[] lines = code.Split('\n');
            return string.Join("\n", lines.Select(line => indent + line));
        }

        private CompilerResults CompileCode(string code)
        {
            // Use cached parameters if available
            if (cachedParameters == null)
            {
                cachedParameters = CreateCompilerParameters();
            }

            CSharpCodeProvider provider = new CSharpCodeProvider();
            return provider.CompileAssemblyFromSource(cachedParameters, code);
        }

        private CompilerParameters CreateCompilerParameters()
        {
            CompilerParameters parameters = new CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false,
                IncludeDebugInformation = false,
                TreatWarningsAsErrors = false
            };

            // Get all currently loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            HashSet<string> addedAssemblies = new HashSet<string>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                    {
                        string name = assembly.GetName().Name;

                        // Include necessary assemblies
                        if (ShouldIncludeAssembly(name))
                        {
                            if (addedAssemblies.Add(assembly.Location))
                            {
                                parameters.ReferencedAssemblies.Add(assembly.Location);
                            }
                        }
                    }
                }
                catch
                {
                    // Skip assemblies that can't be referenced
                }
            }

            return parameters;
        }

        private bool ShouldIncludeAssembly(string assemblyName)
        {
            // Skip old framework
            if (assemblyName == "mscorlib") return false;

            // Include these
            return assemblyName == "netstandard" ||
                   assemblyName == "System" ||
                   assemblyName == "System.Core" ||
                   assemblyName == "System.Linq" ||
                   assemblyName.StartsWith("UnityEngine") ||
                   assemblyName == "Unity.TextMeshPro" ||
                   assemblyName.Contains("Assembly-CSharp") ||
                   assemblyName.Contains("com.nimrita.flowui");
        }

        private string[] ExtractErrors(CompilerErrorCollection errors)
        {
            List<string> errorList = new List<string>();

            foreach (CompilerError error in errors)
            {
                if (error.IsWarning) continue;

                // Adjust line numbers to account for wrapper code
                int actualLine = error.Line - WRAPPER_LINE_OFFSET;
                string errorMsg = actualLine > 0
                    ? $"Line {actualLine}: {error.ErrorText}"
                    : error.ErrorText;

                errorList.Add(errorMsg);
            }

            return errorList.ToArray();
        }
    }
}
#endif
