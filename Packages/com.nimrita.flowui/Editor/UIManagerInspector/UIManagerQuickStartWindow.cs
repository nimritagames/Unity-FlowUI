#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public partial class UIManagerEditor
{
    /// <summary>
    /// Interactive quick start walkthrough window with professional styling.
    /// </summary>
    public class UIManagerQuickStartWindow : EditorWindow
    {
        // ── State ──
        private int currentPage = 0;
        private const int TotalPages = 4;
        private UIManager uiManager;
        private Action<int> onNavigateToTab;

        // ── Cached GUIStyles ──
        private static GUIStyle s_headerTitleStyle;
        private static GUIStyle s_headerSubtitleStyle;
        private static GUIStyle s_pageTitleStyle;
        private static GUIStyle s_pageDescStyle;
        private static GUIStyle s_checkpointStyle;
        private static GUIStyle s_buttonLabelStyle;
        private static GUIStyle s_dotStyle;
        private static GUIStyle s_actionLinkStyle;

        // ── Cached GUIContent (icons) ──
        private static GUIContent s_welcomeIcon;
        private static GUIContent s_scanIcon;
        private static GUIContent s_libraryIcon;
        private static GUIContent s_handlersIcon;

        // ── Page Data ──
        private static readonly string[] PageTitles =
        {
            "Welcome to Flow UI",
            "Step 1: Scan Your UI",
            "Step 2: Generate Library",
            "Step 3: Create Handlers"
        };

        private static readonly string[] PageDescriptions =
        {
            "Flow UI gives you centralized UI management with a fluent builder API. " +
            "Register your existing UGUI elements, generate type-safe code, and wire up events — all from the inspector.",

            "Open the UI Hierarchy tab in the UIManager inspector. It displays every Canvas and UI element in your scene. " +
            "Click the Add button next to any element to register it with the manager.",

            "Once your UI elements are registered, switch to the Library tab and click Generate. " +
            "This creates a C# constants file with typed paths so you never use magic strings again.",

            "Finally, use the Handlers tab to generate event handler scripts. " +
            "Flow UI creates partial classes so your custom logic is safe across regenerations."
        };

        private static readonly string[][] PageKeyPoints =
        {
            new[] { "Manage all UI from one place", "Generate type-safe code", "Auto-handle events & state" },
            new[] { "Open UI Hierarchy tab", "Click Add on elements", "Elements auto-categorize by type" },
            new[] { "Creates C# constants file", "Type-safe path access", "Regenerate after UI changes" },
            new[] { "Auto-generates event wiring", "Partial class pattern (safe to edit)", "Supports all UGUI events" }
        };

        private static readonly string[] PageActionLabels =
        {
            null,
            "Go to Hierarchy Tab",
            "Go to Library Tab",
            "Get Started!"
        };

        private static readonly int[] PageActionTabIndices = { -1, 0, 1, -1 };

        // ── Icons (built-in) ──
        private static readonly string[] PageIconNames =
        {
            "d_UnityEditor.SceneHierarchyWindow",
            "d_SceneViewTools",
            "d_cs Script Icon",
            "d_PlayButton"
        };

        // ══════════════════════════════════════════════
        //  Public API
        // ══════════════════════════════════════════════

        public static void ShowWindow(UIManager manager, Action<int> onNavigateToTab = null)
        {
            var window = GetWindow<UIManagerQuickStartWindow>(true, "Flow UI \u2014 Quick Start", true);
            window.uiManager = manager;
            window.onNavigateToTab = onNavigateToTab;
            window.minSize = new Vector2(650, 500);
            window.maxSize = new Vector2(900, 700);

            // Center on screen
            float x = (Screen.currentResolution.width - 650) * 0.5f;
            float y = (Screen.currentResolution.height - 500) * 0.5f;
            window.position = new Rect(x, y, 650, 500);
            window.Show();
        }

        // ══════════════════════════════════════════════
        //  Style Initialization
        // ══════════════════════════════════════════════

        private static void EnsureStyles()
        {
            if (s_headerTitleStyle != null) return;

            s_headerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
            };

            s_headerSubtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.7f, 0.75f, 0.85f) }
            };

            s_pageTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.92f, 0.93f, 0.97f) }
            };

            s_pageDescStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.88f, 0.88f, 0.92f) },
                padding = new RectOffset(4, 4, 0, 0)
            };

            s_checkpointStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.82f, 0.95f, 0.82f) },
                padding = new RectOffset(8, 4, 2, 2)
            };

            s_buttonLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.92f, 0.92f, 0.97f) }
            };

            s_dotStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0)
            };

            s_actionLinkStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.5f, 0.75f, 1f) },
                hover = { textColor = new Color(0.65f, 0.85f, 1f) }
            };
        }

        private static void EnsureIcons()
        {
            if (s_welcomeIcon != null) return;

            s_welcomeIcon = LoadIcon(PageIconNames[0]);
            s_scanIcon = LoadIcon(PageIconNames[1]);
            s_libraryIcon = LoadIcon(PageIconNames[2]);
            s_handlersIcon = LoadIcon(PageIconNames[3]);
        }

        private static GUIContent LoadIcon(string iconName)
        {
            var tex = EditorGUIUtility.IconContent(iconName);
            return tex != null && tex.image != null ? tex : EditorGUIUtility.IconContent("d_Prefab Icon");
        }

        private GUIContent GetPageIcon(int page)
        {
            switch (page)
            {
                case 0: return s_welcomeIcon;
                case 1: return s_scanIcon;
                case 2: return s_libraryIcon;
                case 3: return s_handlersIcon;
                default: return s_welcomeIcon;
            }
        }

        // ══════════════════════════════════════════════
        //  OnGUI
        // ══════════════════════════════════════════════

        private void OnGUI()
        {
            EnsureStyles();
            EnsureIcons();

            DrawGradientHeader();
            GUILayout.Space(10);
            DrawPageIndicators();
            GUILayout.Space(8);
            DrawContentCard();
            GUILayout.FlexibleSpace();
            DrawNavigationBar();
        }

        // ── Gradient Header ──
        private void DrawGradientHeader()
        {
            Rect headerRect = GUILayoutUtility.GetRect(position.width, 56);

            if (Event.current.type == EventType.Repaint)
            {
                Color topColor = new Color(0.2f, 0.2f, 0.3f);
                Color bottomColor = new Color(0.15f, 0.15f, 0.2f);

                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, headerRect.width, headerRect.height * 0.5f), topColor);
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y + headerRect.height * 0.5f, headerRect.width, headerRect.height * 0.5f), bottomColor);

                // Bottom accent line
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y + headerRect.height - 2, headerRect.width, 2),
                    new Color(0.3f, 0.5f, 0.8f, 0.6f));
            }

            // Title
            Rect titleRect = new Rect(headerRect.x + 20, headerRect.y + 8, headerRect.width - 40, 22);
            EditorGUI.LabelField(titleRect, "Flow UI Framework", s_headerTitleStyle);

            // Subtitle
            Rect subtitleRect = new Rect(headerRect.x + 20, headerRect.y + 30, headerRect.width - 40, 18);
            EditorGUI.LabelField(subtitleRect, "Get started in 3 steps", s_headerSubtitleStyle);
        }

        // ── Page Indicators (dots) ──
        private void DrawPageIndicators()
        {
            Rect dotsRect = GUILayoutUtility.GetRect(position.width, 20);
            float totalWidth = TotalPages * 14 + (TotalPages - 1) * 10;
            float startX = dotsRect.x + (dotsRect.width - totalWidth) * 0.5f;

            for (int i = 0; i < TotalPages; i++)
            {
                float dotX = startX + i * 24;
                Rect dotRect = new Rect(dotX, dotsRect.y + 4, 14, 14);

                if (Event.current.type == EventType.Repaint)
                {
                    bool isCurrent = i == currentPage;
                    Color dotColor = isCurrent
                        ? new Color(0.3f, 0.5f, 0.8f)
                        : new Color(0.4f, 0.4f, 0.45f);

                    // Draw filled circle approximation (small rect with surrounding rects)
                    DrawFilledCircle(dotRect, dotColor, isCurrent ? 6f : 5f);
                }

                // Draw connecting line between dots
                if (i < TotalPages - 1 && Event.current.type == EventType.Repaint)
                {
                    float lineX = dotX + 14;
                    Rect lineRect = new Rect(lineX, dotsRect.y + 10, 10, 1);
                    EditorGUI.DrawRect(lineRect, new Color(0.35f, 0.35f, 0.4f));
                }

                // Clickable dots
                if (Event.current.type == EventType.MouseDown && dotRect.Contains(Event.current.mousePosition))
                {
                    currentPage = i;
                    Event.current.Use();
                    Repaint();
                }
            }
        }

        private static void DrawFilledCircle(Rect bounds, Color color, float radius)
        {
            // Approximate a circle with layered rects
            float cx = bounds.x + bounds.width * 0.5f;
            float cy = bounds.y + bounds.height * 0.5f;

            // Core rect
            EditorGUI.DrawRect(new Rect(cx - radius * 0.7f, cy - radius, radius * 1.4f, radius * 2f), color);
            // Side rects for roundness
            EditorGUI.DrawRect(new Rect(cx - radius, cy - radius * 0.7f, radius * 2f, radius * 1.4f), color);
            // Corner fill
            float d = radius * 0.5f;
            EditorGUI.DrawRect(new Rect(cx - radius * 0.9f, cy - radius * 0.5f, radius * 1.8f, radius * 1f), color);
            EditorGUI.DrawRect(new Rect(cx - radius * 0.5f, cy - radius * 0.9f, radius * 1f, radius * 1.8f), color);
        }

        // ── Content Card ──
        private Rect cachedCardRect;

        private void DrawContentCard()
        {
            float padding = 24;

            // Draw card background FIRST using cached rect from previous frame
            if (Event.current.type == EventType.Repaint && cachedCardRect.height > 0)
            {
                EditorGUI.DrawRect(cachedCardRect, new Color(0.22f, 0.22f, 0.25f));

                // Subtle border
                EditorGUI.DrawRect(new Rect(cachedCardRect.x, cachedCardRect.y, cachedCardRect.width, 1), new Color(0.32f, 0.32f, 0.38f));
                EditorGUI.DrawRect(new Rect(cachedCardRect.x, cachedCardRect.yMax - 1, cachedCardRect.width, 1), new Color(0.16f, 0.16f, 0.19f));
            }

            Rect outerRect = EditorGUILayout.BeginVertical();
            Rect cardRect = new Rect(padding, outerRect.y, position.width - padding * 2, 0);

            GUILayout.Space(16);

            // Icon
            GUIContent icon = GetPageIcon(currentPage);
            if (icon != null && icon.image != null)
            {
                Rect iconArea = GUILayoutUtility.GetRect(position.width - padding * 2, 40);
                Rect iconRect = new Rect(iconArea.x + (iconArea.width - 36) * 0.5f, iconArea.y, 36, 36);
                GUI.DrawTexture(iconRect, icon.image, ScaleMode.ScaleToFit);
            }

            GUILayout.Space(8);

            // Title
            Rect titleArea = GUILayoutUtility.GetRect(position.width - padding * 2 - 40, 24);
            EditorGUI.LabelField(titleArea, PageTitles[currentPage], s_pageTitleStyle);

            GUILayout.Space(8);

            // Description
            float descWidth = position.width - padding * 2 - 60;
            float descHeight = s_pageDescStyle.CalcHeight(new GUIContent(PageDescriptions[currentPage]), descWidth);
            Rect descArea = GUILayoutUtility.GetRect(descWidth, descHeight + 4);
            Rect descRect = new Rect(descArea.x + 30, descArea.y, descWidth, descHeight + 4);
            EditorGUI.LabelField(descRect, PageDescriptions[currentPage], s_pageDescStyle);

            GUILayout.Space(12);

            // Key Points box
            DrawKeyPointsBox(padding);

            GUILayout.Space(8);

            // Action link (pages 1-3)
            string actionLabel = PageActionLabels[currentPage];
            if (actionLabel != null && currentPage < TotalPages - 1)
            {
                Rect linkRect = GUILayoutUtility.GetRect(position.width - padding * 2, 20);
                Rect centeredLink = new Rect(linkRect.x + (linkRect.width - 180) * 0.5f, linkRect.y, 180, 20);

                EditorGUIUtility.AddCursorRect(centeredLink, MouseCursor.Link);
                if (GUI.Button(centeredLink, "\u25B6 " + actionLabel, s_actionLinkStyle))
                {
                    ExecutePageAction(currentPage);
                }
            }

            GUILayout.Space(12);

            EditorGUILayout.EndVertical();

            // Cache card rect for next frame's background draw
            float cardBottom = GUILayoutUtility.GetLastRect().yMax;
            cardRect.height = cardBottom - cardRect.y;
            cachedCardRect = cardRect;
        }

        private void DrawKeyPointsBox(float outerPadding)
        {
            string[] points = PageKeyPoints[currentPage];
            float boxPadding = outerPadding + 30;

            Rect boxOuter = GUILayoutUtility.GetRect(position.width - boxPadding * 2, points.Length * 26 + 12);
            Rect boxRect = new Rect(boxOuter.x + boxPadding - outerPadding, boxOuter.y, position.width - boxPadding * 2, boxOuter.height);

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(boxRect, new Color(0.19f, 0.23f, 0.19f));
                // Left accent
                EditorGUI.DrawRect(new Rect(boxRect.x, boxRect.y, 2, boxRect.height), new Color(0.3f, 0.7f, 0.3f, 0.8f));
            }

            for (int i = 0; i < points.Length; i++)
            {
                Rect pointRect = new Rect(boxRect.x + 12, boxRect.y + 6 + i * 26, boxRect.width - 24, 22);
                EditorGUI.LabelField(pointRect, "\u2713  " + points[i], s_checkpointStyle);
            }
        }

        // ── Navigation Bar ──
        private void DrawNavigationBar()
        {
            Rect navRect = GUILayoutUtility.GetRect(position.width, 50);

            if (Event.current.type == EventType.Repaint)
            {
                // Separator line
                EditorGUI.DrawRect(new Rect(navRect.x, navRect.y, navRect.width, 1), new Color(0.3f, 0.3f, 0.35f, 0.5f));
            }

            float btnWidth = 110;
            float btnHeight = 28;
            float btnY = navRect.y + (navRect.height - btnHeight) * 0.5f;

            // Back button
            if (currentPage > 0)
            {
                Rect backRect = new Rect(navRect.x + 20, btnY, btnWidth, btnHeight);
                DrawStyledButton(backRect, "\u2190 Back", false, () =>
                {
                    currentPage--;
                    Repaint();
                });
            }

            // Page counter (center dots)
            Rect counterRect = new Rect(navRect.x + (navRect.width - 80) * 0.5f, btnY, 80, btnHeight);
            s_dotStyle.normal.textColor = new Color(0.5f, 0.5f, 0.55f);
            string dots = "";
            for (int i = 0; i < TotalPages; i++)
                dots += i == currentPage ? "\u25CF " : "\u25CB ";
            EditorGUI.LabelField(counterRect, dots.Trim(), s_dotStyle);

            // Next / Get Started button
            bool isLastPage = currentPage == TotalPages - 1;
            string nextLabel = isLastPage ? "Get Started!" : "Next \u2192";
            Rect nextRect = new Rect(navRect.x + navRect.width - btnWidth - 20, btnY, btnWidth, btnHeight);
            DrawStyledButton(nextRect, nextLabel, true, () =>
            {
                if (isLastPage)
                {
                    Close();
                    if (uiManager != null)
                        Selection.activeObject = uiManager;
                }
                else
                {
                    currentPage++;
                    Repaint();
                }
            });
        }

        // ── Styled Button (matches DrawActionButton from main editor) ──
        private void DrawStyledButton(Rect position, string label, bool isPrimary, Action onClick)
        {
            Color bgColor = isPrimary
                ? new Color(0.2f, 0.4f, 0.7f)
                : new Color(0.3f, 0.3f, 0.35f);

            Color hoverColor = isPrimary
                ? new Color(0.3f, 0.5f, 0.8f)
                : new Color(0.35f, 0.35f, 0.4f);

            bool isHovering = position.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
            {
                Color c = isHovering ? hoverColor : bgColor;
                EditorGUI.DrawRect(position, c);

                // Top highlight
                EditorGUI.DrawRect(new Rect(position.x, position.y, position.width, 1),
                    new Color(c.r + 0.08f, c.g + 0.08f, c.b + 0.08f, 0.8f));
                // Bottom shadow
                EditorGUI.DrawRect(new Rect(position.x, position.y + position.height - 1, position.width, 1),
                    new Color(c.r - 0.08f, c.g - 0.08f, c.b - 0.08f, 0.8f));
            }

            if (isHovering)
                EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

            EditorGUI.LabelField(position, label, s_buttonLabelStyle);

            if (Event.current.type == EventType.MouseDown && position.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                onClick?.Invoke();
            }
        }

        // ── Page Actions ──
        private void ExecutePageAction(int page)
        {
            int tabIndex = PageActionTabIndices[page];

            if (tabIndex >= 0 && onNavigateToTab != null)
            {
                Close();
                onNavigateToTab.Invoke(tabIndex);
            }
            else if (page == TotalPages - 1)
            {
                Close();
                if (uiManager != null)
                    Selection.activeObject = uiManager;
            }
        }
    }
}
#endif
