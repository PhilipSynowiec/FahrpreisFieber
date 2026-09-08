using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class HandlingQA {
 static BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool c,string m){if(!c)throw new Exception(m);}
 public static void Validate(){
 EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
 var car=UnityEngine.Object.FindFirstObjectByType<CarControllerInputSystem>();var rb=car.GetComponent<Rigidbody>();
 typeof(CarControllerInputSystem).GetMethod("Awake",F).Invoke(car,null);
 var drive=typeof(CarControllerInputSystem).GetMethod("Drive",F);
 Physics.simulationMode=SimulationMode.Script;rb.interpolation=RigidbodyInterpolation.None;
 Action<float,float> step=(s,t)=>{drive.Invoke(car,new object[]{s,t,0.02f});Physics.Simulate(.02f);};
 for(int i=0;i<100;i++)step(0,1);
 Check(rb.linearVelocity.magnitude>9 && rb.linearVelocity.magnitude<=10.01f,"Forward speed");
 var before=rb.rotation;for(int i=0;i<25;i++)step(1,1);
 Check(Quaternion.Angle(before,rb.rotation)>20,"Explicit steering blocked by constraints");
 float slip=Mathf.Abs(Vector3.Dot(rb.linearVelocity,rb.rotation*Vector3.right));Check(slip>0.01f && slip<=2.01f,"Controlled drift");
 for(int i=0;i<40;i++)step(0,1);before=rb.rotation;
 rb.AddTorque(Vector3.up*10000,ForceMode.Impulse);Physics.Simulate(.02f);
 Check(Quaternion.Angle(before,rb.rotation)<.01f,"External spin");
 for(int i=0;i<150;i++)step(0,-1);
 Check(Vector3.Dot(rb.linearVelocity,rb.rotation*Vector3.forward)<-9,"Brake to reverse");
 var interaction=car.GetComponent<TaxiInteractionController>();var slow=typeof(TaxiInteractionController).GetMethod("IsWithinSpeed",F);
 rb.linearVelocity=Vector3.forward*4;Check((bool)slow.Invoke(interaction,new object[]{4f}),"4 must be allowed");
 rb.linearVelocity=Vector3.forward*4.01f;Check(!(bool)slow.Invoke(interaction,new object[]{4f}),"Above 4 must be blocked");
 var taxi=car.transform.GetChild(0);Check(Mathf.Approximately(taxi.localScale.x,taxi.localScale.y)&&Mathf.Approximately(taxi.localScale.y,taxi.localScale.z),"Uniform taxi scale");
 Check(UnityEngine.Object.FindFirstObjectByType<CurrencyManager>()!=null,"Direct Game currency manager");
 var trips=UnityEngine.Object.FindFirstObjectByType<TripManager>();var nav=UnityEngine.Object.FindFirstObjectByType<TaxiRouteNavigation>();
 typeof(TripManager).GetField("activeJob",F).SetValue(trips,new PassengerJob{destinationWorld=new Vector3(120,0,120)});
 nav.RefreshRoute();Check(nav.Route.Count>0,"Route missing");int rev=nav.Revision;car.transform.position+=new Vector3(30,0,0);nav.RefreshRoute();Check(nav.Revision>rev,"Route stale after moving");
 Debug.Log("PASS: arcade movement, controlled drift, torque suppression, reverse, rolling interaction threshold, uniform scale, currency scene, dynamic route");
 EditorApplication.Exit(0);
 }
}
