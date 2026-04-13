using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Uppgrader : MonoBehaviour
{
    public enum UpgradeMode { Add, Multiply }

    [Header("Target (optional)")]
    [Tooltip("Optional GameObject whose 'cost' will also be increased when a part passes (e.g. a BuyPad GameObject).")]
    public GameObject upgradeTarget;

    [Header("Part upgrade")]
    [Tooltip("Mode used to modify parts' value.")]
    public UpgradeMode mode = UpgradeMode.Add;

    [Tooltip("Amount added to part value when mode = Add.")]
    public int addAmount = 1;

    [Tooltip("Multiplier applied to part value when mode = Multiply (2 = double).")]
    public float multiplyFactor = 2f;

    [Header("Filtering")]
    [Tooltip("If non-empty, only parts with this exact name will trigger the upgrader. Leave empty to accept all names.")]
    public string partNameFilter = "";

    [Tooltip("If non-empty, only GameObjects with this tag will trigger the upgrader. Leave empty to accept all tags.")]
    public string partTagFilter = "";

    [Header("Behavior")]
    [Tooltip("If true, all colliders on this GameObject (and children) will be set to triggers so parts can pass through.")]
    public bool ensureTriggerColliders = true;

    [Tooltip("If true the passing part will be destroyed after applying the upgrade.")]
    public bool consumePart = false;

    // Tracks parts currently inside this upgrader so each part is upgraded at most once per pass
    readonly HashSet<int> upgradedThisPass = new HashSet<int>();

    private void Awake()
    {
        if (ensureTriggerColliders)
            EnsureTriggerColliders();
    }

    void EnsureTriggerColliders()
    {
        var colliders = GetComponents<Collider>();
        if (colliders.Length > 0)
        {
            foreach (var col in colliders)
            {
                if (col == null) continue;
                col.isTrigger = true;
            }
            return;
        }

        var childColliders = GetComponentsInChildren<Collider>();
        if (childColliders.Length > 0)
        {
            foreach (var col in childColliders)
            {
                if (col == null) continue;
                col.isTrigger = true;
            }
            return;
        }

        var rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            var bounds = rend.bounds;
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = transform.InverseTransformPoint(bounds.center);
            box.size = bounds.size;
        }
        else
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = Vector3.one;
        }
    }

    // Use attachedRigidbody root when available so we upgrade the whole moving object
    GameObject GetRootObject(Collider other)
    {
        return (other.attachedRigidbody != null) ? other.attachedRigidbody.gameObject : other.gameObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryUpgrade(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // physics may call this multiple times; guard ensures single upgrade per pass
        TryUpgrade(other);
    }

    private void OnTriggerExit(Collider other)
    {
        var root = GetRootObject(other);
        if (root == null) return;
        upgradedThisPass.Remove(root.GetInstanceID());
    }

    void TryUpgrade(Collider other)
    {
        if (other == null) return;

        var root = GetRootObject(other);
        if (root == null) return;

        // Filters
        if (!string.IsNullOrEmpty(partNameFilter) && root.name != partNameFilter)
            return;

        if (!string.IsNullOrEmpty(partTagFilter) && !root.CompareTag(partTagFilter))
            return;

        int id = root.GetInstanceID();
        if (upgradedThisPass.Contains(id))
            return; // already upgraded while inside this zone

        // Mark as upgraded so subsequent contacts inside the trigger won't re-upgrade
        upgradedThisPass.Add(id);

        // Find or add PartValue component on the passing object
        var pv = root.GetComponent<PartValue>();
        if (pv == null)
        {
            pv = root.AddComponent<PartValue>();
            pv.value = Mathf.Max(1, 1);
        }

        // Apply upgrade
        if (mode == UpgradeMode.Add)
        {
            pv.Add(addAmount);
            Debug.Log($"[Uppgrader] Added {addAmount} to part '{root.name}' (new value {pv.value}).");
        }
        else
        {
            pv.Multiply(multiplyFactor);
            Debug.Log($"[Uppgrader] Multiplied part '{root.name}' by {multiplyFactor} (new value {pv.value}).");
        }

        // Optionally increase designated upgradeTarget's cost (keeps previous behavior)
        if (upgradeTarget != null)
        {
            var buyPad = upgradeTarget.GetComponent<BuyPadActivator>();
            if (buyPad != null)
            {
                // For multiply mode we add rounded value of current addAmount influence (best-effort)
                int increment = (mode == UpgradeMode.Add) ? addAmount : Mathf.RoundToInt(addAmount * multiplyFactor);
                buyPad.cost += increment;
                Debug.Log($"[Uppgrader] Also increased BuyPadActivator.cost by {increment} (new cost {buyPad.cost}).");
            }
            else
            {
                var behaviours = upgradeTarget.GetComponents<MonoBehaviour>();
                foreach (var b in behaviours)
                {
                    if (b == null) continue;
                    var type = b.GetType();
                    var field = type.GetField("cost", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field != null && field.FieldType == typeof(int))
                    {
                        int old = (int)field.GetValue(b);
                        field.SetValue(b, old + addAmount);
                        Debug.Log($"[Uppgrader] Increased '{type.Name}.cost' on '{upgradeTarget.name}' from {old} to {old + addAmount}.");
                        break;
                    }
                }
            }
        }

        if (consumePart)
            Destroy(root);
    }
}
