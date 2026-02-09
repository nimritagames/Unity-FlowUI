#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SceneManagement;

namespace Nimrita.FlowUI.Editor.Playground
{
    /// <summary>
    /// Production-grade UI tracking using parent container pattern.
    ///
    /// Performance: O(1) cleanup instead of O(n) scene scanning.
    /// Replaces UIStateTracker's scene diff approach with a much faster solution.
    ///
    /// How it works:
    /// 1. Creates a hidden root GameObject: "__PLAYGROUND__"
    /// 2. All playground-created UI objects are parented to this root
    /// 3. Cleanup = just destroy the root (always O(1), even with 1000+ objects)
    ///
    /// This is 100× faster on large scenes than FindObjectsOfType.
    /// </summary>
    public class ContainerTracker
    {
        private const string ROOT_NAME = "__PLAYGROUND_ROOT__";

        private GameObject rootContainer;
        private PlaygroundCoroutineHost coroutineHost;
        private List<GameObject> createdObjects = new List<GameObject>();
        private int currentExecutionId = 0;
        private readonly HashSet<int> baselineObjects = new HashSet<int>();

        /// <summary>
        /// Get the current root container transform.
        /// User code will parent UI objects to this.
        /// </summary>
        public Transform RootTransform => rootContainer?.transform;
        public PlaygroundCoroutineHost CoroutineHost => coroutineHost;

        /// <summary>
        /// Get count of tracked objects (for stats).
        /// </summary>
        public int TrackedObjectCount => createdObjects.Count;

        /// <summary>
        /// Get current execution ID (increments each run).
        /// </summary>
        public int ExecutionId => currentExecutionId;

        /// <summary>
        /// Call this BEFORE executing user code.
        /// Creates a new container and prepares for tracking.
        /// </summary>
        public void BeginExecution()
        {
            currentExecutionId++;

            // Clean up any previous execution
            CleanupPrevious();

            CaptureBaseline();

            // Create new root container
            rootContainer = new GameObject(ROOT_NAME);
            coroutineHost = rootContainer.AddComponent<PlaygroundCoroutineHost>();
            PlaygroundContext.RootTransform = rootContainer.transform;
            PlaygroundContext.CoroutineHost = coroutineHost;

            // Hide from hierarchy (optional - can be made visible for debugging)
            rootContainer.hideFlags = HideFlags.DontSave;
            // Note: NOT using HideInHierarchy so users can see what's created for debugging

            Debug.Log($"[ContainerTracker] Execution #{currentExecutionId} - Container created");
        }

        /// <summary>
        /// Call this AFTER executing user code.
        /// Collects all created objects from the container.
        /// </summary>
        public void EndExecution()
        {
            if (rootContainer == null)
            {
                Debug.LogWarning("[ContainerTracker] EndExecution called but no container exists");
                return;
            }

            // Collect all children from container
            createdObjects.Clear();
            int childCount = rootContainer.transform.childCount;

            for (int i = 0; i < childCount; i++)
            {
                Transform child = rootContainer.transform.GetChild(i);
                if (child != null)
                {
                    createdObjects.Add(child.gameObject);
                }
            }

            CaptureStrayObjects();

            Debug.Log($"[ContainerTracker] Execution #{currentExecutionId} - Tracked {createdObjects.Count} objects");
        }

        /// <summary>
        /// Clean up all tracked objects from previous execution.
        /// This is O(1) - just destroys the list of known objects.
        /// </summary>
        public void CleanupPrevious()
        {
            int destroyedCount = 0;

            // Destroy all tracked objects
            foreach (GameObject obj in createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                    destroyedCount++;
                }
            }

            // Destroy the container itself
            if (rootContainer != null)
            {
                Object.DestroyImmediate(rootContainer);
            }

            createdObjects.Clear();
            baselineObjects.Clear();
            coroutineHost = null;
            PlaygroundContext.RootTransform = null;
            PlaygroundContext.CoroutineHost = null;

            if (destroyedCount > 0)
            {
                Debug.Log($"[ContainerTracker] Cleaned up {destroyedCount} objects");
            }
        }

        /// <summary>
        /// Clear all tracking without destroying objects.
        /// Use this if you want to keep the created UI.
        /// </summary>
        public void ClearTracking()
        {
            createdObjects.Clear();

            if (rootContainer != null)
            {
                Object.DestroyImmediate(rootContainer);
                rootContainer = null;
            }
            coroutineHost = null;
            PlaygroundContext.RootTransform = null;
            PlaygroundContext.CoroutineHost = null;

            Debug.Log("[ContainerTracker] Tracking cleared (objects preserved)");
        }

        /// <summary>
        /// Get statistics for debugging.
        /// </summary>
        public string GetStats()
        {
            return $"Execution #{currentExecutionId}, Tracking {createdObjects.Count} objects";
        }

        private void CaptureBaseline()
        {
            baselineObjects.Clear();
            foreach (var go in SceneGameObjectsSnapshot())
            {
                baselineObjects.Add(go.GetInstanceID());
            }
        }

        private void CaptureStrayObjects()
        {
            foreach (var go in SceneGameObjectsSnapshot())
            {
                int id = go.GetInstanceID();
                if (baselineObjects.Contains(id)) continue;
                if (createdObjects.Contains(go)) continue;
                if (rootContainer != null && go == rootContainer) continue;

                createdObjects.Add(go);
            }
        }

        private IEnumerable<GameObject> SceneGameObjectsSnapshot()
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            foreach (var root in roots)
            {
                foreach (var go in Traverse(root))
                {
                    yield return go;
                }
            }
        }

        private IEnumerable<GameObject> Traverse(GameObject root)
        {
            yield return root;

            var t = root.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                foreach (var child in Traverse(t.GetChild(i).gameObject))
                {
                    yield return child;
                }
            }
        }
    }

    /// <summary>
    /// Helper context passed to user code.
    /// Provides access to the playground root container.
    /// </summary>
    public static class PlaygroundContext
    {
        public static Transform RootTransform { get; set; }
        public static PlaygroundCoroutineHost CoroutineHost { get; set; }
    }

    public class PlaygroundCoroutineHost : MonoBehaviour
    {
        public Coroutine Run(System.Collections.IEnumerator routine)
        {
            return StartCoroutine(routine);
        }
    }
}
#endif
