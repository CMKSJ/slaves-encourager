using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Globalization;

namespace SlavesEncourager.Mac;

public sealed record Choice(string Code, string Caption)
{ public override string ToString() => Caption; }

public sealed class SettingsWindow : Window
{
    readonly MainWindow main;
    readonly Settings original;
    Settings draft;
    public readonly StockDraft Stock;
    readonly Dictionary<string, TextBox> inputs = new();
    readonly Dictionary<string, ComboBox> choices = new();
    readonly Dictionary<string, CheckBox> checks = new();
    readonly Dictionary<int, CheckBox> days = new();
    GoalEditor goals, expenses;
    CalendarEditor calendar;
    TextBlock error;
    bool adjust;
    public TabControl Tabs;
    public Settings Result { get; private set; }
    public Func<bool> PortfolioSaveReady;
    public SettingsWindow(MainWindow owner, Settings settings, PortfolioState portfolio, int tab)
    {
        main = owner; original = owner.Settings.Copy(); draft = settings.Copy(); Stock = new StockDraft(portfolio); PortfolioSaveReady = () => !main.Stocks.Busy;
        Width = 740; Height = 790; MinWidth = 620; MinHeight = 570; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Build(tab); Closed += (_, _) => { L.Use(main.Settings.Language); Ui.Theme(main.Settings.Theme); };
    }
    TextBox Field(StackPanel body, string key, string text, int max = 48)
    { var input = Ui.Input(text, max); inputs[key] = input; body.Children.Add(Ui.Label(key)); body.Children.Add(input); return input; }
    CheckBox Check(StackPanel body, string key, bool value)
    { var check = Ui.Check(key, value); checks[key] = check; body.Children.Add(check); return check; }
    ComboBox ChoiceBox(StackPanel body, string key, string value, params (string code, string label)[] items)
    {
        var all = items.Select(i => new Choice(i.code, L.Keys.Contains(i.label) ? L.T(i.label) : i.label)).ToArray();
        var box = new ComboBox { ItemsSource = all, SelectedItem = all.FirstOrDefault(i => i.Code == value), HorizontalAlignment = HorizontalAlignment.Stretch };
        choices[key] = box; body.Children.Add(Ui.Label(key)); body.Children.Add(box); return box;
    }
    public void Build(int selected)
    {
        L.Use(draft.Language); Ui.Theme(draft.Theme); Title = L.T("settings.title"); Background = Ui.Bg; Foreground = Ui.Ink;
        inputs.Clear(); choices.Clear(); checks.Clear(); days.Clear();
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto"), Margin = new Thickness(24) };
        var heading = Ui.Stack(Ui.Label("settings.headline", 24), Ui.Label("settings.previewHint", 11));
        var language = ChoiceBox(heading, "language.label", draft.Language ?? "auto", new[] { ("auto", "language.auto"), ("zh-Hans", "简体中文"), ("zh-Hant", "繁體中文"), ("en", "English"), ("ja", "日本語"), ("es", "Español"), ("ko", "한국어") });
        language.SelectionChanged += (_, _) =>
        {
            try { Read(); draft.Language = ((Choice)language.SelectedItem).Code; Build(Tabs.SelectedIndex); }
            catch (Exception e) { error.Text = e.Message; }
        };
        root.Children.Add(heading);
        var preview = new FlipNumber(); preview.Set(Engine.Format(1234.605m, draft), draft); Grid.SetRow(preview, 1); root.Children.Add(preview);
        var appearance = Ui.Stack();
        ChoiceBox(appearance, "appearance.animation", draft.Animation, ("flip", "animation.flip"), ("slide", "animation.slide"), ("none", "animation.none"));
        ChoiceBox(appearance, "appearance.theme", draft.Theme, ("midnight", "theme.mist"), ("forest", "theme.dusk"), ("paper", "theme.paper"));
        ChoiceBox(appearance, "appearance.font", draft.Font, ("DIN", "font.din"), ("Mono", "font.mono"), ("Classic", "font.classic"));
        ChoiceBox(appearance, "appearance.decimals", draft.Decimals.ToString(), Enumerable.Range(0, 5).Select(i => (i.ToString(), "precision." + i)).ToArray());
        ChoiceBox(appearance, "appearance.currency", draft.Currency, ("CNY", "currency.cny"), ("USD", "currency.usd"), ("EUR", "currency.eur"), ("JPY", "currency.jpy"));
        Check(appearance, "appearance.grouping", draft.Grouping);
        var pattern = ChoiceBox(appearance, "appearance.layout", new[] { "{symbol}{amount}", "{amount} {code}", "{amount}" }.Contains(draft.Pattern) ? draft.Pattern : "custom",
            ("{symbol}{amount}", "pattern.prefix"), ("{amount} {code}", "pattern.code"), ("{amount}", "pattern.number"), ("custom", "pattern.custom"));
        var patternInput = Field(appearance, "pattern.label", draft.Pattern); appearance.Children.Add(Ui.Label("pattern.help", 11));
        pattern.SelectionChanged += (_, _) => { if (pattern.SelectedItem is Choice c && c.Code != "custom") patternInput.Text = c.Code; };
        ChoiceBox(appearance, "appearance.speed", draft.Speed.ToString(), ("220", "speed.fast"), ("420", "speed.normal"), ("650", "speed.slow"));
        foreach (var control in appearance.Children)
        {
            if (control is ComboBox combo)
            {
                if (combo == choices["appearance.theme"]) combo.SelectionChanged += (_, _) => { try { Read(); Build(0); } catch (Exception e) { error.Text = e.Message; } };
                else combo.SelectionChanged += (_, _) => Preview();
            }
            if (control is TextBox text) text.TextChanged += (_, _) => Preview();
            if (control is CheckBox check) check.IsCheckedChanged += (_, _) => Preview();
        }
        void Preview()
        {
            if (error == null) return;
            try { Read(); Ui.Theme(draft.Theme); Background = Ui.Bg; preview.Set(Engine.Format(1234.605m, draft), draft); error.Text = ""; }
            catch (Exception e) { error.Text = e.Message; }
        }
        var salary = Ui.Stack(); Field(salary, "salary.amount", draft.MonthlySalary.ToString(CultureInfo.InvariantCulture), 16);
        ChoiceBox(salary, "salary.basis", draft.Basis, ("actual", "basis.actual"), ("fixed", "basis.fixed"));
        ChoiceBox(salary, "salary.settlement", draft.CycleDay.ToString(), Enumerable.Range(1, 31).Select(i => (i.ToString(), i.ToString())).ToArray());
        salary.Children.Add(Ui.Label("salary.cycleHelp", 11));
        salary.Children.Add(Ui.Button("income.adjust", () => { adjust = true; error.Text = L.T("income.adjustHint"); }));
        var expensesOn = Check(salary, "expense.enable", draft.ExpensesEnabled);
        Field(salary, "salary.start", draft.Start, 5); Field(salary, "salary.end", draft.End, 5); Check(salary, "salary.breakEnabled", draft.LunchEnabled);
        Field(salary, "salary.breakStart", draft.LunchStart, 5); Field(salary, "salary.breakEnd", draft.LunchEnd, 5);
        var weekday = Ui.Row();
        foreach (var i in new[] { 1, 2, 3, 4, 5, 6, 0 })
        { var c = Ui.Check("weekday." + new[] { "sun", "mon", "tue", "wed", "thu", "fri", "sat" }[i], draft.ParseDays().Contains(i)); days[i] = c; weekday.Children.Add(c); }
        salary.Children.Add(Ui.Label("salary.weekdays")); salary.Children.Add(weekday);
        salary.Children.Add(Ui.Button("calendar.open", () => Tabs.SelectedIndex = 2));
        calendar = new CalendarEditor(draft);
        goals = new GoalEditor(draft.GoalItems, false); expenses = new GoalEditor(draft.ExpenseItems, true);
        var stocks = Ui.Stack(Ui.Label("stock.tabTitle", 19)); var enabled = Check(stocks, "stock.enable", draft.StocksEnabled);
        var stockPanel = new StockPanel(this, Stock); stockPanel.IsEnabled = draft.StocksEnabled; stocks.Children.Add(stockPanel); stocks.Children.Add(Ui.Label("stock.tabHelp", 11));
        enabled.IsCheckedChanged += (_, _) => stockPanel.IsEnabled = enabled.IsChecked == true;
        var expenseTab = Tab("expense.title", expenses); expenseTab.IsVisible = draft.ExpensesEnabled;
        expensesOn.IsCheckedChanged += (_, _) => expenseTab.IsVisible = expensesOn.IsChecked == true;
        Tabs = new TabControl { ItemsSource = new[] { Tab("tab.appearance", appearance), Tab("tab.salary", salary), Tab("tab.calendar", calendar), Tab("tab.goals", goals), expenseTab, Tab("tab.stocks", stocks) }, SelectedIndex = selected };
        Grid.SetRow(Tabs, 2); root.Children.Add(Tabs);
        error = Ui.Text("", 11, Ui.Red); Grid.SetRow(error, 3); root.Children.Add(error);
        var actions = Ui.Row(Ui.Button("action.cancel", Close), Ui.AsyncButton("action.save", Save)); Grid.SetRow(actions, 4); root.Children.Add(actions); Content = root;
    }
    TabItem Tab(string key, Control content) => new() { Header = L.T(key), Content = new ScrollViewer { Content = content, Margin = new Thickness(0, 12, 0, 0), HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled } };
    string Code(string key) => ((Choice)choices[key].SelectedItem).Code;
    public void PreviewTheme(string theme) => choices["appearance.theme"].SelectedItem = choices["appearance.theme"].ItemsSource.Cast<Choice>().Single(c => c.Code == theme);
    void Read()
    {
        if (goals == null || calendar == null || !inputs.ContainsKey("salary.amount")) return;
        draft.Animation = Code("appearance.animation"); draft.Font = Code("appearance.font"); draft.Theme = Code("appearance.theme");
        draft.Decimals = int.Parse(Code("appearance.decimals")); draft.Currency = Code("appearance.currency"); draft.Speed = int.Parse(Code("appearance.speed"));
        draft.Grouping = checks["appearance.grouping"].IsChecked == true; draft.Pattern = inputs["pattern.label"].Text;
        draft.MonthlySalary = Ui.Number(inputs["salary.amount"].Text, "validation.salaryNumber"); draft.Basis = Code("salary.basis"); draft.SettlementDay = int.Parse(Code("salary.settlement"));
        draft.Start = inputs["salary.start"].Text; draft.End = inputs["salary.end"].Text; draft.LunchEnabled = checks["salary.breakEnabled"].IsChecked == true;
        draft.LunchStart = inputs["salary.breakStart"].Text; draft.LunchEnd = inputs["salary.breakEnd"].Text;
        draft.Weekdays = string.Join(",", days.Where(d => d.Value.IsChecked == true).Select(d => d.Key));
        draft.RestDates = CalendarOverrides.Serialize(calendar.Overrides.Rest); draft.WorkDates = CalendarOverrides.Serialize(calendar.Overrides.Work);
        draft.Goals = goals.Read(); draft.Expenses = expenses.Read(); draft.ExpensesEnabled = checks["expense.enable"].IsChecked == true; draft.StocksEnabled = checks["stock.enable"].IsChecked == true;
    }
    public async Task Save()
    {
        try
        {
            Read(); draft.Validate();
            if (!PortfolioSaveReady()) throw new ArgumentException(L.T("stock.busy"));
            if (adjust || !original.Configured || original.CycleDay != draft.CycleDay || (!original.ExpensesEnabled && draft.ExpensesEnabled))
            {
                if (original.CycleDay != draft.CycleDay) { draft.OpeningAt = null; draft.OpeningIncome = null; }
                var amount = await Ui.Income(this, draft); if (!amount.HasValue) return; draft.ConfirmIncome(DateTime.Now, amount.Value);
            }
            if (!PortfolioSaveReady()) throw new ArgumentException(L.T("stock.busy"));
            main.Apply(draft.Copy(), draft.StocksEnabled && Stock.Changed ? Stock.State.Copy() : null); Result = draft.Copy(); Close();
        }
        catch (Exception e) { error.Text = e.Message; }
    }
}

public sealed class GoalEditor : StackPanel
{
    readonly bool expense;
    readonly List<(TextBox name, TextBox amount)> rows = new();
    List<RewardGoal> values;
    public GoalEditor(List<RewardGoal> goals, bool expense) { this.expense = expense; values = goals.Select(x => new RewardGoal(x.Name, x.Amount)).ToList(); Build(); }
    public List<RewardGoal> Read() => rows.Select(x => new RewardGoal(x.name.Text, Ui.Number(x.amount.Text, expense ? "validation.expenseAmount" : "validation.goalAmount"))).ToList();
    void Build()
    {
        Children.Clear(); rows.Clear(); Spacing = 10;
        Children.Add(Ui.Label(expense ? "expense.editorHelp" : "goal.allocationHelp", 11));
        for (int i = 0; i < values.Count; i++)
        {
            int index = i; var name = Ui.Input(values[i].Name, 28); name.Width = 240;
            var amount = Ui.Input(values[i].Amount.ToString(CultureInfo.InvariantCulture), 16); amount.Width = 130; rows.Add((name, amount));
            Children.Add(Ui.Row(name, amount, Ui.LiteralButton("↑", () => Move(index, -1)), Ui.LiteralButton("↓", () => Move(index, 1)), Ui.Button("goal.remove", () => Change(() => values.RemoveAt(index)))));
        }
        Children.Add(Ui.Button(expense ? "expense.add" : "goal.add", () => Change(() => { if (values.Count < 20) values.Add(new RewardGoal(L.T(expense ? "expense.new" : "goal.new"), 1000)); })));
    }
    void Change(Action action)
    {
        try { values = Read(); action(); Build(); }
        catch (ArgumentException e) { _ = Ui.Alert(TopLevel.GetTopLevel(this) as Window, e.Message); }
    }
    void Move(int i, int delta) => Change(() => { int j = i + delta; if (j >= 0 && j < values.Count) (values[i], values[j]) = (values[j], values[i]); });
}

public sealed class CalendarEditor : StackPanel
{
    public readonly CalendarOverrides Overrides;
    readonly Settings settings;
    DateTime month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    DayOverride mode = DayOverride.Rest;
    public CalendarEditor(Settings settings) { this.settings = settings; Overrides = new CalendarOverrides(settings.RestDates, settings.WorkDates); Build(); }
    void Build()
    {
        Children.Clear(); Spacing = 10;
        Children.Add(Ui.Row(Ui.Button("calendar.previous", () => { month = month.AddMonths(-1); Build(); }), Ui.Text(month.ToString("Y", L.Culture), 19), Ui.Button("calendar.next", () => { month = month.AddMonths(1); Build(); }), Ui.Button("calendar.current", () => { month = new(DateTime.Today.Year, DateTime.Today.Month, 1); Build(); })));
        Children.Add(Ui.Row(Ui.Button("calendar.rest", () => { mode = DayOverride.Rest; Build(); }), Ui.Button("calendar.work", () => { mode = DayOverride.Work; Build(); }), Ui.Button("calendar.restore", () => { mode = DayOverride.Schedule; Build(); })));
        Children.Add(Ui.Label(mode == DayOverride.Rest ? "calendar.restHint" : mode == DayOverride.Work ? "calendar.workHint" : "calendar.restoreHint", 11));
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto") };
        for (int i = 0; i < 7; i++) { var t = Ui.Text(L.Culture.DateTimeFormat.AbbreviatedDayNames[(i + 1) % 7]); Grid.SetColumn(t, i); grid.Children.Add(t); }
        int offset = ((int)month.DayOfWeek + 6) % 7;
        for (int n = 1; n <= DateTime.DaysInMonth(month.Year, month.Month); n++)
        {
            var date = month.AddDays(n - 1); int index = offset + n - 1;
            string mark = Overrides.Rest.Contains(date) ? L.T("calendar.restMark") : Overrides.Work.Contains(date) ? L.T("calendar.workMark") : "";
            var button = Ui.LiteralButton(n + " " + mark, () => { Overrides.Click(date, mode); Build(); }); button.Margin = new Thickness(2); button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Background = mark.Length > 0 ? Ui.Alt : Ui.Surface; Grid.SetColumn(button, index % 7); Grid.SetRow(button, index / 7 + 1); grid.Children.Add(button);
        }
        Children.Add(grid); Children.Add(Ui.Text(L.T("calendar.summary", Overrides.Rest.Count, Overrides.Work.Count), 11));
    }
}

public static class CopyEditor
{
    public static async Task Open(Window owner, Settings original, Action<Settings> save, string selected = null)
    {
        var draft = original.Copy(); var w = Ui.Window(L.T("copy.edit"), 670, 550);
        var keys = HomeCopy.Keys.ToArray(); var select = new ComboBox { ItemsSource = keys, SelectedItem = selected ?? keys[0], HorizontalAlignment = HorizontalAlignment.Stretch };
        var input = Ui.Input("", 300); input.AcceptsReturn = true; input.MinHeight = 90;
        var defaults = Ui.Text("", 11); var error = Ui.Text("", 11, Ui.Red);
        void Load() { string key = (string)select.SelectedItem; input.Text = HomeCopy.Raw(draft, key); defaults.Text = L.T("copy.original", L.T(key)); }
        select.SelectionChanged += (_, _) => Load(); Load();
        w.Content = Ui.DialogBody(Ui.Stack(Ui.Label("copy.help"), select, defaults, input, error), Ui.Row(
            Ui.Button("copy.reset", () => { HomeCopy.Reset(draft, (string)select.SelectedItem); Load(); }),
            Ui.Button("copy.resetAll", () => { HomeCopy.ResetLanguage(draft); Load(); }), Ui.Button("action.cancel", w.Close), Ui.Button("action.save", () =>
            { try { HomeCopy.Set(draft, (string)select.SelectedItem, input.Text); save(draft); w.Close(); } catch (Exception e) { error.Text = e.Message; } })));
        await w.ShowDialog(owner);
    }
}
