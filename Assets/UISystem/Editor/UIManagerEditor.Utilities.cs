#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public partial class UIManagerEditor : Editor
{
    #region Helper Methods

    /// <summary>
    /// Pings the generated UI Library file in the Project window.
    /// </summary>
    private void PingUILibrary()
    {
        string sceneName = uiManager.gameObject.scene.name;
        string sanitizedSceneName = SanitizeIdentifier(sceneName);
        string filePath = Path.Combine(libraryOutputPath, $"{libraryClassPrefix}{sanitizedSceneName}.cs");

        // Convert project-relative path to absolute
        string fullPath = Path.GetFullPath(filePath);
        string assetsPath = Path.GetFullPath("Assets");

        if (fullPath.StartsWith(assetsPath))
        {
            string relativePath = filePath.Replace('\\', '/');
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);

            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
                AssetDatabase.OpenAsset(asset);
            }
            else
            {
                Debug.LogWarning($"Could not find UI library file at: {relativePath}");
                EditorUtility.RevealInFinder(fullPath);
            }
        }
        else
        {
            Debug.LogWarning($"Library path is outside of project: {fullPath}");
        }
    }

    /// <summary>
    /// Pings the generated UI Handler file in the Project window.
    /// </summary>
    private void PingUIHandler()
    {
        string sceneName = uiManager.gameObject.scene.name;
        string sanitizedSceneName = SanitizeIdentifier(sceneName);
        string filePath = Path.Combine(handlerOutputPath, $"{handlerClassPrefix}{sanitizedSceneName}UIHandler.cs");

        // Convert project-relative path to absolute
        string fullPath = Path.GetFullPath(filePath);
        string assetsPath = Path.GetFullPath("Assets");

        if (fullPath.StartsWith(assetsPath))
        {
            string relativePath = filePath.Replace('\\', '/');
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);

            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
                AssetDatabase.OpenAsset(asset);
            }
            else
            {
                Debug.LogWarning($"Could not find UI handler file at: {relativePath}");
                EditorUtility.RevealInFinder(fullPath);
            }
        }
        else
        {
            Debug.LogWarning($"Handler path is outside of project: {fullPath}");
        }
    }

    /// <summary>
    /// Sanitizes an input string to create a valid C# identifier.
    /// Replaces any character that is not a letter, digit, or underscore with an underscore.
    /// Also trims duplicate underscores and ensures the identifier does not start with a digit.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <returns>A sanitized string safe for use as an identifier or file name.</returns>
    private string SanitizeIdentifier(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "Unnamed";

        // Replace any character that is not a letter, digit, or underscore with an underscore.
        string sanitized = Regex.Replace(input, @"[^a-zA-Z0-9_]", "_");

        // Replace multiple underscores with a single underscore.
        sanitized = Regex.Replace(sanitized, @"_+", "_");

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

    private void DrawProgressIndicator()
    {
        EditorGUILayout.Space(10);

        // Calculate progress
        float progress = 0;
        int totalSteps = 3;
        int completedSteps = 0;

        if (addedUIElements.Count > 0) completedSteps++;
        if (IsLibraryGenerated(uiManager.gameObject.scene.name)) completedSteps++;
        if (HandlerFileExists()) completedSteps++;

        progress = (float)completedSteps / totalSteps;

        // Progress bar container
        Rect progressRect = EditorGUILayout.GetControlRect(false, 24);
        float innerWidth = progressRect.width - 20;

        // Status text
        GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        string statusText = completedSteps >= totalSteps ?
            "Setup Complete! Your UI Framework is ready to use." :
            $"UI Framework Setup: {completedSteps}/{totalSteps} steps completed";

        EditorGUI.LabelField(
            new Rect(progressRect.x + 10, progressRect.y, innerWidth * 0.7f, progressRect.height),
            statusText,
            statusStyle
        );

        // Progress percentage
        GUIStyle percentStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        EditorGUI.LabelField(
            new Rect(progressRect.x + innerWidth * 0.7f, progressRect.y, innerWidth * 0.3f, progressRect.height),
            $"{(int)(progress * 100)}%",
            percentStyle
        );

        // Progress bar
        Rect barContainerRect = new Rect(progressRect.x + 10, progressRect.y + progressRect.height - 6, innerWidth, 4);

        // Background
        EditorGUI.DrawRect(barContainerRect, new Color(0.2f, 0.2f, 0.2f));

        // Progress fill
        Color progressColor = progress >= 1 ?
            new Color(0.2f, 0.7f, 0.2f) : // Green for complete
            new Color(0.2f, 0.4f, 0.7f);  // Blue for in-progress

        EditorGUI.DrawRect(
            new Rect(barContainerRect.x, barContainerRect.y, barContainerRect.width * progress, barContainerRect.height),
            progressColor
        );
    }

    #endregion
}
#endif