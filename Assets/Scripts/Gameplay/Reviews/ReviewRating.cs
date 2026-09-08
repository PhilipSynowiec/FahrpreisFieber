public static class ReviewRating
{
    // timeRatio is the fraction of the deadline remaining at arrival.
    public static int FromTimeRatio(float timeRatio)
    {
        if (timeRatio >= .25f) return 5;
        if (timeRatio >= .10f) return 4;
        if (timeRatio >= 0f) return 3;
        if (timeRatio >= -.25f) return 2;
        return 1;
    }
}
