using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class Logic
{
    /// <summary>
    /// Calculates the interpolation weight for exponential smoothing.
    /// </summary>
    /// <param name="smoothingSpeed">Smoothing speed (rate) in units per second; higher values converge faster.</param>
    /// <param name="delta">Frame delta time in seconds.</param>
    /// <returns>Interpolation weight in the range [0, 1), where values closer to 1 indicate stronger immediate response.</returns>
    public static float ComputeLerpWeight(float smoothingSpeed, double delta)
    {
        float weight = (float)(1f - Math.Exp(-smoothingSpeed * delta));
        return weight;
    }

    /// <summary>
    /// Returns the inverse linear interpolation of a value within the given range,
    /// clamped between 0.0 and 1.0.
    /// </summary>
    /// <param name="from">The start value of the range.</param>
    /// <param name="to">The end value of the range.</param>
    /// <param name="weight">The value to normalize within the range.</param>
    /// <returns>
    /// A normalized value between 0.0 and 1.0 representing the relative position of
    /// <paramref name="weight"/> between <paramref name="from"/> and <paramref name="to"/>.
    /// </returns>
    public static float InverseLerpClamped(float from, float to, float weight)
    {
        float value = (weight - from) / (to - from);
        return Mathf.Clamp(value, 0f, 1f);
    }
}
