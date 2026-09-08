using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
public static class RevisionQA
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Validate()
    {
        foreach (string scene in new[] { "MainMenu", "Game" })
        {
            var loaded = EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            if (scene == "MainMenu") Capture("main-menu");
            foreach (var root in loaded.GetRootGameObjects())
                foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject) == 0, "Missing script on " + tr.name);
            foreach (var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Check(text.font != null, "Missing font on " + text.name);
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Check(button.targetGraphic != null, "Missing button graphic");
        }
        foreach (string name in new[] { "Passenger", "PassengerCommuter", "PassengerTourist" })
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + name + ".prefab");
            Check(asset != null && asset.GetComponent<PassengerMotion>() != null, "Missing motion on " + name);
            Check(asset.layer == 6, "Wrong passenger detection layer");
            Check(asset.GetComponentsInChildren<Renderer>().Length >= 14, "Incomplete passenger model");
            foreach (var renderer in asset.GetComponentsInChildren<Renderer>())
                Check(renderer.sharedMaterial != null && renderer.GetComponent<MeshFilter>().sharedMesh != null, "Missing character material/mesh");
        }
        var world = UnityEngine.Object.FindFirstObjectByType<TileStreamingManager>();
        Check(world != null, "Missing world");
        // Repeated movement before construction must replace, rather than append, pending work.
        var target = (Transform)typeof(TileStreamingManager).GetField("target", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(world);
        Vector3 original = target.position;
        world.Refresh(true);
        target.position += new Vector3(1000f, 0f, -1000f);
        world.Refresh(true);
        target.position = original;
        world.Refresh(true);
        var build = typeof(TileStreamingManager).GetMethod("BuildSomeTiles", BindingFlags.NonPublic | BindingFlags.Instance);
        for (int i = 0; i < 30; i++) build.Invoke(world, null);
        int count = 0;
        foreach (var pair in world.EnumerateSpawnedTiles())
        {
            count++;
            Check(pair.Value != null && pair.Value.GetComponent<TileInstance>() != null, "Incomplete tile");
        }
        var cache = (System.Collections.IDictionary)typeof(TileStreamingManager).GetField("tileDataCache", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(world);
        for (int i = 1000; i < 2000; i++) world.GetOrGenerateTileData(new Vector2Int(i, -i));
        Check(cache.Count == count, "Unloaded lookups leaked cached tiles");
        foreach (var pair in world.EnumerateSpawnedTiles()) Check(pair.Key.sqrMagnitude <= 144, "Stale queued tile survived relocation");
        foreach (string name in new[] { "Passenger", "PassengerCommuter", "PassengerTourist" })
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + name + ".prefab");
            var passenger = UnityEngine.Object.Instantiate(asset, target.position + Vector3.right * 2f, Quaternion.identity);
            var motion = passenger.GetComponent<PassengerMotion>();
            motion.Initialize(target);
            var update = typeof(PassengerMotion).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            update.Invoke(motion, null);
            var arm = (Transform)typeof(PassengerMotion).GetField("rightArm", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(motion);
            Check(arm.localEulerAngles.z > 100f, "Passenger failed to hail a nearby taxi");
            target.position += Vector3.right * 100f;
            update.Invoke(motion, null);
            Check(arm.localEulerAngles.z < 20f, "Passenger failed to return to idle");
            target.position = original;
            UnityEngine.Object.DestroyImmediate(passenger);
        }
        Check(count == 441, "Unexpected circular streaming count: " + count);
        CheckRevision();
        Capture("game");
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
        Camera.main.transform.position = new Vector3(85f, 140f, -60f);
        Camera.main.transform.LookAt(new Vector3(25f, 0f, 25f));
        Camera.main.orthographic = true; Camera.main.orthographicSize = 72f;
        Capture("city-overview");
        Debug.Log("CITY_QA_PASS: scenes, fonts, button bindings, character meshes/materials, 441 streamed tiles, bounded cache, relocation queue, and passenger hailing/idle transitions.");
        EditorApplication.Exit(0);
    }
    static void CheckRevision()
    {
        var hud = UnityEngine.Object.FindFirstObjectByType<TaxiHUDUI>();
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var button = (Button)typeof(TaxiHUDUI).GetField("acceptButton", flags).GetValue(hud);
        Check(button.GetComponentsInChildren<TMP_Text>(true).Length == 0, "Action button still contains text");
        var icon = (Image)typeof(TaxiHUDUI).GetField("actionIcon", flags).GetValue(hud);
        hud.SetAccept(true, true, false, null); Check(icon.sprite != null, "Missing pickup icon");
        var pickup = icon.sprite; hud.SetAccept(true, true, true, null); Check(icon.sprite != pickup, "Dropoff icon failed to switch");
        hud.SetAccept(true, true, false, null);
        var map = UnityEngine.Object.FindFirstObjectByType<TaxiMinimapGraphic>();
        Check(map != null && map.material.shader.name == "Taxi/MinimapCircle" && map.material.shader.isSupported, "Missing circular map mask");
        using (var vertices = new VertexHelper())
        {
            typeof(TaxiMinimapGraphic).GetMethod("OnPopulateMesh", flags, null, new[] { typeof(VertexHelper) }, null).Invoke(map, new object[] { vertices });
            Check(vertices.currentVertCount > 400, "Minimap does not contain streets and buildings");
        }
        var trips = UnityEngine.Object.FindFirstObjectByType<TripManager>();
        trips.StartTrip(new PassengerJob { timeLimitSeconds = 12f, destinationWorld = new Vector3(15, 0, 8) });
        typeof(TaxiHUDUI).GetMethod("Update", flags).Invoke(hud, null);
        var countdown = (TMP_Text)typeof(TaxiHUDUI).GetField("shiftStatus", flags).GetValue(hud);
        Check(countdown.text == "12s", "Countdown should contain only seconds");
        var marker = UnityEngine.Object.FindFirstObjectByType<RotatingDestinationMarker>();
        Check(marker != null && marker.GetComponentsInChildren<Collider>().Length == 0, "Marker should be a nonblocking rotating arrow");
        var toast = UnityEngine.Object.FindFirstObjectByType<ToastMessageUI>();
        toast.ShowReview("Caught my train with time for coffee. Five stars for this taxi!", 5, 45);
        var stars = (Image[])typeof(ToastMessageUI).GetField("stars", flags).GetValue(toast);
        Check(stars.Length == 5 && stars[0].color == stars[4].color, "On-time rating should show five stars");
        toast.ShowReview("My coffee retired during this ride.", 1, 0);
        Check(stars[0].color != stars[1].color, "Late rating should show one star");
        toast.ShowReview("Caught my train with time for coffee. Five stars for this taxi!", 5, 45);
        var review = (TMP_Text)typeof(ToastMessageUI).GetField("text", flags).GetValue(toast);
        Check(review.horizontalAlignment == HorizontalAlignmentOptions.Left, "Review is not left aligned");
        var offer = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/OfferWidget.prefab");
        Check(offer.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay, "Offer is still occluded by world geometry");
        var passengerAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Passenger.prefab");
        var passenger = UnityEngine.Object.Instantiate(passengerAsset, new Vector3(8f, .16f, 5f), Quaternion.identity);
        var shownOffer = UnityEngine.Object.Instantiate(offer).GetComponent<PassengerOfferWidget>();
        shownOffer.Bind(passenger.transform, new PassengerJob { distanceWorld = 120, timeLimitSeconds = 30, quotedPriceCoins = 45 });
        Canvas.ForceUpdateCanvases();
        typeof(PassengerOfferWidget).GetMethod("LateUpdate", flags).Invoke(shownOffer, null);
        var taxiAsset = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cars/CityCab.prefab"));
        Bounds taxiBounds = taxiAsset.GetComponentsInChildren<Renderer>()[0].bounds;
        foreach (var renderer in taxiAsset.GetComponentsInChildren<Renderer>()) taxiBounds.Encapsulate(renderer.bounds);
        Bounds passengerBounds = passenger.GetComponentsInChildren<Renderer>()[0].bounds;
        foreach (var renderer in passenger.GetComponentsInChildren<Renderer>()) passengerBounds.Encapsulate(renderer.bounds);
        Check(taxiBounds.size.z > 2.5f && taxiBounds.size.z < 3f && passengerBounds.size.y < 1.5f, "Requested mixed scaling was not applied");
        Debug.Log("REVISION_SCALE: taxi=" + taxiBounds.size + " passenger=" + passengerBounds.size);
        UnityEngine.Object.DestroyImmediate(taxiAsset);
        Debug.Log("REVISION_QA_PASS: icons, numeric countdown, minimap geometry/mask, rotating arrow, app review ratings, overlay offer and mixed scaling.");
    }

    static void Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var camera = Camera.main;
        if (camera == null) return;
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
        }
        var rt = new RenderTexture(1280, 720, 24);
        camera.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        foreach (var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) text.ForceMeshUpdate();
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
        System.IO.File.WriteAllBytes("/tmp/fahrpreis-" + name + ".png", image.EncodeToPNG());
        RenderTexture.active = previous;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(rt);
    }
}
