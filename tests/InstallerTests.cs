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
    static string Sjson(string value)
    {
        string body = value.Replace("\r", @"\r").Replace("\n", @"\n").Replace("\t", @"\t");
        string quote = body.Contains("\"") ? "\"\"\"" : "\"";
        return quote + body + quote;
    }
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
    static Edit[] TextEdits(string path, byte[] original)
    {
        var counts = new System.Collections.Generic.Dictionary<string, int>();
        return TextCatalog.Read(original, path).Select(entry => {
            int occurrence; counts.TryGetValue(entry.id, out occurrence); counts[entry.id] = ++occurrence;
            string target = "Türkçe: " + entry.value;
            string encoded = path.EndsWith(".csv") ? "\"" + target.Replace("\"", "\"\"") + "\"" :
                path.EndsWith(".lua") ? new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(target) : Sjson(target);
            return new Edit { offset = entry.start, length = entry.length, entry = entry.id + "::" + occurrence,
                source_sha256 = Engine.Hash(Bytes(entry.value)), replacement = Convert.ToBase64String(Bytes(encoded)) };
        }).ToArray();
    }
    static PatchFile TextFile(string path, string original)
    {
        byte[] bytes = Bytes(original); var edits = TextEdits(path, bytes);
        using (var stream = new MemoryStream())
        {
            int cursor = 0;
            foreach (var edit in edits)
            {
                stream.Write(bytes, cursor, edit.offset - cursor);
                var replacement = Convert.FromBase64String(edit.replacement); stream.Write(replacement, 0, replacement.Length);
                cursor = edit.offset + edit.length;
            }
            stream.Write(bytes, cursor, bytes.Length - cursor);
            return new PatchFile { path = path, before = Engine.Hash(bytes), after = Engine.Hash(stream.ToArray()), edits = edits };
        }
    }
    static void CompatibilityTests(string root)
    {
        const string path = "Content/Game/Text/en/Test.en.sjson";
        string original = "{ Texts=[{Id=\"a\" DisplayName=\"Hello\"}{Id=\"b\" DisplayName=\"Old\"}] }";
        string current = "\uFEFF// {Id=\"a\" DisplayName=\"Hello\"}\r\n{ Texts=[{Id=\"new\" DisplayName=\"New 😀\"}{Id=\"b\" DisplayName=\"Changed\"}{Id=\"a\" DisplayName=\"Hello\"}] }\r\n";
        var file = TextFile(path, original);
        var rebased = TextCatalog.Rebase(Bytes(current), file);
        string result = Encoding.UTF8.GetString(rebased.data);
        Assert(rebased.applied == 1 && result.Contains("DisplayName=\"Türkçe: Hello\"") && result.Contains("DisplayName=\"Changed\"") && result.Contains("DisplayName=\"New 😀\""), "SJSON rebases reordered IDs and preserves changed/new text and Unicode");
        Assert(result.StartsWith("\uFEFF// {Id=\"a\" DisplayName=\"Hello\"}\r\n") && result.EndsWith("\r\n"), "fallback preserves BOM, comments and line endings");
        string raw = "{Id=\"credits\" Description=\"\"\"Raw \"credits\" {Id=\"a\" DisplayName=\"Hello\"}\nKeep original\"\"\"}";
        var rawResult = TextCatalog.Rebase(Bytes(raw + current), file);
        Assert(rawResult.applied == 1 && Encoding.UTF8.GetString(rawResult.data).StartsWith(raw), "SJSON triple-quoted raw blocks are preserved and do not hide following IDs");
        string directives = "{Id=\"choice\" DisplayName=" + Sjson(@"\[ Damage: \Column 380 {$Value} \]") + "}";
        var directiveFile = TextFile(path, directives);
        var directiveResult = TextCatalog.Rebase(Bytes("// version shift\n" + directives), directiveFile);
        string directiveWire = Encoding.UTF8.GetString(directiveResult.data);
        Assert(directiveResult.applied == 1 && directiveWire.Contains(@"\Column 380") && !directiveWire.Contains(@"\\Column") &&
            directiveWire.Contains(@"\[") && !directiveWire.Contains(@"\\["), "SJSON choice brackets and column directives retain exactly one backslash on disk");
        string quotes = "{Id=\"quote\" Description=" + Sjson("Bir \"söz\"") + "}";
        var quoteResult = TextCatalog.Rebase(Bytes("// shifted\n" + quotes), TextFile(path, quotes));
        Assert(quoteResult.applied == 1 && TextCatalog.Read(quoteResult.data, path)[0].value == "Türkçe: Bir \"söz\"", "translated raw blocks preserve embedded and final quotes");
        Assert(TextCatalog.Rebase(Bytes(current.Replace("Id=\"a\" DisplayName=\"Hello\"}", "Id=\"other\" DisplayName=\"Hello\"}")), file).applied == 0, "identical English under a different ID never receives a translation");

        var duplicate = TextFile(path, "{Id=\"x\" DisplayName=\"Same\"}{Id=\"x\" DisplayName=\"Same\"}");
        duplicate.edits[1].replacement = Convert.ToBase64String(Bytes("\"Another target\""));
        Assert(TextCatalog.Rebase(Bytes("// new\n{Id=\"x\" DisplayName=\"Same\"}{Id=\"x\" DisplayName=\"Same\"}"), duplicate).applied == 0, "ambiguous duplicate IDs with different translations are preserved");
        duplicate = TextFile(path, "{Id=\"x\" DisplayName=\"Same\"}{Id=\"x\" DisplayName=\"Same\"}");
        Assert(TextCatalog.Rebase(Bytes("{Id=\"x\" DisplayName=\"Same\"}"), duplicate).applied == 0, "changed duplicate occurrence counts are preserved");

        string csvPath = "Content/Subtitles/en/Test.csv";
        string csv = "Speaker,ID,ID,ID,,,Line\r\nRecorded,A,1,Voice_001,,,\"Hello, \"\"friend\"\"\nNext\"\r\n";
        var csvFile = TextFile(csvPath, csv);
        var csvResult = TextCatalog.Rebase(Bytes(csv + "Recorded,A,2,Voice_002,,,Keep English\r\n"), csvFile);
        Assert(csvResult.applied == 1 && TextCatalog.Read(csvResult.data, csvPath)[0].value == "Türkçe: Hello, \"friend\"\nNext" && Encoding.UTF8.GetString(csvResult.data).EndsWith("Keep English\r\n"), "CSV quoted commas, escaped quotes and multiline cells rebase safely");
        string luaPath = "Content/Scripts/Test.lua";
        string lua = "Root={{Cue=\"/VO/X_01\", Args={Text=\"Do not patch\"},Text=\"Hello\"}}";
        var luaResult = TextCatalog.Rebase(Bytes("--[[ {Cue=\"/VO/X_01\",Text=\"Hello\"} ]]\nIgnored=[=[{Cue=\"/VO/X_01\",Text=\"Hello\"}]=]\n" + lua), TextFile(luaPath, lua));
        Assert(luaResult.applied == 1 && TextCatalog.Read(luaResult.data, luaPath).Count == 1 && Encoding.UTF8.GetString(luaResult.data).Contains("Args={Text=\"Do not patch\"}"), "Lua comments, long strings and nested argument Text stay untouched");
        bool malformed = false;
        try { TextCatalog.Rebase(Bytes("{ Id=\"a\" DisplayName=\"Hello\" } /* unfinished"), file); } catch (FormatException) { malformed = true; }
        Assert(malformed, "unterminated comments fail closed");

        string game = Path.Combine(root, "compatibility");
        Write(Path.Combine(game, "x64", "Hades.exe"), "not executable"); Write(SourcePath(game, path), current);
        var payload = new Payload { schema = 2, version = "compat", translated = 2, total = 2, files = new[] { file } };
        var engine = new Engine(game, payload);
        Assert(engine.Check().Contains("1 metin") && !Directory.Exists(Path.Combine(game, Engine.BackupName)), "partial compatibility check reports actual matches without writes");
        engine.Install(); Assert(File.ReadAllBytes(SourcePath(game, path)).SequenceEqual(rebased.data), "partial installation writes only verified matches");
        byte[] patchedBytes = File.ReadAllBytes(SourcePath(game, path));
        string patched = File.ReadAllText(SourcePath(game, path));
        Write(SourcePath(game, path), patched + "// external modification");
        Refuses(() => engine.Restore(), "partial installation preserves external file modifications");
        File.WriteAllBytes(SourcePath(game, path), patchedBytes);
        engine.Restore(); Assert(File.ReadAllBytes(SourcePath(game, path)).SequenceEqual(Bytes(current)), "partial install restores the installed game version byte-identically");
        string next = current + "// newer version\n"; Write(SourcePath(game, path), next);
        engine.Install(); engine.Restore();
        Assert(File.ReadAllBytes(SourcePath(game, path)).SequenceEqual(Bytes(next)) && Directory.GetDirectories(Path.Combine(game, Engine.BackupName, "snapshots")).Length == 2, "newer game version receives a separate backup without overwriting older snapshots");
        bool failed = false;
        try { engine.Install(n => { throw new IOException("Injected failure"); }); } catch (IOException) { failed = true; }
        Assert(failed && File.ReadAllBytes(SourcePath(game, path)).SequenceEqual(Bytes(next)), "partial compatibility installation rolls back after write failure");

        game = Path.Combine(root, "missing-compatible-file"); Write(Path.Combine(game, "x64", "Hades.exe"), "not executable");
        Write(SourcePath(game, path), current);
        var missing = TextFile("Content/Game/Text/en/Missing.en.sjson", original);
        var partial = new Payload { schema = 2, version = "missing", translated = 4, total = 4, files = new[] { file, missing } };
        var partialEngine = new Engine(game, partial); partialEngine.Install(); partialEngine.Restore();
        Assert(File.ReadAllBytes(SourcePath(game, path)).SequenceEqual(Bytes(current)) && !File.Exists(SourcePath(game, missing.path)), "missing files are skipped without creating them and other matches restore");

        game = Path.Combine(root, "malformed-compatible-file"); Write(Path.Combine(game, "x64", "Hades.exe"), "not executable");
        Write(SourcePath(game, path), current); Write(SourcePath(game, missing.path), "{Id=\"a\" DisplayName=\"unfinished");
        partialEngine = new Engine(game, partial); partialEngine.Install(); partialEngine.Restore();
        Assert(File.ReadAllText(SourcePath(game, missing.path)) == "{Id=\"a\" DisplayName=\"unfinished", "unrecognized file syntax stays unchanged while other files install");

        game = Path.Combine(root, "compatibility-font-mismatch"); var font = Fixture(game); font.schema = 2;
        font.files[0].path = "Content/Win/Fonts/en/Fonts.remap"; Write(SourcePath(game, font.files[0].path), "new font mapping");
        font.requirements = new[] { "AlegreyaSCBold", "AlegreyaSCBoldTracking", "UbuntuMonoBold" }.Select(name => {
            string relative = "Content/Win/Fonts/en/" + name + ".xnb"; Write(SourcePath(game, relative), "font fixture");
            return new RequiredFile { path = relative, sha256 = Engine.Hash(Bytes("font fixture")) };
        }).ToArray();
        var fontEngine = new Engine(game, font); fontEngine.Install(); fontEngine.Restore();
        Assert(File.ReadAllText(SourcePath(game, font.files[0].path)) == "new font mapping", "unknown font mapping is preserved without blocking compatible texts");

        game = Path.Combine(root, "compatibility-crash");
        using (var child = Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location, "\"" + game + "\" --compat-crash") { UseShellExecute = false, CreateNoWindow = true }))
        {
            if (!child.WaitForExit(15000)) { child.Kill(); throw new Exception("Compatibility crash timeout"); }
            Assert(child.ExitCode == 73, "compatibility install process stops abruptly after a write");
        }
        var receipt = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Receipt>(File.ReadAllText(Path.Combine(game, Engine.BackupName, "receipt.json")));
        Assert(receipt.state == "prepared" && receipt.backupDirectory.StartsWith("snapshots/"), "compatibility crash preserves durable actual-version backup journal");
        new Engine(game, new Payload { schema = 2, files = receipt.files }).Restore();
        Assert(Original(game, new Payload { files = receipt.files }) && !File.Exists(Path.Combine(game, Engine.UninstallerName)), "new process restores actual-version bytes after compatibility crash");

        game = Path.Combine(root, "no-compatible-text"); Write(Path.Combine(game, "x64", "Hades.exe"), "not executable");
        Write(SourcePath(game, path), "{Id=\"unknown\" DisplayName=\"Keep\"}");
        Refuses(() => new Engine(game, payload).Install(), "zero matches refuses installation");
        Assert(!Directory.Exists(Path.Combine(game, Engine.BackupName)), "zero matches leaves no installation or backup directory");
    }
    static void Main(string[] args)
    {
        string root = Path.GetFullPath(args[0]);
        if (Directory.Exists(root)) throw new Exception("Test directory must be fresh");
        Directory.CreateDirectory(root);
        if (args.Length == 2 && args[1] == "--compat-crash")
        {
            Write(Path.Combine(root, "x64", "Hades.exe"), "not executable");
            const string original = "{Id=\"test\" DisplayName=\"Hello\"}";
            var files = new[] { "A", "B" }.Select(name => TextFile("Content/Game/Text/en/" + name + ".sjson", original)).ToArray();
            foreach (var file in files) Write(SourcePath(root, file.path), "// shifted source\n" + original);
            new Engine(root, new Payload { schema = 2, version = "crash", files = files, translated = 2, total = 2 }).Install(n => Environment.Exit(73));
            throw new Exception("Compatibility crash injection did not run");
        }
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

        string fontGame = Path.Combine(root, "font-remap"); var fontPayload = Fixture(fontGame);
        fontPayload.files[0].path = "Content/Win/Fonts/en/Fonts.remap";
        Write(SourcePath(fontGame, fontPayload.files[0].path), "prefix EN suffix");
        fontPayload.requirements = new[] { "AlegreyaSCBold", "AlegreyaSCBoldTracking", "UbuntuMonoBold" }.Select(name => {
            string path = "Content/Win/Fonts/en/" + name + ".xnb";
            Write(SourcePath(fontGame, path), "font fixture");
            return new RequiredFile { path = path, sha256 = Engine.Hash(Bytes("font fixture")) };
        }).ToArray();
        var fontEngine = new Engine(fontGame, fontPayload);
        Write(SourcePath(fontGame, fontPayload.requirements[0].path), "modified font");
        Refuses(() => fontEngine.Install(), "changed source font stops installation before writes");
        Assert(!Directory.Exists(Path.Combine(fontGame, Engine.BackupName)), "font mismatch leaves no partial installation");
        Write(SourcePath(fontGame, fontPayload.requirements[0].path), "font fixture");
        fontEngine.Install(); Assert(Patched(fontGame, fontPayload), "font remap uses verified backup and installation");
        Write(SourcePath(fontGame, fontPayload.requirements[0].path), "modified after installation");
        fontEngine.Restore(); Assert(Original(fontGame, fontPayload), "font remap restored byte-identically");
        Assert(File.ReadAllText(SourcePath(fontGame, fontPayload.requirements[0].path)) == "modified after installation", "restore preserves font binaries and ignores changed read-only prerequisites");
        foreach (string forbidden in new[] { "Content/Win/Fonts/en/Custom.remap", "Content/Win/Fonts/fr/Fonts.remap", "Content/Win/Fonts/en/UbuntuMonoBold.xnb" })
        {
            fontPayload.files[0].path = forbidden;
            Refuses(() => new Engine(fontGame, fontPayload), "unapproved font path rejected: " + forbidden);
        }
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
        CompatibilityTests(root);
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
