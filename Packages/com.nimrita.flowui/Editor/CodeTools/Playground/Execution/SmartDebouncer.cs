#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Nimrita.FlowUI.Editor.Playground
{
    /// <summary>
    /// Smart debouncing system that understands code syntax.
    ///
    /// Unlike simple time-based debouncing, this:
    /// - Waits for syntactically complete code (balanced braces, parens, brackets)
    /// - Ignores whitespace-only changes
    /// - Tracks edit history for better decision making
    /// - Only triggers when user actually stops typing
    ///
    /// This prevents executing incomplete code like "if (true) {" mid-typing.
    /// </summary>
    public class SmartDebouncer
    {
        private const float DEFAULT_DEBOUNCE_DELAY = 0.5f; // 500ms
        private const int MAX_EDIT_HISTORY = 10;

        // Edit tracking
        private struct Edit
        {
            public double Time;
            public int ChangePosition;
            public string Code;
        }

        private Queue<Edit> recentEdits = new Queue<Edit>(MAX_EDIT_HISTORY);
        private double lastEditTime = 0;
        private string lastSeenCode = "";
        private string lastExecutedCode = "";
        private float debounceDelay = DEFAULT_DEBOUNCE_DELAY;

        /// <summary>
        /// Gets or sets the debounce delay in seconds.
        /// </summary>
        public float DebounceDelay
        {
            get => debounceDelay;
            set => debounceDelay = Math.Max(0.1f, Math.Min(value, 5f));
        }

        /// <summary>
        /// Call this every frame with current code.
        /// Returns true if code should be executed.
        /// </summary>
        public bool ShouldExecute(string currentCode, out string reason)
        {
            reason = "";

            // Track code change
            if (currentCode != lastSeenCode)
            {
                TrackEdit(currentCode);
                lastSeenCode = currentCode;
                return false; // Just changed, don't execute yet
            }

            // Check if enough time has passed since last edit
            double timeSinceLastEdit = EditorApplication.timeSinceStartup - lastEditTime;
            if (timeSinceLastEdit < debounceDelay)
            {
                reason = $"Waiting {(debounceDelay - timeSinceLastEdit):F1}s...";
                return false;
            }

            // Check if code is empty
            if (string.IsNullOrWhiteSpace(currentCode))
            {
                // Allow execution of empty code (for cleanup)
                if (!string.IsNullOrWhiteSpace(lastExecutedCode))
                {
                    reason = "Executing cleanup";
                    return true;
                }
                return false;
            }

            // Check if code is different from last execution
            if (currentCode == lastExecutedCode)
            {
                return false; // Already executed this exact code
            }

            // Check if only whitespace changed
            if (OnlyWhitespaceChanged(currentCode, lastExecutedCode))
            {
                reason = "Only whitespace changed";
                lastExecutedCode = currentCode; // Update so we don't keep checking
                return false;
            }

            // Check if code is syntactically complete
            if (!IsSyntacticallyComplete(currentCode, out string syntaxReason))
            {
                reason = syntaxReason;
                return false;
            }

            // All checks passed - execute!
            reason = "Ready to execute";
            return true;
        }

        /// <summary>
        /// Mark code as executed (call after successful execution).
        /// </summary>
        public void MarkExecuted(string code)
        {
            lastExecutedCode = code;
        }

        /// <summary>
        /// Mark code as processed after an error to avoid immediate re-exec spam.
        /// </summary>
        public void MarkErrored(string code)
        {
            lastExecutedCode = code;
        }

        /// <summary>
        /// Reset debouncer state.
        /// </summary>
        public void Reset()
        {
            recentEdits.Clear();
            lastEditTime = 0;
            lastSeenCode = "";
            lastExecutedCode = "";
        }

        private void TrackEdit(string newCode)
        {
            lastEditTime = EditorApplication.timeSinceStartup;

            // Find change position (for future optimizations)
            int changePos = FindChangePosition(lastSeenCode, newCode);

            // Add to history
            recentEdits.Enqueue(new Edit
            {
                Time = lastEditTime,
                ChangePosition = changePos,
                Code = newCode
            });

            // Keep history size limited
            while (recentEdits.Count > MAX_EDIT_HISTORY)
            {
                recentEdits.Dequeue();
            }
        }

        private int FindChangePosition(string oldCode, string newCode)
        {
            if (string.IsNullOrEmpty(oldCode)) return 0;
            if (string.IsNullOrEmpty(newCode)) return 0;

            int minLen = Math.Min(oldCode.Length, newCode.Length);
            for (int i = 0; i < minLen; i++)
            {
                if (oldCode[i] != newCode[i])
                    return i;
            }
            return minLen;
        }

        private bool OnlyWhitespaceChanged(string code1, string code2)
        {
            if (string.IsNullOrEmpty(code1) && string.IsNullOrEmpty(code2))
                return true;

            string normalized1 = NormalizeWhitespace(code1);
            string normalized2 = NormalizeWhitespace(code2);

            return normalized1 == normalized2;
        }

        private string NormalizeWhitespace(string code)
        {
            if (string.IsNullOrEmpty(code))
                return "";

            // Remove all whitespace for comparison
            return System.Text.RegularExpressions.Regex.Replace(code, @"\s+", "");
        }

        /// <summary>
        /// Check if code is syntactically complete (balanced braces, parens, brackets).
        /// </summary>
        private bool IsSyntacticallyComplete(string code, out string reason)
        {
            reason = "";

            int braces = 0;      // { }
            int parens = 0;      // ( )
            int brackets = 0;    // [ ]
            bool inString = false;
            bool inChar = false;
            bool inLineComment = false;
            bool inBlockComment = false;
            char prevChar = '\0';

            for (int i = 0; i < code.Length; i++)
            {
                char c = code[i];

                // Handle escapes inside strings
                if (inString && c == '\\')
                {
                    i++; // skip escaped character
                    prevChar = c;
                    continue;
                }

                // Handle line comments
                if (!inString && !inChar && !inBlockComment && c == '/' && i + 1 < code.Length && code[i + 1] == '/')
                {
                    inLineComment = true;
                    continue;
                }

                if (inLineComment && c == '\n')
                {
                    inLineComment = false;
                    continue;
                }

                if (inLineComment)
                    continue;

                // Handle block comments
                if (!inString && !inChar && c == '/' && i + 1 < code.Length && code[i + 1] == '*')
                {
                    inBlockComment = true;
                    i++; // Skip next char
                    continue;
                }

                if (inBlockComment && c == '*' && i + 1 < code.Length && code[i + 1] == '/')
                {
                    inBlockComment = false;
                    i++; // Skip next char
                    continue;
                }

                if (inBlockComment)
                    continue;

                // Handle strings
                if (c == '"' && prevChar != '\\')
                {
                    inString = !inString;
                }

                // Handle chars
                if (c == '\'' && prevChar != '\\')
                {
                    inChar = !inChar;
                }

                // Don't count brackets inside strings/chars
                if (inString || inChar)
                {
                    prevChar = c;
                    continue;
                }

                // Count brackets
                switch (c)
                {
                    case '{': braces++; break;
                    case '}': braces--; break;
                    case '(': parens++; break;
                    case ')': parens--; break;
                    case '[': brackets++; break;
                    case ']': brackets--; break;
                }

                prevChar = c;
            }

            // Check for unclosed strings/comments
            if (inString)
            {
                reason = "Unclosed string";
                return false;
            }

            if (inChar)
            {
                reason = "Unclosed character literal";
                return false;
            }

            if (inBlockComment)
            {
                reason = "Unclosed block comment";
                return false;
            }

            // Check for unbalanced brackets
            if (braces > 0)
            {
                reason = $"{braces} unclosed {{ brace(s)";
                return false;
            }

            if (braces < 0)
            {
                reason = "Too many }} closing braces";
                return false;
            }

            if (parens > 0)
            {
                reason = $"{parens} unclosed ( paren(s)";
                return false;
            }

            if (parens < 0)
            {
                reason = "Too many ) closing parens";
                return false;
            }

            if (brackets > 0)
            {
                reason = $"{brackets} unclosed [ bracket(s)";
                return false;
            }

            if (brackets < 0)
            {
                reason = "Too many ] closing brackets";
                return false;
            }

            // All checks passed
            return true;
        }

        /// <summary>
        /// Get statistics for debugging.
        /// </summary>
        public string GetStats()
        {
            double timeSinceLast = EditorApplication.timeSinceStartup - lastEditTime;
            return $"Edits: {recentEdits.Count}, Time since last: {timeSinceLast:F2}s";
        }
    }
}
#endif
