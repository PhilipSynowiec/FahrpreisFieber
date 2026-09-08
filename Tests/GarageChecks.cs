using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
[InitializeOnLoad]
public static class GarageQA
{
 static BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 static GarageQA(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("GarageQA",false)){SessionState.SetBool("GarageQA",false);EditorApplication.delayCall+=PlayChecks;}};}
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
 static string[][] Reviews => (string[][])typeof(OfflineReviewService).Assembly.GetType("PassengerReviewCatalog").GetField("ByStars",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
 static CarCatalog Catalog=>AssetDatabase.LoadAssetAtPath<CarCatalog>("Assets/Prefabs/Cars/CarCatalog.asset");
 public static void Validate(){try{
  var catalog=Catalog;Check(catalog.cars.Length==6,"Six cars required");
  foreach(var car in catalog.cars){Check(car.prefab!=null && car.thumbnail!=null,"Missing car asset");Check(car.prefab.GetComponent<CarVisualMotion>()!=null,"Missing car motion");Check(car.prefab.GetComponentsInChildren<Collider>().Length==0,"Visual colliders must not affect handling");}
  int count=0;var unique=new HashSet<string>();
  foreach(var bank in Reviews){Check(bank.Length==200,"200 reviews per star rating");foreach(var review in bank){count++;Check(unique.Add(review),"Duplicate review");}}
  Check(count==1000,"1000 reviews");
  var serviceGO=new GameObject("ReviewTest");var service=serviceGO.AddComponent<OfflineReviewService>();
  foreach(float ratio in new[]{.5f,.2f,.05f,-.1f,-.5f}){string last=null;for(int i=0;i<30;i++){string review=service.GetReview(ratio);Check(Reviews[ReviewRating.FromTimeRatio(ratio)-1].Contains(review),"Review text from wrong star bank");Check(review!=last,"Immediate repeat");last=review;}}UnityEngine.Object.DestroyImmediate(serviceGO);
  foreach(var x in new[]{(1f,5),(.25f,5),(.2499f,4),(.1f,4),(.0999f,3),(0f,3),(-.0001f,2),(-.25f,2),(-.2501f,1)})Check(ReviewRating.FromTimeRatio(x.Item1)==x.Item2,"Rating boundary "+x);
  foreach(string name in new[]{"MainMenu","Game"}){
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject)==0,"Missing script "+tr.name);
   if(name=="MainMenu"){
    Capture("menu-cars");var store=UnityEngine.Object.FindFirstObjectByType<CarStoreUI>();store.Open();Capture("garage");
    var click=Field<Button>(store,"openButton");Vector3 carScreen=Camera.main.WorldToScreenPoint(GameObject.Find("CarPreview").transform.position+Vector3.up);
    Check(RectTransformUtility.RectangleContainsScreenPoint((RectTransform)click.transform,carScreen,Camera.main),"Store click target misses car");
   }else{
    var toast=UnityEngine.Object.FindFirstObjectByType<ToastMessageUI>();var stars=Field<Image[]>(toast,"stars");
    for(int rating=1;rating<=5;rating++){toast.ShowReview(Reviews[rating-1][0],rating,10);int filled=stars.Count(s=>s.color==new Color32(235,172,28,255));Check(filled==rating,"Wrong star count");}
    var world=UnityEngine.Object.FindFirstObjectByType<TileStreamingManager>();world.Refresh(true);for(int i=0;i<30;i++)typeof(TileStreamingManager).GetMethod("BuildSomeTiles",F).Invoke(world,null);Capture("game-cars");
   }
  }
  EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");PlayerPrefs.SetInt("coins",0);PlayerPrefs.DeleteKey("garage.selected");foreach(var car in catalog.cars)PlayerPrefs.DeleteKey("garage.owned."+car.id);PlayerPrefs.Save();SessionState.SetBool("GarageQA",true);EditorApplication.isPlaying=true;
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void PlayChecks(){try{
  var catalog=Catalog;var store=UnityEngine.Object.FindFirstObjectByType<CarStoreUI>();Field<Button>(store,"openButton").onClick.Invoke();Check(Field<GameObject>(store,"panel").activeSelf,"Click does not open store");
  Check(!CarGarage.SelectOrBuy(catalog,1),"Unaffordable purchase accepted");Check(CurrencyManager.Balance==0 && !CarGarage.IsOwned(catalog.cars[1]),"Failed purchase changed state");
  int budget=catalog.cars.Sum(car=>car.price)+1000;CurrencyManager.Instance.AddCoins(budget);var buttons=Field<Button[]>(store,"purchaseButtons");int total=budget;
  for(int i=1;i<6;i++){buttons[i].onClick.Invoke();total-=catalog.cars[i].price;Check(CarGarage.IsOwned(catalog.cars[i])&&CarGarage.SelectedIndex(catalog)==i,"Purchase not selected");Check(CurrencyManager.Balance==total,"Wrong charge");}
  CarGarage.SelectOrBuy(catalog,4);Check(CurrencyManager.Balance==total,"Owned car charged twice");Check(PlayerPrefs.GetString("garage.selected")==catalog.cars[4].id,"Selection not saved");
  Field<Button>(store,"closeButton").onClick.Invoke();Check(!Field<GameObject>(store,"panel").activeSelf,"Close does not hide store");
  SceneManager.sceneLoaded+=GameLoaded;SceneManager.LoadScene("Game");
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void GameLoaded(Scene scene,LoadSceneMode mode){SceneManager.sceneLoaded-=GameLoaded;EditorApplication.delayCall+=()=>{try{
  var controller=UnityEngine.Object.FindFirstObjectByType<CarControllerInputSystem>();Check(Field<float>(controller,"maxSpeed")==18,"Purchased speed not applied");
  var view=controller.GetComponent<CarSelectionView>();var models=Field<GameObject[]>(view,"models");Check(models.Count(g=>g.activeSelf)==1&&models[4].activeSelf,"Selected gameplay model incorrect");
  Check(CurrencyManager.Balance==1000,"Balance did not survive scene transition");
  var motion=models[4].GetComponent<CarVisualMotion>();var wheel=Field<Transform[]>(motion,"wheels")[0];var before=wheel.localRotation;controller.GetComponent<Rigidbody>().linearVelocity=Vector3.forward*8;typeof(CarVisualMotion).GetMethod("LateUpdate",F).Invoke(motion,null);Check(Quaternion.Angle(before,wheel.localRotation)>0,"Wheels did not rotate");
  var body=Field<Transform>(motion,"body");var rb=controller.GetComponent<Rigidbody>();rb.rotation=Quaternion.Euler(0,25,0);typeof(CarVisualMotion).GetMethod("LateUpdate",F).Invoke(motion,null);Check(Quaternion.Angle(Quaternion.identity,body.localRotation)>0 && Quaternion.Angle(Quaternion.identity,body.localRotation)<5,"Body lean missing or excessive");
  var drive=typeof(CarControllerInputSystem).GetMethod("Drive",F);for(int i=0;i<200;i++)drive.Invoke(controller,new object[]{0f,1f,.02f});Check(rb.linearVelocity.magnitude>17.9f && rb.linearVelocity.magnitude<18.01f,"Selected top speed not reached");
  Debug.Log("GARAGE_QA_PASS: assets, 1000 unique reviews, star boundaries/counts, store open/close, affordability, all purchases, no double charge, saved selection, scene transition, applied speed, wheel motion");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};}
 static void Capture(string name){var camera=Camera.main;foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
 var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();foreach(var t in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))t.ForceMeshUpdate();camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();System.IO.File.WriteAllBytes("/tmp/fahrpreis-"+name+".png",image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
}
