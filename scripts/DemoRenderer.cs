using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SlavesEncourager;

// 从真实 WPF 控件导出演示帧，不读取或保存用户设置 / Export real WPF controls without reading or saving user settings.
public static class DemoRenderer
{
    const int Width = 1000, Height = 660, FramesPerSecond = 20, FramesPerLanguage = 60;
    static readonly string[] Languages = { "zh-Hans", "en", "zh-Hant", "ja", "es", "ko" };
    static readonly string[] Labels = { "简体中文", "English", "繁體中文", "日本語", "Español", "한국어" };
    static readonly Typeface Regular = new Typeface(new FontFamily("Microsoft YaHei UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
    static readonly Typeface Bold = new Typeface(new FontFamily("Microsoft YaHei UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

    static Brush Brush(string color) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); }
    static void Text(DrawingContext drawing, string text, double x, double y, double size, string color, double width, bool bold)
    {
        var formatted = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, bold ? Bold : Regular, size, Brush(color), 1.0);
        formatted.MaxTextWidth = width; drawing.DrawText(formatted, new Point(x, y));
    }
    static void Visual(DrawingContext drawing, FrameworkElement element, Rect bounds)
    { drawing.DrawRectangle(new VisualBrush(element) { Stretch = Stretch.Fill }, null, bounds); }
    static void Capture(MainController controller, int language, int frame, string directory)
    {
        controller.Window.UpdateLayout();
        var visual = new DrawingVisual();
        using (DrawingContext drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(Brush("#F1F4FA"), null, new Rect(0, 0, Width, Height));
            drawing.DrawRoundedRectangle(Brush("#E7EDF8"), null, new Rect(582, 22, 392, 566), 30, 30);
            drawing.DrawRoundedRectangle(Brush("#FAE9D6"), null, new Rect(44, 109, 44, 5), 2, 2);
            drawing.DrawImage(Program.LoadIcon(), new Rect(48, 38, 40, 40));
            Text(drawing, "Slaves Encourager", 102, 39, 26, "#27364D", 450, true);
            Text(drawing, "WINDOWS  /  OFFLINE  /  MIT", 103, 76, 11, "#74839A", 430, false);
            Text(drawing, L.T("main.headline"), 48, 134, 32, "#27364D", 490, true);
            Text(drawing, L.T("main.subtitle"), 50, 225, 16, "#74839A", 480, false);
            // 左侧放大卡片、右侧完整窗口均来自同一帧 / Both close-up and full window use the same live frame.
            Visual(drawing, (FrameworkElement)controller.Window.FindName("HeroCard"), new Rect(48, 304, 500, 194));
            Visual(drawing, controller.Window, new Rect(598, 30, 360, 519));
            Text(drawing, "LIVE FLIP DIGITS", 51, 519, 11, "#526FA8", 230, true);
            Text(drawing, "6 LANGUAGES", 779, 559, 11, "#74839A", 180, false);
            for (int i = 0; i < Languages.Length; i++)
            {
                double x = 48 + i * 152;
                drawing.DrawRoundedRectangle(Brush(i == language ? "#526FA8" : "#E7EDF8"), null, new Rect(x, 594, 142, 37), 12, 12);
                var label = new FormattedText(Labels[i], CultureInfo.CurrentCulture, FlowDirection.LeftToRight, i == language ? Bold : Regular, 14, Brush(i == language ? "#FFFFFF" : "#74839A"), 1.0);
                drawing.DrawText(label, new Point(x + (142 - label.Width) / 2, 601));
            }
        }
        var bitmap = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        using (var stream = File.Create(Path.Combine(directory, "frame-" + frame.ToString("D4", CultureInfo.InvariantCulture) + ".png"))) png.Save(stream);
    }

    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length != 1) return 2;
        string directory = Path.GetFullPath(args[0]); Directory.CreateDirectory(directory);
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        try
        {
            var controller = new MainController(Path.Combine(directory, "unused-sample-settings.json"), true, Languages[0]);
            var settings = (Settings)typeof(MainController).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
            settings.Configured = true; settings.Speed = 650;
            controller.Window.WindowStartupLocation = WindowStartupLocation.Manual;
            controller.Window.Left = -1600; controller.Window.Top = 0; controller.Window.Show();
            var editor = new SettingsEditor(settings, controller.Window);
            MethodInfo apply = typeof(MainController).GetMethod("ApplyLanguage", BindingFlags.Instance | BindingFlags.NonPublic);
            editor.LanguageChanged += delegate(string choice) { apply.Invoke(controller, new object[] { choice }); };
            var selector = (ComboBox)editor.Window.FindName("LanguageBox");
            var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle) { Interval = TimeSpan.FromMilliseconds(1000.0 / FramesPerSecond) };
            int frame = 0, language = -1;
            timer.Tick += delegate
            {
                try
                {
                    int next = frame / FramesPerLanguage;
                    if (next != language)
                    {
                        language = next;
                        selector.SelectedItem = selector.Items.Cast<ComboBoxItem>().First(item => (string)item.Tag == Languages[language]);
                        settings.Goals = new System.Collections.Generic.List<RewardGoal> { new RewardGoal(L.T("sample.coffee"), 150m), new RewardGoal(L.T("goal.default"), 3000m) };
                        controller.Refresh(false);
                    }
                    Capture(controller, language, frame, directory); frame++;
                    if (frame >= FramesPerLanguage * Languages.Length)
                    {
                        timer.Stop(); editor.Window.Close(); controller.Window.Close(); app.Shutdown(0);
                    }
                }
                catch (Exception ex)
                {
                    timer.Stop(); File.WriteAllText(Path.Combine(directory, "render-error.txt"), ex.ToString(), new UTF8Encoding(false)); app.Shutdown(1);
                }
            };
            timer.Start();
            int result = app.Run();
            if (result == 0) File.WriteAllText(Path.Combine(directory, "capture.txt"), "Real WPF controls; settings language selector events; built-in sample data only.\n1000x660; 20 fps; 360 frames; 18 seconds; zh-Hans/en/zh-Hant/ja/es/ko.\n", new UTF8Encoding(false));
            return result;
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(directory, "render-error.txt"), ex.ToString(), new UTF8Encoding(false)); return 1;
        }
    }
}
