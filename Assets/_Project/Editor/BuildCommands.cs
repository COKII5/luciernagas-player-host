using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Fireflies.EditorTools
{
    public static class BuildCommands
    {
        [MenuItem("Fireflies/Build Windows Player")]
        public static void BuildWindows()
        {
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectAssetPaths.MainScene },
                locationPathName = ProjectAssetPaths.WindowsBuild,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[Build] {report.summary.result} in {report.summary.totalTime}, size {report.summary.totalSize / (1024 * 1024)} MB");
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
