using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using HadesTurkce;

static class InstallerTests
{
    static int passed;
    static void Assert(bool condition, string name)
    { if (!condition) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    static void Refuses(Action action, string name)
    { bool rejected = false; try { action(); } catch (IOException) { rejected = true; } Assert(rejected, name); }
    static byte[] Bytes(string s) { return Encoding.UTF8.GetBytes(s); }
    static string SourcePath(string root, string rel) { return Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar)); }
    static void Write(string path, string text) { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, text, new UTF8Encoding(false)); }
    static Payload Fixture(string root)
    {
        Write(Path.Combine(root, "x64", "Hades.exe"), "fixture only - not executable");
        var names = new[] { "Content/Game/Text/en/Test.en.sjson", "Content/Subtitles/en/Test.csv" };
        var files = names.Select(name => {
            Write(SourcePath(root, name), "prefix EN suffix");
            return new PatchFile { path = name, before = Engine.Hash(Bytes("prefix EN suffix")), after = Engine.Hash(Bytes("prefix Türkçe suffix")),
                edits = new[] { new Edit { offset = 7, length = 2, replacement = Convert.ToBase64String(Bytes("Türkçe")) } } };
        }).ToArray();
        return new Payload { schema = 1, version = "test", preview = true, translated = 2, total = 2, files = files };
    }
    static bool Original(string root, Payload p) { return p.files.All(f => Engine.Hash(File.ReadAllBytes(SourcePath(root, f.path))) == f.before); }
    static bool Patched(string root, Payload p) { return p.files.All(f => Engine.Hash(File.ReadAllBytes(SourcePath(root, f.path))) == f.after); }
    static void Main(string[] args)
    {
        string root = Path.GetFullPath(args[0]);
        if (Directory.Exists(root)) throw new Exception("Test directory must be fresh");
        Directory.CreateDirectory(root);
        if (args.Length == 2 && args[1] == "--crash")
        {
            var crashPayload = Fixture(root);
            new Engine(root, crashPayload).Install(n => { if (n == 1) Environment.Exit(73); });
            throw new Exception("Crash injection did not run");
        }
        var p = Fixture(Path.Combine(root, "roundtrip")); var game = Path.Combine(root, "roundtrip");
        var engine = new Engine(game, p);
        engine.Check(); Assert(Original(game, p) && !Directory.Exists(Path.Combine(game, Engine.BackupName)), "check is read-only");
        engine.Install(); Assert(Patched(game, p), "install produces exact expected bytes");
        Assert(File.Exists(Path.Combine(game, Engine.UninstallerName)), "uninstaller placed in game directory");
        Assert(p.files.All(f => Engine.Hash(File.ReadAllBytes(SourcePath(Path.Combine(game, Engine.BackupName), f.path))) == f.before), "backup hashes match originals");
        Refuses(() => engine.Install(), "duplicate install refuses");
        engine.Restore(); Assert(Original(game, p), "restore is byte-identical");
        Assert(!File.Exists(Path.Combine(game, Engine.UninstallerName)), "uninstaller removed after successful restore");
        engine.Restore(); Assert(Original(game, p), "restore is idempotent");
        engine.Install(); Assert(Patched(game, p), "reinstall after restore");
        Write(SourcePath(game, p.files[1].path), "external modification");
        Refuses(() => engine.Restore(), "external modifications preserved");
        Assert(Engine.Hash(File.ReadAllBytes(SourcePath(game, p.files[0].path))) == p.files[0].after, "restore preflight prevents partial overwrite");

        game = Path.Combine(root, "rollback"); p = Fixture(game); engine = new Engine(game, p);
        bool failed = false;
        try { engine.Install(n => { if (n == 1) throw new IOException("Simulated write failure"); }); } catch (IOException) { failed = true; }
        Assert(failed && Original(game, p), "mid-install failure rolls back all files");

        game = Path.Combine(root, "damaged-backup"); p = Fixture(game); engine = new Engine(game, p); engine.Install();
        Write(SourcePath(Path.Combine(game, Engine.BackupName), p.files[1].path), "corruption");
        Refuses(() => engine.Restore(), "corrupt backup rejected"); Assert(Patched(game, p), "corrupt backup never changes game");

        game = Path.Combine(root, "unknown-version"); p = Fixture(game); engine = new Engine(game, p);
        Write(SourcePath(game, p.files[1].path), "new version");
        Refuses(() => engine.Install(), "unknown version rejected");
        Assert(!Directory.Exists(Path.Combine(game, Engine.BackupName)), "incompatible version leaves no backup or changes");

        game = Path.Combine(root, "paths"); p = Fixture(game);
        p.files[0].path = "../outside.sjson"; Refuses(() => new Engine(game, p), "path traversal rejected");
        p = Fixture(Path.Combine(root, "hash")); p.files[0].after = new string('a', 64);
        Refuses(() => new Engine(Path.Combine(root, "hash"), p).Install(), "tampered patch output rejected");

        game = Path.Combine(root, "changed-uninstaller"); p = Fixture(game); engine = new Engine(game, p); engine.Install();
        Write(Path.Combine(game, Engine.UninstallerName), "external executable replacement");
        Refuses(() => engine.Restore(), "changed uninstaller is preserved");
        Assert(Patched(game, p), "changed uninstaller stops restoration before any file write");

        game = Path.Combine(root, "duplicate-path"); p = Fixture(game);
        p.files[1].path = p.files[0].path;
        Refuses(() => new Engine(game, p), "duplicate payload paths rejected");

        game = Path.Combine(root, "interrupted process");
        var start = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location, "\"" + game + "\" --crash")
            { UseShellExecute = false, CreateNoWindow = true };
        using (var child = Process.Start(start))
        {
            if (!child.WaitForExit(15000)) { child.Kill(); throw new Exception("Crash test timed out"); }
            Assert(child.ExitCode == 73, "process terminates without managed rollback");
        }
        string receiptJson = File.ReadAllText(Path.Combine(game, Engine.BackupName, "receipt.json"));
        var receipt = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Receipt>(receiptJson);
        p = new Payload { schema = 1, files = receipt.files };
        Assert(receipt.state == "prepared" && !Original(game, p) && !Patched(game, p), "durable journal survives a partially installed crash");
        new Engine(game, p).Restore();
        Assert(Original(game, p) && !File.Exists(Path.Combine(game, Engine.UninstallerName)), "next process restores every original and removes uninstaller");

        string steam = Path.Combine(root, "steam");
        Write(Path.Combine(steam, "steamapps", "libraryfolders.vdf"), "\"libraryfolders\" { \"1\" { \"path\" \"D:\\\\Games Library\" } }");
        Assert(Discovery.SteamLibraries(steam).Contains(@"D:\Games Library"), "Steam extra library with spaces");
        string epic = Path.Combine(root, "epic");
        Write(Path.Combine(epic, "valid.item"), "{\"DisplayName\":\"Hades\",\"InstallLocation\":\"D:\\\\Games\\\\Hades\"}");
        Write(Path.Combine(epic, "other.item"), "{\"DisplayName\":\"Hades II\",\"InstallLocation\":\"D:\\\\Games\\\\Hades2\"}");
        Write(Path.Combine(epic, "broken.item"), "malformed");
        Assert(Discovery.EpicInstalls(epic).Length == 1, "Epic finds Hades and excludes sequel and malformed manifests");
        Console.WriteLine("TOTAL " + passed);
    }
}
