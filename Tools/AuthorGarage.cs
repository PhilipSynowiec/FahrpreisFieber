using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
public static class AuthorGarage
{
 static Color Navy=new Color(.055f,.09f,.14f), Yellow=new Color(1,.76f,.16f);
 static string Dir="Assets/Prefabs/Cars";
 static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/City/"+name+".mat");
 static void Ref(UnityEngine.Object o,string key,UnityEngine.Object value){var s=new SerializedObject(o);s.FindProperty(key).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static void Refs(UnityEngine.Object o,string key,UnityEngine.Object[] values){var s=new SerializedObject(o);var a=s.FindProperty(key);a.arraySize=values.Length;for(int i=0;i<values.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=values[i];s.ApplyModifiedPropertiesWithoutUndo();}
 static Transform Node(string name,Transform p,Vector3 pos){var g=new GameObject(name);g.transform.SetParent(p,false);g.transform.localPosition=pos;return g.transform;}
 static GameObject Box(string name,Transform p,Vector3 pos,Vector3 size,string mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=Mat(mat);return g;}
 static GameObject MakeCar(int i,string name){
  var root=new GameObject(name);var body=Node("Body",root.transform,Vector3.zero);
  string paint=new[]{"Yellow","Teal","Cream","Blue","Coral","White"}[i];
  float length=new[]{3.3f,2.8f,3.6f,3.6f,3.5f,3.4f}[i];
  float roof=new[]{1.35f,1.4f,1.4f,1.5f,1.15f,1.05f}[i];
  Box("Chassis",body,new Vector3(0,.40f,0),new Vector3(1.45f,.28f,length),"Hair");
  Box("Body paint",body,new Vector3(0,.65f,0),new Vector3(1.6f,.45f,length),paint);
  float cabinZ=i==3?.35f:-.2f;float cabinLength=i==2?2.25f:i==3?1.3f:1.65f;
  Box("Glass cabin",body,new Vector3(0,(roof+.8f)/2,cabinZ),new Vector3(1.30f,roof-.8f,cabinLength),"Glass");
  Box("Roof",body,new Vector3(0,roof,cabinZ),new Vector3(1.43f,.12f,cabinLength+.08f),i==2?"Coral":paint);
  foreach(float x in new[]{-.69f,.69f})foreach(float z in new[]{cabinZ-cabinLength/2,cabinZ+cabinLength/2})Box("Window pillar",body,new Vector3(x,(roof+.8f)/2,z),new Vector3(.10f,roof-.8f,.10f),paint);
  foreach(float side in new[]{-1f,1f}){
   Box("Bumper",body,new Vector3(0,.43f,side*(length/2+.025f)),new Vector3(1.48f,.14f,.13f),"White");
   foreach(float x in new[]{-.53f,.53f})Box(side>0?"Headlight":"Tail light",body,new Vector3(x,.69f,side*(length/2+.02f)),new Vector3(.32f,.18f,.06f),side>0?"Cream":"Coral");
   Box("Mirror",body,new Vector3(side*.87f,1f,cabinZ+.4f),new Vector3(.20f,.13f,.22f),paint);
  }
  if(i==0){Box("Taxi sign",body,new Vector3(0,roof+.18f,0),new Vector3(.65f,.24f,.34f),"Yellow");for(int k=0;k<5;k++)foreach(float side in new[]{-1f,1f})Box("Checker",body,new Vector3(side*.805f,.7f,-.6f+k*.25f),new Vector3(.02f,.15f,.14f),"Hair");}
  if(i==2){foreach(float x in new[]{-.55f,.55f})Box("Roof rail",body,new Vector3(x,roof+.14f,-.2f),new Vector3(.08f,.12f,1.8f),"Hair");}
  if(i==3){Box("Open bed",body,new Vector3(0,.9f,-1.05f),new Vector3(1.25f,.04f,1.35f),"Hair");foreach(float x in new[]{-.72f,.72f})Box("Bed side",body,new Vector3(x,1f,-1.05f),new Vector3(.16f,.3f,1.45f),paint);Box("Tailgate",body,new Vector3(0,1f,-1.70f),new Vector3(1.5f,.3f,.12f),paint);}
  if(i==4||i==5){foreach(float x in new[]{-.25f,.25f}){Box("Hood stripe",body,new Vector3(x,.883f,1.05f),new Vector3(.20f,.015f,.95f),i==4?"Hair":"Teal");Box("Roof stripe",body,new Vector3(x,roof+.068f,cabinZ),new Vector3(.20f,.015f,cabinLength),i==4?"Hair":"Teal");}if(i==5)Box("Rear spoiler",body,new Vector3(0,1.03f,-1.45f),new Vector3(1.7f,.12f,.3f),"Teal");}
  var wheels=new Transform[4];int w=0;
  foreach(float x in new[]{-.80f,.80f})foreach(float z in new[]{-length*.30f,length*.30f}){
   var pivot=Node("Wheel "+w,body,new Vector3(x,.32f,z));wheels[w++]=pivot;
   var tire=GameObject.CreatePrimitive(PrimitiveType.Cylinder);tire.name="Tire";tire.transform.SetParent(pivot,false);tire.transform.localRotation=Quaternion.Euler(0,0,90);tire.transform.localScale=new Vector3(.64f,.13f,.64f);UnityEngine.Object.DestroyImmediate(tire.GetComponent<Collider>());tire.GetComponent<Renderer>().sharedMaterial=Mat("Hair");
   Box("Hub",pivot,new Vector3(Mathf.Sign(x)*.14f,0,0),new Vector3(.035f,.24f,.24f),"White");
  }
  var motion=root.AddComponent<CarVisualMotion>();Ref(motion,"body",body);Refs(motion,"wheels",wheels);
  return root;
 }
 static Sprite Thumbnail(GameObject prefab,string id){
  var scene=EditorSceneManager.NewPreviewScene();var g=UnityEngine.Object.Instantiate(prefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g,scene);
  var cameraGO=new GameObject("ThumbnailCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGO,scene);var c=cameraGO.AddComponent<Camera>();c.scene=scene;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.055f,.09f,.14f,0);c.orthographic=true;c.orthographicSize=2.4f;c.transform.position=new Vector3(4,3.2f,5);c.transform.LookAt(new Vector3(0,.65f,0));
  var l=new GameObject("Light");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(l,scene);var light=l.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(45,-30,0);
  var rt=new RenderTexture(512,320,24);c.targetTexture=rt;c.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(512,320,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,512,320),0,0);tex.Apply();string path="Assets/Art/UI/Cars/"+id+".png";File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=previous;c.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);EditorSceneManager.ClosePreviewScene(scene);
  AssetDatabase.ImportAsset(path);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
 }
 static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/"+name+".png");
 static TMP_FontAsset Font=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset");
 static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size){var g=new GameObject(name,typeof(RectTransform));g.layer=5;var r=(RectTransform)g.transform;r.SetParent(parent,false);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
 static Image Image(string name,Transform parent,Vector2 pos,Vector2 size,Color color,Sprite sprite=null){var r=Rect(name,parent,pos,size);var im=r.gameObject.AddComponent<Image>();im.color=color;im.sprite=sprite;im.raycastTarget=false;return im;}
 static TMP_Text Text(string name,Transform parent,string value,Vector2 pos,Vector2 size,float fontSize,Color color){var r=Rect(name,parent,pos,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=Font;t.text=value;t.fontSize=fontSize;t.color=color;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
 static Button Button(string name,Transform parent,Vector2 pos,Vector2 size,string label,out TMP_Text text){var im=Image(name,parent,pos,size,Yellow,Sprite("SquareButton"));im.type=UnityEngine.UI.Image.Type.Sliced;im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;var colors=b.colors;colors.disabledColor=new Color(.65f,.65f,.65f,.85f);b.colors=colors;text=Text("Label",im.transform,label,Vector2.zero,size,24,Navy);return b;}
 static void Models(Transform parent,CarCatalog catalog,CarControllerInputSystem controller){
  foreach(Transform child in parent.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
  var models=new GameObject[6];for(int i=0;i<6;i++){models[i]=(GameObject)PrefabUtility.InstantiatePrefab(catalog.cars[i].prefab,parent);models[i].transform.localPosition=controller==null?Vector3.zero:Vector3.down*.5f;models[i].SetActive(i==0);}
  var view=parent.gameObject.AddComponent<CarSelectionView>();Ref(view,"catalog",catalog);Refs(view,"models",models);Ref(view,"controller",controller);
 }
 public static void Polish(){
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");var panel=(RectTransform)GameObject.Find("Canvas").transform.Find("CarStore");panel.anchorMin=Vector2.zero;panel.anchorMax=Vector2.one;panel.sizeDelta=Vector2.zero;panel.anchoredPosition=Vector2.zero;EditorSceneManager.SaveScene(scene);EditorApplication.Exit(0);
 }
 public static void Finish(){
 foreach(var item in new[]{("Mint",new Color(.40f,.82f,.64f)),("CarRed",new Color(.84f,.12f,.12f)),("Orange",new Color(1f,.48f,.12f))}){
 string path="Assets/Art/Materials/City/"+item.Item1+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Mat("White"));mat.name=item.Item1;AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",item.Item2);mat.SetColor("_Color",item.Item2);EditorUtility.SetDirty(mat);
 }
 foreach(var item in new[]{("Mini","Teal","Mint"),("Muscle","Coral","CarRed"),("Wagon","Coral","Orange")}){string path=Dir+"/"+item.Item1+".prefab";var root=PrefabUtility.LoadPrefabContents(path);foreach(var r in root.GetComponentsInChildren<Renderer>())if(r.sharedMaterial==Mat(item.Item2))r.sharedMaterial=Mat(item.Item3);PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);}
 var catalog=AssetDatabase.LoadAssetAtPath<CarCatalog>(Dir+"/CarCatalog.asset");foreach(var car in catalog.cars)car.thumbnail=Thumbnail(car.prefab,car.id);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
 foreach(string name in new[]{"MainMenu","Game"}){var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");catalog=AssetDatabase.LoadAssetAtPath<CarCatalog>(Dir+"/CarCatalog.asset");foreach(var view in UnityEngine.Object.FindObjectsByType<CarSelectionView>(FindObjectsSortMode.None))Ref(view,"catalog",catalog);var store=UnityEngine.Object.FindFirstObjectByType<CarStoreUI>();if(store!=null){Ref(store,"catalog",catalog);var panel=GameObject.Find("Canvas").transform.Find("CarStore");foreach(var car in catalog.cars)panel.Find(car.displayName+"/Car").GetComponent<Image>().sprite=car.thumbnail;}EditorSceneManager.SaveScene(scene);}
 AssetDatabase.SaveAssets();EditorApplication.Exit(0);
 }
 public static void FixThumbnails(){
 var catalog=AssetDatabase.LoadAssetAtPath<CarCatalog>(Dir+"/CarCatalog.asset");
 foreach(var car in catalog.cars){string path="Assets/Art/UI/Cars/"+car.id+".png";var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.spriteImportMode=SpriteImportMode.Single;imp.SaveAndReimport();car.thumbnail=AssetDatabase.LoadAssetAtPath<Sprite>(path);}
 EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");var store=GameObject.Find("Canvas").transform.Find("CarStore");foreach(var car in catalog.cars)store.Find(car.displayName+"/Car").GetComponent<Image>().sprite=car.thumbnail;
 EditorSceneManager.SaveScene(scene);EditorApplication.Exit(0);
 }
 public static void Build(){
  if(AssetDatabase.LoadAssetAtPath<CarCatalog>(Dir+"/CarCatalog.asset")!=null)throw new InvalidOperationException("Garage already authored. Edit existing assets directly.");
  Directory.CreateDirectory(Dir);Directory.CreateDirectory("Assets/Art/UI/Cars");AssetDatabase.Refresh();
  string[] ids={"city-cab","mini","wagon","pickup","muscle","sport"};string[] names={"City Cab","Mini","Wagon","Pickup","Muscle","Sport"};int[] prices={0,2000,7000,22000,65000,180000};
  var catalog=ScriptableObject.CreateInstance<CarCatalog>();catalog.cars=new CarCatalog.Entry[6];
  for(int i=0;i<6;i++){var g=MakeCar(i,names[i]);var prefab=PrefabUtility.SaveAsPrefabAsset(g,Dir+"/"+names[i].Replace(" ","")+".prefab");UnityEngine.Object.DestroyImmediate(g);catalog.cars[i]=new CarCatalog.Entry{id=ids[i],displayName=names[i],price=prices[i],topSpeed=10+i*2,prefab=prefab,thumbnail=Thumbnail(prefab,ids[i])};}
  AssetDatabase.CreateAsset(catalog,Dir+"/CarCatalog.asset");AssetDatabase.SaveAssets();
  var menu=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");catalog=AssetDatabase.LoadAssetAtPath<CarCatalog>(Dir+"/CarCatalog.asset");var preview=GameObject.Find("CarPreview");Models(preview.transform,catalog,null);
  var canvas=GameObject.Find("Canvas").transform;
  // Project the displayed car center into canvas coordinates so the hit area follows its existing placement.
  var openImage=Image("OpenCarStore",canvas,new Vector2(310,-25),new Vector2(410,340),new Color(1,1,1,0));openImage.raycastTarget=true;var open=openImage.gameObject.AddComponent<Button>();open.targetGraphic=openImage;open.transition=Selectable.Transition.None;
  var panel=Image("CarStore",canvas,Vector2.zero,new Vector2(1120,650),Navy,Sprite("SquareButton"));panel.raycastTarget=true;panel.type=UnityEngine.UI.Image.Type.Sliced;
  Text("Title",panel.transform,"GARAGE",new Vector2(-410,280),new Vector2(240,50),40,Yellow);
  var balance=Text("Balance",panel.transform,"0",new Vector2(315,280),new Vector2(170,45),28,Yellow);var binder=balance.gameObject.AddComponent<CoinsTextBinder>();Ref(binder,"label",balance);Image("Coin",panel.transform,new Vector2(210,280),new Vector2(26,26),Yellow,Sprite("Coin"));
  var close=Button("Close",panel.transform,new Vector2(480,280),new Vector2(55,45),"X",out _);
  var buttons=new Button[6];var labels=new TMP_Text[6];
  for(int i=0;i<6;i++){
   float x=(i%3-1)*352;float y=i<3?112:-148;
   var card=Image(names[i],panel.transform,new Vector2(x,y),new Vector2(328,244),new Color(.12f,.19f,.25f),Sprite("SquareButton"));card.type=UnityEngine.UI.Image.Type.Sliced;
   Text("Name",card.transform,names[i].ToUpperInvariant(),new Vector2(0,96),new Vector2(310,36),28,Yellow);
   var thumb=Image("Car",card.transform,new Vector2(-30,12),new Vector2(232,132),Color.white,catalog.cars[i].thumbnail);thumb.preserveAspect=true;
   Text("Speed",card.transform,(10+i*2)+"\nSPEED",new Vector2(115,20),new Vector2(75,64),22,Color.white);
   buttons[i]=Button("Purchase",card.transform,new Vector2(0,-83),new Vector2(278,44),i==0?"SELECTED":prices[i].ToString("N0"),out labels[i]);
  }
  var store=canvas.gameObject.AddComponent<CarStoreUI>();Ref(store,"catalog",catalog);Ref(store,"panel",panel.gameObject);Ref(store,"openButton",open);Ref(store,"closeButton",close);Refs(store,"purchaseButtons",buttons);Refs(store,"actionLabels",labels);
  panel.gameObject.SetActive(false);EditorSceneManager.SaveScene(menu);
  var game=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");catalog=AssetDatabase.LoadAssetAtPath<CarCatalog>(Dir+"/CarCatalog.asset");var controller=UnityEngine.Object.FindFirstObjectByType<CarControllerInputSystem>();Ref(controller,"centerOfMass",null);
  Models(controller.transform,catalog,controller);EditorSceneManager.SaveScene(game);AssetDatabase.SaveAssets();Debug.Log("GARAGE_AUTHOR_PASS");EditorApplication.Exit(0);
 }
}
