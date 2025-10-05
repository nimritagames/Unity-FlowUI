#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public partial class UIManagerEditor : Editor
{
    #region Handler Generation

    private void DrawHandlerSettings()
    {
        // Get scene name 
        string sceneName = uiManager.gameObject.scene.name;
        string sanitizedSceneName = SanitizeIdentifier(sceneName);

        DrawSectionHeader("Handler Generation",
            "Generate handlers to manage UI interactions. \nHandlers provide methods for responding to button clicks, slider changes, and panel management.");

        EditorGUILayout.Space(10);

        // Handler status panel with enhanced styling
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Status indicator with icon
        Rect statusRect = EditorGUILayout.GetControlRect(false, 36);
        bool handlerExists = HandlerFileExists();
        bool libraryExists = IsLibraryGenerated(sceneName);

        if (Event.current.type == EventType.Repaint)
        {
            // Background with subtle gradient
            Color bgColor = handlerExists ?
                new Color(0.2f, 0.4f, 0.2f, 0.2f) : // Green tint for success
                new Color(0.4f, 0.4f, 0.5f, 0.2f);  // Neutral for not generated

            EditorGUI.DrawRect(statusRect, bgColor);
        }

        // Status icon
        Texture2D statusIcon;
        if (handlerExists)
        {
            statusIcon = EditorGUIUtility.IconContent("d_forward@2x").image as Texture2D;
            GUI.DrawTexture(
                new Rect(statusRect.x + 12, statusRect.y + 9, 18, 18),
                statusIcon
            );
        }
        else
        {
            statusIcon = EditorGUIUtility.IconContent("d_console.infoicon@2x").image as Texture2D;
            GUI.DrawTexture(
                new Rect(statusRect.x + 12, statusRect.y + 9, 18, 18),
                statusIcon
            );
        }

        // Status text
        GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = handlerExists ? new Color(0.3f, 0.7f, 0.3f) : new Color(0.7f, 0.7f, 0.8f) }
        };

        string statusText = handlerExists ?
            "Handler Status: ? Generated" :
            "Handler Status: Not Generated";

        EditorGUI.LabelField(
            new Rect(statusRect.x + 40, statusRect.y + 9, statusRect.width - 50, 18),
            statusText,
            statusStyle
        );

        // Last updated date if available
        if (handlerExists)
        {
            GUIStyle dateStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };

            // Get file date info (check both generated and user files)
            string generatedFilePath = Path.Combine(handlerOutputPath, $"{handlerClassPrefix}{sanitizedSceneName}UIHandler.g.cs");
            string userFilePath = Path.Combine(handlerOutputPath, $"{handlerClassPrefix}{sanitizedSceneName}UIHandler.cs");
            DateTime lastModified = File.Exists(generatedFilePath) ? File.GetLastWriteTime(generatedFilePath) : File.GetLastWriteTime(userFilePath);

            string dateText = $"    Last updated: {lastModified.ToString(" MMM d, yyyy 'at' h:mm tt")}";

            EditorGUI.LabelField(
                new Rect(statusRect.x + 200, statusRect.y + 12, statusRect.width - 210, 14),
                dateText,
                dateStyle
            );
        }

        // Library dependency warning if needed
        if (!libraryExists)
        {
            GUIStyle warningStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                wordWrap = true,
                normal = { textColor = new Color(0.9f, 0.6f, 0.2f) }
            };

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);
            EditorGUILayout.LabelField(
                "?? Library must be generated first. Please go to the Library Generation tab.",
                warningStyle
            );
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(15);

        // Quick actions with enhanced styling
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // Generate button
        float buttonWidth = 180;
        float buttonHeight = 36;
        Rect generateRect = GUILayoutUtility.GetRect(buttonWidth, buttonHeight);

        EditorGUI.BeginDisabledGroup(!libraryExists);

        string generateButtonText = handlerExists ? "Regenerate Handler" : "Generate Handler";
        DrawActionButton(
            generateRect,
            generateButtonText,
            "Create or update the UI Handler file",
            () => GenerateHandlerTemplate(),
            true
        );

        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);

        // View code button (only if handler exists)
        if (handlerExists)
        {
            Rect viewRect = GUILayoutUtility.GetRect(buttonWidth - 60, buttonHeight);

            DrawActionButton(
                viewRect,
                "View Code",
                "Open the generated handler file",
                () => PingUIHandler()
            );

            GUILayout.Space(10);
        }

        // Preview code button
        Rect previewRect = GUILayoutUtility.GetRect(buttonWidth - 60, buttonHeight);

        EditorGUI.BeginDisabledGroup(!libraryExists);

        DrawActionButton(
            previewRect,
            "Preview Code",
            "Show a preview of the handler code",
            () => ShowHandlerCodePreview()
        );

        EditorGUI.EndDisabledGroup();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(15);

        // Settings section
        DrawSectionHeader("Output Settings", null);
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Output path with browse button
        EditorGUILayout.BeginHorizontal();
        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.8f, 0.8f, 0.85f) }
        };

        EditorGUILayout.LabelField(
            new GUIContent("Output Path", "The folder where the generated handler files will be stored"),
            labelStyle,
            GUILayout.Width(100)
        );

        // Custom field style
        GUIStyle pathStyle = new GUIStyle(EditorStyles.textField)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
        };

        string newPath = EditorGUILayout.TextField(handlerOutputPath, pathStyle);
        if (newPath != handlerOutputPath)
        {
            handlerOutputPath = newPath;
            EditorPrefs.SetString(HANDLER_OUTPUT_PATH_KEY, handlerOutputPath);
        }

        GUIStyle browseStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fixedHeight = 18,
            fontSize = 10
        };

        if (GUILayout.Button("Browse...", browseStyle, GUILayout.Width(70)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Handler Output Path", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                handlerOutputPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                EditorPrefs.SetString(HANDLER_OUTPUT_PATH_KEY, handlerOutputPath);
            }
        }

        EditorGUILayout.EndHorizontal();

        // Validate path
        if (string.IsNullOrEmpty(handlerOutputPath) || !handlerOutputPath.StartsWith("Assets"))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox("Warning: The output path must be inside the 'Assets' folder.", MessageType.Warning);
        }

        EditorGUILayout.Space(8);

        // Namespace with styling
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(
            new GUIContent("Namespace", "The namespace for the generated UI Handler"),
            labelStyle,
            GUILayout.Width(100)
        );

        string newNamespace = EditorGUILayout.TextField(handlerNamespace, pathStyle);
        if (newNamespace != handlerNamespace)
        {
            handlerNamespace = newNamespace;
            EditorPrefs.SetString(HANDLER_NAMESPACE_KEY, handlerNamespace);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Class prefix with styling
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(
            new GUIContent("Class Prefix", "A prefix to use for the generated handler classes (optional)"),
            labelStyle,
            GUILayout.Width(100)
        );

        string newPrefix = EditorGUILayout.TextField(handlerClassPrefix, pathStyle);
        if (newPrefix != handlerClassPrefix)
        {
            handlerClassPrefix = newPrefix;
            EditorPrefs.SetString(HANDLER_CLASS_PREFIX_KEY, handlerClassPrefix);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Preview section
        EditorGUILayout.LabelField("Generated File Preview:", labelStyle);
        EditorGUILayout.Space(2);

        // File path preview
        GUIStyle previewPathStyle = new GUIStyle(EditorStyles.textField)
        {
            fontSize = 10,
            normal = { textColor = new Color(0.7f, 0.7f, 0.75f) }
        };

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Generated:", GUILayout.Width(70));
        GUILayout.TextField($"{handlerOutputPath}/{handlerClassPrefix}{sanitizedSceneName}UIHandler.g.cs", previewPathStyle, GUILayout.Height(20));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("User Code:", GUILayout.Width(70));
        GUILayout.TextField($"{handlerOutputPath}/{handlerClassPrefix}{sanitizedSceneName}UIHandler.cs", previewPathStyle, GUILayout.Height(20));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(15);

        // Handler features section
        DrawSectionHeader("Handler Features", "The following features will be included in the generated handler");
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Draw feature boxes with icons
        GUIStyle featureStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.8f, 0.8f, 0.9f) }
        };

        DrawFeatureItem("Component Referencing", "Automatically get references to UI components via UIManager", featureStyle);
        DrawFeatureItem("Event Handling", "Event handlers for Buttons, Toggles, Sliders, Input Fields, etc.", featureStyle);
        DrawFeatureItem("Panel Management", "Methods to show/hide UI panels with optional animations", featureStyle);
        DrawFeatureItem("Lifecycle Hooks", "Properly initializes and cleans up UI elements and listeners", featureStyle);

        EditorGUILayout.Space(5);

        // Element detection section
        GUIStyle detectedStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 9,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.7f, 0.7f, 0.75f) }
        };

        // Count detected UI elements by type
        int buttonCount = CountUIElementsByType("Button", uiManager);
        int toggleCount = CountUIElementsByType("Toggle", uiManager);
        int sliderCount = CountUIElementsByType("Slider", uiManager);
        int panelCount = CountUIElementsByType("Panel", uiManager);
        int inputCount = CountUIElementsByType("InputField", uiManager);
        int textCount = CountUIElementsByType("Text", uiManager);

        // Detect UI elements
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Detected UI Elements:", detectedStyle, GUILayout.Width(120));
        string elementsText = $"Buttons: {buttonCount} | Toggles: {toggleCount} | Sliders: {sliderCount} | Panels: {panelCount} | Inputs: {inputCount} | Texts: {textCount}";
        EditorGUILayout.LabelField(elementsText, detectedStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(15);

        // Code example section
        DrawSectionHeader("Usage Example", null);
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUIStyle codeStyle = new GUIStyle(EditorStyles.textArea)
        {
            fontSize = 11,
            wordWrap = true,
            richText = true,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        string example =
            "<color=#569CD6>// Add this component to your scene and assign UIManager</color>\n" +
            "<color=#569CD6>void</color> <color=#DCDCAA>Start</color>() {\n" +
            "    <color=#6A9955>// All UI elements are automatically referenced</color>\n" +
            $"    <color=#DCDCAA>Show{GetFirstPanelName(uiManager)}</color>(true);\n" +
            "}\n\n" +
            "<color=#569CD6>// Auto-generated event handler</color>\n" +
            "<color=#569CD6>private void</color> <color=#DCDCAA>On{GetFirstButtonName(uiManager)}Clicked</color>() {\n" +
            "    <color=#6A9955>// Your game logic here</color>\n" +
            "    Debug.Log(<color=#CE9178>\"Button clicked!\"</color>);\n" +
            "}\n";

        // Disable editing in the Inspector
        EditorGUILayout.SelectableLabel(example, codeStyle, GUILayout.Height(150));

        EditorGUILayout.Space(5);

        GUIStyle noteStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 9,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };

        EditorGUILayout.LabelField(
            "Note: The handler provides a clean MonoBehaviour interface to your UI elements, separating UI logic from game logic " +
            "and reducing boilerplate code for setting up event listeners.",
            noteStyle
        );

        EditorGUILayout.EndVertical();
    }

    private void DrawFeatureItem(string title, string description, GUIStyle style)
    {
        EditorGUILayout.BeginHorizontal();

        // Checkmark
        GUIStyle checkStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            normal = { textColor = new Color(0.4f, 0.8f, 0.4f) }
        };

        EditorGUILayout.LabelField("?", checkStyle, GUILayout.Width(20));

        // Title and description
        EditorGUILayout.BeginVertical();

        // Title in bold
        GUIStyle titleStyle = new GUIStyle(style)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };

        EditorGUILayout.LabelField(title, titleStyle);

        // Description in normal text, slightly indented
        GUIStyle descStyle = new GUIStyle(style)
        {
            fontSize = 10,
            normal = { textColor = new Color(0.7f, 0.7f, 0.75f) }
        };

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);
        EditorGUILayout.LabelField(description, descStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
    }

    private int CountUIElementsByType(string typeName, UIManager manager)
    {
        var elements = manager.GetAllUICategories()
            .FirstOrDefault(c => c.name.Equals(typeName, StringComparison.OrdinalIgnoreCase))?.references;

        return elements?.Count ?? 0;
    }

    private string GetFirstPanelName(UIManager manager)
    {
        var panelCategory = manager.GetAllUICategories()
            .FirstOrDefault(c => c.name.Equals("Panel", StringComparison.OrdinalIgnoreCase));

        if (panelCategory != null && panelCategory.references.Count > 0)
        {
            return SanitizeIdentifier(panelCategory.references[0].name);
        }

        return "MainMenu";
    }

    /// <summary>
    /// Shows a preview of the handler code.
    /// </summary>
    private void ShowHandlerCodePreview()
    {
        if (!IsLibraryGenerated(uiManager.gameObject.scene.name))
        {
            EditorUtility.DisplayDialog("Library Not Generated",
                "You need to generate the UI Library first before previewing the handler code.",
                "OK");
            return;
        }

        string sceneName = uiManager.gameObject.scene.name;
        string sanitizedSceneName = SanitizeIdentifier(sceneName);
        string previewCode = GenerateHandlerPreviewCode(sceneName);
        string handlerFileName = $"{handlerClassPrefix}{sanitizedSceneName}Handler.cs";

        CodePreviewWindow codePreviewWindow = EditorWindow.GetWindow<CodePreviewWindow>(true, "Handler Code Preview", true);
        codePreviewWindow.SetContent(previewCode, handlerFileName);
    }

    private string GenerateHandlerPreviewCode(string sceneName)
    {
        string sanitizedSceneName = SanitizeIdentifier(sceneName);
        StringBuilder sb = new StringBuilder();

        // Sample code generator
        sb.AppendLine("// This is a preview of the handler code that will be generated");
        sb.AppendLine("// Actual code may vary based on your UI elements");
        sb.AppendLine();

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using System;");
        sb.AppendLine($"using {libraryNamespace}.{sanitizedSceneName};");
        sb.AppendLine();

        sb.AppendLine($"namespace {handlerNamespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    public class {handlerClassPrefix}{sanitizedSceneName}UIHandler : MonoBehaviour");
        sb.AppendLine("    {");
        sb.AppendLine("        [SerializeField] private UIManager uiManager;");
        sb.AppendLine("        ");
        sb.AppendLine("        private bool isInitialized;");
        sb.AppendLine("        ");
        sb.AppendLine("        private void Awake()");
        sb.AppendLine("        {");
        sb.AppendLine("            ValidateReferences();");
        sb.AppendLine("        }");
        sb.AppendLine("        ");
        sb.AppendLine("        private void Start()");
        sb.AppendLine("        {");
        sb.AppendLine("            Initialize();");
        sb.AppendLine("        }");
        sb.AppendLine("        ");
        sb.AppendLine("        private void Initialize()");
        sb.AppendLine("        {");
        sb.AppendLine("            // Setup event listeners");
        sb.AppendLine("            SetupButtons();");
        sb.AppendLine("            SetupToggles();");
        sb.AppendLine("            // etc...");
        sb.AppendLine("            ");
        sb.AppendLine("            isInitialized = true;");
        sb.AppendLine("        }");
        sb.AppendLine("        ");

        // Generate sample methods for actual UI elements
        // Add sample methods for buttons
        var buttonCategory = uiManager.GetAllUICategories().FirstOrDefault(c => c.name == "Button");
        if (buttonCategory != null && buttonCategory.references.Any())
        {
            sb.AppendLine("        private void SetupButtons()");
            sb.AppendLine("        {");

            foreach (var button in buttonCategory.references.Take(3))
            {
                string sanitizedName = SanitizeIdentifier(button.name);
                sb.AppendLine($"            // Set up {button.name} button");
                sb.AppendLine($"            uiManager.SetUIComponentListener<Button>({libraryClassPrefix}{sanitizedSceneName}.{sanitizedName}_Path, On{sanitizedName}Clicked);");
            }

            if (buttonCategory.references.Count > 3)
            {
                sb.AppendLine("            // Additional buttons...");
            }

            sb.AppendLine("        }");
            sb.AppendLine("        ");

            // Add a sample event handler for one button
            if (buttonCategory.references.Count > 0)
            {
                string sanitizedName = SanitizeIdentifier(buttonCategory.references[0].name);
                sb.AppendLine($"        private void On{sanitizedName}Clicked()");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.Log(\"{sanitizedName} clicked\");");
                sb.AppendLine("            // TODO: Implement your button logic here");
                sb.AppendLine("        }");
                sb.AppendLine("        ");
            }
        }

        // Add panel methods if there are any panels
        var panelCategory = uiManager.GetAllUICategories().FirstOrDefault(c => c.name == "Panel");
        if (panelCategory != null && panelCategory.references.Any())
        {
            string sanitizedName = SanitizeIdentifier(panelCategory.references[0].name);

            sb.AppendLine($"        public void Show{sanitizedName}(bool hideOthers = true)");
            sb.AppendLine("        {");
            sb.AppendLine($"            uiManager.SetPanelActive({libraryClassPrefix}{sanitizedSceneName}.{sanitizedName}_Path, true, false, hideOthers);");
            sb.AppendLine("        }");
            sb.AppendLine("        ");
        }

        sb.AppendLine("        // Many more methods will be generated...");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private void GenerateHandlerTemplate()
    {
        // Check for naming issues before generating
        if (!SmartNamingController.ValidateNamingForCodeGeneration(uiManager, "Handler Generation"))
        {
            return; // User chose to fix naming first or cancelled
        }

        // Get scene name and sanitize it.
        string sceneName = uiManager.gameObject.scene.name;
        string sanitizedSceneName = SanitizeIdentifier(sceneName);
        string className = $"{handlerClassPrefix}{sanitizedSceneName}UIHandler";
        string generatedFilePath = Path.Combine(handlerOutputPath, $"{className}.g.cs");
        string userFilePath = Path.Combine(handlerOutputPath, $"{className}.cs");

        // Check that the library has been generated first.
        if (!IsLibraryGenerated(sceneName))
        {
            EditorUtility.DisplayDialog("Library Not Found",
                "The UI Library has not been generated yet.\n\nPlease generate the library first before generating the handler template.",
                "OK");
            return;
        }

        // Store existing user methods for migration hints
        HashSet<string> existingUserMethods = new HashSet<string>();
        if (File.Exists(generatedFilePath))
        {
            existingUserMethods = ExtractEventHandlerMethodNames(generatedFilePath);
        }

        // Generate the GENERATED file (.g.cs) - always overwritten
        GenerateGeneratedHandlerFile(generatedFilePath, className, sanitizedSceneName, existingUserMethods);

        // Generate the USER file (.cs) - only created if it doesn't exist
        GenerateUserHandlerFile(userFilePath, className, sanitizedSceneName);

        AssetDatabase.Refresh();
        Debug.Log($"Generated UI Handler files:\n- {generatedFilePath}\n- {userFilePath}");

        ShowQuickTip("Handler Generated", "UI Handler code has been successfully generated using partial classes. The .g.cs file contains auto-generated code and will be overwritten on regeneration. The .cs file is for your custom logic and will never be touched.");
    }

    private void GenerateGeneratedHandlerFile(string filePath, string className, string sanitizedSceneName, HashSet<string> existingMethods)
    {
        var sb = new StringBuilder();

        // Collect current UI methods to detect changes
        HashSet<string> currentMethods = new HashSet<string>();
        CollectEventHandlerMethods(uiManager, currentMethods);

        #region File Header & Using Statements

        sb.AppendLine("// ---------------------------------------------------");
        sb.AppendLine("// AUTO-GENERATED CODE - DO NOT MODIFY");
        sb.AppendLine("// Generated by UIFramework");
        sb.AppendLine($"// Scene: {sanitizedSceneName}");
        sb.AppendLine($"// Date: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
        sb.AppendLine("//");
        sb.AppendLine("// This file is regenerated whenever you update the Handler.");
        sb.AppendLine("// All custom code should be placed in the corresponding .cs file.");
        sb.AppendLine("// ---------------------------------------------------");
        sb.AppendLine();

        // Migration hints
        if (existingMethods.Count > 0)
        {
            var added = currentMethods.Except(existingMethods).ToList();
            var removed = existingMethods.Except(currentMethods).ToList();

            if (added.Any() || removed.Any())
            {
                sb.AppendLine("// ⚠️ MIGRATION HINTS ⚠️");
                sb.AppendLine("// UI elements have changed since last generation:");

                if (added.Any())
                {
                    sb.AppendLine("//");
                    sb.AppendLine("// ADDED - Implement these partial methods in the .cs file:");
                    foreach (var method in added)
                    {
                        sb.AppendLine($"//   - {method}()");
                    }
                }

                if (removed.Any())
                {
                    sb.AppendLine("//");
                    sb.AppendLine("// REMOVED - These methods are no longer needed (safe to delete from .cs file):");
                    foreach (var method in removed)
                    {
                        sb.AppendLine($"//   - {method}()");
                    }
                }

                sb.AppendLine("// ---------------------------------------------------");
                sb.AppendLine();
            }
        }

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using UnityEngine.Events;");
        sb.AppendLine("using TMPro;");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections;");
        sb.AppendLine($"using {libraryNamespace}.{sanitizedSceneName};");
        sb.AppendLine();

        #endregion

        #region Namespace & Partial Class Declaration

        sb.AppendLine($"namespace {handlerNamespace}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Handles UI interactions for {sanitizedSceneName}.");
        sb.AppendLine("    /// This class manages all UI element references, events, and state management");
        sb.AppendLine($"    /// for the {sanitizedSceneName} scene.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public partial class {className} : MonoBehaviour");
        sb.AppendLine("    {");

        #endregion

        #region Fields & Properties

        sb.AppendLine("        #region Fields");
        sb.AppendLine();
        sb.AppendLine("        [Header(\"Dependencies\")]");
        sb.AppendLine("        [SerializeField, Tooltip(\"Reference to the UI Manager in the scene\")]");
        sb.AppendLine("        private UIManager uiManager;");
        sb.AppendLine();
        sb.AppendLine("        // Internal state tracking");
        sb.AppendLine("        private bool isInitialized;");
        sb.AppendLine("        private bool areListenersSetup;");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();

        sb.AppendLine("        #region Properties");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Gets whether the handler is properly initialized.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public bool IsInitialized => isInitialized;");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();

        #endregion

        #region Unity Lifecycle Methods

        sb.AppendLine("        #region Unity Methods");
        sb.AppendLine();
        sb.AppendLine("        private void Awake()");
        sb.AppendLine("        {");
        sb.AppendLine("            ValidateReferences();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        private void OnEnable()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (isInitialized)");
        sb.AppendLine("            {");
        sb.AppendLine("                SetupListeners();");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        private void Start()");
        sb.AppendLine("        {");
        sb.AppendLine("            Initialize();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        private void OnDisable()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (isInitialized)");
        sb.AppendLine("            {");
        sb.AppendLine("                CleanupListeners();");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        private void OnDestroy()");
        sb.AppendLine("        {");
        sb.AppendLine("            Cleanup();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();

        #endregion

        #region Initialization Methods

        sb.AppendLine("        #region Initialization");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Validates all required references.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private void ValidateReferences()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (uiManager == null)");
        sb.AppendLine("            {");
        sb.AppendLine($"                Debug.LogError($\"[{className}] UIManager reference is missing! Please assign it in the inspector.\", this);");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Initializes the UI handler.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private void Initialize()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (isInitialized) return;");
        sb.AppendLine("            if (uiManager == null) return;");
        sb.AppendLine();
        sb.AppendLine("            // Initialize the UI library with the UIManager");
        sb.AppendLine($"            {sanitizedSceneName}.UI.Initialize(uiManager);");
        sb.AppendLine();
        sb.AppendLine("            InitializeUI();");
        sb.AppendLine("            SetupListeners();");
        sb.AppendLine();
        sb.AppendLine("            isInitialized = true;");
        sb.AppendLine("            Debug.Log($\"[{this.GetType().Name}] Initialized\");");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Cleans up the UI handler.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private void Cleanup()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!isInitialized) return;");
        sb.AppendLine();
        sb.AppendLine("            CleanupListeners();");
        sb.AppendLine("            isInitialized = false;");
        sb.AppendLine("            Debug.Log($\"[{this.GetType().Name}] Cleaned up\");");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Partial method for custom initialization logic");
        sb.AppendLine("        partial void InitializeUI();");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();

        #endregion

        #region UI Setup Methods

        sb.AppendLine("        #region UI Setup Methods");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Sets up all UI event listeners.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private void SetupListeners()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (uiManager == null || areListenersSetup) return;");
        sb.AppendLine();
        sb.AppendLine("            SetupButtons();");
        sb.AppendLine("            SetupToggles();");
        sb.AppendLine("            SetupSliders();");
        sb.AppendLine("            SetupInputFields();");
        sb.AppendLine("            SetupDropdowns();");
        sb.AppendLine();
        sb.AppendLine("            areListenersSetup = true;");
        sb.AppendLine("            Debug.Log($\"[{this.GetType().Name}] Listeners setup complete\");");
        sb.AppendLine("        }");
        sb.AppendLine();
        GenerateSetupForTypePartial(sb, "Button", uiManager, sanitizedSceneName);
        GenerateSetupForTypePartial(sb, "Toggle", uiManager, sanitizedSceneName);
        GenerateSetupForTypePartial(sb, "Slider", uiManager, sanitizedSceneName);
        GenerateSetupForTypePartial(sb, "InputField", uiManager, sanitizedSceneName);
        GenerateSetupForTypePartial(sb, "Dropdown", uiManager, sanitizedSceneName);
        sb.AppendLine("        #endregion");
        sb.AppendLine();

        #endregion

        #region Partial Method Declarations

        // Calculate which methods are new
        var addedMethods = currentMethods.Except(existingMethods).ToHashSet();

        sb.AppendLine("        #region Partial Method Declarations");
        sb.AppendLine();
        sb.AppendLine("        // Implement these methods in the .cs file to handle UI events");
        sb.AppendLine();
        GeneratePartialMethodDeclarations(sb, "Button", uiManager, addedMethods);
        GeneratePartialMethodDeclarations(sb, "Toggle", uiManager, addedMethods);
        GeneratePartialMethodDeclarations(sb, "Slider", uiManager, addedMethods);
        GeneratePartialMethodDeclarations(sb, "InputField", uiManager, addedMethods);
        GeneratePartialMethodDeclarations(sb, "Dropdown", uiManager, addedMethods);
        sb.AppendLine("        #endregion");
        sb.AppendLine();

        #endregion

        #region UI State Management

        sb.AppendLine("        #region UI State Management");
        sb.AppendLine();
        sb.AppendLine("        // Panel management is available via the UI library:");
        sb.AppendLine($"        // {sanitizedSceneName}.UI.PanelName.Show() / Hide() / Toggle() / IsVisible");
        sb.AppendLine("        // Add custom UI state management methods in the .cs file as needed.");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();

        #endregion

        #region Cleanup Methods

        sb.AppendLine("        #region Cleanup Methods");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// A helper method to clean up UI event listeners.");
        sb.AppendLine("        /// This method is referenced in OnDisable() and Cleanup().");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private void CleanupListeners()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (uiManager == null || !areListenersSetup) return;");
        sb.AppendLine("            CleanupCustomListeners();");
        sb.AppendLine("            areListenersSetup = false;");
        sb.AppendLine("            Debug.Log($\"[{this.GetType().Name}] Listeners cleaned up\");");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Partial method for custom cleanup logic");
        sb.AppendLine("        partial void CleanupCustomListeners();");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();

        #endregion

        #region Close Class & Namespace

        sb.AppendLine("    }");
        sb.AppendLine("}");

        #endregion

        #region Write to File

        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(filePath, sb.ToString());

        #endregion
    }

    private void GenerateUserHandlerFile(string filePath, string className, string sanitizedSceneName)
    {
        // Only create if it doesn't exist
        if (File.Exists(filePath))
        {
            Debug.Log($"User file already exists, skipping: {filePath}");
            return;
        }

        var sb = new StringBuilder();

        sb.AppendLine("// ---------------------------------------------------");
        sb.AppendLine("// USER CODE FILE");
        sb.AppendLine($"// Handler for: {sanitizedSceneName}");
        sb.AppendLine($"// Created: {DateTime.Now:dd-MM-yyyy HH:mm:ss}");
        sb.AppendLine("//");
        sb.AppendLine("// This file is for your custom UI logic and event implementations.");
        sb.AppendLine("// This file will NEVER be overwritten by the code generator.");
        sb.AppendLine("// ---------------------------------------------------");
        sb.AppendLine();

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using TMPro;");
        sb.AppendLine();

        sb.AppendLine($"namespace {handlerNamespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    public partial class {className}");
        sb.AppendLine("    {");

        // ============================================
        // CUSTOM INITIALIZATION
        // ============================================
        sb.AppendLine("        // ============================================");
        sb.AppendLine("        // CUSTOM INITIALIZATION");
        sb.AppendLine("        // ============================================");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Called during handler initialization.");
        sb.AppendLine("        /// Use this to set initial UI states (show/hide panels, set text, etc.).");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        partial void InitializeUI()");
        sb.AppendLine("        {");
        sb.AppendLine("            // TODO: Set initial UI states here");
        sb.AppendLine($"            // Example: {sanitizedSceneName}.UI.MainMenu.Show();");
        sb.AppendLine("        }");
        sb.AppendLine();

        // ============================================
        // UI EVENT HANDLERS - Generate actual implementations
        // ============================================
        sb.AppendLine("        // ============================================");
        sb.AppendLine("        // UI EVENT HANDLERS");
        sb.AppendLine("        // ============================================");
        sb.AppendLine();

        GenerateUserEventHandlers(sb, "Button", uiManager);
        GenerateUserEventHandlers(sb, "Toggle", uiManager);
        GenerateUserEventHandlers(sb, "Slider", uiManager);
        GenerateUserEventHandlers(sb, "InputField", uiManager);
        GenerateUserEventHandlers(sb, "Dropdown", uiManager);

        // ============================================
        // CUSTOM CLEANUP
        // ============================================
        sb.AppendLine("        // ============================================");
        sb.AppendLine("        // CUSTOM CLEANUP");
        sb.AppendLine("        // ============================================");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Called during handler cleanup.");
        sb.AppendLine("        /// Use this to remove any persistent event listeners you added.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        partial void CleanupCustomListeners()");
        sb.AppendLine("        {");
        sb.AppendLine("            // TODO: Remove custom listeners here if needed");
        sb.AppendLine("        }");
        sb.AppendLine();

        // ============================================
        // CUSTOM METHODS
        // ============================================
        sb.AppendLine("        // ============================================");
        sb.AppendLine("        // CUSTOM METHODS");
        sb.AppendLine("        // ============================================");
        sb.AppendLine();
        sb.AppendLine("        // Add your custom helper methods here");
        sb.AppendLine();
        sb.AppendLine("    }");
        sb.AppendLine("}");

        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(filePath, sb.ToString());
        Debug.Log($"Created user file: {filePath}");
    }

    private void GenerateUserEventHandlers(StringBuilder sb, string type, UIManager manager)
    {
        var elements = manager.GetAllUICategories()
            .FirstOrDefault(c => c.name.Equals(type, StringComparison.OrdinalIgnoreCase))?.references;

        if (elements == null || !elements.Any())
            return;

        sb.AppendLine($"        #region {type} Handlers");
        sb.AppendLine();

        foreach (var element in elements)
        {
            GenerateUserEventHandler(sb, type, element);
        }

        sb.AppendLine($"        #endregion");
        sb.AppendLine();
    }

    private void GenerateUserEventHandler(StringBuilder sb, string type, UIReference element)
    {
        string methodName = GetEventHandlerMethodName(type, element.name);

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Handles {element.name} {type.ToLower()} events.");
        sb.AppendLine("        /// </summary>");

        switch (type)
        {
            case "Button":
                sb.AppendLine($"        partial void {methodName}()");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.Log($\"[{{this.GetType().Name}}] {element.name} clicked\");");
                sb.AppendLine("            // TODO: Implement click handling");
                sb.AppendLine("        }");
                break;

            case "Toggle":
                sb.AppendLine("        /// <param name=\"isOn\">New state of the toggle</param>");
                sb.AppendLine($"        partial void {methodName}(bool isOn)");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.Log($\"[{{this.GetType().Name}}] {element.name} toggled to {{isOn}}\");");
                sb.AppendLine("            // TODO: Implement toggle handling");
                sb.AppendLine("        }");
                break;

            case "Slider":
                sb.AppendLine("        /// <param name=\"value\">New value of the slider</param>");
                sb.AppendLine($"        partial void {methodName}(float value)");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.Log($\"[{{this.GetType().Name}}] {element.name} value changed to {{value}}\");");
                sb.AppendLine("            // TODO: Implement slider value handling");
                sb.AppendLine("        }");
                break;

            case "InputField":
            case "TMP_InputField":
                sb.AppendLine("        /// <param name=\"text\">New text in the input field</param>");
                sb.AppendLine($"        partial void {methodName}(string text)");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.Log($\"[{{this.GetType().Name}}] {element.name} text changed to {{text}}\");");
                sb.AppendLine("            // TODO: Implement input field text handling");
                sb.AppendLine("        }");
                break;

            case "Dropdown":
            case "TMP_Dropdown":
                sb.AppendLine("        /// <param name=\"index\">Index of the selected option</param>");
                sb.AppendLine($"        partial void {methodName}(int index)");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.Log($\"[{{this.GetType().Name}}] {element.name} selection changed to index {{index}}\");");
                sb.AppendLine("            // TODO: Implement dropdown selection handling");
                sb.AppendLine("        }");
                break;
        }
        sb.AppendLine();
    }

    #region Helper Methods for Partial Class Pattern

    private HashSet<string> ExtractEventHandlerMethodNames(string filePath)
    {
        HashSet<string> methods = new HashSet<string>();

        if (!File.Exists(filePath))
            return methods;

        try
        {
            string content = File.ReadAllText(filePath);
            // Match partial void method declarations: partial void MethodName(
            var matches = System.Text.RegularExpressions.Regex.Matches(
                content,
                @"partial\s+void\s+(\w+)\s*\("
            );

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    methods.Add(match.Groups[1].Value);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to extract method names from {filePath}: {ex.Message}");
        }

        return methods;
    }

    private void CollectEventHandlerMethods(UIManager manager, HashSet<string> methods)
    {
        string[] types = { "Button", "Toggle", "Slider", "InputField", "Dropdown" };

        foreach (var type in types)
        {
            var elements = manager.GetAllUICategories()
                .FirstOrDefault(c => c.name.Equals(type, StringComparison.OrdinalIgnoreCase))?.references;

            if (elements == null) continue;

            foreach (var element in elements)
            {
                string methodName = GetEventHandlerMethodName(type, element.name);
                methods.Add(methodName);
            }
        }
    }

    private void GenerateSetupForTypePartial(StringBuilder sb, string type, UIManager manager, string sanitizedSceneName)
    {
        var elements = manager.GetAllUICategories()
            .FirstOrDefault(c => c.name.Equals(type, StringComparison.OrdinalIgnoreCase))?.references;

        if (elements == null || !elements.Any())
        {
            GenerateEmptySetupMethod(sb, type);
            return;
        }

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Sets up all {type.ToLower()} listeners in the scene.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        private void Setup{type}s()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (uiManager == null) return;");
        sb.AppendLine();

        foreach (var element in elements)
        {
            string methodName = GetEventHandlerMethodName(type, element.name);
            string listenerSetup = GetListenerSetupLinePartial(type, element, manager, methodName);
            sb.AppendLine($"            {listenerSetup}");
        }

        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private string GetListenerSetupLinePartial(string type, UIReference element, UIManager manager, string methodName)
    {
        // Get scene name for qualification
        string sceneName = manager.gameObject.scene.name;
        string sanitizedSceneName = SanitizeIdentifier(sceneName);

        // Parse element name to get panel and element parts for dot notation
        string panelName = GetPanelFromName(element.name);
        string elementName = GetElementNameWithoutPanel(element.name, panelName);
        string sanitizedPanelName = SanitizeIdentifier(panelName);
        string sanitizedElementName = SanitizeIdentifier(elementName);

        // Generate scene-qualified dot notation access calling partial method
        return type switch
        {
            "Button" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onClick.AddListener(() => {methodName}());",
            "Toggle" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onValueChanged.AddListener((value) => {methodName}(value));",
            "Slider" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onValueChanged.AddListener((value) => {methodName}(value));",
            "InputField" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onValueChanged.AddListener((text) => {methodName}(text));",
            "TMP_InputField" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onValueChanged.AddListener((text) => {methodName}(text));",
            "Dropdown" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onValueChanged.AddListener((index) => {methodName}(index));",
            "TMP_Dropdown" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onValueChanged.AddListener((index) => {methodName}(index));",
            _ => $"// Unknown type: {type}"
        };
    }

    private void GeneratePartialMethodDeclarations(StringBuilder sb, string type, UIManager manager, HashSet<string> addedMethods)
    {
        var elements = manager.GetAllUICategories()
            .FirstOrDefault(c => c.name.Equals(type, StringComparison.OrdinalIgnoreCase))?.references;

        if (elements == null || !elements.Any())
            return;

        // Add region for this type
        sb.AppendLine($"        #region {type} Handlers");
        sb.AppendLine();

        foreach (var element in elements)
        {
            string methodName = GetEventHandlerMethodName(type, element.name);

            // Mark new methods with a comment
            if (addedMethods.Contains(methodName))
            {
                sb.AppendLine($"        // ✨ NEW - Implement this in the .cs file");
            }

            switch (type)
            {
                case "Button":
                    sb.AppendLine($"        partial void {methodName}();");
                    break;

                case "Toggle":
                    sb.AppendLine($"        partial void {methodName}(bool isOn);");
                    break;

                case "Slider":
                    sb.AppendLine($"        partial void {methodName}(float value);");
                    break;

                case "InputField":
                case "TMP_InputField":
                    sb.AppendLine($"        partial void {methodName}(string text);");
                    break;

                case "Dropdown":
                case "TMP_Dropdown":
                    sb.AppendLine($"        partial void {methodName}(int index);");
                    break;
            }
        }

        sb.AppendLine();
        sb.AppendLine($"        #endregion");
        sb.AppendLine();
    }

    #endregion

    #region Helper Methods for UI Setup & Event Handler Generation

    private void GenerateSetupForType(StringBuilder sb, string type, UIManager manager)
    {
        var elements = manager.GetAllUICategories()
            .FirstOrDefault(c => c.name.Equals(type, StringComparison.OrdinalIgnoreCase))?.references;

        if (elements == null || !elements.Any())
        {
            GenerateEmptySetupMethod(sb, type);
            return;
        }

        GenerateSetupMethod(sb, type, elements, manager);
    }

    private void GenerateEmptySetupMethod(StringBuilder sb, string type)
    {
        sb.AppendLine($"        private void Setup{type}s()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (uiManager == null) return;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private void GenerateSetupMethod(StringBuilder sb, string type, IEnumerable<UIReference> elements, UIManager manager)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Sets up all {type.ToLower()} listeners in the scene.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        private void Setup{type}s()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (uiManager == null) return;");
        sb.AppendLine();

        foreach (var element in elements)
        {
            string methodName = GetEventHandlerMethodName(type, element.name);
            string listenerSetup = GetListenerSetupLine(type, element, manager, methodName);
            sb.AppendLine($"            {listenerSetup}");
        }

        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private string GetEventHandlerMethodName(string type, string elementName)
    {
        // Sanitize element name for method naming
        string sanitizedElementName = SanitizeIdentifier(elementName);
        return type switch
        {
            "Button" => $"On{sanitizedElementName}Clicked",
            "Toggle" => $"On{sanitizedElementName}ValueChanged",
            "Slider" => $"On{sanitizedElementName}ValueChanged",
            "InputField" or "TMP_InputField" => $"On{sanitizedElementName}TextChanged",
            "Dropdown" or "TMP_Dropdown" => $"On{sanitizedElementName}SelectionChanged",
            "ScrollRect" => $"On{sanitizedElementName}ScrollChanged",
            _ => $"On{sanitizedElementName}"
        };
    }

    private string GetListenerSetupLine(string type, UIReference element, UIManager manager, string methodName)
    {
        // Get scene name for qualification
        string sceneName = manager.gameObject.scene.name;
        string sanitizedSceneName = SanitizeIdentifier(sceneName);

        // Parse element name to get panel and element parts for dot notation
        string panelName = GetPanelFromName(element.name);
        string elementName = GetElementNameWithoutPanel(element.name, panelName);
        string sanitizedPanelName = SanitizeIdentifier(panelName);
        string sanitizedElementName = SanitizeIdentifier(elementName);

        // Generate scene-qualified dot notation access: SceneName.UI.PanelName.ElementName.event.AddListener(method)
        return type switch
        {
            "Button" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onClick.AddListener({methodName});",
            "Toggle" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onValueChanged.AddListener({methodName});",
            "Slider" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onValueChanged.AddListener({methodName});",
            "InputField" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onValueChanged.AddListener({methodName});",
            "TMP_InputField" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onValueChanged.AddListener({methodName});",
            "Dropdown" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onValueChanged.AddListener({methodName});",
            "TMP_Dropdown" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onValueChanged.AddListener({methodName});",
            "ScrollRect" => $"{sanitizedSceneName}.UI.{sanitizedPanelName}.{sanitizedElementName}.onValueChanged.AddListener({methodName});",
            _ => $"// Unknown type: {type}"
        };
    }

    private void GenerateHandlersForType(StringBuilder sb, string type, UIManager manager)
    {
        var elements = manager.GetAllUICategories()
            .FirstOrDefault(c => c.name.Equals(type, StringComparison.OrdinalIgnoreCase))?.references;

        if (elements == null || !elements.Any())
        {
            return;
        }

        // Add region header for this element type
        sb.AppendLine($"        #region {type} Handlers");
        sb.AppendLine();

        foreach (var element in elements)
        {
            GenerateEventHandler(sb, type, element, manager);
        }

        // Close region
        sb.AppendLine($"        #endregion");
        sb.AppendLine();
    }

    private void GenerateEventHandler(StringBuilder sb, string type, UIReference element, UIManager manager)
    {
        string methodName = GetEventHandlerMethodName(type, element.name);

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Handles {element.name} {type.ToLower()} events.");
        sb.AppendLine("        /// </summary>");

        switch (type)
        {
            case "Button":
                sb.AppendLine($"        private void {methodName}()");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.Log($\"[{{this.GetType().Name}}] {element.name} clicked\");");
                sb.AppendLine("            // TODO: Implement click handling");
                sb.AppendLine("        }");
                break;

            case "Toggle":
                sb.AppendLine("        /// <param name=\"isOn\">New state of the toggle</param>");
                sb.AppendLine($"        private void {methodName}(bool isOn)");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.Log($\"[{{this.GetType().Name}}] {element.name} toggled to {{isOn}}\");");
                sb.AppendLine("            // TODO: Implement toggle handling");
                sb.AppendLine("        }");
                break;

            case "Slider":
                sb.AppendLine("        /// <param name=\"value\">New value of the slider</param>");
                sb.AppendLine($"        private void {methodName}(float value)");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.Log($\"[{{this.GetType().Name}}] {element.name} value changed to {{value}}\");");
                sb.AppendLine("            // TODO: Implement slider value handling");
                sb.AppendLine("        }");
                break;

            case "InputField":
            case "TMP_InputField":
                sb.AppendLine("        /// <param name=\"text\">New text in the input field</param>");
                sb.AppendLine($"        private void {methodName}(string text)");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.Log($\"[{{this.GetType().Name}}] {element.name} text changed to {{text}}\");");
                sb.AppendLine("            // TODO: Implement input field text handling");
                sb.AppendLine("        }");
                break;

            case "Dropdown":
            case "TMP_Dropdown":
                sb.AppendLine("        /// <param name=\"index\">Index of the selected option</param>");
                sb.AppendLine($"        private void {methodName}(int index)");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.Log($\"[{{this.GetType().Name}}] {element.name} selection changed to index {{index}}\");");
                sb.AppendLine("            // TODO: Implement dropdown selection handling");
                sb.AppendLine("        }");
                break;

            case "ScrollRect":
                sb.AppendLine("        /// <param name=\"position\">Normalized scroll position (x,y)</param>");
                sb.AppendLine($"        private void {methodName}(Vector2 position)");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.Log($\"[{{this.GetType().Name}}] {element.name} scroll position changed to {{position}}\");");
                sb.AppendLine("            // TODO: Implement scroll handling");
                sb.AppendLine("        }");
                break;
        }
        sb.AppendLine();
    }
    #endregion

    #endregion
}
#endif