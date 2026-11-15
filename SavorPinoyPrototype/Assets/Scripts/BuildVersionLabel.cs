using UnityEngine;
using UnityEngine.SceneManagement;

// Shows "Version x.y.z" (Player Settings > Version) in the bottom-right corner of the main menu.
// Display only: it does not take input or change gameplay.
public sealed class BuildVersionLabel : MonoBehaviour
{
    private GUIStyle style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var host = new GameObject("BuildVersionLabel");
        DontDestroyOnLoad(host);
        host.AddComponent<BuildVersionLabel>();
    }

    private void OnGUI()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu") return;
        if (style == null)
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerRight, fontSize = Mathf.Max(14, Screen.height / 50) };
        var area = new Rect(0, 0, Screen.width - 16, Screen.height - 10);
        string text = "Version " + Application.version;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
        GUI.Label(new Rect(area.x + 1, area.y + 1, area.width, area.height), text, style);
        style.normal.textColor = Color.white;
        GUI.Label(area, text, style);
    }
}
