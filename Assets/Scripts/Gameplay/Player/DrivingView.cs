using UnityEngine;

public enum DrivingView { Elevated, Driver, Chase }
public static class DrivingViewPreference
{
    public static DrivingView Selected => (DrivingView)Mathf.Clamp(PlayerPrefs.GetInt("camera.view", 0), 0, 2);
    public static void Set(DrivingView view)
    {
        PlayerPrefs.SetInt("camera.view", Mathf.Clamp((int)view, 0, 2));
        PlayerPrefs.Save();
    }
}
