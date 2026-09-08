using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
public static class AuthorRefinement
{
 public static void Build(){
 var catalog=AssetDatabase.LoadAssetAtPath<CarCatalog>("Assets/Prefabs/Cars/CarCatalog.asset");int[] prices={0,2000,7000,22000,65000,180000};
 for(int i=0;i<6;i++)catalog.cars[i].price=prices[i];EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
 var menu=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");var panel=GameObject.Find("Canvas").transform.Find("CarStore");
 for(int i=1;i<6;i++)panel.Find(catalog.cars[i].displayName+"/Purchase/Label").GetComponent<TMP_Text>().text=prices[i].ToString("N0");EditorSceneManager.SaveScene(menu);
 var game=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");var controller=Object.FindFirstObjectByType<CarControllerInputSystem>();var so=new SerializedObject(controller);so.FindProperty("steerResponse").floatValue=4;so.FindProperty("wheelbase").floatValue=1.8f;so.FindProperty("maxSteeringAngle").floatValue=30;so.FindProperty("maxLateralAcceleration").floatValue=96;so.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.SaveScene(game);
 Debug.Log("REFINEMENT_AUTHOR_PASS");EditorApplication.Exit(0);
 }
}
