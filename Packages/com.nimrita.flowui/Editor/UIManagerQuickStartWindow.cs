#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public partial class UIManagerEditor
{
    /// <summary>
    /// Quick start guide window to help users get started.
    /// </summary>
    public class UIManagerQuickStartWindow : EditorWindow
    {
        private int currentPage = 0;
        private Texture2D[] tutorialImages;
        private string[] tutorialTexts = new string[]
        {
        "Welcome to UI Framework! Let's get started in 3 easy steps.",
        "Step 1: Add UI elements from the scene using the UI Hierarchy panel. Click the Add button next to UI elements.",
        "Step 2: Generate the UI Library to create code access to your UI. This creates constants for paths.",
        "Step 3: Create handlers to manage your UI interactions. These handle events and state."
        };

        private UIManager uiManager;

        public static void ShowWindow(UIManager manager)
        {
            UIManagerQuickStartWindow window = GetWindow<UIManagerQuickStartWindow>(true, "UI Framework Quick Start", true);
            window.uiManager = manager;
            window.position = new Rect(
                Screen.width / 2 - 300,
                Screen.height / 2 - 200,
                600,
                400
            );
            window.Show();
        }

        private void OnEnable()
        {
            tutorialImages = new Texture2D[4];
            for (int i = 0; i < 4; i++)
            {
                tutorialImages[i] = Resources.Load<Texture2D>($"UITutorial/step{i}");
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            // Tutorial content
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 14;
            titleStyle.alignment = TextAnchor.MiddleCenter;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(tutorialTexts[currentPage], titleStyle);
            EditorGUILayout.Space(10);

            // Show tutorial image if available
            if (tutorialImages[currentPage] != null)
            {
                Rect rect = GUILayoutUtility.GetRect(position.width - 40, 200);
                GUI.DrawTexture(rect, tutorialImages[currentPage], ScaleMode.ScaleToFit);
            }
            else
            {
                // Show placeholder if image is missing
                EditorGUILayout.LabelField("Tutorial image", GUILayout.Height(200));
            }

            EditorGUILayout.Space(20);

            // Navigation buttons
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = currentPage > 0;
            if (GUILayout.Button("← Previous", GUILayout.Width(100)))
            {
                currentPage--;
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            if (currentPage == tutorialTexts.Length - 1)
            {
                if (GUILayout.Button("Get Started", GUILayout.Width(100)))
                {
                    Close();
                    // Focus on the UIManager
                    Selection.activeObject = uiManager;
                }
            }
            else
            {
                if (GUILayout.Button("Next →", GUILayout.Width(100)))
                {
                    currentPage++;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
}
#endif