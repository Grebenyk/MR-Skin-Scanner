#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Workaround for a Unity 6000.4.x + Android Gradle Plugin 9 incompatibility with the
/// Meta XR SDK. Several Meta prebuilt AARs declare the SAME library package name in their
/// AndroidManifest.xml (e.g. OVRPlugin.aar and SDKTelemetry.aar are both
/// "com.oculus.Integration"). AGP 9 turns each AAR's manifest package into its namespace,
/// and two libraries sharing a namespace makes the launcher manifest merge fail with
/// "more than one library with package name ..." (the build error you hit).
///
/// This runs before every Android build, finds Meta AARs whose manifest package collides
/// with one already seen, and rewrites the duplicate to a unique value. It does NOT touch
/// the first/canonical occurrence, so the change is minimal and idempotent: once duplicates
/// are made unique, subsequent builds find nothing to change.
/// </summary>
public class MetaAarPackageFix : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    private const string PackageCacheGlob = "com.meta.xr.*";

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        string packageCache = Path.GetFullPath(Path.Combine("Library", "PackageCache"));
        if (!Directory.Exists(packageCache))
        {
            Debug.LogWarning("[MetaAarPackageFix] Library/PackageCache not found; skipping.");
            return;
        }

        // Collect Meta AARs deterministically (sorted) so the "first" occurrence is stable.
        var aars = Directory.GetDirectories(packageCache, PackageCacheGlob)
            .SelectMany(d => Directory.GetFiles(d, "*.aar", SearchOption.AllDirectories))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        var seenPackages = new HashSet<string>(StringComparer.Ordinal);
        int renamed = 0;

        foreach (var aar in aars)
        {
            try
            {
                string pkg = ReadManifestPackage(aar);
                if (string.IsNullOrEmpty(pkg))
                    continue;

                if (seenPackages.Contains(pkg))
                {
                    // Duplicate: give this AAR a unique, deterministic package name.
                    string suffix = Sanitize(Path.GetFileNameWithoutExtension(aar));
                    string newPkg = pkg + "." + suffix;
                    // Guard against an unlikely second-level collision.
                    while (seenPackages.Contains(newPkg))
                        newPkg += "x";

                    if (SetManifestPackage(aar, pkg, newPkg))
                    {
                        seenPackages.Add(newPkg);
                        renamed++;
                        Debug.Log($"[MetaAarPackageFix] {Path.GetFileName(aar)}: '{pkg}' -> '{newPkg}'");
                    }
                }
                else
                {
                    seenPackages.Add(pkg);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MetaAarPackageFix] Could not process {aar}: {e.Message}");
            }
        }

        Debug.Log($"[MetaAarPackageFix] Done. Renamed {renamed} duplicate Meta AAR package name(s).");
    }

    private static string ReadManifestPackage(string aarPath)
    {
        using var zip = ZipFile.OpenRead(aarPath);
        var entry = zip.GetEntry("AndroidManifest.xml");
        if (entry == null)
            return null;
        using var reader = new StreamReader(entry.Open());
        string content = reader.ReadToEnd();
        var m = Regex.Match(content, "package=\"([^\"]*)\"");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static bool SetManifestPackage(string aarPath, string oldPkg, string newPkg)
    {
        // Clear read-only so we can rewrite the cached AAR.
        try { File.SetAttributes(aarPath, FileAttributes.Normal); } catch { /* ignore */ }

        // IMPORTANT: do NOT edit the AAR in-place with ZipArchiveMode.Update. That leaves
        // STORED (uncompressed) entries carrying data descriptors, which Android's Jetifier
        // rejects with "only DEFLATED entries can have EXT descriptor". Instead we fully
        // extract and re-create the archive with every entry DEFLATED (Optimal), which both
        // avoids data descriptors (Create mode on a seekable file) and keeps all entries
        // compressed -> a clean, tool-compatible AAR.
        string tempDir = Path.Combine(Path.GetTempPath(), "metaaarfix_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            ZipFile.ExtractToDirectory(aarPath, tempDir);

            string manifestPath = Path.Combine(tempDir, "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
                return false;

            string content = File.ReadAllText(manifestPath);
            string newContent = content.Replace($"package=\"{oldPkg}\"", $"package=\"{newPkg}\"");
            if (newContent == content)
                return false;

            File.WriteAllText(manifestPath, newContent, new UTF8Encoding(false));

            File.Delete(aarPath);
            ZipFile.CreateFromDirectory(tempDir, aarPath, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);
            return true;
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* ignore */ }
        }
    }

    private static string Sanitize(string name)
    {
        var sb = new StringBuilder();
        foreach (char c in name)
            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_');
        return sb.ToString();
    }
}
#endif
