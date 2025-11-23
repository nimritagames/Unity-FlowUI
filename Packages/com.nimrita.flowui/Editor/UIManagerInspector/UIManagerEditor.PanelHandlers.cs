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
    #region Panel Handler Settings

    // Initialize panel handler settings
    private void InitializePanelHandlerSettings()
    {
        // Load panel handler generation settings from EditorPrefs
        panelHandlerOutputPath = EditorPrefs.GetString(PANEL_HANDLER_OUTPUT_PATH_KEY, handlerOutputPath + "Panels/");
        panelHandlerNamespace = EditorPrefs.GetString(PANEL_HANDLER_NAMESPACE_KEY, handlerNamespace + ".Panels");
        panelHandlerClassPrefix = EditorPrefs.GetString(PANEL_HANDLER_CLASS_PREFIX_KEY, "");

        // Get panels for quick reference
        var panelCategory = uiManager.GetAllUICategories().FirstOrDefault(c => c.name == "Panel");
        if (panelCategory != null)
        {
            panels = panelCategory.references;
        }
        else
        {
            panels = new List<UIReference>();
        }
    }

    private void DrawPanelHandlerSettings()
    {
        DrawSectionHeader("Panel Handlers",
            "Generate handlers for individual UI panels.\nEach panel handler manages a specific panel's UI elements and interactions.");

        EditorGUILayout.Space(10);

        // Check if library exists with warning
        bool libraryExists = IsLibraryGenerated(uiManager.gameObject.scene.name);
        if (!libraryExists)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Rect warningRect = EditorGUILayout.GetControlRect(false, 28);

            if (Event.current.type == EventType.Repaint)
            {
                Color warningColor = new Color(0.8f, 0.6f, 0.2f, 0.1f);
                EditorGUI.DrawRect(warningRect, warningColor);
            }

            GUIStyle warningStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.9f, 0.6f, 0.1f) },
                padding = new RectOffset(10, 10, 6, 6)
            };

            Texture2D warningIcon = EditorGUIUtility.IconContent("console.warnicon.sml").image as Texture2D;

            if (warningIcon != null)
            {
                GUI.DrawTexture(
                    new Rect(warningRect.x + 8, warningRect.y + 6, 16, 16),
                    warningIcon
                );

                EditorGUI.LabelField(
                    new Rect(warningRect.x + 30, warningRect.y, warningRect.width - 40, warningRect.height),
                    "Library must be generated first. Please go to the Library Generation tab.",
                    warningStyle
                );
            }
            else
            {
                EditorGUI.LabelField(
                    warningRect,
                    "⚠️ Library must be generated first. Please go to the Library Generation tab.",
                    warningStyle
                );
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        // Get panels
        var panelCategory = uiManager.GetAllUICategories().FirstOrDefault(c => c.name == "Panel");
        var panels = panelCategory?.references ?? new List<UIReference>();

        // Panel count indicator with enhanced styling
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        Rect countRect = EditorGUILayout.GetControlRect(false, 36);

        if (Event.current.type == EventType.Repaint)
        {
            // Background with subtle gradient
            Color bgColor = panels.Any() ?
                new Color(0.2f, 0.4f, 0.2f, 0.2f) : // Green tint for panels found
                new Color(0.4f, 0.4f, 0.5f, 0.2f);  // Neutral for no panels

            EditorGUI.DrawRect(countRect, bgColor);
        }

        // Panel icon
        Texture2D panelIcon = GetIcon("panel_icon");
        if (panelIcon != null)
        {
            GUI.DrawTexture(
                new Rect(countRect.x + 12, countRect.y + 9, 18, 18),
                panelIcon
            );
        }

        // Panel count text
        GUIStyle countStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = panels.Any() ? new Color(0.3f, 0.7f, 0.3f) : new Color(0.7f, 0.7f, 0.8f) }
        };

        string countText = panels.Any() ?
            $"Found {panels.Count} UI panels in the scene" :
            "No UI panels found in the scene";

        EditorGUI.LabelField(
            new Rect(countRect.x + 40, countRect.y + 9, countRect.width - 50, 18),
            countText,
            countStyle
        );

        EditorGUILayout.EndVertical();

        // Instructions when no panels
        if (!panels.Any())
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUIStyle infoStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.7f, 0.85f, 1f) },
                padding = new RectOffset(20, 20, 10, 10)
            };

            // Info text
            EditorGUILayout.LabelField(
                "Add panels to the UI Manager first using the UI Hierarchy tab. Panels should have names ending with '_Panel'.",
                infoStyle
            );

            // Add button to navigate to hierarchy tab
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Go to UI Hierarchy Tab", GUILayout.Width(180), GUILayout.Height(24)))
            {
                selectedTab = 0; // Set to the Hierarchy tab index
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }
        else // Display panel list if panels exist
        {
            EditorGUILayout.Space(15);

            // Generate All button with enhanced styling
            Rect genAllRect = EditorGUILayout.GetControlRect(false, 36);

            EditorGUI.BeginDisabledGroup(!libraryExists);

            DrawActionButton(
                genAllRect,
                "Generate All Panel Handlers",
                "Create handlers for all panels in the scene",
                () =>
                {
                    if (EditorUtility.DisplayDialog("Generate All Panel Handlers",
                        $"This will generate handler scripts for all {panels.Count} panels. Continue?",
                        "Yes", "No"))
                    {
                        foreach (var panel in panels)
                        {
                            GeneratePanelHandlerTemplate(panel);
                        }
                    }
                },
                true
            );

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(15);

            // Panel list header
            DrawSectionHeader("Panels List", null);
            EditorGUILayout.Space(5);

            // Enhanced panel list
            float listHeight = Mathf.Min(panels.Count * 90, 300); // Height based on panel count
            panelScrollPosition = EditorGUILayout.BeginScrollView(panelScrollPosition, GUILayout.Height(listHeight));

            for (int i = 0; i < panels.Count; i++)
            {
                DrawEnhancedPanelHandlerRow(panels[i], i);
            }

            EditorGUILayout.EndScrollView();
        }

        // Panel Handler Settings
        EditorGUILayout.Space(15);

        DrawSectionHeader("Handler Settings", null);
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
            new GUIContent("Output Path", "The folder where the generated panel handler files will be stored"),
            labelStyle,
            GUILayout.Width(100)
        );

        // Custom field style
        GUIStyle pathStyle = new GUIStyle(EditorStyles.textField)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
        };

        string newPath = EditorGUILayout.TextField(panelHandlerOutputPath, pathStyle);
        if (newPath != panelHandlerOutputPath)
        {
            panelHandlerOutputPath = newPath;
            EditorPrefs.SetString(PANEL_HANDLER_OUTPUT_PATH_KEY, panelHandlerOutputPath);
        }

        GUIStyle browseStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fixedHeight = 18,
            fontSize = 10
        };

        if (GUILayout.Button("Browse...", browseStyle, GUILayout.Width(70)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Panel Handler Output Path", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                panelHandlerOutputPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                EditorPrefs.SetString(PANEL_HANDLER_OUTPUT_PATH_KEY, panelHandlerOutputPath);
            }
        }

        EditorGUILayout.EndHorizontal();

        // Validate path
        if (string.IsNullOrEmpty(panelHandlerOutputPath) || !panelHandlerOutputPath.StartsWith("Assets"))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox("Warning: The output path must be inside the 'Assets' folder.", MessageType.Warning);
        }

        EditorGUILayout.Space(8);

        // Namespace with styling
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(
            new GUIContent("Namespace", "The namespace for the generated Panel Handlers"),
            labelStyle,
            GUILayout.Width(100)
        );

        string newNamespace = EditorGUILayout.TextField(panelHandlerNamespace, pathStyle);
        if (newNamespace != panelHandlerNamespace)
        {
            panelHandlerNamespace = newNamespace;
            EditorPrefs.SetString(PANEL_HANDLER_NAMESPACE_KEY, panelHandlerNamespace);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Class prefix with styling
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(
            new GUIContent("Class Prefix", "A prefix to use for the generated panel handler classes (optional)"),
            labelStyle,
            GUILayout.Width(100)
        );

        string newPrefix = EditorGUILayout.TextField(panelHandlerClassPrefix, pathStyle);
        if (newPrefix != panelHandlerClassPrefix)
        {
            panelHandlerClassPrefix = newPrefix;
            EditorPrefs.SetString(PANEL_HANDLER_CLASS_PREFIX_KEY, panelHandlerClassPrefix);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        // Panel Binder Info with improved styling
        EditorGUILayout.Space(15);

        DrawSectionHeader("Panel Binder Component", null);
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Info box with styling
        GUIStyle binderInfoStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 11,
            wordWrap = true,
            richText = true,
            normal = { textColor = new Color(0.8f, 0.8f, 0.85f) },
            padding = new RectOffset(10, 10, 10, 10)
        };

        string binderInfoText = "<b>UIPanelBinder</b> automatically connects your panel handler to the correct panel and UI Manager. " +
            "Add this component to the same GameObject as your panel handler component for automatic setup.";

        EditorGUILayout.LabelField(binderInfoText, binderInfoStyle);

        // Example code
        EditorGUILayout.Space(10);

        GUIStyle codeHeaderStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.7f, 0.85f, 1f) },
            margin = new RectOffset(10, 0, 0, 0)
        };

        EditorGUILayout.LabelField("Quick Setup Guide:", codeHeaderStyle);

        GUIStyle codeStyle = new GUIStyle(EditorStyles.textArea)
        {
            fontSize = 11,
            wordWrap = true,
            richText = true,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        string exampleCode =
            "<color=#569CD6>// 1. Create an empty GameObject in your scene</color>\n" +
            "<color=#569CD6>// 2. Add your panel handler component</color>\n" +
            "<color=#569CD6>// 3. Add the UIPanelBinder component</color>\n" +
            "<color=#569CD6>// 4. Click 'Auto-Setup Panel Binding' in the inspector</color>\n\n" +
            "<color=#6A9955>// The binder will automatically:</color>\n" +
            "<color=#6A9955>// - Find the UIManager in your scene</color>\n" +
            "<color=#6A9955>// - Connect the panel reference</color>\n" +
            "<color=#6A9955>// - Set up all required connections</color>";

        // Disable editing in the Inspector
        EditorGUILayout.SelectableLabel(exampleCode, codeStyle, GUILayout.Height(140));

        EditorGUILayout.EndVertical();
    }

    private void DrawEnhancedPanelHandlerRow(UIReference panel, int index)
    {
        if (panel == null || panel.uiElement == null) return;

        bool handlerExists = PanelHandlerFileExists(panel);

        // Panel box with subtle gradient
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        Rect headerRect = EditorGUILayout.GetControlRect(false, 32);

        if (Event.current.type == EventType.Repaint)
        {
            // Background color based on status
            Color bgColor = handlerExists ?
                new Color(0.2f, 0.4f, 0.2f, 0.2f) : // Green tint for generated
                new Color(0.25f, 0.25f, 0.28f, 0.3f); // Neutral for not generated

            EditorGUI.DrawRect(headerRect, bgColor);

            // Bottom border
            EditorGUI.DrawRect(
                new Rect(headerRect.x, headerRect.y + headerRect.height - 1, headerRect.width, 1),
                new Color(0.3f, 0.3f, 0.35f, 0.5f)
            );
        }

        // Panel icon
        Texture2D panelIcon = EditorGUIUtility.IconContent("d_Canvas Icon").image as Texture2D;
        if (panelIcon != null)
        {
            GUI.DrawTexture(
                new Rect(headerRect.x + 8, headerRect.y + 8, 16, 16),
                panelIcon
            );
        }

        // Panel name
        GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
        };

        EditorGUI.LabelField(
            new Rect(headerRect.x + 30, headerRect.y + 8, 200, 20),
            panel.name,
            nameStyle
        );

        // Status indicator
        string statusText = handlerExists ? "✓ Generated" : "Not Generated";
        Color statusColor = handlerExists ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);

        GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = statusColor }
        };

        EditorGUI.LabelField(
            new Rect(headerRect.x + headerRect.width - 270, headerRect.y + 8, 80, 16),
            statusText,
            statusStyle
        );

        // Generate button
        float buttonX = headerRect.x + headerRect.width - 180;

        EditorGUI.BeginDisabledGroup(!IsLibraryGenerated(uiManager.gameObject.scene.name));

        Color defaultColor = GUI.backgroundColor;
        if (!handlerExists)
        {
            GUI.backgroundColor = new Color(0.2f, 0.6f, 1.0f); // Blue for primary action
        }

        string buttonText = handlerExists ? "Regenerate" : "Generate";
        if (GUI.Button(new Rect(buttonX, headerRect.y + 6, 90, 20), new GUIContent(buttonText)))
        {
            GeneratePanelHandlerTemplate(panel);
        }

        GUI.backgroundColor = defaultColor;

        EditorGUI.EndDisabledGroup();

        // View code button
        if (handlerExists)
        {
            if (GUI.Button(new Rect(buttonX + 95, headerRect.y + 6, 75, 20), new GUIContent("View Code")))
            {
                PingPanelHandler(panel);
            }
        }

        // Select button
        if (GUI.Button(new Rect(buttonX - 65, headerRect.y + 6, 60, 20), new GUIContent("Select")))
        {
            Selection.activeGameObject = panel.uiElement;
        }

        EditorGUILayout.EndVertical();

        // Add spacing between panels
        EditorGUILayout.Space(4);
    }

    /// <summary>
    /// Checks if a panel handler file exists for the given panel.
    /// </summary>
    private bool PanelHandlerFileExists(UIReference panel)
    {
        if (panel == null) return false;

        string sanitizedPanelName = SanitizeIdentifier(panel.name);
        string className = $"{panelHandlerClassPrefix}{sanitizedPanelName}PanelHandler";
        string handlerFilePath = Path.Combine(panelHandlerOutputPath, $"{className}.cs");

        return File.Exists(handlerFilePath);
    }

    /// <summary>
    /// Gets a list of UI elements that belong to a panel.
    /// </summary>
    private List<UIReference> GetUIElementsInPanel(UIReference panelReference)
    {
        if (panelReference == null || panelReference.uiElement == null)
            return new List<UIReference>();

        // Get the panel's path
        string panelPath = panelReference.fullPath;

        // Find all references whose paths start with the panel's path
        return uiManager.GetAllUICategories()
            .SelectMany(category => category.references)
            .Where(reference => reference.fullPath.StartsWith(panelPath + "/"))
            .ToList();
    }

    /// <summary>
    /// Generates a panel handler template for the given panel.
    /// </summary>
    private void GeneratePanelHandlerTemplate(UIReference panelReference)
    {
        // Check for naming issues before generating
        if (!SmartNamingController.ValidateNamingForCodeGeneration(uiManager, "Panel Handler Generation"))
        {
            return; // User chose to fix naming first or cancelled
        }

        if (panelReference == null || panelReference.uiElement == null)
        {
            Debug.LogError("Cannot generate handler for null panel reference");
            return;
        }

        string sceneName = uiManager.gameObject.scene.name;
        string sanitizedSceneName = SanitizeIdentifier(sceneName);
        string sanitizedPanelName = SanitizeIdentifier(panelReference.name);
        string className = $"{panelHandlerClassPrefix}{sanitizedPanelName}PanelHandler";
        string handlerFilePath = Path.Combine(panelHandlerOutputPath, $"{className}.cs");

        // Get panel class name for dot notation (e.g., "Main_Menu_Panel" -> "MainMenu")
        string panelClassName = GetPanelFromName(panelReference.name);

        // Check that the library has been generated first
        if (!IsLibraryGenerated(sceneName))
        {
            EditorUtility.DisplayDialog("Library Not Found",
                "The UI Library has not been generated yet.\n\nPlease generate the library first before generating the panel handler.",
                "OK");
            return;
        }

        // Check if the handler file already exists
        if (File.Exists(handlerFilePath))
        {
            string message = $"A panel handler file already exists at:\n{handlerFilePath}\n\n" +
                             "Overwriting this file may cause you to lose custom modifications. " +
                             "Do you want to create a backup and overwrite, overwrite without backup, or cancel?";
            int option = EditorUtility.DisplayDialogComplex(
                "Panel Handler File Exists",
                message,
                "Backup & Overwrite", // Option 0
                "Overwrite",          // Option 1
                "Cancel"              // Option 2
            );

            if (option == 2) // Cancel
            {
                Debug.Log("Panel handler template generation cancelled by user.");
                return;
            }
            else if (option == 0) // Backup & Overwrite
            {
                string backupPath = handlerFilePath.Replace(".cs", $"_{DateTime.Now:yyyyMMddHHmmss}.bak.cs");
                try
                {
                    File.Copy(handlerFilePath, backupPath);
                    Debug.LogWarning($"Panel handler backup created at: {backupPath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create panel handler backup: {ex.Message}");
                    return;
                }
            }
            // Option 1: Overwrite without backup.
        }

        // Generate template content
        var sb = new StringBuilder();

        #region File Header & Using Statements
        sb.AppendLine("// ---------------------------------------------------");
        sb.AppendLine("// Generated by UIFramework");
        sb.AppendLine($"// Scene: {sanitizedSceneName}");
        sb.AppendLine($"// Panel: {sanitizedPanelName}");
        sb.AppendLine($"// Date: {DateTime.Now}");
        sb.AppendLine("// This file is auto-generated. You can modify the generated code,");
        sb.AppendLine("// but it might be overwritten if you regenerate the handler.");
        sb.AppendLine("// ---------------------------------------------------");
        sb.AppendLine();

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using UnityEngine.Events;");
        sb.AppendLine("using TMPro;");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections;");
        sb.AppendLine($"using {libraryNamespace}.{sanitizedSceneName};");
        sb.AppendLine($"using {handlerNamespace};");
        sb.AppendLine();
        #endregion

        #region Namespace & Class Declaration
        sb.AppendLine($"namespace {panelHandlerNamespace}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Handles UI interactions for the {panelReference.name} panel.");
        sb.AppendLine("    /// This class manages all UI element references, events, and state management");
        sb.AppendLine($"    /// for this specific panel within the {sanitizedSceneName} scene.");
        sb.AppendLine("    /// </summary>");

        // Base class refers to the scene-level handler class
        sb.AppendLine($"    public class {className} : MonoBehaviour");
        sb.AppendLine("    {");
        #endregion

        #region Panel-Specific Fields
        sb.AppendLine("        #region Fields");
        sb.AppendLine();
        sb.AppendLine("        [Header(\"Dependencies\")]");
        sb.AppendLine("        [SerializeField, Tooltip(\"Reference to the UI Manager in the scene\")]");
        sb.AppendLine("        private UIManager uiManager;");
        sb.AppendLine();
        sb.AppendLine("        [SerializeField, Tooltip(\"Reference to the panel GameObject\")]");
        sb.AppendLine("        private GameObject panelObject;");
        sb.AppendLine();
        sb.AppendLine("        // Internal state tracking");
        sb.AppendLine("        private bool isInitialized;");
        sb.AppendLine("        private bool areListenersSetup;");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();
        #endregion

        #region Properties
        sb.AppendLine("        #region Properties");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Gets whether the panel handler is properly initialized.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public bool IsInitialized => isInitialized;");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Gets whether the panel is currently active (visible).");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public bool IsPanelActive => panelObject != null && panelObject.activeSelf;");
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
        sb.AppendLine();
        sb.AppendLine("            // Auto-assign panel reference if not set");
        sb.AppendLine("            if (panelObject == null)");
        sb.AppendLine("            {");
        sb.AppendLine($"                panelObject = uiManager.GetPanel(\"{panelReference.fullPath}\");");
        sb.AppendLine("                if (panelObject == null)");
        sb.AppendLine("                {");
        sb.AppendLine($"                    Debug.LogError($\"[{className}] Failed to find panel '{panelReference.name}'. Please verify the panel exists in UIManager.\", this);");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Initializes the panel handler.");
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Initializes the panel handler.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private void Initialize()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (isInitialized) return;");
        sb.AppendLine("            if (uiManager == null || panelObject == null) return;");
        sb.AppendLine();
        sb.AppendLine("            // Initialize the UI library with the UIManager");
        sb.AppendLine($"            {sanitizedSceneName}.UI.Initialize(uiManager);");
        sb.AppendLine();
        sb.AppendLine("            InitializeUI();");
        sb.AppendLine("            SetupListeners();");
        sb.AppendLine();
        sb.AppendLine("            isInitialized = true;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Initializes panel UI elements and sets their default states.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private void InitializeUI()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (uiManager == null || panelObject == null) return;");
        sb.AppendLine();
        sb.AppendLine("            // TODO: Set initial UI states here");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Cleans up the panel handler.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private void Cleanup()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!isInitialized) return;");
        sb.AppendLine();
        sb.AppendLine("            CleanupListeners();");
        sb.AppendLine("            isInitialized = false;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();
        #endregion

        #region Panel Specific UI Elements
        // Find UI elements that belong to this panel
        var panelElements = GetUIElementsInPanel(panelReference);

        if (panelElements.Any())
        {
            #region UI Setup Methods
            sb.AppendLine("        #region UI Setup Methods");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Sets up all UI event listeners for this panel.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        private void SetupListeners()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (uiManager == null || panelObject == null || areListenersSetup) return;");
            sb.AppendLine();

            // Only include setup for types we actually have
            var elementTypes = panelElements
                .Select(e => e.elementType.ToString())
                .Distinct()
                .Where(t => t != "Panel") // Skip panel type itself
                .ToList();

            foreach (var type in elementTypes)
            {
                sb.AppendLine($"            Setup{type}s();");
            }

            sb.AppendLine();
            sb.AppendLine("            areListenersSetup = true;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate setup methods for each type
            foreach (var type in elementTypes)
            {
                var elements = panelElements.Where(e => e.elementType.ToString() == type).ToList();
                GenerateSetupMethod(sb, type, elements, uiManager);
            }

            sb.AppendLine("        #endregion");
            sb.AppendLine();

            #endregion

            #region UI Event Handlers
            sb.AppendLine("        #region UI Event Handlers");
            sb.AppendLine();

            foreach (var type in elementTypes)
            {
                var elements = panelElements.Where(e => e.elementType.ToString() == type).ToList();
                foreach (var element in elements)
                {
                    GenerateEventHandler(sb, type, element, uiManager);
                }
            }

            sb.AppendLine("        #endregion");
            sb.AppendLine();
            #endregion
        }
        else
        {
            sb.AppendLine("        // No UI elements found for this panel");
            sb.AppendLine("        // Make sure the panel contains UI elements and they are added to the UIManager");
            sb.AppendLine();

            // Still add empty setup method
            sb.AppendLine("        #region UI Setup Methods");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Sets up all UI event listeners for this panel.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        private void SetupListeners()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (uiManager == null || panelObject == null || areListenersSetup) return;");
            sb.AppendLine("            areListenersSetup = true;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        #endregion");
            sb.AppendLine();
        }
        #endregion

        #region Panel Management
        sb.AppendLine("        #region Panel Management");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Shows this panel and optionally hides other panels.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"hideOthers\">Whether to hide other panels</param>");
        sb.AppendLine("        public void ShowPanel(bool hideOthers = true)");
        sb.AppendLine("        {");
        sb.AppendLine($"            {sanitizedSceneName}.UI.{panelClassName}.Show(hideOthers);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Hides this panel.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public void HidePanel()");
        sb.AppendLine("        {");
        sb.AppendLine($"            {sanitizedSceneName}.UI.{panelClassName}.Hide();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Toggles this panel's visibility.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public void TogglePanel()");
        sb.AppendLine("        {");
        sb.AppendLine($"            {sanitizedSceneName}.UI.{panelClassName}.Toggle();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();
        #endregion

        #region Cleanup Methods
        sb.AppendLine("        #region Cleanup Methods");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Cleans up UI event listeners.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private void CleanupListeners()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!areListenersSetup) return;");
        sb.AppendLine();
        sb.AppendLine("            // TODO: Remove any persistent event listeners here if needed");
        sb.AppendLine();
        sb.AppendLine("            areListenersSetup = false;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();
        #endregion

        #region Close Class & Namespace
        sb.AppendLine("    }");
        sb.AppendLine("}");
        #endregion

        #region Write File
        // Ensure directory exists
        string directory = Path.GetDirectoryName(handlerFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write file
        File.WriteAllText(handlerFilePath, sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log($"Generated Panel Handler for '{panelReference.name}' at: {handlerFilePath}");

        ShowQuickTip("Panel Handler Generated",
            $"Panel handler for '{panelReference.name}' has been generated. You can now open it in your code editor.");
        #endregion
    }

    /// <summary>
    /// Pings the generated panel handler file in the Project window.
    /// </summary>
    private void PingPanelHandler(UIReference panelReference)
    {
        if (panelReference == null) return;

        string sanitizedPanelName = SanitizeIdentifier(panelReference.name);
        string className = $"{panelHandlerClassPrefix}{sanitizedPanelName}PanelHandler";
        string handlerFilePath = Path.Combine(panelHandlerOutputPath, $"{className}.cs");

        if (File.Exists(handlerFilePath))
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(handlerFilePath);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
                Debug.Log($"Pinged Panel Handler file: {handlerFilePath}");
            }
        }
        else
        {
            Debug.LogWarning($"Panel Handler file for '{panelReference.name}' not found. Generate the handler first.");
            EditorUtility.DisplayDialog("Panel Handler Not Found",
                $"The Panel Handler file for '{panelReference.name}' does not exist. Generate the Panel Handler first.",
                "OK");
        }
    }

    #endregion
}
#endif