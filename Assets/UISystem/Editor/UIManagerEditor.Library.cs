#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public partial class UIManagerEditor : Editor
{
    #region Library Generation

    private void DrawLibrarySettings()
    {
        // Calculate the scene name and its sanitized version once
        string sceneName = uiManager.gameObject.scene.name;
        string sanitizedSceneName = SanitizeIdentifier(sceneName);

        DrawSectionHeader("Library Generation", "Generate type-safe code that provides easy access to your UI elements.\nThe library creates constants for element paths and IDs.");


        EditorGUILayout.Space(10);

        // Library status panel with enhanced styling
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Status indicator with icon
        Rect statusRect = EditorGUILayout.GetControlRect(false, 36);
        bool libraryExists = IsLibraryGenerated(sceneName);

        if (Event.current.type == EventType.Repaint)
        {
            // Background with subtle gradient
            Color bgColor = libraryExists ?
                new Color(0.2f, 0.4f, 0.2f, 0.2f) : // Green tint for success
                new Color(0.4f, 0.4f, 0.5f, 0.2f);  // Neutral for not generated

            EditorGUI.DrawRect(statusRect, bgColor);
        }

        // Status icon
        Texture2D statusIcon;
        if (libraryExists)
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
            normal = { textColor = libraryExists ? new Color(0.3f, 0.7f, 0.3f) : new Color(0.7f, 0.7f, 0.8f) }
        };

        string statusText = libraryExists ?
            "Library Status: ✓ Generated" :
            "Library Status: Not Generated";

        EditorGUI.LabelField(
            new Rect(statusRect.x + 40, statusRect.y + 9, statusRect.width - 50, 18),
            statusText,
            statusStyle
        );

        // Last updated date if available
        if (libraryExists)
        {
            GUIStyle dateStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };

            // Get file date info
            string filePath = Path.Combine(libraryOutputPath, $"{libraryClassPrefix}{sanitizedSceneName}.cs");
            DateTime lastModified = File.GetLastWriteTime(filePath);

            string dateText = $"    Last updated: {lastModified.ToString(" MMM d, yyyy 'at' h:mm tt")}";

            EditorGUI.LabelField(
                new Rect(statusRect.x + 200, statusRect.y + 12, statusRect.width - 210, 14),
                dateText,
                dateStyle
            );
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

        string generateButtonText = libraryExists ? "Regenerate Library" : "Generate Library";
        DrawActionButton(
            generateRect,
            generateButtonText,
            "Create or update the UI Library file",
            () => ValidateAndUpdateUILibrary(),
            true
        );

        GUILayout.Space(10);

        // View code button (only if library exists)
        if (libraryExists)
        {
            Rect viewRect = GUILayoutUtility.GetRect(buttonWidth - 60, buttonHeight);

            DrawActionButton(
                viewRect,
                "View Code",
                "Open the generated library file",
                () => PingUILibrary()
            );

            GUILayout.Space(10);
        }

        // Preview code button
        Rect previewRect = GUILayoutUtility.GetRect(buttonWidth - 60, buttonHeight);

        DrawActionButton(
            previewRect,
            "Preview Code",
            "Show a preview of the generated code",
            () => ShowLibraryCodePreview()
        );

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
            new GUIContent("Output Path", "The folder where the generated library files will be stored"),
            labelStyle,
            GUILayout.Width(100)
        );

        // Custom field style
        GUIStyle pathStyle = new GUIStyle(EditorStyles.textField)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
        };

        string newPath = EditorGUILayout.TextField(libraryOutputPath, pathStyle);
        if (newPath != libraryOutputPath)
        {
            libraryOutputPath = newPath;
            EditorPrefs.SetString(LIBRARY_OUTPUT_PATH_KEY, libraryOutputPath);
        }

        GUIStyle browseStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fixedHeight = 18,
            fontSize = 10
        };

        if (GUILayout.Button("Browse...", browseStyle, GUILayout.Width(70)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Library Output Path", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                libraryOutputPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                EditorPrefs.SetString(LIBRARY_OUTPUT_PATH_KEY, libraryOutputPath);
            }
        }

        EditorGUILayout.EndHorizontal();

        // Validate path
        if (string.IsNullOrEmpty(libraryOutputPath) || !libraryOutputPath.StartsWith("Assets"))
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox("Warning: The output path must be inside the 'Assets' folder.", MessageType.Warning);
        }

        EditorGUILayout.Space(8);

        // Namespace with styling
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(
            new GUIContent("Namespace", "The namespace for the generated UI Library"),
            labelStyle,
            GUILayout.Width(100)
        );

        string newNamespace = EditorGUILayout.TextField(libraryNamespace, pathStyle);
        if (newNamespace != libraryNamespace)
        {
            libraryNamespace = newNamespace;
            EditorPrefs.SetString(LIBRARY_NAMESPACE_KEY, libraryNamespace);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Class prefix with styling
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(
            new GUIContent("Class Prefix", "A prefix to use for the generated library classes"),
            labelStyle,
            GUILayout.Width(100)
        );

        string newPrefix = EditorGUILayout.TextField(libraryClassPrefix, pathStyle);
        if (newPrefix != libraryClassPrefix)
        {
            libraryClassPrefix = newPrefix;
            EditorPrefs.SetString(LIBRARY_CLASS_PREFIX_KEY, libraryClassPrefix);
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
        EditorGUILayout.LabelField("File:", GUILayout.Width(50));
        EditorGUILayout.SelectableLabel($"{libraryOutputPath}/{libraryClassPrefix}{sanitizedSceneName}.cs", previewPathStyle, GUILayout.Height(20));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);

        // Usage preview
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Usage:", GUILayout.Width(50));
        EditorGUILayout.SelectableLabel($"using {libraryNamespace}.{sanitizedSceneName};", previewPathStyle, GUILayout.Height(20));
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
            "<color=#569CD6>// Get a Button component:</color>\n" +
            $"<color=#4EC9B0>Button</color> playButton = uiManager.GetUIComponent<<color=#4EC9B0>Button</color>>({libraryClassPrefix}{sanitizedSceneName}.<color=#DCDCAA>PlayButton_Path</color>);\n\n" +
            "<color=#569CD6>// Add a click listener:</color>\n" +
            $"uiManager.SetUIComponentListener<<color=#4EC9B0>Button</color>>({libraryClassPrefix}{sanitizedSceneName}.<color=#DCDCAA>PlayButton_Path</color>, <color=#DCDCAA>OnPlayClicked</color>);";

        // Disable editing in the Inspector
        EditorGUILayout.SelectableLabel(example, codeStyle, GUILayout.Height(95));

        EditorGUILayout.Space(5);

        GUIStyle noteStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 9,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };

        EditorGUILayout.LabelField(
            "Note: The UI library provides convenient access to your UI elements through path constants, " +
            "making your code more maintainable and less prone to errors from hardcoded paths.",
            noteStyle
        );

        EditorGUILayout.EndVertical();


        EditorGUILayout.Space(5);

        // Element counts
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 10,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };

        EditorGUILayout.LabelField(
            $"UI Elements: {addedUIElements.Count} | Categories: {uiManager.GetAllUICategories().Count()}",
            countStyle
        );

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private bool ValidateLibrarySettings()
    {
        if (string.IsNullOrEmpty(libraryOutputPath))
        {
            EditorUtility.DisplayDialog("Invalid Settings", "Library output path cannot be empty.", "OK");
            return false;
        }

        if (string.IsNullOrEmpty(libraryNamespace))
        {
            EditorUtility.DisplayDialog("Invalid Settings", "Library namespace cannot be empty.", "OK");
            return false;
        }

        if (string.IsNullOrEmpty(libraryClassPrefix))
        {
            EditorUtility.DisplayDialog("Invalid Settings", "Library class prefix cannot be empty.", "OK");
            return false;
        }

        return true;
    }

    private void ValidateAndUpdateUILibrary()
    {
        if (isUpdating) return;

        // Let the actual validation handle naming conflicts and trigger Smart Naming Assistant

        if (!ValidateLibrarySettings())
        {
            return;
        }

        isUpdating = true;
        EditorUtility.DisplayProgressBar("Updating UI Library", "Validating UI elements...", 0.3f);

        try
        {
            // Check for minimum UI elements
            if (addedUIElements.Count == 0)
            {
                EditorUtility.DisplayDialog("No UI Elements",
                    "There are no UI elements added to the manager. Add some UI elements first from the UI Hierarchy tab.",
                    "OK");
                isUpdating = false;
                EditorUtility.ClearProgressBar();
                return;
            }

            if (ValidateUIReferences())
            {
                UpdateUILibrary();
            }
            else
            {
                Debug.LogWarning("Validation failed. Please check the UI elements.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Validation and update failed: {ex.Message}");
        }
        finally
        {
            isUpdating = false;
            EditorUtility.ClearProgressBar();
        }
    }

    private bool ValidateUIReferences()
    {
        var allReferences = uiManager.GetAllUICategories()
            .SelectMany(c => c.references)
            .ToList();

        var duplicateNames = allReferences
            .GroupBy(r => r.name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateNames.Any())
        {
            // Show Smart Naming Assistant instead of just an error
            bool openAssistant = EditorUtility.DisplayDialog("🤦‍♂️ Naming Conflicts Detected",
                $"Found duplicate UI reference names: {string.Join(", ", duplicateNames)}\n\n" +
                "These naming conflicts will break your generated code. Let me help you fix them automatically!\n\n" +
                "(This is exactly why you should name your UI elements properly from the start 😉)",
                "Open Smart Naming Assistant",
                "I'll Fix Manually");

            if (openAssistant)
            {
                SmartNamingAssistant.ShowWindow(uiManager);
            }

            return false;
        }

        return true;
    }

    private void UpdateUILibrary()
    {
        try
        {
            string sceneName = uiManager.gameObject.scene.name;
            // Sanitize the scene name
            string sanitizedSceneName = SanitizeIdentifier(sceneName);
            string fileName = $"{libraryClassPrefix}{sanitizedSceneName}.cs";
            string fullPath = Path.Combine(libraryOutputPath, fileName);

            // Create the library content
            string fileContent = GenerateLibraryContent(sceneName);

            // Ensure directory exists
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if file exists and ask the user what to do
            if (File.Exists(fullPath))
            {
                string message = $"A library file already exists at:\n{fullPath}\n\n" +
                                 "Overwriting this file may cause you to lose custom modifications. " +
                                 "Do you want to create a backup and overwrite, overwrite without backup, or cancel?";
                int option = EditorUtility.DisplayDialogComplex(
                    "Library File Exists",
                    message,
                    "Backup & Overwrite", // Option 0
                    "Overwrite",          // Option 1
                    "Cancel"              // Option 2
                );

                if (option == 2) // Cancel
                {
                    Debug.Log("Library generation cancelled by user.");
                    return;
                }
                else if (option == 0) // Backup & Overwrite
                {
                    string backupPath = fullPath.Replace(".cs", $"_{DateTime.Now:yyyyMMddHHmmss}.bak.cs");
                    try
                    {
                        File.Copy(fullPath, backupPath);
                        Debug.LogWarning($"Library backup created at: {backupPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to create library backup: {ex.Message}");
                        return;
                    }
                }
                // Option 1: Overwrite without backup.
            }

            // Write the file
            File.WriteAllText(fullPath, fileContent);
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(target);

            ShowQuickTip("UI Library Generated", "The UI Library has been successfully generated. You can now use it in your code.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to update UI Library: {ex.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to update UI Library: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Checks if the UI Library for the current scene has already been generated.
    /// </summary>
    private bool IsLibraryGenerated(string sceneName)
    {
        string sanitizedSceneName = SanitizeIdentifier(sceneName);
        string libraryFileName = $"{libraryClassPrefix}{sanitizedSceneName}.cs";
        string libraryFilePath = Path.Combine(libraryOutputPath, libraryFileName);
        return File.Exists(libraryFilePath);
    }

    private string GenerateLibraryContent(string sceneName)
    {
        // Sanitize the scene name for identifiers
        string sanitizedSceneName = SanitizeIdentifier(sceneName);

        var sb = new StringBuilder();

        // File header
        sb.AppendLine("// ========================================");
        sb.AppendLine("// AUTO-GENERATED CODE - DO NOT MODIFY");
        sb.AppendLine("// ========================================");
        sb.AppendLine($"// Scene: {sanitizedSceneName}");
        sb.AppendLine($"// Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("// UI Framework v2.0 - Dot Notation");
        sb.AppendLine("// ========================================");
        sb.AppendLine();

        // Using statements
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using TMPro;");
        sb.AppendLine("using System;");
        sb.AppendLine();

        // Namespace with scene for multi-scene support
        sb.AppendLine($"namespace {libraryNamespace}.{sanitizedSceneName}");
        sb.AppendLine("{");

        // Main UI class
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Auto-generated UI library for {sceneName}.");
        sb.AppendLine("    /// Provides type-safe dot notation access to all UI elements.");
        sb.AppendLine("    /// Usage: UI.Initialize(uiManager); then access via UI.PanelName.ElementName");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static class UI");
        sb.AppendLine("    {");
        sb.AppendLine("        private static UIManager _manager;");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Initialize the UI library with a UIManager instance.");
        sb.AppendLine("        /// Call this once at startup (e.g., in Awake or Start).");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public static void Initialize(UIManager manager)");
        sb.AppendLine("        {");
        sb.AppendLine("            _manager = manager;");
        sb.AppendLine("            if (_manager == null)");
        sb.AppendLine("            {");
        sb.AppendLine("                Debug.LogError(\"[UI Library] UIManager is null! UI elements will not be accessible.\");");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Build nested structure from UI elements
        GenerateNestedStructure(sb, sanitizedSceneName);

        // Close main UI class
        sb.AppendLine("    }");

        // Close namespace
        sb.AppendLine("}");

        return sb.ToString();
    }

    private void GenerateNestedStructure(StringBuilder sb, string sanitizedSceneName)
    {
        // Get all UI elements grouped by their hierarchy
        var allElements = uiManager.GetAllUICategories()
            .SelectMany(category => category.references.Select(r => new
            {
                Category = category.name,
                Reference = r
            }))
            .ToList();

        // Group elements by their panel (first part of name before first underscore)
        var panelGroups = allElements
            .GroupBy(e => GetPanelFromName(e.Reference.name))
            .OrderBy(g => g.Key);

        foreach (var panelGroup in panelGroups)
        {
            string panelName = panelGroup.Key;

            // Generate panel class
            sb.AppendLine();
            sb.AppendLine($"        #region {panelName}");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// {panelName} panel and its UI elements.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public static class {panelName}");
            sb.AppendLine("        {");

            // Check if this panel itself exists as an element
            var panelElement = panelGroup.FirstOrDefault(e => e.Category == "Panel");
            if (panelElement != null)
            {
                GeneratePanelManagementMethods(sb, panelElement.Reference);
            }

            // Group elements by type within this panel
            var elementsByType = panelGroup
                .Where(e => e.Category != "Panel") // Exclude the panel itself
                .GroupBy(e => e.Category)
                .OrderBy(g => g.Key);

            foreach (var typeGroup in elementsByType)
            {
                sb.AppendLine();
                sb.AppendLine($"            #region {typeGroup.Key}");

                foreach (var element in typeGroup.OrderBy(e => e.Reference.name))
                {
                    GenerateElementProperty(sb, element.Category, element.Reference, panelName);
                }

                sb.AppendLine($"            #endregion");
            }

            // Close panel class
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        #endregion");
        }
    }

    private string GetPanelFromName(string elementName)
    {
        // Parse element name to extract panel
        // Format: Panel_Purpose_Type or Panel_Type
        // Example: Main_Menu_Panel_Play_Button -> MainMenu

        var parts = elementName.Split('_');

        // Find where "Panel" appears
        int panelIndex = -1;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Equals("Panel", StringComparison.OrdinalIgnoreCase))
            {
                panelIndex = i;
                break;
            }
        }

        if (panelIndex > 0)
        {
            // Everything before "Panel" is the panel name
            return string.Join("", parts.Take(panelIndex));
        }

        // Fallback: use first part
        return parts.Length > 0 ? parts[0] : "Main";
    }

    private void GeneratePanelManagementMethods(StringBuilder sb, UIReference panelRef)
    {
        string panelPath = panelRef.fullPath;

        sb.AppendLine();
        sb.AppendLine("            /// <summary>");
        sb.AppendLine("            /// Shows this panel.");
        sb.AppendLine("            /// </summary>");
        sb.AppendLine("            /// <param name=\"hideOthers\">Hide all other panels</param>");
        sb.AppendLine("            public static void Show(bool hideOthers = true)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (_manager == null) { Debug.LogError(\"[UI] UIManager not initialized!\"); return; }");
        sb.AppendLine($"                _manager.SetPanelActive(\"{panelPath}\", true, deactivateOthers: hideOthers);");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            /// <summary>");
        sb.AppendLine("            /// Hides this panel.");
        sb.AppendLine("            /// </summary>");
        sb.AppendLine("            public static void Hide()");
        sb.AppendLine("            {");
        sb.AppendLine("                if (_manager == null) { Debug.LogError(\"[UI] UIManager not initialized!\"); return; }");
        sb.AppendLine($"                _manager.SetPanelActive(\"{panelPath}\", false);");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            /// <summary>");
        sb.AppendLine("            /// Toggles this panel's visibility.");
        sb.AppendLine("            /// </summary>");
        sb.AppendLine("            public static void Toggle()");
        sb.AppendLine("            {");
        sb.AppendLine("                if (_manager == null) { Debug.LogError(\"[UI] UIManager not initialized!\"); return; }");
        sb.AppendLine($"                var panel = _manager.GetPanel(\"{panelPath}\");");
        sb.AppendLine("                if (panel != null) panel.SetActive(!panel.activeSelf);");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            /// <summary>");
        sb.AppendLine("            /// Gets whether this panel is currently visible.");
        sb.AppendLine("            /// </summary>");
        sb.AppendLine("            public static bool IsVisible");
        sb.AppendLine("            {");
        sb.AppendLine("                get");
        sb.AppendLine("                {");
        sb.AppendLine("                    if (_manager == null) return false;");
        sb.AppendLine($"                    var panel = _manager.GetPanel(\"{panelPath}\");");
        sb.AppendLine("                    return panel != null && panel.activeSelf;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
    }

    private void GenerateElementProperty(StringBuilder sb, string category, UIReference element, string panelName)
    {
        string elementPath = element.fullPath;
        string sanitizedName = SanitizeIdentifier(GetElementNameWithoutPanel(element.name, panelName));
        string componentType = GetComponentTypeForCategory(category);

        sb.AppendLine();
        sb.AppendLine("            /// <summary>");
        sb.AppendLine($"            /// {sanitizedName} ({category})");
        sb.AppendLine("            /// </summary>");
        sb.AppendLine($"            public static {componentType} {sanitizedName}");
        sb.AppendLine("            {");
        sb.AppendLine("                get");
        sb.AppendLine("                {");
        sb.AppendLine("                    if (_manager == null)");
        sb.AppendLine("                    {");
        sb.AppendLine($"                        Debug.LogError(\"[UI] UIManager not initialized! Cannot access {sanitizedName}\");");
        sb.AppendLine("                        return null;");
        sb.AppendLine("                    }");
        sb.AppendLine($"                    return _manager.GetUIComponent<{componentType}>(\"{elementPath}\");");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
    }

    private string GetElementNameWithoutPanel(string fullName, string panelName)
    {
        // Remove panel prefix from element name
        // Example: Main_Menu_Panel_Play_Button -> Play_Button

        var parts = fullName.Split('_');
        var panelParts = panelName.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

        // Skip panel parts and "Panel" keyword
        int skipCount = panelParts.Length + 1; // +1 for "Panel" keyword

        if (parts.Length > skipCount)
        {
            return string.Join("_", parts.Skip(skipCount));
        }

        return fullName;
    }

    private string GetComponentTypeForCategory(string category)
    {
        return category switch
        {
            "Button" => "Button",
            "Text" => "Text",
            "TMP_Text" => "TMP_Text",
            "Toggle" => "Toggle",
            "InputField" => "InputField",
            "TMP_InputField" => "TMP_InputField",
            "Slider" => "Slider",
            "Dropdown" => "Dropdown",
            "TMP_Dropdown" => "TMP_Dropdown",
            "ScrollView" or "ScrollRect" => "ScrollRect",
            "Image" => "Image",
            "RawImage" => "RawImage",
            "Panel" => "GameObject",
            "Canvas" => "Canvas",
            "CanvasGroup" => "CanvasGroup",
            _ => "GameObject"
        };
    }

    /// <summary>
    /// Shows a preview of the generated library code.
    /// </summary>
    private void ShowLibraryCodePreview()
    {
        string sceneName = uiManager.gameObject.scene.name;
        string preview = GenerateLibraryContent(sceneName);

        CodePreviewWindow codePreviewWindow = EditorWindow.GetWindow<CodePreviewWindow>(true, "Library Code Preview", true);
        codePreviewWindow.SetContent(preview, "UI.cs");
    }

    #endregion
}

/// <summary>
/// Window for showing code previews.
/// </summary>
/// <summary>
/// Enhanced window for showing code previews with syntax highlighting and better UI.
/// </summary>
public class CodePreviewWindow : EditorWindow
{
    private string codeContent = "";
    private Vector2 scrollPosition;
    private string fileName = "";
    private bool showLineNumbers = true;
    private float leftMargin = 40f; // For line numbers

    // Theme colors
    private Color backgroundColor = new Color(0.15f, 0.15f, 0.18f);
    private Color lineNumberColor = new Color(0.4f, 0.4f, 0.5f, 0.8f);
    private Color lineNumberBgColor = new Color(0.17f, 0.17f, 0.2f);
    private Color selectedLineBgColor = new Color(0.2f, 0.3f, 0.4f, 0.4f);
    private Color commentColor = new Color(0.4f, 0.6f, 0.4f);
    private Color keywordColor = new Color(0.3f, 0.5f, 0.9f);
    private Color stringColor = new Color(0.9f, 0.6f, 0.3f);
    private Color typeColor = new Color(0.4f, 0.8f, 0.7f);
    private Color identifierColor = new Color(0.9f, 0.9f, 0.9f);

    private bool isDragging = false;
    private Rect dragArea;

    public void SetContent(string content, string filename)
    {
        codeContent = content;
        fileName = filename;
        position = new Rect(
            Screen.width / 2 - 400,
            Screen.height / 2 - 300,
            800,
            600
        );
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Code Preview");
        minSize = new Vector2(500, 300);
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawCodeEditor();
        DrawStatusBar();

        // Handle drag for resizing margins
        HandleDragEvents();
    }

    private void DrawToolbar()
    {
        // Toolbar background
        Rect toolbarRect = new Rect(0, 0, position.width, 28);
        EditorGUI.DrawRect(toolbarRect, new Color(0.2f, 0.2f, 0.23f));
        EditorGUI.DrawRect(new Rect(0, toolbarRect.height - 1, position.width, 1), new Color(0.3f, 0.3f, 0.35f));

        // File name display
        GUIStyle fileNameStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.8f, 0.8f, 0.9f) }
        };

        EditorGUI.LabelField(
            new Rect(10, 5, 250, 20),
            fileName,
            fileNameStyle
        );

        // Copy button
        Rect copyButtonRect = new Rect(position.width - 140, 4, 130, 20);
        if (GUI.Button(copyButtonRect, "Copy to Clipboard"))
        {
            EditorGUIUtility.systemCopyBuffer = codeContent;
            ShowNotification(new GUIContent("Copied to clipboard"));
        }

        // Line numbers toggle
        Rect lineNumbersRect = new Rect(260, 5, 120, 20);
        showLineNumbers = EditorGUI.ToggleLeft(
            lineNumbersRect,
            "Show Line Numbers",
            showLineNumbers
        );
    }

    private void DrawCodeEditor()
    {
        // Editor background
        Rect codeRect = new Rect(0, 28, position.width, position.height - 28 - 22); // Subtract toolbar and status bar heights
        EditorGUI.DrawRect(codeRect, backgroundColor);

        // Draw line numbers background if enabled
        if (showLineNumbers)
        {
            EditorGUI.DrawRect(
                new Rect(0, 28, leftMargin, codeRect.height),
                lineNumberBgColor
            );

            // Divider line
            EditorGUI.DrawRect(
                new Rect(leftMargin - 1, 28, 1, codeRect.height),
                new Color(0.3f, 0.3f, 0.35f)
            );

            // Draw drag handle
            dragArea = new Rect(leftMargin - 3, 28, 6, codeRect.height);
            EditorGUIUtility.AddCursorRect(dragArea, MouseCursor.ResizeHorizontal);
        }

        // Code content
        GUIStyle codeStyle = new GUIStyle(EditorStyles.textArea)
        {
            font = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font,
            fontSize = 12,
            wordWrap = false,
            richText = true,
            border = new RectOffset(0, 0, 0, 0),
            normal = {
                background = null,
                textColor = identifierColor
            }
        };

        // Calculate line count for line numbers
        int lineCount = codeContent.Split('\n').Length;

        Rect scrollViewRect = new Rect(
            showLineNumbers ? leftMargin : 0,
            28,
            position.width - (showLineNumbers ? leftMargin : 0),
            codeRect.height
        );

        scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, new Rect(0, 0, position.width, lineCount * 18 + 10));

        // Draw the code with syntax highlighting (already processed in the content)
        GUI.Label(new Rect(5, 5, position.width - 30, lineCount * 18), codeContent, codeStyle);

        // Draw line numbers if enabled
        if (showLineNumbers)
        {
            GUIStyle lineNumberStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = lineNumberColor }
            };

            for (int i = 0; i < lineCount; i++)
            {
                GUI.Label(
                    new Rect(0, 5 + i * 18, leftMargin - 8, 18),
                    (i + 1).ToString(),
                    lineNumberStyle
                );
            }
        }

        GUI.EndScrollView();
    }

    private void DrawStatusBar()
    {
        // Status bar background
        Rect statusRect = new Rect(0, position.height - 22, position.width, 22);
        EditorGUI.DrawRect(statusRect, new Color(0.2f, 0.2f, 0.23f));
        EditorGUI.DrawRect(new Rect(0, statusRect.y, position.width, 1), new Color(0.3f, 0.3f, 0.35f));

        // Line count display
        GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 10,
            normal = { textColor = new Color(0.7f, 0.7f, 0.75f) }
        };

        int lineCount = codeContent.Split('\n').Length;
        EditorGUI.LabelField(
            new Rect(10, position.height - 19, 200, 16),
            $"Lines: {lineCount}",
            statusStyle
        );

        // Close button
        Rect closeButtonRect = new Rect(position.width - 70, position.height - 20, 60, 18);
        if (GUI.Button(closeButtonRect, "Close"))
        {
            Close();
        }
    }

    private void HandleDragEvents()
    {
        if (!showLineNumbers) return;

        Event evt = Event.current;

        switch (evt.type)
        {
            case EventType.MouseDown:
                if (dragArea.Contains(evt.mousePosition))
                {
                    isDragging = true;
                    evt.Use();
                }
                break;

            case EventType.MouseDrag:
                if (isDragging)
                {
                    leftMargin = Mathf.Clamp(evt.mousePosition.x, 30, 150);
                    Repaint();
                    evt.Use();
                }
                break;

            case EventType.MouseUp:
                if (isDragging)
                {
                    isDragging = false;
                    evt.Use();
                }
                break;
        }
    }
}
#endif