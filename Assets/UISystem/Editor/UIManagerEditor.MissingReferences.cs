#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public partial class UIManagerEditor : Editor
{
    #region Missing References

    private void DrawMissingReferencesSection()
    {
        DrawSectionHeader("Missing References",
            "Identify and fix UI references that are broken or missing. \nFix or remove these references before generating your UI Library.");

        EditorGUILayout.Space(10);

        // Action buttons with enhanced styling
        EditorGUILayout.BeginHorizontal();

        float buttonHeight = 30;
        float scanButtonWidth = 180;

        // Scan button
        Rect scanButtonRect = GUILayoutUtility.GetRect(scanButtonWidth, buttonHeight);
        DrawActionButton(
            scanButtonRect,
            "Scan for Missing References",
            "Check for any missing UI references",
            () => CheckMissingReferences()
        );

        // Only show additional buttons if there are missing references
        if (missingReferences.Any())
        {
            GUILayout.Space(10);

            float fixButtonWidth = 150;
            Rect fixButtonRect = GUILayoutUtility.GetRect(fixButtonWidth, buttonHeight);

            // Fix All button
            DrawActionButton(
                fixButtonRect,
                "Fix All References",
                "Attempt to automatically fix all missing references",
                () => {
                    if (EditorUtility.DisplayDialog("Confirm Fix All",
                        $"Attempt to fix all {missingReferences.Count} missing references?",
                        "Yes, Fix All", "Cancel"))
                    {
                        FixAllMissingReferences();
                        CheckMissingReferences(); // Refresh the list
                    }
                },
                true // primary action
            );

            GUILayout.Space(10);

            // Remove All button
            float removeButtonWidth = 160;
            Rect removeButtonRect = GUILayoutUtility.GetRect(removeButtonWidth, buttonHeight);

            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.7f, 0.3f, 0.3f); // Red for delete action

            DrawActionButton(
                removeButtonRect,
                "Remove All Missing",
                "Remove all missing references from the UI Manager",
                () => {
                    if (EditorUtility.DisplayDialog("Confirm Remove All",
                        $"Are you sure you want to remove all {missingReferences.Count} missing references? This cannot be undone.",
                        "Yes, Remove All", "Cancel"))
                    {
                        RemoveAllMissingReferences();
                    }
                }
            );

            GUI.backgroundColor = defaultColor;
        }

        EditorGUILayout.EndHorizontal();

        // Status display
        EditorGUILayout.Space(15);

        if (missingReferences.Any())
        {
            // Status panel with icon and count
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Rect statusRect = EditorGUILayout.GetControlRect(false, 36);

            if (Event.current.type == EventType.Repaint)
            {
                Color warningColor = new Color(0.8f, 0.6f, 0.2f, 0.1f);
                EditorGUI.DrawRect(statusRect, warningColor);
            }

            // Warning icon
            Texture2D warningIcon = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
            if (warningIcon != null)
            {
                GUI.DrawTexture(
                    new Rect(statusRect.x + 12, statusRect.y + 9, 18, 18),
                    warningIcon
                );
            }

            // Status text
            GUIStyle warningStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.8f, 0.6f, 0.2f) }
            };

            EditorGUI.LabelField(
                new Rect(statusRect.x + 40, statusRect.y + 9, statusRect.width - 50, 18),
                $"Found {missingReferences.Count} missing UI references",
                warningStyle
            );

            EditorGUILayout.EndVertical();

            // Missing references list
            EditorGUILayout.Space(10);
            DrawMissingReferencesList();
        }
        else
        {
            // Success status with icon
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Rect statusRect = EditorGUILayout.GetControlRect(false, 36);

            if (Event.current.type == EventType.Repaint)
            {
                Color successColor = new Color(0.2f, 0.5f, 0.2f, 0.1f);
                EditorGUI.DrawRect(statusRect, successColor);
            }

            // Success icon
            Texture2D successIcon = EditorGUIUtility.IconContent("d_FilterSelectedOnly").image as Texture2D;
            if (successIcon != null)
            {
                GUI.DrawTexture(
                    new Rect(statusRect.x + 12, statusRect.y + 9, 18, 18),
                    successIcon
                );
            }

            // Status text
            GUIStyle successStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.3f, 0.7f, 0.3f) }
            };

            EditorGUI.LabelField(
                new Rect(statusRect.x + 40, statusRect.y + 9, statusRect.width - 50, 18),
                "No missing UI references found. All references are valid.",
                successStyle
            );

            EditorGUILayout.EndVertical();
        }
    }


    /// <summary>
    /// Draws the list of missing references.
    /// </summary>
    private void DrawMissingReferencesList()
    {
        DrawSectionHeader("Missing Elements", null);
        EditorGUILayout.Space(5);

        // Improved list height calculation - more space for better UX
        float listHeight = Mathf.Max(300, Mathf.Min(missingReferences.Count * 100, 500));

        missingRefsScrollPos = EditorGUILayout.BeginScrollView(missingRefsScrollPos, GUILayout.Height(listHeight));

        for (int i = 0; i < missingReferences.Count; i++)
        {
            DrawMissingReferenceCard(missingReferences[i], i);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawMissingReferenceCard(UIReference missingRef, int index)
    {
        bool isSelected = missingRef == selectedMissingReference;

        // Reference card with styling
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Header with enhanced styling
        Rect headerRect = EditorGUILayout.GetControlRect(false, 32);

        if (Event.current.type == EventType.Repaint)
        {
            // Background color based on selection state
            Color bgColor = isSelected ?
                new Color(0.3f, 0.4f, 0.6f, 0.3f) : // Blue for selected
                new Color(0.6f, 0.4f, 0.2f, 0.15f); // Amber tint for warning

            EditorGUI.DrawRect(headerRect, bgColor);
        }

        // Element type pill with corresponding color
        Color typeColor = GetColorForMissingType(missingRef.elementType.ToString());
        Rect typePillRect = new Rect(headerRect.x + 8, headerRect.y + 8, 70, 16);

        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(typePillRect, typeColor);
        }

        // Type text
        GUIStyle typeStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        EditorGUI.LabelField(typePillRect, missingRef.elementType.ToString(), typeStyle);

        // Element name
        GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = isSelected ? new Color(0.7f, 0.85f, 1f) : new Color(0.9f, 0.9f, 0.95f) }
        };

        EditorGUI.LabelField(
            new Rect(headerRect.x + 88, headerRect.y + 8, headerRect.width - 280, 20),
            missingRef.name,
            nameStyle
        );

        // Action buttons with GuiContent
        float buttonX = headerRect.x + headerRect.width - 190;
        Rect fixButtonRect = new Rect(buttonX, headerRect.y + 6, 60, 20);
        Rect removeButtonRect = new Rect(buttonX + 65, headerRect.y + 6, 60, 20);

        // Fix button - use GUI.Button with GUIContent
        if (GUI.Button(fixButtonRect, new GUIContent("Fix")))
        {
            FixMissingReference(missingRef);
            CheckMissingReferences(); // Refresh the list
            GUIUtility.ExitGUI(); // Prevent layout errors
        }

        // Remove button - use GUI.Button with GUIContent
        if (GUI.Button(removeButtonRect, new GUIContent("Remove")))
        {
            if (EditorUtility.DisplayDialog("Confirm Remove",
                $"Are you sure you want to remove '{missingRef.name}'?",
                "Remove", "Cancel"))
            {
                RemoveReference(missingRef);
                GUIUtility.ExitGUI(); // Prevent layout errors
            }
        }

        // Make header clickable to toggle details (EXCLUDING button areas)
        if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
        {
            // Check if click is NOT on the buttons
            if (!fixButtonRect.Contains(Event.current.mousePosition) &&
                !removeButtonRect.Contains(Event.current.mousePosition))
            {
                selectedMissingReference = isSelected ? null : missingRef;
                Event.current.Use();
                Repaint();
            }
        }

        // Arrow indicator for expandable content
        if (Event.current.type == EventType.Repaint)
        {
            Color arrowColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            Vector3[] trianglePoints;

            if (isSelected)
            {
                // Down-pointing triangle
                trianglePoints = new Vector3[]
                {
                new Vector3(headerRect.x + headerRect.width - 20, headerRect.y + 10),
                new Vector3(headerRect.x + headerRect.width - 12, headerRect.y + 10),
                new Vector3(headerRect.x + headerRect.width - 16, headerRect.y + 16)
                };
            }
            else
            {
                // Right-pointing triangle
                trianglePoints = new Vector3[]
                {
                new Vector3(headerRect.x + headerRect.width - 20, headerRect.y + 10),
                new Vector3(headerRect.x + headerRect.width - 20, headerRect.y + 16),
                new Vector3(headerRect.x + headerRect.width - 14, headerRect.y + 13)
                };
            }

            Handles.color = arrowColor;
            Handles.DrawAAConvexPolygon(trianglePoints);
        }

        // Details section if selected
        if (isSelected)
        {
            EditorGUILayout.Space(36); // Space for the header

            // Path and ID info with enhanced styling
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Path section
            EditorGUILayout.Space(8);

            GUIStyle headerTextStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.7f, 0.9f) }
            };

            EditorGUILayout.LabelField("Path:", headerTextStyle);

            // Path field with selectable text
            GUIStyle pathStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 10,
                wordWrap = true,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };

            EditorGUILayout.SelectableLabel(missingRef.fullPath, pathStyle, GUILayout.Height(20));

            EditorGUILayout.Space(4);

            // ID section
            EditorGUILayout.LabelField("Instance ID:", headerTextStyle);
            EditorGUILayout.SelectableLabel(missingRef.instanceID, pathStyle, GUILayout.Height(20));

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();

            // Possible fixes with enhanced styling
            EditorGUILayout.Space(8);

            // Try to find a possible match
            Transform possibleMatch = FindPossibleMatch(missingRef);
            if (possibleMatch != null)
            {
                // Suggestion box with highlighted background
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                Rect suggestionRect = EditorGUILayout.GetControlRect(false, 40);

                if (Event.current.type == EventType.Repaint)
                {
                    // Success background
                    Color successBg = new Color(0.2f, 0.5f, 0.2f, 0.1f);
                    EditorGUI.DrawRect(suggestionRect, successBg);
                }

                // Success icon
                Texture2D successIcon = EditorGUIUtility.IconContent("d_FilterSelectedOnly").image as Texture2D;
                if (successIcon != null)
                {
                    GUI.DrawTexture(
                        new Rect(suggestionRect.x + 10, suggestionRect.y + 12, 16, 16),
                        successIcon
                    );
                }

                // Suggestion text
                GUIStyle suggestionTextStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    wordWrap = true,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.3f, 0.7f, 0.3f) }
                };

                EditorGUI.LabelField(
                    new Rect(suggestionRect.x + 32, suggestionRect.y + 4, suggestionRect.width - 120, suggestionRect.height - 8),
                    $"Possible match found:\n{possibleMatch.name}",
                    suggestionTextStyle
                );

                // Apply fix button
                if (GUI.Button(
                    new Rect(suggestionRect.x + suggestionRect.width - 90, suggestionRect.y + 10, 80, 20),
                    "Apply Fix"
                ))
                {
                    ApplyFixWithGameObject(missingRef, possibleMatch.gameObject);
                    CheckMissingReferences(); // Refresh the list
                    GUIUtility.ExitGUI(); // Prevent layout errors
                }

                EditorGUILayout.Space(40); // Space for the suggestion content
                EditorGUILayout.EndVertical();
            }
            else
            {
                // No matches found - show error box
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                Rect errorRect = EditorGUILayout.GetControlRect(false, 36);

                if (Event.current.type == EventType.Repaint)
                {
                    // Error background
                    Color errorBg = new Color(0.6f, 0.2f, 0.2f, 0.1f);
                    EditorGUI.DrawRect(errorRect, errorBg);
                }

                // Error icon
                Texture2D errorIcon = EditorGUIUtility.IconContent("console.erroricon.sml").image as Texture2D;
                if (errorIcon != null)
                {
                    GUI.DrawTexture(
                        new Rect(errorRect.x + 10, errorRect.y + 10, 16, 16),
                        errorIcon
                    );
                }

                // Error text
                GUIStyle errorTextStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    wordWrap = true,
                    normal = { textColor = new Color(0.8f, 0.4f, 0.4f) }
                };

                EditorGUI.LabelField(
                    new Rect(errorRect.x + 32, errorRect.y, errorRect.width - 40, errorRect.height),
                    "No automatic fix available. You may need to manually reassign this reference.",
                    errorTextStyle
                );

                EditorGUILayout.Space(36); // Space for the error content
                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.EndVertical();

        // Add spacing between cards
        EditorGUILayout.Space(4);
    }

    private Color GetColorForMissingType(string typeName)
    {
        return typeName.ToLowerInvariant() switch
        {
            "button" => new Color(0.3f, 0.5f, 0.9f),
            "toggle" => new Color(0.7f, 0.5f, 0.9f),
            "slider" => new Color(0.9f, 0.5f, 0.3f),
            "inputfield" => new Color(0.4f, 0.7f, 0.4f),
            "dropdown" => new Color(0.9f, 0.7f, 0.3f),
            "panel" => new Color(0.7f, 0.3f, 0.3f),
            "text" => new Color(0.5f, 0.8f, 0.5f),
            "tmp_text" => new Color(0.5f, 0.8f, 0.5f),
            "image" => new Color(0.8f, 0.5f, 0.5f),
            "rawimage" => new Color(0.7f, 0.5f, 0.5f),
            _ => new Color(0.6f, 0.6f, 0.6f)
        };
    }



    /// <summary>
    /// Finds a possible match for a missing reference.
    /// </summary>
    private Transform FindPossibleMatch(UIReference reference)
    {
        if (string.IsNullOrEmpty(reference.fullPath)) return null;

        // Try to find by name
        var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.InstanceID)
            .Where(go => go.scene == uiManager.gameObject.scene);

        // First try exact name match
        foreach (var go in allGameObjects)
        {
            if (go.name == reference.name)
            {
                // Check if it has the right component type
                if (HasMatchingComponentType(go, reference.elementType))
                {
                    return go.transform;
                }
            }
        }

        // Then try similar path
        string[] pathParts = reference.fullPath.Split('/');
        string targetName = pathParts[pathParts.Length - 1];

        foreach (var go in allGameObjects)
        {
            if (go.name == targetName)
            {
                // Check if it has the right component type
                if (HasMatchingComponentType(go, reference.elementType))
                {
                    return go.transform;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a GameObject has a component matching the UI element type.
    /// </summary>
    private bool HasMatchingComponentType(GameObject go, UIElementType elementType)
    {
        switch (elementType)
        {
            case UIElementType.Button: return go.GetComponent<Button>() != null;
            case UIElementType.Text: return go.GetComponent<Text>() != null;
            case UIElementType.TMP_Text: return go.GetComponent<TMPro.TMP_Text>() != null;
            case UIElementType.Toggle: return go.GetComponent<Toggle>() != null;
            case UIElementType.InputField: return go.GetComponent<InputField>() != null;
            case UIElementType.TMP_InputField: return go.GetComponent<TMPro.TMP_InputField>() != null;
            case UIElementType.Slider: return go.GetComponent<Slider>() != null;
            case UIElementType.Dropdown: return go.GetComponent<Dropdown>() != null || go.GetComponent<TMPro.TMP_Dropdown>() != null;
            case UIElementType.Panel: return go.GetComponent<Image>() != null && go.name.EndsWith("_Panel");
            case UIElementType.Image: return go.GetComponent<Image>() != null;
            case UIElementType.RawImage: return go.GetComponent<RawImage>() != null;
            case UIElementType.ScrollView: return go.GetComponent<ScrollRect>() != null;
            default: return false;
        }
    }

    private void CheckMissingReferences()
    {
        missingReferences.Clear();

        foreach (var category in uiManager.GetAllUICategories())
        {
            foreach (var reference in category.references)
            {
                if (reference.uiElement == null)
                {
                    missingReferences.Add(reference);
                }
            }
        }

        if (missingReferences.Any())
        {
            Debug.LogWarning($"Found {missingReferences.Count} missing UI references.");
        }
        else
        {
            Debug.Log("No missing UI references found.");
        }
    }

    private void FixMissingReference(UIReference reference)
    {
        Transform foundTransform = FindTransformByPath(reference.fullPath);
        if (foundTransform != null)
        {
            ApplyFixWithGameObject(reference, foundTransform.gameObject);
        }
        else
        {
            // Try to find a possible match
            Transform possibleMatch = FindPossibleMatch(reference);
            if (possibleMatch != null)
            {
                if (EditorUtility.DisplayDialog("Possible Match Found",
                    $"Found a possible match for '{reference.name}': {possibleMatch.name}\n\nDo you want to use this as a replacement?",
                    "Use This Match", "Cancel"))
                {
                    ApplyFixWithGameObject(reference, possibleMatch.gameObject);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Fix Failed",
                    $"Could not find a replacement for '{reference.name}'.\n\nYou may need to manually reassign this reference or remove it.",
                    "OK");
            }
        }
    }

    /// <summary>
    /// Applies a fix using the provided GameObject.
    /// </summary>
    private void ApplyFixWithGameObject(UIReference reference, GameObject replacement)
    {
        if (replacement == null) return;

        reference.uiElement = replacement;
        reference.fullPath = GetFullPath(replacement.transform);
        reference.instanceID = replacement.GetInstanceID().ToString();

        uiManager.InitializeDictionaries();
        EditorUtility.SetDirty(uiManager);

        Debug.Log($"Fixed missing reference: {reference.name}");
        ShowQuickTip("Reference Fixed", $"Successfully fixed the reference to '{reference.name}'.");
    }

    private void FixAllMissingReferences()
    {
        int fixedCount = 0;
        foreach (var missingRef in missingReferences.ToList())
        {
            Transform foundTransform = FindTransformByPath(missingRef.fullPath);
            if (foundTransform != null)
            {
                ApplyFixWithGameObject(missingRef, foundTransform.gameObject);
                fixedCount++;
            }
            else
            {
                // Try to find by name if path doesn't work
                Transform possibleMatch = FindPossibleMatch(missingRef);
                if (possibleMatch != null)
                {
                    ApplyFixWithGameObject(missingRef, possibleMatch.gameObject);
                    fixedCount++;
                }
            }
        }

        if (fixedCount > 0)
        {
            uiManager.InitializeDictionaries();
            EditorUtility.SetDirty(uiManager);
            ShowQuickTip("References Fixed", $"Successfully fixed {fixedCount} missing references.");
        }
        else
        {
            EditorUtility.DisplayDialog("Fix All Failed",
                "Could not automatically fix any references. You may need to fix them manually.",
                "OK");
        }
    }

    private Transform FindTransformByPath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath)) return null;

        string[] pathParts = fullPath.Split('/');

        var currentScene = uiManager.gameObject.scene;
        var rootObjects = currentScene.GetRootGameObjects();

        foreach (var rootObj in rootObjects)
        {
            if (rootObj.name == pathParts[0])
            {
                Transform currentTransform = rootObj.transform;
                bool pathExists = true;

                for (int i = 1; i < pathParts.Length; i++)
                {
                    currentTransform = currentTransform.Find(pathParts[i]);
                    if (currentTransform == null)
                    {
                        pathExists = false;
                        break;
                    }
                }

                if (pathExists)
                {
                    return currentTransform;
                }
            }
        }

        return null;
    }

    // private void RemoveReference(UIReference reference)
    // {
    //     Undo.RecordObject(uiManager, "Remove UI Reference");

    //     foreach (var category in uiManager.GetAllUICategories())
    //     {
    //         if (category.references.Remove(reference))
    //         {
    //             break;
    //         }
    //     }

    //     missingReferences.Remove(reference);
    //     if (selectedMissingReference == reference)
    //     {
    //         selectedMissingReference = null;
    //     }

    //     EditorUtility.SetDirty(uiManager);
    //     Debug.Log($"Removed missing reference: {reference.name}");
    // }

    private void RemoveAllMissingReferences()
    {
        Undo.RecordObject(uiManager, "Remove All Missing UI References");
        int count = missingReferences.Count;

        foreach (var reference in missingReferences.ToList())
        {
            RemoveReference(reference);
        }

        missingReferences.Clear();
        selectedMissingReference = null;

        ShowQuickTip("References Removed", $"Removed {count} missing references from the UI Manager.");
    }

    #endregion
}
#endif