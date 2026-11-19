using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds a sideloadable APK for pilot testing: <repo>/Builds/SavorPinoy-v<version>-android.apk
public static class SavorPinoyAndroidBuilder
{
    [MenuItem("SavorPinoy/Release/Build Android APK %#&F9")]
    public static void BuildAndroidApk()
    {
        string version = PlayerSettings.bundleVersion;
        string repoDir = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string buildsDir = Path.Combine(repoDir, "Builds");
        Directory.CreateDirectory(buildsDir);
        string apkPath = Path.Combine(buildsDir, "SavorPinoy-v" + version + "-android.apk");

        // The UI is laid out for 1920x1080, so lock to landscape (either side).
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.bundleVersionCode = 3;
        PlayerSettings.Android.useCustomKeystore = false; // debug-signed APK, fine for sideloading
        EditorUserBuildSettings.buildAppBundle = false;

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("ANDROID BUILD FAILED: " + report.summary.result + " with " + report.summary.totalErrors + " error(s).");
            return;
        }
        Debug.Log("ANDROID BUILD OK: " + apkPath + " (" + (new FileInfo(apkPath).Length / 1048576) + " MB)");
        EditorUtility.RevealInFinder(apkPath);
    }
}
