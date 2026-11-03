using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SlavesEncourager
{
    public sealed class FlipDisplay : Viewbox
    {
        readonly StackPanel row = new StackPanel { Orientation = Orientation.Horizontal };
        readonly List<DigitTile> tiles = new List<DigitTile>();
        string previous = "", signature = "";
        public FlipDisplay()
        {
            Child = row; Stretch = Stretch.Uniform; StretchDirection = StretchDirection.DownOnly;
            HorizontalAlignment = HorizontalAlignment.Center; VerticalAlignment = VerticalAlignment.Center;
        }

        public void Set(string value, Settings settings, bool animate)
        {
            string style = settings.Animation + settings.Font + settings.Theme;
            if (signature != style || previous.Length != value.Length)
            {
                row.Children.Clear(); tiles.Clear();
                foreach (char character in value)
                {
                    if (Char.IsDigit(character))
                    {
                        var tile = new DigitTile(character, settings); tiles.Add(tile); row.Children.Add(tile);
                    }
                    else
                    {
                        tiles.Add(null);
                        var label = new TextBlock { Text = character.ToString(), FontSize = Char.IsWhiteSpace(character) ? 15 : 32,
                            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 2, 0), FontFamily = DigitTile.Font(settings.Font) };
                        label.SetResourceReference(TextBlock.ForegroundProperty, "Accent"); row.Children.Add(label);
                    }
                }
                signature = style; previous = value; return;
            }
            for (int i = 0; i < value.Length; i++)
            {
                if (tiles[i] != null) tiles[i].Set(value[i], settings, animate);
                else ((TextBlock)row.Children[i]).Text = value[i].ToString();
            }
            previous = value;
        }

        public void Reset() { signature = ""; }
    }

    public sealed class DigitTile : Grid
    {
        char current;
        int version;
        readonly Settings appearance;
        public DigitTile(char character, Settings settings)
        {
            appearance = settings; Width = 48; Height = 82; Margin = new Thickness(2, 0, 2, 0);
            current = character; Draw(character);
        }
        public static FontFamily Font(string key)
        {
            return new FontFamily(key == "Mono" ? "Consolas" : key == "Classic" ? "Georgia" : "Bahnschrift");
        }

        TextBlock Label(char character)
        {
            var label = new TextBlock { Text = character.ToString(), FontFamily = Font(appearance.Font), FontSize = 57,
                FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, Height = 82, Width = 48,
                LineHeight = 82, LineStackingStrategy = LineStackingStrategy.BlockLineHeight };
            label.SetResourceReference(TextBlock.ForegroundProperty, "Ink"); return label;
        }

        Border Half(char character, bool top)
        {
            var clip = new Border { Width = 48, Height = 41, ClipToBounds = true, CornerRadius = top ? new CornerRadius(7, 7, 0, 0) : new CornerRadius(0, 0, 7, 7), VerticalAlignment = top ? VerticalAlignment.Top : VerticalAlignment.Bottom };
            clip.SetResourceReference(Border.BackgroundProperty, top ? "TileTop" : "TileBottom");
            var canvas = new Canvas { Width = 48, Height = 41 };
            TextBlock label = Label(character);
            Canvas.SetTop(label, top ? 0 : -41);
            canvas.Children.Add(label); clip.Child = canvas; return clip;
        }

        void Draw(char character)
        {
            Children.Clear();
            if (appearance.Animation == "none") { Children.Add(Label(character)); return; }
            Children.Add(Half(character, true)); Children.Add(Half(character, false));
            var seam = new Border { Height = 1, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.8, IsHitTestVisible = false };
            seam.SetResourceReference(Border.BackgroundProperty, "BackgroundBrush"); Children.Add(seam);
            foreach (var side in new[] { HorizontalAlignment.Left, HorizontalAlignment.Right })
            {
                var pin = new Border { Width = 3, Height = 6, HorizontalAlignment = side, VerticalAlignment = VerticalAlignment.Center, CornerRadius = new CornerRadius(1) };
                pin.SetResourceReference(Border.BackgroundProperty, "Muted"); Children.Add(pin);
            }
        }

        public void Set(char character, Settings settings, bool animate)
        {
            if (character == current) return;
            char old = current; current = character; int token = ++version;
            Draw(character);
            if (!animate || settings.Animation == "none" || !SystemParameters.ClientAreaAnimation) return;
            double half = settings.Speed / 2.0;
            if (settings.Animation == "slide")
            {
                var cover = new Border { CornerRadius = new CornerRadius(7), ClipToBounds = true };
                cover.SetResourceReference(Border.BackgroundProperty, "TileBottom");
                var group = new Grid(); var before = Label(old); var after = Label(character);
                var a = new TranslateTransform(); var b = new TranslateTransform(0, 30);
                before.RenderTransform = a; after.RenderTransform = b; after.Opacity = 0;
                group.Children.Add(before); group.Children.Add(after); cover.Child = group; Children.Add(cover);
                var duration = TimeSpan.FromMilliseconds(settings.Speed);
                a.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, -30, duration));
                b.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(30, 0, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                before.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, duration));
                var fade = new DoubleAnimation(0, 1, duration);
                fade.Completed += delegate { if (token == version) Draw(character); };
                after.BeginAnimation(OpacityProperty, fade); return;
            }
            // 先翻旧上半页，再落下新下半页 / Fold the old upper flap, then unfold the new lower flap.
            Children.Clear(); Children.Add(Half(character, true)); Children.Add(Half(old, false));
            Border bottom = Half(character, false), top = Half(old, true);
            var bottomScale = new ScaleTransform(1, 0); var topScale = new ScaleTransform(1, 1);
            bottom.RenderTransform = bottomScale; bottom.RenderTransformOrigin = new Point(0.5, 0);
            top.RenderTransform = topScale; top.RenderTransformOrigin = new Point(0.5, 1);
            Children.Add(bottom); Children.Add(top);
            var fold = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(half)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            fold.Completed += delegate { if (token == version) top.Visibility = Visibility.Hidden; };
            var unfold = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(half)) { BeginTime = TimeSpan.FromMilliseconds(half), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            unfold.Completed += delegate { if (token == version) Draw(character); };
            topScale.BeginAnimation(ScaleTransform.ScaleYProperty, fold);
            bottomScale.BeginAnimation(ScaleTransform.ScaleYProperty, unfold);
        }
    }
}
