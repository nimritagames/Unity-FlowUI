using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Class representing a category of UI elements.
/// </summary>
[System.Serializable]
public class UICategory
{
    public string name;
    public List<UIReference> references = new List<UIReference>();
}

/// <summary>
/// Manages UI elements, providing methods to add, remove, and access them.
/// </summary>
public class UIManager : MonoBehaviour
{
    #region Serialized Fields

    [SerializeField]
    private List<UICategory> uiCategories = new List<UICategory>();

    #endregion

    #region UI Reference Management

    // Single primary storage for UI references
    private Dictionary<string, UIReference> uiReferenceByPath = new Dictionary<string, UIReference>();

    // Lookup map for instance IDs to paths (faster than storing full references twice)
    private Dictionary<int, string> instanceIDToPathMap = new Dictionary<int, string>();

    private void Awake()
    {
        InitializeDictionaries();
    }

    /// <summary>
    /// Initializes the dictionaries from the serialized uiCategories.
    /// </summary>
    public void InitializeDictionaries()
    {
        uiReferenceByPath.Clear();
        instanceIDToPathMap.Clear();

        foreach (var category in uiCategories)
        {
            foreach (var reference in category.references)
            {
                // Ensure the GameObject reference is still valid
                if (reference.uiElement != null)
                {
                    uiReferenceByPath[reference.fullPath] = reference;
                    instanceIDToPathMap[reference.uiElement.GetInstanceID()] = reference.fullPath;
                }
                else
                {
                    LogWarning($"UIManager: UI element '{reference.name}' is missing or has been destroyed.");
                }
            }
        }
    }

    /// <summary>
    /// Adds a UI reference to the manager.
    /// </summary>
    /// <param name="uiElement">The UI element to add.</param>
    public void AddUIReference(GameObject uiElement)
    {
        if (uiElement == null)
        {
            LogError("UIManager: Attempted to add a null UI element.");
            return;
        }

        UIElementType type = DetermineUIElementType(uiElement);
        string categoryName = type.ToString();
        string fullPath = GetFullPath(uiElement.transform);
        int instanceID = uiElement.GetInstanceID();

        if (IsReferenceAlreadyAdded(fullPath, instanceID))
        {
            LogWarning($"UIManager: Duplicate UI element '{uiElement.name}' attempted to be added. Skipping.");
            return;
        }

        UICategory category = uiCategories.Find(cat => cat.name == categoryName);
        if (category == null)
        {
            category = new UICategory { name = categoryName };
            uiCategories.Add(category);
        }

        UIReference reference = new UIReference
        {
            name = uiElement.name,
            uiElement = uiElement,
            elementType = type,
            fullPath = fullPath,
            instanceID = instanceID.ToString()
        };

        category.references.Add(reference);
        uiReferenceByPath[fullPath] = reference;
        instanceIDToPathMap[instanceID] = fullPath;
    }

    private bool IsReferenceAlreadyAdded(string fullPath, int instanceID)
    {
        return uiReferenceByPath.ContainsKey(fullPath) || instanceIDToPathMap.ContainsKey(instanceID);
    }

    private Dictionary<Transform, string> pathCache = new Dictionary<Transform, string>();

    private string GetFullPath(Transform transform)
    {
        if (transform == null)
        {
            LogError("UIManager: Null transform provided for GetFullPath.");
            return string.Empty;
        }

        if (pathCache.TryGetValue(transform, out string cachedPath))
        {
            return cachedPath;
        }

        // Use StringBuilder for more efficient string concatenation
        StringBuilder pathBuilder = new StringBuilder(64);
        GetPathRecursive(transform, pathBuilder);

        string path = pathBuilder.ToString();
        pathCache[transform] = path;
        return path;
    }

    private void GetPathRecursive(Transform current, StringBuilder pathBuilder)
    {
        if (current.parent != null)
        {
            GetPathRecursive(current.parent, pathBuilder);
            pathBuilder.Append('/');
        }
        pathBuilder.Append(current.name);
    }

    /// <summary>
    /// Clears the path cache to free memory.
    /// </summary>
    public void ClearPathCache()
    {
        pathCache.Clear();
    }

    /// <summary>
    /// Removes a UI reference by full hierarchical path.
    /// </summary>
    /// <param name="fullPath">The full path of the UI element to remove.</param>
    public void RemoveUIReference(string fullPath)
    {
        if (uiReferenceByPath.TryGetValue(fullPath, out UIReference reference))
        {
            // Find and remove the instanceID mapping
            int instanceID;
            if (reference.uiElement != null)
            {
                instanceID = reference.uiElement.GetInstanceID();
                instanceIDToPathMap.Remove(instanceID);
            }
            else if (int.TryParse(reference.instanceID, out instanceID))
            {
                instanceIDToPathMap.Remove(instanceID);
            }

            uiReferenceByPath.Remove(fullPath);

            UICategory category = uiCategories.Find(cat => cat.name == reference.elementType.ToString());
            if (category != null)
            {
                category.references.Remove(reference);
            }
        }
        else
        {
            LogWarning($"UIManager: No reference found with path '{fullPath}'.");
        }
    }

    /// <summary>
    /// Removes a UI reference by instance ID.
    /// </summary>
    /// <param name="instanceID">The instance ID of the UI element to remove.</param>
    public void RemoveUIReferenceByInstanceID(int instanceID)
    {
        if (instanceIDToPathMap.TryGetValue(instanceID, out string fullPath))
        {
            if (uiReferenceByPath.TryGetValue(fullPath, out UIReference reference))
            {
                uiReferenceByPath.Remove(fullPath);
                instanceIDToPathMap.Remove(instanceID);

                UICategory category = uiCategories.Find(cat => cat.name == reference.elementType.ToString());
                if (category != null)
                {
                    category.references.Remove(reference);
                }
            }
        }
        else
        {
            LogWarning($"UIManager: No reference found with instance ID '{instanceID}'.");
        }
    }

    // For backwards compatibility
    public void RemoveUIReferenceByInstanceID(string instanceIDString)
    {
        if (int.TryParse(instanceIDString, out int instanceID))
        {
            RemoveUIReferenceByInstanceID(instanceID);
        }
        else
        {
            LogWarning($"UIManager: Invalid instance ID format '{instanceIDString}'.");
        }
    }

    /// <summary>
    /// Gets a UI reference by full hierarchical path.
    /// </summary>
    private GameObject GetUIReference(UIElementType elementType, string key)
    {
        if (uiReferenceByPath.TryGetValue(key, out UIReference reference))
        {
            return reference.uiElement;
        }

        LogWarning($"UIManager: No reference found with path '{key}'.");
        return null;
    }

    /// <summary>
    /// Gets a UI reference by instance ID.
    /// </summary>
    private GameObject GetUIReferenceByInstanceID(string instanceIDString)
    {
        if (int.TryParse(instanceIDString, out int instanceID))
        {
            return GetUIReferenceByInstanceID(instanceID);
        }

        LogWarning($"UIManager: Invalid instance ID format '{instanceIDString}'.");
        return null;
    }

    private GameObject GetUIReferenceByInstanceID(int instanceID)
    {
        if (instanceIDToPathMap.TryGetValue(instanceID, out string path))
        {
            if (uiReferenceByPath.TryGetValue(path, out UIReference reference))
            {
                return reference.uiElement;
            }
        }

        LogWarning($"UIManager: No reference found with instance ID '{instanceID}'.");
        return null;
    }

    // Efficient type checking using a dictionary
    private static readonly Dictionary<Type, UIElementType> componentTypeMap = new Dictionary<Type, UIElementType>
    {
        { typeof(Button), UIElementType.Button },
        { typeof(Text), UIElementType.Text },
        { typeof(TMP_Text), UIElementType.TMP_Text },
        { typeof(Toggle), UIElementType.Toggle },
        { typeof(InputField), UIElementType.InputField },
        { typeof(TMP_InputField), UIElementType.TMP_InputField },
        { typeof(Slider), UIElementType.Slider },
        { typeof(Dropdown), UIElementType.Dropdown },
        { typeof(TMP_Dropdown), UIElementType.Dropdown },
        { typeof(ScrollRect), UIElementType.ScrollView },
        { typeof(Scrollbar), UIElementType.ScrollView },
        { typeof(Image), UIElementType.Image },
        { typeof(RawImage), UIElementType.RawImage },
        { typeof(Mask), UIElementType.Mask },
        { typeof(Canvas), UIElementType.Canvas },
        { typeof(CanvasGroup), UIElementType.CanvasGroup }
    };

    private UIElementType DetermineUIElementType(GameObject uiElement)
    {
        foreach (var pair in componentTypeMap)
        {
            if (uiElement.GetComponent(pair.Key))
            {
                // Special case for Image component to check if it's a panel
                if (pair.Key == typeof(Image) &&
                    uiElement.name.EndsWith("_Panel", StringComparison.OrdinalIgnoreCase))
                {
                    return UIElementType.Panel;
                }

                return pair.Value;
            }
        }

        return UIElementType.Unknown;
    }

    /// <summary>
    /// Gets all UI categories.
    /// </summary>
    /// <returns>List of all UI categories.</returns>
    public List<UICategory> GetAllUICategories()
    {
        return uiCategories;
    }

    #endregion

    #region Generic UI Component Management

    private UIElementType GetElementTypeFromComponent<T>() where T : Component
    {
        if (componentTypeMap.TryGetValue(typeof(T), out UIElementType type))
        {
            return type;
        }

        LogWarning($"UIManager: Unknown component type '{typeof(T).Name}'.");
        return UIElementType.Unknown;
    }

    /// <summary>
    /// Gets a UI component of type T.
    /// </summary>
    public T GetUIComponent<T>(string key, bool isInstanceID = false) where T : Component
    {
        UIElementType elementType = GetElementTypeFromComponent<T>();
        if (elementType == UIElementType.Unknown)
        {
            LogWarning($"UIManager: Unknown component type '{typeof(T).Name}'.");
            return null;
        }

        GameObject uiElement = isInstanceID ? GetUIReferenceByInstanceID(key) : GetUIReference(elementType, key);

        if (uiElement == null)
        {
            LogWarning($"UIManager: UI element not found for key '{key}'.");
            return null;
        }

        T component = uiElement.GetComponent<T>();
        if (component == null)
        {
            LogWarning($"UIManager: Component '{typeof(T).Name}' not found on UI element for key '{key}'.");
        }
        return component;
    }

    #endregion

    #region Panel Activation and Deactivation

    private GameObject lastActivePanel = null;
    // Using HashSet for more efficient contains/lookup operations
    private readonly HashSet<GameObject> activePanels = new HashSet<GameObject>();

    /// <summary>
    /// Gets a panel GameObject by key.
    /// </summary>
    public GameObject GetPanel(string key, bool isInstanceID = false)
    {
        GameObject uiElement = isInstanceID ? GetUIReferenceByInstanceID(key) : GetUIReference(UIElementType.Panel, key);

        if (uiElement == null)
        {
            LogWarning($"UIManager: Panel not found for key '{key}'.");
            return null;
        }

        if (uiElement.GetComponent<Image>() == null)
        {
            LogWarning($"UIManager: UI element '{key}' is not a Panel (missing Image component).");
            return null;
        }

        return uiElement;
    }

    /// <summary>
    /// Sets the active state of a panel.
    /// </summary>
    public void SetPanelActive(string key, bool isActive, bool isInstanceID = false, bool deactivateOthers = true, bool keepLastPanel = false)
    {
        GameObject panel = GetPanel(key, isInstanceID);
        if (panel == null) return;

        if (isActive)
        {
            HandlePanelActivation(panel, deactivateOthers, keepLastPanel);
        }
        else
        {
            panel.SetActive(false);
            activePanels.Remove(panel);
            if (lastActivePanel == panel)
            {
                lastActivePanel = null;
            }
        }
    }

    private void HandlePanelActivation(GameObject panel, bool deactivateOthers, bool keepLastPanel)
    {
        if (deactivateOthers)
        {
            // Create a temp list to avoid collection modification during iteration
            List<GameObject> panelsToDeactivate = new List<GameObject>(activePanels.Count);
            foreach (var activePanel in activePanels)
            {
                if (activePanel != panel && (!keepLastPanel || activePanel != lastActivePanel))
                {
                    panelsToDeactivate.Add(activePanel);
                }
            }

            // Now deactivate the panels
            foreach (var panelToDeactivate in panelsToDeactivate)
            {
                panelToDeactivate.SetActive(false);
                activePanels.Remove(panelToDeactivate);
            }
        }

        panel.SetActive(true);
        activePanels.Add(panel);
        lastActivePanel = panel;
    }

    #endregion

    #region Resource Management

    /// <summary>
    /// Disposes resources used by this manager.
    /// </summary>
    public void Dispose()
    {
        uiReferenceByPath.Clear();
        instanceIDToPathMap.Clear();
        pathCache.Clear();
        activePanels.Clear();
    }

    private void OnDestroy()
    {
        Dispose();
    }

    #endregion

    #region Conditional Logging

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogError(string message)
    {
        Debug.LogError(message);
    }

    #endregion
}