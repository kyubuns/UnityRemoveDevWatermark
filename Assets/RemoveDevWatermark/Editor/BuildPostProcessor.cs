using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RemoveDevWatermark.Editor
{
    public class BuildPostProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            Debug.Log($"BuildPostProcessor.OnPostprocessBuild / Start\nreport.summary.platform = {report.summary.platform}\nreport.summary.outputPath = {report.summary.outputPath}");

            if (!report.summary.options.HasFlag(BuildOptions.Development))
            {
                Debug.Log($"BuildPostProcessor.OnPostprocessBuild / This is not DevelopmentBuild");
                return;
            }

            var errorMessage = Execute(report);

            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                Debug.Log($"BuildPostProcessor.OnPostprocessBuild / Success");
            }
            else
            {
                Debug.LogError($"BuildPostProcessor.OnPostprocessBuild / Failed, {errorMessage}");
            }
        }

        private static string Execute(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.StandaloneOSX)
            {
                var path = Path.Combine(report.summary.outputPath, "Contents", "Resources", "unity default resources");
                return RemoveDevWatermark(path);
            }
            else
            {
                return $"Unknown Platform: {report.summary.platform}";
            }
        }

        private static string RemoveDevWatermark(string path)
        {
            if (!File.Exists(path)) return $"{path} not found";
            return null;
        }
    }
}