using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
public static class AuthorUrban
{
 public static void Build(){
 var catalog=AssetDatabase.LoadAssetAtPath<CarCatalog>("Assets/Prefabs/Cars/CarCatalog.asset");
 foreach(var car in catalog.cars){string path=AssetDatabase.GetAssetPath(car.prefab);var root=PrefabUtility.LoadPrefabContents(path);root.transform.localScale=Vector3.one*.8f;PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);}
 foreach(string sceneName in new[]{"MainMenu","Game"}){
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+sceneName+".unity");
 foreach(var view in UnityEngine.Object.FindObjectsByType<CarSelectionView>(FindObjectsSortMode.None)){
  var so=new SerializedObject(view);var models=so.FindProperty("models");for(int i=0;i<models.arraySize;i++)((GameObject)models.GetArrayElementAtIndex(i).objectReferenceValue).transform.localScale=Vector3.one*.8f;
 }
 if(sceneName=="Game"){
 var controller=UnityEngine.Object.FindFirstObjectByType<CarControllerInputSystem>();var capsule=controller.GetComponent<CapsuleCollider>();capsule.radius=.675f*.8f;capsule.height=3.78f*.8f;
 }else{
 var canvas=GameObject.Find("Canvas").transform;var existing=canvas.Find("DrivingViews");if(existing!=null)UnityEngine.Object.DestroyImmediate(existing.gameObject);
 var bar=new GameObject("DrivingViews",typeof(RectTransform));var rect=(RectTransform)bar.transform;rect.SetParent(canvas,false);rect.sizeDelta=new Vector2(550,56);rect.anchoredPosition=new Vector2(310,-255);bar.transform.SetSiblingIndex(canvas.Find("CarStore").GetSiblingIndex());
 var buttons=new Button[3];var template=GameObject.Find("StartButton");
 for(int i=0;i<3;i++){
  var button=UnityEngine.Object.Instantiate(template,rect);button.name=new[]{"Elevated","Driver","Chase"}[i];var r=(RectTransform)button.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2((i-1)*182,0);r.sizeDelta=new Vector2(170,50);r.localScale=Vector3.one;
  buttons[i]=button.GetComponent<Button>();buttons[i].onClick=new Button.ButtonClickedEvent();
  var text=button.GetComponentInChildren<TMP_Text>();text.text=button.name.ToUpperInvariant();text.fontSize=22;text.fontSizeMin=18;text.fontSizeMax=22;
 }
 var settings=bar.AddComponent<DrivingViewMenu>();var serialized=new SerializedObject(settings);var array=serialized.FindProperty("buttons");array.arraySize=3;for(int i=0;i<3;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=buttons[i];serialized.ApplyModifiedPropertiesWithoutUndo();
 }
 EditorSceneManager.SaveScene(scene);
 }
 AssetDatabase.SaveAssets();Debug.Log("URBAN_AUTHOR_PASS");EditorApplication.Exit(0);
 }
}
