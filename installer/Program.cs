using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: AssemblyTitle("Hades Türkçe Yama")]
[assembly: AssemblyDescription("Yedekli Türkçe yama kurulum aracı")]
[assembly: AssemblyCompany("siberanka")]
[assembly: AssemblyProduct("Hades Türkçe Yama")]
[assembly: AssemblyCopyright("Copyright © 2026 siberanka")]
[assembly: AssemblyVersion("1.0.0.0")]

namespace HadesTurkce
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                if (args.Length == 4 && args[0] == "--cleanup") return Cleanup.Run(args[1], args[2], args[3]);
                Payload payload;
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.json"))
                using (var reader = new StreamReader(stream)) payload = Engine.ReadPayload(reader.ReadToEnd());
                if (args.Length == 2)
                {
                    var engine = new Engine(args[1], payload);
                    switch (args[0])
                    {
                        case "--check": Console.WriteLine(engine.Check()); break;
                        case "--install": engine.Install(); break;
                        case "--restore": engine.Restore(); Cleanup.Schedule(args[1], engine.UninstallerHash); break;
                        default: throw new ArgumentException("Kullanım: --check|--install|--restore OYUN_KLASÖRÜ");
                    }
                    return 0;
                }
                if (args.Length != 0) throw new ArgumentException("Geçersiz komut satırı.");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                if (Cleanup.IsUninstaller)
                {
                    if (MessageBox.Show("Türkçe yama kaldırılacak ve doğrulanmış yedekten orijinal dosyalar geri yüklenecek. Devam edilsin mi?", "Hades Türkçe Yama · Kaldır", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return 0;
                    string gameRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var engine = new Engine(gameRoot, payload);
                    engine.Restore();
                    MessageBox.Show("Orijinal dosyalar geri yüklendi. Bu pencereyi kapattığınızda kaldırma aracı silinecek. Yedekler korunmuştur.", "Kaldırma tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Cleanup.Schedule(gameRoot, engine.UninstallerHash);
                    return 0;
                }
                Application.Run(new InstallerForm(payload));
                return 0;
            }
            catch (Exception error)
            {
                if (args.Length == 0) MessageBox.Show(error.Message, "İşlem tamamlanamadı", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else Console.Error.WriteLine(error.Message);
                return 1;
            }
        }
    }

}
