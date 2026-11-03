using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SlavesEncourager
{
    public sealed class ScheduleCalendar : Grid
    {
        public readonly CalendarOverrides Dates;
        readonly Func<int, bool> weekday;
        readonly Action changed;
        readonly TextBlock monthText = new TextBlock();
        readonly TextBlock hint = new TextBlock();
        readonly UniformGrid days = new UniformGrid { Columns = 7, Rows = 6 };
        readonly Dictionary<DayOverride, Button> modes = new Dictionary<DayOverride, Button>();
        DateTime month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DayOverride mode = DayOverride.Rest;

        public ScheduleCalendar(Settings settings, Func<int, bool> weekly, Action onChange)
        {
            Dates = new CalendarOverrides(settings.RestDates, settings.WorkDates); weekday = weekly; changed = onChange;
            foreach (double height in new[] { 39d, 38d, 24d, 204d, 45d }) RowDefinitions.Add(new RowDefinition { Height = new GridLength(height) });
            var header = new Grid();
            monthText.FontSize = 18; monthText.FontFamily = new FontFamily("Segoe UI"); monthText.VerticalAlignment = VerticalAlignment.Center;
            monthText.MaxWidth = 280; monthText.TextTrimming = TextTrimming.CharacterEllipsis; monthText.HorizontalAlignment = HorizontalAlignment.Left;
            header.Children.Add(monthText);
            var navigation = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            navigation.Children.Add(ActionButton("‹", "calendar.previous", delegate { if (month.Year > 1 || month.Month > 1) { month = month.AddMonths(-1); Refresh(); } }));
            navigation.Children.Add(ActionButton("calendar.current", "calendar.currentTip", delegate { month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); Refresh(); }));
            navigation.Children.Add(ActionButton("›", "calendar.next", delegate { if (month.Year < 9999 || month.Month < 12) { month = month.AddMonths(1); Refresh(); } }));
            header.Children.Add(navigation); Children.Add(header);
            var toolbar = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (DayOverride item in new[] { DayOverride.Rest, DayOverride.Work, DayOverride.Schedule })
            {
                DayOverride selected = item;
                string caption = item == DayOverride.Rest ? "calendar.rest" : item == DayOverride.Work ? "calendar.work" : "calendar.restore";
                var button = ActionButton(caption, caption, delegate { mode = selected; Refresh(); });
                modes.Add(item, button); toolbar.Children.Add(button);
            }
            Grid.SetRow(toolbar, 1); Children.Add(toolbar);
            var weekdayHeader = new UniformGrid { Columns = 7 };
            foreach (string name in new[] { "weekday.mon", "weekday.tue", "weekday.wed", "weekday.thu", "weekday.fri", "weekday.sat", "weekday.sun" })
            {
                var label = new TextBlock { FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                label.SetResourceReference(TextBlock.TextProperty, name);
                label.SetResourceReference(TextBlock.ForegroundProperty, "Muted"); weekdayHeader.Children.Add(label);
            }
            Grid.SetRow(weekdayHeader, 2); Children.Add(weekdayHeader);
            Grid.SetRow(days, 3); Children.Add(days);
            hint.FontSize = 10; hint.TextWrapping = TextWrapping.Wrap; hint.Margin = new Thickness(2, 8, 0, 0);
            hint.SetResourceReference(TextBlock.ForegroundProperty, "Muted"); Grid.SetRow(hint, 4); Children.Add(hint);
            Refresh();
        }

        static Button ActionButton(string text, string name, Action action)
        {
            var button = new Button { Padding = new Thickness(9, 5, 9, 5), Margin = new Thickness(0, 0, 6, 5), FontSize = 10 };
            if (text == "‹" || text == "›") button.Content = text; else button.SetResourceReference(Button.ContentProperty, text);
            button.SetResourceReference(AutomationProperties.NameProperty, name); button.Click += delegate { action(); }; return button;
        }

        public void Refresh()
        {
            monthText.Text = month.ToString("Y", L.Culture);
            foreach (var pair in modes)
            {
                pair.Value.SetResourceReference(Button.BackgroundProperty, pair.Key == mode ? "Accent" : "Surface");
                pair.Value.SetResourceReference(Button.ForegroundProperty, pair.Key == mode ? "AccentInk" : "Ink");
            }
            days.Children.Clear();
            int offset = ((int)month.DayOfWeek + 6) % 7;
            int count = DateTime.DaysInMonth(month.Year, month.Month);
            for (int cell = 0; cell < 42; cell++)
            {
                int day = cell - offset + 1;
                if (day < 1 || day > count) { days.Children.Add(new Border()); continue; }
                DateTime date = month.AddDays(day - 1);
                bool rest = Dates.Rest.Contains(date), work = Dates.Work.Contains(date);
                string marker = rest ? L.T("calendar.restMark") : work ? L.T("calendar.workMark") : weekday((int)date.DayOfWeek) ? "" : L.T("calendar.restMark");
                var content = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                content.Children.Add(new TextBlock { Text = day.ToString(CultureInfo.InvariantCulture), FontFamily = new FontFamily("Bahnschrift"), FontSize = 14 });
                if (marker.Length > 0) content.Children.Add(new TextBlock { Text = marker, FontSize = 9, Margin = new Thickness(3, 2, 0, 0) });
                var button = new Button { Content = content, Padding = new Thickness(0), Margin = new Thickness(2), BorderThickness = new Thickness(date == DateTime.Today ? 2 : 1),
                    ToolTip = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + (rest ? L.T("calendar.restSuffix") : work ? L.T("calendar.workSuffix") : L.T("calendar.normalSuffix")) };
                button.SetResourceReference(Button.BackgroundProperty, rest || work ? "SurfaceAlt" : "Surface");
                button.SetResourceReference(Button.BorderBrushProperty, rest || work || date == DateTime.Today ? "Accent" : "Stroke");
                foreach (TextBlock label in content.Children) label.SetResourceReference(TextBlock.ForegroundProperty, rest || work ? "Accent" : "Ink");
                if (!rest && !work && marker == L.T("calendar.restMark")) button.Opacity = 0.58;
                AutomationProperties.SetName(button, (string)button.ToolTip);
                button.Click += delegate { Dates.Click(date, mode); if (changed != null) changed(); Refresh(); };
                days.Children.Add(button);
            }
            hint.Text = L.T(mode == DayOverride.Schedule ? "calendar.restoreHint" : mode == DayOverride.Rest ? "calendar.restHint" : "calendar.workHint") +
                "\n" + L.T("calendar.summary", Dates.Rest.Count, Dates.Work.Count);
        }
    }

    public sealed class GoalListEditor : StackPanel
    {
        sealed class GoalRow
        {
            public TextBox Name, Amount;
            public Border Card;
            public TextBlock Number;
            public Button Up, Down;
        }
        readonly List<GoalRow> rows = new List<GoalRow>();
        readonly Action changed;
        public int Count { get { return rows.Count; } }
        public GoalListEditor(IEnumerable<RewardGoal> goals, Action onChange)
        {
            changed = onChange;
            foreach (RewardGoal goal in goals) Add(goal);
            Reorder();
        }

        public void Add(RewardGoal goal)
        {
            if (rows.Count >= 20) return;
            var row = new GoalRow();
            var body = new StackPanel();
            var header = new Grid();
            row.Number = new TextBlock { FontSize = 10, VerticalAlignment = VerticalAlignment.Center };
            row.Number.SetResourceReference(TextBlock.ForegroundProperty, "Muted"); header.Children.Add(row.Number);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            row.Up = RowButton("↑", "goal.moveUp", delegate { Move(row, -1); });
            row.Down = RowButton("↓", "goal.moveDown", delegate { Move(row, 1); });
            actions.Children.Add(row.Up); actions.Children.Add(row.Down);
            actions.Children.Add(RowButton("goal.remove", "goal.removeTip", delegate { rows.Remove(row); Reorder(); }));
            header.Children.Add(actions); body.Children.Add(header);
            var fields = new Grid { Margin = new Thickness(0, 5, 0, 0) };
            fields.ColumnDefinitions.Add(new ColumnDefinition()); fields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            fields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
            row.Name = new TextBox { Text = goal.Name, MaxLength = 28, Margin = new Thickness(0, 5, 0, 0) };
            row.Amount = new TextBox { Text = goal.Amount.ToString(CultureInfo.InvariantCulture), MaxLength = 16, Margin = new Thickness(0, 5, 0, 0) };
            row.Name.SetResourceReference(AutomationProperties.NameProperty, "goal.name"); row.Amount.SetResourceReference(AutomationProperties.NameProperty, "goal.amount");
            row.Name.TextChanged += delegate { Notify(); }; row.Amount.TextChanged += delegate { Notify(); };
            var nameLabel = new TextBlock { FontSize = 10 }; nameLabel.SetResourceReference(TextBlock.TextProperty, "goal.name");
            var amountLabel = new TextBlock { FontSize = 10 }; amountLabel.SetResourceReference(TextBlock.TextProperty, "goal.amountShort");
            var names = new StackPanel(); names.Children.Add(nameLabel); names.Children.Add(row.Name);
            var amounts = new StackPanel(); amounts.Children.Add(amountLabel); amounts.Children.Add(row.Amount);
            fields.Children.Add(names); Grid.SetColumn(amounts, 2); fields.Children.Add(amounts); body.Children.Add(fields);
            row.Card = new Border { CornerRadius = new CornerRadius(10), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 10), BorderThickness = new Thickness(1), Child = body };
            row.Card.SetResourceReference(Border.BackgroundProperty, "Surface"); row.Card.SetResourceReference(Border.BorderBrushProperty, "Stroke");
            rows.Add(row); Reorder();
        }

        static Button RowButton(string text, string name, Action action)
        {
            var button = new Button { FontSize = 10, Padding = new Thickness(8, 3, 8, 3), Margin = new Thickness(4, 0, 0, 0) };
            if (text == "↑" || text == "↓") button.Content = text; else button.SetResourceReference(Button.ContentProperty, text);
            button.SetResourceReference(AutomationProperties.NameProperty, name); button.Click += delegate { action(); }; return button;
        }
        void Notify() { if (changed != null) changed(); }
        void Move(GoalRow row, int direction)
        {
            int index = rows.IndexOf(row), target = index + direction;
            if (target < 0 || target >= rows.Count) return;
            rows.RemoveAt(index); rows.Insert(target, row); Reorder();
        }
        void Reorder()
        {
            Children.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].Number.Text = L.T("goal.number", i + 1);
                rows[i].Up.IsEnabled = i > 0; rows[i].Down.IsEnabled = i < rows.Count - 1;
                Children.Add(rows[i].Card);
            }
            if (rows.Count == 0)
            {
                var empty = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 20) };
                empty.SetResourceReference(TextBlock.TextProperty, "goal.emptyEditor");
                empty.SetResourceReference(TextBlock.ForegroundProperty, "Muted"); Children.Add(empty);
            }
            Notify();
        }
        public List<RewardGoal> Read()
        {
            var result = new List<RewardGoal>();
            for (int i = 0; i < rows.Count; i++)
            {
                decimal amount;
                if (!decimal.TryParse(rows[i].Amount.Text.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount))
                    throw new ArgumentException(L.T("validation.goalNumber", i + 1));
                result.Add(new RewardGoal(rows[i].Name.Text.Trim(), amount));
            }
            return result;
        }
        public void RefreshLanguage()
        {
            for (int i = 0; i < rows.Count; i++) rows[i].Number.Text = L.T("goal.number", i + 1);
        }
    }
}
