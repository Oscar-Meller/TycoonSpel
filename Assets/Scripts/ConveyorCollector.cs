using UnityEngine;

public class ConveyorCollector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Prefer the root Rigidbody object (common for moving parts);
        // fall back to the collider's GameObject.
        var root = (other.attachedRigidbody != null) ? other.attachedRigidbody.gameObject : other.gameObject;
        if (root == null) return;

        // Try to read PartValue from the root first, then from the contacted collider.
        var pv = root.GetComponent<PartValue>() ?? other.GetComponent<PartValue>();
        int amount = (pv != null) ? pv.value : 1;

        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddStoredMoney(amount);
        }

        Debug.Log($"[ConveyorCollector] Collected '{root.name}' value={amount}");

        // Destroy the whole part (root) so it won't be collected again.
        Destroy(root);
    }
}
