using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RemoveDevWatermark.Editor
{
    public static class RemoveDevWatermark
    {
        public static (LogType, string)? Execute(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.StandaloneWindows || report.summary.platform == BuildTarget.StandaloneWindows64)
            {
                var path = Path.Combine(report.summary.outputPath.Replace(".exe", "_Data"), "Resources", "unity default resources");
                var modifiableFile = new ModifiableFile(path);
                return Execute(modifiableFile);
            }

            if (report.summary.platform == BuildTarget.StandaloneOSX)
            {
#if UNITY_2021
                var path = Path.Combine(report.summary.outputPath, "Contents", "Resources", "unity default resources");
#else
                var path = Path.Combine(report.summary.outputPath, "Contents", "Resources", "Data", "Resources", "unity default resources");
#endif
                var modifiableFile = new ModifiableFile(path);
                var log = Execute(modifiableFile);
                
#if UNITY_STANDALONE_OSX
                Debug.Log($"BuildPostProcessor.OnPostprocessBuild / MacOSCodeSigning.CodeSignAppBundle({report.summary.outputPath})");
                UnityEditor.OSXStandalone.MacOSCodeSigning.CodeSignAppBundle(report.summary.outputPath);
#endif
                return log;
            }

            if (report.summary.platform == BuildTarget.iOS)
            {
#if UNITY_2021 || UNITY_2022
                var path = Path.Combine(report.summary.outputPath, "Data", "unity default resources");
#else
                var path = Path.Combine(report.summary.outputPath, "Data", "Resources", "unity default resources");
#endif
                var modifiableFile = new ModifiableFile(path);
                return Execute(modifiableFile);
            }

#if UNITY_2022
            if (report.summary.platform == BuildTarget.Android)
            {
                var modifiableZip = new ModifiableZip(report.summary.outputPath, "assets/bin/Data/unity default resources");
                var log = Execute(modifiableZip);
                SignAndroidApk(report.summary.outputPath);
                return log;
            }      
#endif

            return (LogType.Warning, $"Unknown Platform: {report.summary.platform}");
        }

        private static (LogType, string)? Execute(IModifiableDocument modifiableDocument)
        {
            if (!modifiableDocument.Validate()) return (LogType.Error, $"{modifiableDocument.Path} not found");

            var bytes = modifiableDocument.ReadAllBytes();
            var nameHex = new byte[] { 0x55, 0x6E, 0x69, 0x74, 0x79, 0x57, 0x61, 0x74, 0x65, 0x72, 0x6D, 0x61, 0x72, 0x6B, 0x2D, 0x64, 0x65, 0x76 }; // "UnityWatermark-dev"

            var index = KMP(bytes, nameHex);
            if (index == -1) return (LogType.Error, "nameHex is not found");

#if UNITY_2021 || UNITY_2022
            const int widthIndex = 28;
#else
            const int widthIndex = 24;
#endif
            const int widthValue = 115;
            if (bytes[index + widthIndex] != widthValue) return (LogType.Error, $"bytes[index + widthIndex]({bytes[index + widthIndex]}) != widthValue({widthValue})");
            bytes[index + widthIndex] = 1;

#if UNITY_2021 || UNITY_2022
            const int heightIndex = 32;
#else
            const int heightIndex = 28;
#endif
            const int heightValue = 17;
            if (bytes[index + heightIndex] != heightValue) return (LogType.Error, $"bytes[index + heightIndex]({bytes[index + heightIndex]}) != heightValue({heightValue})");
            bytes[index + heightIndex] = 1;

            modifiableDocument.WriteAllBytes(bytes);
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

        private static void SignAndroidApk(string apkPath)
        {
            var embeddedAndroidSdkRelativePath = Application.platform == RuntimePlatform.OSXEditor
                ? "PlaybackEngines/AndroidPlayer/SDK"
                : "Data/PlaybackEngines/AndroidPlayer/SDK";

            var embeddedAndroidSdkFullPath = Path.Combine(new DirectoryInfo(EditorApplication.applicationPath).Parent!.FullName, embeddedAndroidSdkRelativePath);

            try
            {
                var aligned = $"{apkPath}.aligned";
                var zipAlign = Directory.GetFiles(embeddedAndroidSdkFullPath, searchOption: SearchOption.AllDirectories, searchPattern: "zipalign.*").Single();
                RunCommand(
                    zipAlign,
                    $"-v 4 \"{apkPath}\" \"{aligned}\""
                );

                File.Delete(apkPath);
                File.Move(aligned, apkPath);

                var userHome = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("USERPROFILE");
                var keystore = Path.Combine(userHome!, ".android/debug.keystore");
                var apkSigner = Directory.GetFiles(embeddedAndroidSdkFullPath, searchOption: SearchOption.AllDirectories, searchPattern: "apksigner.*").Single(p => !p.EndsWith(".jar"));

                RunCommand(
                    apkSigner,
                    $"sign --ks-key-alias androiddebugkey --ks \"{keystore}\" --ks-pass pass:android --key-pass pass:android \"{apkPath}\""
                );

                Debug.Log("Both commands executed successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error: " + ex.Message);
            }
        }

        private static void RunCommand(string fileName, string arguments)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = processInfo;
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Debug.Log(output);
            if (!string.IsNullOrWhiteSpace(error))
            {
                Debug.LogError("Error output:");
                Debug.LogError(error);
            }

            if (process.ExitCode != 0)
            {
                throw new Exception($"Command '{fileName}' exited with code {process.ExitCode}");
            }
        }
    }
}
