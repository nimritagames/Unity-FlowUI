#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public partial class UIManagerEditor : Editor
{
    #region UI Categories Panel

    private Vector2 categoriesScrollPosition;
    private string filterCategoriesText = "";
    private bool showEmptyCategories = true;
    private GUIStyle categoryHeaderStyle;
    private GUIStyle categoryItemStyle;
    private GUIStyle titleBarStyle;
    private GUIStyle statusBarStyle;
    private Dictionary<string, Color> categoryColors = new Dictionary<string, Color>();

    // Support for multi-selection
    private int lastSelectedItemIndex = -1;
    private string lastSelectedCategory = null;

    // Map of category name to expanded state
    private Dictionary<string, bool> expandedCategories = new Dictionary<string, bool>();

    private void DrawCategoriesPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        InitializeStyles();

        // Title bar with tools
        DrawCategoriesTitleBar();

        // Filter and tools
        DrawCategoriesToolbar();

        // Categories list with proper scroll constraints
        DrawCategoriesList();

        EditorGUILayout.EndVertical();
    }

    private void InitializeStyles()
    {
        if (categoryHeaderStyle == null)
        {
            categoryHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            categoryHeaderStyle.fontSize = 12;
            categoryHeaderStyle.alignment = TextAnchor.MiddleLeft;
            categoryHeaderStyle.padding = new RectOffset(5, 5, 6, 6);
            categoryHeaderStyle.margin = new RectOffset(0, 0, 0, 0);
            categoryHeaderStyle.richText = true;
            categoryHeaderStyle.wordWrap = false;
        }

        if (categoryItemStyle == null)
        {
            categoryItemStyle = new GUIStyle(EditorStyles.label);
            categoryItemStyle.fontSize = 11;
            categoryItemStyle.padding = new RectOffset(5, 5, 3, 3);
            categoryItemStyle.margin = new RectOffset(0, 0, 0, 0);
            categoryItemStyle.richText = true;
            categoryItemStyle.wordWrap = false;
        }

        if (titleBarStyle == null)
        {
            titleBarStyle = new GUIStyle();
            titleBarStyle.normal.background = MakeColorTexture(new Color(0.2f, 0.2f, 0.3f));
            titleBarStyle.padding = new RectOffset(10, 10, 8, 8);
            titleBarStyle.margin = new RectOffset(0, 0, 0, 0);
        }

        if (statusBarStyle == null)
        {
            statusBarStyle = new GUIStyle();
            statusBarStyle.normal.background = MakeColorTexture(new Color(0.22f, 0.22f, 0.25f));
            statusBarStyle.padding = new RectOffset(10, 10, 5, 5);
            statusBarStyle.margin = new RectOffset(0, 0, 0, 0);
        }

        // Initialize category colors if needed
        if (categoryColors.Count == 0)
        {
            InitializeCategoryColors();
        }
    }

    private Texture2D MakeColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void InitializeCategoryColors()
    {
        // Create visually distinct colors for different categories
        categoryColors["Button"] = new Color(0.35f, 0.6f, 1f);
        categoryColors["Text"] = new Color(0.4f, 0.85f, 0.4f);
        categoryColors["TMP_Text"] = new Color(0.4f, 0.85f, 0.4f);
        categoryColors["Toggle"] = new Color(0.85f, 0.5f, 0.85f);
        categoryColors["InputField"] = new Color(0.85f, 0.8f, 0.3f);
        categoryColors["TMP_InputField"] = new Color(0.85f, 0.8f, 0.3f);
        categoryColors["Slider"] = new Color(1f, 0.6f, 0.3f);
        categoryColors["Dropdown"] = new Color(0.6f, 0.35f, 0.85f);
        categoryColors["ScrollView"] = new Color(0.5f, 0.6f, 0.7f);
        categoryColors["Panel"] = new Color(0.9f, 0.4f, 0.4f);
        categoryColors["Image"] = new Color(0.6f, 0.75f, 0.9f);
        categoryColors["RawImage"] = new Color(0.6f, 0.75f, 0.9f);
        categoryColors["Mask"] = new Color(0.6f, 0.6f, 0.6f);
        categoryColors["Canvas"] = new Color(0.85f, 0.6f, 0.2f);
        categoryColors["CanvasGroup"] = new Color(0.85f, 0.6f, 0.2f);
        categoryColors["Unknown"] = new Color(0.6f, 0.6f, 0.6f);
    }

    private void DrawCategoriesTitleBar()
    {
        EditorGUILayout.BeginVertical(titleBarStyle);
        EditorGUILayout.BeginHorizontal();

        // Title with icon
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
        };

        Texture2D folderIcon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
        if (folderIcon != null)
        {
            GUILayout.Label(folderIcon, GUILayout.Width(16), GUILayout.Height(16));
            GUILayout.Space(5);
        }

        GUILayout.Label("UI Categories", titleStyle);

        // Flexible space to push button to the right
        GUILayout.FlexibleSpace();

        // Refresh button
        if (GUILayout.Button(new GUIContent("↻", "Refresh Categories"), GUILayout.Width(30), GUILayout.Height(22)))
        {
            uiManager.InitializeDictionaries();
            BuildAddedUIElementsCache();
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawCategoriesToolbar()
    {
        EditorGUILayout.Space(5);

        // Filter section - constrain to available width
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);

        GUIContent filterLabel = new GUIContent("Filter:", "Filter categories and items by name");
        GUILayout.Label(filterLabel, GUILayout.Width(40));

        string newFilter = EditorGUILayout.TextField(filterCategoriesText);
        if (newFilter != filterCategoriesText)
        {
            filterCategoriesText = newFilter;
            Repaint();
        }

        // Clear button - only show if there's text to clear
        if (!string.IsNullOrEmpty(filterCategoriesText))
        {
            if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(18)))
            {
                filterCategoriesText = "";
                Repaint();
            }
        }

        GUILayout.Space(10);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Options and buttons section - constrain to available width
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);

        // Show empty categories toggle
        bool prevShowEmpty = showEmptyCategories;
        showEmptyCategories = EditorGUILayout.ToggleLeft(
            new GUIContent("Show Empty", "Show categories with no items"),
            showEmptyCategories,
            GUILayout.Width(100)
        );

        if (prevShowEmpty != showEmptyCategories)
        {
            Repaint();
        }

        // Flexible space to push buttons to the right
        GUILayout.FlexibleSpace();

        // Action buttons with constrained widths
        string selectButtonText = selectedUIElements.Count > 0 ? "Deselect All" : "Select All";
        if (GUILayout.Button(selectButtonText, GUILayout.Width(80)))
        {
            ToggleSelectAll();
        }

        if (GUILayout.Button("Expand All", GUILayout.Width(80)))
        {
            ExpandAllCategories(true);
        }

        if (GUILayout.Button("Collapse All", GUILayout.Width(80)))
        {
            ExpandAllCategories(false);
        }

        GUILayout.Space(10);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }

    private void ToggleSelectAll()
    {
        if (selectedUIElements.Count > 0)
        {
            // Deselect all
            selectedUIElements.Clear();
            lastSelectedItemIndex = -1;
            lastSelectedCategory = null;
        }
        else
        {
            // Select all visible items
            var categories = uiManager.GetAllUICategories();
            if (categories != null)
            {
                foreach (var category in categories)
                {
                    if (category.references != null)
                    {
                        foreach (var reference in category.references)
                        {
                            if (reference.uiElement != null && !selectedUIElements.Contains(reference.uiElement))
                            {
                                selectedUIElements.Add(reference.uiElement);
                            }
                        }
                    }
                }
            }
        }

        Repaint();
    }

    private void DrawCategoriesList()
    {
        var categories = uiManager.GetAllUICategories();

        if (categories == null || categories.Count == 0)
        {
            EditorGUILayout.HelpBox("No UI categories found. Add UI elements from the Hierarchy tab.", MessageType.Info);
            return;
        }

        // Get the current view width to constrain content
        float viewWidth = EditorGUIUtility.currentViewWidth - 40; // Account for margins and scroll

        // Create a constrained container for the scroll view
        EditorGUILayout.BeginVertical(GUILayout.MaxWidth(viewWidth));

        // Start scrollable area - CRITICAL: Use explicit scroll styles
        categoriesScrollPosition = EditorGUILayout.BeginScrollView(
            categoriesScrollPosition,
            GUIStyle.none,           // Horizontal scrollbar style (none = no horizontal bar)
            GUI.skin.verticalScrollbar, // Vertical scrollbar style
            GUILayout.Height(400),
            GUILayout.MaxWidth(viewWidth)
        );

        // Apply filter
        bool hasFilter = !string.IsNullOrEmpty(filterCategoriesText);

        // Track total element count and selected count
        int totalElements = 0;
        int selectedElements = 0;

        // Draw each category within a width-constrained container
        foreach (UICategory category in categories)
        {
            // Skip empty categories if option is off
            if (!showEmptyCategories && (category.references == null || category.references.Count == 0))
                continue;

            // Apply category filter
            if (hasFilter && !category.name.Contains(filterCategoriesText, StringComparison.OrdinalIgnoreCase))
            {
                // Check if any items in this category match the filter
                bool anyItemMatches = category.references != null &&
                                      category.references.Any(r => r.name.Contains(filterCategoriesText, StringComparison.OrdinalIgnoreCase));

                if (!anyItemMatches)
                    continue;
            }

            // Draw this category with width constraint
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(viewWidth - 20));
            DrawCategoryHeader(category);

            // Get expanded state with default (first is expanded, others collapsed)
            bool isExpanded = GetCategoryExpanded(category.name);

            if (isExpanded)
            {
                DrawCategoryItems(category, hasFilter);
            }

            EditorGUILayout.EndVertical();

            // Update counts
            if (category.references != null)
            {
                totalElements += category.references.Count;
                selectedElements += category.references.Count(r => selectedUIElements.Contains(r.uiElement));
            }

            EditorGUILayout.Space(2);
        }

        // Add extra padding at bottom
        GUILayout.Space(20);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // Draw status bar below scroll view
        DrawCategoriesStatusBar(totalElements, selectedElements);
    }

    private bool GetCategoryExpanded(string categoryName)
    {
        // Return cached state or set default (true for Button/Panel, otherwise false)
        if (expandedCategories.TryGetValue(categoryName, out bool expanded))
        {
            return expanded;
        }

        // Default state: expand important categories by default
        bool defaultExpanded = categoryName == "Button" || categoryName == "Panel";
        expandedCategories[categoryName] = defaultExpanded;
        return defaultExpanded;
    }

    private void SetCategoryExpanded(string categoryName, bool expanded)
    {
        expandedCategories[categoryName] = expanded;
    }

    private void ExpandAllCategories(bool expand)
    {
        var categories = uiManager.GetAllUICategories();
        foreach (var category in categories)
        {
            expandedCategories[category.name] = expand;
        }
        Repaint();
    }

    private void DrawCategoryHeader(UICategory category)
    {
        // Get expanded state
        bool isExpanded = GetCategoryExpanded(category.name);

        // Get color for this category
        Color categoryColor = Color.white;
        if (categoryColors.TryGetValue(category.name, out Color color))
        {
            categoryColor = color;
        }

        // Create header background style with width constraint
        GUIStyle headerStyle = new GUIStyle();
        Color bgColor = isExpanded ?
            new Color(categoryColor.r * 0.3f, categoryColor.g * 0.3f, categoryColor.b * 0.3f, 0.6f) :
            new Color(categoryColor.r * 0.2f, categoryColor.g * 0.2f, categoryColor.b * 0.2f, 0.4f);
        headerStyle.normal.background = MakeColorTexture(bgColor);
        headerStyle.padding = new RectOffset(5, 5, 5, 5);
        headerStyle.stretchWidth = true;

        EditorGUILayout.BeginVertical(headerStyle);
        EditorGUILayout.BeginHorizontal();

        // Foldout arrow with constrained width
        string arrow = isExpanded ? "▼" : "▶";
        if (GUILayout.Button(arrow, EditorStyles.label, GUILayout.Width(15)))
        {
            SetCategoryExpanded(category.name, !isExpanded);
            Event.current.Use();
            Repaint();
        }

        // Category icon with constrained width
        Texture2D categoryIcon = GetCategoryIcon(category.name);
        if (categoryIcon != null)
        {
            GUILayout.Label(categoryIcon, GUILayout.Width(16), GUILayout.Height(16));
            GUILayout.Space(5);
        }

        // Category name and count - allow to expand but within constraints
        string itemCount = category.references != null ? $"({category.references.Count})" : "(0)";
        string categoryDisplayText = $"{category.name} {itemCount}";

        GUIStyle nameStyle = new GUIStyle(categoryHeaderStyle)
        {
            fontStyle = FontStyle.Bold,
            wordWrap = false // Prevent word wrapping that could cause layout issues
        };

        if (GUILayout.Button(categoryDisplayText, nameStyle, GUILayout.ExpandWidth(true)))
        {
            SetCategoryExpanded(category.name, !isExpanded);
            Event.current.Use();
            Repaint();
        }

        // Refresh button with constrained width
        if (GUILayout.Button(new GUIContent("↻", "Refresh Category"), GUILayout.Width(24), GUILayout.Height(20)))
        {
            uiManager.InitializeDictionaries();
            BuildAddedUIElementsCache();
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private Texture2D GetCategoryIcon(string categoryName)
    {
        // Try to get a Unity built-in icon for the category
        string iconName = categoryName switch
        {
            "Button" => "d_Button Icon",
            "Text" or "TMP_Text" => "d_Text Icon",
            "Toggle" => "d_Toggle Icon",
            "InputField" or "TMP_InputField" => "d_InputField Icon",
            "Slider" => "d_Slider Icon",
            "Dropdown" => "d_Dropdown Icon",
            "ScrollView" => "d_ScrollRect Icon",
            "Panel" => "d_Image Icon",
            "Image" or "RawImage" => "d_RawImage Icon",
            "Canvas" => "d_Canvas Icon",
            _ => "d_GameObject Icon"
        };

        return EditorGUIUtility.IconContent(iconName).image as Texture2D;
    }

    private void DrawCategoryItems(UICategory category, bool applyFilter)
    {
        if (category.references == null || category.references.Count == 0)
        {
            // Empty category with constrained layout
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(40);
            EditorGUILayout.LabelField("No elements in this category", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            return;
        }

        // Indent category items with width constraint
        EditorGUILayout.BeginVertical();

        // Draw each item in the category
        for (int i = 0; i < category.references.Count; i++)
        {
            UIReference reference = category.references[i];

            // Skip filtered items
            if (applyFilter && !string.IsNullOrEmpty(filterCategoriesText) &&
                !reference.name.Contains(filterCategoriesText, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Create item row with alternating background
            GUIStyle itemStyle = new GUIStyle();
            Color rowColor = (i % 2 == 0) ?
                new Color(0.22f, 0.22f, 0.22f, 0.3f) :
                new Color(0.25f, 0.25f, 0.25f, 0.2f);

            // If item is selected, highlight it
            bool isItemSelected = selectedUIElements.Contains(reference.uiElement);
            if (isItemSelected)
            {
                rowColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
            }

            // Special coloring for inactive elements
            bool isInactive = reference.uiElement != null && !reference.uiElement.activeSelf;
            if (isInactive)
            {
                rowColor = new Color(rowColor.r, rowColor.g, rowColor.b, 0.15f);
            }

            itemStyle.normal.background = MakeColorTexture(rowColor);
            itemStyle.padding = new RectOffset(5, 5, 3, 3);
            itemStyle.stretchWidth = true;

            EditorGUILayout.BeginHorizontal(itemStyle);

            // Checkbox for selection with constrained width
            bool wasSelected = selectedUIElements.Contains(reference.uiElement);
            bool isSelected = EditorGUILayout.Toggle(wasSelected, GUILayout.Width(16));

            if (wasSelected != isSelected && reference.uiElement != null)
            {
                if (isSelected)
                {
                    selectedUIElements.Add(reference.uiElement);
                    lastSelectedItemIndex = i;
                    lastSelectedCategory = category.name;
                }
                else
                {
                    selectedUIElements.Remove(reference.uiElement);
                }
            }

            // Space after checkbox
            GUILayout.Space(5);

            // Item name with click handling - expandable but constrained
            string displayName = reference.name;
            if (reference.uiElement != null && !reference.uiElement.activeSelf)
            {
                displayName = $"{displayName} (inactive)";
            }

            GUIStyle labelStyle = new GUIStyle(categoryItemStyle);
            if (isInactive)
            {
                labelStyle.normal.textColor = Color.gray;
            }
            labelStyle.wordWrap = false; // Prevent text wrapping

            // Clickable item name with text truncation
            if (GUILayout.Button(displayName, labelStyle, GUILayout.ExpandWidth(true)))
            {
                // Handle item selection with modifiers
                Event evt = Event.current;
                HandleItemSelection(reference, category.name, i, evt);
            }

            // Options button with constrained width
            if (GUILayout.Button(new GUIContent("⋮", "Options"), GUILayout.Width(18), GUILayout.Height(18)))
            {
                ShowItemContextMenu(reference);
            }

            // Select button with constrained width
            if (GUILayout.Button(new GUIContent("→", "Select in Hierarchy"), GUILayout.Width(18), GUILayout.Height(18)))
            {
                if (reference.uiElement != null)
                {
                    Selection.activeGameObject = reference.uiElement;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    // Handle multi-selection with shift/ctrl keys
    private void HandleItemSelection(UIReference reference, string categoryName, int index, Event evt)
    {
        if (reference.uiElement == null)
            return;

        // Shift key for range selection
        if (evt.shift && lastSelectedItemIndex >= 0 && lastSelectedCategory == categoryName)
        {
            // Select range
            var category = uiManager.GetAllUICategories().FirstOrDefault(c => c.name == categoryName);
            if (category == null || category.references == null)
                return;

            int startIdx = Mathf.Min(lastSelectedItemIndex, index);
            int endIdx = Mathf.Max(lastSelectedItemIndex, index);

            // Clear previous selection if not holding Ctrl
            if (!evt.control)
            {
                selectedUIElements.Clear();
            }

            // Select range
            for (int i = startIdx; i <= endIdx; i++)
            {
                if (i >= 0 && i < category.references.Count && category.references[i].uiElement != null)
                {
                    selectedUIElements.Add(category.references[i].uiElement);
                }
            }
        }
        // Ctrl key for toggle selection
        else if (evt.control)
        {
            // Toggle selection
            if (selectedUIElements.Contains(reference.uiElement))
            {
                selectedUIElements.Remove(reference.uiElement);
            }
            else
            {
                selectedUIElements.Add(reference.uiElement);
                lastSelectedItemIndex = index;
                lastSelectedCategory = categoryName;
            }
        }
        // Normal click - select just this item
        else
        {
            selectedUIElements.Clear();
            selectedUIElements.Add(reference.uiElement);
            lastSelectedItemIndex = index;
            lastSelectedCategory = categoryName;
        }

        Repaint();
    }

    private void ShowItemContextMenu(UIReference reference)
    {
        GenericMenu menu = new GenericMenu();

        // Add menu items
        menu.AddItem(new GUIContent("Select in Hierarchy"), false, () => {
            if (reference.uiElement != null)
            {
                Selection.activeGameObject = reference.uiElement;
            }
        });

        menu.AddItem(new GUIContent("Copy Path"), false, () => {
            EditorGUIUtility.systemCopyBuffer = reference.fullPath;
        });

        menu.AddItem(new GUIContent("Copy Name"), false, () => {
            EditorGUIUtility.systemCopyBuffer = reference.name;
        });

        menu.AddSeparator("");

        menu.AddItem(new GUIContent("Remove from UIManager"), false, () => {
            RemoveReference(reference);
        });

        // Show the menu
        menu.ShowAsContext();
    }

    private void RemoveReference(UIReference reference)
    {
        if (reference == null) return;

        Undo.RecordObject(uiManager, "Remove UI Reference");

        // Find and remove from category
        foreach (var category in uiManager.GetAllUICategories())
        {
            if (category.references.Contains(reference))
            {
                category.references.Remove(reference);
                break;
            }
        }

        // Remove from selected elements if present
        if (reference.uiElement != null)
        {
            selectedUIElements.Remove(reference.uiElement);
            addedUIElements.Remove(reference.uiElement);
        }

        // Update manager
        EditorUtility.SetDirty(uiManager);
        uiManager.InitializeDictionaries();
        Repaint();
    }

    private void DrawCategoriesStatusBar(int totalElements, int selectedElements)
    {
        EditorGUILayout.BeginHorizontal(statusBarStyle);

        // Element counts with constrained text
        GUIStyle statsTextStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 10,
            normal = { textColor = new Color(0.7f, 0.7f, 0.75f) },
            wordWrap = false
        };

        string statsText = $"Total: {totalElements}";
        if (selectedElements > 0)
        {
            statsText += $" | Selected: {selectedElements}";
        }

        GUILayout.Label(statsText, statsTextStyle);

        // Flexible space to push button to the right
        GUILayout.FlexibleSpace();

        // Remove selected button with constrained width
        if (selectedElements > 0)
        {
            if (GUILayout.Button("Remove Selected", GUILayout.Width(120)))
            {
                RemoveSelectedReferences();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void RemoveSelectedReferences()
    {
        if (selectedUIElements == null || selectedUIElements.Count == 0)
            return;

        bool confirm = EditorUtility.DisplayDialog(
            "Remove Selected Elements",
            $"Are you sure you want to remove {selectedUIElements.Count} selected elements from the UI Manager?",
            "Remove",
            "Cancel"
        );

        if (!confirm) return;

        Undo.RecordObject(uiManager, "Remove Selected UI References");

        // Find and remove selected references from categories
        foreach (var category in uiManager.GetAllUICategories())
        {
            if (category.references != null)
            {
                category.references.RemoveAll(r => selectedUIElements.Contains(r.uiElement));
            }
        }

        // Remove from added elements
        foreach (var element in selectedUIElements)
        {
            addedUIElements.Remove(element);
        }

        // Clear selection
        selectedUIElements.Clear();
        lastSelectedItemIndex = -1;
        lastSelectedCategory = null;

        // Update manager
        EditorUtility.SetDirty(uiManager);
        uiManager.InitializeDictionaries();
        Repaint();
    }

    #endregion
}
#endif