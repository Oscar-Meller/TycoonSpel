using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class SceneInitializer : MonoBehaviour
{
    [Tooltip("Exact root GameObject names to keep active at Play start.")]
    public string[] whitelistNames = new string[]
    {
        "Terrain",
        "Player",
        "BuyPad1",
        "EventSystem",
        "MoneyManager",
        "Canvas",
        "Main Camera"
    };

    [Tooltip("Any GameObject with one of these tags will be kept active at Play start.")]
    public string[] whitelistTags = new string[] {  };

    [Tooltip("If a root (or any of its children, including inactive) has one of these component type names it will be kept.")]
    public string[] keepIfHasComponent = new string[] { "MoneyManager" };

    [Tooltip("Names of BuyPad root-children that should be kept active at start. Use the exact child names under the BuyPads parent.")]
    public string[] initialBuyPadNames = new string[] { "BuyPad1" };

    [Tooltip("Name of the parent GameObject which contains all BuyPad entries in the Hierarchy.")]
    public string buyPadsParentName = "BuyPads";

    [Tooltip("Run Initialize automatically in Awake when Play starts.")]
    public bool runOnAwake = true;

    [Tooltip("Write which roots are kept/disabled to Console.")]
    public bool debugLogs = true;

    // Public API — call from other scripts/managers if you don't want auto-run.
    public void Initialize()
    {
        if (!Application.isPlaying)
        {
            if (debugLogs) Debug.Log("SceneInitializer.Initialize aborted: not in Play mode.");
            return;
        }

        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root == null) continue;

            // Never disable the object that holds this component
            if (root == gameObject)
            {
                if (debugLogs) Debug.Log($"SceneInitializer: Keeping initializer '{root.name}'");
                continue;
            }

            // Special handling for the BuyPads parent: keep the parent but only enable the children listed in initialBuyPadNames
            if (!string.IsNullOrEmpty(buyPadsParentName) && root.name.Equals(buyPadsParentName, StringComparison.Ordinal))
            {
                ProcessBuyPadsParent(root);
                // keep the parent itself active
                if (debugLogs) Debug.Log($"SceneInitializer: Processed BuyPads parent '{root.name}'");
                continue;
            }

            if (ShouldKeep(root))
            {
                if (debugLogs) Debug.Log($"SceneInitializer: Keeping '{root.name}'");
                continue;
            }

            root.SetActive(false);
            if (debugLogs) Debug.Log($"SceneInitializer: Disabled root '{root.name}'");
        }
    }

    // Enable only the child BuyPads explicitly named in initialBuyPadNames. Disable other BuyPad children.
    private void ProcessBuyPadsParent(GameObject parent)
    {
        if (parent == null) return;

        // Ensure parent is active so we can set children appropriately
        if (!parent.activeSelf) parent.SetActive(true);

        // Build a HashSet for fast lookup
        var keepSet = new HashSet<string>(StringComparer.Ordinal);
        if (initialBuyPadNames != null)
        {
            foreach (var n in initialBuyPadNames)
                if (!string.IsNullOrWhiteSpace(n)) keepSet.Add(n);
        }

        // Find direct children that have BuyPadActivator (or any child that should be considered a BuyPad)
        // We'll treat any direct child with a BuyPadActivator component as a pad entry.
        var childTransforms = parent.transform;
        for (int i = 0; i < childTransforms.childCount; i++)
        {
            var child = childTransforms.GetChild(i).gameObject;
            if (child == null) continue;

            // If the child contains BuyPadActivator anywhere in its subtree, consider it a pad entry
            var hasActivator = child.GetComponentInChildren(typeof(BuyPadActivator), true) != null;
            if (!hasActivator)
            {
                // leave non-buypad children alone (they may be other organizational objects)
                if (debugLogs) Debug.Log($"SceneInitializer: Skipping non-buypad child '{child.name}' under '{parent.name}'");
                continue;
            }

            // If listed in initialBuyPadNames -> keep active, otherwise deactivate
            if (keepSet.Contains(child.name))
            {
                if (!child.activeSelf) child.SetActive(true);
                if (debugLogs) Debug.Log($"SceneInitializer: Keeping initial BuyPad child '{child.name}'");
            }
            else
            {
                if (child.activeSelf) child.SetActive(false);
                if (debugLogs) Debug.Log($"SceneInitializer: Disabling BuyPad child '{child.name}'");
            }
        }
    }

    // Decide if a root should remain active at Play start.
    private bool ShouldKeep(GameObject go)
    {
        if (go == null) return false;

        // Exact name match
        if (whitelistNames != null)
        {
            foreach (var n in whitelistNames)
            {
                if (string.IsNullOrWhiteSpace(n)) continue;
                if (go.name.Equals(n, StringComparison.Ordinal)) return true;
            }
        }

        // Tag match
        if (whitelistTags != null)
        {
            foreach (var tag in whitelistTags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                try
                {
                    if (go.CompareTag(tag)) return true;
                }
                catch { }
            }
        }

        // Keep if contains one of the configured components (search children, include inactive)
        if (keepIfHasComponent != null)
        {
            foreach (var typeName in keepIfHasComponent)
            {
                if (string.IsNullOrWhiteSpace(typeName)) continue;

                Type t = Type.GetType(typeName);
                if (t == null)
                {
                    // search assemblies for short name
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            foreach (var tt in asm.GetTypes())
                            {
                                if (tt.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                                {
                                    t = tt;
                                    break;
                                }
                            }
                            if (t != null) break;
                        }
                        catch { }
                    }
                }

                if (t != null)
                {
                    var comp = go.GetComponentInChildren(t, true);
                    if (comp != null) return true;
                }
            }
        }

        return false;
    }

    private void Awake()
    {
        // Only operate when Play starts. In Editor (not playing) nothing is changed so you keep full visibility while building.
        if (!Application.isPlaying) return;
        if (!runOnAwake) return;
        Initialize();
    }

    // Editor helper: logs which roots WOULD be disabled (no changes) — use from context menu in Editor.
    [ContextMenu("Log Preview: Which roots would be disabled")]
    private void LogPreview()
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        var toDisable = new List<string>();
        foreach (var root in roots)
        {
            if (root == null) continue;
            if (root == gameObject) continue;
            if (!ShouldKeep(root)) toDisable.Add(root.name);
        }

        Debug.Log($"SceneInitializer Preview: {toDisable.Count} root(s) would be disabled: {string.Join(", ", toDisable)}");
    }
}