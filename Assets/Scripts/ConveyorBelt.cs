   using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    [Tooltip("Hastighet i enheter/sekund l�ngs banans lokala X-axel.")]
    public float speed = 2f;

    [Tooltip("Om true anv�nds banans lokala X-axel. Om false anv�nds v�rldens X-axel.")]
    public bool useLocalSpace = true;

    [Tooltip("Om true p�verkar endast objekt med angiven tag. L�mna false f�r alla objekt.")]
    public bool filterByTag = false;

    [Tooltip("Tag som p�verkas n�r filterByTag �r true.")]
    public string allowedTag = "Untagged";

    [Tooltip("Om true ritar en pil i scenen som visar riktningen.")]
    public bool drawGizmo = true;

    private Vector3 ConveyorVelocity => (useLocalSpace ? transform.right : Vector3.right) * speed;

    private void OnCollisionStay(Collision collision)
    {
        TryApplyConveyor(collision.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        TryApplyConveyor(other.gameObject);
    }

    private void TryApplyConveyor(GameObject obj)
    {
        if (filterByTag && obj.tag != allowedTag) return;

        var vel = ConveyorVelocity;
        ApplyToRigidbody(obj, vel);
        ApplyToCharacterController(obj, vel);
        ApplyToTransformFallback(obj, vel);
    }

    private void ApplyToRigidbody(GameObject obj, Vector3 vel)
    {
        // Hitta en Rigidbody p� objektet eller dess barn
        var rb = obj.GetComponent<Rigidbody>() ?? obj.GetComponentInChildren<Rigidbody>();
        if (rb == null) return;

        // Beh�ll vertikal (Y) r�relse, ers�tt endast horisontella komponenter
        Vector3 newVel = rb.linearVelocity;
        // Ber�kna v�rldskomponenter (x,z) fr�n vel
        Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);

        // Om rigidbody �r kinematisk, flytta med MovePosition (fysiskt korrekt)
        if (rb.isKinematic)
        {
            rb.MovePosition(rb.position + horizontal * Time.fixedDeltaTime);
            return;
        }

        // F�r icke-kinematisk rigidbody, s�tt horisontell hastighet direkt f�r stabil conveyor-effekt
        newVel.x = horizontal.x;
        newVel.z = horizontal.z;
        rb.linearVelocity = newVel;
    }

    private void ApplyToCharacterController(GameObject obj, Vector3 vel)
    {
        var cc = obj.GetComponent<CharacterController>() ?? obj.GetComponentInChildren<CharacterController>();
        if (cc == null) return;

        // Flytta CharacterController med Move (Time.fixedDeltaTime eftersom detta anrop k�rs i fysik)
        cc.Move(vel * Time.fixedDeltaTime);
    }

    private void ApplyToTransformFallback(GameObject obj, Vector3 vel)
    {
        // Om objektet saknar Rigidbody och CharacterController, g�r en f�rsiktig transform-�vers�ttning.
        // Detta p�verkar t.ex. enkla non-physical pynt. Kontrollera collider-setup s� detta inte skapar tunnling.
        var hasRb = obj.GetComponent<Rigidbody>() || obj.GetComponentInChildren<Rigidbody>();
        var hasCc = obj.GetComponent<CharacterController>() || obj.GetComponentInChildren<CharacterController>();
        if (hasRb || hasCc) return;

        // Flytta bara rot-Transform (v�rldsf�rflyttning)
        obj.transform.Translate(vel * Time.fixedDeltaTime, Space.World);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo) return;
        Gizmos.color = Color.cyan;
        Vector3 start = transform.position;
        Vector3 dir = (useLocalSpace ? transform.right : Vector3.right) * Mathf.Sign(speed) * 0.5f;
        Gizmos.DrawLine(start, start + dir);
        Gizmos.DrawSphere(start + dir, 0.02f);
    }
}
