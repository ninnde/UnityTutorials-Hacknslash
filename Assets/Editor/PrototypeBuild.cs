using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

// A build runs only on an explicit menu action or a request file in Temp.
[InitializeOnLoad]
public static class PrototypeBuild
{
    private const string Request = "Temp/PrototypeBuild.request";
    private const string Status = "Temp/PrototypeBuild.status";
    private const string Output = "Builds/EjecutableHacknslash/Ejecutable Hacknslash.exe";

    static PrototypeBuild() { EditorApplication.update += CheckRequest; }

    [MenuItem("Prototype/Build Windows Prototype")]
    public static void RequestBuild()
    {
        Directory.CreateDirectory("Temp");
        File.WriteAllText(Request, "Windows64");
    }

    private static void CheckRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
            return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.WriteAllText(Status, "WAITING: Stop Play mode to build.");
            return;
        }
        File.Delete(Request);
        BuildWindows();
    }

    public static void BuildWindows()
    {
        Directory.CreateDirectory("Temp");
        File.WriteAllText(Status, "BUILDING: Windows x64 and Addressables");
        string previousName = PlayerSettings.productName;
        int previousWidth = PlayerSettings.defaultScreenWidth;
        int previousHeight = PlayerSettings.defaultScreenHeight;
        FullScreenMode previousMode = PlayerSettings.fullScreenMode;
        try
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new Exception("Windows x64 build support is not installed.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            {
                File.WriteAllText(Request, "Windows64");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                return;
            }
            PlayerSettings.productName = "Prototipo Hack and Slash";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            AddressableAssetSettings.BuildPlayerContent(out var content);
            if (!string.IsNullOrEmpty(content.Error)) throw new Exception(content.Error);
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/PrototypeArena.unity" },
                locationPathName = Output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"Build failed: {report.summary.result}, {report.summary.totalErrors} errors");
            File.WriteAllText(Status, $"SUCCESS: {Path.GetFullPath(Output)}\nBytes: {report.summary.totalSize}\nWarnings: {report.summary.totalWarnings}");
        }
        catch (Exception exception)
        {
            File.WriteAllText(Status, "FAILED: " + exception);
            Debug.LogException(exception);
        }
        finally
        {
            PlayerSettings.productName = previousName;
            PlayerSettings.defaultScreenWidth = previousWidth;
            PlayerSettings.defaultScreenHeight = previousHeight;
            PlayerSettings.fullScreenMode = previousMode;
            AssetDatabase.SaveAssets();
        }
    }
}
