using System;
using UnityEngine;

public class DisableTrace : MonoBehaviour
{
    private void OnDisable()
    {
        Debug.LogError($"[DisableTrace] {name} DISABLED\n{Environment.StackTrace}", this);
    }

    private void OnEnable()
    {
        Debug.Log($"[DisableTrace] {name} enabled", this);
    }
}
