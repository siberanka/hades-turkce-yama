using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HadesTurkce
{
    static class Palette
    {
        public static readonly Color Night = Color.FromArgb(17, 19, 23);
        public static readonly Color Panel = Color.FromArgb(27, 30, 35);
        public static readonly Color Field = Color.FromArgb(37, 40, 46);
        public static readonly Color Gold = Color.FromArgb(217, 181, 108);
        public static readonly Color Text = Color.FromArgb(241, 235, 224);
        public static readonly Color Muted = Color.FromArgb(172, 170, 163);
        public static readonly Color Red = Color.FromArgb(132, 42, 53);
    }

    sealed class HeroPanel : Panel
    {
        readonly List<Image> slides = new List<Image>();
        readonly Timer timer = new Timer { Interval = 60 };
        int current, ticks;
        float fade;
        public bool Playing = true;
        public bool HasSlideshow { get { return slides.Count > 1; } }
        public HeroPanel()
        {
            DoubleBuffered = true;
            foreach (string name in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (!name.StartsWith("art.slide", StringComparison.Ordinal) || !name.EndsWith(".jpg", StringComparison.Ordinal)) continue;
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                using (var image = Image.FromStream(stream)) slides.Add(new Bitmap(image));
            }
            timer.Tick += delegate
            {
                if (!Playing || slides.Count < 2) return;
                ticks++;
                if (ticks > 85) fade = Math.Min(1, (ticks - 85) / 16f);
                if (fade >= 1) { current = (current + 1) % slides.Count; ticks = 0; fade = 0; }
                if (ticks > 84 || ticks == 0) Invalidate();
            };
            timer.Start();
        }
        void DrawSlide(Graphics g, Image image, float opacity)
        {
            float ratio = Math.Max((float)Width / image.Width, (float)Height / image.Height);
            int width = (int)(image.Width * ratio), height = (int)(image.Height * ratio);
            var rect = new Rectangle((Width - width) / 2, (Height - height) / 2, width, height);
            using (var attributes = new System.Drawing.Imaging.ImageAttributes())
            {
                var matrix = new System.Drawing.Imaging.ColorMatrix { Matrix33 = opacity };
                attributes.SetColorMatrix(matrix);
                g.DrawImage(image, rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            if (slides.Count > 0)
            {
                DrawSlide(g, slides[current], 1);
                if (fade > 0) DrawSlide(g, slides[(current + 1) % slides.Count], fade);
            }
            else
            {
                using (var background = new LinearGradientBrush(ClientRectangle, Palette.Red, Palette.Night, 70f)) g.FillRectangle(background, ClientRectangle);
                using (var pen = new Pen(Color.FromArgb(60, Palette.Gold), 2))
                for (int i = -Height; i < Width + Height; i += 72) g.DrawLine(pen, i, 0, i + Height, Height);
            }
            using (var overlay = new LinearGradientBrush(ClientRectangle, Color.FromArgb(35, 8, 10, 14), Color.FromArgb(245, 12, 14, 18), 90f)) g.FillRectangle(overlay, ClientRectangle);
            using (var shade = new SolidBrush(Color.FromArgb(60, 10, 10, 16))) g.FillRectangle(shade, ClientRectangle);
            using (var pen = new Pen(Color.FromArgb(155, Palette.Gold), 1))
            {
                g.DrawRectangle(pen, 18, 18, Math.Max(1, Width - 37), Math.Max(1, Height - 37));
                int cx = Width / 2;
                g.DrawPolygon(pen, new[] { new Point(cx, 38), new Point(cx + 13, 52), new Point(cx, 66), new Point(cx - 13, 52) });
            }
            int left = 34, top = Height - 242;
            using (var title = new Font("Georgia", 34, FontStyle.Bold))
            using (var subtitle = new Font("Segoe UI", 16))
            using (var body = new Font("Segoe UI", 10))
            using (var gold = new SolidBrush(Palette.Gold))
            using (var white = new SolidBrush(Palette.Text))
            using (var muted = new SolidBrush(Palette.Muted))
            {
                g.DrawString("HADES", title, white, left - 3, top + 27);
                g.DrawString("TÜRKÇE YAMA", subtitle, gold, left, top + 86);
                g.DrawString("Çeviri ve proje: siberanka", body, muted, left, top + 168);
            }
            if (slides.Count > 1)
            {
                for (int i = 0; i < slides.Count; i++)
                    using (var brush = new SolidBrush(i == current ? Palette.Gold : Color.FromArgb(100, Palette.Text)))
                        g.FillEllipse(brush, left + i * 18, Height - 42, 6, 6);
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { timer.Dispose(); foreach (var image in slides) image.Dispose(); }
            base.Dispose(disposing);
        }
    }

    sealed class LocalMusic : IDisposable
    {
        [DllImport("winmm.dll", CharSet = CharSet.Unicode)] static extern int mciSendString(string command, StringBuilder result, int length, IntPtr callback);
        readonly string alias = "hadestr" + Guid.NewGuid().ToString("N");
        string file;
        public bool Playing { get; private set; }
        public void Toggle(IWin32Window owner)
        {
            if (Playing) { mciSendString("pause " + alias, null, 0, IntPtr.Zero); Playing = false; return; }
            if (file == null)
            {
                using (var dialog = new OpenFileDialog { Title = "Bilgisayarınızdan bir müzik seçin", Filter = "Ses dosyaları (*.mp3;*.wav)|*.mp3;*.wav", CheckFileExists = true })
                {
                    if (dialog.ShowDialog(owner) != DialogResult.OK) return;
                    string selected = Path.GetFullPath(dialog.FileName);
                    if (selected.Contains("\"")) throw new IOException("Geçersiz müzik dosyası yolu.");
                    if (mciSendString("open \"" + selected + "\" alias " + alias, null, 0, IntPtr.Zero) != 0)
                        throw new IOException("Bu ses dosyası Windows tarafından oynatılamıyor.");
                    file = selected;
                    mciSendString("setaudio " + alias + " volume to 250", null, 0, IntPtr.Zero);
                }
            }
            if (mciSendString("play " + alias + " repeat", null, 0, IntPtr.Zero) != 0 && mciSendString("play " + alias, null, 0, IntPtr.Zero) != 0)
                throw new IOException("Müzik başlatılamadı.");
            Playing = true;
        }
        public void Dispose() { mciSendString("close " + alias, null, 0, IntPtr.Zero); }
    }

    sealed class InstallerForm : Form
    {
        readonly Payload payload;
        readonly ComboBox folder = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown, FlatStyle = FlatStyle.Flat, BackColor = Palette.Field, ForeColor = Palette.Text };
        readonly TextBox status = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.None, BorderStyle = BorderStyle.None, BackColor = Palette.Panel, ForeColor = Palette.Text };
        readonly FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        readonly Label headline = new Label { Dock = DockStyle.Fill, ForeColor = Palette.Gold, Text = "Kuruluma hazırlık" };
        readonly ProgressBar progress = new ProgressBar { Dock = DockStyle.Fill, Style = ProgressBarStyle.Blocks, Maximum = 100 };
        readonly LocalMusic music = new LocalMusic();
        Button browse;
        bool busy, checkedFolder;

        static Label Label(string text, float size, Color color, bool bold = false)
        { return new Label { Text = text, Dock = DockStyle.Fill, ForeColor = color, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), AutoEllipsis = true }; }
        static Button Button(string text, bool primary = false)
        {
            var button = new Button { Text = text, AutoSize = false, FlatStyle = FlatStyle.Flat, BackColor = primary ? Palette.Red : Palette.Field, ForeColor = Palette.Text, Cursor = Cursors.Hand, Height = 40, Width = 140, Margin = new Padding(0, 0, 10, 0), Font = new Font("Segoe UI", 10, FontStyle.Bold), UseVisualStyleBackColor = false };
            button.FlatAppearance.BorderColor = primary ? Color.FromArgb(171, 76, 83) : Color.FromArgb(69, 67, 63);
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(160, 51, 62) : Color.FromArgb(53, 55, 61);
            return button;
        }
        public InstallerForm(Payload patch)
        {
            payload = patch;
            Text = "Hades Türkçe Yama · siberanka";
            Font = new Font("Segoe UI", 10);
            BackColor = Palette.Night;
            ForeColor = Palette.Text;
            ClientSize = new Size(1040, 600);
            MinimumSize = new Size(1000, 638);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37)); shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63));
            var hero = new HeroPanel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            shell.Controls.Add(hero, 0, 0);
            var main = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(30, 36, 30, 20), ColumnCount = 1, RowCount = 11, Margin = Padding.Empty };
            int[] heights = { 58, 52, 27, 44, 46, 28, 8, 0, 50, 40, 30 };
            foreach (int height in heights) main.RowStyles.Add(height == 0 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.Absolute, height));
            main.Controls.Add(Label("Kurulum", 27, Palette.Text, true), 0, 0);
            string coverage = patch.total > 0 ? (100.0 * patch.translated / patch.total).ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) : "0";
            main.Controls.Add(Label(String.Format("{0}  ·  Çeviri %{1}", patch.version, coverage), 10, Palette.Gold), 0, 1);
            main.Controls.Add(Label("OYUN KLASÖRÜ", 9, Palette.Muted, true), 0, 2);
            var pathRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            browse = Button("Gözat…"); browse.Width = 88; browse.Height = 30; browse.Margin = new Padding(8, 0, 0, 0);
            browse.Click += delegate { using (var dialog = new FolderBrowserDialog { Description = "Content ve x64 klasörlerini içeren Hades klasörünü seçin." }) if (dialog.ShowDialog() == DialogResult.OK) folder.Text = dialog.SelectedPath; };
            folder.TextChanged += delegate { checkedFolder = false; headline.Text = "Uyumluluk kontrolü bekleniyor"; };
            pathRow.Controls.Add(folder, 0, 0); pathRow.Controls.Add(browse, 1, 0); main.Controls.Add(pathRow, 0, 3);
            main.Controls.Add(Label("Orijinal dosyalar kurulumdan önce yedeklenir.", 9, Palette.Muted), 0, 4);
            main.Controls.Add(headline, 0, 5);
            progress.Margin = new Padding(0, 0, 0, 2); main.Controls.Add(progress, 0, 6);
            var statusCard = new Panel { Dock = DockStyle.Fill, BackColor = Palette.Panel, Padding = new Padding(14), Margin = new Padding(0, 10, 0, 16) };
            status.Text = "Steam ve Epic kurulumları aranıyor…";
            statusCard.Controls.Add(status); main.Controls.Add(statusCard, 0, 7);
            AddButton("Uyumluluğu denetle", "check", false, 160); AddButton("Çeviriyi kur", "install", true, 125); AddButton("Çeviriyi kaldır", "restore", false, 145);
            main.Controls.Add(buttons, 0, 8);
            var extras = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            var musicButton = Button("♫  Müzik: kapalı"); musicButton.Width = 170; musicButton.Height = 30; musicButton.Font = new Font("Segoe UI", 9);
            musicButton.Click += delegate { try { music.Toggle(this); musicButton.Text = music.Playing ? "♫  Müziği sustur" : "♫  Müzik: kapalı"; } catch (Exception e) { status.Text = e.Message; } };
            var slidesButton = Button("Slaytları duraklat"); slidesButton.Width = 150; slidesButton.Height = 30; slidesButton.Font = new Font("Segoe UI", 9);
            slidesButton.Click += delegate { hero.Playing = !hero.Playing; slidesButton.Text = hero.Playing ? "Slaytları duraklat" : "Slaytları sürdür"; };
            extras.Controls.Add(musicButton); if (hero.HasSlideshow) extras.Controls.Add(slidesButton); else slidesButton.Dispose(); main.Controls.Add(extras, 0, 9);
            main.Controls.Add(Label("Resmî olmayan hayran projesi.", 8, Palette.Muted), 0, 10);
            shell.Controls.Add(main, 1, 0); Controls.Add(shell);
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (busy) e.Cancel = true; };
            Shown += delegate { Discover(); };
        }
        void OnUi(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke(action); } catch (InvalidOperationException) { }
        }
        void Discover()
        {
            Task.Factory.StartNew(delegate
            {
                string[] paths;
                try { paths = Discovery.Find(); } catch { paths = new string[0]; }
                OnUi(delegate
                {
                    if (!String.IsNullOrWhiteSpace(folder.Text) || busy) return;
                    folder.Items.AddRange(paths);
                    if (paths.Length > 0)
                    {
                        folder.SelectedIndex = 0;
                        status.Text = "Hades bulundu. Kurulum için uyumluluğu denetleyin.";
                    }
                    else { headline.Text = "Oyun klasörünü seçin"; status.Text = "Steam, Epic ve varsayılan klasörlerde Hades bulunamadı.\r\n\r\nGözat düğmesiyle Content ve x64 klasörlerini içeren ana oyun klasörünü seçin."; }
                });
            });
        }
        void AddButton(string title, string action, bool primary, int width)
        {
            var button = Button(title, primary); button.Width = width;
            button.Click += delegate
            {
                if (String.IsNullOrWhiteSpace(folder.Text)) { status.Text = "Önce oyun klasörünü seçin."; return; }
                if (action == "install" && !checkedFolder) { status.Text = "Kurulumdan önce Uyumluluğu denetle düğmesini kullanın."; return; }
                string confirmation = payload.preview ? "Çeviri henüz tamamlanmadı ve oyun içinde doğrulanmadı. Çevrilmeyen metinler İngilizce kalacak.\r\n\r\nOrijinal dosyalar yedeklenerek kurulsun mu?" : "Orijinal dosyalar yedeklenerek çeviri kurulsun mu?";
                if (action == "install" && MessageBox.Show(confirmation, Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK) return;
                if (action == "restore" && MessageBox.Show("Çeviri kaldırılıp orijinal dosyalar geri yüklensin mi?", Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
                string selected = folder.Text;
                busy = true; buttons.Enabled = false; folder.Enabled = false; browse.Enabled = false;
                progress.Style = ProgressBarStyle.Marquee;
                headline.Text = action == "install" ? "Yedekleme ve kurulum" : action == "restore" ? "Orijinal dosyalar geri yükleniyor" : "Dosyalar denetleniyor";
                status.Text = "Kaynak dosyalar ve bütünlük kayıtları kontrol ediliyor…";
                status.ScrollBars = ScrollBars.None;
                Task.Factory.StartNew(delegate
                {
                    string result; bool success = false;
                    try
                    {
                        var engine = new Engine(selected, payload);
                        if (action == "install") { engine.Install(); result = "Kurulum tamamlandı.\r\n\r\n" + engine.CompatibilitySummary + "\r\n\r\nOyunda İngilizce dilini seçin. Eşleşmeyen metinler özgün hâlinde kalır.\r\n\r\nKaldırmak için oyun klasöründeki Hades-Turkce-Yamayi-Kaldir.exe dosyasını açabilirsiniz."; }
                        else if (action == "restore") { engine.Restore(); result = "Çeviri kaldırıldı.\r\n\r\nOrijinal dosyalar geri yüklendi ve doğrulandı. Kaldırma aracı silindi; yedekler korundu."; }
                        else result = engine.Check();
                        success = true;
                    }
                    catch (Exception error) { result = "İşlem tamamlanamadı.\r\n\r\n" + error.Message; }
                    OnUi(delegate
                    {
                        status.Text = result; busy = false; buttons.Enabled = true; folder.Enabled = true; browse.Enabled = true;
                        if ((status.GetLineFromCharIndex(status.TextLength) + 1) * status.Font.Height > status.ClientSize.Height)
                            status.ScrollBars = ScrollBars.Vertical;
                        progress.Style = ProgressBarStyle.Blocks; progress.Value = success ? 100 : 0;
                        checkedFolder = success && action == "check";
                        headline.Text = success ? (action == "check" ? "Uyumluluk doğrulandı" : "İşlem tamamlandı") : "İşlem durduruldu";
                    });
                });
            };
            buttons.Controls.Add(button);
        }
        protected override void Dispose(bool disposing)
        { if (disposing) music.Dispose(); base.Dispose(disposing); }
    }
}
