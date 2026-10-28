using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using HadesTurkce;

static class InstallerUiTests
{
    static int passed;
    static void Assert(bool value, string label)
    { if (!value) throw new Exception(label); passed++; Console.WriteLine("PASS " + label); }
    static void BoundsCheck(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (!control.Visible) continue;
            var scroll = parent as ScrollableControl;
            if (scroll == null || !scroll.AutoScroll)
                if (control.Right > parent.ClientSize.Width + 1 || control.Bottom > parent.ClientSize.Height + 1)
                    throw new Exception("Clipped control: " + control.GetType().Name + " " + control.Text);
            var button = control as Button;
            if (button != null)
            {
                Size text = TextRenderer.MeasureText(button.Text, button.Font);
                if (text.Width > button.ClientSize.Width || text.Height > button.ClientSize.Height)
                    throw new Exception("Clipped button label: " + button.Text);
            }
            BoundsCheck(control);
        }
    }
    static void Layout(int width, int height, float scale)
    {
        using (var form = new InstallerForm(new Payload { version = "test", translated = 1, total = 1 }))
        {
            form.ShowInTaskbar = false; form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-20000, -20000); form.ClientSize = new Size(width, height);
            form.Show(); Application.DoEvents();
            if (scale != 1)
            {
                form.Scale(new SizeF(scale, scale));
                form.ClientSize = new Size((int)(width * scale), (int)(height * scale));
                typeof(InstallerForm).GetMethod("FitToScreen", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[0]);
            }
            form.Location = new Point(-20000, -20000); Application.DoEvents();
            BoundsCheck(form);
            Assert(form.Height <= Screen.FromControl(form).WorkingArea.Height, "window fits available screen: " + width + "x" + height + " scale " + scale);
            var shell = (TableLayoutPanel)form.Controls[0];
            var viewport = (Panel)shell.GetControlFromPosition(1, 0);
            var main = (TableLayoutPanel)viewport.Controls[0];
            if (viewport.VerticalScroll.Visible)
            {
                Control lower = main.GetControlFromPosition(0, 9);
                viewport.ScrollControlIntoView(lower);
                Application.DoEvents();
                Rectangle visible = viewport.RectangleToClient(lower.RectangleToScreen(lower.ClientRectangle));
                Assert(visible.Top >= 0 && visible.Bottom <= viewport.ClientSize.Height, "bottom controls remain reachable by scrolling");
            }
            var actions = (FlowLayoutPanel)main.GetControlFromPosition(0, 8);
            Assert(actions.Controls.Count == 3 && actions.Controls.Cast<Control>().All(c => c.Enabled && c.CanSelect), "check/install/restore remain available");
            form.Close();
        }
    }
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Layout(640, 420, 1); Layout(900, 480, 1); Layout(1040, 600, 1); Layout(1440, 900, 1);
        // Geometry simulations complement actual screen layout; these do not claim a different OS DPI.
        Layout(1040, 600, 1.25f); Layout(1040, 600, 1.5f); Layout(1040, 600, 2);
        using (var hero = new HeroPanel())
        {
            Assert(hero.HasSlideshow, "three original fallback scenes are available without game assets");
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var timer = (Timer)typeof(HeroPanel).GetField("timer", flags).GetValue(hero);
            var tick = typeof(Timer).GetMethod("OnTick", flags);
            hero.Playing = false;
            for (int i = 0; i < 110; i++) tick.Invoke(timer, new object[] { EventArgs.Empty });
            Assert((int)typeof(HeroPanel).GetField("current", flags).GetValue(hero) == 0, "paused slideshow does not advance");
            hero.Playing = true;
            for (int i = 0; i < 101; i++) tick.Invoke(timer, new object[] { EventArgs.Empty });
            Assert((int)typeof(HeroPanel).GetField("current", flags).GetValue(hero) == 1, "slideshow advances and completes its fade");
        }
        Console.WriteLine("UI TOTAL " + passed);
    }
}
