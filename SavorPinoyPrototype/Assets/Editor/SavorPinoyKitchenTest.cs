using UnityEditor;
using UnityEditor.SceneManagement;

// Developer shortcut: play the cooking stage directly with a chosen dish (editor only).
public static class SavorPinoyKitchenTest
{
    private static void Play(string id)
    {
        if(EditorApplication.isPlaying)return;
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        EditorPrefs.SetString(DevKitchenShortcut.Key,id);
        EditorSceneManager.OpenScene("Assets/Scenes/CookingStage.unity");EditorApplication.isPlaying=true;
    }
    [MenuItem("SavorPinoy/DEV-01/Test Kitchen/Lomi %#&F5")] public static void Lomi()=>Play(RecipePlan.Lomi.Id);
    [MenuItem("SavorPinoy/DEV-01/Test Kitchen/Adobo sa Dilaw %#&F6")] public static void Adobo()=>Play(RecipePlan.Batangas.Id);
    [MenuItem("SavorPinoy/DEV-01/Test Kitchen/Pancit Habhab %#&F7")] public static void Habhab()=>Play(RecipePlan.Quezon.Id);
    [MenuItem("SavorPinoy/DEV-01/Test Kitchen/Hardinera %#&F8")] public static void Hardinera()=>Play(RecipePlan.Hardinera.Id);
}
