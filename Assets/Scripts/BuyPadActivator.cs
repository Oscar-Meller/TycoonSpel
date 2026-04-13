using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BuyPadActivator : MonoBehaviour
{
    public int cost = 0;

    [Tooltip("Objects to enable")]
    public GameObject[] targetsToEnable;

    [Tooltip("Color when player can afford this pad")]
    public Color affordableColor = Color.green;

    [Tooltip("Color when player cannot afford this pad")]
    public Color unaffordableColor = Color.red;

    [Tooltip("Sound played when player CAN afford and activates the pad")]
    public AudioClip canAffordSfx;

    [Tooltip("Sound played when player CANNOT afford the pad")]
    public AudioClip cannotAffordSfx;

    [Range(0f, 1f)]
    [Tooltip("Volume for the buy pad SFX")]
    public float sfxVolume = 1f;

    private bool activated = false;

    // Cached renderers for tinting visuals
    Renderer[] cachedRenderers;

    // 🔑 Global lista (delas mellan alla BuyPads)
    private static HashSet<Transform> globallyEnabled = new HashSet<Transform>();

    private void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnEnable()
    {
        MoneyManager.OnMoneyChanged += OnMoneyChanged;
        UpdateColor();
    }

    private void OnDisable()
    {
        MoneyManager.OnMoneyChanged -= OnMoneyChanged;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        TryActivate();
    }

    private void TryActivate()
    {
        if (activated) return;

        // 🔥 FIX: rensa bort destroyed objects
        CleanupDestroyed();

        // 💰 Payment
        if (cost > 0)
        {
            if (MoneyManager.Instance == null) return;

            if (!MoneyManager.Instance.TrySpend(cost))
            {
                Debug.Log($"Need {cost}, have {MoneyManager.Instance.money}");

                // Play cannot-afford SFX if assigned
                if (cannotAffordSfx != null)
                    AudioSource.PlayClipAtPoint(cannotAffordSfx, transform.position, Mathf.Clamp01(sfxVolume));

                return;
            }
        }

        // Play can-afford SFX (covers cost==0 or successful purchase)
        if (canAffordSfx != null)
            AudioSource.PlayClipAtPoint(canAffordSfx, transform.position, Mathf.Clamp01(sfxVolume));

        if (targetsToEnable == null || targetsToEnable.Length == 0)
        {
            activated = true;
            Destroy(gameObject);
            return;
        }

        // 🔑 Lägg till nya objekt utan att ta bort gamla
        foreach (GameObject target in targetsToEnable)
        {
            if (target == null) continue;

            Transform t = target.transform;

            // Om parent → ta alla children
            CollectAllChildren(t, globallyEnabled);

            // Lägg till parents (så de syns)
            Transform p = t.parent;
            while (p != null)
            {
                globallyEnabled.Add(p);
                p = p.parent;
            }
        }

        // 🔑 Applicera state
        ApplyGlobalState();

        activated = true;
        Destroy(gameObject);
    }

    // 🧹 Ta bort förstörda referenser
    void CleanupDestroyed()
    {
        globallyEnabled.RemoveWhere(t => t == null);
    }

    // 🔧 Samla children (för parent targets)
    void CollectAllChildren(Transform t, HashSet<Transform> set)
    {
        if (t == null) return;

        set.Add(t);

        foreach (Transform child in t)
        {
            CollectAllChildren(child, set);
        }
    }

    // 🔧 Applicera allt som ska vara aktivt
    void ApplyGlobalState()
    {
        HashSet<Transform> roots = new HashSet<Transform>();

        // Hitta roots säkert
        foreach (var t in globallyEnabled)
        {
            if (t == null) continue;

            Transform r = t;

            while (r != null && r.parent != null)
                r = r.parent;

            if (r != null)
                roots.Add(r);
        }

        // Applicera state per root
        foreach (var root in roots)
        {
            if (root == null) continue;

            root.gameObject.SetActive(true);
            ApplyRecursive(root);
        }
    }

    void ApplyRecursive(Transform current)
    {
        if (current == null) return;

        bool shouldBeActive = globallyEnabled.Contains(current);

        if (current.gameObject.activeSelf != shouldBeActive)
            current.gameObject.SetActive(shouldBeActive);

        foreach (Transform child in current)
        {
            ApplyRecursive(child);
        }
    }

    // Called when MoneyManager notifies a change
    void OnMoneyChanged(int newMoney)
    {
        UpdateColor();
    }

    void UpdateColor()
    {
        bool canAfford = cost <= 0 || (MoneyManager.Instance != null && MoneyManager.Instance.money >= cost);
        Color targetColor = canAfford ? affordableColor : unaffordableColor;

        if (cachedRenderers == null || cachedRenderers.Length == 0)
            cachedRenderers = GetComponentsInChildren<Renderer>(true);

        foreach (var r in cachedRenderers)
        {
            if (r == null) continue;

            // Using material creates an instance so each pad is tinted independently.
            try
            {
                if (r.material != null && r.material.HasProperty("_Color"))
                {
                    r.material.color = targetColor;
                }
            }
            catch
            {
                // ignore materials we can't modify at runtime
            }
        }
    }
}