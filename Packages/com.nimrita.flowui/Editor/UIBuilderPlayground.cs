#if UNITY_EDITOR
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Live UI Builder Playground - Write UI builder code and execute it in edit mode!
/// Creates actual GameObjects in the scene hierarchy without entering Play mode.
/// </summary>
public class UIBuilderPlayground : EditorWindow
{
    private string builderCode = "";
    private Vector2 scrollPosition;
    private Vector2 errorScrollPosition;
    private UIManager targetUIManager;
    private bool showLineNumbers = true;
    private float leftMargin = 40f;
    private string lastError = "";
    private bool hasError = false;

    // Theme colors
    private Color backgroundColor = new Color(0.15f, 0.15f, 0.18f);
    private Color lineNumberColor = new Color(0.4f, 0.4f, 0.5f, 0.8f);
    private Color lineNumberBgColor = new Color(0.17f, 0.17f, 0.2f);
    private Color errorBgColor = new Color(0.4f, 0.15f, 0.15f, 0.3f);
    private Color successBgColor = new Color(0.15f, 0.4f, 0.15f, 0.3f);

    [MenuItem("Tools/UI System/UI Builder Playground", false, 50)]
    public static void ShowWindow()
    {
        UIBuilderPlayground window = GetWindow<UIBuilderPlayground>("UI Builder Playground");
        window.minSize = new Vector2(700, 500);
        window.Show();
    }

    private void OnEnable()
    {
        // Load saved code from EditorPrefs
        builderCode = EditorPrefs.GetString("UIBuilderPlayground_Code", GetDefaultTemplate());

        // Try to find UIManager in scene
        if (targetUIManager == null)
        {
            targetUIManager = FindObjectOfType<UIManager>();
        }
    }

    private void OnDisable()
    {
        // Save code to EditorPrefs
        EditorPrefs.SetString("UIBuilderPlayground_Code", builderCode);
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawCodeEditor();
        if (hasError)
        {
            DrawErrorPanel();
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // UIManager reference
        GUILayout.Label("UIManager:", GUILayout.Width(80));
        UIManager newManager = (UIManager)EditorGUILayout.ObjectField(
            targetUIManager,
            typeof(UIManager),
            true,
            GUILayout.Width(200)
        );

        if (newManager != targetUIManager)
        {
            targetUIManager = newManager;
            hasError = false;
            lastError = "";
        }

        GUILayout.FlexibleSpace();

        // Line numbers toggle
        showLineNumbers = GUILayout.Toggle(showLineNumbers, "Line Numbers", EditorStyles.toolbarButton, GUILayout.Width(100));

        // Clear button
        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            builderCode = "";
            hasError = false;
            lastError = "";
            GUI.FocusControl(null);
        }

        // Load template button
        if (GUILayout.Button("Load Template", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            ShowTemplateMenu();
        }

        // Execute button (primary action)
        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
        if (GUILayout.Button("Execute (Ctrl+E)", EditorStyles.toolbarButton, GUILayout.Width(120)))
        {
            ExecuteBuilderCode();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCodeEditor()
    {
        // Calculate editor area
        float toolbarHeight = 20;
        float errorPanelHeight = hasError ? 120 : 0;
        float editorHeight = position.height - toolbarHeight - errorPanelHeight - 5;

        Rect editorRect = new Rect(0, toolbarHeight, position.width, editorHeight);

        // Background
        EditorGUI.DrawRect(editorRect, backgroundColor);

        // Line numbers background
        if (showLineNumbers)
        {
            EditorGUI.DrawRect(new Rect(0, toolbarHeight, leftMargin, editorHeight), lineNumberBgColor);
            EditorGUI.DrawRect(new Rect(leftMargin - 1, toolbarHeight, 1, editorHeight), new Color(0.3f, 0.3f, 0.35f));
        }

        // Code area
        int lineCount = builderCode.Split('\n').Length;
        float contentWidth = position.width - (showLineNumbers ? leftMargin : 0) - 20;
        float contentHeight = Math.Max(editorHeight - 10, lineCount * 18);

        Rect scrollViewRect = new Rect(
            showLineNumbers ? leftMargin : 0,
            toolbarHeight,
            position.width - (showLineNumbers ? leftMargin : 0),
            editorHeight
        );

        scrollPosition = GUI.BeginScrollView(
            scrollViewRect,
            scrollPosition,
            new Rect(0, 0, contentWidth, contentHeight)
        );

        GUIStyle codeStyle = new GUIStyle(EditorStyles.textArea)
        {
            fontSize = 12,
            wordWrap = false,
            richText = false,
            padding = new RectOffset(5, 5, 5, 5),
            normal = {
                background = null,
                textColor = new Color(0.9f, 0.9f, 0.95f)
            }
        };

        builderCode = GUI.TextArea(
            new Rect(5, 5, contentWidth - 10, contentHeight),
            builderCode,
            codeStyle
        );

        GUI.EndScrollView();

        // Draw line numbers outside scroll view
        if (showLineNumbers)
        {
            GUIStyle lineNumberStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = lineNumberColor }
            };

            int firstVisibleLine = Mathf.FloorToInt(scrollPosition.y / 18);
            int visibleLineCount = Mathf.CeilToInt(editorHeight / 18) + 1;
            int lastVisibleLine = Mathf.Min(firstVisibleLine + visibleLineCount, lineCount);

            for (int i = firstVisibleLine; i < lastVisibleLine; i++)
            {
                float yPos = toolbarHeight + (i * 18) - scrollPosition.y;
                if (yPos >= toolbarHeight && yPos < toolbarHeight + editorHeight)
                {
                    GUI.Label(
                        new Rect(5, yPos, leftMargin - 10, 18),
                        (i + 1).ToString(),
                        lineNumberStyle
                    );
                }
            }
        }

        // Handle keyboard shortcuts
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.E)
        {
            ExecuteBuilderCode();
            e.Use();
        }
    }

    private void DrawErrorPanel()
    {
        float errorPanelHeight = 120;
        Rect errorRect = new Rect(0, position.height - errorPanelHeight, position.width, errorPanelHeight);

        EditorGUI.DrawRect(errorRect, errorBgColor);
        EditorGUI.DrawRect(new Rect(0, errorRect.y, position.width, 1), new Color(0.7f, 0.3f, 0.3f));

        GUILayout.BeginArea(new Rect(errorRect.x + 10, errorRect.y + 5, errorRect.width - 20, errorRect.height - 10));

        GUIStyle errorHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = new Color(1f, 0.5f, 0.5f) }
        };

        EditorGUILayout.LabelField("❌ Execution Error", errorHeaderStyle);

        GUIStyle errorStyle = new GUIStyle(EditorStyles.textArea)
        {
            fontSize = 10,
            wordWrap = true,
            normal = { textColor = new Color(1f, 0.8f, 0.8f) }
        };

        errorScrollPosition = EditorGUILayout.BeginScrollView(errorScrollPosition, GUILayout.Height(errorPanelHeight - 35));
        EditorGUILayout.TextArea(lastError, errorStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    private void ExecuteBuilderCode()
    {
        hasError = false;
        lastError = "";

        // Validate UIManager
        if (targetUIManager == null)
        {
            ShowError("No UIManager assigned! Please assign a UIManager in the scene.");
            return;
        }

        // Validate code
        if (string.IsNullOrWhiteSpace(builderCode))
        {
            ShowError("No code to execute! Write some UI builder code first.");
            return;
        }

        try
        {
            // Wrap the user code in a class and method
            string wrappedCode = WrapCodeForCompilation(builderCode);

            // Compile the code
            CompilerResults results = CompileCode(wrappedCode);

            if (results.Errors.HasErrors)
            {
                StringBuilder errorMsg = new StringBuilder();
                foreach (CompilerError error in results.Errors)
                {
                    // Adjust line numbers to account for wrapper code
                    int actualLine = error.Line - 18; // Offset for wrapper lines
                    errorMsg.AppendLine($"Line {actualLine}: {error.ErrorText}");
                }
                ShowError("Compilation failed:\n\n" + errorMsg.ToString());
                return;
            }

            // Execute the compiled code
            Assembly assembly = results.CompiledAssembly;
            Type generatedType = assembly.GetType("UIBuilderPlaygroundGenerated.GeneratedUIBuilder");
            MethodInfo executeMethod = generatedType.GetMethod("Execute");

            // Mark UIManager for undo
            Undo.RecordObject(targetUIManager, "Execute UI Builder Code");

            // Execute!
            executeMethod.Invoke(null, new object[] { targetUIManager });

            // Mark scene dirty
            EditorUtility.SetDirty(targetUIManager);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("<color=green>✓ UI Builder code executed successfully!</color>");
            ShowSuccess();
        }
        catch (Exception ex)
        {
            ShowError($"Execution failed:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}");
        }
    }

    private string WrapCodeForCompilation(string userCode)
    {
        StringBuilder sb = new StringBuilder();

        // Using statements
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using TMPro;");
        sb.AppendLine("using System;");
        sb.AppendLine("using UnityEngine.Events;");
        sb.AppendLine();

        // Namespace and class wrapper
        sb.AppendLine("namespace UIBuilderPlaygroundGenerated");
        sb.AppendLine("{");
        sb.AppendLine("    public static class GeneratedUIBuilder");
        sb.AppendLine("    {");
        sb.AppendLine("        public static void Execute(UIManager uiManager)");
        sb.AppendLine("        {");
        sb.AppendLine("            // User code starts here");
        sb.AppendLine(IndentCode(userCode, 12));
        sb.AppendLine("            // User code ends here");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string IndentCode(string code, int spaces)
    {
        string indent = new string(' ', spaces);
        string[] lines = code.Split('\n');
        return string.Join("\n", lines.Select(line => indent + line));
    }

    private CompilerResults CompileCode(string code)
    {
        CSharpCodeProvider provider = new CSharpCodeProvider();
        CompilerParameters parameters = new CompilerParameters();

        // Get all currently loaded assemblies
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

        // Use netstandard instead of mscorlib for Unity 2020+
        bool useNetStandard = true;
        HashSet<string> addedAssemblies = new HashSet<string>();

        foreach (var assembly in assemblies)
        {
            try
            {
                if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                {
                    string name = assembly.GetName().Name;

                    // Decide which base library to use
                    if (useNetStandard)
                    {
                        // Use netstandard and skip mscorlib
                        if (name == "mscorlib")
                            continue;

                        if (name == "netstandard" ||
                            name == "System" ||
                            name == "System.Core" ||
                            name.StartsWith("UnityEngine") ||
                            name == "Unity.TextMeshPro" ||
                            name.Contains("Assembly-CSharp"))
                        {
                            if (addedAssemblies.Add(assembly.Location))
                            {
                                parameters.ReferencedAssemblies.Add(assembly.Location);
                            }
                        }
                    }
                    else
                    {
                        // Use mscorlib and skip netstandard
                        if (name == "netstandard")
                            continue;

                        if (name == "mscorlib" ||
                            name == "System" ||
                            name == "System.Core" ||
                            name.StartsWith("UnityEngine") ||
                            name == "Unity.TextMeshPro" ||
                            name.Contains("Assembly-CSharp"))
                        {
                            if (addedAssemblies.Add(assembly.Location))
                            {
                                parameters.ReferencedAssemblies.Add(assembly.Location);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Skip assemblies that can't be referenced
            }
        }

        parameters.GenerateInMemory = true;
        parameters.GenerateExecutable = false;

        return provider.CompileAssemblyFromSource(parameters, code);
    }

    private void ShowError(string error)
    {
        hasError = true;
        lastError = error;
        Debug.LogError("[UIBuilderPlayground] " + error);
        Repaint();
    }

    private void ShowSuccess()
    {
        hasError = false;
        lastError = "";
        Repaint();
    }

    private void ShowTemplateMenu()
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Button"), false, () => LoadTemplate(GetButtonTemplate()));
        menu.AddItem(new GUIContent("Panel with Buttons"), false, () => LoadTemplate(GetPanelTemplate()));
        menu.AddItem(new GUIContent("Input Form"), false, () => LoadTemplate(GetInputFormTemplate()));
        menu.AddItem(new GUIContent("Settings Menu"), false, () => LoadTemplate(GetSettingsTemplate()));
        menu.AddItem(new GUIContent("Complete UI"), false, () => LoadTemplate(GetCompleteUITemplate()));
        menu.ShowAsContext();
    }

    private void LoadTemplate(string template)
    {
        builderCode = template;
        hasError = false;
        lastError = "";
    }

    private string GetDefaultTemplate()
    {
        return @"// Write your UI builder code here
// Get the canvas (or create one if it doesn't exist)
var canvas = UIBuilderHelpers.EnsureCanvas();

// Example: Create a button
uiManager.CreateButton(""MyButton"")
    .WithText(""Click Me!"")
    .WithColors(Color.blue, Color.cyan, new Color(0f, 0f, 0.5f), Color.gray)
    .WithSize(200, 50)
    .WithPosition(new Vector2(0, 0))
    .OnClick(() => Debug.Log(""Button clicked!""))
    .AddTo(canvas.transform);";
    }

    private string GetButtonTemplate()
    {
        return @"// Simple Button Example
var canvas = UIBuilderHelpers.EnsureCanvas();

uiManager.CreateButton(""PlayButton"")
    .WithText(""Play Game"")
    .WithColors(
        new Color(0.2f, 0.6f, 0.2f), // Normal
        new Color(0.3f, 0.7f, 0.3f), // Hover
        new Color(0.1f, 0.5f, 0.1f), // Pressed
        new Color(0.5f, 0.5f, 0.5f)  // Disabled
    )
    .WithSize(200, 60)
    .WithPosition(new Vector2(0, 0))
    .OnClick(() => Debug.Log(""Play button clicked!""))
    .AddTo(canvas.transform);";
    }

    private string GetPanelTemplate()
    {
        return @"// Panel with Multiple Buttons
var canvas = UIBuilderHelpers.EnsureCanvas();

var panel = uiManager.CreatePanel(""MainPanel"")
    .WithSize(400, 300)
    .WithPosition(new Vector2(0, 0))
    .WithColor(new Color(0.2f, 0.2f, 0.2f, 0.9f))
    .AddTo(canvas.transform);

uiManager.CreateButton(""StartButton"")
    .WithText(""Start"")
    .WithSize(150, 40)
    .WithPosition(new Vector2(0, 80))
    .WithParent(panel.transform)
    .AddTo(canvas.transform);

uiManager.CreateButton(""OptionsButton"")
    .WithText(""Options"")
    .WithSize(150, 40)
    .WithPosition(new Vector2(0, 20))
    .WithParent(panel.transform)
    .AddTo(canvas.transform);

uiManager.CreateButton(""QuitButton"")
    .WithText(""Quit"")
    .WithSize(150, 40)
    .WithPosition(new Vector2(0, -40))
    .WithParent(panel.transform)
    .AddTo(canvas.transform);";
    }

    private string GetInputFormTemplate()
    {
        return @"// Input Form Example
var canvas = UIBuilderHelpers.EnsureCanvas();

var formPanel = uiManager.CreatePanel(""FormPanel"")
    .WithSize(400, 250)
    .WithPosition(new Vector2(0, 0))
    .WithColor(new Color(0.15f, 0.15f, 0.2f, 0.95f))
    .AddTo(canvas.transform);

uiManager.CreateText(""TitleText"")
    .WithText(""Login Form"")
    .WithFontSize(24)
    .WithPosition(new Vector2(0, 90))
    .WithParent(formPanel.transform)
    .AddTo(canvas.transform);

uiManager.CreateInputField(""UsernameInput"")
    .WithPlaceholder(""Username"")
    .WithSize(300, 40)
    .WithPosition(new Vector2(0, 30))
    .WithParent(formPanel.transform)
    .AddTo(canvas.transform);

uiManager.CreateInputField(""PasswordInput"")
    .WithPlaceholder(""Password"")
    .WithSize(300, 40)
    .WithPosition(new Vector2(0, -20))
    .WithParent(formPanel.transform)
    .AddTo(canvas.transform);

uiManager.CreateButton(""LoginButton"")
    .WithText(""Login"")
    .WithSize(150, 40)
    .WithPosition(new Vector2(0, -80))
    .WithParent(formPanel.transform)
    .OnClick(() => Debug.Log(""Login clicked!""))
    .AddTo(canvas.transform);";
    }

    private string GetSettingsTemplate()
    {
        return @"// Settings Menu with Toggles and Sliders
var canvas = UIBuilderHelpers.EnsureCanvas();

var settingsPanel = uiManager.CreatePanel(""SettingsPanel"")
    .WithSize(450, 400)
    .WithPosition(new Vector2(0, 0))
    .AddTo(canvas.transform);

uiManager.CreateText(""SettingsTitle"")
    .WithText(""Settings"")
    .WithFontSize(28)
    .WithPosition(new Vector2(0, 160))
    .WithParent(settingsPanel.transform)
    .AddTo(canvas.transform);

uiManager.CreateToggle(""SoundToggle"")
    .WithLabel(""Enable Sound"")
    .WithPosition(new Vector2(-100, 80))
    .WithParent(settingsPanel.transform)
    .AddTo(canvas.transform);

uiManager.CreateSlider(""VolumeSlider"")
    .WithRange(0, 100)
    .WithValue(75)
    .WithSize(300, 20)
    .WithPosition(new Vector2(0, 20))
    .WithParent(settingsPanel.transform)
    .AddTo(canvas.transform);

uiManager.CreateToggle(""FullscreenToggle"")
    .WithLabel(""Fullscreen"")
    .WithPosition(new Vector2(-100, -40))
    .WithParent(settingsPanel.transform)
    .AddTo(canvas.transform);

uiManager.CreateButton(""ApplyButton"")
    .WithText(""Apply"")
    .WithSize(120, 40)
    .WithPosition(new Vector2(-70, -120))
    .WithParent(settingsPanel.transform)
    .AddTo(canvas.transform);

uiManager.CreateButton(""CancelButton"")
    .WithText(""Cancel"")
    .WithSize(120, 40)
    .WithPosition(new Vector2(70, -120))
    .WithParent(settingsPanel.transform)
    .AddTo(canvas.transform);";
    }

    private string GetCompleteUITemplate()
    {
        return @"// Complete Game Menu UI
var canvas = UIBuilderHelpers.EnsureCanvas();

// Main Menu Panel
var mainMenu = uiManager.CreatePanel(""MainMenuPanel"")
    .WithSize(500, 600)
    .WithPosition(new Vector2(0, 0))
    .WithColor(new Color(0.1f, 0.1f, 0.15f, 0.95f))
    .AddTo(canvas.transform);

// Title
uiManager.CreateText(""GameTitle"")
    .WithText(""MY AWESOME GAME"")
    .WithFontSize(36)
    .WithColor(new Color(1f, 0.8f, 0.2f))
    .WithPosition(new Vector2(0, 220))
    .WithParent(mainMenu.transform)
    .AddTo(canvas.transform);

// Play Button
uiManager.CreateButton(""PlayButton"")
    .WithText(""PLAY"")
    .WithSize(250, 60)
    .WithPosition(new Vector2(0, 120))
    .WithColors(
        new Color(0.2f, 0.6f, 0.2f),
        new Color(0.3f, 0.7f, 0.3f),
        new Color(0.1f, 0.5f, 0.1f),
        new Color(0.5f, 0.5f, 0.5f)
    )
    .WithParent(mainMenu.transform)
    .OnClick(() => Debug.Log(""Starting game...""))
    .AddTo(canvas.transform);

// Options Button
uiManager.CreateButton(""OptionsButton"")
    .WithText(""OPTIONS"")
    .WithSize(250, 50)
    .WithPosition(new Vector2(0, 40))
    .WithParent(mainMenu.transform)
    .AddTo(canvas.transform);

// Credits Button
uiManager.CreateButton(""CreditsButton"")
    .WithText(""CREDITS"")
    .WithSize(250, 50)
    .WithPosition(new Vector2(0, -30))
    .WithParent(mainMenu.transform)
    .AddTo(canvas.transform);

// Quit Button
uiManager.CreateButton(""QuitButton"")
    .WithText(""QUIT"")
    .WithSize(250, 50)
    .WithPosition(new Vector2(0, -100))
    .WithColors(
        new Color(0.6f, 0.2f, 0.2f),
        new Color(0.7f, 0.3f, 0.3f),
        new Color(0.5f, 0.1f, 0.1f),
        new Color(0.5f, 0.5f, 0.5f)
    )
    .WithParent(mainMenu.transform)
    .AddTo(canvas.transform);

// Version Text
uiManager.CreateText(""VersionText"")
    .WithText(""v1.0.0"")
    .WithFontSize(14)
    .WithColor(new Color(0.5f, 0.5f, 0.5f))
    .WithPosition(new Vector2(0, -240))
    .WithParent(mainMenu.transform)
    .AddTo(canvas.transform);

Debug.Log(""Complete UI created!"");";
    }
}
#endif
