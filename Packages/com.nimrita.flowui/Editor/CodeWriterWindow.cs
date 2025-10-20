#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Enhanced window for writing and editing code files with syntax highlighting and VS-style UI.
/// Reuses the same visual style as CodePreviewWindow but with full editing capabilities.
/// </summary>
public class CodeWriterWindow : EditorWindow
{
    private string codeContent = "";
    private Vector2 scrollPosition;
    private string fileName = "NewScript.cs";
    private string filePath = "";
    private bool showLineNumbers = true;
    private float leftMargin = 40f; // For line numbers
    private new bool hasUnsavedChanges = false; // Use 'new' to hide EditorWindow's hasUnsavedChanges

    // Theme colors (matching CodePreviewWindow)
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
    private string originalContent = ""; // Track original content for dirty checking

    /// <summary>
    /// Opens the code writer window with optional initial content.
    /// </summary>
    public static CodeWriterWindow ShowWindow(string initialContent = "", string fileName = "NewScript.cs", string savePath = "")
    {
        CodeWriterWindow window = GetWindow<CodeWriterWindow>(true, "Code Writer", true);
        window.SetContent(initialContent, fileName, savePath);
        return window;
    }

    public void SetContent(string content, string filename, string path = "")
    {
        codeContent = content;
        originalContent = content;
        fileName = filename;
        filePath = string.IsNullOrEmpty(path) ? "" : path;
        hasUnsavedChanges = false;

        position = new Rect(
            Screen.width / 2 - 500,
            Screen.height / 2 - 350,
            1000,
            700
        );
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Code Writer");
        minSize = new Vector2(600, 400);
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawCodeEditor();
        DrawStatusBar();

        // Handle drag for resizing margins
        HandleDragEvents();

        // Track dirty state
        if (GUI.changed)
        {
            hasUnsavedChanges = codeContent != originalContent;
        }
    }

    private void DrawToolbar()
    {
        // Toolbar background
        Rect toolbarRect = new Rect(0, 0, position.width, 40);
        EditorGUI.DrawRect(toolbarRect, new Color(0.2f, 0.2f, 0.23f));
        EditorGUI.DrawRect(new Rect(0, toolbarRect.height - 1, position.width, 1), new Color(0.3f, 0.3f, 0.35f));

        // File name display and edit
        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.8f, 0.8f, 0.9f) }
        };

        EditorGUI.LabelField(
            new Rect(10, 7, 60, 20),
            "File Name:",
            labelStyle
        );

        GUIStyle fileNameStyle = new GUIStyle(EditorStyles.textField)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
        };

        string newFileName = EditorGUI.TextField(
            new Rect(75, 7, 250, 18),
            fileName,
            fileNameStyle
        );

        if (newFileName != fileName)
        {
            fileName = newFileName;
            hasUnsavedChanges = true;
        }

        // Path display
        EditorGUI.LabelField(
            new Rect(10, 25, 40, 16),
            "Path:",
            new GUIStyle(EditorStyles.miniLabel) { fontSize = 9, normal = { textColor = new Color(0.7f, 0.7f, 0.75f) } }
        );

        GUIStyle pathStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 9,
            normal = { textColor = new Color(0.6f, 0.6f, 0.65f) }
        };

        EditorGUI.LabelField(
            new Rect(50, 25, 275, 16),
            string.IsNullOrEmpty(filePath) ? "Not saved yet" : filePath,
            pathStyle
        );

        // Browse button
        Rect browseButtonRect = new Rect(335, 7, 70, 18);
        if (GUI.Button(browseButtonRect, "Browse...", EditorStyles.miniButton))
        {
            BrowseForSavePath();
        }

        // Line numbers toggle - moved further right to avoid overlap
        GUIStyle toggleStyle = new GUIStyle(EditorStyles.toggle)
        {
            fontSize = 10,
            normal = { textColor = new Color(0.8f, 0.8f, 0.85f) }
        };

        showLineNumbers = GUI.Toggle(
            new Rect(position.width - 450, 10, 150, 20),
            showLineNumbers,
            "Show Line Numbers",
            toggleStyle
        );

        // Save button (primary action)
        Rect saveButtonRect = new Rect(position.width - 235, 7, 70, 26);
        GUI.backgroundColor = hasUnsavedChanges ? new Color(0.3f, 0.6f, 0.9f) : new Color(0.5f, 0.5f, 0.5f);
        if (GUI.Button(saveButtonRect, "Save", new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = FontStyle.Bold }))
        {
            SaveFile(false);
        }
        GUI.backgroundColor = Color.white;

        // Save As button
        Rect saveAsButtonRect = new Rect(position.width - 160, 7, 70, 26);
        if (GUI.Button(saveAsButtonRect, "Save As...", EditorStyles.miniButton))
        {
            SaveFile(true);
        }

        // Close button
        Rect closeButtonRect = new Rect(position.width - 85, 7, 70, 26);
        if (GUI.Button(closeButtonRect, "Close", EditorStyles.miniButton))
        {
            CloseWindow();
        }
    }

    private void DrawCodeEditor()
    {
        // Editor background
        Rect codeRect = new Rect(0, 40, position.width, position.height - 40 - 24); // Subtract toolbar and status bar heights
        EditorGUI.DrawRect(codeRect, backgroundColor);

        // Draw line numbers background if enabled
        if (showLineNumbers)
        {
            EditorGUI.DrawRect(
                new Rect(0, 40, leftMargin, codeRect.height),
                lineNumberBgColor
            );

            // Divider line
            EditorGUI.DrawRect(
                new Rect(leftMargin - 1, 40, 1, codeRect.height),
                new Color(0.3f, 0.3f, 0.35f)
            );

            // Draw drag handle
            dragArea = new Rect(leftMargin - 3, 40, 6, codeRect.height);
            EditorGUIUtility.AddCursorRect(dragArea, MouseCursor.ResizeHorizontal);
        }

        // Calculate line count for line numbers
        int lineCount = codeContent.Split('\n').Length;

        // Code content area
        GUIStyle codeStyle = new GUIStyle(EditorStyles.textArea)
        {
            font = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font,
            fontSize = 12,
            wordWrap = false,
            richText = false, // We'll handle syntax highlighting differently for editable text
            border = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(5, 5, 5, 5),
            normal = {
                background = null,
                textColor = identifierColor
            }
        };

        // Calculate text area dimensions
        float textAreaWidth = position.width - (showLineNumbers ? leftMargin : 0) - 30;
        float textAreaHeight = Math.Max(codeRect.height - 10, lineCount * 18);

        Rect scrollViewRect = new Rect(
            showLineNumbers ? leftMargin : 0,
            40,
            position.width - (showLineNumbers ? leftMargin : 0),
            codeRect.height
        );

        scrollPosition = GUI.BeginScrollView(
            scrollViewRect,
            scrollPosition,
            new Rect(0, 0, textAreaWidth, textAreaHeight + 20)
        );

        // Editable text area
        string newContent = GUI.TextArea(
            new Rect(5, 5, textAreaWidth, textAreaHeight),
            codeContent,
            codeStyle
        );

        if (newContent != codeContent)
        {
            codeContent = newContent;
            hasUnsavedChanges = true;
        }

        GUI.EndScrollView();

        // Draw line numbers OUTSIDE scroll view if enabled
        if (showLineNumbers)
        {
            GUIStyle lineNumberStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = lineNumberColor }
            };

            // Calculate visible line range based on scroll position
            int firstVisibleLine = Mathf.FloorToInt(scrollPosition.y / 18);
            int visibleLineCount = Mathf.CeilToInt(codeRect.height / 18) + 1;
            int lastVisibleLine = Mathf.Min(firstVisibleLine + visibleLineCount, lineCount);

            // Draw only visible line numbers
            for (int i = firstVisibleLine; i < lastVisibleLine; i++)
            {
                float yPos = 40 + (i * 18) - scrollPosition.y;
                if (yPos >= 40 && yPos < 40 + codeRect.height)
                {
                    GUI.Label(
                        new Rect(5, yPos, leftMargin - 10, 18),
                        (i + 1).ToString(),
                        lineNumberStyle
                    );
                }
            }
        }
    }

    private void DrawStatusBar()
    {
        // Status bar background
        Rect statusRect = new Rect(0, position.height - 24, position.width, 24);
        EditorGUI.DrawRect(statusRect, new Color(0.2f, 0.2f, 0.23f));
        EditorGUI.DrawRect(new Rect(0, statusRect.y, position.width, 1), new Color(0.3f, 0.3f, 0.35f));

        // Status indicators
        GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 10,
            normal = { textColor = new Color(0.7f, 0.7f, 0.75f) }
        };

        int lineCount = codeContent.Split('\n').Length;
        int charCount = codeContent.Length;

        EditorGUI.LabelField(
            new Rect(10, position.height - 21, 200, 18),
            $"Lines: {lineCount} | Characters: {charCount}",
            statusStyle
        );

        // Unsaved changes indicator
        if (hasUnsavedChanges)
        {
            GUIStyle unsavedStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.9f, 0.6f, 0.3f) }
            };

            EditorGUI.LabelField(
                new Rect(position.width - 180, position.height - 21, 170, 18),
                "● Unsaved changes",
                unsavedStyle
            );
        }
        else if (!string.IsNullOrEmpty(filePath))
        {
            GUIStyle savedStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.4f, 0.8f, 0.4f) }
            };

            EditorGUI.LabelField(
                new Rect(position.width - 180, position.height - 21, 170, 18),
                "✓ Saved",
                savedStyle
            );
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

    private void BrowseForSavePath()
    {
        string directory = string.IsNullOrEmpty(filePath) ? "Assets" : Path.GetDirectoryName(filePath);
        string selectedPath = EditorUtility.SaveFilePanel("Save Code File", directory, fileName, "cs");

        if (!string.IsNullOrEmpty(selectedPath))
        {
            // Convert to relative path if inside Assets folder
            if (selectedPath.StartsWith(Application.dataPath))
            {
                filePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
            }
            else
            {
                filePath = selectedPath;
            }

            fileName = Path.GetFileName(filePath);
        }
    }

    private void SaveFile(bool saveAs)
    {
        string targetPath = filePath;

        if (saveAs || string.IsNullOrEmpty(targetPath))
        {
            BrowseForSavePath();
            targetPath = filePath;
        }

        if (string.IsNullOrEmpty(targetPath))
        {
            Debug.LogWarning("[CodeWriter] Save cancelled - no path specified");
            return;
        }

        try
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write file
            File.WriteAllText(targetPath, codeContent);

            // Update state
            originalContent = codeContent;
            hasUnsavedChanges = false;
            filePath = targetPath;

            // Refresh Unity's asset database if saved in Assets folder
            if (targetPath.StartsWith("Assets") || targetPath.StartsWith(Application.dataPath))
            {
                AssetDatabase.Refresh();
            }

            Debug.Log($"[CodeWriter] File saved successfully: {targetPath}");
            ShowNotification(new GUIContent($"Saved: {fileName}"));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CodeWriter] Failed to save file: {ex.Message}");
            EditorUtility.DisplayDialog("Save Failed", $"Failed to save file:\n{ex.Message}", "OK");
        }
    }

    private void CloseWindow()
    {
        if (hasUnsavedChanges)
        {
            int option = EditorUtility.DisplayDialogComplex(
                "Unsaved Changes",
                $"You have unsaved changes in '{fileName}'. Do you want to save before closing?",
                "Save",
                "Don't Save",
                "Cancel"
            );

            switch (option)
            {
                case 0: // Save
                    SaveFile(false);
                    if (!hasUnsavedChanges) // Only close if save succeeded
                    {
                        Close();
                    }
                    break;
                case 1: // Don't Save
                    Close();
                    break;
                case 2: // Cancel
                    break;
            }
        }
        else
        {
            Close();
        }
    }

    private void OnDestroy()
    {
        // Final chance to save
        if (hasUnsavedChanges)
        {
            Debug.LogWarning($"[CodeWriter] Window closed with unsaved changes in '{fileName}'");
        }
    }
}
#endif
