// SOURCE: Unity Discussions official workaround (May 12 2026)
// https://discussions.unity.com/t/bug-android-unable-to-export-due-to-oculus-integration-6000-4-4f1/1717674
//
// Place this file at:  Assets/Editor/MetaAarNamespacePatcher.cs
//
// It auto-runs before every Android build and patches the two conflicting
// Meta XR AAR files so Gradle 9 (Unity 6.4+) stops throwing the
// "Namespace 'com.oculus.Integration' is used in multiple modules" error.
//
// You can also trigger it manually via:  Build → Patch Meta AAR Namespaces

using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class MetaAarNamespacePatcher : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;

    private const string Tag = "[MetaAarNamespacePatcher]";

    private static readonly (string packagePrefix, string aarRelativePath, string newNamespace)[] Patches =
    {
        (
            "com.meta.xr.sdk.core",
            "Plugins/AndroidOpenXR/OVRPlugin.aar",
            "com.oculus.Integration.core"
        ),
        (
            "com.meta.xr.sdk.interaction",
            "Runtime/Plugins/Android/InteractionSdk.aar",
            "com.oculus.Integration.interaction"
        ),
    };

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        ApplyAll();
    }

    [MenuItem("Build/Patch Meta AAR Namespaces")]
    public static void PatchManually()
    {
        ApplyAll();
        UnityEngine.Debug.Log($"{Tag} Done.");
    }

    private static void ApplyAll()
    {
        string sevenZa = EditorApplication.sevenZipPath;
        if (!File.Exists(sevenZa))
        {
            UnityEngine.Debug.LogError($"{Tag} 7za not found at: {sevenZa}");
            return;
        }

        string packageCache = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", "Library", "PackageCache"));

        foreach (var (prefix, relativePath, newNs) in Patches)
            PatchAar(sevenZa, packageCache, prefix, relativePath, newNs);
    }

    private static void PatchAar(string sevenZa, string packageCache, string packagePrefix,
                                  string aarRelativePath, string newNamespace)
    {
        string[] candidates = Directory.GetDirectories(packageCache, packagePrefix + "@*",
                                                       SearchOption.TopDirectoryOnly);
        if (candidates.Length == 0)
        {
            UnityEngine.Debug.LogWarning(
                $"{Tag} Package '{packagePrefix}' not found in PackageCache.");
            return;
        }

        string aarPath = Path.Combine(candidates[0], aarRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(aarPath))
        {
            UnityEngine.Debug.LogWarning($"{Tag} AAR not found: {aarPath}");
            return;
        }

        string tmpDir = Path.Combine(Path.GetTempPath(), "MetaAarPatch_" + Path.GetFileNameWithoutExtension(aarPath));
        if (Directory.Exists(tmpDir))
            Directory.Delete(tmpDir, recursive: true);
        Directory.CreateDirectory(tmpDir);

        try
        {
            Run7za(sevenZa, $"x \"{aarPath}\" -o\"{tmpDir}\" -y");

            string manifestPath = Path.Combine(tmpDir, "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                UnityEngine.Debug.LogWarning(
                    $"{Tag} No AndroidManifest.xml inside {Path.GetFileName(aarPath)}");
                return;
            }

            string xml = File.ReadAllText(manifestPath);
            string patched = PatchPackageAttribute(xml, newNamespace, out bool changed);
            if (!changed)
            {
                UnityEngine.Debug.Log(
                    $"{Tag} {Path.GetFileName(aarPath)} already patched, skipping.");
                return;
            }

            File.WriteAllText(manifestPath, patched);

            File.Delete(aarPath);
            Run7za(sevenZa, $"a \"{aarPath}\" \"{tmpDir}{Path.DirectorySeparatorChar}*\" -tzip -mx=5");

            UnityEngine.Debug.Log(
                $"{Tag} Patched {Path.GetFileName(aarPath)} -> {newNamespace}");
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    private static void Run7za(string sevenZa, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = sevenZa,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var proc = Process.Start(psi);
        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        if (proc.ExitCode != 0)
            throw new System.Exception(
                $"{Tag} 7za failed (exit {proc.ExitCode}):\n{stdout}\n{stderr}");
    }

    private static string PatchPackageAttribute(string xml, string newNamespace, out bool changed)
    {
        var match = Regex.Match(xml, @"(?<=\bpackage="")[^""]*");
        if (!match.Success || match.Value == newNamespace)
        {
            changed = false;
            return xml;
        }

        changed = true;
        return xml.Substring(0, match.Index) + newNamespace + xml.Substring(match.Index + match.Length);
    }
}