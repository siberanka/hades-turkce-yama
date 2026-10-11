using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Reflection;

namespace HadesTurkce
{
    public sealed class Edit { public int offset; public int length; public string replacement; }
    public sealed class PatchFile { public string path; public string before; public string after; public Edit[] edits; }
    public sealed class Payload
    {
        public int schema; public string version; public bool preview;
        public int translated; public int total; public PatchFile[] files;
    }
    public sealed class Receipt { public string state; public string version; public string uninstallerSha256; public PatchFile[] files; }

    public sealed class Engine
    {
        public const string BackupName = ".hades-tr-backup";
        public const string UninstallerName = "Hades-Turkce-Yamayi-Kaldir.exe";
        public string UninstallerHash { get; private set; }
        readonly string root;
        readonly Payload payload;
        static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = 32 * 1024 * 1024 };
        static readonly Regex Allowed = new Regex(@"^Content/(Game/Text/en/[A-Za-z0-9_.-]+\.sjson|Subtitles/en/[A-Za-z0-9_.-]+\.csv|Scripts/[A-Za-z0-9_.-]+\.lua)$");
        static readonly Regex HashPattern = new Regex("^[a-f0-9]{64}$");

        public Engine(string directory, Payload patch)
        {
            root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar);
            payload = patch;
            NoLinks(root);
            if (!Directory.Exists(root)) throw new IOException("Oyun klasörü bulunamadı.");
            if (!new[] { "x64", "x64Vk", "x86" }.Any(d => File.Exists(Path.Combine(root, d, "Hades.exe"))))
                throw new IOException("Seçilen klasörde Hades kurulumu bulunamadı.");
            if (patch == null || patch.schema != 1 || patch.files == null)
                throw new IOException("Geçersiz yama paketi.");
            ValidateFiles(patch.files);
        }

        public static Payload ReadPayload(string json) { return Json.Deserialize<Payload>(json); }
        public static string Hash(byte[] data)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }
        static void NoLinks(string path)
        {
            var item = new FileInfo(Path.GetFullPath(path));
            string current = item.FullName;
            while (!String.IsNullOrEmpty(current))
            {
                if ((File.Exists(current) || Directory.Exists(current)) &&
                    (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Bağlantı veya yönlendirilmiş klasör kabul edilmiyor: " + current);
                var parent = Directory.GetParent(current);
                current = parent == null ? null : parent.FullName;
            }
        }
        string Under(string parent, string relative)
        {
            if (String.IsNullOrEmpty(relative) || relative.Contains("..") || relative.Contains(":") || relative.Contains("\\") || relative.StartsWith("/"))
                throw new IOException("Geçersiz dosya yolu.");
            string full = Path.GetFullPath(Path.Combine(parent, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Dosya yolu oyun klasörünün dışında.");
            NoLinks(full);
            return full;
        }
        static void ValidateFiles(PatchFile[] files)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in files)
            {
                if (f == null || f.path == null || !Allowed.IsMatch(f.path) || !seen.Add(f.path) ||
                    f.before == null || !HashPattern.IsMatch(f.before) || f.after == null || !HashPattern.IsMatch(f.after))
                    throw new IOException("Yama dosya listesi geçersiz.");
            }
        }
        static byte[] Read(string path)
        {
            if (!File.Exists(path) || new FileInfo(path).Length > 32 * 1024 * 1024)
                throw new IOException("Dosya bulunamadı veya beklenen boyutu aşıyor: " + path);
            return File.ReadAllBytes(path);
        }
        static void ClosedGame()
        {
            foreach (var p in Process.GetProcessesByName("Hades"))
            { p.Dispose(); throw new IOException("İşlemden önce Hades'i kapatın."); }
        }
        public static byte[] Transform(byte[] original, PatchFile file)
        {
            if (Hash(original) != file.before) throw new IOException("Kaynak dosya sürümü farklı: " + file.path);
            if (file.edits == null) throw new IOException("Eksik yama kayıtları.");
            using (var stream = new MemoryStream())
            {
                int cursor = 0;
                foreach (var edit in file.edits)
                {
                    if (edit == null || edit.offset < cursor || edit.length <= 0 || edit.offset > original.Length || edit.length > original.Length - edit.offset)
                        throw new IOException("Çakışan veya geçersiz yama aralığı.");
                    stream.Write(original, cursor, edit.offset - cursor);
                    byte[] bytes = Convert.FromBase64String(edit.replacement);
                    if (bytes.Length > 1024 * 1024 || stream.Length + bytes.Length > 32 * 1024 * 1024)
                        throw new IOException("Yama boyutu beklenen sınırı aşıyor.");
                    stream.Write(bytes, 0, bytes.Length);
                    cursor = edit.offset + edit.length;
                }
                stream.Write(original, cursor, original.Length - cursor);
                byte[] output = stream.ToArray();
                if (Hash(output) != file.after) throw new IOException("Yama bütünlük doğrulaması başarısız.");
                return output;
            }
        }
        static void AtomicWrite(string path, byte[] data)
        {
            NoLinks(path);
            string temp = path + ".hades-tr-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var f = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                { f.Write(data, 0, data.Length); f.Flush(true); }
                NoLinks(path);
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        string Backup { get { return Under(root, BackupName); } }
        string ReceiptPath { get { return Under(Backup, "receipt.json"); } }
        void SaveReceipt(Receipt receipt)
        { AtomicWrite(ReceiptPath, Encoding.UTF8.GetBytes(Json.Serialize(receipt))); }
        FileStream Lock()
        {
            Directory.CreateDirectory(Backup);
            return new FileStream(Under(Backup, "operation.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        public string Check()
        {
            ClosedGame();
            if (payload.files.Length == 0) return "Bu araç paketinde çeviri bulunmuyor. Kurulum yapılamaz.";
            foreach (var f in payload.files) Transform(Read(Under(root, f.path)), f);
            return String.Format("Dosyalar uyumlu. {0}/{1} metin kaydı; {2} dosya.\r\n{3}",
                payload.translated, payload.total, payload.files.Length,
                payload.preview ? "ÖN SÜRÜM: Çeviri eksiktir; oyun içi doğrulama tamamlanmamıştır." : "Tam çeviri paketi.");
        }
        public void Install(Action<int> afterWrite = null)
        {
            if (payload.files.Length == 0) throw new IOException("Bu paket çeviri içermiyor.");
            Check();
            using (Lock())
            {
                string uninstallPath = Under(root, UninstallerName);
                if (File.Exists(uninstallPath)) throw new IOException("Mevcut kaldırma aracı korunuyor. Önce çeviriyi kaldırın.");
                if (File.Exists(ReceiptPath))
                {
                    var previous = Json.Deserialize<Receipt>(Encoding.UTF8.GetString(Read(ReceiptPath)));
                    if (previous == null || previous.state != "restored")
                        throw new IOException("Önceki kurulumun yedeği mevcut. Yeni sürümden önce çeviriyi kaldırın.");
                    RestoreCore(previous);
                    string history = Under(Backup, "history/" + Guid.NewGuid().ToString("N") + ".json");
                    Directory.CreateDirectory(Path.GetDirectoryName(history));
                    AtomicWrite(history, Encoding.UTF8.GetBytes(Json.Serialize(previous)));
                }
                var originals = new Dictionary<string, byte[]>();
                var outputs = new Dictionary<string, byte[]>();
                foreach (var f in payload.files)
                {
                    var data = Read(Under(root, f.path));
                    originals.Add(f.path, data);
                    outputs.Add(f.path, Transform(data, f));
                }
                long needed = originals.Values.Sum(b => (long)b.Length) * 3 + outputs.Values.Sum(b => (long)b.Length) * 2 + 10485760;
                if (new DriveInfo(Path.GetPathRoot(root)).AvailableFreeSpace < needed)
                    throw new IOException("Yedekleme için yeterli boş alan yok.");
                foreach (var f in payload.files)
                {
                    string backupFile = Under(Backup, f.path);
                    Directory.CreateDirectory(Path.GetDirectoryName(backupFile));
                    if (!File.Exists(backupFile)) AtomicWrite(backupFile, originals[f.path]);
                    if (Hash(Read(backupFile)) != f.before) throw new IOException("Yedek doğrulanamadı.");
                }
                byte[] uninstaller = Read(Assembly.GetExecutingAssembly().Location);
                var receipt = new Receipt { state = "prepared", version = payload.version, uninstallerSha256 = Hash(uninstaller),
                    files = payload.files.Select(f => new PatchFile { path = f.path, before = f.before, after = f.after }).ToArray() };
                SaveReceipt(receipt);
                try
                {
                    AtomicWrite(uninstallPath, uninstaller);
                    int count = 0;
                    foreach (var f in payload.files)
                    {
                        string target = Under(root, f.path);
                        if (Hash(Read(target)) != f.before) throw new IOException("Dosya işlem sırasında değişti: " + f.path);
                        AtomicWrite(target, outputs[f.path]);
                        if (Hash(Read(target)) != f.after) throw new IOException("Yazılan dosya doğrulanamadı.");
                        if (afterWrite != null) afterWrite(++count);
                    }
                    receipt.state = "installed";
                    SaveReceipt(receipt);
                }
                catch
                {
                    // A crash can be recovered from the durable receipt on the next run.
                    RestoreCore(receipt);
                    throw;
                }
            }
        }
        void RestoreCore(Receipt receipt)
        {
            if (receipt == null || receipt.files == null || receipt.files.Length == 0)
                throw new IOException("Yedek kayıtları geçersiz.");
            ValidateFiles(receipt.files);
            string uninstallPath = Under(root, UninstallerName);
            if (File.Exists(uninstallPath) && Hash(Read(uninstallPath)) != receipt.uninstallerSha256)
                throw new IOException("Kaldırma aracı değiştirilmiş; dosya korunuyor.");
            var backups = new Dictionary<string, byte[]>();
            foreach (var f in receipt.files)
            {
                string current = Hash(Read(Under(root, f.path)));
                if (current != f.before && current != f.after)
                    throw new IOException("Dosya başka bir uygulama tarafından değiştirilmiş; üzerine yazılmadı: " + f.path);
                byte[] original = Read(Under(Backup, f.path));
                if (Hash(original) != f.before) throw new IOException("Yedek bütünlüğü doğrulanamadı: " + f.path);
                backups.Add(f.path, original);
            }
            foreach (var f in receipt.files)
            {
                string target = Under(root, f.path);
                string current = Hash(Read(target));
                if (current == f.before) continue;
                if (current != f.after) throw new IOException("Dosya geri yükleme sırasında değişti.");
                AtomicWrite(target, backups[f.path]);
                if (Hash(Read(target)) != f.before) throw new IOException("Geri yükleme doğrulanamadı.");
            }
            receipt.state = "restored";
            SaveReceipt(receipt);
            UninstallerHash = receipt.uninstallerSha256;
            if (File.Exists(uninstallPath) && !String.Equals(Path.GetFullPath(Assembly.GetExecutingAssembly().Location), uninstallPath, StringComparison.OrdinalIgnoreCase))
                File.Delete(uninstallPath);
        }
        public void Restore()
        {
            ClosedGame();
            if (!File.Exists(ReceiptPath)) throw new IOException("Geri yüklenecek yedek bulunamadı.");
            using (Lock()) RestoreCore(Json.Deserialize<Receipt>(Encoding.UTF8.GetString(Read(ReceiptPath))));
        }
    }
}
