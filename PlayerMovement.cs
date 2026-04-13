
/* 
PSEUDOCODE / PLAN (detailed):
- Purpose: Make the Player (a capsule) move forward, right, back, left using keyboard input.
- Use Unity's input axes ("Horizontal" and "Vertical") so WASD and arrow keys work by default.
- Use Rigidbody for physics-friendly movement:
  - Cache Rigidbody in Start().
  - In FixedUpdate() read input axes.
  - Build a Vector3 movement = new Vector3(horizontal, 0, vertical).
  - Normalize movement so diagonal speed is not faster.
  - Compute newPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime.
  - Move the Rigidbody using rb.MovePosition(newPosition) for smooth physics.
- Optionally rotate the capsule to face movement direction:
  - If movement is non-zero, compute target rotation with Quaternion.LookRotation(movement).
  - Smoothly interpolate rotation with rb.MoveRotation(Quaternion.Slerp(...)).
- Add public fields for moveSpeed and rotateToMovement so they can be adjusted in the Inspector.
- Warn if no Rigidbody is attached.

IMPLEMENTATION BELOW: Unity C# script (PlayerMovement.cs)
*/

using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Tooltip("Movement speed in units per second.")]
    public float moveSpeed = 5f;

    [Tooltip("If true, rotate the player to face movement direction.")]
    public bool rotateToMovement = true;

    [Tooltip("Rotation speed used when rotating to face movement direction.")]
    public float rotationSpeed = 10f;

    private Rigidbody rb;
    // Input captured in Update() and consumed in FixedUpdate()
    private Vector3 pendingInput = Vector3.zero;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("PlayerMovement: No Rigidbody found on the player. Adding one at runtime.");
            rb = gameObject.AddComponent<Rigidbody>();
            // Keep object upright by freezing X and Z rotation
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        if (rb.isKinematic)
        {
            Debug.LogWarning("PlayerMovement: Rigidbody is kinematic. Setting isKinematic = false so physics movement works.");
            rb.isKinematic = false;
        }
    }

    // Read input every frame (recommended) and store it for physics step.
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal"); // A/D, Left/Right
        float vertical = Input.GetAxis("Vertical");     // W/S, Up/Down

        pendingInput = new Vector3(horizontal, 0f, vertical);

        // Normalize to prevent faster diagonal movement
        if (pendingInput.sqrMagnitude > 1f)
            pendingInput.Normalize();

        // Quick debug hint: log when input is detected so you can confirm keys register.
        if (pendingInput.sqrMagnitude > 0.0001f)
        {
            // Use LogFormat to reduce string allocations when disabled, and keep messages short.
            Debug.LogFormat("PlayerMovement: input {0}", pendingInput);
            // Optional: draw a green line in Scene view so you can see direction while testing.
            Debug.DrawLine(transform.position, transform.position + pendingInput, Color.green, 0.1f);
        }
    }

    // Apply physics-based movement in FixedUpdate
    void FixedUpdate()
    {
        if (rb == null)
            return;

        if (pendingInput.sqrMagnitude < 0.0001f)
            return; // nothing to do

        Vector3 displacement = pendingInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + displacement);

        if (rotateToMovement)
        {
            // Only rotate based on horizontal input direction (ignore Y)
            Vector3 lookDir = new Vector3(pendingInput.x, 0f, pendingInput.z);
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir, Vector3.up);
                Quaternion smoothed = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                rb.MoveRotation(smoothed);
            }
        }
    }
}
