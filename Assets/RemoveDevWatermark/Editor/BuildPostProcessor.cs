using System.Diagnostics;
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
            UnityEngine.Debug.Log($"BuildPostProcessor.OnPostprocessBuild / Start\nreport.summary.platform = {report.summary.platform}\nreport.summary.outputPath = {report.summary.outputPath}");

            if (!report.summary.options.HasFlag(BuildOptions.Development))
            {
                UnityEngine.Debug.Log($"BuildPostProcessor.OnPostprocessBuild / This is not DevelopmentBuild");
                return;
            }

            UnityEngine.Debug.Log($"BuildPostProcessor.OnPostprocessBuild / This is a DevelopmentBuild");

            var log = RemoveDevWatermark.Execute(report);

            if (log == null)
            {
                UnityEngine.Debug.Log($"BuildPostProcessor.OnPostprocessBuild / Success");
            }
            else
            {
                UnityEngine.Debug.unityLogger.Log(log.Value.Item1, $"BuildPostProcessor.OnPostprocessBuild / Failed, {log.Value.Item2}");
            }
        }
    }
}
