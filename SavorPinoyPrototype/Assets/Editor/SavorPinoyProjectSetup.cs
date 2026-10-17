using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SavorPinoyProjectSetup
{
    private static readonly string[] Scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/RegionalTriviaHub.unity", "Assets/Scenes/CookingStage.unity" };

    [MenuItem("SavorPinoy/DEV-01/Validate Project Setup")]
    public static void Validate()
    {
        if (Application.unityVersion != "6000.6.3f1") throw new InvalidOperationException("Use Unity 6000.6.3f1 from ProjectVersion.txt.");
        if (EditorSettings.serializationMode != SerializationMode.ForceText) throw new InvalidOperationException("Set Asset Serialization to Force Text.");
        string[] enabled = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (!enabled.SequenceEqual(Scenes)) throw new InvalidOperationException("Enabled build scenes must be MainMenu, RegionalTriviaHub, CookingStage in that order.");
        foreach (string scene in Scenes) if (!File.Exists(scene)) throw new FileNotFoundException("Missing scene", scene);
        if (PlayerSettings.Android.minSdkVersion != AndroidSdkVersions.AndroidApiLevel26) throw new InvalidOperationException("Android minimum SDK must be 26.");
        if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64) throw new InvalidOperationException("Android architecture must be ARM64.");
        Debug.Log("DEV-01 PASS: pinned Unity, text serialization, build scene order, Android SDK 26 and ARM64.");
    }

    // Optional batch verification opens sources additively and closes without saving user scenes.
    public static void ValidateBatch()
    {
        Validate();
        foreach (string scene in Scenes)
        {
            var loaded = SceneManager.GetSceneByPath(scene);
            bool wasLoaded = loaded.IsValid() && loaded.isLoaded;
            if (!wasLoaded) loaded = EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive);
            try
            {
                foreach (var root in loaded.GetRootGameObjects())
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                            throw new InvalidOperationException("Missing script in " + scene + ": " + transform.name);
            }
            finally { if (!wasLoaded) EditorSceneManager.CloseScene(loaded, true); }
        }
        Debug.Log("DEV-01 PASS: all three source scenes open without missing scripts.");
        ValidateFirstImport();
    }

    private static void ValidateFirstImport()
    {
        string path = "Assets/Sprites/__Dev01Probe_" + Guid.NewGuid().ToString("N") + ".png";
        var texture = new Texture2D(2, 2);
        try
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single || importer.mipmapEnabled || importer.filterMode != FilterMode.Bilinear || importer.wrapMode != TextureWrapMode.Clamp || !importer.alphaIsTransparency)
                throw new InvalidOperationException("New sprite import defaults did not apply.");
            string guid = AssetDatabase.AssetPathToGUID(path);
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
            importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.filterMode != FilterMode.Point || guid != AssetDatabase.AssetPathToGUID(path))
                throw new InvalidOperationException("Reimport changed authored settings or GUID.");
            Debug.Log("DEV-01 PASS: first-import sprite defaults; authored reimport settings and GUID preserved.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.DeleteAsset(path);
        }
    }
}
