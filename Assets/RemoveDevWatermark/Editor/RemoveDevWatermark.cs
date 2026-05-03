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
#if UNITY_6000_3_OR_NEWER
                var path = Path.Combine(report.summary.outputPath, "Contents", "Resources", "Data", "Resources", "unity default resources");
#elif UNITY_2021
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
#if UNITY_6000_3_OR_NEWER
                var path = Path.Combine(report.summary.outputPath, "Data", "Resources", "unity default resources");
#elif UNITY_2021_1_OR_NEWER || UNITY_2022_1_OR_NEWER
                var path = Path.Combine(report.summary.outputPath, "Data", "unity default resources");
#else
                var path = Path.Combine(report.summary.outputPath, "Data", "Resources", "unity default resources");
#endif
                var modifiableFile = new ModifiableFile(path);
                return Execute(modifiableFile);
            }

#if UNITY_2022_1_OR_NEWER
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

            const int widthValue = 115;
            const int heightValue = 17;
            var offsetPair = GetWatermarkOffsetPair(bytes, index, widthValue, heightValue);
            if (offsetPair == null)
            {
                return (LogType.Error, $"Unsupported watermark layout near index {index}: expected width={widthValue} and height={heightValue} at offsets 24/28 or 28/32");
            }

            var (widthIndex, heightIndex) = offsetPair.Value;
            bytes[index + widthIndex] = 1;
            bytes[index + heightIndex] = 1;

            modifiableDocument.WriteAllBytes(bytes);
            return null;
        }

        private static (int widthIndex, int heightIndex)? GetWatermarkOffsetPair(byte[] bytes, int index, int widthValue, int heightValue)
        {
            var candidateOffsets = new[]
            {
                (widthIndex: 24, heightIndex: 28),
                (widthIndex: 28, heightIndex: 32)
            };

            foreach (var candidate in candidateOffsets)
            {
                if (bytes.Length <= index + candidate.heightIndex)
                {
                    continue;
                }

                if (bytes[index + candidate.widthIndex] == widthValue && bytes[index + candidate.heightIndex] == heightValue)
                {
                    return candidate;
                }
            }

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
