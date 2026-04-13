using UnityEngine;

/// <summary>
/// Holds the monetary value for a spawned part (e.g. a ball).
/// Attach to moving parts or let the Dropper add it at spawn time.
/// </summary>
public class PartValue : MonoBehaviour
{
    [Tooltip("Current value this part will give when collected.")]
    public int value = 1;

    /// <summary>
    /// Increase value by a fixed amount.
    /// Value is clamped to a minimum of 1.
    /// </summary>
    public void Add(int amount)
    {
        value += amount;
        if (value < 1) value = 1;
    }

    /// <summary>
    /// Multiply value by factor (rounded to nearest int), minimum 1.
    /// </summary>
    public void Multiply(float factor)
    {
        value = Mathf.Max(1, Mathf.RoundToInt(value * factor));
    }
}