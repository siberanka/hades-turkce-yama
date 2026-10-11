using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace HadesTurkce
{
    public static class Discovery
    {
        public static bool IsGame(string path)
        {
            try
            {
                return Directory.Exists(Path.Combine(path, "Content", "Game", "Text")) &&
                    new[] { "x64", "x64Vk", "x86" }.Any(d => File.Exists(Path.Combine(path, d, "Hades.exe")));
            }
            catch (Exception e) { if (e is IOException || e is ArgumentException || e is UnauthorizedAccessException) return false; throw; }
        }
        public static string[] SteamLibraries(string steamRoot)
        {
            var result = new List<string> { steamRoot };
            string config = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (File.Exists(config) && new FileInfo(config).Length < 2 * 1024 * 1024)
            {
                string text = File.ReadAllText(config);
                foreach (Match m in Regex.Matches(text, "\"(?:path|[0-9]+)\"\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase))
                {
                    string value = m.Groups[1].Value.Replace("\\\\", "\\");
                    if (Path.IsPathRooted(value)) result.Add(value);
                }
            }
            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
        public static string[] EpicInstalls(string manifestDirectory)
        {
            var result = new List<string>();
            if (!Directory.Exists(manifestDirectory)) return result.ToArray();
            foreach (string file in Directory.GetFiles(manifestDirectory, "*.item"))
            {
                try
                {
                    if (new FileInfo(file).Length > 2 * 1024 * 1024) continue;
                    var data = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(file));
                    object name, location;
                    if (data.TryGetValue("DisplayName", out name) && String.Equals(name as string, "Hades", StringComparison.OrdinalIgnoreCase) &&
                        data.TryGetValue("InstallLocation", out location) && location is string)
                        result.Add((string)location);
                }
                catch (Exception e) { if (!(e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is InvalidOperationException)) throw; }
            }
            return result.ToArray();
        }
        public static string[] Find()
        {
            var candidates = new List<string>();
            var steamRoots = new List<string>();
            foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                try
                {
                    using (var registry = RegistryKey.OpenBaseKey(hive, view))
                    using (var key = registry.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                    {
                        if (key == null) continue;
                        foreach (var field in new[] { "SteamPath", "InstallPath" })
                        { var value = key.GetValue(field) as string; if (!String.IsNullOrEmpty(value)) steamRoots.Add(value); }
                    }
                }
                catch (Exception e) { if (!(e is System.Security.SecurityException || e is IOException || e is UnauthorizedAccessException)) throw; }
            }
            foreach (var env in new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 })
            {
                string programFiles = Environment.GetFolderPath(env);
                if (String.IsNullOrEmpty(programFiles)) continue;
                steamRoots.Add(Path.Combine(programFiles, "Steam"));
                candidates.Add(Path.Combine(programFiles, "Epic Games", "Hades"));
            }
            foreach (string steam in steamRoots.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try { foreach (string library in SteamLibraries(steam)) candidates.Add(Path.Combine(library, "steamapps", "common", "Hades")); }
                catch (Exception e) { if (!(e is IOException || e is UnauthorizedAccessException || e is ArgumentException)) throw; }
            }
            try { candidates.AddRange(EpicInstalls(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests"))); }
            catch (Exception e) { if (!(e is IOException || e is UnauthorizedAccessException)) throw; }
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady))
            {
                candidates.Add(Path.Combine(drive.RootDirectory.FullName, "SteamLibrary", "steamapps", "common", "Hades"));
                candidates.Add(Path.Combine(drive.RootDirectory.FullName, "Epic Games", "Hades"));
                candidates.Add(Path.Combine(drive.RootDirectory.FullName, "Games", "Hades"));
            }
            return candidates.Where(IsGame).Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}
