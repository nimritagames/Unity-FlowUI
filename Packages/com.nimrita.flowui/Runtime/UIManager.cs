using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
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
    private Dictionary<string, UICategory> categoryByName = new Dictionary<string, UICategory>(StringComparer.Ordinal);

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
        RebuildCategoryIndex();

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
                    LogWarning(
                        $"UIManager initialization warning: reference '{reference.name}' in category '{category.name}' points to a missing/destroyed object. Reassign or remove this entry.");
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
            LogError(
                "UIManager add failed: attempted to register a null UI element. Ensure a valid GameObject is passed to AddUIReference.");
            return;
        }

        UIElementType type = DetermineUIElementType(uiElement);
        string categoryName = type.ToString();
        string fullPath = GetFullPath(uiElement.transform);
        int instanceID = uiElement.GetInstanceID();

        if (IsReferenceAlreadyAdded(fullPath, instanceID))
        {
            LogWarning(
                $"UIManager add skipped: '{uiElement.name}' is already registered (duplicate path or instance ID).");
            return;
        }

        UICategory category = GetOrCreateCategory(categoryName);

        UIReference reference = new UIReference
        {
            name = uiElement.name,
            uiElement = uiElement,
            elementType = type,
            fullPath = fullPath,
            instanceID = instanceID
        };

        category.references.Add(reference);
        uiReferenceByPath[fullPath] = reference;
        instanceIDToPathMap[instanceID] = fullPath;
    }

    private bool IsReferenceAlreadyAdded(string fullPath, int instanceID)
    {
        return uiReferenceByPath.ContainsKey(fullPath) || instanceIDToPathMap.ContainsKey(instanceID);
    }

    private struct PathCacheEntry
    {
        public string Path;
        public int ParentInstanceId;
        public string NameAtCache;
        public int HierarchySignature;
    }

    private Dictionary<Transform, PathCacheEntry> pathCache = new Dictionary<Transform, PathCacheEntry>();
    private readonly List<Transform> ancestorBuffer = new List<Transform>(16);
    private readonly StringBuilder pathBuilderCached = new StringBuilder(128);

    private string GetFullPath(Transform transform)
    {
        if (transform == null)
        {
            LogError(
                "UIManager path resolution failed: received a null transform while computing full path.");
            return string.Empty;
        }

        if (pathCache.TryGetValue(transform, out PathCacheEntry cacheEntry) &&
            IsPathCacheEntryValid(transform, cacheEntry))
        {
            return cacheEntry.Path;
        }

        // Collect ancestors iteratively to avoid recursion overhead
        ancestorBuffer.Clear();
        Transform current = transform;
        while (current != null)
        {
            ancestorBuffer.Add(current);
            current = current.parent;
        }

        // Build path from root to leaf
        pathBuilderCached.Clear();
        for (int i = ancestorBuffer.Count - 1; i >= 0; i--)
        {
            if (i < ancestorBuffer.Count - 1)
            {
                pathBuilderCached.Append('/');
            }
            pathBuilderCached.Append(ancestorBuffer[i].name);
        }

        string path = pathBuilderCached.ToString();
        pathCache[transform] = new PathCacheEntry
        {
            Path = path,
            ParentInstanceId = transform.parent != null ? transform.parent.GetInstanceID() : 0,
            NameAtCache = transform.name,
            HierarchySignature = ComputeHierarchySignature(transform)
        };
        return path;
    }

    private bool IsPathCacheEntryValid(Transform transform, PathCacheEntry cacheEntry)
    {
        if (transform == null)
        {
            return false;
        }

        if (!string.Equals(transform.name, cacheEntry.NameAtCache, StringComparison.Ordinal))
        {
            return false;
        }

        int currentParentId = transform.parent != null ? transform.parent.GetInstanceID() : 0;
        if (currentParentId != cacheEntry.ParentInstanceId)
        {
            return false;
        }

        return cacheEntry.HierarchySignature == ComputeHierarchySignature(transform);
    }

    // Allocation-free hierarchy fingerprint used to validate cached paths
    // without rebuilding full path strings.
    private int ComputeHierarchySignature(Transform transform)
    {
        unchecked
        {
            int hash = 17;
            Transform current = transform;
            while (current != null)
            {
                hash = (hash * 31) + current.GetInstanceID();
                hash = (hash * 31) + current.name.GetHashCode();
                current = current.parent;
            }

            return hash;
        }
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
            if (reference.uiElement != null)
            {
                instanceIDToPathMap.Remove(reference.uiElement.GetInstanceID());
            }
            else if (reference.instanceID != 0)
            {
                instanceIDToPathMap.Remove(reference.instanceID);
            }

            uiReferenceByPath.Remove(fullPath);

            if (TryGetCategory(reference.elementType.ToString(), out UICategory category))
            {
                category.references.Remove(reference);
            }
        }
        else
        {
            LogWarning(
                $"UIManager remove failed: no element is registered for key '{fullPath}'. Verify the path before removing.");
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

                if (TryGetCategory(reference.elementType.ToString(), out UICategory category))
                {
                    category.references.Remove(reference);
                }
            }
        }
        else
        {
            LogWarning(
                $"UIManager remove failed: no element is registered for instance ID '{instanceID}'. Verify the ID before removing.");
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
            LogWarning(
                $"UIManager lookup failed: instance ID value '{instanceIDString}' is not a valid integer. Pass a numeric instance ID.");
        }
    }

    /// <summary>
    /// Gets a UI reference by full hierarchical path with type validation.
    /// </summary>
    private GameObject GetUIReference(UIElementType elementType, string key)
    {
        if (!uiReferenceByPath.TryGetValue(key, out UIReference reference))
        {
            LogWarning(
                $"UIManager lookup failed: no element is registered for key '{key}'. Verify the path or reinitialize UI references.");
            return null;
        }

        return ValidateAndReturnReference(elementType, key, reference, source: "path");
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

        LogWarning(
            $"UIManager lookup failed: instance ID value '{instanceIDString}' is not a valid integer. Pass a numeric instance ID.");
        return null;
    }

    private GameObject GetUIReferenceByInstanceID(string instanceIDString, UIElementType expectedType)
    {
        if (int.TryParse(instanceIDString, out int instanceID))
        {
            return GetUIReferenceByInstanceID(instanceID, expectedType);
        }

        LogWarning(
            $"UIManager lookup failed: instance ID value '{instanceIDString}' is not a valid integer. Pass a numeric instance ID.");
        return null;
    }

    private GameObject GetUIReferenceByInstanceID(int instanceID)
    {
        if (!instanceIDToPathMap.TryGetValue(instanceID, out string path))
        {
            LogWarning(
                $"UIManager lookup failed: no element is registered for instance ID '{instanceID}'. Verify the ID or reinitialize UI references.");
            return null;
        }

        if (!uiReferenceByPath.TryGetValue(path, out UIReference reference))
        {
            LogWarning(
                $"UIManager lookup failed: instance ID '{instanceID}' points to missing key '{path}'. The stale index entry was removed.");
            instanceIDToPathMap.Remove(instanceID);
            return null;
        }

        return ValidateAndReturnReference(UIElementType.Unknown, path, reference, source: "instanceID");
    }

    private GameObject GetUIReferenceByInstanceID(int instanceID, UIElementType expectedType)
    {
        if (!instanceIDToPathMap.TryGetValue(instanceID, out string path))
        {
            LogWarning(
                $"UIManager lookup failed: no element is registered for instance ID '{instanceID}'. Verify the ID or reinitialize UI references.");
            return null;
        }

        if (!uiReferenceByPath.TryGetValue(path, out UIReference reference))
        {
            LogWarning(
                $"UIManager lookup failed: instance ID '{instanceID}' points to missing key '{path}'. The stale index entry was removed.");
            instanceIDToPathMap.Remove(instanceID);
            return null;
        }

        return ValidateAndReturnReference(expectedType, path, reference, source: "instanceID");
    }

    private GameObject ValidateAndReturnReference(
        UIElementType expectedType,
        string key,
        UIReference reference,
        string source)
    {
        if (reference == null)
        {
            LogWarning(
                $"UIManager lookup failed: key '{key}' has an invalid null reference entry from {source}. Reinitialize UI references.");
            return null;
        }

        if (reference.uiElement == null)
        {
            LogWarning(
                $"UIManager lookup failed: key '{key}' points to a destroyed/missing object. The stale reference was removed; re-register the element if it still exists.");
            RemoveUIReference(reference.fullPath);
            return null;
        }

        if (!IsTypeCompatible(expectedType, reference.elementType))
        {
            LogWarning(
                $"UIManager lookup failed: key '{key}' is registered as '{reference.elementType}', but '{expectedType}' was requested. Use the correct key/type pair or re-register this element with the expected type.");
            return null;
        }

        if (expectedType == UIElementType.Panel && reference.elementType == UIElementType.Image)
        {
            LogWarning(
                $"UIManager compatibility mode: key '{key}' is stored as 'Image' but requested as 'Panel'. This still works for now; re-register as 'Panel' to avoid future breakage.");
        }

        return reference.uiElement;
    }

    private bool IsTypeCompatible(UIElementType expectedType, UIElementType actualType)
    {
        if (expectedType == UIElementType.Unknown)
        {
            return true;
        }

        if (expectedType == actualType)
        {
            return true;
        }

        // Legacy compatibility: older references may have panel entries stored as Image.
        if (expectedType == UIElementType.Panel && actualType == UIElementType.Image)
        {
            return true;
        }

        return false;
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
    public IReadOnlyList<UICategory> GetAllUICategoriesReadOnly()
    {
        return uiCategories;
    }

#if UNITY_EDITOR
    public List<UICategory> GetAllUICategoriesMutable()
    {
        return uiCategories;
    }
#endif

    public List<UICategory> GetAllUICategories()
    {
        return uiCategories;
    }

    private void RebuildCategoryIndex()
    {
        categoryByName.Clear();

        foreach (UICategory category in uiCategories)
        {
            if (category == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(category.name))
            {
                continue;
            }

            if (!categoryByName.ContainsKey(category.name))
            {
                categoryByName.Add(category.name, category);
            }
        }
    }

    private bool TryGetCategory(string categoryName, out UICategory category)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            category = null;
            return false;
        }

        return categoryByName.TryGetValue(categoryName, out category);
    }

    private UICategory GetOrCreateCategory(string categoryName)
    {
        if (TryGetCategory(categoryName, out UICategory category))
        {
            return category;
        }

        category = new UICategory { name = categoryName };
        uiCategories.Add(category);
        categoryByName[categoryName] = category;
        return category;
    }

    #endregion

    #region Generic UI Component Management

    private UIElementType GetElementTypeFromComponent<T>() where T : Component
    {
        if (componentTypeMap.TryGetValue(typeof(T), out UIElementType type))
        {
            return type;
        }

        LogWarning(
            $"UIManager lookup failed: component type '{typeof(T).Name}' is not mapped to a UIElementType. Add it to componentTypeMap.");
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
            return null;
        }

        GameObject uiElement = isInstanceID
            ? GetUIReferenceByInstanceID(key, elementType)
            : GetUIReference(elementType, key);

        if (uiElement == null)
        {
            LogWarning(
                $"UIManager lookup failed: no matching UI element was found for key '{key}'. Verify the key, expected type, and registration state.");
            return null;
        }

        T component = uiElement.GetComponent<T>();
        if (component == null)
        {
            LogWarning(
                $"UIManager component lookup failed: key '{key}' resolved, but component '{typeof(T).Name}' is missing on that GameObject.");
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
        GameObject uiElement = isInstanceID
            ? GetUIReferenceByInstanceID(key, UIElementType.Panel)
            : GetUIReference(UIElementType.Panel, key);

        if (uiElement == null)
        {
            LogWarning(
                $"UIManager panel lookup failed: no panel was found for key '{key}'. Verify the key and panel registration.");
            return null;
        }

        if (uiElement.GetComponent<Image>() == null)
        {
            LogWarning(
                $"UIManager panel lookup failed: key '{key}' resolved, but the GameObject is not a valid panel (missing Image component).");
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

    private readonly List<GameObject> panelDeactivateBuffer = new List<GameObject>();

    private void HandlePanelActivation(GameObject panel, bool deactivateOthers, bool keepLastPanel)
    {
        if (deactivateOthers)
        {
            panelDeactivateBuffer.Clear();
            foreach (var activePanel in activePanels)
            {
                if (activePanel != panel && (!keepLastPanel || activePanel != lastActivePanel))
                {
                    panelDeactivateBuffer.Add(activePanel);
                }
            }

            foreach (var panelToDeactivate in panelDeactivateBuffer)
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
        categoryByName.Clear();
        pathCache.Clear();
        ancestorBuffer.Clear();
        panelDeactivateBuffer.Clear();
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

