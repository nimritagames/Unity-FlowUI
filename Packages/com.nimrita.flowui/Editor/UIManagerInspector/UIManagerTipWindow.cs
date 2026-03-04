#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public partial class UIManagerEditor
{
    /// <summary>
    /// Rich notification window with typed styling, optional action button, and auto-dismiss.
    /// </summary>
    public class UIManagerTipWindow : EditorWindow
    {
        // ── Tip Types ──
        public enum TipType { Info, Warning, Success, Error }

        // ── State ──
        private string tipTitle;
        private string tipMessage;
        private TipType tipType = TipType.Info;
        private string actionLabel;
        private Action onAction;
        private float autoDismissSeconds;
        private double openTime;
        private bool hasAutoDismiss;

        // ── Cached GUIStyles ──
        private static GUIStyle s_headerTitleStyle;
        private static GUIStyle s_messageStyle;
        private static GUIStyle s_buttonLabelStyle;

        // ── Cached GUIContent (icons per type) ──
        private static GUIContent s_infoIcon;
        private static GUIContent s_warningIcon;
        private static GUIContent s_successIcon;
        private static GUIContent s_errorIcon;

        // ══════════════════════════════════════════════
        //  Public API (multiple overloads)
        // ══════════════════════════════════════════════

        /// <summary>Simple tip (backwards compatible).</summary>
        public static void ShowWindow(string title, string message)
        {
            ShowWindow(title, message, TipType.Info, null, null, 0f);
        }

        /// <summary>Typed tip.</summary>
        public static void ShowWindow(string title, string message, TipType type)
        {
            ShowWindow(title, message, type, null, null, 0f);
        }

        /// <summary>Typed tip with action button.</summary>
        public static void ShowWindow(string title, string message, TipType type,
            string actionLabel, Action onAction)
        {
            ShowWindow(title, message, type, actionLabel, onAction, 0f);
        }

        /// <summary>Typed tip with auto-dismiss.</summary>
        public static void ShowWindow(string title, string message, TipType type,
            float autoDismissSeconds)
        {
            ShowWindow(title, message, type, null, null, autoDismissSeconds);
        }

        /// <summary>Full-featured tip.</summary>
        public static void ShowWindow(string title, string message, TipType type,
            string actionLabel, Action onAction, float autoDismissSeconds)
        {
            string windowTitle = GetWindowTitle(type);
            var window = GetWindow<UIManagerTipWindow>(true, windowTitle, true);
            window.tipTitle = title;
            window.tipMessage = message;
            window.tipType = type;
            window.actionLabel = actionLabel;
            window.onAction = onAction;
            window.autoDismissSeconds = autoDismissSeconds;
            window.hasAutoDismiss = autoDismissSeconds > 0f;
            window.openTime = EditorApplication.timeSinceStartup;

            window.minSize = new Vector2(380, 180);
            window.maxSize = new Vector2(420, 320);

            float x = (Screen.currentResolution.width - 380) * 0.5f;
            float y = (Screen.currentResolution.height - 200) * 0.5f;
            window.position = new Rect(x, y, 380, 200);
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
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.95f, 0.95f, 0.98f) }
            };

            s_messageStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) },
                padding = new RectOffset(6, 6, 4, 4)
            };

            s_buttonLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.92f, 0.92f, 0.97f) }
            };
        }

        private static void EnsureIcons()
        {
            if (s_infoIcon != null) return;

            s_infoIcon = SafeLoadIcon("d_console.infoicon");
            s_warningIcon = SafeLoadIcon("d_console.warnicon");
            s_successIcon = SafeLoadIcon("d_FilterSelectedOnly");
            s_errorIcon = SafeLoadIcon("d_console.erroricon");
        }

        private static GUIContent SafeLoadIcon(string name)
        {
            var content = EditorGUIUtility.IconContent(name);
            return content != null && content.image != null ? content : new GUIContent();
        }

        // ══════════════════════════════════════════════
        //  Color Helpers
        // ══════════════════════════════════════════════

        private static string GetWindowTitle(TipType type)
        {
            switch (type)
            {
                case TipType.Warning: return "Warning";
                case TipType.Success: return "Success";
                case TipType.Error:   return "Error";
                default:              return "Info";
            }
        }

        private static Color GetAccentColor(TipType type)
        {
            switch (type)
            {
                case TipType.Warning: return new Color(0.8f, 0.6f, 0.2f);
                case TipType.Success: return new Color(0.3f, 0.7f, 0.3f);
                case TipType.Error:   return new Color(0.8f, 0.3f, 0.3f);
                default:              return new Color(0.3f, 0.5f, 0.8f);
            }
        }

        private static Color GetAccentDark(TipType type)
        {
            switch (type)
            {
                case TipType.Warning: return new Color(0.5f, 0.38f, 0.12f);
                case TipType.Success: return new Color(0.18f, 0.42f, 0.18f);
                case TipType.Error:   return new Color(0.5f, 0.18f, 0.18f);
                default:              return new Color(0.18f, 0.3f, 0.5f);
            }
        }

        private GUIContent GetTypeIcon()
        {
            switch (tipType)
            {
                case TipType.Warning: return s_warningIcon;
                case TipType.Success: return s_successIcon;
                case TipType.Error:   return s_errorIcon;
                default:              return s_infoIcon;
            }
        }

        // ══════════════════════════════════════════════
        //  OnGUI
        // ══════════════════════════════════════════════

        private void OnGUI()
        {
            EnsureStyles();
            EnsureIcons();

            // Auto-dismiss check
            if (hasAutoDismiss)
            {
                double elapsed = EditorApplication.timeSinceStartup - openTime;
                if (elapsed >= autoDismissSeconds)
                {
                    Close();
                    return;
                }
                Repaint(); // Keep repainting for progress bar
            }

            DrawHeader();
            GUILayout.Space(12);
            DrawMessage();
            GUILayout.FlexibleSpace();
            DrawFooter();
        }

        // ── Accent-Colored Header ──
        private void DrawHeader()
        {
            Color accent = GetAccentColor(tipType);
            Color accentDark = GetAccentDark(tipType);

            Rect headerRect = GUILayoutUtility.GetRect(position.width, 42);

            if (Event.current.type == EventType.Repaint)
            {
                // Gradient: accent top → darker bottom
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, headerRect.width, headerRect.height * 0.5f), accent);
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y + headerRect.height * 0.5f, headerRect.width, headerRect.height * 0.5f), accentDark);

                // Bottom edge
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1, headerRect.width, 1),
                    new Color(accent.r * 0.6f, accent.g * 0.6f, accent.b * 0.6f));
            }

            // Icon
            GUIContent icon = GetTypeIcon();
            if (icon != null && icon.image != null)
            {
                Rect iconRect = new Rect(headerRect.x + 14, headerRect.y + (headerRect.height - 24) * 0.5f, 24, 24);
                GUI.DrawTexture(iconRect, icon.image, ScaleMode.ScaleToFit);
            }

            // Title
            Rect titleRect = new Rect(headerRect.x + 46, headerRect.y, headerRect.width - 60, headerRect.height);
            EditorGUI.LabelField(titleRect, tipTitle, s_headerTitleStyle);
        }

        // ── Message Body ──
        private void DrawMessage()
        {
            float padding = 16;
            float msgWidth = position.width - padding * 2;
            float msgHeight = s_messageStyle.CalcHeight(new GUIContent(tipMessage), msgWidth);
            Rect msgArea = GUILayoutUtility.GetRect(msgWidth, Mathf.Max(msgHeight, 30));
            Rect msgRect = new Rect(msgArea.x + padding, msgArea.y, msgWidth, msgHeight);

            EditorGUI.LabelField(msgRect, tipMessage, s_messageStyle);
        }

        // ── Footer (buttons + progress bar) ──
        private void DrawFooter()
        {
            // Separator
            Rect sepRect = GUILayoutUtility.GetRect(position.width, 1);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(sepRect, new Color(0.3f, 0.3f, 0.35f, 0.5f));

            Rect footerRect = GUILayoutUtility.GetRect(position.width, 44);
            float btnHeight = 26;
            float btnY = footerRect.y + (footerRect.height - btnHeight) * 0.5f;

            bool hasAction = !string.IsNullOrEmpty(actionLabel) && onAction != null;

            if (hasAction)
            {
                // Action button (primary, accent-colored)
                float actionWidth = 110;
                Rect actionRect = new Rect(footerRect.x + footerRect.width - actionWidth - 130, btnY, actionWidth, btnHeight);
                DrawAccentButton(actionRect, actionLabel, GetAccentColor(tipType), () =>
                {
                    onAction?.Invoke();
                    Close();
                });
            }

            // Dismiss button (always present)
            float dismissWidth = 100;
            Rect dismissRect = new Rect(footerRect.x + footerRect.width - dismissWidth - 14, btnY, dismissWidth, btnHeight);
            DrawStyledButton(dismissRect, "Got it", false, Close);

            // Auto-dismiss progress bar
            if (hasAutoDismiss)
            {
                double elapsed = EditorApplication.timeSinceStartup - openTime;
                float progress = Mathf.Clamp01((float)(elapsed / autoDismissSeconds));
                float remaining = 1f - progress;

                Rect barRect = new Rect(footerRect.x, footerRect.yMax - 3, position.width * remaining, 3);
                if (Event.current.type == EventType.Repaint)
                {
                    Color accent = GetAccentColor(tipType);
                    EditorGUI.DrawRect(barRect, new Color(accent.r, accent.g, accent.b, 0.6f));
                }
            }

            // Click anywhere resets auto-dismiss timer
            if (hasAutoDismiss && Event.current.type == EventType.MouseDown)
            {
                openTime = EditorApplication.timeSinceStartup;
            }
        }

        // ── Button Helpers ──
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
                EditorGUI.DrawRect(new Rect(position.x, position.y, position.width, 1),
                    new Color(c.r + 0.08f, c.g + 0.08f, c.b + 0.08f, 0.8f));
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

        private void DrawAccentButton(Rect position, string label, Color accent, Action onClick)
        {
            Color hoverColor = new Color(
                Mathf.Min(accent.r + 0.1f, 1f),
                Mathf.Min(accent.g + 0.1f, 1f),
                Mathf.Min(accent.b + 0.1f, 1f));

            bool isHovering = position.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
            {
                Color c = isHovering ? hoverColor : accent;
                EditorGUI.DrawRect(position, c);
                EditorGUI.DrawRect(new Rect(position.x, position.y, position.width, 1),
                    new Color(c.r + 0.08f, c.g + 0.08f, c.b + 0.08f, 0.8f));
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
        // ═══════════════════════════════════════
        //  Test Menu (remove after testing)
        // ═══════════════════════════════════════

        [MenuItem("Flow UI/Test Tips/Info")]
        private static void TestInfo()
        {
            ShowWindow("Element Registered", "Button_Submit has been added to the UI Manager successfully.", TipType.Info);
        }

        [MenuItem("Flow UI/Test Tips/Warning")]
        private static void TestWarning()
        {
            ShowWindow("Missing References", "3 UI elements could not be found in the scene. Run a scan to fix.", TipType.Warning);
        }

        [MenuItem("Flow UI/Test Tips/Success")]
        private static void TestSuccess()
        {
            ShowWindow("Library Generated", "UILibrary.cs has been created with 12 element paths.", TipType.Success);
        }

        [MenuItem("Flow UI/Test Tips/Error")]
        private static void TestError()
        {
            ShowWindow("Generation Failed", "Could not write to Assets/Generated/. Check folder permissions.", TipType.Error);
        }

        [MenuItem("Flow UI/Test Tips/With Action")]
        private static void TestAction()
        {
            ShowWindow("New Elements Found", "5 unregistered UI elements detected in the scene.",
                TipType.Info, "Scan Now", () => Debug.Log("Scan action triggered!"));
        }

        [MenuItem("Flow UI/Test Tips/Auto Dismiss (5s)")]
        private static void TestAutoDismiss()
        {
            ShowWindow("Saved", "All changes have been saved successfully.", TipType.Success, 5f);
        }
    }
}
#endif
