#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Nimrita.FlowUI.Editor.Playground
{
    /// <summary>
    /// Tracks all UI GameObjects created by the playground using Scene Diff.
    /// ZERO manual tracking - automatically detects what was created!
    /// Philosophy: "Solve once, reuse forever"
    /// </summary>
    public class UIStateTracker
    {
        private HashSet<int> preExecutionObjectIds = new HashSet<int>();
        private List<GameObject> trackedObjects = new List<GameObject>();
        private int currentExecutionId = 0;

        /// <summary>
        /// Takes a snapshot of the scene BEFORE execution.
        /// Call this right before running user code.
        /// </summary>
        public void BeginExecution()
        {
            currentExecutionId++;

            // Snapshot all GameObjects currently in scene
            preExecutionObjectIds.Clear();
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                preExecutionObjectIds.Add(obj.GetInstanceID());
            }

            // Silent snapshot
        }

        /// <summary>
        /// Detects what was created during execution by diffing the scene.
        /// Call this right AFTER user code executes.
        /// </summary>
        public void EndExecution()
        {
            // Find all NEW objects (created during execution)
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            List<GameObject> newObjects = new List<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                int id = obj.GetInstanceID();
                if (!preExecutionObjectIds.Contains(id))
                {
                    newObjects.Add(obj);
                }
            }

            // Track them
            trackedObjects.AddRange(newObjects);

            // Log only what was created (helpful for user)
            if (newObjects.Count > 0)
            {
                string names = string.Join(", ", newObjects.Select(o => o.name));
                Debug.Log($"[Playground] ✓ Created: {names}");
            }
        }

        /// <summary>
        /// Cleans up all tracked GameObjects from previous execution.
        /// Call this BEFORE starting new execution.
        /// </summary>
        public void CleanupPreviousExecution()
        {
            // Remove nulls first (might have been manually deleted)
            trackedObjects.RemoveAll(obj => obj == null);

            // Silent cleanup (user sees new objects appear, that's enough feedback)

            // Destroy all tracked objects
            for (int i = trackedObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = trackedObjects[i];
                if (obj != null)
                {
                    // Use DestroyImmediate in editor
                    Object.DestroyImmediate(obj);
                }
            }

            trackedObjects.Clear();
        }

        /// <summary>
        /// Gets the count of currently tracked objects.
        /// </summary>
        public int GetTrackedObjectCount()
        {
            trackedObjects.RemoveAll(obj => obj == null);
            return trackedObjects.Count;
        }

        /// <summary>
        /// Clears all tracking without destroying objects.
        /// Useful when you want to keep the UI but stop tracking.
        /// </summary>
        public void ClearTracking()
        {
            trackedObjects.Clear();
            preExecutionObjectIds.Clear();
        }

        /// <summary>
        /// Gets all currently tracked objects (read-only).
        /// </summary>
        public IReadOnlyList<GameObject> GetTrackedObjects()
        {
            trackedObjects.RemoveAll(obj => obj == null);
            return trackedObjects.AsReadOnly();
        }
    }
}
#endif
