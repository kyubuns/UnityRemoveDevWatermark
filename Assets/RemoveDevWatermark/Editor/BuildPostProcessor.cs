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

            if (errorMessage == null)
            {
                Debug.Log($"BuildPostProcessor.OnPostprocessBuild / Success");
            }
            else
            {
                Debug.unityLogger.Log(errorMessage.Value.Item1, $"BuildPostProcessor.OnPostprocessBuild / Failed, {errorMessage.Value.Item2}");
            }
        }

        private static (LogType, string)? Execute(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.StandaloneWindows || report.summary.platform == BuildTarget.StandaloneWindows64)
            {
                var path = Path.Combine(report.summary.outputPath.Replace(".exe", "_Data"), "Resources", "unity default resources");
                return RemoveDevWatermark(path);
            }
            else if (report.summary.platform == BuildTarget.StandaloneOSX)
            {
                var path = Path.Combine(report.summary.outputPath, "Contents", "Resources", "unity default resources");
                return RemoveDevWatermark(path);
            }
            else if (report.summary.platform == BuildTarget.iOS)
            {
                var path = Path.Combine(report.summary.outputPath, "Data", "unity default resources");
                return RemoveDevWatermark(path);
            }
            else
            {
                return (LogType.Warning, $"Unknown Platform: {report.summary.platform}");
            }
        }

        private static (LogType, string)? RemoveDevWatermark(string path)
        {
            if (!File.Exists(path)) return (LogType.Error, $"{path} not found");

            var bytes = File.ReadAllBytes(path);
            var nameHex = new byte[] { 0x55, 0x6E, 0x69, 0x74, 0x79, 0x57, 0x61, 0x74, 0x65, 0x72, 0x6D, 0x61, 0x72, 0x6B, 0x2D, 0x64, 0x65, 0x76 }; // "UnityWatermark-dev"

            var index = KMP(bytes, nameHex);
            if (index == -1) return (LogType.Error, "nameHex is not found");

            const int widthIndex = 28;
            const int widthValue = 115;
            if (bytes[index + widthIndex] != widthValue) return (LogType.Error, $"bytes[index + widthIndex]({bytes[index + widthIndex]}) != widthValue({widthValue})");
            bytes[index + widthIndex] = 1;

            const int heightIndex = 32;
            const int heightValue = 17;
            if (bytes[index + heightIndex] != heightValue) return (LogType.Error, $"bytes[index + heightIndex]({bytes[index + heightIndex]}) != heightValue({heightValue})");
            bytes[index + heightIndex] = 1;

            File.WriteAllBytes(path, bytes);
            return null;
        }

        private static int[] ComputeFailureFunction(byte[] pattern)
        {
            var fail = new int[pattern.Length];
            var m = pattern.Length;
            int j;

            fail[0] = -1;
            for (j = 1; j < m; j++)
            {
                var i = fail[j - 1];
                while ((pattern[j] != pattern[i + 1]) && (i >= 0))
                    i = fail[i];
                if (pattern[j] == pattern[i + 1])
                    fail[j] = i + 1;
                else
                    fail[j] = -1;
            }

            return fail;
        }

        // ReSharper disable once InconsistentNaming
        private static int KMP(byte[] bytes, byte[] nameHex)
        {
            int i = 0, j = 0;
            var n = bytes.Length;
            var m = nameHex.Length;
            var fail = ComputeFailureFunction(nameHex);

            while (i < n)
            {
                if (bytes[i] == nameHex[j])
                {
                    if (j == m - 1)
                        return i - m + 1;
                    i++;
                    j++;
                }
                else if (j > 0)
                    j = fail[j - 1] + 1;
                else
                    i++;
            }

            return -1;
        }
    }
}