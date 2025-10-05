#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Smart naming assistant that tries to fix bad naming automatically,
/// but asks the user to fix hopeless cases (because they should have named things properly from the start 😉)
/// </summary>
public class SmartNamingAssistant : EditorWindow
    {
        #region Data Structures

        [Serializable]
        public class NamingAnalysis
        {
            public List<NamingFix> AutoFixed = new List<NamingFix>();
            public List<NamingConflict> Conflicts = new List<NamingConflict>();
            public List<PurposeNeeded> NeedsPurpose = new List<PurposeNeeded>();
            public List<NamingIssue> Hopeless = new List<NamingIssue>();
            public int TotalElementsAnalyzed = 0;
        }

        [Serializable]
        public class NamingFix
        {
            public GameObject Element;
            public string OriginalName;
            public string SuggestedName;
            public string Reasoning;
            public bool Applied = false;
        }

        [Serializable]
        public class NamingConflict
        {
            public List<GameObject> ConflictingElements = new List<GameObject>();
            public string ConflictingName;
            public List<string> SuggestedResolutions = new List<string>();
        }

        [Serializable]
        public class PurposeNeeded
        {
            public GameObject Element;
            public string OriginalName;
            public string PartialName; // e.g., "Login_[PURPOSE]_InputField"
            public StructuredName StructuredInfo;
            public string UserInputPurpose; // User will fill this
        }

        [Serializable]
        public class NamingIssue
        {
            public GameObject Element;
            public string Problem;
            public string Suggestion;
        }

        #endregion

        #region Private Fields

        private UIManager targetUIManager;
        private NamingAnalysis analysis;
        private Vector2 scrollPosition;

        // Professional responsive design constants (matching UIManagerEditor)
        private const float NARROW_WIDTH_THRESHOLD = 280f;
        private const float MEDIUM_WIDTH_THRESHOLD = 350f;

        // Card style for consistent UI element styling
        private GUIStyle cardStyle;

        // Simplified 2-tab UI state
        private enum TabType { FixIssues, Preview }
        private TabType currentTab = TabType.FixIssues;

        // Collapsible sections state
        private bool needsInputExpanded = true;
        private bool conflictsExpanded = true;
        private bool cantFixExpanded = false;

        // Preview mode - shows before/after for all changes
        private bool showingPreview = false;
        private List<(GameObject element, string oldName, string newName)> previewChanges = new List<(GameObject, string, string)>();
        private Dictionary<GameObject, bool> previewSelections = new Dictionary<GameObject, bool>(); // Track which changes to apply
        private Dictionary<GameObject, string> previewCustomNames = new Dictionary<GameObject, string>(); // Track custom names

        // Pagination state
        private const int MIN_ITEMS_PER_PAGE = 5;
        private const int MAX_ITEMS_PER_PAGE = 50;
        private int itemsPerPage = 10;
        private int currentPageManualFix = 0;
        private int currentPageConflicts = 0;
        private int currentPageHopeless = 0;

        #endregion

        #region Public Methods

        public static void ShowWindow(UIManager uiManager)
        {
            var window = GetWindow<SmartNamingAssistant>(true, "🎯 Smart Naming Assistant v2.0", true);
            window.targetUIManager = uiManager;

            // Set better window size and constraints
            window.position = new Rect(Screen.width / 2 - 500, Screen.height / 2 - 350, 1000, 700);
            window.minSize = new Vector2(800, 500);
            window.maxSize = new Vector2(1400, 1000);

            window.AnalyzeNaming();
            window.Show();
        }

        #endregion

        #region Professional Responsive Design (Matching UIManagerEditor)

        private ResponsiveMode GetResponsiveMode()
        {
            float width = EditorGUIUtility.currentViewWidth;
            if (width < NARROW_WIDTH_THRESHOLD) return ResponsiveMode.Narrow;
            if (width < MEDIUM_WIDTH_THRESHOLD) return ResponsiveMode.Medium;
            return ResponsiveMode.Wide;
        }

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

        private float GetResponsiveSpacing(float baseSpacing)
        {
            ResponsiveMode mode = GetResponsiveMode();
            switch (mode)
            {
                case ResponsiveMode.Narrow:
                    return baseSpacing * 0.75f;
                case ResponsiveMode.Medium:
                    return baseSpacing * 0.875f;
                default:
                    return baseSpacing;
            }
        }

        private float GetResponsivePadding(float basePadding)
        {
            ResponsiveMode mode = GetResponsiveMode();
            switch (mode)
            {
                case ResponsiveMode.Narrow:
                    return basePadding * 0.7f;
                case ResponsiveMode.Medium:
                    return basePadding * 0.85f;
                default:
                    return basePadding;
            }
        }

        #endregion 

        #region Unity Methods

        private void OnEnable()
        {
            // Don't initialize styles immediately - wait for first OnGUI call
        }

        private void OnGUI()
        {
            // Initialize styles on first GUI call when EditorStyles is ready
            if (cardStyle == null)
            {
                InitializeStyles();
            }

            if (targetUIManager == null)
            {
                EditorGUILayout.HelpBox("No UIManager selected", MessageType.Error);
                return;
            }

            if (analysis == null)
            {
                if (GUILayout.Button("Analyze Naming Issues"))
                {
                    AnalyzeNaming();
                }
                return;
            }

            DrawHeader();
            DrawAnalysisResults();
            DrawActionButtons();
        }

        #endregion

        #region GUI Drawing

        private void InitializeStyles()
        {
            // Ensure EditorStyles is ready
            if (EditorStyles.helpBox == null)
                return;

            try
            {
                // Card style for individual items (matching UIManagerEditor pattern)
                cardStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(12, 12, 8, 8),
                    margin = new RectOffset(5, 5, 2, 2),
                    normal = { background = MakeTex(1, 1, EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f, 0.5f) : new Color(0.95f, 0.95f, 0.95f, 0.8f)) }
                };
            }
            catch (System.Exception)
            {
                // If styles fail to initialize, we'll use default styles in GUI
                cardStyle = null;
            }
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        /// <summary>
        /// Draws a professional section header matching UIManagerEditor styling.
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

                float textPadding = GetResponsivePadding(12f);
                float textWidth = availableWidth - (textPadding * 2);
                descriptionHeight = descCalcStyle.CalcHeight(new GUIContent(description), textWidth);
                descriptionHeight += mode == ResponsiveMode.Narrow ? 2f : 4f;
            }

            float totalHeight = baseHeight + descriptionHeight;
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

        private void DrawHeader()
        {
            ResponsiveMode mode = GetResponsiveMode();
            float titleHeight = mode == ResponsiveMode.Narrow ? 50f : 60f;

            // Background gradient for title area (matching UIManagerEditor)
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

            // Header
            EditorGUI.LabelField(new Rect(titleRect.x, titleRect.y + titleTopPadding, titleRect.width, titleHeight_Text), "🎯 Smart Naming Assistant", titleStyle);

            // Subtitle with summary
            if (mode != ResponsiveMode.Narrow)
            {
                int linkFontSize = GetResponsiveFontSize(11, 9, 10);
                float linkTopOffset = mode == ResponsiveMode.Medium ? 32f : 38f;

                GUIStyle subtitleStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = linkFontSize,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.75f) },
                    wordWrap = true
                };

                string summary = GetCompactSummary();
                EditorGUI.LabelField(new Rect(titleRect.x + 10, titleRect.y + linkTopOffset, titleRect.width - 20, 18), summary, subtitleStyle);
            }
        }

        private void DrawAnalysisResults()
        {
            // Draw tabs
            DrawTabs();

            EditorGUILayout.Space(5);

            // Draw items per page control
            DrawItemsPerPageControl();

            EditorGUILayout.Space(5);

            // Draw content based on active tab
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case TabType.FixIssues:
                    DrawFixIssuesTabContent();
                    break;
                case TabType.Preview:
                    DrawPreviewTabContent();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();

            if (showingPreview)
            {
                // Preview mode - only show preview tab
                if (DrawTab($"👁️ PREVIEW ({previewChanges.Count} changes)", true))
                    currentTab = TabType.Preview;
            }
            else
            {
                // Normal mode - show Fix Issues tab
                int totalIssues = analysis.NeedsPurpose.Count + analysis.Conflicts.Count + analysis.Hopeless.Count;
                if (DrawTab($"🔧 FIX ISSUES ({totalIssues})", currentTab == TabType.FixIssues))
                    currentTab = TabType.FixIssues;
            }

            EditorGUILayout.EndHorizontal();
        }

        private bool DrawTab(string label, bool isActive)
        {
            GUIStyle tabStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal,
                normal = { background = isActive ? MakeTex(1, 1, new Color(0.3f, 0.5f, 0.8f, 0.5f)) : null },
                fontSize = 11
            };

            return GUILayout.Button(label, tabStyle, GUILayout.Height(30));
        }

        private void DrawItemsPerPageControl()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Items per page:", GUILayout.Width(100));

            // Input field for items per page
            string itemsPerPageStr = EditorGUILayout.TextField(itemsPerPage.ToString(), GUILayout.Width(60));
            if (int.TryParse(itemsPerPageStr, out int newValue))
            {
                itemsPerPage = Mathf.Clamp(newValue, MIN_ITEMS_PER_PAGE, MAX_ITEMS_PER_PAGE);
            }

            EditorGUILayout.LabelField($"(Min: {MIN_ITEMS_PER_PAGE}, Max: {MAX_ITEMS_PER_PAGE})", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }


        private void DrawNeedsPurposeSection()
        {
            // Professional section header
            DrawSectionHeader("⚡ Lightning Fast Fixes",
                $"{analysis.NeedsPurpose.Count} elements need purpose only - I figured out the rest!");

            // Pagination controls at top
            DrawPaginationControls(
                currentPageManualFix,
                analysis.NeedsPurpose.Count,
                (newPage) => currentPageManualFix = newPage,
                ApplyPurposeFixesOnCurrentPage,
                ApplyAllPurposeFixes,
                "Apply Fixes on This Page",
                "Apply All Quick Fixes"
            );

            EditorGUILayout.Space(GetResponsiveSpacing(5f));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.Space(GetResponsiveSpacing(8f));

            // Instructions
            EditorGUILayout.BeginVertical(cardStyle ?? EditorStyles.helpBox);
            EditorGUILayout.LabelField("🎯 Just type the PURPOSE and watch the magic happen!", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Examples: Username, Password, Email, Search, Cancel, Submit, Save...", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Get current page items
            var pagedItems = GetPagedItems(analysis.NeedsPurpose, currentPageManualFix);

            foreach (var item in pagedItems)
            {
                EditorGUILayout.BeginVertical(cardStyle ?? EditorStyles.helpBox);

                // Element header with Apply Fix button
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("📝", GUILayout.Width(20));
                EditorGUILayout.LabelField($"Element: {item.OriginalName}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                // Apply Fix button (only enabled if purpose is provided)
                GUI.enabled = !string.IsNullOrEmpty(item.UserInputPurpose);
                if (GUILayout.Button("Apply Fix", GUILayout.Width(80), GUILayout.Height(22)))
                {
                    ApplySinglePurposeFix(item);
                }
                GUI.enabled = true;

                if (GUILayout.Button("View", GUILayout.Width(50), GUILayout.Height(20)))
                {
                    Selection.activeGameObject = item.Element;
                    EditorGUIUtility.PingObject(item.Element);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Template preview
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Template:", EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField(item.PartialName, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);

                // Purpose input section
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Purpose:", EditorStyles.miniLabel, GUILayout.Width(60));

                // Larger, more prominent input field
                GUI.SetNextControlName($"Purpose_{item.Element.GetInstanceID()}");
                string newPurpose = EditorGUILayout.TextField(item.UserInputPurpose, EditorStyles.textField, GUILayout.Height(25));

                if (newPurpose != item.UserInputPurpose)
                {
                    item.UserInputPurpose = newPurpose;
                }

                EditorGUILayout.EndHorizontal();

                // Live preview
                if (!string.IsNullOrEmpty(item.UserInputPurpose))
                {
                    EditorGUILayout.Space(5);
                    var tempStructured = new StructuredName
                    {
                        Panel = item.StructuredInfo.Panel,
                        Purpose = item.UserInputPurpose,
                        ComponentType = item.StructuredInfo.ComponentType,
                        SubComponent = item.StructuredInfo.SubComponent
                    };
                    string preview = tempStructured.GetFullName();

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("✨ Preview:", EditorStyles.boldLabel, GUILayout.Width(70));
                    EditorGUILayout.LabelField(preview, EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("💭", GUILayout.Width(20));
                    EditorGUILayout.LabelField("Type a purpose to see live preview...", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawConflictsSection()
        {
            // Professional section header
            DrawSectionHeader($"⚠️ Naming Conflicts",
                $"{analysis.Conflicts.Count} groups - Multiple elements would get the same name. Manual resolution required");

            // Pagination controls at top
            DrawPaginationControls(
                currentPageConflicts,
                analysis.Conflicts.Count,
                (newPage) => currentPageConflicts = newPage,
                null, // No "Apply Page" action for conflicts
                null, // No "Apply All" action for conflicts
                "Resolve on This Page",
                "Resolve All Conflicts"
            );

            EditorGUILayout.Space(GetResponsiveSpacing(5f));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.Space(GetResponsiveSpacing(5f));

            // Explanation box
            EditorGUILayout.BeginVertical(cardStyle ?? EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔍 Why This Happens:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Multiple elements from different parts of your hierarchy would result in the same name. Only you can decide how to differentiate them.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(GetResponsiveSpacing(8f));

            // Get current page items
            var pagedConflicts = GetPagedItems(analysis.Conflicts, currentPageConflicts);

            foreach (var conflict in pagedConflicts)
            {
                EditorGUILayout.BeginVertical(cardStyle ?? EditorStyles.helpBox);

                // Conflict header
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"⚠️ Conflict: '{conflict.ConflictingName}'", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"{conflict.ConflictingElements.Count} elements", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(GetResponsiveSpacing(5f));

                // List conflicting elements with full paths and actions
                for (int i = 0; i < conflict.ConflictingElements.Count; i++)
                {
                    var element = conflict.ConflictingElements[i];

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    // Element path
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"#{i + 1}:", GUILayout.Width(30));
                    EditorGUILayout.LabelField(GetElementPath(element), EditorStyles.wordWrappedLabel);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(3);

                    // Action buttons
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("View in Hierarchy", GUILayout.Width(120)))
                    {
                        Selection.activeGameObject = element;
                        EditorGUIUtility.PingObject(element);
                    }

                    if (GUILayout.Button("Rename Now", GUILayout.Width(100)))
                    {
                        // Show rename dialog with suggestions
                        string suggestion = i < conflict.SuggestedResolutions.Count ?
                            conflict.SuggestedResolutions[i] :
                            $"{conflict.ConflictingName}_{i + 1}";
                        ShowRenameDialogWithSuggestions(element, conflict.SuggestedResolutions.ToArray());
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                EditorGUILayout.Space(GetResponsiveSpacing(5f));

                // Suggestions section
                if (conflict.SuggestedResolutions.Count > 0)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("💡 Suggested Names:", EditorStyles.boldLabel);
                    EditorGUILayout.Space(2);

                    foreach (var solution in conflict.SuggestedResolutions.Take(5))
                    {
                        EditorGUILayout.LabelField($"  • {solution}", EditorStyles.miniLabel);
                    }

                    if (conflict.SuggestedResolutions.Count > 5)
                    {
                        EditorGUILayout.LabelField($"  ... and {conflict.SuggestedResolutions.Count - 5} more variations", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(GetResponsiveSpacing(8f));
            }

            // Help section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("💡 How to Resolve:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. Click 'Rename Now' on each conflicting element", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("2. Choose from suggested names or provide your own", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("3. Click 'Re-analyze' to check if conflicts are resolved", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        private void DrawHopelessSection()
        {
            // Professional section header
            DrawSectionHeader($"🤦‍♂️ Manual Fixes Required",
                $"{analysis.Hopeless.Count} elements need your attention - These require manual naming decisions");

            // Pagination controls at top
            DrawPaginationControls(
                currentPageHopeless,
                analysis.Hopeless.Count,
                (newPage) => currentPageHopeless = newPage,
                null, // No bulk apply for hopeless cases
                null, // No bulk apply for hopeless cases
                "Work on This Page",
                "Handle All Manual Fixes"
            );

            EditorGUILayout.Space(GetResponsiveSpacing(5f));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.Space(GetResponsiveSpacing(5f));

            // Get current page items
            var pagedIssues = GetPagedItems(analysis.Hopeless, currentPageHopeless);

            foreach (var issue in pagedIssues)
            {
                EditorGUILayout.BeginHorizontal(cardStyle ?? EditorStyles.helpBox);

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"❌ {issue.Element.name}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Problem: {issue.Problem}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Suggestion: {issue.Suggestion}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUILayout.Width(120));
                if (GUILayout.Button("Select & Fix", GUILayout.Height(22)))
                {
                    Selection.activeGameObject = issue.Element;
                    EditorGUIUtility.PingObject(issue.Element);
                }
                if (GUILayout.Button("Rename Now", GUILayout.Height(22)))
                {
                    ShowRenameDialog(issue.Element, issue.Suggestion);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFixIssuesTabContent()
        {
            int totalIssues = analysis.NeedsPurpose.Count + analysis.Conflicts.Count + analysis.Hopeless.Count;

            if (totalIssues == 0)
            {
                // Check if there are auto-fixes ready
                if (analysis.AutoFixed.Count > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"✨ Great news! We found {analysis.AutoFixed.Count} elements that can be automatically renamed to follow proper naming conventions.\n\n" +
                        "👇 Click 'PREVIEW ALL CHANGES' below to review the proposed names before applying them.",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("✅ All clear! No issues found.", MessageType.Info);
                }
                return;
            }

            EditorGUILayout.LabelField($"📋 {totalIssues} issues need attention", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Section 1: Needs Input (most important - expanded by default)
            if (analysis.NeedsPurpose.Count > 0)
            {
                needsInputExpanded = DrawCollapsibleSection(
                    $"📝 NEEDS INPUT ({analysis.NeedsPurpose.Count})",
                    "Provide names for these elements",
                    needsInputExpanded,
                    () => DrawNeedsPurposeSection()
                );
                EditorGUILayout.Space(10);
            }

            // Section 2: Conflicts (important if present - expanded by default)
            if (analysis.Conflicts.Count > 0)
            {
                conflictsExpanded = DrawCollapsibleSection(
                    $"⚠️ CONFLICTS ({analysis.Conflicts.Count})",
                    "Duplicate names that need resolution",
                    conflictsExpanded,
                    () => DrawConflictsSection()
                );
                EditorGUILayout.Space(10);
            }

            // Section 3: Can't Auto-Fix (less urgent - collapsed by default)
            if (analysis.Hopeless.Count > 0)
            {
                cantFixExpanded = DrawCollapsibleSection(
                    $"❌ CAN'T AUTO-FIX ({analysis.Hopeless.Count})",
                    "These require manual fixes in the hierarchy",
                    cantFixExpanded,
                    () => DrawHopelessSection()
                );
            }
        }

        private bool DrawCollapsibleSection(string title, string description, bool isExpanded, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header with expand/collapse
            EditorGUILayout.BeginHorizontal();

            string arrow = isExpanded ? "▼" : "▶";
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            if (GUILayout.Button($"{arrow} {title}", headerStyle, GUILayout.Height(25)))
            {
                isExpanded = !isExpanded;
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(description))
            {
                GUIStyle descStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                };
                EditorGUILayout.LabelField(description, descStyle);
            }

            // Content (if expanded)
            if (isExpanded)
            {
                EditorGUILayout.Space(5);
                drawContent?.Invoke();
            }

            EditorGUILayout.EndVertical();

            return isExpanded;
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space(GetResponsiveSpacing(15f));

            bool hasAnyChanges = analysis.AutoFixed.Any() || analysis.NeedsPurpose.Any(p => !string.IsNullOrEmpty(p.UserInputPurpose));
            bool hasConflicts = analysis.Conflicts.Count > 0;
            bool hasHopeless = analysis.Hopeless.Count > 0;

            ResponsiveMode mode = GetResponsiveMode();

            // Action buttons section with professional styling
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            int headerFontSize = GetResponsiveFontSize(12, 11, 11);
            GUIStyle actionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = headerFontSize,
                normal = { textColor = new Color(0.8f, 0.8f, 0.9f) }
            };

            if (!showingPreview)
            {
                EditorGUILayout.LabelField("👀 Preview Changes First", actionHeaderStyle);
                EditorGUILayout.Space(GetResponsiveSpacing(8f));

                // Primary action: Show Preview
                if (hasAnyChanges && GUILayout.Button("👁️ PREVIEW ALL CHANGES", GUILayout.Height(40)))
                {
                    ShowPreview();
                }
            }
            else
            {
                EditorGUILayout.LabelField("🚀 Apply Changes", actionHeaderStyle);
                EditorGUILayout.Space(GetResponsiveSpacing(8f));

                // Show apply and cancel buttons
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("✅ APPLY CHANGES", GUILayout.Height(40)))
                {
                    ApplyPreviewedChanges();
                }

                if (GUILayout.Button("❌ CANCEL", GUILayout.Height(40)))
                {
                    CancelPreview();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(GetResponsiveSpacing(8f));

            // Secondary actions
            EditorGUILayout.BeginHorizontal();

            if (hasConflicts || hasHopeless)
            {
                GUI.enabled = false;
                GUILayout.Button("❌ Fix Conflicts First", EditorStyles.miniButton, GUILayout.Height(25));
                GUI.enabled = true;
            }
            else if (hasAnyChanges)
            {
                if (GUILayout.Button("🎯 Perfect! Generate Clean Code", GUILayout.Height(30)))
                    GenerateCleanCode();
            }

            GUILayout.FlexibleSpace();

            // Utility buttons
            if (mode != ResponsiveMode.Narrow)
            {
                if (GUILayout.Button("🔄 Re-analyze", EditorStyles.miniButton, GUILayout.Width(100)))
                    AnalyzeNaming();

                if (GUILayout.Button("❓ Help", EditorStyles.miniButton, GUILayout.Width(100)))
                    ShowNamingGuide();
            }

            EditorGUILayout.EndHorizontal();

            // Utility buttons for narrow mode (stacked)
            if (mode == ResponsiveMode.Narrow)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🔄 Re-analyze", EditorStyles.miniButton))
                    AnalyzeNaming();
                if (GUILayout.Button("❓ Help", EditorStyles.miniButton))
                    ShowNamingGuide();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            // Educational section with professional styling
            if (hasHopeless || hasConflicts)
            {
                EditorGUILayout.Space(GetResponsiveSpacing(10f));
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                int tipFontSize = GetResponsiveFontSize(11, 10, 10);
                GUIStyle tipStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = tipFontSize
                };

                EditorGUILayout.LabelField("💡 Pro Tip:", tipStyle);
                EditorGUILayout.LabelField("Name your UI elements meaningfully from the start to avoid this step next time!", EditorStyles.wordWrappedLabel);

                if (GUILayout.Button("Show Me Naming Best Practices", GUILayout.Height(25)))
                    ShowNamingGuide();

                EditorGUILayout.EndVertical();
            }
        }

        #endregion

        #region Analysis Logic

        private void AnalyzeNaming()
        {
            analysis = new NamingAnalysis();

            var allUIElements = FindAllUIElements();
            analysis.TotalElementsAnalyzed = allUIElements.Count;

            foreach (var element in allUIElements)
            {
                AnalyzeElement(element);
            }

            DetectConflicts();

            // Auto-show preview if ONLY auto-fixes exist (no user input needed)
            int totalIssues = analysis.NeedsPurpose.Count + analysis.Conflicts.Count + analysis.Hopeless.Count;
            if (totalIssues == 0 && analysis.AutoFixed.Count > 0)
            {
                // Jump straight to preview - no need to show "Fix Issues" tab
                ShowPreview();
            }
        }

        private List<GameObject> FindAllUIElements()
        {
            var uiElements = new List<GameObject>();

            // Find all UI components in the scene
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
                if (IsUIElement(child.gameObject))
                {
                    uiElements.Add(child.gameObject);
                }

                FindUIElementsRecursive(child, uiElements);
            }
        }

        private bool IsUIElement(GameObject obj)
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

        private void AnalyzeElement(GameObject element)
        {
            string originalName = element.name;

            // SKIP sub-components (Text/Placeholder inside Button/InputField/Toggle)
            // They will be automatically renamed when their parent is renamed
            if (IsSubComponent(element))
            {
                return; // Don't analyze sub-components separately
            }

            // Check if it's obviously bad naming
            if (IsBadlyNamed(originalName))
            {
                // Try to build structured name
                StructuredName structuredName = BuildStructuredName(element);

                if (!string.IsNullOrEmpty(structuredName.Purpose))
                {
                    // Can auto-fix completely (has purpose)
                    analysis.AutoFixed.Add(new NamingFix
                    {
                        Element = element,
                        OriginalName = originalName,
                        SuggestedName = structuredName.GetFullName(),
                        Reasoning = $"Auto-detected purpose '{structuredName.Purpose}' and applied structured naming pattern"
                    });
                }
                else if (!string.IsNullOrEmpty(structuredName.Panel) && !string.IsNullOrEmpty(structuredName.ComponentType))
                {
                    // Need user input for purpose
                    analysis.NeedsPurpose.Add(new PurposeNeeded
                    {
                        Element = element,
                        OriginalName = originalName,
                        PartialName = structuredName.GetPartialName(),
                        StructuredInfo = structuredName,
                        UserInputPurpose = "" // User will fill this
                    });
                }
                else
                {
                    // Hopeless case - can't even detect basic structure
                    analysis.Hopeless.Add(new NamingIssue
                    {
                        Element = element,
                        Problem = GetNamingProblem(originalName),
                        Suggestion = GetNamingSuggestion(element)
                    });
                }
            }
        }

        /// <summary>
        /// Checks if an element is a sub-component (Text/Placeholder inside Button/InputField/Toggle)
        /// </summary>
        private bool IsSubComponent(GameObject element)
        {
            Transform parent = element.transform.parent;
            if (parent == null) return false;

            // Check if this element is a Text or Image inside a Button/InputField/Toggle
            bool isTextOrImage = element.GetComponent<Text>() != null ||
                                element.GetComponent<TMP_Text>() != null ||
                                element.GetComponent<Image>() != null;

            if (!isTextOrImage) return false;

            // Check if parent is a Button, InputField, or Toggle
            bool parentIsInteractiveComponent = parent.GetComponent<Button>() != null ||
                                               parent.GetComponent<InputField>() != null ||
                                               parent.GetComponent<TMP_InputField>() != null ||
                                               parent.GetComponent<Toggle>() != null;

            return parentIsInteractiveComponent;
        }

        private bool IsBadlyNamed(string name)
        {
            // Check for Unity default names
            var badPatterns = new[]
            {
                @"^(Button|InputField|Text|Image|Toggle|Slider|Dropdown|ScrollRect|Panel)(\s*\(\d+\))?$",
                @"^GameObject(\s*\(\d+\))?$",
                @"^New\s+",
                @"^\s*$",
                // Also check for generic combinations like "Button_Text", "Text_Text", "Image_Text"
                @"^(Button|InputField|Image|Toggle|Slider|Dropdown|ScrollRect)_(Text|Image|Placeholder)$",
                @"^Text_Text$",
                @"^Image_Image$"
            };

            return badPatterns.Any(pattern => Regex.IsMatch(name, pattern, RegexOptions.IgnoreCase));
        }

        private string TryToFixAutomatically(GameObject element)
        {
            // Try to build the structured name: [Panel]_[Purpose]_[ComponentType]_[SubComponent]
            StructuredName structuredName = BuildStructuredName(element);

            // If we can auto-detect the purpose, return complete name
            if (!string.IsNullOrEmpty(structuredName.Purpose))
            {
                return structuredName.GetFullName();
            }

            // If we can't detect purpose, this needs user input
            return null;
        }

        private StructuredName BuildStructuredName(GameObject element)
        {
            var structured = new StructuredName();

            // Check if this is a sub-component (Text/Placeholder inside Button/InputField)
            string subComponentType = GetSubComponentName(element);

            if (!string.IsNullOrEmpty(subComponentType))
            {
                // SUB-COMPONENT: Use parent's FULL name (keep type suffix!)
                Transform parent = element.transform.parent;
                if (parent != null)
                {
                    // Check if parent already has a proper name (not default Unity name)
                    if (!IsBadlyNamed(parent.name))
                    {
                        // Parent has a good name, use it directly as the full prefix
                        structured.Panel = parent.name; // Use full parent name including type
                        structured.Purpose = null; // No separate purpose needed
                        structured.ComponentType = null; // No separate component type needed
                        structured.SubComponent = subComponentType;
                        return structured;
                    }
                }
            }

            // MAIN COMPONENT: Build structured name with minimal context

            // 1. PURPOSE (auto-detect or will need user input)
            structured.Purpose = TryToDetectPurpose(element);

            // 2. COMPONENT TYPE (100% auto-detectable)
            structured.ComponentType = GetComponentTypeName(element);

            // 3. Check if simple name is unique
            if (!string.IsNullOrEmpty(structured.Purpose))
            {
                string simpleName = $"{structured.Purpose}_{structured.ComponentType}";

                if (!HasDuplicateName(simpleName, element))
                {
                    // No duplicate! Use simple name (no context needed)
                    structured.Panel = null; // No context prefix
                    return structured;
                }
            }

            // 4. Duplicate exists OR no purpose detected - need context
            structured.Panel = FindActualPanel(element); // Already returns cleaned context

            return structured;
        }

        /// <summary>
        /// Checks if a name already exists in the scene (duplicate detection)
        /// </summary>
        private bool HasDuplicateName(string candidateName, GameObject currentElement = null)
        {
            var allElements = FindAllUIElements();

            foreach (var element in allElements)
            {
                // Skip the current element itself
                if (currentElement != null && element == currentElement)
                    continue;

                // Check if name matches
                if (element.name.Equals(candidateName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private string FindActualPanel(GameObject element)
        {
            // Look for parent panel or container to use as context
            // Returns CLEAN name (type suffix stripped) for use as context prefix

            Transform current = element.transform;

            while (current != null)
            {
                string name = current.name;
                GameObject currentObj = current.gameObject;

                // Check if this is a panel/container (ends with _Panel, _Screen, _View, etc.)
                if (name.EndsWith("_Panel") || name.EndsWith("_Screen") ||
                    name.EndsWith("_View") || name.EndsWith("_Container") ||
                    name.EndsWith("_Group") || currentObj.transform.childCount > 3)
                {
                    // Found context parent! Strip type suffix for clean context name
                    return StripTypeSuffix(name, currentObj);
                }

                // Stop at Canvas
                if (currentObj.GetComponent<Canvas>() != null)
                    break;

                current = current.parent;
            }

            return "Unknown"; // Return without type suffix
        }

        private string GetTextContent(GameObject element)
        {
            var text = element.GetComponentInChildren<Text>();
            if (text != null && !string.IsNullOrEmpty(text.text))
                return text.text;

            var tmpText = element.GetComponentInChildren<TMP_Text>();
            if (tmpText != null && !string.IsNullOrEmpty(tmpText.text))
                return tmpText.text;

            var inputField = element.GetComponent<InputField>();
            if (inputField?.placeholder != null)
            {
                var placeholderText = inputField.placeholder.GetComponent<Text>();
                if (placeholderText != null && !string.IsNullOrEmpty(placeholderText.text))
                    return placeholderText.text;
            }

            var tmpInputField = element.GetComponent<TMP_InputField>();
            if (tmpInputField?.placeholder != null)
            {
                var placeholderText = tmpInputField.placeholder.GetComponent<TMP_Text>();
                if (placeholderText != null && !string.IsNullOrEmpty(placeholderText.text))
                    return placeholderText.text;
            }

            return null;
        }

        private string GetElementContentHint(GameObject element)
        {
            string content = GetTextContent(element);
            if (!string.IsNullOrEmpty(content) && IsValidForNaming(content))
            {
                return SanitizeIdentifier(content);
            }
            return null;
        }

        private bool IsValidForNaming(string text)
        {
            // Text is valid for naming if it's not too long, not dynamic, and meaningful
            return text.Length <= 20 &&
                   text.Length >= 2 &&
                   !Regex.IsMatch(text, @"\d{3,}|%|\$|@|\{|\}") && // No dynamic content
                   !text.Contains("...") &&
                   Regex.IsMatch(text, @"^[a-zA-Z]"); // Starts with letter
        }

        private string GetMeaningfulParentName(GameObject element)
        {
            Transform parent = element.transform.parent;

            while (parent != null)
            {
                string parentName = parent.name;

                // Remove Unity numbering
                parentName = Regex.Replace(parentName, @"\s*\(\d+\)$", "");

                // Check if it's meaningful (not default Unity names)
                if (!IsBadlyNamed(parentName) && parentName.Length > 2)
                {
                    return SanitizeForIdentifier(parentName);
                }

                parent = parent.parent;
            }

            return null;
        }

        private string GetElementTypeSuffix(GameObject element)
        {
            if (element.GetComponent<Button>()) return "Button";
            if (element.GetComponent<InputField>() || element.GetComponent<TMP_InputField>()) return "Field";
            if (element.GetComponent<Text>() || element.GetComponent<TMP_Text>()) return "Text";
            if (element.GetComponent<Image>()) return "Image";
            if (element.GetComponent<Toggle>()) return "Toggle";
            if (element.GetComponent<Slider>()) return "Slider";
            if (element.GetComponent<Dropdown>() || element.GetComponent<TMP_Dropdown>()) return "Dropdown";
            if (element.GetComponent<ScrollRect>()) return "ScrollView";

            return "Element";
        }

        private string GetSiblingContext(GameObject element)
        {
            Transform parent = element.transform.parent;
            if (parent == null) return null;

            var siblings = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                var sibling = parent.GetChild(i);
                if (IsUIElement(sibling.gameObject) && sibling.GetComponent<Text>() == null) // Skip text labels
                {
                    siblings.Add(sibling);
                }
            }

            int index = siblings.IndexOf(element.transform);
            if (index == 0 && siblings.Count > 1) return "Primary";
            if (index == 1 && siblings.Count > 2) return "Secondary";
            if (index == siblings.Count - 1 && siblings.Count > 1) return "Last";

            return index > 0 ? $"{index + 1}" : null;
        }

        private string GetPositionalName(GameObject element)
        {
            string elementType = GetElementTypeSuffix(element);
            int siblingIndex = element.transform.GetSiblingIndex();

            return $"{elementType}_{siblingIndex + 1}";
        }

        private string SanitizeForIdentifier(string input)
        {
            // Remove special characters and make valid C# identifier
            string sanitized = Regex.Replace(input, @"[^a-zA-Z0-9]", " ");
            sanitized = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(sanitized.ToLower());
            sanitized = Regex.Replace(sanitized, @"\s+", "");

            // Ensure it starts with a letter
            if (!char.IsLetter(sanitized[0]))
            {
                sanitized = "Element" + sanitized;
            }

            return sanitized;
        }

        private void DetectConflicts()
        {
            // STEP 1: Check for existing duplicate names in the scene (like the validation does)
            var allUIElements = FindAllUIElements();
            var existingDuplicateGroups = allUIElements
                .GroupBy(e => e.name)
                .Where(g => g.Count() > 1)
                .ToList();

            // Try to auto-fix existing duplicates with parent prefixes
            foreach (var duplicateGroup in existingDuplicateGroups)
            {
                var elementsToAutoFix = new List<GameObject>();
                var elementsNeedingManualFix = new List<GameObject>();
                var usedNames = new HashSet<string>(allUIElements.Select(e => e.name), StringComparer.OrdinalIgnoreCase);

                // CRITICAL FIX: When we have duplicates, ALL of them need Panel context, including the first one!
                // Remove the duplicate name from usedNames so all elements get renamed with context
                usedNames.Remove(duplicateGroup.Key);

                foreach (var element in duplicateGroup)
                {
                    string autoFixName = TryGenerateUniqueNameWithContext(element, duplicateGroup.Key, usedNames);

                    if (!string.IsNullOrEmpty(autoFixName))
                    {
                        // We can auto-fix this one!
                        var autoFix = new NamingFix
                        {
                            Element = element,
                            OriginalName = element.name,
                            SuggestedName = autoFixName,
                            Reasoning = GetAutoFixReasoning(element, autoFixName)
                        };

                        analysis.AutoFixed.Add(autoFix);
                        usedNames.Add(autoFixName); // Reserve this name
                        elementsToAutoFix.Add(element);
                    }
                    else
                    {
                        // Needs manual intervention
                        elementsNeedingManualFix.Add(element);
                    }
                }

                // Only create conflicts for elements we couldn't auto-fix
                if (elementsNeedingManualFix.Count > 1)
                {
                    var conflict = new NamingConflict
                    {
                        ConflictingName = duplicateGroup.Key,
                        ConflictingElements = elementsNeedingManualFix,
                        SuggestedResolutions = new List<string>()
                    };

                    // Generate manual suggestions for remaining conflicts
                    foreach (var element in elementsNeedingManualFix)
                    {
                        string parentContext = GetMeaningfulParentName(element);
                        string contentHint = GetElementContentHint(element);

                        if (!string.IsNullOrEmpty(parentContext))
                        {
                            conflict.SuggestedResolutions.Add($"{parentContext}_{duplicateGroup.Key}");
                        }
                        if (!string.IsNullOrEmpty(contentHint))
                        {
                            conflict.SuggestedResolutions.Add($"{contentHint}_{duplicateGroup.Key}");
                        }
                    }

                    // Add fallback suggestions
                    if (conflict.SuggestedResolutions.Count == 0)
                    {
                        for (int i = 1; i <= elementsNeedingManualFix.Count; i++)
                        {
                            conflict.SuggestedResolutions.Add($"{duplicateGroup.Key}_{i}");
                        }
                    }

                    analysis.Conflicts.Add(conflict);
                }
            }

            // STEP 2: Get remaining existing names (not in duplicates)
            var nonDuplicateNames = allUIElements
                .Where(e => !existingDuplicateGroups.Any(g => g.Contains(e)))
                .Where(e => !analysis.AutoFixed.Any(f => f.Element == e))
                .Select(e => e.name)
                .ToHashSet();

            // STEP 3: Check for conflicts within AutoFixed suggestions (CRITICAL FOR YOUR SCENARIO)
            // This handles cases like "Menu_Panel/Button" and "Button" both becoming "Menu_Panel_Button"
            var nameGroups = analysis.AutoFixed
                .GroupBy(f => f.SuggestedName)
                .Where(g => g.Count() > 1);

            foreach (var group in nameGroups)
            {
                // These elements would all get the same name - definitely needs manual intervention
                CreateConflictForGroup(group);
            }

            // STEP 4: Check for conflicts with existing non-duplicate names
            foreach (var fix in analysis.AutoFixed.ToList()) // ToList to avoid modification during enumeration
            {
                if (nonDuplicateNames.Contains(fix.SuggestedName))
                {
                    // This suggested name conflicts with an existing element
                    var conflict = new NamingConflict
                    {
                        ConflictingName = fix.SuggestedName,
                        ConflictingElements = new List<GameObject> { fix.Element }
                    };

                    // Generate resolution suggestions
                    string parentContext = GetMeaningfulParentName(fix.Element);
                    if (!string.IsNullOrEmpty(parentContext))
                    {
                        conflict.SuggestedResolutions.Add($"{parentContext}_{fix.SuggestedName}");
                    }
                    conflict.SuggestedResolutions.Add($"{fix.SuggestedName}_New");
                    conflict.SuggestedResolutions.Add($"{fix.SuggestedName}_2");

                    analysis.Conflicts.Add(conflict);
                    analysis.AutoFixed.Remove(fix);
                }
            }

            // STEP 5: NEW - Check if auto-fixed elements would conflict with each other after applying
            // This is specifically for your scenario where multiple elements from different hierarchies
            // would get the same suggested name
            CheckForPostFixConflicts(allUIElements);
        }

        /// <summary>
        /// Checks if applying auto-fixes would create new conflicts
        /// </summary>
        private void CheckForPostFixConflicts(List<GameObject> allUIElements)
        {
            // Create a set of all names that will exist after auto-fixes are applied
            var futureNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add all unchanged element names
            foreach (var element in allUIElements)
            {
                if (!analysis.AutoFixed.Any(f => f.Element == element))
                {
                    futureNames.Add(element.name);
                }
            }

            // Now check each auto-fix to see if it would create a conflict
            var conflictingFixes = new Dictionary<string, List<NamingFix>>();

            foreach (var fix in analysis.AutoFixed.ToList())
            {
                // Check if this name already exists in future state
                if (futureNames.Contains(fix.SuggestedName))
                {
                    // Would create a conflict!
                    if (!conflictingFixes.ContainsKey(fix.SuggestedName))
                    {
                        conflictingFixes[fix.SuggestedName] = new List<NamingFix>();
                    }
                    conflictingFixes[fix.SuggestedName].Add(fix);
                }
                else
                {
                    // This name is safe, add it to future names
                    futureNames.Add(fix.SuggestedName);
                }
            }

            // Convert conflicting fixes to conflicts that need manual resolution
            foreach (var kvp in conflictingFixes)
            {
                var conflict = new NamingConflict
                {
                    ConflictingName = kvp.Key,
                    ConflictingElements = kvp.Value.Select(f => f.Element).ToList(),
                    SuggestedResolutions = new List<string>()
                };

                // Generate unique suggestions for each element
                for (int i = 0; i < conflict.ConflictingElements.Count; i++)
                {
                    var element = conflict.ConflictingElements[i];

                    // Try to use hierarchical context
                    string fullPath = GetElementPath(element);
                    var pathParts = fullPath.Split('/');

                    // Use more hierarchy context for differentiation
                    if (pathParts.Length > 1)
                    {
                        conflict.SuggestedResolutions.Add($"{pathParts[pathParts.Length - 2]}_{kvp.Key}");
                    }

                    // Try content-based differentiation
                    string contentHint = GetElementContentHint(element);
                    if (!string.IsNullOrEmpty(contentHint))
                    {
                        conflict.SuggestedResolutions.Add($"{contentHint}_{kvp.Key}");
                    }

                    // Numbered fallback
                    conflict.SuggestedResolutions.Add($"{kvp.Key}_{i + 1}");
                }

                // Remove duplicates from suggestions
                conflict.SuggestedResolutions = conflict.SuggestedResolutions.Distinct().ToList();

                analysis.Conflicts.Add(conflict);

                // Remove these from auto-fixed list since they need manual intervention
                foreach (var fix in kvp.Value)
                {
                    analysis.AutoFixed.Remove(fix);
                }
            }
        }

        private void CreateConflictForGroup(IGrouping<string, NamingFix> group)
        {
            var conflict = new NamingConflict
            {
                ConflictingName = group.Key,
                ConflictingElements = group.Select(f => f.Element).ToList()
            };

            // Generate resolution suggestions
            for (int i = 0; i < conflict.ConflictingElements.Count; i++)
            {
                var element = conflict.ConflictingElements[i];
                string parentContext = GetMeaningfulParentName(element);

                if (!string.IsNullOrEmpty(parentContext))
                {
                    conflict.SuggestedResolutions.Add($"{parentContext}_{group.Key}");
                }
                else
                {
                    conflict.SuggestedResolutions.Add($"{group.Key}_{i + 1}");
                }
            }

            analysis.Conflicts.Add(conflict);

            // Remove from auto-fixed since they need manual resolution
            analysis.AutoFixed.RemoveAll(f => f.SuggestedName == group.Key);
        }

        #endregion

        #region Pagination Helper Methods

        private void DrawPaginationControls(int currentPage, int totalItems, System.Action<int> onPageChanged, System.Action onApplyPage, System.Action onApplyAll, string applyPageLabel = "Fix This Page", string applyAllLabel = "Fix All")
        {
            int totalPages = Mathf.CeilToInt((float)totalItems / itemsPerPage);
            if (totalPages <= 0) totalPages = 1;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Pagination navigation
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"Showing {Mathf.Min(currentPage * itemsPerPage + 1, totalItems)}-{Mathf.Min((currentPage + 1) * itemsPerPage, totalItems)} of {totalItems}", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            GUI.enabled = currentPage > 0;
            if (GUILayout.Button("<", GUILayout.Width(30)))
            {
                onPageChanged(currentPage - 1);
            }
            GUI.enabled = true;

            EditorGUILayout.LabelField($"{currentPage + 1} of {totalPages}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(60));

            GUI.enabled = currentPage < totalPages - 1;
            if (GUILayout.Button(">", GUILayout.Width(30)))
            {
                onPageChanged(currentPage + 1);
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            // Only show action buttons if callbacks are provided
            if (onApplyPage != null || onApplyAll != null)
            {
                EditorGUILayout.Space(5);

                // Action buttons
                EditorGUILayout.BeginHorizontal();

                if (onApplyPage != null && GUILayout.Button(applyPageLabel, GUILayout.Height(30)))
                {
                    onApplyPage();
                }

                if (onApplyAll != null && GUILayout.Button(applyAllLabel, GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Confirm", $"Apply fixes to all {totalItems} items?", "Yes", "No"))
                    {
                        onApplyAll();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private List<T> GetPagedItems<T>(List<T> items, int currentPage)
        {
            int startIndex = currentPage * itemsPerPage;
            int count = Mathf.Min(itemsPerPage, items.Count - startIndex);

            if (startIndex >= items.Count)
                return new List<T>();

            return items.GetRange(startIndex, count);
        }

        #endregion

        #region Helper Methods

        private string GetCompactSummary()
        {
            if (analysis == null)
                return "Ready to analyze your UI elements";

            int total = analysis.TotalElementsAnalyzed;
            int autoFixed = analysis.AutoFixed.Count;
            int conflicts = analysis.Conflicts.Count;
            int hopeless = analysis.Hopeless.Count;

            if (autoFixed == total && total > 0)
                return $"Perfect! {autoFixed} elements auto-fixed and ready to apply";
            else if (hopeless == 0 && conflicts == 0 && autoFixed > 0)
                return $"{autoFixed} auto-fixes ready - Just review and apply";
            else if (hopeless > 0 || conflicts > 0)
                return $"Analyzed {total} elements - {autoFixed} auto-fixed, {conflicts + hopeless} need attention";
            else
                return $"Analyzed {total} UI elements";
        }

        private string GetPassiveAggressiveSummary()
        {
            int total = analysis.TotalElementsAnalyzed;
            int autoFixed = analysis.AutoFixed.Count;
            int conflicts = analysis.Conflicts.Count;
            int hopeless = analysis.Hopeless.Count;

            string summary = $"Analyzed {total} UI elements:\n";

            if (autoFixed > 0)
            {
                summary += $"✅ I managed to fix {autoFixed} elements automatically (because I'm helpful)\n";
            }

            if (conflicts > 0)
            {
                summary += $"⚠️ Found {conflicts} naming conflicts that need your input\n";
            }

            if (hopeless > 0)
            {
                summary += $"🤦‍♂️ Found {hopeless} elements that need your attention (you should have named these properly from the start)\n";
            }

            if (autoFixed == total)
            {
                summary += "\n🎉 Perfect! I fixed everything. You can generate clean code now!";
            }
            else if (hopeless == 0 && conflicts == 0)
            {
                summary += "\n😊 Not bad! Just approve the fixes and you're good to go.";
            }
            else
            {
                summary += "\n😐 Some manual work needed. Let's get this sorted out.";
            }

            return summary;
        }

        private string GetFixReasoning(GameObject element, string suggestedName)
        {
            string textContent = GetTextContent(element);
            if (!string.IsNullOrEmpty(textContent))
            {
                return $"from text '{textContent}'";
            }

            string parentContext = GetMeaningfulParentName(element);
            if (!string.IsNullOrEmpty(parentContext))
            {
                return $"from parent '{parentContext}'";
            }

            return "from position";
        }

        private string GetNamingProblem(string name)
        {
            if (Regex.IsMatch(name, @"^(Button|InputField|Text|Image)(\s*\(\d+\))?$"))
            {
                return "Using default Unity component name";
            }

            if (name.Contains("("))
            {
                return "Unity auto-numbered duplicate";
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return "Empty or whitespace name";
            }

            return "Generic or meaningless name";
        }

        private string GetNamingSuggestion(GameObject element)
        {
            string elementType = GetElementTypeSuffix(element);

            var suggestions = new List<string>();

            if (element.GetComponent<Button>())
            {
                suggestions.Add($"What does this button do? (e.g., PlayButton, SettingsButton, QuitButton)");
            }
            else if (element.GetComponent<InputField>() || element.GetComponent<TMP_InputField>())
            {
                suggestions.Add($"What input is this for? (e.g., UsernameField, PasswordField, EmailField)");
            }
            else if (element.GetComponent<Text>() || element.GetComponent<TMP_Text>())
            {
                suggestions.Add($"What does this text show? (e.g., ScoreText, TitleText, StatusText)");
            }
            else
            {
                suggestions.Add($"Give it a meaningful name describing its purpose");
            }

            return suggestions.First();
        }

        private string GetElementPath(GameObject element)
        {
            Transform current = element.transform;
            var pathParts = new List<string>();

            while (current != null && current.GetComponent<Canvas>() == null)
            {
                pathParts.Insert(0, current.name);
                current = current.parent;
            }

            return string.Join("/", pathParts);
        }

        private string SanitizeIdentifier(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "Unnamed";

            // Replace any character that is not a letter, digit, or underscore with an underscore.
            string sanitized = System.Text.RegularExpressions.Regex.Replace(input, @"[^a-zA-Z0-9_]", "_");

            // Replace multiple underscores with a single underscore.
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"_+", "_");

            // Trim any leading or trailing underscores.
            sanitized = sanitized.Trim('_');

            // If the first character is a digit, prefix with an underscore.
            if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
            {
                sanitized = "_" + sanitized;
            }

            // Fallback in case the string is empty after sanitization.
            return string.IsNullOrEmpty(sanitized) ? "Unnamed" : sanitized;
        }

        private string TryGenerateUniqueNameWithContext(GameObject element, string baseName, HashSet<string> usedNames)
        {
            // Try panel context first (using the new structured naming)
            string panelContext = FindActualPanel(element);
            if (!string.IsNullOrEmpty(panelContext) && panelContext != "Unknown_Panel")
            {
                string panelBasedName = $"{panelContext}_{baseName}";
                if (!usedNames.Contains(panelBasedName))
                {
                    return panelBasedName;
                }
            }

            // Try content hint
            string contentHint = GetElementContentHint(element);
            if (!string.IsNullOrEmpty(contentHint))
            {
                string contentBasedName = $"{contentHint}_{baseName}";
                if (!usedNames.Contains(contentBasedName))
                {
                    return contentBasedName;
                }
            }

            // Try sibling context
            string siblingContext = GetSiblingContext(element);
            if (!string.IsNullOrEmpty(siblingContext))
            {
                string siblingBasedName = $"{siblingContext}_{baseName}";
                if (!usedNames.Contains(siblingBasedName))
                {
                    return siblingBasedName;
                }
            }

            // Try positional naming
            string positionalName = GetPositionalName(element);
            if (!string.IsNullOrEmpty(positionalName) && positionalName != baseName)
            {
                if (!usedNames.Contains(positionalName))
                {
                    return positionalName;
                }
            }

            // Last resort: numbered suffix
            for (int i = 1; i <= 10; i++)
            {
                string numberedName = $"{baseName}_{i}";
                if (!usedNames.Contains(numberedName))
                {
                    return numberedName;
                }
            }

            return null; // Couldn't generate a unique name
        }

        private string GetAutoFixReasoning(GameObject element, string suggestedName)
        {
            string parentContext = GetMeaningfulParentName(element);
            string contentHint = GetElementContentHint(element);

            if (!string.IsNullOrEmpty(parentContext) && suggestedName.StartsWith(parentContext))
            {
                return $"Added parent context '{parentContext}' to resolve naming conflict";
            }
            else if (!string.IsNullOrEmpty(contentHint) && suggestedName.StartsWith(contentHint))
            {
                return $"Used content hint '{contentHint}' to create unique name";
            }
            else if (suggestedName.Contains("_"))
            {
                return "Added contextual prefix to resolve naming conflict";
            }
            else
            {
                return "Applied smart naming rules to resolve conflict";
            }
        }

        private void ShowRenameDialog(GameObject element, string suggestion)
        {
            ShowRenameDialogWithSuggestions(element, new string[] { suggestion });
        }

        /// <summary>
        /// Shows a rename dialog with multiple suggestions for the user to choose from
        /// </summary>
        private void ShowRenameDialogWithSuggestions(GameObject element, string[] suggestions)
        {
            if (suggestions == null || suggestions.Length == 0)
            {
                suggestions = new string[] { element.name + "_New" };
            }

            // Create a window for better UX
            RenameElementWindow.ShowWindow(element, suggestions, () => {
                // Re-analyze after rename
                AnalyzeNaming();
                Repaint();
            });
        }

        /// <summary>
        /// Simple rename element window for conflict resolution
        /// </summary>
        public class RenameElementWindow : EditorWindow
        {
            private GameObject targetElement;
            private string[] suggestions;
            private string customName;
            private int selectedSuggestionIndex = -1;
            private System.Action onRenameComplete;
            private Vector2 scrollPosition;

            public static void ShowWindow(GameObject element, string[] suggestions, System.Action onComplete)
            {
                var window = GetWindow<RenameElementWindow>(true, "Rename UI Element", true);
                window.targetElement = element;
                window.suggestions = suggestions;
                window.customName = element.name;
                window.onRenameComplete = onComplete;
                window.selectedSuggestionIndex = -1;

                window.position = new Rect(
                    Screen.width / 2 - 250,
                    Screen.height / 2 - 200,
                    500,
                    400
                );
                window.minSize = new Vector2(400, 300);
                window.maxSize = new Vector2(600, 600);
                window.Show();
            }

            private void OnGUI()
            {
                if (targetElement == null)
                {
                    EditorGUILayout.HelpBox("Target element is null", MessageType.Error);
                    if (GUILayout.Button("Close"))
                        Close();
                    return;
                }

                EditorGUILayout.Space(10);

                // Header
                GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
                EditorGUILayout.LabelField("Rename UI Element", headerStyle);
                EditorGUILayout.Space(10);

                // Current name
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Current Name:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(targetElement.name, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10);

                // Suggestions
                if (suggestions != null && suggestions.Length > 0)
                {
                    EditorGUILayout.LabelField("💡 Suggested Names (Click to Select):", EditorStyles.boldLabel);
                    EditorGUILayout.Space(5);

                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));

                    for (int i = 0; i < suggestions.Length; i++)
                    {
                        bool isSelected = selectedSuggestionIndex == i;

                        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                        {
                            alignment = TextAnchor.MiddleLeft,
                            normal = { background = isSelected ? MakeTex(1, 1, new Color(0.3f, 0.5f, 0.8f, 0.5f)) : null }
                        };

                        if (GUILayout.Button($"  {(isSelected ? "✓" : "•")} {suggestions[i]}", buttonStyle, GUILayout.Height(25)))
                        {
                            selectedSuggestionIndex = i;
                            customName = suggestions[i];
                        }
                    }

                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.Space(10);
                }

                // Custom name input
                EditorGUILayout.LabelField("✏️ Or Enter Custom Name:", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                GUI.SetNextControlName("CustomNameField");
                string newCustomName = EditorGUILayout.TextField(customName, GUILayout.Height(25));
                if (newCustomName != customName)
                {
                    customName = newCustomName;
                    selectedSuggestionIndex = -1; // Deselect suggestion if user types custom name
                }

                EditorGUILayout.Space(10);

                // Preview
                if (!string.IsNullOrEmpty(customName) && customName != targetElement.name)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Preview:", EditorStyles.boldLabel, GUILayout.Width(60));
                    EditorGUILayout.LabelField(customName, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(10);

                // Action buttons
                EditorGUILayout.BeginHorizontal();

                GUI.enabled = !string.IsNullOrEmpty(customName) && customName != targetElement.name;
                if (GUILayout.Button("Apply Rename", GUILayout.Height(35)))
                {
                    ApplyRename();
                }
                GUI.enabled = true;

                if (GUILayout.Button("Cancel", GUILayout.Height(35)))
                {
                    Close();
                }

                EditorGUILayout.EndHorizontal();
            }

            private void ApplyRename()
            {
                if (targetElement != null && !string.IsNullOrEmpty(customName) && customName != targetElement.name)
                {
                    Undo.RecordObject(targetElement, "Rename UI Element");
                    targetElement.name = customName;
                    EditorUtility.SetDirty(targetElement);

                    onRenameComplete?.Invoke();
                    Close();
                }
            }

            private Texture2D MakeTex(int width, int height, Color col)
            {
                Color[] pix = new Color[width * height];
                for (int i = 0; i < pix.Length; i++)
                    pix[i] = col;

                Texture2D result = new Texture2D(width, height);
                result.SetPixels(pix);
                result.Apply();
                return result;
            }
        }


        private void ApplySinglePurposeFix(PurposeNeeded purposeItem)
        {
            if (purposeItem == null || purposeItem.Element == null || string.IsNullOrEmpty(purposeItem.UserInputPurpose))
                return;

            // Update the structured name with user-provided purpose
            purposeItem.StructuredInfo.Purpose = SanitizeIdentifier(purposeItem.UserInputPurpose);
            string finalName = purposeItem.StructuredInfo.GetFullName();

            Undo.RecordObject(purposeItem.Element, "Apply Purpose-Based Name Fix");
            purposeItem.Element.name = finalName;
            EditorUtility.SetDirty(purposeItem.Element);

            // Automatically rename children
            RenameChildrenBasedOnParent(purposeItem.Element);

            // Update UIManager references
            RefreshUIManagerReferences();

            // Re-analyze to update the UI
            AnalyzeNaming();
            Repaint();
        }

        private void ApplyPurposeFixesOnCurrentPage()
        {
            var pagedItems = GetPagedItems(analysis.NeedsPurpose, currentPageManualFix);
            int appliedCount = 0;

            foreach (var purposeItem in pagedItems)
            {
                if (!string.IsNullOrEmpty(purposeItem.UserInputPurpose) && purposeItem.Element != null)
                {
                    // Update the structured name with user-provided purpose
                    purposeItem.StructuredInfo.Purpose = SanitizeIdentifier(purposeItem.UserInputPurpose);
                    string finalName = purposeItem.StructuredInfo.GetFullName();

                    Undo.RecordObject(purposeItem.Element, "Apply Purpose-Based Name Fix");
                    purposeItem.Element.name = finalName;
                    EditorUtility.SetDirty(purposeItem.Element);

                    // Automatically rename children
                    RenameChildrenBasedOnParent(purposeItem.Element);

                    appliedCount++;
                }
            }

            if (appliedCount > 0)
            {
                // Update UIManager references
                RefreshUIManagerReferences();

                EditorUtility.DisplayDialog("Fixes Applied",
                    $"Applied {appliedCount} purpose-based fixes on this page.",
                    "OK");

                // Re-analyze to update the UI
                AnalyzeNaming();
            }
            else
            {
                EditorUtility.DisplayDialog("No Fixes to Apply",
                    "Please provide purposes for the elements before applying fixes.",
                    "OK");
            }
        }

        private void ApplyAllPurposeFixes()
        {
            int appliedCount = 0;

            foreach (var purposeItem in analysis.NeedsPurpose)
            {
                if (!string.IsNullOrEmpty(purposeItem.UserInputPurpose) && purposeItem.Element != null)
                {
                    // Update the structured name with user-provided purpose
                    purposeItem.StructuredInfo.Purpose = SanitizeIdentifier(purposeItem.UserInputPurpose);
                    string finalName = purposeItem.StructuredInfo.GetFullName();

                    Undo.RecordObject(purposeItem.Element, "Apply Purpose-Based Name Fix");
                    purposeItem.Element.name = finalName;
                    EditorUtility.SetDirty(purposeItem.Element);

                    // Automatically rename children
                    RenameChildrenBasedOnParent(purposeItem.Element);

                    appliedCount++;
                }
            }

            if (appliedCount > 0)
            {
                // Update UIManager references
                RefreshUIManagerReferences();

                EditorUtility.DisplayDialog("Purpose-Based Fixes Applied",
                    $"Applied {appliedCount} purpose-based name fixes.\n\n" +
                    "Your UI elements now follow the perfect naming convention:\n" +
                    "[Panel]_[Purpose]_[ComponentType]_[SubComponent]",
                    "Perfect!");

                // Re-analyze to check if there are still issues
                AnalyzeNaming();
            }
            else
            {
                EditorUtility.DisplayDialog("No Fixes to Apply",
                    "Please provide purposes for the elements before applying fixes.",
                    "OK");
            }
        }

        /// <summary>
        /// Refreshes the UIManager's internal references after renaming elements
        /// </summary>
        private void RefreshUIManagerReferences()
        {
            if (targetUIManager == null) return;

            // Mark the UIManager as dirty so changes will be saved
            Undo.RecordObject(targetUIManager, "Refresh UIManager References");

            // Use reflection to access the private uiCategories field
            var uiManagerType = targetUIManager.GetType();
            var uiCategoriesField = uiManagerType.GetField("uiCategories",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (uiCategoriesField != null)
            {
                var uiCategories = uiCategoriesField.GetValue(targetUIManager) as List<UICategory>;

                if (uiCategories != null)
                {
                    // Iterate through all categories and update references
                    foreach (var category in uiCategories)
                    {
                        foreach (var reference in category.references)
                        {
                            // Update the name and fullPath based on the actual GameObject
                            if (reference.uiElement != null)
                            {
                                reference.name = reference.uiElement.name;
                                reference.fullPath = GetFullPath(reference.uiElement.transform);
                            }
                        }
                    }

                    // Reinitialize dictionaries to update the runtime lookups
                    targetUIManager.InitializeDictionaries();
                }
            }

            EditorUtility.SetDirty(targetUIManager);
        }

        /// <summary>
        /// Gets the full hierarchical path of a transform
        /// </summary>
        private string GetFullPath(Transform transform)
        {
            if (transform == null) return "";

            string path = transform.name;
            Transform parent = transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        /// <summary>
        /// Automatically renames all children of a parent element based on the parent's new name
        /// </summary>
        private void RenameChildrenBasedOnParent(GameObject parent)
        {
            if (parent == null) return;

            // Check if this parent can have sub-components (Button, InputField, Toggle)
            bool isButton = parent.GetComponent<Button>() != null;
            bool isInputField = parent.GetComponent<InputField>() != null || parent.GetComponent<TMP_InputField>() != null;
            bool isToggle = parent.GetComponent<Toggle>() != null;

            if (!isButton && !isInputField && !isToggle)
                return; // This parent doesn't have sub-components to rename

            // Iterate through all direct children
            foreach (Transform child in parent.transform)
            {
                GameObject childObj = child.gameObject;

                // Determine the sub-component type
                string subComponentType = null;

                if (isButton)
                {
                    if (childObj.GetComponent<Text>() != null || childObj.GetComponent<TMP_Text>() != null)
                        subComponentType = "Text";
                    else if (childObj.GetComponent<Image>() != null)
                        subComponentType = "Image";
                }
                else if (isInputField)
                {
                    if (childObj.name.ToLower().Contains("placeholder"))
                        subComponentType = "Placeholder";
                    else if (childObj.GetComponent<Text>() != null || childObj.GetComponent<TMP_Text>() != null)
                        subComponentType = "Text";
                }
                else if (isToggle)
                {
                    if (childObj.GetComponent<Text>() != null || childObj.GetComponent<TMP_Text>() != null)
                        subComponentType = "Label";
                    else if (childObj.name.ToLower().Contains("background"))
                        subComponentType = "Background";
                    else if (childObj.name.ToLower().Contains("checkmark"))
                        subComponentType = "Checkmark";
                }

                // If we found a sub-component, rename it
                if (!string.IsNullOrEmpty(subComponentType))
                {
                    string newChildName = $"{parent.name}_{subComponentType}";

                    // Only rename if the name is different
                    if (childObj.name != newChildName)
                    {
                        Undo.RecordObject(childObj, "Auto-Rename Child Element");
                        childObj.name = newChildName;
                        EditorUtility.SetDirty(childObj);
                    }
                }
            }
        }

        private void GenerateCleanCode()
        {
            Close();

            // Trigger code generation in UIManager
            if (targetUIManager != null)
            {
                EditorUtility.DisplayDialog("Ready for Code Generation",
                    "✅ All naming issues resolved!\n\n" +
                    "You can now generate clean, collision-free code with meaningful names.\n\n" +
                    "Go to the Library Generation tab in your UIManager inspector.",
                    "Let's Do It!");
            }
        }

        private void ShowNamingGuide()
        {
            EditorUtility.DisplayDialog("UI Naming Best Practices",
                "📝 Quick Guide to Better UI Naming:\n\n" +
                "✅ Be descriptive: 'PlayButton' not 'Button'\n" +
                "✅ Include purpose: 'UsernameField' not 'InputField'\n" +
                "✅ Use context: 'Login_SubmitButton' for clarity\n" +
                "✅ Be consistent: Pick a convention and stick to it\n\n" +
                "❌ Avoid generic names like 'Button', 'Text', 'Panel'\n" +
                "❌ Don't rely on Unity's auto-numbering\n" +
                "❌ Don't use special characters or spaces\n\n" +
                "Follow these rules and you'll never see this dialog again! 😉",
                "Got It!");
        }

        #endregion

        #region Structured Naming System

        public class StructuredName
        {
            public string Panel { get; set; }
            public string Purpose { get; set; }
            public string ComponentType { get; set; }
            public string SubComponent { get; set; }

            public string GetFullName()
            {
                var parts = new List<string>();

                if (!string.IsNullOrEmpty(Panel))
                    parts.Add(Panel);

                if (!string.IsNullOrEmpty(Purpose))
                    parts.Add(Purpose);

                if (!string.IsNullOrEmpty(ComponentType))
                    parts.Add(ComponentType);

                if (!string.IsNullOrEmpty(SubComponent))
                    parts.Add(SubComponent);

                return string.Join("_", parts);
            }

            public string GetPartialName()
            {
                // Returns name with [PURPOSE] placeholder
                var parts = new List<string>();

                if (!string.IsNullOrEmpty(Panel))
                    parts.Add(Panel);

                parts.Add("[PURPOSE]");

                if (!string.IsNullOrEmpty(ComponentType))
                    parts.Add(ComponentType);

                if (!string.IsNullOrEmpty(SubComponent))
                    parts.Add(SubComponent);

                return string.Join("_", parts);
            }
        }


        private string TryToDetectPurpose(GameObject element)
        {
            // NEVER guess the purpose - always ask the user!
            // This ensures explicit, intentional naming rather than auto-magic guessing.
            return null;
        }

        private string GetComponentTypeName(GameObject element)
        {
            if (element.GetComponent<Button>()) return "Button";
            if (element.GetComponent<InputField>()) return "InputField";
            if (element.GetComponent<TMP_InputField>()) return "InputField";
            if (element.GetComponent<Toggle>()) return "Toggle";
            if (element.GetComponent<Slider>()) return "Slider";
            if (element.GetComponent<Dropdown>()) return "Dropdown";
            if (element.GetComponent<TMP_Dropdown>()) return "Dropdown";
            if (element.GetComponent<ScrollRect>()) return "ScrollView";
            if (element.GetComponent<Text>()) return "Text";
            if (element.GetComponent<TMP_Text>()) return "Text";
            if (element.GetComponent<Image>()) return "Image";

            return "Element";
        }

        private string GetSubComponentName(GameObject element)
        {
            // Check if this is a placeholder or text inside an InputField
            Transform parent = element.transform.parent;
            if (parent != null)
            {
                if (parent.GetComponent<InputField>() != null || parent.GetComponent<TMP_InputField>() != null)
                {
                    if (element.name.ToLower().Contains("placeholder"))
                        return "Placeholder";
                    if (element.GetComponent<Text>() != null || element.GetComponent<TMP_Text>() != null)
                        return "Text";
                }

                if (parent.GetComponent<Button>() != null)
                {
                    if (element.GetComponent<Text>() != null || element.GetComponent<TMP_Text>() != null)
                        return "Text";
                }
            }

            // Not a sub-component
            return null;
        }

        /// <summary>
        /// Strips type suffix from element name when used as context.
        /// Only strips container-type suffixes (Panel, Screen, View, etc.) - NOT regular UI elements!
        /// Uses dynamic detection based on name patterns.
        /// </summary>
        private string StripTypeSuffix(string name, GameObject element)
        {
            if (string.IsNullOrEmpty(name) || element == null)
                return name;

            // Define container-type suffixes that should be stripped when used as context
            string[] containerSuffixes = { "_Panel", "_Screen", "_View", "_Container", "_Group" };

            // Check if this name ends with a container suffix
            foreach (var suffix in containerSuffixes)
            {
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    // Found a container suffix - strip it!
                    return name.Substring(0, name.Length - suffix.Length);
                }
            }

            // Not a container or no matching suffix - return name unchanged
            // Regular UI elements like Button, InputField keep their suffix
            return name;
        }

        #endregion

        #region Preview Methods

        private void ShowPreview()
        {
            previewChanges.Clear();

            // Track elements and their children that we've processed to avoid conflicts
            HashSet<GameObject> processedElements = new HashSet<GameObject>();
            HashSet<GameObject> elementsWithUserInput = new HashSet<GameObject>();

            // FIRST: Collect purpose fixes (user input)
            foreach (var purposeItem in analysis.NeedsPurpose)
            {
                if (!string.IsNullOrEmpty(purposeItem.UserInputPurpose))
                {
                    // Use the StructuredInfo to build the complete name with Panel context
                    purposeItem.StructuredInfo.Purpose = purposeItem.UserInputPurpose;
                    string newName = purposeItem.StructuredInfo.GetFullName();
                    previewChanges.Add((purposeItem.Element, purposeItem.Element.name, newName));
                    processedElements.Add(purposeItem.Element);
                    elementsWithUserInput.Add(purposeItem.Element);

                    // Also collect changes for children (Text, Placeholder, etc.)
                    foreach (Transform child in purposeItem.Element.transform)
                    {
                        if (IsSubComponent(child.gameObject))
                        {
                            string childType = GetSubComponentName(child.gameObject);
                            string childNewName = $"{newName}_{childType}";
                            previewChanges.Add((child.gameObject, child.gameObject.name, childNewName));
                            processedElements.Add(child.gameObject);
                        }
                    }
                }
            }

            // SECOND: Collect auto-fixes ONLY for elements not already handled
            foreach (var fix in analysis.AutoFixed)
            {
                // Skip if already processed
                if (processedElements.Contains(fix.Element))
                    continue;

                // Skip if this is a child of an element with user input
                bool isChildOfUserInput = false;
                if (fix.Element.transform.parent != null)
                {
                    if (elementsWithUserInput.Contains(fix.Element.transform.parent.gameObject))
                    {
                        isChildOfUserInput = true;
                    }
                }

                if (!isChildOfUserInput)
                {
                    previewChanges.Add((fix.Element, fix.Element.name, fix.SuggestedName));
                    processedElements.Add(fix.Element);
                }
            }

            // Initialize selections (all checked by default) and custom names
            previewSelections.Clear();
            previewCustomNames.Clear();
            foreach (var change in previewChanges)
            {
                previewSelections[change.element] = true; // Checked by default
                previewCustomNames[change.element] = change.newName; // Store default name
            }

            showingPreview = true;
            currentTab = TabType.Preview;
        }

        private void DrawPreviewTabContent()
        {
            if (previewChanges.Count == 0)
            {
                EditorGUILayout.HelpBox("No changes to preview.", MessageType.Info);
                return;
            }

            int selectedCount = previewSelections.Count(kvp => kvp.Value);
            EditorGUILayout.LabelField($"📋 {previewChanges.Count} total changes ({selectedCount} selected):", EditorStyles.boldLabel);

            // Select/Deselect all buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("☑️ Select All", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                foreach (var change in previewChanges)
                    previewSelections[change.element] = true;
            }
            if (GUILayout.Button("☐ Deselect All", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                foreach (var change in previewChanges)
                    previewSelections[change.element] = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            foreach (var (element, oldName, newName) in previewChanges)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();

                // Checkbox to include/exclude this change
                bool isSelected = previewSelections.ContainsKey(element) && previewSelections[element];
                bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                if (newSelected != isSelected)
                {
                    previewSelections[element] = newSelected;
                }

                EditorGUILayout.BeginVertical();

                // Element path
                string path = GetElementPath(element);
                EditorGUILayout.LabelField($"📍 {path}", EditorStyles.miniLabel);

                EditorGUILayout.BeginHorizontal();

                // OLD name (red background)
                GUIStyle oldStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { background = MakeTex(1, 1, new Color(0.8f, 0.2f, 0.2f, 0.3f)), textColor = Color.white },
                    padding = new RectOffset(8, 8, 4, 4),
                    fontStyle = FontStyle.Bold
                };
                EditorGUILayout.LabelField($"❌ {oldName}", oldStyle, GUILayout.Height(24), GUILayout.MinWidth(150));

                // Arrow
                EditorGUILayout.LabelField("→", GUILayout.Width(30));

                // NEW name (editable text field)
                string currentName = previewCustomNames.ContainsKey(element) ? previewCustomNames[element] : newName;
                string editedName = EditorGUILayout.TextField(currentName, GUILayout.Height(24), GUILayout.MinWidth(150));
                if (editedName != currentName)
                {
                    previewCustomNames[element] = editedName;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
        }

        private void ApplyPreviewedChanges()
        {
            // Count selected changes
            int selectedCount = previewSelections.Count(kvp => kvp.Value);

            if (selectedCount == 0)
            {
                EditorUtility.DisplayDialog("No Changes Selected",
                    "Please select at least one change to apply by checking the checkboxes.",
                    "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Confirm Changes",
                $"Apply {selectedCount} naming changes?\n\nThis action can be undone with Ctrl+Z.",
                "Apply", "Cancel"))
            {
                // Get only selected elements
                var selectedElements = previewChanges
                    .Where(p => previewSelections.ContainsKey(p.element) && previewSelections[p.element])
                    .Select(p => p.element)
                    .ToArray();

                Undo.RecordObjects(selectedElements, "Smart Naming Changes");

                int appliedCount = 0;
                foreach (var (element, oldName, newName) in previewChanges)
                {
                    // Only apply if selected
                    if (previewSelections.ContainsKey(element) && previewSelections[element])
                    {
                        // Use custom name if edited, otherwise use suggested name
                        string finalName = previewCustomNames.ContainsKey(element) ? previewCustomNames[element] : newName;
                        element.name = finalName;
                        EditorUtility.SetDirty(element);
                        appliedCount++;
                    }
                }

                EditorUtility.DisplayDialog("Success",
                    $"✅ Applied {appliedCount} naming changes successfully!",
                    "OK");

                // Exit preview mode and re-analyze
                showingPreview = false;
                previewChanges.Clear();
                previewSelections.Clear();
                previewCustomNames.Clear();
                AnalyzeNaming();
            }
        }

        private void CancelPreview()
        {
            showingPreview = false;
            previewChanges.Clear();
            previewSelections.Clear();
            previewCustomNames.Clear();
            currentTab = TabType.FixIssues;
        }

        #endregion
    }

    // Professional responsive design enum (matching UIManagerEditor)
    public enum ResponsiveMode
    {
        Narrow,
        Medium,
        Wide
    }
#endif