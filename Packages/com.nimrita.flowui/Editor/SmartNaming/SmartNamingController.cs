#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls automatic smart naming triggers and notifications.
/// Handles proactive detection, blocking, and gentle suggestions.
/// </summary>
[InitializeOnLoad]
public static class SmartNamingController
    {
        #region Constants & Keys

        private const string FIRST_TIME_KEY = "UIManager_SmartNaming_FirstTime";
        private const string LAST_CHECK_KEY = "UIManager_SmartNaming_LastCheck";
        private const string SUGGESTION_DISMISSED_KEY = "UIManager_SmartNaming_SuggestionDismissed";
        private const float CHECK_INTERVAL_SECONDS = 30f; // Check every 30 seconds
        private const int SUGGESTION_THRESHOLD = 5; // Show suggestion if 5+ bad names

        #endregion

        #region Static Constructor & Events

        static SmartNamingController()
        {
            // Register for scene events
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.update += OnEditorUpdate;

            // Check on Unity startup
            EditorApplication.delayCall += () => CheckForNamingIssuesDelayed();
        }

        #endregion

        #region Proactive Detection

        private static void OnHierarchyChanged()
        {
            // Delay check to avoid spamming during bulk operations
            EditorApplication.delayCall += () => CheckForNamingIssues(false);
        }

        private static void OnEditorUpdate()
        {
            // Periodic check (but not too often)
            string lastCheckStr = EditorPrefs.GetString(LAST_CHECK_KEY, "0");
            if (DateTime.TryParse(lastCheckStr, out DateTime lastCheck))
            {
                if ((DateTime.Now - lastCheck).TotalSeconds > CHECK_INTERVAL_SECONDS)
                {
                    CheckForNamingIssues(false);
                    EditorPrefs.SetString(LAST_CHECK_KEY, DateTime.Now.ToString());
                }
            }
            else
            {
                EditorPrefs.SetString(LAST_CHECK_KEY, DateTime.Now.ToString());
            }
        }

        private static void CheckForNamingIssuesDelayed()
        {
            // Delayed call for startup
            EditorApplication.delayCall += () => CheckForNamingIssues(true);
        }

        #endregion

        #region Issue Detection Logic

        public static bool CheckForNamingIssues(bool isStartupCheck = false)
        {
            var uiManager = FindUIManagerInScene();
            if (uiManager == null) return false;

            var issues = AnalyzeNamingIssues();

            if (issues.TotalBadElements == 0)
            {
                // Clear any existing suggestions if issues are resolved
                ClearSuggestionState();
                return false;
            }

            // Handle different scenarios
            if (isStartupCheck && IsFirstTimeUser())
            {
                HandleFirstTimeUser(uiManager, issues);
            }
            else if (issues.TotalBadElements >= SUGGESTION_THRESHOLD && !IsSuggestionRecentlyDismissed())
            {
                ShowGentleSuggestion(uiManager, issues);
            }

            return issues.TotalBadElements > 0;
        }

        public static bool HasCriticalNamingIssues()
        {
            var issues = AnalyzeNamingIssues();
            return issues.TotalBadElements > 0; // Any bad naming is considered critical for code generation
        }

        public static NamingIssuesSummary AnalyzeNamingIssues()
        {
            var summary = new NamingIssuesSummary();
            var allUIElements = FindAllUIElementsInScene();

            summary.TotalElements = allUIElements.Count;
            summary.BadElements = allUIElements.Where(e => IsBadlyNamedElement(e.name)).ToList();
            summary.TotalBadElements = summary.BadElements.Count;

            return summary;
        }

        #endregion

        #region First Time User Flow

        private static bool IsFirstTimeUser()
        {
            return !EditorPrefs.GetBool(FIRST_TIME_KEY, false);
        }

        private static void HandleFirstTimeUser(UIManager uiManager, NamingIssuesSummary issues)
        {
            EditorPrefs.SetBool(FIRST_TIME_KEY, true);

            if (issues.TotalBadElements > 0)
            {
                bool openAssistant = EditorUtility.DisplayDialog(
                    "Welcome to UI Framework! 🎉",
                    $"I detected {issues.TotalBadElements} UI elements with naming issues in your scene.\n\n" +
                    "Would you like me to help fix them automatically? This will make your generated code much cleaner!\n\n" +
                    "(Don't worry, I'll show you exactly what I'm changing before applying anything)",
                    "Yes, Help Me Fix Them!",
                    "I'll Fix Them Later");

                if (openAssistant)
                {
                    SmartNamingAssistant.ShowWindow(uiManager);
                }
                else
                {
                    Debug.Log("💡 UI Framework Tip: You can always access the Smart Naming Assistant from the UIManager inspector's 'Smart Naming' tab!");
                }
            }
        }

        #endregion

        #region Gentle Suggestions

        private static bool IsSuggestionRecentlyDismissed()
        {
            string dismissedStr = EditorPrefs.GetString(SUGGESTION_DISMISSED_KEY, "0");
            if (DateTime.TryParse(dismissedStr, out DateTime dismissed))
            {
                // Don't show again for 1 hour after dismissal
                return (DateTime.Now - dismissed).TotalHours < 1;
            }
            return false;
        }

        private static void ShowGentleSuggestion(UIManager uiManager, NamingIssuesSummary issues)
        {
            // Show a non-intrusive console suggestion
            Debug.Log($"💡 <color=orange>Smart Naming Suggestion:</color> Found {issues.TotalBadElements} UI elements with naming issues. " +
                     "Use the Smart Naming Assistant to fix them automatically and improve your code generation!");

            // Also show occasional popup (less frequent)
            if (UnityEngine.Random.Range(0f, 1f) < 0.3f) // 30% chance
            {
                int choice = EditorUtility.DisplayDialogComplex(
                    "💡 Smart Naming Suggestion",
                    $"I found {issues.TotalBadElements} UI elements with naming issues.\n\n" +
                    "Fixing them will make your generated code much cleaner and more readable.\n\n" +
                    "Sample issues: {string.Join(\", \", issues.BadElements.Take(3).Select(e => e.name))}",
                    "Fix Now",
                    "Remind Me Later",
                    "Don't Suggest Again");

                switch (choice)
                {
                    case 0: // Fix Now
                        SmartNamingAssistant.ShowWindow(uiManager);
                        break;
                    case 1: // Remind Later
                        // Set short dismissal (30 minutes)
                        EditorPrefs.SetString(SUGGESTION_DISMISSED_KEY, DateTime.Now.AddMinutes(-30).ToString());
                        break;
                    case 2: // Don't suggest again
                        EditorPrefs.SetString(SUGGESTION_DISMISSED_KEY, DateTime.MaxValue.ToString());
                        break;
                }
            }
        }

        private static void ClearSuggestionState()
        {
            if (EditorPrefs.HasKey(SUGGESTION_DISMISSED_KEY))
            {
                EditorPrefs.DeleteKey(SUGGESTION_DISMISSED_KEY);
            }
        }

        #endregion

        #region Code Generation Blocking

        public static bool ValidateNamingForCodeGeneration(UIManager uiManager, string operationType)
        {
            if (!HasCriticalNamingIssues()) return true;

            var issues = AnalyzeNamingIssues();

            int choice = EditorUtility.DisplayDialogComplex(
                $"⚠️ Naming Issues Detected",
                $"Found {issues.TotalBadElements} poorly named UI elements that will create messy generated code.\n\n" +
                $"Sample issues:\n{string.Join("\n", issues.BadElements.Take(5).Select(e => $"• {e.name}"))}" +
                (issues.TotalBadElements > 5 ? $"\n... and {issues.TotalBadElements - 5} more" : "") + "\n\n" +
                $"For best results, fix these naming issues before {operationType.ToLower()}.",
                "Fix Names First",
                $"Generate Anyway",
                "Cancel");

            switch (choice)
            {
                case 0: // Fix Names First
                    SmartNamingAssistant.ShowWindow(uiManager);
                    return false; // Block generation
                case 1: // Generate Anyway
                    if (EditorUtility.DisplayDialog("⚠️ Are You Sure?",
                        "Generating code with bad naming will create messy, hard-to-use constants.\n\n" +
                        "You'll likely need to regenerate after fixing names anyway.",
                        "Generate With Bad Names", "Let Me Fix Names First"))
                    {
                        Debug.LogWarning("⚠️ UI Framework: Generated code with poor naming. Consider fixing names and regenerating for better results.");
                        return true; // Allow but warn
                    }
                    else
                    {
                        SmartNamingAssistant.ShowWindow(uiManager);
                        return false; // Changed mind, go fix names
                    }
                case 2: // Cancel
                default:
                    return false; // Block generation
            }
        }

        #endregion

        #region Helper Methods

        private static UIManager FindUIManagerInScene()
        {
            return UnityEngine.Object.FindObjectOfType<UIManager>();
        }

        private static List<GameObject> FindAllUIElementsInScene()
        {
            var uiElements = new List<GameObject>();
            var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();

            foreach (var canvas in canvases)
            {
                FindUIElementsRecursive(canvas.transform, uiElements);
            }

            return uiElements;
        }

        private static void FindUIElementsRecursive(Transform parent, List<GameObject> uiElements)
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

        private static bool IsUIElementForNaming(GameObject obj)
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

        private static bool IsBadlyNamedElement(string name)
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

        #endregion

        #region Data Classes

        public class NamingIssuesSummary
        {
            public int TotalElements { get; set; }
            public int TotalBadElements { get; set; }
            public List<GameObject> BadElements { get; set; } = new List<GameObject>();
        }

        #endregion

        #region Public API for Manual Checks

        /// <summary>
        /// Force a manual check for naming issues (used by UI elements)
        /// </summary>
        public static void ForceNamingCheck()
        {
            CheckForNamingIssues(false);
        }

        /// <summary>
        /// Reset first-time user state (useful for testing)
        /// </summary>
        [MenuItem("Tools/UI Framework/Reset First Time User")]
        public static void ResetFirstTimeUser()
        {
            EditorPrefs.DeleteKey(FIRST_TIME_KEY);
            EditorPrefs.DeleteKey(SUGGESTION_DISMISSED_KEY);
            EditorPrefs.DeleteKey(LAST_CHECK_KEY);
            Debug.Log("🔄 Reset first-time user state. Next scene load will trigger welcome flow.");
        }

        #endregion
    }
#endif