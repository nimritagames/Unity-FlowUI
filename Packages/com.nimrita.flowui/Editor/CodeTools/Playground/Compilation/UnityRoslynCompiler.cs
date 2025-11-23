#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Nimrita.FlowUI.Editor.Playground
{
    /// <summary>
    /// Roslyn-based compiler that accesses Unity Editor's built-in Roslyn via reflection.
    /// This avoids DLL loading issues while getting full Roslyn power.
    ///
    /// Performance: ~20-50ms for new code, ~2-5ms for cached code.
    /// </summary>
    public class UnityRoslynCompiler : IPlaygroundCompiler
    {
        private const string NAMESPACE = "UIBuilderPlaygroundGenerated";
        private const string CLASS_NAME = "GeneratedUIBuilder";
        private const string METHOD_NAME = "Execute";
        private const int WRAPPER_LINE_OFFSET = 12;

        // Roslyn types accessed via reflection
        private Type csharpCompilationType;
        private Type csharpSyntaxTreeType;
        private Type compilationOptionsType;
        private Type metadataReferenceType;
        private Type emitResultType;
        private Type diagnosticType;
        private Type syntaxTreeType;

        // Base compilation instance
        private object baseCompilation;

        // Cache
        private Dictionary<string, CachedCompilation> compilationCache = new Dictionary<string, CachedCompilation>();
        private bool isInitialized = false;

        private class CachedCompilation
        {
            public Assembly Assembly;
            public float CompilationTimeMs;
        }

        public bool TryCompile(string userCode, out CompilationResult result)
        {
            float startTime = Time.realtimeSinceStartup;

            try
            {
                // Lazy initialization
                if (!isInitialized)
                {
                    if (!Initialize())
                    {
                        result = CompilationResult.CreateFailure(
                            new[] { "Failed to initialize Roslyn compiler. Using FastPlaygroundCompiler instead." },
                            0
                        );
                        return false;
                    }
                }

                // Check cache
                string hash = ComputeHash(userCode);
                if (compilationCache.TryGetValue(hash, out CachedCompilation cached))
                {
                    float cacheTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                    result = CompilationResult.CreateSuccess(cached.Assembly, cacheTime, cached: true);
                    return true;
                }

                // Wrap user code
                string wrappedCode = WrapCodeForCompilation(userCode);

                // Parse to syntax tree
                object syntaxTree = ParseText(wrappedCode);
                if (syntaxTree == null)
                {
                    result = CompilationResult.CreateFailure(new[] { "Failed to parse code" }, 0);
                    return false;
                }

                // Add syntax tree to base compilation
                object compilation = AddSyntaxTrees(baseCompilation, new[] { syntaxTree });

                // Emit to memory
                using (var ms = new MemoryStream())
                {
                    object emitResult = Emit(compilation, ms);
                    bool success = (bool)emitResultType.GetProperty("Success").GetValue(emitResult);

                    if (!success)
                    {
                        string[] errors = ExtractErrors(emitResult);
                        float errorTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                        result = CompilationResult.CreateFailure(errors, errorTime);
                        return false;
                    }

                    // Load assembly
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());

                    // Cache it
                    float compileTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                    compilationCache[hash] = new CachedCompilation
                    {
                        Assembly = assembly,
                        CompilationTimeMs = compileTime
                    };

                    result = CompilationResult.CreateSuccess(assembly, compileTime, cached: false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                float errorTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                Debug.LogError($"[UnityRoslynCompiler] Compilation exception: {ex}");
                result = CompilationResult.CreateFailure(
                    new[] { $"Compilation exception: {ex}" },
                    errorTime
                );
                return false;
            }
        }

        public void ClearCache()
        {
            compilationCache.Clear();
            Debug.Log("[UnityRoslynCompiler] Cache cleared");
        }

        private bool Initialize()
        {
            try
            {
                Debug.Log("[UnityRoslynCompiler] Initializing via Unity's built-in Roslyn...");

                if (!EnsureRoslynLoaded())
                {
                    Debug.LogWarning("[UnityRoslynCompiler] Roslyn assemblies not found in this Unity install. Falling back to FastPlaygroundCompiler.");
                    return false;
                }

                // Get Roslyn types from Unity's loaded assemblies
                csharpCompilationType = FindType("Microsoft.CodeAnalysis.CSharp.CSharpCompilation");
                csharpSyntaxTreeType = FindType("Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree");
                compilationOptionsType = FindType("Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions");
                metadataReferenceType = FindType("Microsoft.CodeAnalysis.MetadataReference");
                emitResultType = FindType("Microsoft.CodeAnalysis.Emit.EmitResult");
                diagnosticType = FindType("Microsoft.CodeAnalysis.Diagnostic");
                syntaxTreeType = FindType("Microsoft.CodeAnalysis.SyntaxTree");
                var outputKindType = FindType("Microsoft.CodeAnalysis.OutputKind");

                if (csharpCompilationType == null ||
                    csharpSyntaxTreeType == null ||
                    compilationOptionsType == null ||
                    metadataReferenceType == null ||
                    emitResultType == null ||
                    diagnosticType == null ||
                    syntaxTreeType == null ||
                    outputKindType == null)
                {
                    LogMissingTypes();
                    return false;
                }

                // Get metadata references
                var references = GetMetadataReferences();
                if (references == null)
                {
                    Debug.LogWarning("[UnityRoslynCompiler] Failed to build metadata references. Falling back to FastPlaygroundCompiler.");
                    return false;
                }

                // Create base compilation
                var createMethod = FindCSharpCompilationCreate();
                if (createMethod == null)
                {
                    Debug.LogWarning("[UnityRoslynCompiler] Unable to find CSharpCompilation.Create overload. Falling back to FastPlaygroundCompiler.");
                    return false;
                }

                // Create compilation options (OutputKind.DynamicallyLinkedLibrary)
                var dllKind = Enum.Parse(outputKindType, "DynamicallyLinkedLibrary");
                var optionsConstructor = FindCompilationOptionsConstructor(outputKindType);
                if (optionsConstructor == null)
                {
                    Debug.LogWarning("[UnityRoslynCompiler] CSharpCompilationOptions constructor not found.");
                    return false;
                }
                var optionsArgs = BuildCompilationOptionsArgs(optionsConstructor.GetParameters(), dllKind);
                var options = optionsConstructor.Invoke(optionsArgs);

                var args = BuildCreateArguments(createMethod.GetParameters(), references, options);
                baseCompilation = createMethod.Invoke(null, args);

                isInitialized = true;
                Debug.Log($"[UnityRoslynCompiler] Initialized successfully with {(references as Array)?.Length ?? 0} references");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityRoslynCompiler] Failed to initialize: {ex}");
                return false;
            }
        }

        private object GetMetadataReferences()
        {
            var referenceList = new List<object>();
            var createFromFileMethod = FindCreateFromFile(metadataReferenceType);
            var assemblyMetadataType = FindType("Microsoft.CodeAnalysis.AssemblyMetadata");
            var assemblyMetadataCreate = FindCreateFromFile(assemblyMetadataType);
            var assemblyMetadataGetReference = assemblyMetadataType?.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "GetReference" && m.GetParameters().Length == 0);

            var referencesAdded = new HashSet<string>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location)) continue;
                string name = assembly.GetName().Name;

                if (!ShouldIncludeAssembly(name)) continue;
                if (!referencesAdded.Add(assembly.Location)) continue;

                try
                {
                    object reference = null;

                    if (createFromFileMethod != null)
                    {
                        reference = InvokeWithDefaults(createFromFileMethod, null, assembly.Location);
                    }
                    else if (assemblyMetadataCreate != null && assemblyMetadataGetReference != null)
                    {
                        var metadata = InvokeWithDefaults(assemblyMetadataCreate, null, assembly.Location);
                        reference = assemblyMetadataGetReference.Invoke(metadata, null);
                    }
                    else
                    {
                        Debug.LogWarning("[UnityRoslynCompiler] No available API to create metadata references. Roslyn unavailable.");
                        return null;
                    }

                    referenceList.Add(reference);
                }
                catch
                {
                    // ignore assemblies Roslyn cannot load as metadata
                }
            }

            // Convert to array
            var array = Array.CreateInstance(metadataReferenceType, referenceList.Count);
            for (int i = 0; i < referenceList.Count; i++)
            {
                array.SetValue(referenceList[i], i);
            }

            Debug.Log($"[UnityRoslynCompiler] Metadata references collected: {referenceList.Count}");
            return array;
        }

        private object ParseText(string code)
        {
            var parseMethod = FindParseTextMethod();
            if (parseMethod == null)
            {
                Debug.LogWarning("[UnityRoslynCompiler] CSharpSyntaxTree.ParseText overload not found.");
                return null;
            }

            var parameters = parseMethod.GetParameters();
            var args = new object[parameters.Length];
            if (parameters.Length > 0)
            {
                args[0] = code;
            }
            for (int i = 1; i < parameters.Length; i++)
            {
                args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
            }

            return parseMethod.Invoke(null, args);
        }

        private object AddSyntaxTrees(object compilation, object[] trees)
        {
            var addMethod = FindAddSyntaxTrees();
            if (addMethod == null)
            {
                throw new InvalidOperationException("Roslyn AddSyntaxTrees method not found.");
            }

            var parameters = addMethod.GetParameters();
            var treeArray = Array.CreateInstance(syntaxTreeType, trees.Length);
            for (int i = 0; i < trees.Length; i++)
            {
                treeArray.SetValue(trees[i], i);
            }

            var args = new object[parameters.Length];
            if (parameters.Length > 0)
            {
                args[0] = treeArray;
            }
            for (int i = 1; i < parameters.Length; i++)
            {
                args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
            }

            return addMethod.Invoke(compilation, args);
        }

        private object Emit(object compilation, MemoryStream stream)
        {
            var emitMethod = FindEmitMethod();
            if (emitMethod == null)
            {
                throw new InvalidOperationException("Roslyn Emit method not found.");
            }

            var parameters = emitMethod.GetParameters();
            var args = new object[parameters.Length];
            args[0] = stream;

            for (int i = 1; i < parameters.Length; i++)
            {
                args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
            }

            return emitMethod.Invoke(compilation, args);
        }

        private string[] ExtractErrors(object emitResult)
        {
            var errors = new List<string>();

            // emitResult.Diagnostics
            var diagnosticsProperty = emitResultType.GetProperty("Diagnostics");
            var diagnostics = diagnosticsProperty.GetValue(emitResult) as System.Collections.IEnumerable;

            if (diagnostics != null)
            {
                foreach (var diagnostic in diagnostics)
                {
                    // diagnostic.Severity
                    var severityProperty = diagnosticType.GetProperty("Severity");
                    var severity = severityProperty.GetValue(diagnostic);

                    // Check if it's an error (DiagnosticSeverity.Error = 3)
                    if ((int)severity == 3)
                    {
                        var getMessageMethod = diagnosticType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                            .FirstOrDefault(m => m.Name == "GetMessage");
                        string message = getMessageMethod != null
                            ? InvokeWithDefaults(getMessageMethod, diagnostic) as string
                            : null;
                        errors.Add(message ?? "Unknown error");
                    }
                }
            }

            return errors.ToArray();
        }

        private string WrapCodeForCompilation(string userCode)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine("using TMPro;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using UnityEngine.Events;");
            sb.AppendLine();
            sb.AppendLine($"namespace {NAMESPACE}");
            sb.AppendLine("{");
            sb.AppendLine($"    public static class {CLASS_NAME}");
            sb.AppendLine("    {");
            sb.AppendLine($"        public static void {METHOD_NAME}(UIManager uiManager, Transform playgroundRoot)");
            sb.AppendLine("        {");
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

        private string ComputeHash(string code)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(code);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private void LogMissingTypes()
        {
            var missing = new List<string>();
            if (csharpCompilationType == null) missing.Add("CSharpCompilation");
            if (csharpSyntaxTreeType == null) missing.Add("CSharpSyntaxTree");
            if (compilationOptionsType == null) missing.Add("CSharpCompilationOptions");
            if (metadataReferenceType == null) missing.Add("MetadataReference");
            if (emitResultType == null) missing.Add("EmitResult");
            if (diagnosticType == null) missing.Add("Diagnostic");
            if (syntaxTreeType == null) missing.Add("SyntaxTree");

            Debug.LogWarning($"[UnityRoslynCompiler] Roslyn types not found after load ({string.Join(", ", missing)}). Falling back to FastPlaygroundCompiler.");
        }

        private MethodInfo FindCSharpCompilationCreate()
        {
            var methods = csharpCompilationType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "Create");

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length < 3) continue;
                if (parameters[0].ParameterType != typeof(string)) continue;

                if (!IsCompatibleEnumerable(parameters[1].ParameterType, syntaxTreeType)) continue;
                if (!IsCompatibleEnumerable(parameters[2].ParameterType, metadataReferenceType)) continue;

                Debug.Log($"[UnityRoslynCompiler] Using CSharpCompilation.Create overload ({string.Join(", ", parameters.Select(p => p.ParameterType.Name))})");
                return method;
            }

            return null;
        }

        private ConstructorInfo FindCompilationOptionsConstructor(Type outputKindType)
        {
            if (outputKindType == null) return null;

            return compilationOptionsType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(ctor =>
                {
                    var parameters = ctor.GetParameters();
                    return parameters.Length >= 1 && parameters[0].ParameterType == outputKindType;
                });
        }

        private object[] BuildCompilationOptionsArgs(ParameterInfo[] parameters, object dllKind)
        {
            var args = new object[parameters.Length];
            if (parameters.Length == 0) return args;

            args[0] = dllKind;

            for (int i = 1; i < parameters.Length; i++)
            {
                args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
            }

            return args;
        }

        private object[] BuildCreateArguments(ParameterInfo[] parameters, object references, object options)
        {
            var args = new object[parameters.Length];
            args[0] = "PlaygroundDynamicAssembly";

            for (int i = 1; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;

                if (IsCompatibleEnumerable(paramType, syntaxTreeType))
                {
                    args[i] = null; // no base syntax trees
                    continue;
                }

                if (IsCompatibleEnumerable(paramType, metadataReferenceType))
                {
                    args[i] = references;
                    continue;
                }

                if (compilationOptionsType.IsAssignableFrom(paramType))
                {
                    args[i] = options;
                    continue;
                }

                // Use default values when present, otherwise null
                args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
            }

            return args;
        }

        private bool IsCompatibleEnumerable(Type candidate, Type elementType)
        {
            if (candidate == null || elementType == null) return false;

            if (candidate.IsArray)
            {
                return candidate.GetElementType() == elementType;
            }

            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return candidate.GetGenericArguments()[0] == elementType;
            }

            // Covers cases like ImmutableArray<T> which still implement IEnumerable<T>
            return candidate.GetInterfaces()
                .Any(i => i.IsGenericType &&
                          i.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                          i.GetGenericArguments()[0] == elementType);
        }

        private MethodInfo FindEmitMethod()
        {
            return csharpCompilationType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "Emit") return false;
                    var parameters = m.GetParameters();
                    if (parameters.Length == 0) return false;
                    return typeof(Stream).IsAssignableFrom(parameters[0].ParameterType);
                });
        }

        private MethodInfo FindParseTextMethod()
        {
            return csharpSyntaxTreeType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "ParseText") return false;
                    var parameters = m.GetParameters();
                    if (parameters.Length == 0) return false;
                    return parameters[0].ParameterType == typeof(string);
                });
        }

        private MethodInfo FindAddSyntaxTrees()
        {
            return csharpCompilationType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "AddSyntaxTrees") return false;
                    var parameters = m.GetParameters();
                    if (parameters.Length == 0) return false;
                    return IsCompatibleEnumerable(parameters[0].ParameterType, syntaxTreeType);
                });
        }

        private bool EnsureRoslynLoaded()
        {
            if (HasRoslynAssembliesLoaded())
            {
                return true;
            }

            string contentsPath = EditorApplication.applicationContentsPath;
            var candidateDirectories = new[]
            {
                Path.Combine(contentsPath, "Tools", "ScriptUpdater"),
                Path.Combine(contentsPath, "DotNetSdkRoslyn"),
                Path.Combine(contentsPath, "MonoBleedingEdge", "lib", "mono", "4.5")
            };

            foreach (var directory in candidateDirectories)
            {
                if (!TryLoadRoslynFromDirectory(directory))
                {
                    continue;
                }

                if (HasRoslynAssembliesLoaded())
                {
                    Debug.Log($"[UnityRoslynCompiler] Loaded Roslyn from {directory}");
                    return true;
                }
            }

            return HasRoslynAssembliesLoaded();
        }

        private bool TryLoadRoslynFromDirectory(string directory)
        {
            try
            {
                var corePath = Path.Combine(directory, "Microsoft.CodeAnalysis.dll");
                var csharpPath = Path.Combine(directory, "Microsoft.CodeAnalysis.CSharp.dll");

                if (!File.Exists(corePath) || !File.Exists(csharpPath))
                {
                    return false;
                }

                Assembly.LoadFrom(corePath);
                Assembly.LoadFrom(csharpPath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UnityRoslynCompiler] Failed to load Roslyn from {directory}: {ex.Message}");
                return false;
            }
        }

        private bool HasRoslynAssembliesLoaded()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            bool hasCore = assemblies.Any(a => a.GetName().Name == "Microsoft.CodeAnalysis");
            bool hasCSharp = assemblies.Any(a => a.GetName().Name == "Microsoft.CodeAnalysis.CSharp");
            return hasCore && hasCSharp;
        }

        private Type FindType(string fullName)
        {
            var type = Type.GetType(fullName);
            if (type != null) return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName);
                if (type != null) return type;
            }

            return null;
        }

        private MethodInfo FindCreateFromFile(Type type)
        {
            if (type == null) return null;
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "CreateFromFile") return false;
                    var parameters = m.GetParameters();
                    return parameters.Length >= 1 && parameters[0].ParameterType == typeof(string);
                });
        }

        private object InvokeWithDefaults(MethodInfo method, object target, params object[] providedArgs)
        {
            var parameters = method.GetParameters();
            var args = new object[parameters.Length];

            // Fill provided args
            for (int i = 0; i < providedArgs.Length && i < args.Length; i++)
            {
                args[i] = providedArgs[i];
            }

            // Fill remaining with defaults/null
            for (int i = providedArgs.Length; i < parameters.Length; i++)
            {
                args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
            }

            return method.Invoke(target, args);
        }

        private bool ShouldIncludeAssembly(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName)) return false;

            return assemblyName == "mscorlib" ||
                   assemblyName == "System.Private.CoreLib" ||
                   assemblyName == "netstandard" ||
                   assemblyName.StartsWith("System") ||
                   assemblyName.StartsWith("UnityEngine") ||
                   assemblyName == "Unity.TextMeshPro" ||
                   assemblyName.Contains("Assembly-CSharp") ||
                   assemblyName.Contains("com.nimrita.flowui");
        }
    }
}
#endif
