using UnityEngine;

public class CollectorPad : MonoBehaviour
{
    [Tooltip("Sound played when the player collects cash")]
    public AudioClip collectSfx;

    [Range(0f, 1f)]
    [Tooltip("Volume for the collect sound")]
    public float sfxVolume = 1f;

    private void OnTriggerEnter(Collider other)
    {
        // Kolla om det är spelaren
        if (!other.CompareTag("Player"))
            return;

        // Cash in
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.CollectMoney();

        // Play optional SFX. PlayClipAtPoint creates a temporary AudioSource so no component is required.
        if (collectSfx != null)
            AudioSource.PlayClipAtPoint(collectSfx, transform.position, Mathf.Clamp01(sfxVolume));
    }
}
