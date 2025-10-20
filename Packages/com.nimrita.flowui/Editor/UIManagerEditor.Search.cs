#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class UIManagerEditor : Editor
{
    #region Search and Filtering

    // Search state variables
    private string searchQuery = string.Empty;
    private string previousSearchQuery = string.Empty;
    private bool isSearching = false;
    private float lastSearchTime = 0f;
    private const float SEARCH_DELAY = 0.2f; // Debounce time in seconds

    // Caching for search performance
    private Dictionary<int, string> transformPathCache = new Dictionary<int, string>();
    private Dictionary<Transform, bool> searchMatchCache = new Dictionary<Transform, bool>();
    private Dictionary<Transform, bool> childMatchCache = new Dictionary<Transform, bool>();
    private HashSet<Transform> expandedSearchResults = new HashSet<Transform>();

    // Pre-fetched scene objects to avoid Unity API calls in background threads
    private List<Transform> sceneHierarchyCache = new List<Transform>();
    private bool searchResultsUpdated = false;

    /// <summary>
    /// Draws the search bar for the hierarchy view with debouncing.
    /// </summary>
    public void DrawHierarchySearchBar()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();

        // Search icon with status indicator
        GUIContent searchContent = EditorGUIUtility.IconContent("Search Icon");

        // Show spinner if currently searching
        if (isSearching)
        {
            float time = (float)EditorApplication.timeSinceStartup;
            int frame = Mathf.FloorToInt((time * 10) % 4);
            string spinner = frame == 0 ? "◐" : frame == 1 ? "◓" : frame == 2 ? "◑" : "◒";

            GUILayout.Label(spinner, GUILayout.Width(20), GUILayout.Height(20));
        }
        else if (searchContent != null && searchContent.image != null)
        {
            GUILayout.Label(searchContent, GUILayout.Width(20), GUILayout.Height(20));
        }
        else
        {
            GUILayout.Label("🔍", GUILayout.Width(20));
        }

        // Search field with improved styling
        GUIStyle searchFieldStyle = new GUIStyle(EditorStyles.toolbarSearchField);
        searchFieldStyle.fixedHeight = 22;

        EditorGUI.BeginChangeCheck();
        string newSearchQuery = EditorGUILayout.TextField(searchQuery, searchFieldStyle, GUILayout.ExpandWidth(true));
        if (EditorGUI.EndChangeCheck())
        {
            // Debounce search input
            searchQuery = newSearchQuery;
            lastSearchTime = (float)EditorApplication.timeSinceStartup;
            EditorApplication.update -= DelayedSearch;
            EditorApplication.update += DelayedSearch;
        }

        // Clear button with better styling
        GUIStyle clearButtonStyle = new GUIStyle(EditorStyles.miniButton);
        clearButtonStyle.fixedHeight = 22;
        if (GUILayout.Button(new GUIContent("Clear", "Clear search"), clearButtonStyle, GUILayout.Width(50)))
        {
            if (!string.IsNullOrEmpty(searchQuery))
            {
                searchQuery = string.Empty;
                previousSearchQuery = string.Empty;
                ClearSearchCache();
                GUI.FocusControl(null); // Remove focus from search field
                Repaint();
            }
        }

        EditorGUILayout.EndHorizontal();

        // Display search stats if we're showing results 
        if (!string.IsNullOrEmpty(searchQuery) && !isSearching)
        {
            int resultCount = searchMatchCache.Count(kv => kv.Value);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(22); // Match indentation with search field

            GUIStyle resultStyle = new GUIStyle(EditorStyles.miniLabel);
            resultStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            string resultsText = resultCount == 1
                ? "1 result found"
                : $"{resultCount} results found";

            EditorGUILayout.LabelField(resultsText, resultStyle);

            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// Performs search after a short delay to avoid searching on every keystroke.
    /// </summary>
    private void DelayedSearch()
    {
        if ((float)EditorApplication.timeSinceStartup - lastSearchTime >= SEARCH_DELAY)
        {
            EditorApplication.update -= DelayedSearch;

            if (searchQuery != previousSearchQuery)
            {
                // Pre-fetch scene hierarchy on main thread before starting search
                PreFetchSceneHierarchy();

                // Start the search process
                PerformSearch();
                previousSearchQuery = searchQuery;
            }
        }
    }

    /// <summary>
    /// Pre-fetches the scene hierarchy to avoid Unity API calls from background threads.
    /// This MUST run on the main thread.
    /// </summary>
    private void PreFetchSceneHierarchy()
    {
        sceneHierarchyCache.Clear();

        // Get all canvases in current scene
        var currentScene = uiManager.gameObject.scene;
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.InstanceID)
            .Where(c => c != null && c.gameObject.scene == currentScene);

        // Add all transforms in the hierarchy to our cache
        foreach (var canvas in canvases)
        {
            if (canvas == null || canvas.transform == null) continue;

            // Add the canvas transform
            sceneHierarchyCache.Add(canvas.transform);

            // Get all child transforms
            var childTransforms = canvas.GetComponentsInChildren<Transform>(true);
            sceneHierarchyCache.AddRange(childTransforms);

            // Cache all paths
            foreach (var transform in childTransforms)
            {
                if (transform == null) continue;

                // Store path in cache if not already present
                int instanceId = transform.GetInstanceID();
                if (!transformPathCache.ContainsKey(instanceId))
                {
                    transformPathCache[instanceId] = CalculateTransformPath(transform);
                }
            }
        }
    }

    /// <summary>
    /// Executes the search operation with caching.
    /// </summary>
    private void PerformSearch()
    {
        if (string.IsNullOrEmpty(searchQuery))
        {
            ClearSearchCache();
            Repaint();
            return;
        }

        isSearching = true;

        // Start search on the main thread to avoid Unity API access issues
        EditorApplication.delayCall += () => {
            try
            {
                // Clear previous search results
                searchMatchCache.Clear();
                childMatchCache.Clear();
                expandedSearchResults.Clear();

                if (string.IsNullOrEmpty(searchQuery))
                {
                    isSearching = false;
                    Repaint();
                    return;
                }

                // Process all transforms in the cached hierarchy
                foreach (var transform in sceneHierarchyCache)
                {
                    if (transform == null) continue;

                    // Check if this transform matches
                    bool isMatch = DoesTransformMatchSearch(transform);
                    searchMatchCache[transform] = isMatch;
                }

                // Compute child match status (must be done after all direct matches are computed)
                ComputeChildMatchStatus();

                // Compute which transforms should be expanded
                ComputeExpandedTransforms();

                searchResultsUpdated = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during search: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                isSearching = false;
                Repaint();
            }
        };
    }

    /// <summary>
    /// Computes which transforms have matching children.
    /// </summary>
    private void ComputeChildMatchStatus()
    {
        // Build parent-child relationships
        Dictionary<Transform, List<Transform>> childrenByParent = new Dictionary<Transform, List<Transform>>();

        foreach (var transform in sceneHierarchyCache)
        {
            if (transform == null || transform.parent == null) continue;

            if (!childrenByParent.ContainsKey(transform.parent))
            {
                childrenByParent[transform.parent] = new List<Transform>();
            }

            childrenByParent[transform.parent].Add(transform);
        }

        // Start with transforms that have no children (leaf nodes)
        var leaves = sceneHierarchyCache.Where(t => t != null && !childrenByParent.ContainsKey(t)).ToList();

        // Mark all leaves
        foreach (var leaf in leaves)
        {
            childMatchCache[leaf] = searchMatchCache.TryGetValue(leaf, out bool matches) && matches;
        }

        // Process bottom-up for all transforms
        var remainingTransforms = sceneHierarchyCache
            .Where(t => t != null && !leaves.Contains(t))
            .OrderByDescending(t => GetTransformDepth(t)) // Process deepest first
            .ToList();

        foreach (var transform in remainingTransforms)
        {
            bool anyChildMatches = false;

            // Check if this transform has any children in our relationship map
            if (childrenByParent.TryGetValue(transform, out var children))
            {
                foreach (var child in children)
                {
                    // A child matches if it directly matches or has matching descendants
                    bool childMatches =
                        (searchMatchCache.TryGetValue(child, out bool directMatch) && directMatch) ||
                        (childMatchCache.TryGetValue(child, out bool descendantMatch) && descendantMatch);

                    if (childMatches)
                    {
                        anyChildMatches = true;
                        break;
                    }
                }
            }

            // Store the result - a transform matches if it directly matches or any child matches
            bool directlyMatches = searchMatchCache.TryGetValue(transform, out bool transformMatches) && transformMatches;
            childMatchCache[transform] = anyChildMatches || directlyMatches;
        }
    }

    /// <summary>
    /// Gets the depth of a transform in the hierarchy.
    /// </summary>
    private int GetTransformDepth(Transform transform)
    {
        int depth = 0;
        Transform current = transform;

        while (current.parent != null)
        {
            depth++;
            current = current.parent;
        }

        return depth;
    }

    /// <summary>
    /// Compute which transforms should be expanded to show search results.
    /// </summary>
    private void ComputeExpandedTransforms()
    {
        foreach (var match in searchMatchCache.Where(kv => kv.Value))
        {
            if (match.Key == null) continue;

            // Expand all parents
            Transform parent = match.Key.parent;
            while (parent != null)
            {
                expandedSearchResults.Add(parent);
                parent = parent.parent;
            }
        }
    }

    /// <summary>
    /// Determines if a transform matches the current search query with caching.
    /// </summary>
    public bool IsMatchingSearch(Transform transform)
    {
        if (transform == null)
            return false;

        if (string.IsNullOrEmpty(searchQuery))
            return true;

        // Check if we have a cached result
        if (searchMatchCache.TryGetValue(transform, out bool isMatch))
        {
            return isMatch;
        }

        // If not cached (should be rare), compute result directly
        isMatch = DoesTransformMatchSearch(transform);
        searchMatchCache[transform] = isMatch;

        return isMatch;
    }

    /// <summary>
    /// Low-level search match function without caching.
    /// </summary>
    private bool DoesTransformMatchSearch(Transform transform)
    {
        if (transform == null || string.IsNullOrEmpty(searchQuery))
            return false;

        return transform.name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Determines if any child of the transform matches the search query with caching.
    /// </summary>
    public bool AnyChildMatchesSearch(Transform transform)
    {
        if (transform == null)
            return false;

        if (string.IsNullOrEmpty(searchQuery))
            return true;

        // Check if we have a cached result
        if (childMatchCache.TryGetValue(transform, out bool anyChildMatches))
        {
            return anyChildMatches;
        }

        // Fall back to direct computation for safety - this should rarely happen
        anyChildMatches = false;

        // Check each child
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (IsMatchingSearch(child) || AnyChildMatchesSearch(child))
            {
                anyChildMatches = true;
                break;
            }
        }

        childMatchCache[transform] = anyChildMatches;
        return anyChildMatches;
    }

    /// <summary>
    /// Calculates the full path of a transform in the hierarchy.
    /// </summary>
    private string CalculateTransformPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        string path = transform.name;
        Transform current = transform;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    /// <summary>
    /// Gets the full path of a transform in the hierarchy with caching.
    /// </summary>
    public string GetCachedPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        int instanceId = transform.GetInstanceID();

        // Check if path is already cached
        if (transformPathCache.TryGetValue(instanceId, out string cachedPath))
        {
            return cachedPath;
        }

        // Calculate and cache the path
        string path = CalculateTransformPath(transform);
        transformPathCache[instanceId] = path;
        return path;
    }

    /// <summary>
    /// Should we expand this transform in the UI based on search results?
    /// </summary>
    public bool ShouldExpandForSearch(Transform transform)
    {
        if (string.IsNullOrEmpty(searchQuery))
            return false;

        return expandedSearchResults.Contains(transform);
    }

    /// <summary>
    /// Clears search-related caches to free memory.
    /// </summary>
    private void ClearSearchCache()
    {
        searchMatchCache.Clear();
        childMatchCache.Clear();
        expandedSearchResults.Clear();
        isSearching = false;

        // Don't clear path cache - it's useful beyond just search
    }

    /// <summary>
    /// Updates the DrawHierarchyItem method to use search caching.
    /// This needs to be called from DrawHierarchyItem in UIManagerEditor.Hierarchy.cs.
    /// </summary>
    public void UpdateHierarchyDrawing()
    {
        if (searchResultsUpdated)
        {
            // Apply the search results to the UI state
            if (!string.IsNullOrEmpty(searchQuery))
            {
                foreach (var transform in expandedSearchResults)
                {
                    if (foldoutStates.ContainsKey(transform))
                    {
                        foldoutStates[transform] = false; // false = expanded
                    }
                }
            }

            searchResultsUpdated = false;
        }
    }

    /// <summary>
    /// Gets suggested UI elements based on search query.
    /// </summary>
    public UIReference[] GetSearchSuggestions()
    {
        if (string.IsNullOrEmpty(searchQuery))
            return Array.Empty<UIReference>();

        return uiManager.GetAllUICategories()
            .SelectMany(category => category.references)
            .Where(reference => reference.name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
            .Take(10) // Limit to 10 results for performance
            .ToArray();
    }

    #endregion
}
#endif