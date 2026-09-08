using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
[InitializeOnLoad]
public static class UrbanQA
{
 static BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static UrbanQA(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("UrbanQA",false)){SessionState.SetBool("UrbanQA",false);EditorApplication.delayCall+=MenuChecks;}};}
 public static void Validate(){try{
  foreach(string name in new[]{"MainMenu","Game"}){
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject)==0,"Missing script "+tr.name);
   if(name=="MainMenu")Capture("urban-menu");
  }
  var catalog=AssetDatabase.LoadAssetAtPath<CarCatalog>("Assets/Prefabs/Cars/CarCatalog.asset");foreach(var car in catalog.cars)Check(car.prefab.transform.localScale==Vector3.one*.8f,"Car scale is not uniformly .8");
  var controller=UnityEngine.Object.FindFirstObjectByType<CarControllerInputSystem>();var capsule=controller.GetComponent<CapsuleCollider>();Check(Mathf.Approximately(capsule.radius,.54f)&&Mathf.Approximately(capsule.height,3.024f),"Collider not reduced by 20%");
  var world=UnityEngine.Object.FindFirstObjectByType<TileStreamingManager>();world.Refresh(true);Build(world);CheckLots(world);
  var camera=UnityEngine.Object.FindFirstObjectByType<FollowCamera>();typeof(FollowCamera).GetMethod("Awake",F).Invoke(camera,null);
  foreach(DrivingView mode in Enum.GetValues(typeof(DrivingView))){camera.SetView(mode);Capture("urban-"+mode.ToString().ToLowerInvariant());}
  camera.SetView(DrivingView.Elevated);foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
  Camera.main.transform.position=new Vector3(85,140,-60);Camera.main.transform.LookAt(new Vector3(25,0,25));Camera.main.orthographic=true;Camera.main.orthographicSize=72;Capture("urban-city");
  EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");PlayerPrefs.SetInt("camera.view",0);SessionState.SetBool("UrbanQA",true);EditorApplication.isPlaying=true;
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void MenuChecks(){try{
  var menu=UnityEngine.Object.FindFirstObjectByType<DrivingViewMenu>();var buttons=Field<Button[]>(menu,"buttons");Check(buttons.Length==3,"Missing camera choices");
  for(int i=0;i<3;i++){buttons[i].onClick.Invoke();Check((int)DrivingViewPreference.Selected==i && PlayerPrefs.GetInt("camera.view")==i,"Choice not saved");Check(!buttons[i].interactable,"Selected view not indicated");}
  buttons[1].onClick.Invoke();SceneManager.sceneLoaded+=GameLoaded;SceneManager.LoadScene("Game");
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void GameLoaded(Scene scene,LoadSceneMode mode){SceneManager.sceneLoaded-=GameLoaded;EditorApplication.delayCall+=()=>{try{
  var camera=UnityEngine.Object.FindFirstObjectByType<FollowCamera>();Check(Field<DrivingView>(camera,"mode")==DrivingView.Driver,"Saved camera choice not applied");
  var controller=UnityEngine.Object.FindFirstObjectByType<CarControllerInputSystem>();controller.enabled=false;var rb=controller.GetComponent<Rigidbody>();rb.linearVelocity=Vector3.zero;
  var world=UnityEngine.Object.FindFirstObjectByType<TileStreamingManager>();world.Refresh(true);Build(world);CheckLots(world);
  var renderers=controller.GetComponentsInChildren<Renderer>(true);Check(renderers.All(r=>r.forceRenderingOff),"Driver camera sees own roof");
  controller.transform.rotation=Quaternion.Euler(0,90,0);camera.SetView(DrivingView.Driver);Check(Vector3.Dot(camera.transform.forward,controller.transform.forward)>.999f,"Driver camera does not follow heading");
  camera.SetView(DrivingView.Chase);Check(renderers.All(r=>!r.forceRenderingOff),"Car still hidden in chase view");
  camera.SetView(DrivingView.Elevated);Check(Vector3.Distance(camera.transform.position-controller.transform.position,new Vector3(0,18,-10.4f))<.01f,"Original camera changed");
  controller.transform.position=new Vector3(650,.5f,-650);world.Refresh(true);Build(world);CheckLots(world);
  controller.transform.position=new Vector3(0,.5f,0);world.Refresh(true);Build(world);CheckLots(world);
  Debug.Log("URBAN_QA_PASS: authored camera buttons, saved camera mode, all views, original elevated pose, self-visibility, six uniform car scales, collider scale, parcel visuals and streaming reference counts");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};}
 static void Build(TileStreamingManager world){for(int i=0;i<30;i++)typeof(TileStreamingManager).GetMethod("BuildSomeTiles",F).Invoke(world,null);}
 static void CheckLots(TileStreamingManager world){
  var expected=new Dictionary<Vector2Int,int>();int tiles=0;
  foreach(var item in world.EnumerateSpawnedTiles()){
   tiles++;var data=item.Value.GetComponent<TileInstance>().Data;if(data.kind!=TileKind.Building)continue;
   expected.TryGetValue(data.lotOrigin,out int n);expected[data.lotOrigin]=n+1;
  }
  var actual=Field<Dictionary<Vector2Int,(GameObject visual,int users)>>(world,"buildingLots");Check(actual.Count==expected.Count,"Duplicate or missing building visuals");
  foreach(var item in actual){Check(item.Value.users==expected[item.Key] && item.Value.visual!=null,"Building unloaded while part remains visible");Check(item.Value.visual.GetComponentsInChildren<Renderer>().Length>0,"Empty building visual");}
  Check(tiles==441,"Streaming tile count");Debug.Log($"PARCEL_STREAM: {tiles} tiles, {actual.Count} distinct building visuals");
 }
 static void Capture(string name){var camera=Camera.main;foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f;}
  var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();foreach(var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))text.ForceMeshUpdate();camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();System.IO.File.WriteAllBytes("/tmp/fahrpreis-"+name+".png",image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
 }
}
