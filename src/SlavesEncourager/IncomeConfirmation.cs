using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace SlavesEncourager
{
    // 确认前只操作副本；关闭弹窗不保存 / Work on a draft only; closing the dialog saves nothing.
    public sealed class IncomeConfirmation
    {
        public Window Window { get; private set; }
        public decimal Amount { get; private set; }
        readonly TextBox input;
        readonly TextBlock error;

        public IncomeConfirmation(Settings settings, Window owner, DateTime now)
        {
            decimal estimate = Engine.Calculate(now, settings).Month;
            Interval period = Engine.PayPeriod(now, settings);
            Window = new Window { Owner = owner, Title = L.T("income.title"), Width = 590, SizeToContent = SizeToContent.Height,
                ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FontFamily = owner.FontFamily, FontSize = 13, Icon = owner.Icon };
            foreach (object key in owner.Resources.Keys) Window.Resources[key] = owner.Resources[key];
            L.Apply(Window.Resources); Palette.Apply(Window.Resources, settings.Theme);
            Window.SetResourceReference(Control.BackgroundProperty, "BackgroundBrush"); Window.SetResourceReference(Control.ForegroundProperty, "Ink");
            var body = new StackPanel { Margin = new Thickness(25) };
            body.Children.Add(new TextBlock { Text = L.T("income.title"), FontSize = 23, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            body.Children.Add(new TextBlock { Text = L.T("income.period", period.Start.ToString("yyyy-MM-dd"), period.End.AddDays(-1).ToString("yyyy-MM-dd")), Margin = new Thickness(0, 12, 0, 12), TextWrapping = TextWrapping.Wrap });
            body.Children.Add(new TextBlock { Text = L.T("income.estimate", Engine.Money(estimate, settings)), FontSize = 18, TextWrapping = TextWrapping.Wrap });
            body.Children.Add(new TextBlock { Text = L.T("income.help"), Margin = new Thickness(0, 12, 0, 15), TextWrapping = TextWrapping.Wrap });
            body.Children.Add(new TextBlock { Text = L.T("income.amount", settings.Currency) });
            input = new TextBox { Text = Decimal.Round(estimate, 2, MidpointRounding.AwayFromZero).ToString("F2", CultureInfo.InvariantCulture), MaxLength = 16,
                FontSize = 23, Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0) };
            input.SetResourceReference(Control.BackgroundProperty, "Surface"); input.SetResourceReference(Control.ForegroundProperty, "Ink"); body.Children.Add(input);
            error = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 0, 7) }; error.SetResourceReference(TextBlock.ForegroundProperty, "ExpenseRed"); body.Children.Add(error);
            var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
            var zero = Button("income.zero", delegate { input.Text = "0.00"; }); actions.Children.Add(zero);
            var cancel = Button("action.cancel", delegate { Window.Close(); }); cancel.IsCancel = true; actions.Children.Add(cancel);
            var confirm = Button("income.confirm", delegate
            {
                decimal amount;
                if (!Decimal.TryParse(input.Text.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) || amount < 0m || amount > 100000000m)
                { error.Text = L.T("validation.opening"); return; }
                Amount = amount; Window.DialogResult = true;
            }); confirm.IsDefault = true; actions.Children.Add(confirm); body.Children.Add(actions); Window.Content = body;
            Window.ContentRendered += delegate { input.Focus(); input.SelectAll(); };
        }
        static Button Button(string key, Action click)
        {
            var button = new Button { Content = L.T(key), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(5, 4, 0, 0) };
            button.Click += delegate { click(); }; return button;
        }
    }
}
