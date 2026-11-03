using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SlavesEncourager
{
    // 红条表示待覆盖比例；显示估算分配而非真实付款 / Red bars show the uncovered share, not actual bill payments.
    public sealed class ExpenseWindow
    {
        sealed class Row
        {
            public TextBlock Name, Amount, State;
            public ProgressBar Bar;
        }
        public Window Window { get; private set; }
        readonly FlipDisplay remaining = new FlipDisplay();
        readonly TextBlock covered = new TextBlock(), reward = new TextBlock(), summary = new TextBlock();
        readonly TextBlock badge = new TextBlock(), empty = new TextBlock { TextWrapping = TextWrapping.Wrap };
        readonly ProgressBar totalBar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 7, BorderThickness = new Thickness(0), Margin = new Thickness(0, 12, 0, 12) };
        readonly StackPanel items = new StackPanel();
        readonly List<Row> rows = new List<Row>();
        Settings source;
        string language;

        public ExpenseWindow(Window owner, Action edit)
        {
            Window = new Window { Owner = owner, Title = L.T("expense.title"), Width = 690, Height = 740, MinWidth = 610, MinHeight = 530,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, FontFamily = owner.FontFamily, FontSize = 12, Icon = owner.Icon };
            Window.Height = Math.Min(Window.Height, SystemParameters.WorkArea.Height - 12);
            foreach (object key in owner.Resources.Keys) Window.Resources[key] = owner.Resources[key];
            Window.SetResourceReference(Control.BackgroundProperty, "BackgroundBrush"); Window.SetResourceReference(Control.ForegroundProperty, "Ink");
            var root = new DockPanel { Margin = new Thickness(24) };
            var header = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var heading = Label("expense.title", 25); heading.FontWeight = FontWeights.SemiBold;
            header.Children.Add(heading);
            var editButton = new Button { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(13, 8, 13, 8) };
            editButton.SetResourceReference(ContentControl.ContentProperty, "expense.edit"); editButton.Click += delegate { if (edit != null) edit(); };
            Grid.SetColumn(editButton, 1); header.Children.Add(editButton); DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            var body = new StackPanel();
            var top = new StackPanel(); var topHeader = new Grid();
            topHeader.Children.Add(Label("expense.pending", 13));
            badge.HorizontalAlignment = HorizontalAlignment.Right; badge.SetResourceReference(TextBlock.ForegroundProperty, "Muted"); topHeader.Children.Add(badge); top.Children.Add(topHeader);
            top.Children.Add(new Border { Height = 90, Child = remaining, Margin = new Thickness(0, 8, 0, 0) });
            totalBar.SetResourceReference(Control.ForegroundProperty, "ExpenseRed"); totalBar.SetResourceReference(Control.BackgroundProperty, "ExpenseTrack");
            totalBar.SetResourceReference(AutomationProperties.NameProperty, "expense.pending"); top.Children.Add(totalBar);
            summary.TextWrapping = TextWrapping.Wrap; summary.SetResourceReference(TextBlock.ForegroundProperty, "ExpenseRed"); top.Children.Add(summary);
            body.Children.Add(Card(top, "Surface"));
            var metrics = new Grid { Margin = new Thickness(0, 12, 0, 15) };
            metrics.ColumnDefinitions.Add(new ColumnDefinition()); metrics.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) }); metrics.ColumnDefinitions.Add(new ColumnDefinition());
            var a = new StackPanel(); a.Children.Add(Label("expense.covered", 11)); covered.FontSize = 22; a.Children.Add(covered); metrics.Children.Add(Card(a, "Surface"));
            var b = new StackPanel(); b.Children.Add(Label("expense.rewardBalance", 11)); reward.FontSize = 22; b.Children.Add(reward); var bCard = Card(b, "Surface"); Grid.SetColumn(bCard, 2); metrics.Children.Add(bCard);
            body.Children.Add(metrics); body.Children.Add(items);
            empty.SetResourceReference(TextBlock.TextProperty, "expense.emptyEditor"); empty.Margin = new Thickness(4, 15, 4, 20); body.Children.Add(empty);
            var help = Label("expense.explanation", 11); help.TextWrapping = TextWrapping.Wrap; help.SetResourceReference(TextBlock.ForegroundProperty, "Muted"); help.Margin = new Thickness(2, 10, 2, 0); body.Children.Add(help);
            scroll.Content = body; root.Children.Add(scroll); Window.Content = root;
            Window.PreviewMouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.ChangedButton != MouseButton.Left || Mouse.LeftButton != MouseButtonState.Pressed) return;
                DependencyObject origin = e.OriginalSource as DependencyObject;
                while (origin != null)
                {
                    if (origin is ButtonBase || origin is ScrollBar || origin is Thumb) return;
                    var content = origin as FrameworkContentElement;
                    origin = content != null ? content.Parent : origin is Visual ? VisualTreeHelper.GetParent(origin) : LogicalTreeHelper.GetParent(origin);
                }
                Window.DragMove(); e.Handled = true;
            };
        }

        static TextBlock Label(string key, double size)
        { var label = new TextBlock { FontSize = size }; label.SetResourceReference(TextBlock.TextProperty, key); return label; }
        static Border Card(UIElement content, string brush)
        {
            var card = new Border { CornerRadius = new CornerRadius(15), Padding = new Thickness(17), Child = content, BorderThickness = new Thickness(1) };
            card.SetResourceReference(Border.BackgroundProperty, brush); card.SetResourceReference(Border.BorderBrushProperty, "Stroke"); return card;
        }
        void Build(Settings settings)
        {
            source = settings; language = L.Code; L.Apply(Window.Resources); Palette.Apply(Window.Resources, settings.Theme); Window.Title = L.T("expense.title");
            rows.Clear(); items.Children.Clear();
            foreach (RewardGoal item in settings.ExpenseItems)
            {
                var row = new Row { Name = new TextBlock { Text = item.Name, FontSize = 15, FontWeight = FontWeights.SemiBold, MaxWidth = 390, HorizontalAlignment = HorizontalAlignment.Left, TextTrimming = TextTrimming.CharacterEllipsis },
                    Amount = new TextBlock { FontSize = 15, HorizontalAlignment = HorizontalAlignment.Right }, State = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap },
                    Bar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 5, BorderThickness = new Thickness(0), Margin = new Thickness(0, 12, 0, 9) } };
                row.Amount.SetResourceReference(TextBlock.ForegroundProperty, "ExpenseRed"); row.State.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
                row.Bar.SetResourceReference(Control.ForegroundProperty, "ExpenseRed"); row.Bar.SetResourceReference(Control.BackgroundProperty, "ExpenseTrack"); row.Bar.SetResourceReference(AutomationProperties.NameProperty, "expense.pending");
                var panel = new StackPanel(); var line = new Grid();
                line.ColumnDefinitions.Add(new ColumnDefinition()); line.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.Name.Margin = new Thickness(0, 0, 12, 0); Grid.SetColumn(row.Amount, 1);
                line.Children.Add(row.Name); line.Children.Add(row.Amount); panel.Children.Add(line); panel.Children.Add(row.Bar); panel.Children.Add(row.State);
                var card = Card(panel, "Surface"); card.Margin = new Thickness(0, 0, 0, 10); items.Children.Add(card); rows.Add(row);
            }
            empty.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed; remaining.Reset();
        }
        public void Update(Settings settings, decimal income, bool privacy, bool demo, bool animate)
        {
            if (!Object.ReferenceEquals(settings, source) || language != L.Code) Build(settings);
            List<ExpenseProgress> progress = Engine.ExpenseCoverage(income, settings);
            decimal total = Engine.ExpenseTotal(settings), left = progress.Sum(item => item.Remaining);
            var displaySettings = settings.Copy(); displaySettings.Decimals = 2; displaySettings.Pattern = "{symbol}{amount}";
            remaining.Set(privacy ? "••••••" : Engine.Format(left, displaySettings), displaySettings, animate);
            totalBar.Value = privacy || total == 0 ? 0 : (double)(left / total) * 100;
            covered.Text = privacy ? "••••" : Engine.Money(total - left, settings);
            reward.Text = privacy ? "••••" : Engine.Money(Engine.RewardIncome(income, settings), settings);
            badge.Text = demo ? L.T("main.demoBadge") : L.T("expense.monthly");
            Interval period = Engine.PayPeriod(DateTime.Now, settings);
            badge.ToolTip = demo ? null : L.T("income.period", period.Start.ToString("yyyy-MM-dd"), period.End.AddDays(-1).ToString("yyyy-MM-dd"));
            summary.Text = privacy ? L.T("expense.hidden") : total == 0 ? L.T("expense.emptyTotal") : left == 0 ? L.T("expense.allCovered") : L.T("expense.summary", Engine.Money(left, settings), Engine.Money(total, settings));
            for (int i = 0; i < progress.Count; i++)
            {
                rows[i].Amount.Text = privacy ? "••••" : L.T("expense.remainingValue", Engine.Money(progress[i].Remaining, settings));
                rows[i].Bar.Value = privacy ? 0 : progress[i].RemainingFraction * 100;
                rows[i].State.Text = privacy ? L.T("expense.hidden") : progress[i].Remaining == 0 ? L.T("expense.completed") : L.T("expense.coveredValue", Engine.Money(progress[i].Covered, settings), Engine.Money(progress[i].Item.Amount, settings));
            }
        }
    }
}
