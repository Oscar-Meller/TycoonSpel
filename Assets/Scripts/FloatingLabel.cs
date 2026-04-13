using UnityEngine;
using TMPro;

[ExecuteAlways]
public class FloatingLabel : MonoBehaviour
{
    public enum FaceMode { Full, YAxisOnly }

    [Tooltip("One-word label shown above the pad.")]
    public string label = "Item";

    [Tooltip("World offset from the pad's position where the label will be placed.")]
    public Vector3 offset = new Vector3(0f, 1.0f, 0f);

    [Tooltip("Base font size (points).")]
    public float fontSize = 36f;

    [Tooltip("Local scale applied to the label GameObject.")]
    public float labelScale = 0.02f;

    [Tooltip("Label color.")]
    public Color color = Color.white;

    [Tooltip("If set, this camera is used. Otherwise the script will try to find the player camera or Camera.main.")]
    public Camera targetCamera;

    [Tooltip("How the label faces the camera.")]
    public FaceMode faceMode = FaceMode.Full;

    [Header("Appearance")]
    [Tooltip("Enable an outline (gives perceived thickness).")]
    public bool useOutline = true;
    [Tooltip("Outline width (0..1).")]
    [Range(0f, 1f)] public float outlineWidth = 0.2f;
    [Tooltip("Outline color.")]
    public Color outlineColor = Color.black;

    [Tooltip("Create a shadow duplicate behind the text for extra depth.")]
    public bool useShadow = true;
    [Tooltip("Shadow local offset in world units.")]
    public Vector3 shadowOffset = new Vector3(0.02f, -0.02f, 0.02f);
    [Tooltip("Shadow color.")]
    public Color shadowColor = new Color(0f, 0f, 0f, 0.6f);

    const string labelName = "__FloatingLabel_TMP";
    const string shadowName = "__FloatingLabelShadow_TMP";

    private TextMeshPro tmp;
    private TextMeshPro tmpShadow;
    private GameObject labelGO;
    private GameObject shadowGO;

    // Keep a reference to the parent transform so we can follow it.
    private Transform parentTransform;

    private void OnEnable()
    {
        parentTransform = transform;
        Setup();
        UpdateLabel();
        UpdateVisibility(true);
    }

    private void OnValidate()
    {
        parentTransform = transform;
        Setup();
        UpdateLabel();
        UpdateVisibility(true);
    }

    private void OnDisable()
    {
        // Hide labels when the component / BuyPad is disabled
        UpdateVisibility(false);

        // Clean up created objects in edit mode to avoid leftovers
        if (!Application.isPlaying)
        {
            if (labelGO != null) DestroyImmediate(labelGO);
            if (shadowGO != null) DestroyImmediate(shadowGO);
            labelGO = null;
            shadowGO = null;
            tmp = null;
            tmpShadow = null;
        }
    }

    private void OnDestroy()
    {
        // Remove runtime objects when pad is destroyed
        if (labelGO != null)
        {
            if (Application.isPlaying) Destroy(labelGO);
            else DestroyImmediate(labelGO);
        }
        if (shadowGO != null)
        {
            if (Application.isPlaying) Destroy(shadowGO);
            else DestroyImmediate(shadowGO);
        }
    }

    private void Setup()
    {
        // Create label as a root GameObject (no parent) so it doesn't inherit parent's scale
        if (labelGO == null)
        {
            var existing = GameObject.Find(labelName + "_" + GetInstanceID());
            if (existing != null) labelGO = existing;
        }

        if (labelGO == null)
        {
            labelGO = new GameObject(labelName + "_" + GetInstanceID());
            // Do NOT parent to this transform — place in root so scale is independent
            labelGO.transform.SetParent(null, false);
        }

        tmp = labelGO.GetComponent<TextMeshPro>() ?? labelGO.AddComponent<TextMeshPro>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.transform.localRotation = Quaternion.identity;
        tmp.transform.localScale = Vector3.one * Mathf.Max(0.0001f, labelScale);

        if (useOutline)
        {
            tmp.outlineWidth = outlineWidth;
            tmp.outlineColor = outlineColor;
        }
        else
        {
            tmp.outlineWidth = 0f;
        }

        // Shadow (separate GameObject also at root)
        if (useShadow)
        {
            if (shadowGO == null)
            {
                var existingS = GameObject.Find(shadowName + "_" + GetInstanceID());
                if (existingS != null) shadowGO = existingS;
            }

            if (shadowGO == null)
            {
                shadowGO = new GameObject(shadowName + "_" + GetInstanceID());
                shadowGO.transform.SetParent(null, false);
            }

            tmpShadow = shadowGO.GetComponent<TextMeshPro>() ?? shadowGO.AddComponent<TextMeshPro>();
            tmpShadow.text = label;
            tmpShadow.alignment = TextAlignmentOptions.Center;
            tmpShadow.enableWordWrapping = false;
            tmpShadow.fontSize = fontSize;
            tmpShadow.color = shadowColor;
            tmpShadow.raycastTarget = false;
            tmpShadow.transform.localScale = tmp.transform.localScale;
        }
        else
        {
            if (shadowGO != null)
            {
                if (Application.isPlaying) Destroy(shadowGO);
                else DestroyImmediate(shadowGO);
                shadowGO = null;
                tmpShadow = null;
            }
        }
    }

    private void UpdateLabel()
    {
        if (tmp == null) Setup();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.transform.localScale = Vector3.one * Mathf.Max(0.0001f, labelScale);
        tmp.outlineWidth = useOutline ? outlineWidth : 0f;
        tmp.outlineColor = outlineColor;

        if (tmpShadow != null)
        {
            tmpShadow.text = label;
            tmpShadow.fontSize = fontSize;
            tmpShadow.color = shadowColor;
            tmpShadow.transform.localScale = tmp.transform.localScale;
        }
    }

    private void LateUpdate()
    {
        if (labelGO == null) Setup();
        if (parentTransform == null) parentTransform = transform;

        // Position label in worldspace relative to the pad
        Vector3 worldPos = parentTransform.position + parentTransform.TransformVector(offset);
        labelGO.transform.position = worldPos;

        if (shadowGO != null)
            shadowGO.transform.position = parentTransform.position + parentTransform.TransformVector(offset + shadowOffset);

        // Keep label visibility synchronized (in case runtime code enabled/disabled pad)
        UpdateVisibility(parentTransform.gameObject.activeInHierarchy);

        // Ensure we have a camera reference: prefer explicit targetCamera, otherwise try to find "Main Camera" or camera under Player, then Camera.main
        if (targetCamera == null)
        {
            // 1) try GameObject named "Main Camera"
            var mainByName = GameObject.Find("Main Camera");
            if (mainByName != null)
                targetCamera = mainByName.GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            // 2) try camera under GameObject tagged "Player"
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                var cam = playerGO.GetComponentInChildren<Camera>();
                if (cam != null) targetCamera = cam;
            }
        }

        if (targetCamera == null)
        {
            // 3) fallback to Camera.main (requires camera tagged MainCamera)
            targetCamera = Camera.main;
        }

        if (targetCamera == null) return; // still not found

        if (faceMode == FaceMode.Full)
        {
            // Full face: text faces camera exactly
            Vector3 dir = labelGO.transform.position - targetCamera.transform.position;
            if (dir.sqrMagnitude > 0.000001f)
            {
                Quaternion rot = Quaternion.LookRotation(dir.normalized, targetCamera.transform.up);
                labelGO.transform.rotation = rot;
                if (shadowGO != null) shadowGO.transform.rotation = rot;
            }
        }
        else
        {
            // Y-axis only: keep label upright, rotate around Y so it faces camera horizontally
            Vector3 toCamera = targetCamera.transform.position - labelGO.transform.position;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude <= 0.000001f)
            {
                // fallback to full face if camera is exactly above/below
                Vector3 dir = labelGO.transform.position - targetCamera.transform.position;
                if (dir.sqrMagnitude > 0.000001f)
                {
                    Quaternion rot = Quaternion.LookRotation(dir.normalized, targetCamera.transform.up);
                    labelGO.transform.rotation = rot;
                    if (shadowGO != null) shadowGO.transform.rotation = rot;
                }
            }
            else
            {
                Quaternion rot = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
                // Quaternion.LookRotation expects forward vector; we want text forward toward camera => -toCamera
                labelGO.transform.rotation = rot;
                if (shadowGO != null) shadowGO.transform.rotation = rot;
            }
        }
    }

    // Synchronize label active state with the BuyPad visibility.
    private void UpdateVisibility(bool visible)
    {
        if (labelGO != null) labelGO.SetActive(visible);
        if (shadowGO != null) shadowGO.SetActive(visible && tmpShadow != null);
    }

    // API for other scripts to set the label text
    public void SetLabel(string text)
    {
        label = text;
        UpdateLabel();
    }
}