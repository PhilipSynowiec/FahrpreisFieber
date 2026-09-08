using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class RefinementQA
{
 static BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 public static void Validate(){try{
  float slow=Circle(4,.02f,1),normal=Circle(10,.02f,1),fast=Circle(20,.02f,1),fine=Circle(10,.01f,1),left=Circle(10,.02f,-1);
  Check(normal>=slow*.95f&&normal<fast,"Fast turns should widen modestly");Check(normal<3.7f&&fast<5f,"Arcade turns too wide for city streets");Check(Mathf.Abs(normal-fine)/normal<.05f,"Curve depends on physics step");Check(Mathf.Abs(normal-left)/normal<.02f,"Left/right asymmetric");
  var catalog=AssetDatabase.LoadAssetAtPath<CarCatalog>("Assets/Prefabs/Cars/CarCatalog.asset");int[] expected={0,2000,7000,22000,65000,180000};for(int i=0;i<6;i++)Check(catalog.cars[i].price==expected[i],"Wrong approved price");
  EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");var world=UnityEngine.Object.FindFirstObjectByType<TileStreamingManager>();world.Refresh(true);for(int i=0;i<30;i++)typeof(TileStreamingManager).GetMethod("BuildSomeTiles",F).Invoke(world,null);
  var lots=(Dictionary<Vector2Int,(GameObject visual,int users)>)typeof(TileStreamingManager).GetField("buildingLots",F).GetValue(world);int irregular=0;
  foreach(var item in lots){Check(CityLots.TryGetLot(world.WorldSeed,item.Key.x,item.Key.y,out var lot),"Missing parcel anchor");var mesh=item.Value.visual.GetComponent<MeshFilter>().sharedMesh;Check(mesh!=null&&mesh.vertexCount>0,"Empty exterior mesh");Check(item.Value.visual.GetComponent<MeshRenderer>().sharedMaterials.Length==4,"Missing facade materials");
   int count=0;for(int y=lot.y;y<lot.y+lot.depth;y++)for(int x=lot.x;x<lot.x+lot.width;x++)if(lot.Contains(x,y))count++;if(count<lot.width*lot.depth)irregular++;Check(item.Value.visual.GetComponents<BoxCollider>().Length==count,"Collider does not match footprint");
   foreach(var vertex in mesh.vertices)Check(!float.IsNaN(vertex.x)&&!float.IsInfinity(vertex.x),"Invalid mesh coordinates");
   foreach(var n in mesh.normals)Check(Mathf.Abs(n.magnitude-1)<.001f,"Invalid surface normal");
  }
  Check(irregular>0,"No nonrectangular buildings in view");Debug.Log($"REFINEMENT_QA_PASS: held-input circular arcs, speed-dependent radii, timestep consistency, mirrored steering, approved prices, {irregular} nonrectangular meshes with matching colliders");
  UrbanQA.Validate();
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void ValidateSteering(){try{
  float normal=Circle(10,.02f,1),fast=Circle(20,.02f,1),fine=Circle(10,.01f,1),left=Circle(10,.02f,-1);
  Check(normal<3.7f&&fast<5f,"Arcade turns too wide for city streets");
  Check(Mathf.Abs(normal-fine)/normal<.05f,"Timestep inconsistency");
  Check(Mathf.Abs(normal-left)/normal<.02f,"Left/right asymmetry");
  EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
  var car=UnityEngine.Object.FindFirstObjectByType<CarControllerInputSystem>();
  Check(new SerializedObject(car).FindProperty("maxLateralAcceleration").floatValue==96f,"Scene steering tuning stale");
  Debug.Log("STEERING_QA_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static float Circle(float speed,float dt,float input){
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var physics=scene.GetPhysicsScene();var previousMode=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;var go=new GameObject("CurveCar");var rb=go.AddComponent<Rigidbody>();var controller=go.AddComponent<CarControllerInputSystem>();typeof(CarControllerInputSystem).GetMethod("Awake",F).Invoke(controller,null);rb.interpolation=RigidbodyInterpolation.None;controller.SetTopSpeed(speed);var drive=typeof(CarControllerInputSystem).GetMethod("Drive",F);
  Action<float> step=steer=>{drive.Invoke(controller,new object[]{steer,1f,dt});physics.Simulate(dt);};
  for(int i=0;i<(int)(3/dt);i++)step(0);for(int i=0;i<(int)(3/dt);i++)step(input);
  var points=new List<Vector2>();for(int i=0;i<(int)(8/dt);i++){step(input);points.Add(new Vector2(rb.position.x,rb.position.z));}
  var a=points[0];var b=points[points.Count/3];var c=points[points.Count*2/3];float determinant=2*(a.x*(b.y-c.y)+b.x*(c.y-a.y)+c.x*(a.y-b.y));Check(Mathf.Abs(determinant)>.001f,"No curved trajectory");
  Vector2 center=new Vector2((a.sqrMagnitude*(b.y-c.y)+b.sqrMagnitude*(c.y-a.y)+c.sqrMagnitude*(a.y-b.y))/determinant,(a.sqrMagnitude*(c.x-b.x)+b.sqrMagnitude*(a.x-c.x)+c.sqrMagnitude*(b.x-a.x))/determinant);
  float radius=Vector2.Distance(center,a),maxError=0;foreach(var p in points)maxError=Mathf.Max(maxError,Mathf.Abs(Vector2.Distance(center,p)-radius));Check(maxError/radius<.04f,"Held steering does not produce a stable circle");
  Debug.Log($"CURVE: speed={speed}, dt={dt}, input={input}, radius={radius:F3}, radial error={maxError:F4}");
  UnityEngine.Object.DestroyImmediate(go);Physics.simulationMode=previousMode;return radius;
 }
}
