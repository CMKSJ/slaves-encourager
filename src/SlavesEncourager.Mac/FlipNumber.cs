using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System.Diagnostics;
using System.Globalization;

namespace SlavesEncourager.Mac;

// 按数字拆分上下半页；滑动与无动效共享格式 / Split each digit into clock halves; slide/plain share formatting.
public sealed class FlipNumber : Control
{
    string value = "", previous = "";
    Settings appearance = new();
    readonly Stopwatch clock = new();
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    public FlipNumber()
    {
        Height = 96; MinHeight = 96;
        timer.Tick += (_, _) => { InvalidateVisual(); if (clock.ElapsedMilliseconds >= appearance.Speed) timer.Stop(); };
        DetachedFromVisualTree += (_, _) => timer.Stop();
    }
    public void Set(string next, Settings settings)
    {
        if (value == next && appearance.Theme == settings.Theme && appearance.Font == settings.Font && appearance.Animation == settings.Animation) return;
        previous = value; value = next; appearance = settings.Copy(); clock.Restart();
        if (appearance.Animation != "none" && previous.Length == value.Length) timer.Start();
        InvalidateVisual();
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context); if (value.Length == 0) return;
        double width = value.Sum(c => char.IsDigit(c) ? 52 : 27), scale = Math.Min(1, Bounds.Width / width);
        using (context.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation((Bounds.Width - width * scale) / 2, (96 - 82 * scale) / 2)))
        {
            double x = 0, phase = Math.Clamp(clock.Elapsed.TotalMilliseconds / appearance.Speed, 0, 1);
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i]; var old = previous.Length == value.Length ? previous[i] : c;
                double w = char.IsDigit(c) ? 48 : 23;
                if (char.IsDigit(c)) context.DrawRectangle(Ui.Alt, null, new Rect(x, 0, w, 82), 7, 7);
                if (c == old || phase >= 1 || appearance.Animation == "none") Glyph(context, c, x, w);
                else if (appearance.Animation == "slide")
                {
                    using (context.PushClip(new Rect(x, 0, w, 82)))
                    {
                        using (context.PushTransform(Matrix.CreateTranslation(0, -82 * phase))) Glyph(context, old, x, w);
                        using (context.PushTransform(Matrix.CreateTranslation(0, 82 * (1 - phase)))) Glyph(context, c, x, w);
                    }
                }
                else
                {
                    using (context.PushClip(new Rect(x, 0, w, 41))) Glyph(context, c, x, w);
                    using (context.PushClip(new Rect(x, 41, w, 41))) Glyph(context, old, x, w);
                    double sy = phase < .5 ? 1 - phase * 2 : (phase - .5) * 2;
                    using (context.PushTransform(Matrix.CreateTranslation(0, -41) * Matrix.CreateScale(1, sy) * Matrix.CreateTranslation(0, 41)))
                    using (context.PushClip(new Rect(x, phase < .5 ? 0 : 41, w, 41)))
                    {
                        context.DrawRectangle(Ui.Alt, null, new Rect(x, 0, w, 82), 7, 7);
                        Glyph(context, phase < .5 ? old : c, x, w);
                    }
                }
                if (char.IsDigit(c)) context.DrawLine(new Pen(Ui.Stroke, 1), new Point(x, 41), new Point(x + w, 41));
                x += w + 4;
            }
        }
    }
    void Glyph(DrawingContext context, char c, double x, double width)
    {
        string font = appearance.Font == "Classic" ? "Georgia" : appearance.Font == "Mono" ? "Menlo,Consolas,monospace" : "DIN Alternate,Bahnschrift,Helvetica Neue";
        var text = new FormattedText(c.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(font, FontStyle.Normal, FontWeight.SemiBold), char.IsDigit(c) ? 57 : 29, Ui.Ink);
        context.DrawText(text, new Point(x + (width - text.Width) / 2, (82 - text.Height) / 2));
    }
}
