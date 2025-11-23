#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Nimrita.FlowUI.Editor.Playground
{
    /// <summary>
    /// Live UI Builder Playground - Write code and see UI changes INSTANTLY!
    /// Now with hot-reload magic - no Execute button needed in Live Mode!
    /// </summary>
    public class UIBuilderPlaygroundWindow : EditorWindow
    {
        #region Fields

        // Core components
        private LiveExecutionEngine liveEngine;
        private FastPlaygroundCompiler compiler;
        private UIStateTracker stateTracker;

        // UI State
        private string builderCode = "";
        private string previousCode = ""; // Track previous to detect changes
        private Vector2 scrollPosition;
        private Vector2 errorScrollPosition;
        private UIManager targetUIManager;
        private bool showLineNumbers = true;
        private float leftMargin = 40f;

        // Live Mode State
        private bool liveModeEnabled = true; // ON by default!
        private ExecutionStatus currentStatus;
        private string lastErrorMessage = "";

        // Theme
        private Color backgroundColor = new Color(0.15f, 0.15f, 0.18f);
        private Color lineNumberColor = new Color(0.4f, 0.4f, 0.5f, 0.8f);
        private Color lineNumberBgColor = new Color(0.17f, 0.17f, 0.2f);
        private Color errorBgColor = new Color(0.4f, 0.15f, 0.15f, 0.3f);
        private Color successBgColor = new Color(0.15f, 0.4f, 0.15f, 0.3f);
        private Color liveModeColor = new Color(0.2f, 0.8f, 0.3f);

        #endregion

        #region Unity Methods

        [MenuItem("Tools/UI System/UI Builder Playground 🔥", false, 50)]
        public static void ShowWindow()
        {
            UIBuilderPlaygroundWindow window = GetWindow<UIBuilderPlaygroundWindow>("🔥 Live UI Playground");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        private void OnEnable()
        {
            // Initialize core components
            compiler = new FastPlaygroundCompiler();
            stateTracker = new UIStateTracker();
            liveEngine = new LiveExecutionEngine(compiler, stateTracker, OnStatusChanged);

            // Load saved code
            builderCode = EditorPrefs.GetString("UIBuilderPlayground_Code", GetDefaultTemplate());

            // Find UIManager
            if (targetUIManager == null)
            {
                targetUIManager = FindObjectOfType<UIManager>();
            }

            liveEngine.SetTargetUIManager(targetUIManager);

            // Load live mode preference
            liveModeEnabled = EditorPrefs.GetBool("UIBuilderPlayground_LiveMode", true);
            liveEngine.LiveModeEnabled = liveModeEnabled;

            // Subscribe to editor updates
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            // Unsubscribe
            EditorApplication.update -= OnEditorUpdate;

            // Save code
            EditorPrefs.SetString("UIBuilderPlayground_Code", builderCode);
            EditorPrefs.SetBool("UIBuilderPlayground_LiveMode", liveModeEnabled);
        }

        private void OnEditorUpdate()
        {
            // Feed current code to live engine (this runs continuously)
            liveEngine.Update(builderCode);
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawStatusBar();
            DrawCodeEditor();

            if (currentStatus != null && currentStatus.CurrentState == ExecutionStatus.State.Error)
            {
                DrawErrorPanel();
            }
        }

        #endregion

        #region GUI Drawing

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Live Mode Toggle (BIG and PROMINENT!)
            GUI.backgroundColor = liveModeEnabled ? liveModeColor : Color.gray;
            string liveModeText = liveModeEnabled ? "🔥 LIVE MODE: ON" : "⏸ LIVE MODE: OFF";
            if (GUILayout.Button(liveModeText, EditorStyles.toolbarButton, GUILayout.Width(140), GUILayout.Height(20)))
            {
                liveModeEnabled = !liveModeEnabled;
                liveEngine.LiveModeEnabled = liveModeEnabled;
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            // UIManager reference
            GUILayout.Label("UIManager:", GUILayout.Width(75));
            UIManager newManager = (UIManager)EditorGUILayout.ObjectField(
                targetUIManager,
                typeof(UIManager),
                true,
                GUILayout.Width(150)
            );

            if (newManager != targetUIManager)
            {
                targetUIManager = newManager;
                liveEngine.SetTargetUIManager(targetUIManager);
            }

            GUILayout.FlexibleSpace();

            // Line numbers toggle
            showLineNumbers = GUILayout.Toggle(showLineNumbers, "Lines", EditorStyles.toolbarButton, GUILayout.Width(50));

            // Clear button
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                builderCode = "";
                lastErrorMessage = "";
                GUI.FocusControl(null);
            }

            // Templates button
            if (GUILayout.Button("Templates", EditorStyles.toolbarButton, GUILayout.Width(75)))
            {
                ShowTemplateMenu();
            }

            // Clear Cache button
            if (GUILayout.Button("Clear Cache", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                liveEngine.ClearCache();
                Debug.Log("Compilation cache cleared!");
            }

            // Manual Execute button (for when live mode is off)
            if (!liveModeEnabled)
            {
                GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
                if (GUILayout.Button("Execute", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    ExecuteManual();
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatusBar()
        {
            float statusBarHeight = 24;
            Rect statusRect = EditorGUILayout.GetControlRect(false, statusBarHeight);

            // Background
            Color bgColor = Color.black;
            if (currentStatus != null)
            {
                bgColor = currentStatus.CurrentState switch
                {
                    ExecutionStatus.State.Success => new Color(0.15f, 0.3f, 0.15f),
                    ExecutionStatus.State.Error => new Color(0.3f, 0.1f, 0.1f),
                    ExecutionStatus.State.Compiling => new Color(0.2f, 0.2f, 0.3f),
                    _ => new Color(0.2f, 0.2f, 0.2f)
                };
            }

            EditorGUI.DrawRect(statusRect, bgColor);

            // Status text
            string statusText = "Ready";
            Color textColor = Color.gray;

            if (currentStatus != null)
            {
                statusText = currentStatus.Message;
                textColor = currentStatus.CurrentState switch
                {
                    ExecutionStatus.State.Success => new Color(0.5f, 1f, 0.5f),
                    ExecutionStatus.State.Error => new Color(1f, 0.5f, 0.5f),
                    ExecutionStatus.State.Compiling => new Color(0.7f, 0.7f, 1f),
                    _ => Color.white
                };
            }

            GUIStyle statusStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = textColor },
                alignment = TextAnchor.MiddleLeft
            };

            EditorGUI.LabelField(new Rect(statusRect.x + 10, statusRect.y, statusRect.width - 20, statusRect.height), statusText, statusStyle);

            // Show stats on the right
            if (liveModeEnabled)
            {
                var stats = liveEngine.GetStats();
                string statsText = $"Executions: {stats.TotalExecutions} | Success: {stats.SuccessRate:F0}%";

                GUIStyle statsStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                    alignment = TextAnchor.MiddleRight
                };

                EditorGUI.LabelField(new Rect(statusRect.x, statusRect.y, statusRect.width - 10, statusRect.height), statsText, statsStyle);
            }
        }

        private void DrawCodeEditor()
        {
            // Calculate editor area
            float toolbarHeight = 44; // toolbar + status bar
            float errorPanelHeight = (currentStatus?.CurrentState == ExecutionStatus.State.Error) ? 120 : 0;
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

            string newCode = GUI.TextArea(
                new Rect(5, 5, contentWidth - 10, contentHeight),
                builderCode,
                codeStyle
            );

            // Detect code change from typing
            if (newCode != builderCode)
            {
                builderCode = newCode;
            }

            GUI.EndScrollView();

            // Draw line numbers
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
                ExecuteManual();
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

            EditorGUILayout.LabelField("❌ Error", errorHeaderStyle);

            GUIStyle errorStyle = new GUIStyle(EditorStyles.textArea)
            {
                fontSize = 10,
                wordWrap = true,
                normal = { textColor = new Color(1f, 0.8f, 0.8f) }
            };

            errorScrollPosition = EditorGUILayout.BeginScrollView(errorScrollPosition, GUILayout.Height(errorPanelHeight - 35));
            EditorGUILayout.TextArea(lastErrorMessage, errorStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        #endregion

        #region Execution

        private void ExecuteManual()
        {
            if (targetUIManager == null)
            {
                Debug.LogWarning("No UIManager assigned!");
                return;
            }

            liveEngine.ExecuteManual(builderCode, targetUIManager);
        }

        private void OnStatusChanged(ExecutionStatus status)
        {
            currentStatus = status;

            if (status.CurrentState == ExecutionStatus.State.Error)
            {
                lastErrorMessage = status.Message;
            }
            else if (status.CurrentState == ExecutionStatus.State.Success)
            {
                lastErrorMessage = "";
                if (!status.WasCached)
                {
                    Debug.Log($"<color=green>✓ UI Builder executed: {status.Message}</color>");
                }
            }

            Repaint();
        }

        #endregion

        #region Templates

        private void ShowTemplateMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Simple Button"), false, () => LoadTemplate(GetSimpleButtonTemplate()));
            menu.AddItem(new GUIContent("Panel with Buttons"), false, () => LoadTemplate(GetPanelTemplate()));
            menu.AddItem(new GUIContent("Input Form"), false, () => LoadTemplate(GetInputFormTemplate()));
            menu.ShowAsContext();
        }

        private void LoadTemplate(string template)
        {
            builderCode = template;
            lastErrorMessage = "";
            Repaint();
        }

        private string GetDefaultTemplate()
        {
            return @"// 🔥 LIVE MODE - Changes appear INSTANTLY!
// Just type and watch the magic happen...

// Get or create canvas
var canvas = UIBuilderHelpers.EnsureCanvas();

// Example: Create a button
uiManager.CreateButton(""LiveButton"")
    .WithText(""I Update Live! ⚡"")
    .WithSize(250, 60)
    .WithPosition(new Vector2(0, 0))
    .OnClick(() => Debug.Log(""Button clicked!""))
    .AddTo(canvas.transform);

// Try changing the text above and watch it update!
// Delete the button code and watch it vanish!
// Add more UI elements and see them appear!";
        }

        private string GetSimpleButtonTemplate()
        {
            return @"// Simple Button Example
var canvas = UIBuilderHelpers.EnsureCanvas();

uiManager.CreateButton(""MyButton"")
    .WithText(""Click Me!"")
    .WithSize(200, 50)
    .WithPosition(Vector2.zero)
    .OnClick(() => Debug.Log(""Button clicked!""))
    .AddTo(canvas.transform);";
        }

        private string GetPanelTemplate()
        {
            return @"// Panel with Multiple Buttons
var canvas = UIBuilderHelpers.EnsureCanvas();

var panel = uiManager.CreatePanel(""MainPanel"")
    .WithSize(400, 300)
    .WithPosition(Vector2.zero)
    .WithColor(new Color(0.2f, 0.2f, 0.2f, 0.9f))
    .AddTo(canvas.transform);

uiManager.CreateButton(""Button1"")
    .WithText(""Button 1"")
    .WithSize(150, 40)
    .WithPosition(new Vector2(0, 80))
    .WithParent(panel.transform)
    .AddTo(canvas.transform);

uiManager.CreateButton(""Button2"")
    .WithText(""Button 2"")
    .WithSize(150, 40)
    .WithPosition(new Vector2(0, 0))
    .WithParent(panel.transform)
    .AddTo(canvas.transform);";
        }

        private string GetInputFormTemplate()
        {
            return @"// Input Form Example
var canvas = UIBuilderHelpers.EnsureCanvas();

var panel = uiManager.CreatePanel(""FormPanel"")
    .WithSize(400, 250)
    .WithPosition(Vector2.zero)
    .WithColor(new Color(0.15f, 0.15f, 0.2f, 0.95f))
    .AddTo(canvas.transform);

uiManager.CreateText(""TitleText"")
    .WithText(""Login Form"")
    .WithFontSize(24)
    .WithPosition(new Vector2(0, 80))
    .WithParent(panel.transform)
    .AddTo(canvas.transform);

uiManager.CreateInputField(""UsernameInput"")
    .WithPlaceholder(""Username"")
    .WithSize(300, 40)
    .WithPosition(new Vector2(0, 20))
    .WithParent(panel.transform)
    .AddTo(canvas.transform);

uiManager.CreateButton(""LoginButton"")
    .WithText(""Login"")
    .WithSize(150, 40)
    .WithPosition(new Vector2(0, -40))
    .WithParent(panel.transform)
    .OnClick(() => Debug.Log(""Login clicked!""))
    .AddTo(canvas.transform);";
        }

        #endregion
    }
}
#endif
