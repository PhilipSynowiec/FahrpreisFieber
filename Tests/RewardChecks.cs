using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Runs only in the disposable QA project; never clear the developer's real save.
public static class RewardQA
{
    public static void Validate()
    {
        try
        {
            if (Application.productName != "FahrpreisFieberQA")
                throw new Exception("Use the isolated QA project.");
            PlayerPrefs.DeleteAll();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Check(CurrencyManager.Balance == 0, "Fresh profile must start at zero");
            var job = new PassengerJob { timeLimitSeconds = 50, quotedPriceCoins = 408 };
            Check(TripRewardCalculator.TimeLimit(300, 6) == 50, "Deadline");
            Check(TripRewardCalculator.TimeLimit(1, 6) == 10, "Minimum deadline");
            Check(TripRewardCalculator.TimeLimit(3000, 6) == 240, "Maximum deadline");
            Check(TripRewardCalculator.Quote(300, 50, 1.2f, 8) == 408, "Quote");
            foreach (float elapsed in new[] { 30f, 45f, 49f, 50f })
                Check(TripRewardCalculator.Complete(job, elapsed, true).payoutCoins == 408, "On-time payout");
            Check(TripRewardCalculator.Complete(job, 50.01f, true).payoutCoins == 0, "Late payout");
            Check(TripRewardCalculator.Complete(job, 30, false).payoutCoins == 0, "No arrival");
            var first = new GameObject("Difficulty").AddComponent<RatingSpeedModel>();
            InvokeAwake(first);
            Check(first.GetDistribution().mean == 6, "Initial difficulty");
            first.ReportTripResult(25, 50);
            UnityEngine.Object.DestroyImmediate(first.gameObject);
            var second = new GameObject("Reloaded difficulty").AddComponent<RatingSpeedModel>();
            InvokeAwake(second);
            Check(second.GetDistribution().mean == 7, "Difficulty reload");
            Debug.Log("REWARD_QA_PASS: zero start, quotes, deadlines, payout boundaries, persisted difficulty");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void InvokeAwake(RatingSpeedModel model) => typeof(RatingSpeedModel)
        .GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
        .Invoke(model, null);
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
}
