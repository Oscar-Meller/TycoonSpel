using System.Collections;
using UnityEngine;

public class Dropper : MonoBehaviour
{
    [Tooltip("The static visual body (kept in place). If empty the script will try to find a child named 'DropperBody'")]
    public GameObject dropperBody;

    [Tooltip("The original spawner. The script will keep this in place, make it semi-transparent and non-colliding.")]
    public GameObject dropperSpawner;

    [Tooltip("Seconds between spawned copies")]
    public float spawnInterval = 1f;

    [Tooltip("Name to give to each duplicate spawner")]
    public string duplicateName = "Dropp1";

    [Tooltip("Scale applied to each spawned duplicate (XYZ)")]
    public Vector3 duplicateScale = new Vector3(0.25f, 0.25f, 0.25f);

    [Tooltip("Initial monetary value assigned to each spawned part")]
    public int initialPartValue = 1;

    private void Start()
    {
        // try to auto-find children if not assigned in inspector
        if (dropperBody == null)
            dropperBody = transform.Find("DropperBody")?.gameObject;

        if (dropperSpawner == null)
            dropperSpawner = transform.Find("DropperSpawner")?.gameObject;

        if (dropperSpawner == null)
        {
            Debug.LogError("Dropper: dropperSpawner not assigned and not found as child named 'DropperSpawner'.");
            enabled = false;
            return;
        }

        // Make original spawner semi-transparent (50%) if it has a renderer
        var rend = dropperSpawner.GetComponent<Renderer>() ?? dropperSpawner.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            // Use material instance so we don't modify shared material unexpectedly
            var mat = rend.material;
            if (mat.HasProperty("_Color"))
            {
                Color c = mat.color;
                c.a = 0.5f;
                mat.color = c;
                // Ensure rendering mode allows transparency (best-effort; depends on shader)
            }
        }

        // Make original spawner non-colliding so duplicates can fall through it
        var origCollider = dropperSpawner.GetComponent<Collider>() ?? dropperSpawner.GetComponentInChildren<Collider>();
        if (origCollider != null)
        {
            origCollider.isTrigger = true;
        }
        else
        {
            // If there is no collider, add a trigger box collider sized to the renderer bounds (if available)
            if (rend != null)
            {
                var box = dropperSpawner.AddComponent<BoxCollider>();
                box.isTrigger = true;
                // try to size box to renderer bounds
                var bounds = rend.bounds;
                box.center = dropperSpawner.transform.InverseTransformPoint(bounds.center);
                box.size = bounds.size;
            }
        }

        // Start spawning loop
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Instantiate a copy of the original spawner at the same place and rotation
            var dup = Instantiate(dropperSpawner, dropperSpawner.transform.position, dropperSpawner.transform.rotation);
            dup.name = duplicateName;
            dup.transform.parent = null;
            dup.SetActive(true);

            // Force the duplicate to the desired scale immediately so bounds/colliders reflect correct size
            dup.transform.localScale = duplicateScale;

            // Ensure all renderers on duplicate are enabled and opaque (so they are visible)
            var drends = dup.GetComponentsInChildren<Renderer>();
            foreach (var dr in drends)
            {
                if (dr == null) continue;
                dr.enabled = true;
                var dmat = dr.material;
                if (dmat != null && dmat.HasProperty("_Color"))
                {
                    Color dc = dmat.color;
                    dc.a = 1f;
                    dmat.color = dc;
                }
            }

            // Make all colliders on duplicate non-trigger so it collides with world
            var dcols = dup.GetComponentsInChildren<Collider>();
            if (dcols.Length > 0)
            {
                foreach (var c in dcols)
                {
                    c.isTrigger = false;
                }
            }
            else
            {
                // add a fallback collider so it can collide with the world
                var added = dup.AddComponent<BoxCollider>();
                added.isTrigger = false;
                if (drends.Length > 0)
                {
                    var b = drends[0].bounds;
                    added.center = dup.transform.InverseTransformPoint(b.center);
                    added.size = b.size;
                }
            }

            // Ensure duplicate falls: enable any existing Rigidbodies, otherwise add one on root
            var rbs = dup.GetComponentsInChildren<Rigidbody>();
            if (rbs.Length > 0)
            {
                foreach (var r in rbs)
                {
                    r.isKinematic = false;
                    r.useGravity = true;
                }
            }
            else
            {
                var rb = dup.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            // Ensure the spawned object has a PartValue and set its initial value
            var pv = dup.GetComponent<PartValue>();
            if (pv == null)
                pv = dup.AddComponent<PartValue>();
            pv.value = Mathf.Max(1, initialPartValue);

            Debug.Log($"[Dropper] Spawned '{dup.name}' with initial value {pv.value}");

            // Explicitly ensure the spawned object's world position matches the original spawner's world position
            dup.transform.SetPositionAndRotation(dropperSpawner.transform.position, dropperSpawner.transform.rotation);
        }
    }
}
