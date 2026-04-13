using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class BuyPadFloatingText : MonoBehaviour
{
    [Tooltip("Text shown above the pad.")]
    public string label = "Buy";

    [Tooltip("World offset from the pad's position.")]
    public Vector3 offset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("Font size (points).")]
    public float fontSize = 36f;

    [Tooltip("Uniform world scale applied to the label.")]
    public float labelScale = 0.02f;

    [Tooltip("If true the label faces the camera fully. If false the label only rotates around Y (no tilt).")]
    public bool fullBillboard = false; // default: only Y-axis

    [Tooltip("Label color.")]
    public Color color = Color.white;

    [Tooltip("Optional camera to face. If null the script will try to find the player camera or Camera.main.")]
    public Camera targetCamera;

    private GameObject labelGO;
    private TextMeshPro tmp;

    private void Awake()
    {
        CreateLabel();
    }

    private void OnValidate()
    {
        if (labelGO != null)
            UpdateLabelVisuals();
    }

    private void CreateLabel()
    {
        if (labelGO != null) return;

        labelGO = new GameObject($"{name}_FloatingLabel_{GetInstanceID()}");
        labelGO.transform.SetParent(null, false);

        tmp = labelGO.AddComponent<TextMeshPro>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;

        UpdateLabelVisuals();
        // start inactive until parent active
        labelGO.SetActive(false);
    }

    private void UpdateLabelVisuals()
    {
        if (tmp == null) return;
        tmp.text = label;
        tmp.fontSize = Mathf.Max(1f, fontSize);
        tmp.color = color;
        labelGO.transform.localScale = Vector3.one * Mathf.Max(0.0001f, labelScale);
    }

    private void LateUpdate()
    {
        if (labelGO == null) CreateLabel();

        bool padActive = this != null && gameObject != null && gameObject.activeInHierarchy;
        if (labelGO.activeSelf != padActive)
            labelGO.SetActive(padActive);
        if (!padActive) return;

        // Position: pad world position + offset (fixed)
        Vector3 worldPos = transform.position + transform.TransformVector(offset);
        labelGO.transform.position = worldPos;

        // Ensure we have a camera reference
        if (targetCamera == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                targetCamera = player.GetComponentInChildren<Camera>();
            if (targetCamera == null)
            {
                var named = GameObject.Find("Main Camera");
                if (named != null) targetCamera = named.GetComponent<Camera>();
            }
            if (targetCamera == null)
                targetCamera = Camera.main;
        }
        if (targetCamera == null) return;

        // Face the camera: either full billboard or Y-axis only (no tilt)
        if (fullBillboard)
        {
            Vector3 dir = labelGO.transform.position - targetCamera.transform.position;
            if (dir.sqrMagnitude > 0.000001f)
                labelGO.transform.rotation = Quaternion.LookRotation(dir.normalized, targetCamera.transform.up);
        }
        else
        {
            // keep upright: compute direction on XZ plane
            Vector3 toCam = targetCamera.transform.position - labelGO.transform.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.000001f)
            {
                labelGO.transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
            }
        }
    }

    private void OnDestroy()
    {
        if (labelGO != null)
        {
            if (Application.isPlaying) Destroy(labelGO);
            else DestroyImmediate(labelGO);
        }
    }
}