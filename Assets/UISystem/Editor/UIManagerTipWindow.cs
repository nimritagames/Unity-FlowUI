#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public partial class UIManagerEditor
{
    /// <summary>
    /// Small popup window for quick tips.
    /// </summary>
    public class UIManagerTipWindow : EditorWindow
    {
        private string tipTitle;
        private string tipMessage;

        public static void ShowWindow(string title, string message)
        {
            UIManagerTipWindow window = GetWindow<UIManagerTipWindow>(true, title, true);
            window.tipTitle = title;
            window.tipMessage = message;
            window.position = new Rect(
                Screen.width / 2 - 150,
                Screen.height / 2 - 75,
                300,
                150
            );
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.Space(10);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 14;
            titleStyle.alignment = TextAnchor.MiddleCenter;

            EditorGUILayout.LabelField(tipTitle, titleStyle);

            EditorGUILayout.Space(10);

            GUIStyle messageStyle = new GUIStyle(EditorStyles.label);
            messageStyle.wordWrap = true;
            messageStyle.alignment = TextAnchor.MiddleCenter;

            EditorGUILayout.LabelField(tipMessage, messageStyle, GUILayout.Height(50));

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Got it!"))
            {
                Close();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif