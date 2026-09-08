using UnityEngine;

/// <summary>Pure trip economy rules. No scene state, saving, or UI side effects.</summary>
public static class TripRewardCalculator
{
    public static float TimeLimit(float distance, float targetSpeed)
        => Mathf.Clamp(distance / Mathf.Max(.001f, targetSpeed), 10f, 240f);

    public static int Quote(float distance, float timeLimit, float coinsPerWorldUnit, float rushCoinsFactor)
        => Mathf.Max(0, Mathf.RoundToInt(distance * coinsPerWorldUnit
            + distance / Mathf.Max(.001f, timeLimit) * rushCoinsFactor));

    public static TripResult Complete(PassengerJob job, float elapsed, bool reached)
    {
        bool onTime = reached && elapsed <= job.timeLimitSeconds;
        return new TripResult
        {
            onTime = onTime,
            elapsedSeconds = elapsed,
            timeLimitSeconds = job.timeLimitSeconds,
            payoutCoins = onTime ? Mathf.Max(0, job.quotedPriceCoins) : 0
        };
    }
}
