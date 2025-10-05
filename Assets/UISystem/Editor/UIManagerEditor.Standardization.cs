#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.IO;

public partial class UIManagerEditor : Editor
{
    #region UI Element Naming Standardization

    // Standardization settings
    [Serializable]
    public class NamingConventionSettings
    {
        public bool EnableAutoStandardization = true;
        public NamingConventionType ConventionType = NamingConventionType.PascalCase_Suffix;
        public bool StandardizeChildElements = true;
        public bool RespectExistingConventions = true;
        public bool AutoStandardizeOnAdd = true;
    }

    public enum NamingConventionType
    {
        PascalCase_Suffix,    // PlayButton
        Snake_Case_Suffix,    // Play_Button
        Camel_case_Suffix,    // playButton
        PascalCase_Prefix,    // ButtonPlay
        Snake_Case_Prefix,    // Button_Play
        Camel_case_Prefix     // buttonPlay
    }

    // Store settings
    private NamingConventionSettings namingSettings = new NamingConventionSettings();

    // Dictionary for storing original names before standardization
    private Dictionary<GameObject, string> originalNames = new Dictionary<GameObject, string>();

    /// <summary>
    /// Standardizes the name of a UI element based on its type.
    /// </summary>
    /// <param name="uiElement">The UI element to standardize.</param>
    /// <param name="forceStandardize">Whether to force standardization even if settings would normally prevent it.</param>
    /// <returns>True if the name was changed, false otherwise.</returns>
    private bool StandardizeUIElementName(GameObject uiElement, bool forceStandardize = false)
    {
        if (uiElement == null || (!namingSettings.EnableAutoStandardization && !forceStandardize))
            return false;

        // Save original name for reference if we haven't already
        if (!originalNames.ContainsKey(uiElement))
        {
            originalNames[uiElement] = uiElement.name;
        }

        string originalName = uiElement.name;

        // Skip if already standardized and we're respecting existing conventions
        if (namingSettings.RespectExistingConventions && !forceStandardize && IsAlreadyStandardized(uiElement))
        {
            return false;
        }

        string standardizedName = GetStandardizedName(uiElement);

        if (originalName != standardizedName)
        {
            // Register undo operation
            Undo.RecordObject(uiElement, "Standardize UI Element Name");

            // Apply new name
            uiElement.name = standardizedName;

            // Mark dirty
            EditorUtility.SetDirty(uiElement);

            // If enabled, standardize child elements
            if (namingSettings.StandardizeChildElements || forceStandardize)
            {
                StandardizeChildElements(uiElement);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the type indicator part of an element name
    /// </summary>
    private string GetTypeIndicator(GameObject uiElement)
    {
        string typeSuffix = DetermineTypeSuffix(uiElement);
        if (string.IsNullOrEmpty(typeSuffix))
            return string.Empty;

        return typeSuffix.Substring(1); // Remove the leading "_"
    }

    /// <summary>
    /// Standardizes child elements based on parent, with special handling for complex UI components.
    /// </summary>
    private void StandardizeChildElements(GameObject parent)
    {
        if (parent == null) return;

        string parentName = GetNameWithoutTypeSuffix(parent.name);
        string parentType = GetTypeIndicator(parent);

        // If the parent name (without type) is empty or just the type name itself,
        // use only the type for child naming to avoid duplication like "Button_Button_Text"
        if (string.IsNullOrWhiteSpace(parentName) || parentName.Equals(parentType, StringComparison.OrdinalIgnoreCase))
        {
            parentName = "";
        }

        // Special handling for InputField (has Text and Placeholder)
        InputField inputField = parent.GetComponent<InputField>();
        TMP_InputField tmpInputField = parent.GetComponent<TMP_InputField>();

        if (inputField != null || tmpInputField != null)
        {
            // Find all Text/TMP_Text children
            var inputTextElements = parent.GetComponentsInChildren<Text>(true);
            var inputTmpTextElements = parent.GetComponentsInChildren<TMP_Text>(true);

            foreach (var element in inputTextElements)
            {
                // Skip if this is the parent itself
                if (element.gameObject == parent) continue;

                // Determine if this is a placeholder or the main text field
                string childType = "Text";
                if ((inputField != null && inputField.placeholder == element) ||
                    element.gameObject.name.Contains("Placeholder", StringComparison.OrdinalIgnoreCase))
                {
                    childType = "Placeholder";
                }

                // Create a name that shows relationship to parent with proper role (Nnnn_Nnnn format)
                string inputTextName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_{childType}") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_{childType}");
                StoreOriginalNameAndRename(element.gameObject, inputTextName);
            }

            foreach (var element in inputTmpTextElements)
            {
                // Skip if this is the parent itself
                if (element.gameObject == parent) continue;

                // Determine if this is a placeholder or the main text field
                string childType = "Text";
                if ((tmpInputField != null && tmpInputField.placeholder == element) ||
                    element.gameObject.name.Contains("Placeholder", StringComparison.OrdinalIgnoreCase))
                {
                    childType = "Placeholder";
                }

                // Create a name that shows relationship to parent with proper role (Nnnn_Nnnn format)
                string inputTmpTextName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_{childType}") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_{childType}");
                StoreOriginalNameAndRename(element.gameObject, inputTmpTextName);
            }

            return; // Skip the rest of the method since we've handled the special case
        }

        // Special handling for Toggle (checkmark and label)
        Toggle toggle = parent.GetComponent<Toggle>();
        if (toggle != null)
        {
            // Handle the checkmark (usually an Image component)
            if (toggle.graphic != null)
            {
                GameObject checkmarkObj = toggle.graphic.gameObject;
                string checkmarkName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_Checkmark") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Checkmark");
                StoreOriginalNameAndRename(checkmarkObj, checkmarkName);
            }

            // Find text that might be the label
            Text[] toggleTextElements = parent.GetComponentsInChildren<Text>(true);
            TMP_Text[] toggleTmpTextElements = parent.GetComponentsInChildren<TMP_Text>(true);

            // Handle regular Text components
            foreach (var text in toggleTextElements)
            {
                if (text.gameObject == parent) continue;
                if (toggle.graphic != null && text.gameObject == toggle.graphic.gameObject) continue; // Skip the checkmark

                string toggleLabelName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_Label") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Label");
                StoreOriginalNameAndRename(text.gameObject, toggleLabelName);
            }

            // Handle TMP_Text components
            foreach (var tmpText in toggleTmpTextElements)
            {
                if (tmpText.gameObject == parent) continue;
                if (toggle.graphic != null && tmpText.gameObject == toggle.graphic.gameObject) continue;

                string toggleTmpLabelName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_Label") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Label");
                StoreOriginalNameAndRename(tmpText.gameObject, toggleTmpLabelName);
            }

            return; // Skip the rest of the method
        }

        // Special handling for Dropdown
        Dropdown dropdown = parent.GetComponent<Dropdown>();
        TMP_Dropdown tmpDropdown = parent.GetComponent<TMP_Dropdown>();

        if (dropdown != null || tmpDropdown != null)
        {
            // Handle the main dropdown components
            Transform captionTrans = parent.transform.Find("Label");
            Transform arrowTrans = parent.transform.Find("Arrow");
            Transform templateTrans = parent.transform.Find("Template");

            if (captionTrans != null)
            {
                string captionName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_Label") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Label");
                StoreOriginalNameAndRename(captionTrans.gameObject, captionName);
            }

            if (arrowTrans != null)
            {
                string arrowName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_Arrow") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Arrow");
                StoreOriginalNameAndRename(arrowTrans.gameObject, arrowName);
            }

            if (templateTrans != null)
            {
                string templateName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_Template") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Template");
                StoreOriginalNameAndRename(templateTrans.gameObject, templateName);

                // Handle common template children
                Transform scrollbarTrans = templateTrans.Find("Scrollbar");
                Transform viewportTrans = templateTrans.Find("Viewport");
                Transform contentTrans = viewportTrans?.Find("Content");

                if (scrollbarTrans != null)
                {
                    string scrollbarName = string.IsNullOrEmpty(parentName) ?
                        ToPascalCaseWithUnderscores($"{parentType}_Scrollbar") :
                        ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Scrollbar");
                    StoreOriginalNameAndRename(scrollbarTrans.gameObject, scrollbarName);
                }

                if (viewportTrans != null)
                {
                    string viewportName = string.IsNullOrEmpty(parentName) ?
                        ToPascalCaseWithUnderscores($"{parentType}_Viewport") :
                        ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Viewport");
                    StoreOriginalNameAndRename(viewportTrans.gameObject, viewportName);
                }

                if (contentTrans != null)
                {
                    string contentName = string.IsNullOrEmpty(parentName) ?
                        ToPascalCaseWithUnderscores($"{parentType}_Content") :
                        ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Content");
                    StoreOriginalNameAndRename(contentTrans.gameObject, contentName);
                }
            }

            return; // Skip the rest of the method
        }

        // Special handling for Slider
        Slider slider = parent.GetComponent<Slider>();
        if (slider != null)
        {
            // Handle the fill area
            if (slider.fillRect != null && slider.fillRect.parent != null)
            {
                GameObject fillArea = slider.fillRect.parent.gameObject;
                string fillAreaName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_Fill_Area") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Fill_Area");
                StoreOriginalNameAndRename(fillArea, fillAreaName);

                // Handle the fill itself
                string fillName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_Fill") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Fill");
                StoreOriginalNameAndRename(slider.fillRect.gameObject, fillName);
            }

            // Handle the handle
            if (slider.handleRect != null)
            {
                string handleName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_Handle") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Handle");
                StoreOriginalNameAndRename(slider.handleRect.gameObject, handleName);

                // Handle parent of handle
                if (slider.handleRect.parent != slider.transform)
                {
                    string handleAreaName = string.IsNullOrEmpty(parentName) ?
                        ToPascalCaseWithUnderscores($"{parentType}_Handle_Slide_Area") :
                        ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Handle_Slide_Area");
                    StoreOriginalNameAndRename(slider.handleRect.parent.gameObject, handleAreaName);
                }
            }

            return; // Skip the rest of the method
        }

        // Handle ScrollRect
        ScrollRect scrollRect = parent.GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            // Handle viewport
            if (scrollRect.viewport != null)
            {
                string srViewportName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_Viewport") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Viewport");
                StoreOriginalNameAndRename(scrollRect.viewport.gameObject, srViewportName);
            }

            // Handle content
            if (scrollRect.content != null)
            {
                string srContentName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_Content") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Content");
                StoreOriginalNameAndRename(scrollRect.content.gameObject, srContentName);
            }

            // Handle scrollbars
            if (scrollRect.horizontalScrollbar != null)
            {
                string hScrollbarName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_H_Scrollbar") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_H_Scrollbar");
                StoreOriginalNameAndRename(scrollRect.horizontalScrollbar.gameObject, hScrollbarName);
            }

            if (scrollRect.verticalScrollbar != null)
            {
                string vScrollbarName = string.IsNullOrEmpty(parentName) ?
                    ToPascalCaseWithUnderscores($"{parentType}_V_Scrollbar") :
                    ToPascalCaseWithUnderscores($"{parentName}_{parentType}_V_Scrollbar");
                StoreOriginalNameAndRename(scrollRect.verticalScrollbar.gameObject, vScrollbarName);
            }

            return; // Skip the rest of the method
        }

        // Default handling for other components

        // Handle text elements
        Text[] defaultTextElements = parent.GetComponentsInChildren<Text>(true);
        foreach (var text in defaultTextElements)
        {
            // Skip if this is the parent itself
            if (text.gameObject == parent) continue;

            string defaultTextName;
            if (string.IsNullOrEmpty(parentName))
            {
                defaultTextName = ToPascalCaseWithUnderscores($"{parentType}_Text");
            }
            else if (string.IsNullOrEmpty(parentType))
            {
                defaultTextName = ToPascalCaseWithUnderscores($"{parentName}_Text");
            }
            else
            {
                defaultTextName = ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Text");
            }

            StoreOriginalNameAndRename(text.gameObject, defaultTextName);
        }

        // Handle TMP_Text elements
        TMP_Text[] defaultTmpTexts = parent.GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmpText in defaultTmpTexts)
        {
            // Skip if this is the parent itself
            if (tmpText.gameObject == parent) continue;

            string defaultTmpTextName;
            if (string.IsNullOrEmpty(parentName))
            {
                defaultTmpTextName = ToPascalCaseWithUnderscores($"{parentType}_Text");
            }
            else if (string.IsNullOrEmpty(parentType))
            {
                defaultTmpTextName = ToPascalCaseWithUnderscores($"{parentName}_Text");
            }
            else
            {
                defaultTmpTextName = ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Text");
            }

            StoreOriginalNameAndRename(tmpText.gameObject, defaultTmpTextName);
        }

        // Handle images
        Image[] defaultImages = parent.GetComponentsInChildren<Image>(true);
        foreach (var image in defaultImages)
        {
            // Skip if this is the parent itself
            if (image.gameObject == parent) continue;

            // Skip if this image has another UI component
            if (image.GetComponent<Button>() != null ||
                image.GetComponent<Toggle>() != null ||
                image.GetComponent<Slider>() != null)
                continue;

            string defaultImageName;
            if (string.IsNullOrEmpty(parentName))
            {
                defaultImageName = ToPascalCaseWithUnderscores($"{parentType}_Image");
            }
            else if (string.IsNullOrEmpty(parentType))
            {
                defaultImageName = ToPascalCaseWithUnderscores($"{parentName}_Image");
            }
            else
            {
                defaultImageName = ToPascalCaseWithUnderscores($"{parentName}_{parentType}_Image");
            }

            StoreOriginalNameAndRename(image.gameObject, defaultImageName);
        }
    }

    // Helper method to store original name and apply the new one
    private void StoreOriginalNameAndRename(GameObject gameObject, string newName)
    {
        if (gameObject == null || gameObject.name == newName)
            return;

        // Store original name
        if (!originalNames.ContainsKey(gameObject))
        {
            originalNames[gameObject] = gameObject.name;
        }

        // Apply the new name with undo support
        Undo.RecordObject(gameObject, "Standardize Child UI Element Name");
        gameObject.name = newName;
        EditorUtility.SetDirty(gameObject);
    }

    /// <summary>
    /// Checks if an element is already standardized according to our conventions.
    /// </summary>
    private bool IsAlreadyStandardized(GameObject uiElement)
    {
        if (uiElement == null) return false;

        // First check if name contains Unity artifacts that need cleanup
        if (ContainsUnityArtifacts(uiElement.name))
            return false; // Needs standardization to clean up artifacts

        string typeSuffix = DetermineTypeSuffix(uiElement);
        if (string.IsNullOrEmpty(typeSuffix)) return false;

        // Type indicator without the underscore
        string typeIndicator = typeSuffix.Substring(1);

        // Check if name already contains the type as a suffix (regardless of casing)
        if (uiElement.name.EndsWith(typeIndicator, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if name already contains the type as a suffix with underscore
        if (uiElement.name.EndsWith(typeSuffix, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if name starts with the type as a prefix (regardless of casing)
        if (uiElement.name.StartsWith(typeIndicator, StringComparison.OrdinalIgnoreCase) &&
            uiElement.name.Length > typeIndicator.Length)
            return true;

        // Check common abbreviations
        var abbreviations = GetTypeAbbreviations();
        string lowercaseName = uiElement.name.ToLowerInvariant();

        foreach (var abbr in abbreviations)
        {
            if (typeIndicator.Equals(abbr.Value, StringComparison.OrdinalIgnoreCase))
            {
                // Check if name contains abbreviation as suffix
                if (lowercaseName.EndsWith(abbr.Key.ToLowerInvariant()))
                    return true;

                // Check if name starts with abbreviation as prefix
                if (lowercaseName.StartsWith(abbr.Key.ToLowerInvariant()))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the name contains Unity artifacts like (Legacy), (1), (2), etc.
    /// </summary>
    private bool ContainsUnityArtifacts(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        // Check for (Legacy)
        if (Regex.IsMatch(name, @"\(Legacy\)", RegexOptions.IgnoreCase))
            return true;

        // Check for duplicate markers like (1), (2), etc.
        if (Regex.IsMatch(name, @"\(\d+\)"))
            return true;

        // Check for excessive spaces
        if (name.Contains("  "))
            return true;

        return false;
    }

    /// <summary>
    /// Gets the standardized name for a UI element based on its type.
    /// </summary>
    private string GetStandardizedName(GameObject uiElement)
    {
        string baseName = uiElement.name;
        string typeMarker = DetermineTypeSuffix(uiElement);

        if (string.IsNullOrEmpty(typeMarker)) return baseName;

        // Extract type without the underscore
        string typeIndicator = typeMarker.Substring(1); // Remove the leading "_"

        // Clean up the base name first (remove Legacy, extra spaces, etc.)
        baseName = CleanupElementName(baseName);

        // Remove any type indicators from the base name
        baseName = RemoveExistingTypePatterns(baseName);

        // If after removing type patterns, the name is empty or just whitespace,
        // it means the original name was ONLY the type (e.g., "Button (Legacy)" -> "Button" -> "")
        // In this case, just use the type name itself without adding it twice
        if (string.IsNullOrWhiteSpace(baseName))
        {
            return ToPascalCaseWithUnderscores(typeIndicator);
        }

        // Apply standard formatting - Always use Nnnn_Nnnn_Type format
        return FormatElementName(baseName, typeIndicator, true);
    }

    /// <summary>
    /// Cleans up element name by removing Unity artifacts like "(Legacy)" and extra spaces
    /// </summary>
    private string CleanupElementName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Remove "(Legacy)" text that appears with legacy UI components
        name = Regex.Replace(name, @"\s*\(Legacy\)\s*", "", RegexOptions.IgnoreCase);

        // Remove other Unity artifacts like (1), (2), etc.
        name = Regex.Replace(name, @"\s*\(\d+\)\s*", "");

        // Remove extra whitespace
        name = Regex.Replace(name, @"\s+", " ").Trim();

        // Replace spaces with underscores for consistency
        name = name.Replace(" ", "_");

        return name;
    }

    /// <summary>
    /// Formats an element name according to the chosen naming convention.
    /// Default format is Nnnn_Nnnn_Nnnn (PascalCase words separated by underscores)
    /// </summary>
    /// <param name="baseName">The base name without type markers.</param>
    /// <param name="typeMarker">The type marker (without leading underscore).</param>
    /// <param name="includeTypeMarker">Whether to include the type marker.</param>
    private string FormatElementName(string baseName, string typeMarker, bool includeTypeMarker)
    {
        if (!includeTypeMarker)
            return ToPascalCaseWithUnderscores(baseName);

        // Clean and format the base name to Nnnn_Nnnn format
        baseName = ToPascalCaseWithUnderscores(baseName);

        // Convert the baseName to the appropriate case style
        switch (namingSettings.ConventionType)
        {
            case NamingConventionType.PascalCase_Suffix:
                return $"{baseName}_{typeMarker}";

            case NamingConventionType.Snake_Case_Suffix:
                // Even in snake_case, we want Nnnn_Nnnn_Type format
                return $"{baseName}_{typeMarker}";

            case NamingConventionType.Camel_case_Suffix:
                return $"{ToCamelCase(baseName)}_{typeMarker}";

            case NamingConventionType.PascalCase_Prefix:
                return $"{ToPascalCase(typeMarker)}{ToPascalCase(baseName)}";

            case NamingConventionType.Snake_Case_Prefix:
                return $"{ToSnakeCase(typeMarker)}_{baseName}";

            case NamingConventionType.Camel_case_Prefix:
                return $"{ToCamelCase(typeMarker)}{ToPascalCase(baseName)}";

            default:
                return $"{baseName}_{typeMarker}";
        }
    }

    /// <summary>
    /// Converts a string to PascalCase with underscores between words (Nnnn_Nnnn_Nnnn format)
    /// </summary>
    private string ToPascalCaseWithUnderscores(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Split by underscores, spaces, and camelCase boundaries
        string[] words = Regex.Split(input, @"[_\s]+|(?<!^)(?=[A-Z])");

        // Filter out empty words and convert each to PascalCase
        var pascalWords = words
            .Where(w => !string.IsNullOrEmpty(w))
            .Select(w => char.ToUpper(w[0]) + (w.Length > 1 ? w.Substring(1).ToLowerInvariant() : ""))
            .ToArray();

        return string.Join("_", pascalWords);
    }

    /// <summary>
    /// Gets a dictionary of common abbreviations for UI types
    /// </summary>
    private Dictionary<string, string> GetTypeAbbreviations()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"BTN", "Button"},
            {"TXT", "Text"},
            {"IMG", "Image"},
            {"PNL", "Panel"},
            {"TGL", "Toggle"},
            {"SLD", "Slider"},
            {"INP", "InputField"},
            {"DRP", "Dropdown"},
            {"SCRV", "ScrollView"}
        };
    }

    /// <summary>
    /// Removes existing type suffixes and prefixes from a name.
    /// </summary>
    private string RemoveExistingTypePatterns(string name)
    {
        string[] knownTypes = new string[] {
            "Button", "Toggle", "Slider", "Panel", "Text",
            "Image", "Dropdown", "InputField", "ScrollView",
            "Label", "Field"
        };

        // Check if the name IS EXACTLY a type (e.g., "Button", "Text")
        foreach (var type in knownTypes)
        {
            if (name.Equals(type, StringComparison.OrdinalIgnoreCase))
            {
                return ""; // Return empty string, will be handled by caller
            }
        }

        // Check for suffix pattern (Name_Type)
        foreach (var type in knownTypes)
        {
            if (name.EndsWith($"_{type}", StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(0, name.Length - (type.Length + 1));
            }
        }

        // Check for prefix pattern (TypeName)
        foreach (var type in knownTypes)
        {
            if (name.StartsWith($"{type}", StringComparison.OrdinalIgnoreCase) &&
                name.Length > type.Length &&
                char.IsUpper(name[type.Length]))
            {
                return name.Substring(type.Length);
            }
        }

        // Check for snake case prefix (type_name)
        foreach (var type in knownTypes)
        {
            if (name.StartsWith($"{type}_", StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(type.Length + 1);
            }
        }

        // Check for camelCase patterns (e.g., playButton, mainMenuPanel)
        foreach (var type in knownTypes)
        {
            // Check for camelCase pattern (e.g., playButton)
            string camelPattern = char.ToLowerInvariant(type[0]) + type.Substring(1);
            if (name.EndsWith(camelPattern, StringComparison.Ordinal) &&
                name.Length > camelPattern.Length)
            {
                return name.Substring(0, name.Length - camelPattern.Length);
            }
        }

        // Check for common abbreviations
        var abbreviations = GetTypeAbbreviations();
        foreach (var abbr in abbreviations)
        {
            if (name.EndsWith(abbr.Key, StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(0, name.Length - abbr.Key.Length);
            }
            if (name.EndsWith($"_{abbr.Key}", StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(0, name.Length - (abbr.Key.Length + 1));
            }
            if (name.StartsWith(abbr.Key, StringComparison.OrdinalIgnoreCase) &&
                name.Length > abbr.Key.Length)
            {
                return name.Substring(abbr.Key.Length);
            }
            if (name.StartsWith($"{abbr.Key}_", StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(abbr.Key.Length + 1);
            }
        }

        return name;
    }

    /// <summary>
    /// Gets the name without the type suffix, if present.
    /// </summary>
    private string GetNameWithoutTypeSuffix(string name)
    {
        string[] knownSuffixes = new string[] {
            "_Button", "_Toggle", "_Slider", "_Panel", "_Text",
            "_Image", "_Dropdown", "_InputField", "_ScrollView"
        };

        foreach (var suffix in knownSuffixes)
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(0, name.Length - suffix.Length);
            }
        }

        // Also check for common type patterns without underscore
        string[] typeNames = new string[] {
            "Button", "Toggle", "Slider", "Panel", "Text",
            "Image", "Dropdown", "InputField", "ScrollView"
        };

        foreach (var type in typeNames)
        {
            if (name.EndsWith(type, StringComparison.OrdinalIgnoreCase) &&
                name.Length > type.Length)
            {
                return name.Substring(0, name.Length - type.Length);
            }
        }

        // Check common abbreviations
        var abbreviations = GetTypeAbbreviations();
        foreach (var abbr in abbreviations)
        {
            if (name.EndsWith(abbr.Key, StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(0, name.Length - abbr.Key.Length);
            }
            if (name.EndsWith($"_{abbr.Key}", StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(0, name.Length - (abbr.Key.Length + 1));
            }
        }

        return name;
    }

    /// <summary>
    /// Determines the appropriate type suffix for a UI element.
    /// </summary>
    private string DetermineTypeSuffix(GameObject uiElement)
    {
        if (uiElement.GetComponent<Button>() != null)
            return "_Button";

        if (uiElement.GetComponent<Toggle>() != null)
            return "_Toggle";

        if (uiElement.GetComponent<Slider>() != null)
            return "_Slider";

        if (uiElement.GetComponent<Dropdown>() != null || uiElement.GetComponent<TMP_Dropdown>() != null)
            return "_Dropdown";

        if (uiElement.GetComponent<InputField>() != null || uiElement.GetComponent<TMP_InputField>() != null)
            return "_InputField";

        if (uiElement.GetComponent<ScrollRect>() != null)
            return "_ScrollView";

        if (uiElement.GetComponent<Text>() != null || uiElement.GetComponent<TMP_Text>() != null)
            return "_Text";

        if (uiElement.GetComponent<Image>() != null)
        {
            // Special case for panels
            if (uiElement.transform.childCount > 0 ||
                uiElement.name.Contains("Panel", StringComparison.OrdinalIgnoreCase))
                return "_Panel";
            else
                return "_Image";
        }

        if (uiElement.GetComponent<RawImage>() != null)
            return "_Image";

        return "";
    }

    /// <summary>
    /// Standardizes the names of all child UI elements of a transform.
    /// </summary>
    private void StandardizeUIElementsRecursively(Transform parent, bool forceStandardize = false)
    {
        if (parent == null || (!namingSettings.EnableAutoStandardization && !forceStandardize))
            return;

        // Process all children first (depth-first)
        foreach (Transform child in parent)
        {
            StandardizeUIElementsRecursively(child, forceStandardize);
        }

        // Then process the parent
        if (HasSupportedUIComponent(parent.gameObject))
        {
            StandardizeUIElementName(parent.gameObject, forceStandardize);
        }
    }

    /// <summary>
    /// Resets the name of a UI element to its original value.
    /// </summary>
    private void ResetUIElementName(GameObject uiElement)
    {
        if (uiElement == null)
            return;

        if (originalNames.TryGetValue(uiElement, out string originalName))
        {
            // Register undo operation
            Undo.RecordObject(uiElement, "Reset UI Element Name");

            // Apply original name
            uiElement.name = originalName;

            // Mark dirty
            EditorUtility.SetDirty(uiElement);

            // Remove from dictionary
            originalNames.Remove(uiElement);
        }
    }

    /// <summary>
    /// Convert a string to PascalCase.
    /// </summary>
    private string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Split by non-alphanumeric characters
        string[] words = Regex.Split(input, @"[^a-zA-Z0-9]");

        // Capitalize the first letter of each word
        for (int i = 0; i < words.Length; i++)
        {
            if (!string.IsNullOrEmpty(words[i]))
            {
                words[i] = char.ToUpper(words[i][0]) +
                           (words[i].Length > 1 ? words[i].Substring(1).ToLowerInvariant() : "");
            }
        }

        return string.Join("", words);
    }

    /// <summary>
    /// Convert a string to camelCase.
    /// </summary>
    private string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string pascalCase = ToPascalCase(input);

        return char.ToLowerInvariant(pascalCase[0]) +
               (pascalCase.Length > 1 ? pascalCase.Substring(1) : "");
    }

    /// <summary>
    /// Convert a string to snake_case.
    /// </summary>
    private string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Split by non-alphanumeric characters
        string[] words = Regex.Split(input, @"[^a-zA-Z0-9]");

        // Filter out empty words and convert to lowercase
        words = words.Where(w => !string.IsNullOrEmpty(w))
                    .Select(w => w.ToLowerInvariant())
                    .ToArray();

        return string.Join("_", words);
    }

    /// <summary>
    /// Creates a temporary GameObject for preview purposes
    /// </summary>
    private GameObject CreatePreviewObject(string name, Type componentType)
    {
        GameObject temp = new GameObject(name);
        temp.AddComponent(componentType);
        return temp;
    }

    /// <summary>
    /// Standardizes the names of all UI elements in the scene.
    /// </summary>
    private void StandardizeAllUIElements(bool forceStandardize = false)
    {
        // Get all canvases in the current scene
        var currentScene = uiManager.gameObject.scene;
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.InstanceID)
            .Where(c => c.gameObject.scene == currentScene);

        int renamedCount = 0;
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Standardize All UI Elements");
        int undoGroupIndex = Undo.GetCurrentGroup();

        foreach (var canvas in canvases)
        {
            StandardizeUIElementsRecursively(canvas.transform, forceStandardize);
            renamedCount++;
        }

        Undo.CollapseUndoOperations(undoGroupIndex);

        // Update the UIManager dictionaries to reflect the new names
        uiManager.InitializeDictionaries();
        EditorUtility.SetDirty(uiManager);

        ShowQuickTip("Naming Standardization Complete",
            $"Standardized the names of {renamedCount} UI elements. You can undo this operation with Ctrl+Z or restore original names from the Tools tab.");
    }

    /// <summary>
    /// Restores the original names of all UI elements.
    /// </summary>
    private void RestoreAllOriginalNames()
    {
        if (originalNames.Count == 0)
            return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Restore Original UI Element Names");
        int undoGroupIndex = Undo.GetCurrentGroup();

        foreach (var element in new Dictionary<GameObject, string>(originalNames))
        {
            ResetUIElementName(element.Key);
        }

        Undo.CollapseUndoOperations(undoGroupIndex);

        // Update the UIManager dictionaries to reflect the restored names
        uiManager.InitializeDictionaries();
        EditorUtility.SetDirty(uiManager);

        originalNames.Clear();

        ShowQuickTip("Original Names Restored", "All UI elements have been restored to their original names.");
    }

    /// <summary>
    /// Modified version of AddUIReference that applies naming standardization.
    /// </summary>
    public void AddUIReferenceWithStandardization(GameObject uiElement)
    {
        if (uiElement == null)
        {
            Debug.LogError("UIManager: Attempted to add a null UI element.");
            return;
        }

        // Apply name standardization if enabled and auto-standardize on add is enabled
        if (namingSettings.EnableAutoStandardization && namingSettings.AutoStandardizeOnAdd)
        {
            StandardizeUIElementName(uiElement);
        }

        // Now add the UI reference using the original method
        Undo.RecordObject(uiManager, "Add UI Reference");
        uiManager.AddUIReference(uiElement);
        EditorUtility.SetDirty(uiManager);
        addedUIElements.Add(uiElement);
    }

    #endregion

    #region Settings
    // Add these constants for EditorPrefs keys
    private const string NAMING_ENABLE_AUTO_STANDARDIZATION_KEY = "UIManager_EnableAutoStandardization";
    private const string NAMING_CONVENTION_TYPE_KEY = "UIManager_NamingConventionType";
    private const string NAMING_STANDARDIZE_CHILD_ELEMENTS_KEY = "UIManager_StandardizeChildElements";
    private const string NAMING_RESPECT_EXISTING_CONVENTIONS_KEY = "UIManager_RespectExistingConventions";
    private const string NAMING_AUTO_STANDARDIZE_ON_ADD_KEY = "UIManager_AutoStandardizeOnAdd";

    /// <summary>
    /// Get a scene-specific EditorPrefs key
    /// </summary>
    private string GetSceneSpecificPrefsKey(string baseKey)
    {
        string scenePath = uiManager.gameObject.scene.path;
        string sceneIdentifier = string.IsNullOrEmpty(scenePath) ?
            "UnsavedScene" : Path.GetFileNameWithoutExtension(scenePath);

        return $"{baseKey}_{sceneIdentifier}";
    }

    private void ModifiedOnEnable()
    {
        uiManager = (UIManager)target;
        uiCategoriesProperty = serializedObject.FindProperty("uiCategories");

        // Load library generation settings from EditorPrefs
        libraryOutputPath = EditorPrefs.GetString(LIBRARY_OUTPUT_PATH_KEY, "Assets/Scripts/UI/Generated/");
        libraryNamespace = EditorPrefs.GetString(LIBRARY_NAMESPACE_KEY, "CodeSculptLabs.UIFramework");
        libraryClassPrefix = EditorPrefs.GetString(LIBRARY_CLASS_PREFIX_KEY, "UI_Library_");

        // Load UI Handler generation settings from EditorPrefs
        handlerOutputPath = EditorPrefs.GetString(HANDLER_OUTPUT_PATH_KEY, "Assets/Scripts/UI/Handlers/");
        handlerNamespace = EditorPrefs.GetString(HANDLER_NAMESPACE_KEY, "CodeSculptLabs.UIFramework.Handlers");
        handlerClassPrefix = EditorPrefs.GetString(HANDLER_CLASS_PREFIX_KEY, "");

        // Initialize panel handler settings
        InitializePanelHandlerSettings();

        // Initialize and load naming standardization settings
        InitializeNamingSettings();

        // Build the cache of added UI elements
        BuildAddedUIElementsCache();

        // Automatically check for missing references on enable
        CheckMissingReferences();

        // Load icons
        LoadIcons();
    }

    // Now update DrawNamingStandardizationSettings to save settings when they change
    private void DrawNamingStandardizationSettings()
    {
        EditorGUILayout.Space(10);

        // Naming Standardization section
        DrawSectionHeader("Naming Standardization", "Configure how UI element names are standardized for consistency");

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Enable Auto Standardization toggle
        bool previousEnableAutoStandardization = namingSettings.EnableAutoStandardization;
        namingSettings.EnableAutoStandardization = EditorGUILayout.ToggleLeft(
            new GUIContent("Enable Automatic Name Standardization",
                "When enabled, UI element names will be standardized based on their component types when added to UIManager"),
            namingSettings.EnableAutoStandardization);

        if (previousEnableAutoStandardization != namingSettings.EnableAutoStandardization)
        {
            SaveNamingSettings();
        }

        if (namingSettings.EnableAutoStandardization)
        {
            EditorGUILayout.Space(5);

            // Naming convention selector
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Naming Convention:", GUILayout.Width(150));

            NamingConventionType previousConventionType = namingSettings.ConventionType;
            namingSettings.ConventionType = (NamingConventionType)EditorGUILayout.EnumPopup(namingSettings.ConventionType);

            if (previousConventionType != namingSettings.ConventionType)
            {
                SaveNamingSettings();
            }

            EditorGUILayout.EndHorizontal();

            // Example section to show naming preview with improved examples
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUIStyle exampleHeaderStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.8f, 0.9f) }
            };

            EditorGUILayout.LabelField("Naming Preview:", exampleHeaderStyle);
            EditorGUILayout.Space(2);

            // Original vs standardized examples
            DrawPreviewExamples();

            EditorGUILayout.EndVertical();

            // Additional Options
            EditorGUILayout.Space(5);

            bool previousStandardizeChildElements = namingSettings.StandardizeChildElements;
            namingSettings.StandardizeChildElements = EditorGUILayout.ToggleLeft(
                new GUIContent("Standardize Child Elements",
                    "When enabled, child elements will also be standardized (e.g., Text inside Buttons)"),
                namingSettings.StandardizeChildElements);

            if (previousStandardizeChildElements != namingSettings.StandardizeChildElements)
            {
                SaveNamingSettings();
            }

            bool previousRespectExistingConventions = namingSettings.RespectExistingConventions;
            namingSettings.RespectExistingConventions = EditorGUILayout.ToggleLeft(
                new GUIContent("Respect Existing Conventions",
                    "When enabled, elements that appear to follow a naming convention will not be renamed"),
                namingSettings.RespectExistingConventions);

            if (previousRespectExistingConventions != namingSettings.RespectExistingConventions)
            {
                SaveNamingSettings();
            }

            bool previousAutoStandardizeOnAdd = namingSettings.AutoStandardizeOnAdd;
            namingSettings.AutoStandardizeOnAdd = EditorGUILayout.ToggleLeft(
                new GUIContent("Auto-Standardize When Adding",
                    "When enabled, elements will be automatically standardized when added to the UI Manager"),
                namingSettings.AutoStandardizeOnAdd);

            if (previousAutoStandardizeOnAdd != namingSettings.AutoStandardizeOnAdd)
            {
                SaveNamingSettings();
            }

            EditorGUILayout.Space(5);

            // Action buttons
            EditorGUILayout.BeginHorizontal();

            // Button to standardize all UI elements in the scene
            if (GUILayout.Button("Standardize All UI Elements", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Standardize All UI Elements",
                    "This will rename all UI elements in the scene according to the standardization rules. Continue?",
                    "Standardize All", "Cancel"))
                {
                    StandardizeAllUIElements(true);
                }
            }

            // Button to restore original names
            if (originalNames.Count > 0)
            {
                if (GUILayout.Button("Restore Original Names", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Restore Original Names",
                        "This will restore the original names of all UI elements that were standardized. Continue?",
                        "Restore Names", "Cancel"))
                    {
                        RestoreAllOriginalNames();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draw naming preview examples with before/after format
    /// </summary>
    private void DrawPreviewExamples()
    {
        GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 10,
            fixedWidth = 60,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        GUIStyle exampleHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 10,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };

        // Headers
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Original", exampleHeaderStyle, GUILayout.Width(100));
        EditorGUILayout.LabelField("→", GUILayout.Width(15));
        EditorGUILayout.LabelField("Standardized", exampleHeaderStyle);
        EditorGUILayout.EndHorizontal();

        // Basic Button Example
        string buttonName = "Play";
        GameObject buttonObj = CreatePreviewObject(buttonName, typeof(Button));
        string standardizedButtonName = GetStandardizedName(buttonObj);
        DestroyImmediate(buttonObj);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(buttonName, GUILayout.Width(100));
        EditorGUILayout.LabelField("→", GUILayout.Width(15));
        EditorGUILayout.LabelField(standardizedButtonName);
        EditorGUILayout.LabelField("(Button)", labelStyle);
        EditorGUILayout.EndHorizontal();

        // Already suffixed panel example
        string panelName = "MainMenuPanel";
        GameObject panelObj = CreatePreviewObject(panelName, typeof(Image));
        string standardizedPanelName = GetStandardizedName(panelObj);
        DestroyImmediate(panelObj);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(panelName, GUILayout.Width(100));
        EditorGUILayout.LabelField("→", GUILayout.Width(15));
        EditorGUILayout.LabelField(standardizedPanelName);
        EditorGUILayout.LabelField("(Panel - already has type)", labelStyle);
        EditorGUILayout.EndHorizontal();

        // Child element example
        string childText = "ButtonText";
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(childText, GUILayout.Width(100));
        EditorGUILayout.LabelField("→", GUILayout.Width(15));
        EditorGUILayout.LabelField($"Play_Button_Text");
        EditorGUILayout.LabelField("(Text inside Button)", labelStyle);
        EditorGUILayout.EndHorizontal();
    }

    // Add this static property to hold settings across selection changes
    private static NamingConventionSettings s_cachedNamingSettings;

    /// <summary>
    /// Initializes naming settings, ensuring they persist when switching selection
    /// </summary>
    private void InitializeNamingSettings()
    {
        // First time initialization of the static cached settings
        if (s_cachedNamingSettings == null)
        {
            s_cachedNamingSettings = new NamingConventionSettings();
            LoadNamingSettings();
        }

        // Point the instance settings to the static cached settings
        namingSettings = s_cachedNamingSettings;
    }

    /// <summary>
    /// Loads the naming standardization settings from EditorPrefs with scene-specific keys.
    /// </summary>
    private void LoadNamingSettings()
    {
        // If we already have cached settings, use them instead of loading from EditorPrefs
        if (s_cachedNamingSettings != null)
        {
            namingSettings = s_cachedNamingSettings;
            return;
        }

        // Load settings from EditorPrefs with defaults if not found, using scene-specific keys
        namingSettings.EnableAutoStandardization =
            EditorPrefs.GetBool(GetSceneSpecificPrefsKey(NAMING_ENABLE_AUTO_STANDARDIZATION_KEY), true);

        namingSettings.ConventionType = (NamingConventionType)
            EditorPrefs.GetInt(GetSceneSpecificPrefsKey(NAMING_CONVENTION_TYPE_KEY), (int)NamingConventionType.Snake_Case_Suffix);

        namingSettings.StandardizeChildElements =
            EditorPrefs.GetBool(GetSceneSpecificPrefsKey(NAMING_STANDARDIZE_CHILD_ELEMENTS_KEY), true);

        namingSettings.RespectExistingConventions =
            EditorPrefs.GetBool(GetSceneSpecificPrefsKey(NAMING_RESPECT_EXISTING_CONVENTIONS_KEY), true);

        namingSettings.AutoStandardizeOnAdd =
            EditorPrefs.GetBool(GetSceneSpecificPrefsKey(NAMING_AUTO_STANDARDIZE_ON_ADD_KEY), true);

        // Update the static cached settings
        s_cachedNamingSettings = namingSettings;
    }

    /// <summary>
    /// Saves the naming standardization settings to EditorPrefs with scene-specific keys.
    /// </summary>
    private void SaveNamingSettings()
    {
        // Update the static cached settings
        s_cachedNamingSettings = namingSettings;

        // Also save to EditorPrefs for persistence between sessions, using scene-specific keys
        EditorPrefs.SetBool(GetSceneSpecificPrefsKey(NAMING_ENABLE_AUTO_STANDARDIZATION_KEY), namingSettings.EnableAutoStandardization);
        EditorPrefs.SetInt(GetSceneSpecificPrefsKey(NAMING_CONVENTION_TYPE_KEY), (int)namingSettings.ConventionType);
        EditorPrefs.SetBool(GetSceneSpecificPrefsKey(NAMING_STANDARDIZE_CHILD_ELEMENTS_KEY), namingSettings.StandardizeChildElements);
        EditorPrefs.SetBool(GetSceneSpecificPrefsKey(NAMING_RESPECT_EXISTING_CONVENTIONS_KEY), namingSettings.RespectExistingConventions);
        EditorPrefs.SetBool(GetSceneSpecificPrefsKey(NAMING_AUTO_STANDARDIZE_ON_ADD_KEY), namingSettings.AutoStandardizeOnAdd);
    }
    #endregion
}
#endif