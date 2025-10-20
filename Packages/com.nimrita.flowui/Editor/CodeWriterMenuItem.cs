#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu items for opening the Code Writer window.
/// </summary>
public static class CodeWriterMenuItem
{
    [MenuItem("Tools/UI System/Code Writer", false, 100)]
    public static void OpenCodeWriter()
    {
        CodeWriterWindow.ShowWindow("", "NewScript.cs", "");
    }

    [MenuItem("Tools/UI System/Code Writer (With Template)", false, 101)]
    public static void OpenCodeWriterWithTemplate()
    {
        string template = GenerateDefaultTemplate();
        CodeWriterWindow.ShowWindow(template, "NewScript.cs", "");
    }

    private static string GenerateDefaultTemplate()
    {
        return @"using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace YourNamespace
{
    /// <summary>
    /// Description of your class
    /// </summary>
    public class NewScript : MonoBehaviour
    {
        #region Fields

        [Header(""Dependencies"")]
        [SerializeField] private UIManager uiManager;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            // Initialize
        }

        private void Start()
        {
            // Setup
        }

        #endregion

        #region Public Methods

        // Your public methods here

        #endregion

        #region Private Methods

        // Your private methods here

        #endregion
    }
}
";
    }
}
#endif
