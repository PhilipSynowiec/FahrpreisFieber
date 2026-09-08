using UnityEngine;

// Retain the scene component GUID. All 1,000 reviews are predefined and offline.
public class OfflineReviewService : MonoBehaviour
{
    private readonly int[] previous = { -1, -1, -1, -1, -1 };
    public string GetReview(float timeRatio)
    {
        int rating = ReviewRating.FromTimeRatio(timeRatio);
        var reviews = PassengerReviewCatalog.ByStars[rating - 1];
        int last = previous[rating - 1];
        int index = UnityEngine.Random.Range(0, reviews.Length - (last >= 0 ? 1 : 0));
        if (last >= 0 && index >= last) index++;
        previous[rating - 1] = index;
        return reviews[index];
    }
}
