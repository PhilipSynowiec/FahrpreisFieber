using UnityEngine;

/// <summary>
/// Manages difficulty (target speed) using a Dynamic Difficulty Adjustment (DDA) system.
/// Adjusts the target speed based on how well the player performs relative to the time limit.
/// </summary>
public class RatingSpeedModel : MonoBehaviour
{
    [Header("Difficulty Settings")]
    [SerializeField] private float currentMean = 6.0f;   // Current target speed (world units/sec)
    [SerializeField] private float defaultStd = 1.5f;    // Constant variance

    [Header("Adjustment Logic")]
    [SerializeField] private float adjustmentStrength = 2.0f; // How much the mean changes per 100% performance diff
    [SerializeField] private float maxAdjustmentRatio = 0.5f; // Cap performance ratio to +/- 50% to ignore outliers
    
    [Header("Bounds")]
    [SerializeField] private float minMean = 3.0f;
    [SerializeField] private float maxMean = 15.0f;

    /// <summary>
    /// Reports the result of a trip to adjust difficulty.
    /// </summary>
    /// <param name="elapsed">Time taken to complete the trip.</param>
    /// <param name="timeLimit">The time limit for the trip.</param>
    public void ReportTripResult(float elapsed, float timeLimit)
    {
        if (timeLimit <= 0.001f) return;

        // Calculate performance ratio:
        // Positive = Finished early (Success)
        // Negative = Late (Failure)
        // e.g. Limit 30s, Elapsed 20s -> (30 - 20) / 30 = 0.33 (33% faster)
        // e.g. Limit 30s, Elapsed 40s -> (30 - 40) / 30 = -0.33 (33% slower)
        float ratio = (timeLimit - elapsed) / timeLimit;

        // Clamp to avoid outliers (e.g. AFK or cheats)
        ratio = Mathf.Clamp(ratio, -maxAdjustmentRatio, maxAdjustmentRatio);

        // Adjust the mean
        // If ratio is positive (good), mean increases (harder)
        // If ratio is negative (bad), mean decreases (easier)
        currentMean += ratio * adjustmentStrength;

        // Keep within reasonable bounds
        currentMean = Mathf.Clamp(currentMean, minMean, maxMean);

        Debug.Log($"[RatingSpeedModel] Trip Report: Elapsed {elapsed:F1}s / Limit {timeLimit:F1}s. " +
                  $"Ratio: {ratio:F2}. New Difficulty: {currentMean:F2}");
    }

    public (float mean, float std) GetDistribution()
    {
        return (currentMean, defaultStd);
    }

    // Box-Muller normal sample
    public float SampleSpeed()
    {
        var (m, s) = GetDistribution();
        float u1 = Mathf.Max(1e-6f, Random.value);
        float u2 = Mathf.Max(1e-6f, Random.value);
        float z0 = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        float sample = m + s * z0;
        return Mathf.Clamp(sample, 1.5f, 20f);
    }
}
