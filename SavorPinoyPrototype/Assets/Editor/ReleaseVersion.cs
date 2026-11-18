using UnityEditor;

// Single source of truth for the release version shown on the main menu (BuildVersionLabel).
// To release a new version, change Version here; the editor updates Player Settings on the next compile.
[InitializeOnLoad]
public static class ReleaseVersion
{
    public const string Version = "0.2.0";
    static ReleaseVersion()
    {
        if (PlayerSettings.bundleVersion == Version) return;
        PlayerSettings.bundleVersion = Version;
        AssetDatabase.SaveAssets();
    }
}
