using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ValidationEntry
{
    static bool spawned;
    static ValidationEntry(){EditorApplication.update+=Poll;}
    public static void Run()
    {
        File.WriteAllText("validation.request","run");EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.EnterPlaymode();
    }
    static void Poll()
    {
        if(spawned || !EditorApplication.isPlaying || !File.Exists("validation.request"))return;spawned=true;
        var runner=new GameObject("ValidationRunner").AddComponent<ValidationRunner>();runner.Begin(Path.GetFullPath("../../activity5/tests"));
    }
}
