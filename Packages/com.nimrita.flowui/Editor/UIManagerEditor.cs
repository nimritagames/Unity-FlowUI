#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(UIManager))]
public partial class UIManagerEditor : Editor
{
    #region Private Fields

    private UIManager uiManager;
    private Dictionary<Transform, bool> foldoutStates = new Dictionary<Transform, bool>();
    private List<UIReference> missingReferences = new List<UIReference>();
    private bool isUpdating = false;

    // Library Generation Settings
    private string libraryOutputPath;
    private string libraryNamespace;
    private string libraryClassPrefix;

    // Panel handler settings
    private string panelHandlerOutputPath;
    private string panelHandlerNamespace;
    private string panelHandlerClassPrefix;
    private Vector2 panelScrollPosition;
    private List<UIReference> panels = new List<UIReference>();

    private const string PANEL_HANDLER_OUTPUT_PATH_KEY = "UIManager_PanelHandlerOutputPath";
    private const string PANEL_HANDLER_NAMESPACE_KEY = "UIManager_PanelHandlerNamespace";
    private const string PANEL_HANDLER_CLASS_PREFIX_KEY = "UIManager_PanelHandlerClassPrefix";

    private const string LIBRARY_OUTPUT_PATH_KEY = "UIManager_LibraryOutputPath";
    private const string LIBRARY_NAMESPACE_KEY = "UIManager_LibraryNamespace";
    private const string LIBRARY_CLASS_PREFIX_KEY = "UIManager_LibraryClassPrefix";

    // UI Handler Generation Settings
    private string handlerOutputPath;
    private string handlerNamespace;
    private string handlerClassPrefix;

    private const string HANDLER_OUTPUT_PATH_KEY = "UIManager_HandlerOutputPath";
    private const string HANDLER_NAMESPACE_KEY = "UIManager_HandlerNamespace";
    private const string HANDLER_CLASS_PREFIX_KEY = "UIManager_HandlerClassPrefix";

    // Scroll position for the missing references list
    private Vector2 missingRefsScrollPos;

    // Serialized properties for better handling of UIReferences
    private SerializedProperty uiCategoriesProperty;

    // Variable to track the currently selected missing reference
    private UIReference selectedMissingReference = null;

    // Cache of UI elements already added to the UIManager for quick lookup
    private HashSet<GameObject> addedUIElements = new HashSet<GameObject>();

    // Tab system
    private int selectedTab = 0;
    private string[] tabTitles = new string[] {
        "UI Hierarchy",
        "Library Generation",
        "Handler Generation",
        "Panel Handlers",
        "UI Categories",
        "Missing References",
        "Smart Naming",
        "Tools"
    };

    // Icons for tabs and UI elements
    private Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();

    // Responsive breakpoints
    private const float NARROW_WIDTH_THRESHOLD = 280f;
    private const float MEDIUM_WIDTH_THRESHOLD = 350f;

    #endregion

    #region Unity Methods

    private void OnEnable()
    {
        uiManager = (UIManager)target;
        uiCategoriesProperty = serializedObject.FindProperty("uiCategories");

        // Load library generation settings from EditorPrefs
        libraryOutputPath = EditorPrefs.GetString(LIBRARY_OUTPUT_PATH_KEY, "Assets/Scripts/UI/Generated/");
        libraryNamespace = EditorPrefs.GetString(LIBRARY_NAMESPACE_KEY, "CodeSculptLabs.UIFramework");
        libraryClassPrefix = EditorPrefs.GetString(LIBRARY_CLASS_PREFIX_KEY, "UI_Library_");

        // Load UI Handler generation settings from EditorPrefs
        handlerOutputPath = EditorPrefs.GetString(HANDLER_OUTPUT_PATH_KEY, "Assets/Scripts/UI/Handlers/");
        handlerNamespace = EditorPrefs.GetString(HANDLER_NAMESPACE_KEY, "CodeSculptLabs.UIFramework.Handlers");
        handlerClassPrefix = EditorPrefs.GetString(HANDLER_CLASS_PREFIX_KEY, "");

        // Initialize panel handler settings
        InitializePanelHandlerSettings();

        // Build the cache of added UI elements
        BuildAddedUIElementsCache();

        // Automatically check for missing references on enable
        CheckMissingReferences();

        // Load icons
        LoadIcons();

        // Trigger smart naming check when UIManager inspector is opened
        EditorApplication.delayCall += () => {
            if (uiManager != null) // Check if still valid
            {
                SmartNamingController.ForceNamingCheck();
            }
        };

        ModifiedOnEnable();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawTitle();
        DrawWorkflowSteps();
        DrawTabHeader();
        DrawTabContent();
        DrawProgressIndicator();
        serializedObject.ApplyModifiedProperties();
    }

    #endregion

    #region Responsive Utilities

    /// <summary>
    /// Gets the current responsive mode based on inspector width.
    /// </summary>
    private ResponsiveMode GetResponsiveMode()
    {
        float width = EditorGUIUtility.currentViewWidth;
        if (width < NARROW_WIDTH_THRESHOLD) return ResponsiveMode.Narrow;
        if (width < MEDIUM_WIDTH_THRESHOLD) return ResponsiveMode.Medium;
        return ResponsiveMode.Wide;
    }

    /// <summary>
    /// Gets responsive font size based on current mode and base size.
    /// </summary>
    private int GetResponsiveFontSize(int baseSize, int narrowSize = -1, int mediumSize = -1)
    {
        ResponsiveMode mode = GetResponsiveMode();
        switch (mode)
        {
            case ResponsiveMode.Narrow:
                return narrowSize > 0 ? narrowSize : Mathf.Max(baseSize - 2, 10);
            case ResponsiveMode.Medium:
                return mediumSize > 0 ? mediumSize : Mathf.Max(baseSize - 1, 11);
            default:
                return baseSize;
        }
    }

    /// <summary>
    /// Gets responsive spacing based on current mode.
    /// </summary>
    private float GetResponsiveSpacing(float baseSpacing)
    {
        ResponsiveMode mode = GetResponsiveMode();
        switch (mode)
        {
            case ResponsiveMode.Narrow:
                return baseSpacing * 0.7f;
            case ResponsiveMode.Medium:
                return baseSpacing * 0.85f;
            default:
                return baseSpacing;
        }
    }

    /// <summary>
    /// Gets responsive padding based on current mode.
    /// </summary>
    private float GetResponsivePadding(float basePadding)
    {
        ResponsiveMode mode = GetResponsiveMode();
        switch (mode)
        {
            case ResponsiveMode.Narrow:
                return Mathf.Max(basePadding * 0.6f, 2f);
            case ResponsiveMode.Medium:
                return basePadding * 0.8f;
            default:
                return basePadding;
        }
    }

    #endregion

    #region UI Drawing

    /// <summary>
    /// Draws the title header with responsive behavior.
    /// </summary>
    private void DrawTitle()
    {
        ResponsiveMode mode = GetResponsiveMode();
        float titleHeight = mode == ResponsiveMode.Narrow ? 50f : 60f;

        // Background gradient for title area
        Rect titleRect = EditorGUILayout.GetControlRect(false, titleHeight);
        if (Event.current.type == EventType.Repaint)
        {
            Color topColor = new Color(0.2f, 0.2f, 0.3f);
            Color bottomColor = new Color(0.15f, 0.15f, 0.2f);

            // Draw gradient background
            EditorGUI.DrawRect(new Rect(titleRect.x, titleRect.y, titleRect.width, titleRect.height / 2), topColor);
            EditorGUI.DrawRect(new Rect(titleRect.x, titleRect.y + titleRect.height / 2, titleRect.width, titleRect.height / 2), bottomColor);

            // Draw subtle border
            Color borderColor = new Color(0.3f, 0.3f, 0.4f);
            EditorGUI.DrawRect(new Rect(titleRect.x, titleRect.y + titleRect.height - 1, titleRect.width, 1), borderColor);
        }

        // Responsive title styling
        int titleFontSize = GetResponsiveFontSize(18, 14, 16);
        float titleTopPadding = mode == ResponsiveMode.Narrow ? 4f : 8f;
        float titleHeight_Text = mode == ResponsiveMode.Narrow ? 20f : 30f;

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = titleFontSize,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
        };

        // Header with responsive logo
        EditorGUI.LabelField(new Rect(titleRect.x, titleRect.y + titleTopPadding, titleRect.width, titleHeight_Text), "UI Framework", titleStyle);

        // Logo placement with responsive sizing
        Texture2D logo = GetIcon("UIFrameworkLogo");
        if (logo != null && mode != ResponsiveMode.Narrow)
        {
            float logoSize = mode == ResponsiveMode.Medium ? 20f : 24f;
            float logoOffset = mode == ResponsiveMode.Medium ? -60f : -70f;
            GUI.DrawTexture(new Rect(titleRect.x + titleRect.width / 2 + logoOffset, titleRect.y + titleTopPadding + 3f, logoSize, logoSize), logo);
        }

        // Quick start guide button with responsive styling
        if (mode != ResponsiveMode.Narrow) // Hide in narrow mode to save space
        {
            int linkFontSize = GetResponsiveFontSize(11, 9, 10);
            float linkTopOffset = mode == ResponsiveMode.Medium ? 32f : 38f;

            GUIStyle linkStyle = new GUIStyle(EditorStyles.linkLabel)
            {
                fontSize = linkFontSize,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.85f, 1f) }
            };

            if (GUI.Button(new Rect(titleRect.x, titleRect.y + linkTopOffset, titleRect.width, 18), "Quick Start Guide", linkStyle))
            {
                ShowQuickStartGuide();
            }
        }
    }

    /// <summary>
    /// Draws the workflow steps with responsive behavior.
    /// </summary>
    private void DrawWorkflowSteps()
    {
        float spacing = GetResponsiveSpacing(15f);
        EditorGUILayout.Space(spacing);

        Rect workflowRect = EditorGUILayout.GetControlRect(false, GetWorkflowHeight());
        DrawResponsiveWorkflowSteps(workflowRect);

        EditorGUILayout.Space(GetResponsiveSpacing(10f));
    }

    /// <summary>
    /// Calculates the required height for workflow steps based on available width.
    /// </summary>
    private float GetWorkflowHeight()
    {
        float availableWidth = EditorGUIUtility.currentViewWidth - 40;
        float minStepWidth = 120f;
        float minConnectorWidth = 25f;
        float totalRequiredWidth = (3 * minStepWidth) + (2 * minConnectorWidth);

        if (availableWidth < totalRequiredWidth)
        {
            return 120f; // Height for vertical layout
        }
        else
        {
            return 40f; // Height for horizontal layout
        }
    }

    /// <summary>
    /// Draws workflow steps with responsive behavior.
    /// </summary>
    private void DrawResponsiveWorkflowSteps(Rect workflowRect)
    {
        float availableWidth = workflowRect.width;
        float minStepWidth = 120f;
        float minConnectorWidth = 25f;
        float totalRequiredWidth = (3 * minStepWidth) + (2 * minConnectorWidth);

        bool useVerticalLayout = availableWidth < totalRequiredWidth;

        if (useVerticalLayout)
        {
            DrawVerticalWorkflowSteps(workflowRect);
        }
        else
        {
            DrawHorizontalWorkflowSteps(workflowRect);
        }
    }

    /// <summary>
    /// Draws workflow steps in horizontal layout.
    /// </summary>
    private void DrawHorizontalWorkflowSteps(Rect workflowRect)
    {
        float availableWidth = workflowRect.width;
        float connectorWidth = Mathf.Clamp(availableWidth * 0.08f, 20f, 40f);
        float stepWidth = (availableWidth - (2 * connectorWidth)) / 3f;

        if (stepWidth < 100f)
        {
            stepWidth = 100f;
            connectorWidth = 20f;
        }

        float startX = workflowRect.x + (workflowRect.width - (3 * stepWidth + 2 * connectorWidth)) / 2f;

        var step1Rect = new Rect(startX, workflowRect.y + 4, stepWidth, 32);
        var connector1Rect = new Rect(startX + stepWidth, workflowRect.y + 4, connectorWidth, 32);
        var step2Rect = new Rect(startX + stepWidth + connectorWidth, workflowRect.y + 4, stepWidth, 32);
        var connector2Rect = new Rect(startX + 2 * stepWidth + connectorWidth, workflowRect.y + 4, connectorWidth, 32);
        var step3Rect = new Rect(startX + 2 * stepWidth + 2 * connectorWidth, workflowRect.y + 4, stepWidth, 32);

        DrawWorkflowStep(step1Rect, 1, "Scan UI", addedUIElements.Count > 0);
        DrawWorkflowConnector(connector1Rect);
        DrawWorkflowStep(step2Rect, 2, "Generate Library", IsLibraryGenerated(uiManager.gameObject.scene.name));
        DrawWorkflowConnector(connector2Rect);
        DrawWorkflowStep(step3Rect, 3, "Create Handlers", HandlerFileExists());
    }

    /// <summary>
    /// Draws workflow steps in vertical layout.
    /// </summary>
    private void DrawVerticalWorkflowSteps(Rect workflowRect)
    {
        float stepHeight = 30f;
        float stepSpacing = 8f;
        float stepWidth = workflowRect.width - 20f;
        float startX = workflowRect.x + 10f;

        for (int i = 0; i < 3; i++)
        {
            float currentY = workflowRect.y + (i * (stepHeight + stepSpacing));
            Rect stepRect = new Rect(startX, currentY, stepWidth, stepHeight);

            switch (i)
            {
                case 0:
                    DrawVerticalWorkflowStep(stepRect, 1, "Scan UI", addedUIElements.Count > 0);
                    break;
                case 1:
                    DrawVerticalWorkflowStep(stepRect, 2, "Generate Library", IsLibraryGenerated(uiManager.gameObject.scene.name));
                    break;
                case 2:
                    DrawVerticalWorkflowStep(stepRect, 3, "Create Handlers", HandlerFileExists());
                    break;
            }

            if (i < 2)
            {
                DrawVerticalWorkflowConnector(new Rect(
                    startX + stepWidth / 2 - 8f,
                    currentY + stepHeight + 1f,
                    16f,
                    stepSpacing - 2f
                ));
            }
        }
    }

    /// <summary>
    /// Draws a workflow step optimized for vertical layout.
    /// </summary>
    private void DrawVerticalWorkflowStep(Rect position, int number, string label, bool completed)
    {
        bool isCurrentTab = GetCurrentTabFromStepNumber(number);
        Color bgColor, textColor, borderColor;
        GetStepColors(completed, isCurrentTab, out bgColor, out textColor, out borderColor);

        if (Event.current.type == EventType.Repaint)
        {
            DrawStepBackground(position, bgColor, borderColor);
        }

        float circleSize = 16f;
        Rect circleRect = new Rect(
            position.x + 8f,
            position.y + (position.height - circleSize) / 2f,
            circleSize,
            circleSize
        );

        DrawStepCircle(circleRect, number.ToString(), borderColor, textColor);

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = GetResponsiveFontSize(11, 10, 10),
            normal = { textColor = textColor }
        };

        Rect labelRect = new Rect(
            position.x + circleSize + 12f,
            position.y,
            position.width - circleSize - 16f,
            position.height
        );

        EditorGUI.LabelField(labelRect, label, labelStyle);
        HandleStepClick(position, number);
    }

    /// <summary>
    /// Draws a vertical connector between workflow steps.
    /// </summary>
    private void DrawVerticalWorkflowConnector(Rect position)
    {
        if (Event.current.type == EventType.Repaint)
        {
            Color lineColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            float lineX = position.x + position.width / 2f;

            EditorGUI.DrawRect(
                new Rect(lineX - 0.5f, position.y, 1f, position.height - 6f),
                lineColor
            );

            Vector3[] arrowPoints = new Vector3[]
            {
                new Vector3(lineX - 3f, position.y + position.height - 8f),
                new Vector3(lineX, position.y + position.height - 2f),
                new Vector3(lineX + 3f, position.y + position.height - 8f)
            };

            Handles.color = lineColor;
            Handles.DrawAAConvexPolygon(arrowPoints);
        }
    }

    /// <summary>
    /// Gets the appropriate colors for a workflow step.
    /// </summary>
    private void GetStepColors(bool completed, bool isCurrentTab, out Color bgColor, out Color textColor, out Color borderColor)
    {
        if (completed)
        {
            bgColor = new Color(0.2f, 0.5f, 0.2f, 0.8f);
            borderColor = new Color(0.3f, 0.7f, 0.3f, 0.9f);
            textColor = new Color(0.8f, 1f, 0.8f);
        }
        else if (isCurrentTab)
        {
            bgColor = new Color(0.2f, 0.4f, 0.7f, 0.8f);
            borderColor = new Color(0.4f, 0.6f, 0.9f, 0.9f);
            textColor = new Color(0.8f, 0.9f, 1f);
        }
        else
        {
            bgColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
            borderColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            textColor = new Color(0.8f, 0.8f, 0.8f);
        }
    }

    /// <summary>
    /// Draws the background for a workflow step.
    /// </summary>
    private void DrawStepBackground(Rect position, Color bgColor, Color borderColor)
    {
        EditorGUI.DrawRect(position, bgColor);

        EditorGUI.DrawRect(
            new Rect(position.x, position.y, position.width, 1),
            new Color(borderColor.r + 0.1f, borderColor.g + 0.1f, borderColor.b + 0.1f, 0.8f)
        );

        EditorGUI.DrawRect(
            new Rect(position.x, position.y + position.height - 1, position.width, 1),
            new Color(bgColor.r - 0.1f, bgColor.g - 0.1f, bgColor.b - 0.1f, 0.8f)
        );

        EditorGUI.DrawRect(new Rect(position.x, position.y, 1, position.height), borderColor);
        EditorGUI.DrawRect(new Rect(position.x + position.width - 1, position.y, 1, position.height), borderColor);
    }

    /// <summary>
    /// Draws the numbered circle for a workflow step.
    /// </summary>
    private void DrawStepCircle(Rect circleRect, string number, Color borderColor, Color textColor)
    {
        if (Event.current.type == EventType.Repaint)
        {
            Color circleColor = new Color(borderColor.r, borderColor.g, borderColor.b, 0.9f);
            EditorGUI.DrawRect(circleRect, circleColor);
        }

        GUIStyle numberStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = GetResponsiveFontSize(10, 9, 9),
            normal = { textColor = textColor }
        };

        EditorGUI.LabelField(circleRect, number, numberStyle);
    }

    /// <summary>
    /// Gets the current tab based on step number.
    /// </summary>
    private bool GetCurrentTabFromStepNumber(int stepNumber)
    {
        switch (stepNumber)
        {
            case 1: return selectedTab == 0;
            case 2: return selectedTab == 1;
            case 3: return selectedTab == 2;
            default: return false;
        }
    }

    /// <summary>
    /// Handles click events for workflow steps.
    /// </summary>
    private void HandleStepClick(Rect position, int stepNumber)
    {
        if (Event.current.type == EventType.MouseDown && position.Contains(Event.current.mousePosition))
        {
            switch (stepNumber)
            {
                case 1: selectedTab = 0; break;
                case 2: selectedTab = 1; break;
                case 3: selectedTab = 2; break;
            }
            Event.current.Use();
            Repaint();
        }
    }

    /// <summary>
    /// Draws a single workflow step button.
    /// </summary>
    private void DrawWorkflowStep(Rect position, int number, string label, bool completed)
    {
        bool isCurrentTab = GetCurrentTabFromStepNumber(number);
        Color bgColor, textColor, borderColor;
        GetStepColors(completed, isCurrentTab, out bgColor, out textColor, out borderColor);

        if (Event.current.type == EventType.Repaint)
        {
            DrawStepBackground(position, bgColor, borderColor);
        }

        float circleSize = 18;
        Rect circleRect = new Rect(
            position.x + 10,
            position.y + (position.height - circleSize) / 2,
            circleSize,
            circleSize
        );

        DrawStepCircle(circleRect, number.ToString(), borderColor, textColor);

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = GetResponsiveFontSize(11, 10, 10),
            normal = { textColor = textColor }
        };

        EditorGUI.LabelField(
            new Rect(position.x + circleSize + 15, position.y, position.width - circleSize - 20, position.height),
            label,
            labelStyle
        );

        HandleStepClick(position, number);
    }

    /// <summary>
    /// Draws a connector between workflow steps.
    /// </summary>
    private void DrawWorkflowConnector(Rect position)
    {
        if (Event.current.type == EventType.Repaint)
        {
            Color lineColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            float lineY = position.y + position.height / 2;

            EditorGUI.DrawRect(
                new Rect(position.x + 2, lineY, position.width - 4, 1),
                lineColor
            );

            Vector3[] arrowPoints = new Vector3[]
            {
                new Vector3(position.x + position.width - 8, lineY - 3),
                new Vector3(position.x + position.width - 2, lineY),
                new Vector3(position.x + position.width - 8, lineY + 3)
            };

            Handles.color = lineColor;
            Handles.DrawAAConvexPolygon(arrowPoints);
        }
    }

    /// <summary>
    /// Checks if the handler file exists for the current scene.
    /// </summary>
    private bool HandlerFileExists()
    {
        string sceneName = uiManager.gameObject.scene.name;
        string sanitizedSceneName = SanitizeIdentifier(sceneName);
        string generatedFilePath = System.IO.Path.Combine(handlerOutputPath, $"{handlerClassPrefix}{sanitizedSceneName}UIHandler.g.cs");
        string userFilePath = System.IO.Path.Combine(handlerOutputPath, $"{handlerClassPrefix}{sanitizedSceneName}UIHandler.cs");
        return System.IO.File.Exists(generatedFilePath) || System.IO.File.Exists(userFilePath);
    }

    /// <summary>
    /// Shows the quick start guide window.
    /// </summary>
    private void ShowQuickStartGuide()
    {
        UIManagerQuickStartWindow.ShowWindow(uiManager);
    }

    /// <summary>
    /// Draws the tab header with enhanced responsive behavior.
    /// </summary>
    private void DrawTabHeader()
    {
        float spacing = GetResponsiveSpacing(5f);
        EditorGUILayout.Space(spacing);

        ResponsiveMode mode = GetResponsiveMode();
        float tabHeight = mode == ResponsiveMode.Narrow ? 28f : 32f;

        Rect tabRect = EditorGUILayout.GetControlRect(false, tabHeight);

        // Enhanced responsive logic for tab display
        float minTextWidth = mode == ResponsiveMode.Narrow ? 50f : 70f;
        float totalTabWidth = tabRect.width;
        float singleTabWidth = totalTabWidth / tabTitles.Length;

        // Determine if we should show text labels
        bool showTextLabels = singleTabWidth >= minTextWidth;

        // In very narrow mode, we might want to use a different approach
        if (mode == ResponsiveMode.Narrow && !showTextLabels)
        {
            // Consider showing only active tab with text, others icon-only
            DrawCompactTabHeader(tabRect);
            return;
        }

        for (int i = 0; i < tabTitles.Length; i++)
        {
            Rect singleTabRect = new Rect(tabRect.x + i * singleTabWidth, tabRect.y, singleTabWidth, tabRect.height);
            bool isSelected = selectedTab == i;
            bool isHovered = singleTabRect.Contains(Event.current.mousePosition);

            DrawStylizedTab(singleTabRect, i, isSelected, isHovered, showTextLabels);
        }

        DrawContentSeparator(tabRect);
        EditorGUILayout.BeginVertical();
    }

    /// <summary>
    /// Draws a compact tab header for very narrow inspectors.
    /// </summary>
    private void DrawCompactTabHeader(Rect tabRect)
    {
        // Show current tab with text, others as dots or arrows
        float buttonWidth = 24f;
        float centerWidth = tabRect.width - (2 * buttonWidth + 20f);

        // Previous button
        Rect prevRect = new Rect(tabRect.x + 5f, tabRect.y + 2f, buttonWidth, tabRect.height - 4f);
        if (DrawTabNavigationButton(prevRect, "◀", selectedTab > 0))
        {
            selectedTab = Mathf.Max(0, selectedTab - 1);
            Repaint();
        }

        // Current tab display
        Rect currentRect = new Rect(tabRect.x + buttonWidth + 10f, tabRect.y, centerWidth, tabRect.height);
        DrawStylizedTab(currentRect, selectedTab, true, false, true);

        // Next button
        Rect nextRect = new Rect(tabRect.x + tabRect.width - buttonWidth - 5f, tabRect.y + 2f, buttonWidth, tabRect.height - 4f);
        if (DrawTabNavigationButton(nextRect, "▶", selectedTab < tabTitles.Length - 1))
        {
            selectedTab = Mathf.Min(tabTitles.Length - 1, selectedTab + 1);
            Repaint();
        }

        DrawContentSeparator(tabRect);
        EditorGUILayout.BeginVertical();
    }

    /// <summary>
    /// Draws a tab navigation button.
    /// </summary>
    private bool DrawTabNavigationButton(Rect rect, string symbol, bool enabled)
    {
        Color bgColor = enabled ? new Color(0.3f, 0.3f, 0.35f) : new Color(0.2f, 0.2f, 0.25f);
        Color textColor = enabled ? new Color(0.8f, 0.8f, 0.85f) : new Color(0.5f, 0.5f, 0.55f);

        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(rect, bgColor);
        }

        GUIStyle buttonStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = GetResponsiveFontSize(12, 10, 11),
            normal = { textColor = textColor }
        };

        EditorGUI.LabelField(rect, symbol, buttonStyle);

        if (enabled && Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            Event.current.Use();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Draws a beautifully styled individual tab with responsive behavior.
    /// </summary>
    private void DrawStylizedTab(Rect tabRect, int tabIndex, bool isSelected, bool isHovered, bool showText)
    {
        ResponsiveMode mode = GetResponsiveMode();

        if (Event.current.type == EventType.Repaint)
        {
            Color baseColor, topHighlight, bottomShadow;

            if (isSelected)
            {
                baseColor = new Color(0.28f, 0.35f, 0.45f, 1f);
                topHighlight = new Color(0.35f, 0.42f, 0.55f, 1f);
                bottomShadow = new Color(0.22f, 0.28f, 0.38f, 1f);
            }
            else if (isHovered)
            {
                baseColor = new Color(0.22f, 0.25f, 0.32f, 1f);
                topHighlight = new Color(0.28f, 0.32f, 0.40f, 1f);
                bottomShadow = new Color(0.18f, 0.20f, 0.26f, 1f);
            }
            else
            {
                baseColor = new Color(0.18f, 0.18f, 0.22f, 1f);
                topHighlight = new Color(0.22f, 0.22f, 0.28f, 1f);
                bottomShadow = new Color(0.15f, 0.15f, 0.18f, 1f);
            }

            EditorGUI.DrawRect(tabRect, baseColor);
            EditorGUI.DrawRect(new Rect(tabRect.x, tabRect.y, tabRect.width, 1), topHighlight);
            EditorGUI.DrawRect(new Rect(tabRect.x, tabRect.y + tabRect.height - 1, tabRect.width, 1), bottomShadow);

            if (isSelected)
            {
                Color accentColor = new Color(0.4f, 0.7f, 1f, 0.9f);
                float accentHeight = mode == ResponsiveMode.Narrow ? 2f : 3f;
                EditorGUI.DrawRect(
                    new Rect(tabRect.x, tabRect.y + tabRect.height - accentHeight, tabRect.width, accentHeight),
                    accentColor
                );

                if (mode != ResponsiveMode.Narrow)
                {
                    Color glowColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.3f);
                    EditorGUI.DrawRect(
                        new Rect(tabRect.x, tabRect.y + tabRect.height - 5, tabRect.width, 2),
                        glowColor
                    );
                }
            }

            if (tabIndex < tabTitles.Length - 1)
            {
                Color separatorColor = new Color(0.12f, 0.12f, 0.15f, 0.8f);
                float separatorMargin = mode == ResponsiveMode.Narrow ? 4f : 6f;
                EditorGUI.DrawRect(
                    new Rect(tabRect.x + tabRect.width - 1, tabRect.y + separatorMargin, 1, tabRect.height - 2 * separatorMargin),
                    separatorColor
                );
            }
        }

        DrawTabContent(tabRect, tabIndex, isSelected, showText);
        HandleTabInteraction(tabRect, tabIndex, showText);
    }

    /// <summary>
    /// Draws the tab content with responsive icon and text rendering.
    /// </summary>
    private void DrawTabContent(Rect tabRect, int tabIndex, bool isSelected, bool showText)
    {
        ResponsiveMode mode = GetResponsiveMode();

        Color contentColor = isSelected ?
            new Color(0.95f, 0.98f, 1f, 1f) :
            new Color(0.75f, 0.78f, 0.85f, 1f);

        Texture2D tabIcon = GetTabIcon(tabIndex);
        float padding = GetResponsivePadding(8f);
        float contentStartX = tabRect.x + padding;
        float availableWidth = tabRect.width - 2 * padding;

        if (showText)
        {
            // Icon + Text mode
            if (tabIcon != null)
            {
                float iconSize = mode == ResponsiveMode.Narrow ? 12f : 16f;
                Rect iconRect = new Rect(
                    contentStartX,
                    tabRect.y + (tabRect.height - iconSize) / 2,
                    iconSize, iconSize
                );

                if (Event.current.type == EventType.Repaint)
                {
                    Color shadowColor = new Color(0, 0, 0, 0.3f);
                    GUI.DrawTexture(
                        new Rect(iconRect.x + 1, iconRect.y + 1, iconRect.width, iconRect.height),
                        tabIcon, ScaleMode.ScaleToFit, true, 0, shadowColor, 0, 0
                    );

                    GUI.DrawTexture(iconRect, tabIcon, ScaleMode.ScaleToFit, true, 0, contentColor, 0, 0);
                }

                float iconSpacing = mode == ResponsiveMode.Narrow ? 4f : 6f;
                contentStartX += iconSize + iconSpacing;
                availableWidth -= iconSize + iconSpacing;
            }

            // Responsive text rendering
            string displayText = GetDisplayText(tabTitles[tabIndex], availableWidth);
            int fontSize = GetResponsiveFontSize(11, 9, 10);

            GUIStyle textStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = contentColor },
                fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal
            };

            EditorGUI.LabelField(
                new Rect(contentStartX, tabRect.y, availableWidth, tabRect.height),
                displayText, textStyle
            );
        }
        else
        {
            // Icon-only mode
            if (tabIcon != null)
            {
                float iconSize = mode == ResponsiveMode.Narrow ? 14f : 18f;
                Rect iconRect = new Rect(
                    tabRect.x + (tabRect.width - iconSize) / 2,
                    tabRect.y + (tabRect.height - iconSize) / 2,
                    iconSize, iconSize
                );

                if (Event.current.type == EventType.Repaint)
                {
                    Color shadowColor = new Color(0, 0, 0, 0.4f);
                    GUI.DrawTexture(
                        new Rect(iconRect.x + 1, iconRect.y + 1, iconRect.width, iconRect.height),
                        tabIcon, ScaleMode.ScaleToFit, true, 0, shadowColor, 0, 0
                    );

                    GUI.DrawTexture(iconRect, tabIcon, ScaleMode.ScaleToFit, true, 0, contentColor, 0, 0);
                }
            }
        }
    }

    /// <summary>
    /// Smart text truncation with enhanced responsive behavior.
    /// </summary>
    private string GetDisplayText(string originalText, float maxWidth)
    {
        if (string.IsNullOrEmpty(originalText)) return originalText;

        ResponsiveMode mode = GetResponsiveMode();
        int fontSize = GetResponsiveFontSize(11, 9, 10);
        GUIStyle measureStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = fontSize };

        if (measureStyle.CalcSize(new GUIContent(originalText)).x <= maxWidth)
            return originalText;

        // Apply more aggressive abbreviations in narrow mode
        string smartText = mode == ResponsiveMode.Narrow ?
            ApplyAggressiveAbbreviations(originalText) :
            ApplySmartAbbreviations(originalText);

        if (measureStyle.CalcSize(new GUIContent(smartText)).x <= maxWidth)
            return smartText;

        // Truncation with ellipsis
        for (int i = originalText.Length - 1; i > 2; i--)
        {
            string truncated = originalText.Substring(0, i) + "…";
            if (measureStyle.CalcSize(new GUIContent(truncated)).x <= maxWidth)
                return truncated;
        }

        return "…";
    }

    /// <summary>
    /// Applies smart abbreviations for better readability.
    /// </summary>
    private string ApplySmartAbbreviations(string text)
    {
        return text
            .Replace("Generation", "Gen")
            .Replace("References", "Refs")
            .Replace("Categories", "Cat")
            .Replace("Handler", "Hand")
            .Replace("Settings", "Setup");
    }

    /// <summary>
    /// Applies more aggressive abbreviations for narrow mode.
    /// </summary>
    private string ApplyAggressiveAbbreviations(string text)
    {
        return text
            .Replace("UI Hierarchy", "Hierarchy")
            .Replace("Library Generation", "Library")
            .Replace("Handler Generation", "Handlers")
            .Replace("Panel Handlers", "Panels")
            .Replace("UI Categories", "Categories")
            .Replace("Missing References", "Missing")
            .Replace("Tools", "Settings");
    }

    /// <summary>
    /// Handles tab interactions with responsive feedback.
    /// </summary>
    private void HandleTabInteraction(Rect tabRect, int tabIndex, bool showText)
    {
        if (Event.current.type == EventType.MouseDown && tabRect.Contains(Event.current.mousePosition))
        {
            if (selectedTab != tabIndex)
            {
                selectedTab = tabIndex;
                GUI.changed = true;
                Event.current.Use();
                EditorApplication.delayCall += () => Repaint();
            }
        }
    }

    /// <summary>
    /// Draws an elegant content separator with responsive sizing.
    /// </summary>
    private void DrawContentSeparator(Rect tabRect)
    {
        if (Event.current.type == EventType.Repaint)
        {
            Color leftColor = new Color(0.15f, 0.18f, 0.22f, 0f);
            Color centerColor = new Color(0.3f, 0.35f, 0.4f, 1f);
            Color rightColor = new Color(0.15f, 0.18f, 0.22f, 0f);

            Rect separatorRect = new Rect(
                tabRect.x,
                tabRect.y + tabRect.height,
                tabRect.width,
                1
            );

            float third = separatorRect.width / 3f;

            EditorGUI.DrawRect(new Rect(separatorRect.x, separatorRect.y, third, 1),
                Color.Lerp(leftColor, centerColor, 1f));
            EditorGUI.DrawRect(new Rect(separatorRect.x + third, separatorRect.y, third, 1),
                centerColor);
            EditorGUI.DrawRect(new Rect(separatorRect.x + 2 * third, separatorRect.y, third, 1),
                Color.Lerp(centerColor, rightColor, 1f));
        }
    }

    /// <summary>
    /// Draws the content for the currently selected tab.
    /// </summary>
    private void DrawTabContent()
    {
        float spacing = GetResponsiveSpacing(8f);
        EditorGUILayout.Space(spacing);

        switch (selectedTab)
        {
            case 0: DrawHierarchySection(); break;
            case 1: DrawLibrarySettings(); break;
            case 2: DrawHandlerSettings(); break;
            case 3: DrawPanelHandlerSettings(); break;
            case 4: DrawCategoriesPanel(); break;
            case 5: DrawMissingReferencesSection(); break;
            case 6: DrawSmartNamingSection(); break;
            case 7: DrawUIManagerSettings(); break;
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Get an icon for a specific tab.
    /// </summary>
    private Texture2D GetTabIcon(int tabIndex)
    {
        switch (tabIndex)
        {
            case 0: return EditorGUIUtility.IconContent("d_UnityEditor.SceneHierarchyWindow").image as Texture2D;
            case 1: return EditorGUIUtility.IconContent("d_cs Script Icon").image as Texture2D;
            case 2: return EditorGUIUtility.IconContent("d_SceneViewTools").image as Texture2D;
            case 3: return EditorGUIUtility.IconContent("d_Canvas Icon").image as Texture2D;
            case 4: return EditorGUIUtility.IconContent("d_FilterByType").image as Texture2D;
            case 5: return EditorGUIUtility.IconContent("d_console.warnicon").image as Texture2D;
            case 6: return EditorGUIUtility.IconContent("d_TextScriptImporter Icon").image as Texture2D; // Smart naming icon
            case 7: return EditorGUIUtility.IconContent("d_SettingsIcon").image as Texture2D;
            default: return null;
        }
    }

    /// <summary>
    /// Shows a quick tip to the user.
    /// </summary>
    private void ShowQuickTip(string title, string message)
    {
        UIManagerTipWindow.ShowWindow(title, message);
    }

    /// <summary>
    /// Loads icons for the UI.
    /// </summary>
    private void LoadIcons()
    {
        string[] iconNames = {
            "UIFrameworkLogo", "button_icon", "text_icon", "image_icon",
            "toggle_icon", "slider_icon", "dropdown_icon", "panel_icon", "input_icon"
        };

        foreach (var iconName in iconNames)
        {
            Texture2D icon = Resources.Load<Texture2D>($"UIIcons/{iconName}");
            if (icon != null)
            {
                iconCache[iconName] = icon;
            }
        }
    }

    /// <summary>
    /// Gets an icon from the cache.
    /// </summary>
    private Texture2D GetIcon(string iconName)
    {
        if (iconCache.TryGetValue(iconName, out Texture2D icon))
        {
            return icon;
        }

        icon = Resources.Load<Texture2D>($"UIIcons/{iconName}");
        if (icon != null)
        {
            iconCache[iconName] = icon;
            return icon;
        }

        return null;
    }

    /// <summary>
    /// Gets the icon for a UI element type.
    /// </summary>
    private Texture2D GetTypeIcon(GameObject gameObject)
    {
        if (gameObject == null) return null;

        if (gameObject.GetComponent<Button>()) return GetIcon("button_icon");
        if (gameObject.GetComponent<Text>() || gameObject.GetComponent<TMP_Text>()) return GetIcon("text_icon");
        if (gameObject.GetComponent<Toggle>()) return GetIcon("toggle_icon");
        if (gameObject.GetComponent<Slider>()) return GetIcon("slider_icon");
        if (gameObject.GetComponent<Dropdown>() || gameObject.GetComponent<TMP_Dropdown>()) return GetIcon("dropdown_icon");
        if (gameObject.GetComponent<InputField>() || gameObject.GetComponent<TMP_InputField>()) return GetIcon("input_icon");
        if (gameObject.GetComponent<Image>())
        {
            if (gameObject.name.EndsWith("_Panel", StringComparison.OrdinalIgnoreCase))
                return GetIcon("panel_icon");
            return GetIcon("image_icon");
        }

        return null;
    }

    /// <summary>
    /// Checks if a GameObject has supported UI components.
    /// </summary>
    private bool HasSupportedUIComponent(GameObject gameObject)
    {
        if (gameObject == null) return false;

        return gameObject.GetComponent<Button>() != null ||
               gameObject.GetComponent<Text>() != null ||
               gameObject.GetComponent<TMP_Text>() != null ||
               gameObject.GetComponent<Toggle>() != null ||
               gameObject.GetComponent<Slider>() != null ||
               gameObject.GetComponent<Dropdown>() != null ||
               gameObject.GetComponent<TMP_Dropdown>() != null ||
               gameObject.GetComponent<InputField>() != null ||
               gameObject.GetComponent<TMP_InputField>() != null ||
               gameObject.GetComponent<Image>() != null ||
               gameObject.GetComponent<RawImage>() != null;
    }

    /// <summary>
    /// Draws a section header with responsive styling.
    /// </summary>
    private void DrawSectionHeader(string title, string description = null)
    {
        ResponsiveMode mode = GetResponsiveMode();

        // Responsive spacing before header
        float topSpacing = mode switch
        {
            ResponsiveMode.Narrow => 6f,
            ResponsiveMode.Medium => 8f,
            _ => 10f
        };
        EditorGUILayout.Space(topSpacing);

        // Calculate available width with responsive margins
        float sideMargin = mode switch
        {
            ResponsiveMode.Narrow => 8f,
            ResponsiveMode.Medium => 12f,
            _ => 16f
        };
        float availableWidth = EditorGUIUtility.currentViewWidth - (sideMargin * 2);

        // Calculate responsive heights and sizing
        float baseHeight = mode switch
        {
            ResponsiveMode.Narrow => 24f,
            ResponsiveMode.Medium => 28f,
            _ => 32f
        };

        // Calculate description height if present
        float descriptionHeight = 0;
        GUIStyle descCalcStyle = null;
        if (!string.IsNullOrEmpty(description))
        {
            int descFontSize = GetResponsiveFontSize(10, 9, 8);
            descCalcStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = descFontSize,
                wordWrap = true,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };

            // Account for text padding in width calculation
            float textPadding = GetResponsivePadding(12f);
            float textWidth = availableWidth - (textPadding * 2);
            descriptionHeight = descCalcStyle.CalcHeight(new GUIContent(description), textWidth);

            // Add spacing between title and description
            descriptionHeight += mode == ResponsiveMode.Narrow ? 2f : 4f;
        }

        float totalHeight = baseHeight + descriptionHeight;

        // Add responsive bottom padding to header
        float bottomPadding = mode switch
        {
            ResponsiveMode.Narrow => 6f,
            ResponsiveMode.Medium => 8f,
            _ => 12f
        };
        totalHeight += bottomPadding;

        // Get the header rectangle
        Rect headerRect = EditorGUILayout.GetControlRect(false, totalHeight);

        // Draw responsive background with subtle gradient
        if (Event.current.type == EventType.Repaint)
        {
            // Responsive color scheme - slightly different for each mode
            Color baseColor = mode switch
            {
                ResponsiveMode.Narrow => new Color(0.22f, 0.22f, 0.26f),
                ResponsiveMode.Medium => new Color(0.24f, 0.24f, 0.28f),
                _ => new Color(0.25f, 0.25f, 0.3f)
            };

            Color accentColor = mode switch
            {
                ResponsiveMode.Narrow => new Color(0.2f, 0.4f, 0.7f),
                ResponsiveMode.Medium => new Color(0.25f, 0.45f, 0.75f),
                _ => new Color(0.3f, 0.5f, 0.8f)
            };

            // Draw main background
            EditorGUI.DrawRect(headerRect, baseColor);

            // Draw responsive top border
            float borderHeight = mode == ResponsiveMode.Narrow ? 1f : 1.5f;
            EditorGUI.DrawRect(
                new Rect(headerRect.x, headerRect.y, headerRect.width, borderHeight),
                new Color(0.3f, 0.3f, 0.35f, 0.8f)
            );

            // Draw responsive accent bar
            float accentWidth = mode switch
            {
                ResponsiveMode.Narrow => 2f,
                ResponsiveMode.Medium => 3f,
                _ => 4f
            };
            EditorGUI.DrawRect(
                new Rect(headerRect.x, headerRect.y, accentWidth, headerRect.height),
                accentColor
            );

            // Add subtle highlight on wide screens
            if (mode == ResponsiveMode.Wide)
            {
                EditorGUI.DrawRect(
                    new Rect(headerRect.x + accentWidth, headerRect.y, 1f, headerRect.height),
                    new Color(1f, 1f, 1f, 0.1f)
                );
            }
        }

        // Responsive title styling and positioning
        int titleFontSize = GetResponsiveFontSize(14, 12, 11);
        float titlePadding = GetResponsivePadding(12f);
        float titleTopOffset = mode switch
        {
            ResponsiveMode.Narrow => 3f,
            ResponsiveMode.Medium => 5f,
            _ => 7f
        };

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = titleFontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.95f, 0.95f, 0.98f) },
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0)
        };

        // Add text shadow effect for wide screens
        if (mode == ResponsiveMode.Wide)
        {
            EditorGUI.LabelField(
                new Rect(headerRect.x + titlePadding + 1, headerRect.y + titleTopOffset + 1,
                    headerRect.width - (titlePadding * 2), 20),
                title,
                new GUIStyle(titleStyle) { normal = { textColor = new Color(0, 0, 0, 0.3f) } }
            );
        }

        // Draw main title
        EditorGUI.LabelField(
            new Rect(headerRect.x + titlePadding, headerRect.y + titleTopOffset,
                headerRect.width - (titlePadding * 2), 20),
            title, titleStyle
        );

        // Draw responsive description
        if (!string.IsNullOrEmpty(description) && descCalcStyle != null)
        {
            int descFontSize = GetResponsiveFontSize(10, 9, 8);
            float descTopOffset = baseHeight - bottomPadding + (mode == ResponsiveMode.Narrow ? 2f : 4f);
            float descPadding = GetResponsivePadding(12f);

            GUIStyle descStyle = new GUIStyle(descCalcStyle)
            {
                fontSize = descFontSize,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.75f, 0.75f, 0.8f) },
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };

            // Ensure text doesn't overflow
            float descWidth = headerRect.width - (descPadding * 2);
            float descHeight = descriptionHeight - (mode == ResponsiveMode.Narrow ? 2f : 4f);

            EditorGUI.LabelField(
                new Rect(headerRect.x + descPadding, headerRect.y + descTopOffset,
                    descWidth, descHeight),
                description, descStyle
            );
        }

        // Responsive spacing after header
        float bottomSpacing = mode switch
        {
            ResponsiveMode.Narrow => 4f,
            ResponsiveMode.Medium => 6f,
            _ => 8f
        };
        EditorGUILayout.Space(bottomSpacing);
    }

    /// <summary>
    /// Draws responsive action buttons that stack vertically in narrow mode.
    /// </summary>
    private void DrawResponsiveActionButtons(params ResponsiveButton[] buttons)
    {
        ResponsiveMode mode = GetResponsiveMode();
        float spacing = GetResponsiveSpacing(6f);
        float buttonHeight = mode == ResponsiveMode.Narrow ? 26f : 30f;

        if (mode == ResponsiveMode.Narrow)
        {
            // Stack buttons vertically in narrow mode
            foreach (var button in buttons)
            {
                Rect buttonRect = EditorGUILayout.GetControlRect(false, buttonHeight);
                float padding = GetResponsivePadding(5f);
                buttonRect.x += padding;
                buttonRect.width -= 2 * padding;

                DrawActionButton(buttonRect, button.Label, button.Tooltip, button.OnClick, button.IsPrimary);
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttonRect.x += padding;
                    buttonRect.width -= 2 * padding;

                    DrawActionButton(buttonRect, button.Label, button.Tooltip, button.OnClick, button.IsPrimary);

                    if (i < buttons.Length - 1)
                    {
                        EditorGUILayout.Space(spacing);
                    }
                }

            }
        }
        else
        {
            // Arrange buttons horizontally
            Rect containerRect = EditorGUILayout.GetControlRect(false, buttonHeight);
            float totalSpacing = spacing * (buttons.Length - 1);
            float buttonWidth = (containerRect.width - totalSpacing) / buttons.Length;

            for (int i = 0; i < buttons.Length; i++)
            {
                float buttonX = containerRect.x + i * (buttonWidth + spacing);
                Rect buttonRect = new Rect(buttonX, containerRect.y, buttonWidth, buttonHeight);
                DrawActionButton(buttonRect, buttons[i].Label, buttons[i].Tooltip, buttons[i].OnClick, buttons[i].IsPrimary);
            }
        }
    }

    /// <summary>
    /// Draws a button with responsive styling.
    /// </summary>
    private void DrawActionButton(Rect position, string label, string tooltip, System.Action onClick, bool isPrimary = false)
    {
        ResponsiveMode mode = GetResponsiveMode();

        Color bgColor = isPrimary ?
            new Color(0.2f, 0.4f, 0.7f) :
            new Color(0.3f, 0.3f, 0.35f);

        Color hoverColor = isPrimary ?
            new Color(0.3f, 0.5f, 0.8f) :
            new Color(0.35f, 0.35f, 0.4f);

        bool isHovering = position.Contains(Event.current.mousePosition);

        if (Event.current.type == EventType.Repaint)
        {
            Color currentColor = isHovering ? hoverColor : bgColor;

            EditorGUI.DrawRect(position, currentColor);

            // Responsive highlights and shadows
            float highlightIntensity = mode == ResponsiveMode.Narrow ? 0.08f : 0.1f;
            EditorGUI.DrawRect(
                new Rect(position.x, position.y, position.width, 1),
                new Color(currentColor.r + highlightIntensity, currentColor.g + highlightIntensity,
                    currentColor.b + highlightIntensity, 0.8f)
            );

            EditorGUI.DrawRect(
                new Rect(position.x, position.y + position.height - 1, position.width, 1),
                new Color(currentColor.r - highlightIntensity, currentColor.g - highlightIntensity,
                    currentColor.b - highlightIntensity, 0.8f)
            );
        }

        // Responsive button text
        int fontSize = GetResponsiveFontSize(11, 10, 10);
        GUIStyle buttonLabelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = fontSize,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
        };

        EditorGUI.LabelField(position, new GUIContent(label, tooltip), buttonLabelStyle);

        if (Event.current.type == EventType.MouseDown && position.Contains(Event.current.mousePosition))
        {
            onClick?.Invoke();
            Event.current.Use();
        }
    }

    /// <summary>
    /// Draws the Smart Naming Assistant section.
    /// </summary>
    private void DrawSmartNamingSection()
    {
        DrawSectionHeader("Smart Naming Assistant",
            "Automatically fix poorly named UI elements and resolve naming conflicts.\n" +
            "Because you should have named them properly from the start 😉");

        float spacing = GetResponsiveSpacing(10f);
        EditorGUILayout.Space(spacing);

        // Quick status check
        var allUIElements = FindAllUIElementsInScene();
        var badlyNamedCount = CountBadlyNamedElements(allUIElements);
        var totalElements = allUIElements.Count;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Status indicator
        if (badlyNamedCount > 0)
        {
            var warningStyle = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = new Color(0.8f, 0.6f, 0.2f) },
                fontSize = 12,
                wordWrap = true
            };

            EditorGUILayout.LabelField($"⚠️ Found {badlyNamedCount} of {totalElements} elements with naming issues", warningStyle);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("🔧 Open Smart Naming Assistant", GUILayout.Height(35)))
            {
                SmartNamingAssistant.ShowWindow(uiManager);
            }

            EditorGUILayout.Space(5);

            // Quick preview of issues
            var sampleBadNames = GetSampleBadNames(allUIElements, 3);
            if (sampleBadNames.Any())
            {
                EditorGUILayout.LabelField("Sample issues found:", EditorStyles.miniLabel);
                foreach (var badName in sampleBadNames)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"❌ {badName}", EditorStyles.miniLabel, GUILayout.Width(120));
                    EditorGUILayout.LabelField("(needs better naming)", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }

                if (badlyNamedCount > 3)
                {
                    EditorGUILayout.LabelField($"... and {badlyNamedCount - 3} more", EditorStyles.miniLabel);
                }
            }
        }
        else
        {
            var successStyle = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = new Color(0.2f, 0.7f, 0.3f) },
                fontSize = 12,
                wordWrap = true
            };

            EditorGUILayout.LabelField($"✅ All {totalElements} UI elements have proper names!", successStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Your naming is on point! 🎯", EditorStyles.centeredGreyMiniLabel);

            if (GUILayout.Button("🔍 Run Detailed Analysis Anyway", GUILayout.Height(30)))
            {
                SmartNamingAssistant.ShowWindow(uiManager);
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(spacing);

        // Quick tips section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("💡 Naming Best Practices:", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        var tipStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        EditorGUILayout.LabelField("✅ Be descriptive: 'PlayButton' not 'Button'", tipStyle);
        EditorGUILayout.LabelField("✅ Include purpose: 'UsernameField' not 'InputField'", tipStyle);
        EditorGUILayout.LabelField("✅ Use context: 'Login_SubmitButton' for clarity", tipStyle);
        EditorGUILayout.LabelField("❌ Avoid Unity defaults like 'Button (1)', 'Text (2)'", tipStyle);

        EditorGUILayout.EndVertical();
    }

    #region Smart Naming Helper Methods

    private List<GameObject> FindAllUIElementsInScene()
    {
        var uiElements = new List<GameObject>();
        var canvases = FindObjectsOfType<Canvas>();

        foreach (var canvas in canvases)
        {
            FindUIElementsRecursive(canvas.transform, uiElements);
        }

        return uiElements;
    }

    private void FindUIElementsRecursive(Transform parent, List<GameObject> uiElements)
    {
        foreach (Transform child in parent)
        {
            if (IsUIElementForNaming(child.gameObject))
            {
                uiElements.Add(child.gameObject);
            }
            FindUIElementsRecursive(child, uiElements);
        }
    }

    private bool IsUIElementForNaming(GameObject obj)
    {
        return obj.GetComponent<Button>() != null ||
               obj.GetComponent<InputField>() != null ||
               obj.GetComponent<TMP_InputField>() != null ||
               obj.GetComponent<Text>() != null ||
               obj.GetComponent<TMP_Text>() != null ||
               obj.GetComponent<Image>() != null ||
               obj.GetComponent<Toggle>() != null ||
               obj.GetComponent<Slider>() != null ||
               obj.GetComponent<Dropdown>() != null ||
               obj.GetComponent<TMP_Dropdown>() != null ||
               obj.GetComponent<ScrollRect>() != null;
    }

    private int CountBadlyNamedElements(List<GameObject> elements)
    {
        return elements.Count(element => IsBadlyNamedElement(element.name));
    }

    private bool IsBadlyNamedElement(string name)
    {
        var badPatterns = new[]
        {
            @"^(Button|InputField|Text|Image|Toggle|Slider|Dropdown|ScrollRect|Panel)(\s*\(\d+\))?$",
            @"^GameObject(\s*\(\d+\))?$",
            @"^New\s+"
        };

        return badPatterns.Any(pattern =>
            System.Text.RegularExpressions.Regex.IsMatch(name, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    private List<string> GetSampleBadNames(List<GameObject> elements, int maxSamples)
    {
        return elements
            .Where(e => IsBadlyNamedElement(e.name))
            .Take(maxSamples)
            .Select(e => e.name)
            .ToList();
    }

    #endregion

    /// <summary>
    /// Draws UI settings with responsive layout.
    /// </summary>
    private void DrawUIManagerSettings()
    {
        DrawSectionHeader("UI Manager Settings", "Configure global settings for the UI Manager component.");

        float spacing = GetResponsiveSpacing(10f);
        EditorGUILayout.Space(spacing);

        // UI Categories section with responsive styling
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        ResponsiveMode mode = GetResponsiveMode();
        int categoryHeaderFontSize = GetResponsiveFontSize(12, 11, 11);

        GUIStyle categoryHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = categoryHeaderFontSize,
            normal = { textColor = new Color(0.8f, 0.8f, 0.9f) }
        };

        EditorGUILayout.LabelField("UI Categories", categoryHeaderStyle);
        EditorGUILayout.Space(GetResponsiveSpacing(4f));

        EditorGUILayout.PropertyField(uiCategoriesProperty, GUIContent.none);
        EditorGUILayout.Space(GetResponsiveSpacing(5f));

        // Responsive action buttons
        DrawResponsiveActionButtons(
            new ResponsiveButton("Refresh All References", "Check and update all UI references",
                () => { RefreshUIHierarchy(); CheckMissingReferences(); }),
            new ResponsiveButton("Reset UI Manager", "Remove all UI references and reset the manager",
                () => {
                    if (EditorUtility.DisplayDialog("Reset UI Manager",
                        "Are you sure you want to reset the UI Manager? This will remove all UI references.",
                        "Reset", "Cancel"))
                    {
                        ResetUIManager();
                    }
                })
        );

        EditorGUILayout.EndVertical();

        // Enable Auto Standardization toggle - responsive spacing
        EditorGUILayout.Space(GetResponsiveSpacing(8f));
        DrawNamingStandardizationSettings();

        EditorGUILayout.Space(GetResponsiveSpacing(15f));

        // About section with responsive layout
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        int aboutHeaderFontSize = GetResponsiveFontSize(12, 11, 11);
        GUIStyle aboutHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = aboutHeaderFontSize,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.8f, 0.8f, 0.9f) }
        };

        EditorGUILayout.LabelField("About UI Framework", aboutHeaderStyle);
        EditorGUILayout.Space(GetResponsiveSpacing(4f));

        int versionFontSize = GetResponsiveFontSize(11, 10, 10);
        GUIStyle versionStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = versionFontSize,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.7f, 0.7f, 0.8f) }
        };

        EditorGUILayout.LabelField("Version 1.0.0", versionStyle);
        EditorGUILayout.Space(GetResponsiveSpacing(6f));

        int descFontSize = GetResponsiveFontSize(11, 10, 10);
        GUIStyle descStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = descFontSize,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.7f, 0.7f, 0.75f) }
        };

        EditorGUILayout.LabelField(
            "UI Framework helps you manage Unity UI elements and access them through code. " +
            "Maintain clean architecture with auto-generated code libraries and handlers.",
            descStyle
        );

        EditorGUILayout.Space(GetResponsiveSpacing(10f));

        // Responsive documentation button
        float docButtonHeight = mode == ResponsiveMode.Narrow ? 22f : 24f;
        Rect docButtonRect = EditorGUILayout.GetControlRect(false, docButtonHeight);

        float docButtonWidth = mode == ResponsiveMode.Narrow ?
            docButtonRect.width - GetResponsivePadding(10f) : 180f;

        float docButtonX = mode == ResponsiveMode.Narrow ?
            docButtonRect.x + GetResponsivePadding(5f) :
            docButtonRect.x + (docButtonRect.width - docButtonWidth) / 2;

        DrawActionButton(
            new Rect(docButtonX, docButtonRect.y, docButtonWidth, docButtonHeight),
            "Open Documentation",
            "View the full documentation for UI Framework",
            () => Application.OpenURL("https://docs.example.com/ui-framework"),
            true
        );

        EditorGUILayout.Space(GetResponsiveSpacing(5f));
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Reset the UI Manager to its default state.
    /// </summary>
    private void ResetUIManager()
    {
        Undo.RecordObject(uiManager, "Reset UI Manager");

        SerializedProperty categoriesProperty = serializedObject.FindProperty("uiCategories");
        categoriesProperty.ClearArray();

        categoriesProperty.arraySize = 1;
        SerializedProperty defaultCategory = categoriesProperty.GetArrayElementAtIndex(0);
        SerializedProperty categoryName = defaultCategory.FindPropertyRelative("name");
        categoryName.stringValue = "Default";

        serializedObject.ApplyModifiedProperties();

        foldoutStates.Clear();
        addedUIElements.Clear();

        Repaint();
    }

    #endregion
}

#region Helper Classes
public struct ResponsiveButton
{
    public string Label;
    public string Tooltip;
    public System.Action OnClick;
    public bool IsPrimary;

    public ResponsiveButton(string label, string tooltip, System.Action onClick, bool isPrimary = false)
    {
        Label = label;
        Tooltip = tooltip;
        OnClick = onClick;
        IsPrimary = isPrimary;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is ResponsiveButton))
            return false;

        var other = (ResponsiveButton)obj;
        return Label == other.Label &&
               Tooltip == other.Tooltip &&
               IsPrimary == other.IsPrimary &&
               OnClick == other.OnClick;
    }

    public override int GetHashCode()
    {
        return Label.GetHashCode() ^ Tooltip.GetHashCode() ^ IsPrimary.GetHashCode();
    }

    public static bool operator ==(ResponsiveButton a, ResponsiveButton b) => a.Equals(b);
    public static bool operator !=(ResponsiveButton a, ResponsiveButton b) => !a.Equals(b);
}

#endregion

#endif