using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Activity 8: builds a versioned, traceable Windows release from the current checkout.
// Output: <repo>/Builds/SavorPinoy-v<version>/ and <repo>/Builds/SavorPinoy-v<version>-win64.zip
public static class SavorPinoyReleaseBuilder
{
    [MenuItem("SavorPinoy/Release/Build Windows Release")]
    public static void BuildWindowsRelease()
    {
        string version = PlayerSettings.bundleVersion;
        string projectDir = Directory.GetParent(Application.dataPath).FullName;
        string repoDir = Directory.GetParent(projectDir).FullName;

        bool gitOk = Git(repoDir, "--version").StartsWith("git version");
        string commit = gitOk ? Git(repoDir, "rev-parse HEAD") : ReadCommitFromGitFolder(repoDir);
        string branch = gitOk ? Git(repoDir, "rev-parse --abbrev-ref HEAD") : ReadBranchFromGitFolder(repoDir);
        string tags = gitOk ? Git(repoDir, "tag --points-at HEAD") : "(git not on PATH; check in GitHub Desktop)";
        string dirty = gitOk ? Git(repoDir, "status --porcelain --untracked-files=no") : "";
        if (!gitOk)
            Debug.LogWarning("git is not on PATH: commit read from .git folder; uncommitted-change check skipped. Confirm GitHub Desktop shows no changes.");
        if (!string.IsNullOrEmpty(dirty) &&
            !EditorUtility.DisplayDialog("Uncommitted changes",
                "The working tree has uncommitted changes, so this build will not match a commit exactly.\n\nCommit first for a traceable release.",
                "Build anyway", "Cancel"))
            return;

        string name = "SavorPinoy-v" + version;
        string buildsDir = Path.Combine(repoDir, "Builds");
        string outDir = Path.Combine(buildsDir, name);
        string zipPath = Path.Combine(buildsDir, name + "-win64.zip");
        if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        if (File.Exists(zipPath)) File.Delete(zipPath);
        Directory.CreateDirectory(outDir);

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outDir, PlayerSettings.productName + ".exe"),
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("RELEASE FAILED: " + report.summary.result + " with " + report.summary.totalErrors + " error(s).");
            return;
        }

        foreach (string dir in Directory.GetDirectories(outDir, "*_DoNotShip"))
            Directory.Delete(dir, true);

        string notesDir = Path.Combine(repoDir, "Docs", "Release", "v" + version);
        foreach (string file in new[] { "README.txt", "RELEASE_NOTES.txt" })
        {
            string src = Path.Combine(notesDir, file);
            if (File.Exists(src)) File.Copy(src, Path.Combine(outDir, file), true);
            else Debug.LogWarning("Missing " + src);
        }

        var info = new StringBuilder();
        info.AppendLine("Project: SAVORPINOY");
        info.AppendLine("Version: " + version);
        info.AppendLine("Platform: Windows x86_64");
        info.AppendLine("Commit: " + commit);
        info.AppendLine("Branch: " + branch);
        info.AppendLine("Tags at commit: " + (string.IsNullOrEmpty(tags) ? "(none yet)" : tags.Replace("\n", ", ")));
        info.AppendLine("Uncommitted changes at build time: " + (!gitOk ? "not checked" : string.IsNullOrEmpty(dirty) ? "no" : "YES"));
        info.AppendLine("Built (UTC): " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        info.AppendLine("Built on: " + Environment.MachineName);
        info.AppendLine("Unity: " + Application.unityVersion);
        info.AppendLine("Scenes: " + string.Join(", ", scenes));
        File.WriteAllText(Path.Combine(outDir, "BUILD_INFO.txt"), info.ToString());

        ZipFile.CreateFromDirectory(outDir, zipPath, System.IO.Compression.CompressionLevel.Optimal, true);
        long bytes = new FileInfo(zipPath).Length;
        Debug.Log("RELEASE OK: " + zipPath + " (" + (bytes / 1048576.0).ToString("0.0") + " MB), commit " + commit);
        EditorUtility.RevealInFinder(zipPath);
    }

    private static string ReadBranchFromGitFolder(string repoDir)
    {
        string head = File.ReadAllText(Path.Combine(repoDir, ".git", "HEAD")).Trim();
        return head.StartsWith("ref: refs/heads/") ? head.Substring("ref: refs/heads/".Length) : "(detached)";
    }

    private static string ReadCommitFromGitFolder(string repoDir)
    {
        try
        {
            string gitDir = Path.Combine(repoDir, ".git");
            string head = File.ReadAllText(Path.Combine(gitDir, "HEAD")).Trim();
            if (!head.StartsWith("ref: ")) return head;
            string refName = head.Substring(5);
            string refFile = Path.Combine(gitDir, refName.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(refFile)) return File.ReadAllText(refFile).Trim();
            string packed = Path.Combine(gitDir, "packed-refs");
            if (File.Exists(packed))
                foreach (string line in File.ReadAllLines(packed))
                    if (line.EndsWith(" " + refName)) return line.Split(' ')[0];
            return "unknown";
        }
        catch (Exception e)
        {
            return "unknown (" + e.Message + ")";
        }
    }

    private static string Git(string workDir, string args)
    {
        try
        {
            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = workDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                return output;
            }
        }
        catch (Exception e)
        {
            return "unknown (" + e.Message + ")";
        }
    }
}
