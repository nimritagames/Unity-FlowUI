#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UI;

public partial class UIManagerEditor : Editor
{
    #region UI Hierarchy

    // Cache for hierarchy operations
    private Dictionary<GameObject, bool> supportedComponentCache = new Dictionary<GameObject, bool>();
    private Dictionary<int, Transform> transformCache = new Dictionary<int, Transform>();
    private HashSet<int> childCountCache = new HashSet<int>();
    private int lastHierarchyHash = 0;
    private float lastRefreshTime = 0;
    private const float HIERARCHY_REFRESH_INTERVAL = 2.0f; // Seconds

    // Reference selection
    private HashSet<GameObject> selectedUIElements = new HashSet<GameObject>();

    // Toggle for showing inactive elements
    private bool showInactiveElements = true;

    // Enhanced responsive breakpoints
    private float GetCurrentEditorWidth()
    {
        return EditorGUIUtility.currentViewWidth;
    }

    // More granular responsive modes
    private enum ExtendedResponsiveMode
    {
        ExtraSmall,  // < 320px
        Small,       // 320-480px
        Medium,      // 480-768px
        Large,       // 768-1024px
        ExtraLarge   // > 1024px
    }

    private ExtendedResponsiveMode GetExtendedResponsiveMode()
    {
        float width = GetCurrentEditorWidth();

        if (width < 320f) return ExtendedResponsiveMode.ExtraSmall;
        if (width < 480f) return ExtendedResponsiveMode.Small;
        if (width < 768f) return ExtendedResponsiveMode.Medium;
        if (width < 1024f) return ExtendedResponsiveMode.Large;
        return ExtendedResponsiveMode.ExtraLarge;
    }

    // Enhanced responsive calculations
    private float GetAdaptiveSpacing(float baseSpacing)
    {
        var mode = GetExtendedResponsiveMode();
        return mode switch
        {
            ExtendedResponsiveMode.ExtraSmall => baseSpacing * 0.6f,
            ExtendedResponsiveMode.Small => baseSpacing * 0.75f,
            ExtendedResponsiveMode.Medium => baseSpacing * 0.9f,
            ExtendedResponsiveMode.Large => baseSpacing,
            ExtendedResponsiveMode.ExtraLarge => baseSpacing * 1.2f,
            _ => baseSpacing
        };
    }

    private float GetAdaptivePadding(float basePadding)
    {
        var mode = GetExtendedResponsiveMode();
        return mode switch
        {
            ExtendedResponsiveMode.ExtraSmall => basePadding * 0.5f,
            ExtendedResponsiveMode.Small => basePadding * 0.7f,
            ExtendedResponsiveMode.Medium => basePadding * 0.85f,
            ExtendedResponsiveMode.Large => basePadding,
            ExtendedResponsiveMode.ExtraLarge => basePadding * 1.1f,
            _ => basePadding
        };
    }

    private int GetAdaptiveFontSize(int baseSize)
    {
        var mode = GetExtendedResponsiveMode();
        return mode switch
        {
            ExtendedResponsiveMode.ExtraSmall => Mathf.Max(8, baseSize - 2),
            ExtendedResponsiveMode.Small => Mathf.Max(9, baseSize - 1),
            ExtendedResponsiveMode.Medium => baseSize,
            ExtendedResponsiveMode.Large => baseSize + 1,
            ExtendedResponsiveMode.ExtraLarge => baseSize + 2,
            _ => baseSize
        };
    }

    private float GetAdaptiveRowHeight()
    {
        var mode = GetExtendedResponsiveMode();
        return mode switch
        {
            ExtendedResponsiveMode.ExtraSmall => 24f,
            ExtendedResponsiveMode.Small => 26f,
            ExtendedResponsiveMode.Medium => 28f,
            ExtendedResponsiveMode.Large => 30f,
            ExtendedResponsiveMode.ExtraLarge => 32f,
            _ => 28f
        };
    }

    private void DrawHierarchySection()
    {
        ResponsiveMode mode = GetResponsiveMode();
        var extendedMode = GetExtendedResponsiveMode();

        // Check if we need to refresh the hierarchy data
        float currentTime = (float)EditorApplication.timeSinceStartup;
        if (currentTime - lastRefreshTime > HIERARCHY_REFRESH_INTERVAL)
        {
            int currentHierarchyHash = GetSceneHierarchyHash();
            if (currentHierarchyHash != lastHierarchyHash)
            {
                // Clear caches if hierarchy changed
                supportedComponentCache.Clear();
                childCountCache.Clear();
                lastHierarchyHash = currentHierarchyHash;
            }
            lastRefreshTime = currentTime;
        }

        EditorGUILayout.BeginVertical();

        // Info header with enhanced responsive styling
        EditorGUILayout.BeginVertical("box");
        DrawSectionHeader("UI Hierarchy",
            extendedMode == ExtendedResponsiveMode.ExtraSmall ?
            "Add UI elements from scene" :
            "Add UI elements from your scene to the UI Manager. \nThese elements will be accessible through the generated code library.");
        EditorGUILayout.EndVertical();

        // Search bar with adaptive sizing
        DrawEnhancedHierarchySearchBar();

        // Toggle for showing inactive elements with enhanced responsive styling
        EditorGUILayout.Space(GetAdaptiveSpacing(6f));
        DrawAdaptiveInactiveToggle();

        // Update hierarchy drawing
        UpdateHierarchyDrawing();

        // Enhanced responsive sections
        EditorGUILayout.Space(GetAdaptiveSpacing(10f));
        DrawEnhancedQuickActionButtons();

        EditorGUILayout.Space(GetAdaptiveSpacing(10f));
        DrawEnhancedHierarchyControlButtons();

        EditorGUILayout.Space(GetAdaptiveSpacing(10f));
        DrawEnhancedUICounter();

        EditorGUILayout.Space(GetAdaptiveSpacing(8f));
        DrawEnhancedHierarchyScrollView();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Enhanced search bar with better responsiveness
    /// </summary>
    private void DrawEnhancedHierarchySearchBar()
    {
        var extendedMode = GetExtendedResponsiveMode();

        EditorGUILayout.BeginHorizontal();

        // Adaptive search field width
        float searchWidth = extendedMode switch
        {
            ExtendedResponsiveMode.ExtraSmall => -1f, // Full width
            ExtendedResponsiveMode.Small => -1f,      // Full width
            ExtendedResponsiveMode.Medium => 200f,
            ExtendedResponsiveMode.Large => 250f,
            ExtendedResponsiveMode.ExtraLarge => 300f,
            _ => 200f
        };

        if (searchWidth > 0)
        {
            DrawHierarchySearchBar();
            GUILayout.FlexibleSpace();
        }
        else
        {
            DrawHierarchySearchBar();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Adaptive inactive elements toggle
    /// </summary>
    private void DrawAdaptiveInactiveToggle()
    {
        var extendedMode = GetExtendedResponsiveMode();

        int toggleFontSize = GetAdaptiveFontSize(11);
        GUIStyle toggleStyle = new GUIStyle(EditorStyles.label) { fontSize = toggleFontSize };

        string toggleText = extendedMode == ExtendedResponsiveMode.ExtraSmall ?
            "Show Inactive" : "Show Inactive Elements";

        string tooltip = extendedMode == ExtendedResponsiveMode.ExtraSmall ?
            "Include disabled objects" :
            "Include disabled GameObjects in the hierarchy view and operations";

        bool prevShowInactive = showInactiveElements;
        showInactiveElements = EditorGUILayout.ToggleLeft(
            new GUIContent(toggleText, tooltip),
            showInactiveElements, toggleStyle);

        if (prevShowInactive != showInactiveElements)
        {
            RefreshUIHierarchy();
        }
    }

    /// <summary>
    /// Enhanced quick action buttons with improved responsive layout
    /// </summary>
    private void DrawEnhancedQuickActionButtons()
    {
        var extendedMode = GetExtendedResponsiveMode();

        // Adaptive button styling
        float buttonHeight = GetAdaptiveRowHeight() - 4f;
        int buttonFontSize = GetAdaptiveFontSize(11);

        GUIStyle actionButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fixedHeight = buttonHeight,
            fontSize = buttonFontSize,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(4, 4, 2, 2)
        };

        // Responsive button configurations
        var buttons = new[]
        {
            new {
                label = extendedMode <= ExtendedResponsiveMode.Small ? "+ Buttons" : "+ All Buttons",
                action = (System.Action)(() => AddAllElementsOfType<Button>())
            },
            new {
                label = extendedMode <= ExtendedResponsiveMode.Small ? "+ Texts" : "+ All Texts",
                action = (System.Action)(() => { AddAllElementsOfType<Text>(); AddAllElementsOfType<TMP_Text>(); })
            },
            new {
                label = extendedMode <= ExtendedResponsiveMode.Small ? "+ Toggles" : "+ All Toggles",
                action = (System.Action)(() => AddAllElementsOfType<Toggle>())
            },
            new {
                label = extendedMode <= ExtendedResponsiveMode.Small ? "+ Panels" : "+ All Panels",
                action = (System.Action)(() => AddAllPanels())
            }
        };

        // Layout based on screen size
        switch (extendedMode)
        {
            case ExtendedResponsiveMode.ExtraSmall:
            case ExtendedResponsiveMode.Small:
                DrawVerticalButtonLayout(buttons, actionButtonStyle, buttonHeight);
                break;

            case ExtendedResponsiveMode.Medium:
                DrawGridButtonLayout(buttons, actionButtonStyle, 2);
                break;

            case ExtendedResponsiveMode.Large:
            case ExtendedResponsiveMode.ExtraLarge:
                DrawHorizontalButtonLayout(buttons, actionButtonStyle);
                break;
        }
    }

    /// <summary>
    /// Vertical button layout for small screens
    /// </summary>
    private void DrawVerticalButtonLayout(dynamic[] buttons, GUIStyle style, float height)
    {
        float padding = GetAdaptivePadding(5f);
        float spacing = GetAdaptiveSpacing(3f);

        foreach (var button in buttons)
        {
            Rect buttonRect = EditorGUILayout.GetControlRect(false, height);
            buttonRect.x += padding;
            buttonRect.width -= 2 * padding;

            if (GUI.Button(buttonRect, new GUIContent(button.label), style))
            {
                button.action();
            }

            if (button != buttons.Last())
            {
                EditorGUILayout.Space(spacing);
            }
        }
    }

    /// <summary>
    /// Grid button layout for medium screens
    /// </summary>
    private void DrawGridButtonLayout(dynamic[] buttons, GUIStyle style, int columnsPerRow)
    {
        float spacing = GetAdaptiveSpacing(4f);

        for (int i = 0; i < buttons.Length; i += columnsPerRow)
        {
            EditorGUILayout.BeginHorizontal();

            for (int j = 0; j < columnsPerRow && i + j < buttons.Length; j++)
            {
                var button = buttons[i + j];
                if (GUILayout.Button(new GUIContent(button.label), style))
                {
                    button.action();
                }
            }

            // Fill remaining space if odd number of buttons
            if ((buttons.Length - i) < columnsPerRow)
            {
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndHorizontal();

            if (i + columnsPerRow < buttons.Length)
            {
                EditorGUILayout.Space(spacing);
            }
        }
    }

    /// <summary>
    /// Horizontal button layout for large screens
    /// </summary>
    private void DrawHorizontalButtonLayout(dynamic[] buttons, GUIStyle style)
    {
        EditorGUILayout.BeginHorizontal();

        foreach (var button in buttons)
        {
            if (GUILayout.Button(new GUIContent(button.label), style))
            {
                button.action();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Enhanced hierarchy control buttons with adaptive layout
    /// </summary>
    private void DrawEnhancedHierarchyControlButtons()
    {
        var extendedMode = GetExtendedResponsiveMode();

        float buttonHeight = GetAdaptiveRowHeight() - 4f;
        int buttonFontSize = GetAdaptiveFontSize(11);

        GUIStyle actionButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fixedHeight = buttonHeight,
            fontSize = buttonFontSize,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(4, 4, 2, 2)
        };

        var controlButtons = new[]
        {
            new ResponsiveButton(
                extendedMode <= ExtendedResponsiveMode.Small ? "Refresh" : "Refresh Hierarchy",
                "Refresh the UI hierarchy display",
                () => RefreshUIHierarchy()
            ),
            new ResponsiveButton(
                extendedMode <= ExtendedResponsiveMode.Small ? "Expand" : "Expand All",
                "Expand all hierarchy elements",
                () => ExpandAllHierarchy()
            ),
            new ResponsiveButton(
                extendedMode <= ExtendedResponsiveMode.Small ? "Collapse" : "Collapse All",
                "Collapse all hierarchy elements",
                () => CollapseAllHierarchy()
            ),
            new ResponsiveButton(
                extendedMode <= ExtendedResponsiveMode.Small ? "Remove" : "Remove Selected",
                "Remove selected UI elements from UI Manager",
                () => RemoveSelectedReferences(),
                false
            )
        };

        // Layout based on screen size
        switch (extendedMode)
        {
            case ExtendedResponsiveMode.ExtraSmall:
            case ExtendedResponsiveMode.Small:
                DrawVerticalControlButtons(controlButtons, actionButtonStyle, buttonHeight);
                break;

            case ExtendedResponsiveMode.Medium:
                DrawGridControlButtons(controlButtons, actionButtonStyle, 2);
                break;

            case ExtendedResponsiveMode.Large:
            case ExtendedResponsiveMode.ExtraLarge:
                DrawHorizontalControlButtons(controlButtons, actionButtonStyle);
                break;
        }
    }

    /// <summary>
    /// Vertical control buttons layout
    /// </summary>
    private void DrawVerticalControlButtons(ResponsiveButton[] buttons, GUIStyle style, float height)
    {
        float padding = GetAdaptivePadding(5f);
        float spacing = GetAdaptiveSpacing(3f);

        foreach (var button in buttons)
        {
            Rect buttonRect = EditorGUILayout.GetControlRect(false, height);
            buttonRect.x += padding;
            buttonRect.width -= 2 * padding;

            DrawControlButton(button, buttonRect, style);

            if (button != buttons.Last())
            {
                EditorGUILayout.Space(spacing);
            }
        }
    }

    /// <summary>
    /// Grid control buttons layout
    /// </summary>
    private void DrawGridControlButtons(ResponsiveButton[] buttons, GUIStyle style, int columnsPerRow)
    {
        float spacing = GetAdaptiveSpacing(4f);

        for (int i = 0; i < buttons.Length; i += columnsPerRow)
        {
            EditorGUILayout.BeginHorizontal();

            for (int j = 0; j < columnsPerRow && i + j < buttons.Length; j++)
            {
                var button = buttons[i + j];
                DrawControlButton(button, style);
            }

            if ((buttons.Length - i) < columnsPerRow)
            {
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndHorizontal();

            if (i + columnsPerRow < buttons.Length)
            {
                EditorGUILayout.Space(spacing);
            }
        }
    }

    /// <summary>
    /// Horizontal control buttons layout
    /// </summary>
    private void DrawHorizontalControlButtons(ResponsiveButton[] buttons, GUIStyle style)
    {
        EditorGUILayout.BeginHorizontal();

        foreach (var button in buttons)
        {
            DrawControlButton(button, style);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Helper method to draw individual control buttons
    /// </summary>
    private void DrawControlButton(ResponsiveButton button, GUIStyle style)
    {
        if (button.Label.Contains("Remove"))
        {
            Color defaultColor = GUI.backgroundColor;

            if (selectedUIElements.Count > 0)
            {
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            }
            else
            {
                GUI.enabled = false;
            }

            if (GUILayout.Button(new GUIContent(button.Label, button.Tooltip), style))
            {
                button.OnClick();
            }

            GUI.backgroundColor = defaultColor;
            GUI.enabled = true;
        }
        else
        {
            if (GUILayout.Button(new GUIContent(button.Label, button.Tooltip), style))
            {
                button.OnClick();
            }
        }
    }

    /// <summary>
    /// Helper method to draw control button with specific rect
    /// </summary>
    private void DrawControlButton(ResponsiveButton button, Rect rect, GUIStyle style)
    {
        if (button.Label.Contains("Remove"))
        {
            Color defaultColor = GUI.backgroundColor;

            if (selectedUIElements.Count > 0)
            {
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            }
            else
            {
                GUI.enabled = false;
            }

            if (GUI.Button(rect, new GUIContent(button.Label, button.Tooltip), style))
            {
                button.OnClick();
            }

            GUI.backgroundColor = defaultColor;
            GUI.enabled = true;
        }
        else
        {
            if (GUI.Button(rect, new GUIContent(button.Label, button.Tooltip), style))
            {
                button.OnClick();
            }
        }
    }

    /// <summary>
    /// Enhanced UI counter with responsive design
    /// </summary>
    private void DrawEnhancedUICounter()
    {
        var extendedMode = GetExtendedResponsiveMode();

        EditorGUILayout.BeginVertical("box");

        int counterFontSize = GetAdaptiveFontSize(12);
        GUIStyle counterStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = counterFontSize
        };

        float counterHeight = GetAdaptiveRowHeight() - 6f;

        // Adaptive counter display
        switch (extendedMode)
        {
            case ExtendedResponsiveMode.ExtraSmall:
                // Very compact for extra small screens
                EditorGUILayout.LabelField($"Added: {addedUIElements.Count} | Sel: {selectedUIElements.Count}",
                    counterStyle, GUILayout.Height(counterHeight));
                break;

            case ExtendedResponsiveMode.Small:
                // Stack for small screens
                EditorGUILayout.LabelField($"Added: {addedUIElements.Count}",
                    counterStyle, GUILayout.Height(counterHeight));
                EditorGUILayout.LabelField($"Selected: {selectedUIElements.Count}",
                    counterStyle, GUILayout.Height(counterHeight));
                break;

            case ExtendedResponsiveMode.Medium:
            case ExtendedResponsiveMode.Large:
            case ExtendedResponsiveMode.ExtraLarge:
            default:
                // Single line for larger screens
                EditorGUILayout.LabelField($"UI Elements Added: {addedUIElements.Count} | Selected: {selectedUIElements.Count}",
                    counterStyle, GUILayout.Height(counterHeight));
                break;
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Enhanced hierarchy scroll view with adaptive height and improved performance
    /// </summary>
    private void DrawEnhancedHierarchyScrollView()
    {
        var extendedMode = GetExtendedResponsiveMode();

        // Adaptive scroll view height
        float scrollViewHeight = extendedMode switch
        {
            ExtendedResponsiveMode.ExtraSmall => 300f,
            ExtendedResponsiveMode.Small => 320f,
            ExtendedResponsiveMode.Medium => 350f,
            ExtendedResponsiveMode.Large => 380f,
            ExtendedResponsiveMode.ExtraLarge => 400f,
            _ => 350f
        };

        // Enhanced background styling
        GUIStyle scrollViewStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(0, 0, 0, 0)
        };

        EditorGUILayout.BeginVertical(scrollViewStyle);

        // Adaptive scroll view behavior
        bool showHorizontalScrollbar = extendedMode <= ExtendedResponsiveMode.Small;

        panelScrollPosition = EditorGUILayout.BeginScrollView(
            panelScrollPosition,
            showHorizontalScrollbar,
            true,
            GUILayout.Height(scrollViewHeight)
        );

        // Get canvases with improved filtering
        var currentScene = uiManager.gameObject.scene;
        var canvases = FindAllObjectsOfType<Canvas>(showInactiveElements)
            .Where(c => c.gameObject.scene == currentScene)
            .OrderBy(c => c.sortingOrder)
            .ThenBy(c => c.name);

        if (!canvases.Any())
        {
            DrawEmptyHierarchyMessage();
        }
        else
        {
            DrawHierarchyContent(canvases);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draw empty hierarchy message with responsive styling
    /// </summary>
    private void DrawEmptyHierarchyMessage()
    {
        var extendedMode = GetExtendedResponsiveMode();

        int helpBoxFontSize = GetAdaptiveFontSize(11);
        GUIStyle helpBoxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            fontSize = helpBoxFontSize,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(10, 10, 20, 20)
        };

        string message = extendedMode <= ExtendedResponsiveMode.Small ?
            "No Canvases found.\nAdd a Canvas to your scene first." :
            "No Canvases found in the current scene. Add a Canvas to your scene first.";

        GUILayout.Label(message, helpBoxStyle);
    }

    /// <summary>
    /// Draw hierarchy content with adaptive padding
    /// </summary>
    private void DrawHierarchyContent(IOrderedEnumerable<Canvas> canvases)
    {
        var extendedMode = GetExtendedResponsiveMode();

        // Adaptive padding
        float rightPadding = extendedMode switch
        {
            ExtendedResponsiveMode.ExtraSmall => 8f,
            ExtendedResponsiveMode.Small => 10f,
            ExtendedResponsiveMode.Medium => 12f,
            ExtendedResponsiveMode.Large => 14f,
            ExtendedResponsiveMode.ExtraLarge => 16f,
            _ => 12f
        };

        EditorGUILayout.BeginHorizontal();

        // Main content area
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        // Adaptive spacing
        GUILayout.Space(GetAdaptiveSpacing(4f));

        foreach (var canvas in canvases)
        {
            DrawHierarchyItem(canvas.transform, true, 0);
        }

        GUILayout.Space(GetAdaptiveSpacing(4f));

        EditorGUILayout.EndVertical();

        // Right padding for scrollbar
        GUILayout.Space(rightPadding);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawHierarchyItem(Transform transform, bool isRoot, int indent)
    {
        if (transform == null) return;

        // Skip inactive elements if not showing them
        if (!showInactiveElements && !transform.gameObject.activeSelf && !isRoot) return;

        // Use the optimized search methods
        bool isMatchingSearch = IsMatchingSearch(transform);

        // If not matching search and not a root item, check children recursively
        if (!isMatchingSearch && !isRoot)
        {
            bool anyChildMatches = AnyChildMatchesSearch(transform);
            if (!anyChildMatches) return;
        }

        // Draw the current item with enhanced responsiveness
        DrawEnhancedHierarchyItemRow(transform, indent);

        // Enhanced foldout state management
        bool isFolded = GetEnhancedFoldoutState(transform, isRoot);

        // Draw children if expanded
        if (!isFolded)
        {
            DrawHierarchyChildren(transform, indent);
        }
    }

    /// <summary>
    /// Enhanced foldout state management
    /// </summary>
    private bool GetEnhancedFoldoutState(Transform transform, bool isRoot)
    {
        if (foldoutStates.TryGetValue(transform, out bool state))
        {
            return state;
        }

        // Auto-expand logic with search awareness
        bool autoExpand = isRoot ||
                         (!string.IsNullOrEmpty(searchQuery) && ShouldExpandForSearch(transform)) ||
                         (string.IsNullOrEmpty(searchQuery) && transform.childCount <= 3); // Auto-expand small hierarchies

        foldoutStates[transform] = !autoExpand;
        return foldoutStates[transform];
    }

    /// <summary>
    /// Draw hierarchy children with performance optimization
    /// </summary>
    private void DrawHierarchyChildren(Transform transform, int indent)
    {
        int childCount = transform.childCount;

        // Cache optimization
        if (!childCountCache.Contains(transform.GetInstanceID()))
        {
            childCountCache.Add(transform.GetInstanceID());
        }

        // Virtualization for large hierarchies
        const int MAX_VISIBLE_CHILDREN = 100;

        if (childCount > MAX_VISIBLE_CHILDREN)
        {
            DrawVirtualizedChildren(transform, indent);
        }
        else
        {
            DrawAllChildren(transform, indent);
        }
    }

    /// <summary>
    /// Draw all children (for small hierarchies)
    /// </summary>
    private void DrawAllChildren(Transform transform, int indent)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            int childId = child.GetInstanceID();
            transformCache[childId] = child;
            DrawHierarchyItem(child, false, indent + 1);
        }
    }

    /// <summary>
    /// Draw virtualized children (for large hierarchies)
    /// </summary>
    private void DrawVirtualizedChildren(Transform transform, int indent)
    {
        // Simple virtualization - only show first N children and a summary
        const int VISIBLE_COUNT = 50;

        for (int i = 0; i < Mathf.Min(VISIBLE_COUNT, transform.childCount); i++)
        {
            Transform child = transform.GetChild(i);
            DrawHierarchyItem(child, false, indent + 1);
        }

        if (transform.childCount > VISIBLE_COUNT)
        {
            var extendedMode = GetExtendedResponsiveMode();
            float padding = GetAdaptivePadding(8f);
            float indentSpacing = extendedMode == ExtendedResponsiveMode.ExtraSmall ? 16f : 20f;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(padding + (indent + 1) * indentSpacing);

            GUIStyle summaryStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Italic,
                fontSize = GetAdaptiveFontSize(10)
            };

            EditorGUILayout.LabelField($"... and {transform.childCount - VISIBLE_COUNT} more children", summaryStyle);
            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// Enhanced hierarchy item row with improved responsiveness and accessibility
    /// </summary>
    private void DrawEnhancedHierarchyItemRow(Transform transform, int indent)
    {
        var extendedMode = GetExtendedResponsiveMode();

        bool isMatchingSearch = IsMatchingSearch(transform);
        bool isInactive = !transform.gameObject.activeSelf;

        // Adaptive row height with minimum touch target size
        float rowHeight = Mathf.Max(GetAdaptiveRowHeight(), 24f);

        // Enhanced row styling
        Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(rowHeight));

        // Draw enhanced background
        DrawEnhancedRowBackground(rowRect, transform, isMatchingSearch, isInactive);

        // Adaptive base padding with minimum for touch targets
        float basePadding = Mathf.Max(GetAdaptivePadding(8f), 4f);
        GUILayout.Space(basePadding);

        // Enhanced indentation guides
        DrawEnhancedIndentationGuides(rowRect, indent, basePadding, extendedMode);

        // Draw foldout control
        DrawEnhancedFoldoutControl(transform, rowHeight, extendedMode);

        // Draw type icon
        DrawEnhancedTypeIcon(transform.gameObject, rowHeight, extendedMode);

        // Draw selection checkbox
        DrawEnhancedSelectionCheckbox(transform.gameObject, rowHeight, extendedMode);

        // Draw element name with enhanced styling
        DrawEnhancedElementName(transform, isMatchingSearch, isInactive, rowHeight, extendedMode);

        // Flexible space and action buttons
        GUILayout.FlexibleSpace();
        DrawEnhancedActionButtons(transform.gameObject, rowHeight, extendedMode);

        EditorGUILayout.EndHorizontal();

        // Enhanced divider
        DrawEnhancedRowDivider(rowRect);
    }

    /// <summary>
    /// Enhanced row background with better visual hierarchy
    /// </summary>
    private void DrawEnhancedRowBackground(Rect rowRect, Transform transform, bool isMatchingSearch, bool isInactive)
    {
        if (Event.current.type != EventType.Repaint) return;

        Color rowColor;

        // Priority-based coloring system
        if (Selection.activeGameObject == transform.gameObject)
        {
            // Highest priority: Unity selection
            rowColor = new Color(0.3f, 0.5f, 0.7f, 0.6f);
        }
        else if (selectedUIElements.Contains(transform.gameObject))
        {
            // High priority: Custom selection for removal
            rowColor = new Color(0.7f, 0.3f, 0.3f, 0.4f);
        }
        else if (isMatchingSearch && !string.IsNullOrEmpty(searchQuery))
        {
            // Medium priority: Search match
            rowColor = new Color(0.5f, 0.5f, 0.2f, 0.4f);
        }
        else if (isInactive)
        {
            // Low priority: Inactive element
            rowColor = new Color(0.2f, 0.2f, 0.25f, 0.3f);
        }
        else
        {
            // Default: Alternating rows
            rowColor = (transform.GetInstanceID() % 2 == 0)
                ? new Color(0.22f, 0.22f, 0.22f, 0.4f)
                : new Color(0.25f, 0.25f, 0.25f, 0.2f);
        }

        // Add subtle gradient effect
        Rect gradientRect = new Rect(rowRect.x, rowRect.y, rowRect.width, 1);
        EditorGUI.DrawRect(gradientRect, Color.Lerp(rowColor, Color.white, 0.1f));

        // Main background
        EditorGUI.DrawRect(rowRect, rowColor);
    }

    /// <summary>
    /// Enhanced indentation guides with better visibility
    /// </summary>
    private void DrawEnhancedIndentationGuides(Rect rowRect, int indent, float basePadding, ExtendedResponsiveMode mode)
    {
        if (indent <= 0 || Event.current.type != EventType.Repaint) return;

        float indentSpacing = mode switch
        {
            ExtendedResponsiveMode.ExtraSmall => 14f,
            ExtendedResponsiveMode.Small => 16f,
            ExtendedResponsiveMode.Medium => 18f,
            ExtendedResponsiveMode.Large => 20f,
            ExtendedResponsiveMode.ExtraLarge => 22f,
            _ => 18f
        };

        Color guideColor = EditorGUIUtility.isProSkin ?
            new Color(0.6f, 0.6f, 0.6f, 0.2f) :
            new Color(0.4f, 0.4f, 0.4f, 0.3f);

        // Vertical guides
        for (int i = 0; i < indent; i++)
        {
            float x = rowRect.x + basePadding + 16 + (i * indentSpacing);
            Rect lineRect = new Rect(x, rowRect.y, 1, rowRect.height);
            EditorGUI.DrawRect(lineRect, guideColor);
        }

        // Horizontal connecting line
        float startX = rowRect.x + basePadding + 16 + ((indent - 1) * indentSpacing);
        Rect hLineRect = new Rect(startX, rowRect.y + rowRect.height / 2, indentSpacing - 8, 1);
        EditorGUI.DrawRect(hLineRect, guideColor);

        // Connection point
        Rect pointRect = new Rect(startX + indentSpacing - 8, rowRect.y + rowRect.height / 2 - 1, 3, 3);
        EditorGUI.DrawRect(pointRect, Color.Lerp(guideColor, Color.white, 0.3f));
    }

    /// <summary>
    /// Enhanced foldout control with better visual feedback
    /// </summary>
    private void DrawEnhancedFoldoutControl(Transform transform, float rowHeight, ExtendedResponsiveMode mode)
    {
        bool hasChildren = transform.childCount > 0;

        float foldoutSize = mode switch
        {
            ExtendedResponsiveMode.ExtraSmall => 14f,
            ExtendedResponsiveMode.Small => 16f,
            ExtendedResponsiveMode.Medium => 18f,
            ExtendedResponsiveMode.Large => 20f,
            ExtendedResponsiveMode.ExtraLarge => 22f,
            _ => 18f
        };

        if (hasChildren)
        {
            bool isFolded = foldoutStates.TryGetValue(transform, out bool state) ? state : true;

            Rect foldoutRect = GUILayoutUtility.GetRect(foldoutSize, foldoutSize, GUILayout.ExpandWidth(false));
            foldoutRect.y += (rowHeight - foldoutSize) / 2;

            // Enhanced visual feedback
            if (Event.current.type == EventType.Repaint)
            {
                // Hover detection
                bool isHovered = foldoutRect.Contains(Event.current.mousePosition);

                // Background with hover effect
                Color bgColor = isHovered ?
                    new Color(0.4f, 0.4f, 0.4f, 0.7f) :
                    new Color(0.3f, 0.3f, 0.3f, 0.5f);

                EditorGUI.DrawRect(new Rect(foldoutRect.x - 2, foldoutRect.y - 2, foldoutSize + 4, foldoutSize + 4), bgColor);

                // Enhanced triangle with smooth edges
                Color arrowColor = isHovered ?
                    new Color(1f, 1f, 1f, 1f) :
                    new Color(0.9f, 0.9f, 0.9f, 0.8f);

                DrawSmoothTriangle(foldoutRect, isFolded, arrowColor);
            }

            // Handle interaction with better feedback
            if (Event.current.type == EventType.MouseDown && foldoutRect.Contains(Event.current.mousePosition))
            {
                foldoutStates[transform] = !isFolded;
                Event.current.Use();
                GUI.changed = true;
                Repaint();
            }
        }
        else
        {
            GUILayout.Space(foldoutSize);
        }
    }

    /// <summary>
    /// Draw smooth triangle for foldout control
    /// </summary>
    private void DrawSmoothTriangle(Rect rect, bool isFolded, Color color)
    {
        float triangleOffset = rect.width * 0.25f;
        float triangleSize = rect.width * 0.5f;

        Vector3[] trianglePoints;

        if (isFolded)
        {
            // Right-pointing triangle
            trianglePoints = new Vector3[]
            {
                new Vector3(rect.x + triangleOffset, rect.y + triangleOffset),
                new Vector3(rect.x + triangleOffset, rect.y + rect.height - triangleOffset),
                new Vector3(rect.x + rect.width - triangleOffset, rect.y + rect.height / 2)
            };
        }
        else
        {
            // Down-pointing triangle
            trianglePoints = new Vector3[]
            {
                new Vector3(rect.x + triangleOffset, rect.y + triangleOffset),
                new Vector3(rect.x + rect.width - triangleOffset, rect.y + triangleOffset),
                new Vector3(rect.x + rect.width / 2, rect.y + rect.height - triangleOffset)
            };
        }

        Handles.color = color;
        Handles.DrawAAConvexPolygon(trianglePoints);
    }

    /// <summary>
    /// Enhanced type icon with better visual hierarchy
    /// </summary>
    private void DrawEnhancedTypeIcon(GameObject gameObject, float rowHeight, ExtendedResponsiveMode mode)
    {
        Texture2D typeIcon = GetTypeIcon(gameObject);

        float iconSize = mode switch
        {
            ExtendedResponsiveMode.ExtraSmall => 14f,
            ExtendedResponsiveMode.Small => 16f,
            ExtendedResponsiveMode.Medium => 18f,
            ExtendedResponsiveMode.Large => 20f,
            ExtendedResponsiveMode.ExtraLarge => 22f,
            _ => 18f
        };

        if (typeIcon != null)
        {
            Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize);
            iconRect.y += (rowHeight - iconSize) / 2;

            // Enhanced background with subtle shadow
            if (Event.current.type == EventType.Repaint)
            {
                // Shadow
                Rect shadowRect = new Rect(iconRect.x + 1, iconRect.y + 1, iconSize, iconSize);
                Color shadowColor = new Color(0, 0, 0, 0.1f);
                EditorGUI.DrawRect(shadowRect, shadowColor);

                // Background
                Color bgColor = new Color(1f, 1f, 1f, 0.05f);
                EditorGUI.DrawRect(iconRect, bgColor);
            }

            // Enhanced icon rendering with color modulation
            Color iconTint = gameObject.activeSelf ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            Color oldColor = GUI.color;
            GUI.color = iconTint;
            GUI.DrawTexture(iconRect, typeIcon, ScaleMode.ScaleToFit);
            GUI.color = oldColor;
        }
        else
        {
            GUILayout.Space(iconSize);
        }
    }

    /// <summary>
    /// Enhanced selection checkbox with better visual feedback
    /// </summary>
    private void DrawEnhancedSelectionCheckbox(GameObject gameObject, float rowHeight, ExtendedResponsiveMode mode)
    {
        bool isAdded = addedUIElements.Contains(gameObject);
        bool hasSupportedComponent = HasSupportedUIComponentCached(gameObject);

        if (isAdded || hasSupportedComponent)
        {
            float checkboxSize = mode switch
            {
                ExtendedResponsiveMode.ExtraSmall => 16f,
                ExtendedResponsiveMode.Small => 18f,
                ExtendedResponsiveMode.Medium => 20f,
                ExtendedResponsiveMode.Large => 22f,
                ExtendedResponsiveMode.ExtraLarge => 24f,
                _ => 20f
            };

            if (isAdded)
            {
                bool isSelected = selectedUIElements.Contains(gameObject);

                Rect checkboxRect = GUILayoutUtility.GetRect(checkboxSize, checkboxSize);
                checkboxRect.y += (rowHeight - checkboxSize) / 2;

                // Enhanced checkbox styling
                bool newSelection = DrawEnhancedCheckbox(checkboxRect, isSelected);

                if (newSelection != isSelected)
                {
                    if (newSelection)
                        selectedUIElements.Add(gameObject);
                    else
                        selectedUIElements.Remove(gameObject);
                }
            }
            else
            {
                GUILayout.Space(checkboxSize);
            }
        }
        else
        {
            float checkboxSize = mode <= ExtendedResponsiveMode.Small ? 16f : 20f;
            GUILayout.Space(checkboxSize);
        }
    }

    /// <summary>
    /// Draw enhanced checkbox with custom styling
    /// </summary>
    private bool DrawEnhancedCheckbox(Rect rect, bool value)
    {
        bool newValue = EditorGUI.Toggle(rect, value);

        // Add visual enhancement
        if (Event.current.type == EventType.Repaint && value)
        {
            // Add a subtle glow effect for checked state
            Color glowColor = new Color(0.3f, 0.7f, 1f, 0.3f);
            Rect glowRect = new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4);
            EditorGUI.DrawRect(glowRect, glowColor);
        }

        return newValue;
    }

    /// <summary>
    /// Enhanced element name with improved readability and search highlighting
    /// </summary>
    private void DrawEnhancedElementName(Transform transform, bool isMatchingSearch, bool isInactive,
                                       float rowHeight, ExtendedResponsiveMode mode)
    {
        int nameFontSize = GetAdaptiveFontSize(12);
        float namePadding = GetAdaptivePadding(4f);

        GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
        {
            padding = new RectOffset((int)namePadding, (int)namePadding, (int)(namePadding * 2), (int)(namePadding * 2)),
            margin = new RectOffset(2, 2, 0, 0),
            alignment = TextAnchor.MiddleLeft,
            fontSize = nameFontSize,
            clipping = TextClipping.Clip,
            wordWrap = false
        };

        // Enhanced status-based styling
        GameObject gameObject = transform.gameObject;
        bool isAdded = addedUIElements.Contains(gameObject);
        bool hasSupportedComponent = HasSupportedUIComponentCached(gameObject);

        if (isAdded)
        {
            nameStyle.normal.textColor = isInactive ?
                new Color(0.3f, 0.8f, 0.3f, 0.7f) :
                new Color(0.4f, 1.0f, 0.4f);
            nameStyle.fontStyle = FontStyle.Bold;
        }
        else if (hasSupportedComponent)
        {
            nameStyle.normal.textColor = isInactive ?
                new Color(0.3f, 0.6f, 0.9f, 0.7f) :
                new Color(0.4f, 0.8f, 1.0f);
        }
        else if (isInactive)
        {
            nameStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            nameStyle.fontStyle = FontStyle.Italic;
        }
        else
        {
            nameStyle.normal.textColor = EditorGUIUtility.isProSkin ?
                new Color(0.9f, 0.9f, 0.9f) :
                new Color(0.1f, 0.1f, 0.1f);
        }

        // Enhanced name display with status indicators
        string displayName = transform.name;
        if (isInactive && mode > ExtendedResponsiveMode.ExtraSmall)
        {
            displayName = $"{displayName} (inactive)";
        }
        else if (isInactive)
        {
            displayName = $"{displayName} (!)";
        }

        // Enhanced search highlighting
        if (isMatchingSearch && !string.IsNullOrEmpty(searchQuery))
        {
            DrawHighlightedText(displayName, searchQuery, nameStyle, rowHeight, mode);
        }
        else
        {
            float minWidth = mode switch
            {
                ExtendedResponsiveMode.ExtraSmall => 60f,
                ExtendedResponsiveMode.Small => 80f,
                ExtendedResponsiveMode.Medium => 100f,
                _ => 120f
            };

            EditorGUILayout.LabelField(displayName, nameStyle,
                GUILayout.MinWidth(minWidth),
                GUILayout.ExpandWidth(true),
                GUILayout.Height(rowHeight));
        }
    }

    /// <summary>
    /// Draw text with enhanced search highlighting
    /// </summary>
    private void DrawHighlightedText(string text, string searchTerm, GUIStyle style, float height, ExtendedResponsiveMode mode)
    {
        float minWidth = mode switch
        {
            ExtendedResponsiveMode.ExtraSmall => 60f,
            ExtendedResponsiveMode.Small => 80f,
            ExtendedResponsiveMode.Medium => 100f,
            _ => 120f
        };

        Rect labelRect = GUILayoutUtility.GetRect(minWidth, height, GUILayout.ExpandWidth(true));

        int matchIndex = text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);

        if (matchIndex >= 0)
        {
            // Enhanced highlighting with multiple colors and effects
            string beforeMatch = text.Substring(0, matchIndex);
            string matchText = text.Substring(matchIndex, searchTerm.Length);
            string afterMatch = text.Substring(matchIndex + searchTerm.Length);

            // Calculate text positioning
            float beforeWidth = style.CalcSize(new GUIContent(beforeMatch)).x;
            float matchWidth = style.CalcSize(new GUIContent(matchText)).x;

            // Draw background highlight with gradient effect
            Rect highlightRect = new Rect(labelRect.x + beforeWidth, labelRect.y + 2, matchWidth, height - 4);

            // Multi-layer highlight for better visibility
            EditorGUI.DrawRect(highlightRect, new Color(1f, 0.9f, 0.3f, 0.6f));
            EditorGUI.DrawRect(new Rect(highlightRect.x, highlightRect.y, highlightRect.width, 1),
                              new Color(1f, 0.8f, 0.2f, 0.8f));
            EditorGUI.DrawRect(new Rect(highlightRect.x, highlightRect.yMax - 1, highlightRect.width, 1),
                              new Color(1f, 0.8f, 0.2f, 0.8f));

            // Draw the complete text
            GUI.Label(labelRect, text, style);
        }
        else
        {
            // Fallback rendering
            GUI.Label(labelRect, text, style);
        }
    }

    /// <summary>
    /// Enhanced action buttons with improved responsive behavior
    /// </summary>
    private void DrawEnhancedActionButtons(GameObject gameObject, float rowHeight, ExtendedResponsiveMode mode)
    {
        bool isAdded = addedUIElements.Contains(gameObject);
        bool hasSupportedComponent = HasSupportedUIComponentCached(gameObject);

        // Adaptive button sizing
        float buttonHeight = rowHeight - 6f;
        int buttonFontSize = GetAdaptiveFontSize(10);
        float buttonPadding = GetAdaptivePadding(6f);
        float buttonSpacing = GetAdaptiveSpacing(8f);

        GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fixedHeight = buttonHeight,
            margin = new RectOffset(2, 2, (int)((rowHeight - buttonHeight) / 2), (int)((rowHeight - buttonHeight) / 2)),
            padding = new RectOffset((int)buttonPadding, (int)buttonPadding, 3, 3),
            alignment = TextAnchor.MiddleCenter,
            fontSize = buttonFontSize,
            clipping = TextClipping.Clip
        };

        // Responsive button widths and text
        (float addWidth, string addText, float selectWidth, string selectText) = mode switch
        {
            ExtendedResponsiveMode.ExtraSmall => (35f, "✓", 30f, "S"),
            ExtendedResponsiveMode.Small => (45f, "✓", 35f, "Sel"),
            ExtendedResponsiveMode.Medium => (60f, "Added", 45f, "Sel"),
            ExtendedResponsiveMode.Large => (70f, "Added ✓", 50f, "Select"),
            ExtendedResponsiveMode.ExtraLarge => (80f, "Added ✓", 60f, "Select"),
            _ => (60f, "Added", 45f, "Sel")
        };

        GUILayout.Space(buttonSpacing);

        // Enhanced Add/Added button
        if (isAdded)
        {
            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);
            GUI.enabled = false;

            string tooltip = mode <= ExtendedResponsiveMode.Small ?
                "Added to UI Manager" :
                "This UI element is already managed by the UI Manager";

            GUIContent addedContent = new GUIContent(addText, tooltip);
            GUILayout.Button(addedContent, buttonStyle, GUILayout.Width(addWidth));

            GUI.backgroundColor = defaultColor;
            GUI.enabled = true;
        }
        else if (hasSupportedComponent)
        {
            Color defaultColor = GUI.backgroundColor;

            // Hover effect simulation
            bool isHovered = lastHoveredButton == gameObject.GetInstanceID();
            GUI.backgroundColor = isHovered ?
                new Color(0.1f, 0.7f, 1.0f, 1.0f) :
                new Color(0.2f, 0.6f, 1.0f, 0.9f);

            // Enhanced tooltip with rename preview
            Transform transform = gameObject.transform;
            string standardizedName = namingSettings.EnableAutoStandardization ? GetStandardizedName(gameObject) : transform.name;
            bool wouldRename = transform.name != standardizedName;

            string tooltip = mode <= ExtendedResponsiveMode.Small ?
                "Add to UI Manager" :
                wouldRename ?
                    $"Add to UI Manager (will rename to {standardizedName})" :
                    "Add this UI element to the UI Manager";

            string buttonText = mode <= ExtendedResponsiveMode.Medium ? "+" : "Add +";
            GUIContent addContent = new GUIContent(buttonText, tooltip);

            Rect buttonRect = GUILayoutUtility.GetRect(addWidth, buttonHeight);

            // Track hover state
            if (Event.current.type == EventType.MouseMove && buttonRect.Contains(Event.current.mousePosition))
            {
                if (lastHoveredButton != gameObject.GetInstanceID())
                {
                    lastHoveredButton = gameObject.GetInstanceID();
                    Repaint();
                }
            }

            if (GUI.Button(buttonRect, addContent, buttonStyle))
            {
                HandleAddUIElement(gameObject, wouldRename, standardizedName, transform.name);
            }

            GUI.backgroundColor = defaultColor;
        }
        else
        {
            GUILayout.Space(addWidth + 4);
        }

        // Enhanced Select button
        Color selectBgColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);

        string selectTooltip = mode <= ExtendedResponsiveMode.Small ?
            "Select in Hierarchy" :
            "Select this object in the Hierarchy";

        GUIContent selectContent = new GUIContent(selectText, selectTooltip);

        if (GUILayout.Button(selectContent, buttonStyle, GUILayout.Width(selectWidth)))
        {
            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);
        }

        GUI.backgroundColor = selectBgColor;
    }

    // Track last hovered button for hover effects
    private int lastHoveredButton = -1;

    /// <summary>
    /// Handle adding UI element with enhanced feedback
    /// </summary>
    private void HandleAddUIElement(GameObject gameObject, bool wouldRename, string standardizedName, string originalName)
    {
        if (namingSettings.EnableAutoStandardization && namingSettings.AutoStandardizeOnAdd && wouldRename)
        {
            AddUIReferenceWithStandardization(gameObject);
            ShowQuickTip("Element Added with Standardized Name",
                $"Added and renamed from '{originalName}' to '{standardizedName}' for consistency.");
        }
        else
        {
            Undo.RecordObject(uiManager, "Add UI Reference");
            uiManager.AddUIReference(gameObject);
            EditorUtility.SetDirty(uiManager);
            addedUIElements.Add(gameObject);

            if (addedUIElements.Count == 1)
            {
                ShowQuickTip("First Element Added!",
                    "Great! Add more elements or go to Library Generation to create code access.");
            }
            else
            {
                ShowQuickTip("Element Added",
                    $"'{gameObject.name}' has been added to the UI Manager.");
            }
        }
    }

    /// <summary>
    /// Enhanced row divider with subtle gradient
    /// </summary>
    private void DrawEnhancedRowDivider(Rect rowRect)
    {
        if (Event.current.type != EventType.Repaint) return;

        // Main divider line
        Rect dividerRect = new Rect(rowRect.x, rowRect.y + rowRect.height - 1, rowRect.width, 1);
        Color dividerColor = EditorGUIUtility.isProSkin ?
            new Color(0.1f, 0.1f, 0.1f, 0.4f) :
            new Color(0.7f, 0.7f, 0.7f, 0.3f);
        EditorGUI.DrawRect(dividerRect, dividerColor);

        // Subtle highlight line above
        Rect highlightRect = new Rect(rowRect.x, rowRect.y + rowRect.height - 2, rowRect.width, 1);
        Color highlightColor = EditorGUIUtility.isProSkin ?
            new Color(0.3f, 0.3f, 0.3f, 0.1f) :
            new Color(1f, 1f, 1f, 0.1f);
        EditorGUI.DrawRect(highlightRect, highlightColor);
    }

    /// <summary>
    /// Optimized version that checks if a GameObject has supported UI components with caching.
    /// </summary>
    private bool HasSupportedUIComponentCached(GameObject gameObject)
    {
        if (gameObject == null) return false;

        // Check cache first
        if (supportedComponentCache.TryGetValue(gameObject, out bool hasSupport))
        {
            return hasSupport;
        }

        // Calculate and cache result
        hasSupport = HasSupportedUIComponent(gameObject);
        supportedComponentCache[gameObject] = hasSupport;

        return hasSupport;
    }

    /// <summary>
    /// Gets the full path of a transform in the hierarchy.
    /// Uses GetCachedPath from the search implementation.
    /// </summary>
    private string GetFullPath(Transform transform)
    {
        return GetCachedPath(transform);
    }

    /// <summary>
    /// Calculate a hash value for the scene hierarchy to detect changes with improved performance
    /// </summary>
    private int GetSceneHierarchyHash()
    {
        var currentScene = uiManager.gameObject.scene;
        var rootObjects = currentScene.GetRootGameObjects();

        int hash = currentScene.name.GetHashCode();
        hash = hash * 31 + currentScene.buildIndex;

        foreach (var root in rootObjects)
        {
            if (root != null)
            {
                hash = hash * 31 + root.name.GetHashCode();
                hash = hash * 31 + root.transform.childCount;
                hash = hash * 31 + root.activeSelf.GetHashCode();
            }
        }

        return hash;
    }

    /// <summary>
    /// Enhanced version that finds ALL objects of type T with better performance
    /// </summary>
    private static List<T> FindAllObjectsOfType<T>(bool includeInactive) where T : Component
    {
        if (includeInactive)
        {
            return Resources.FindObjectsOfTypeAll<T>()
                .Where(obj => obj != null &&
                            !EditorUtility.IsPersistent(obj.transform.root.gameObject) &&
                            !(obj.hideFlags == HideFlags.NotEditable || obj.hideFlags == HideFlags.HideAndDontSave))
                .ToList();
        }
        else
        {
            return FindObjectsByType<T>(FindObjectsSortMode.InstanceID).ToList();
        }
    }

    /// <summary>
    /// Enhanced method to add all elements of a specific type with better feedback
    /// </summary>
    private void AddAllElementsOfType<T>() where T : Component
    {
        var elements = FindAllObjectsOfType<T>(showInactiveElements)
            .Where(c => c != null && c.gameObject.scene == uiManager.gameObject.scene);

        int addedCount = 0;
        int skippedCount = 0;
        Undo.RecordObject(uiManager, $"Add All {typeof(T).Name}s");

        foreach (var element in elements)
        {
            if (!addedUIElements.Contains(element.gameObject))
            {
                uiManager.AddUIReference(element.gameObject);
                addedUIElements.Add(element.gameObject);
                addedCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        if (addedCount > 0)
        {
            EditorUtility.SetDirty(uiManager);

            string message = skippedCount > 0 ?
                $"Added {addedCount} new {typeof(T).Name} elements ({skippedCount} were already added)." :
                $"Added {addedCount} {typeof(T).Name} elements to the UI Manager.";

            ShowQuickTip("Elements Added", message);
        }
        else
        {
            ShowQuickTip("No New Elements", $"All {typeof(T).Name} elements have already been added.");
        }
    }

    /// <summary>
    /// Enhanced method to add all panels with better filtering
    /// </summary>
    private void AddAllPanels()
    {
        var images = FindAllObjectsOfType<Image>(showInactiveElements)
            .Where(i => i != null &&
                       i.gameObject.scene == uiManager.gameObject.scene &&
                       (i.gameObject.name.EndsWith("_Panel", StringComparison.OrdinalIgnoreCase) ||
                        i.gameObject.name.EndsWith("Panel", StringComparison.OrdinalIgnoreCase) ||
                        i.gameObject.name.Contains("Panel")));

        int addedCount = 0;
        int skippedCount = 0;
        Undo.RecordObject(uiManager, "Add All Panels");

        foreach (var image in images)
        {
            if (!addedUIElements.Contains(image.gameObject))
            {
                uiManager.AddUIReference(image.gameObject);
                addedUIElements.Add(image.gameObject);
                addedCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        if (addedCount > 0)
        {
            EditorUtility.SetDirty(uiManager);

            string message = skippedCount > 0 ?
                $"Added {addedCount} new panel elements ({skippedCount} were already added)." :
                $"Added {addedCount} panel elements to the UI Manager.";

            ShowQuickTip("Panels Added", message);
        }
        else
        {
            ShowQuickTip("No New Panels", "All panel elements have already been added.");
        }
    }

    /// <summary>
    /// Enhanced hierarchy refresh with progress indication
    /// </summary>
    private void RefreshUIHierarchy()
    {
        // Clear all caches
        foldoutStates.Clear();
        supportedComponentCache.Clear();
        transformCache.Clear();
        childCountCache.Clear();
        selectedUIElements.Clear();

        // Reset hover state
        lastHoveredButton = -1;

        // Rebuild caches
        BuildAddedUIElementsCache();

        // Force repaint
        Repaint();

        ShowQuickTip("Hierarchy Refreshed", "UI hierarchy has been refreshed successfully.");
    }

    /// <summary>
    /// Enhanced method to build the cache of added UI elements
    /// </summary>
    private void BuildAddedUIElementsCache()
    {
        addedUIElements.Clear();

        if (uiManager != null)
        {
            foreach (var category in uiManager.GetAllUICategories())
            {
                if (category?.references != null)
                {
                    foreach (var reference in category.references)
                    {
                        if (reference?.uiElement != null)
                        {
                            addedUIElements.Add(reference.uiElement);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Enhanced expand all with progress for large hierarchies
    /// </summary>
    private void ExpandAllHierarchy()
    {
        var currentScene = uiManager.gameObject.scene;
        var allTransforms = FindAllObjectsOfType<Transform>(showInactiveElements)
            .Where(t => t != null && t.gameObject.scene == currentScene);

        int expandedCount = 0;
        foreach (var transform in allTransforms)
        {
            if (transform.childCount > 0)
            {
                foldoutStates[transform] = false; // false means expanded
                expandedCount++;
            }
        }

        ShowQuickTip("Hierarchy Expanded", $"Expanded {expandedCount} hierarchy elements.");
        Repaint();
    }

    /// <summary>
    /// Enhanced collapse all with feedback
    /// </summary>
    private void CollapseAllHierarchy()
    {
        var currentScene = uiManager.gameObject.scene;
        var allTransforms = FindAllObjectsOfType<Transform>(showInactiveElements)
            .Where(t => t != null && t.gameObject.scene == currentScene);

        int collapsedCount = 0;
        foreach (var transform in allTransforms)
        {
            if (transform.childCount > 0)
            {
                foldoutStates[transform] = true; // true means collapsed
                collapsedCount++;
            }
        }

        ShowQuickTip("Hierarchy Collapsed", $"Collapsed {collapsedCount} hierarchy elements.");
        Repaint();
    }

    #endregion
}
#endif