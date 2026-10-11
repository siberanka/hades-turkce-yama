using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace HadesTurkce
{
    public static class Cleanup
    {
        public static bool IsUninstaller
        {
            get { return String.Equals(Path.GetFileName(Assembly.GetExecutingAssembly().Location), Engine.UninstallerName, StringComparison.OrdinalIgnoreCase); }
        }
        public static void Schedule(string root, string hash)
        {
            if (!IsUninstaller) return;
            string staging = Path.Combine(Path.GetTempPath(), "HadesTurkce-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            string helper = Path.Combine(staging, "cleanup.exe");
            File.Copy(Assembly.GetExecutingAssembly().Location, helper, false);
            var start = new ProcessStartInfo(helper,
                "--cleanup \"" + Path.GetFullPath(root).TrimEnd('\\') + "\" " + Process.GetCurrentProcess().Id + " " + hash)
            { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
            using (var process = Process.Start(start)) { }
        }
        public static int Run(string root, string pid, string hash)
        {
            // The helper can only remove the fixed uninstaller filename, with an
            // exact checksum, after the original process exits. No shell is used.
            root = Path.GetFullPath(root).TrimEnd('\\');
            string target = Path.Combine(root, Engine.UninstallerName);
            string current = root;
            while (!String.IsNullOrEmpty(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return 1;
                var parent = Directory.GetParent(current); current = parent == null ? null : parent.FullName;
            }
            try { using (var parent = Process.GetProcessById(Int32.Parse(pid))) if (!parent.WaitForExit(60000)) return 1; }
            catch (ArgumentException) { }
            for (int i = 0; i < 30; i++)
            {
                if (!File.Exists(target)) return 0;
                if ((File.GetAttributes(target) & FileAttributes.ReparsePoint) != 0 || Engine.Hash(File.ReadAllBytes(target)) != hash) return 1;
                try { File.Delete(target); return 0; }
                catch (IOException) { Thread.Sleep(200); }
            }
            return 1;
        }
    }
}
