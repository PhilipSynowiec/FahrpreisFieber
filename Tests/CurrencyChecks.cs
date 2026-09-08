using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
[InitializeOnLoad]
public static class CurrencyQA {
 static CurrencyQA(){EditorApplication.playModeStateChanged += state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("CurrencyQA",false)){SessionState.SetBool("CurrencyQA",false);EditorApplication.delayCall += Run;}};}
 public static void Validate(){
 SessionState.SetBool("CurrencyQA",true);
 EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");PlayerPrefs.SetInt("coins",123);EditorApplication.isPlaying=true;
 }
 static void Run(){try{
 var f=BindingFlags.NonPublic|BindingFlags.Instance;

 var manager=UnityEngine.Object.FindFirstObjectByType<CurrencyManager>();

 var binder=UnityEngine.Object.FindFirstObjectByType<CoinsTextBinder>();
 typeof(CoinsTextBinder).GetMethod("OnEnable",f).Invoke(binder,null);
 var label=binder.GetComponent<TMP_Text>();
 if(label.text!=123.ToString("N0"))throw new Exception("Initial balance stale");
 manager.AddCoins(27);
 if(label.text!=150.ToString("N0")||PlayerPrefs.GetInt("coins")!=150)throw new Exception("Payout balance stale");
 typeof(CoinsTextBinder).GetMethod("OnDisable",f).Invoke(binder,null);
 manager.AddCoins(10);typeof(CoinsTextBinder).GetMethod("OnEnable",f).Invoke(binder,null);
 if(label.text!=160.ToString("N0"))throw new Exception("Re-enabled HUD stale");
 Debug.Log("PASS: saved balance, immediate payout display, saved payout, re-enabled HUD refresh");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
}
