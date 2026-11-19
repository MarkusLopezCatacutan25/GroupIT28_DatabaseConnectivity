using UnityEngine;

// Editor-only hook for SavorPinoy > DEV-01 > Test Kitchen: picks the dish before the cooking scene loads.
public static class DevKitchenShortcut
{
    public const string Key="SavorPinoy.TestDish";
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        string id=UnityEditor.EditorPrefs.GetString(Key,"");if(string.IsNullOrEmpty(id))return;
        UnityEditor.EditorPrefs.DeleteKey(Key);GameSessionState.SelectedDishId=id;GameSessionState.SelectedProvince=RecipePlan.ById(id).Province;
    }
#endif
}
