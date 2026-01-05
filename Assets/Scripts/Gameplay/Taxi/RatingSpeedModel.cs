using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maintains an exponentially-weighted history of "required speeds" (or any speed metric),
/// and provides a normal distribution to sample future expected speeds.
/// </summary>
public class RatingSpeedModel : MonoBehaviour
{
    [Header("Default distribution (used before enough data)")]
    [SerializeField] private float defaultMean = 6.0f;   // world units/sec
    [SerializeField] private float defaultStd = 1.5f;

    [Header("Exponential weighting")]
    [Range(0.5f, 0.999f)]
    [SerializeField] private float gamma = 0.90f;        // weight decay per older trip
    [SerializeField] private int maxHistory = 50;

    private readonly List<float> speeds = new();

    public void AddSpeedSample(float speed)
    {
        speed = Mathf.Max(0.1f, speed);
        speeds.Insert(0, speed);              // newest at index 0
        if (speeds.Count > maxHistory) speeds.RemoveAt(speeds.Count - 1);
    }

    public (float mean, float std) GetDistribution()
    {
        if (speeds.Count < 5)
            return (defaultMean, defaultStd);

        double wSum = 0;
        double mean = 0;

        for (int i = 0; i < speeds.Count; i++)
        {
            double w = Mathf.Pow(gamma, i);
            wSum += w;
            mean += w * speeds[i];
        }
        mean /= wSum;

        double var = 0;
        for (int i = 0; i < speeds.Count; i++)
        {
            double w = Mathf.Pow(gamma, i);
            double d = speeds[i] - mean;
            var += w * d * d;
        }
        var /= wSum;

        float std = Mathf.Sqrt((float)var);
        std = Mathf.Clamp(std, 0.4f, 5.0f);

        return ((float)mean, std);
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
