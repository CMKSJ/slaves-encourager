using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using System.Globalization;
using System.Reflection;

namespace SlavesEncourager.Mac;

public static class Program
{
    public static string[] Args = Array.Empty<string>();
    [STAThread]
    public static int Main(string[] args)
    {
        Args = args;
        try
        {
            if (args.Contains("--self-test"))
                return EngineTests.Main() | StockTests.Main(Array.Empty<string>()) | MacChecks.Run();
            if (args.Length == 2 && args[0] == "--ocr-table-check")
            {
                var rows = HoldingImport.ParseText(File.ReadAllText(args[1]));
                var header = rows.FindIndex(r => HoldingImport.GuessColumn(r, "symbol") >= 0 && HoldingImport.GuessColumn(r, "quantity") >= 0);
                if (header < 0) throw new InvalidDataException("OCR header not recognized");
                var columns = rows[header]; var positions = HoldingImport.Map(rows, header, HoldingImport.GuessColumn(columns, "symbol"), HoldingImport.GuessColumn(columns, "quantity"), HoldingImport.GuessColumn(columns, "cost"), HoldingImport.GuessColumn(columns, "currency"));
                if (positions.Count != 2 || positions[0].Symbol != "AAPL" || positions[0].Quantity != 10m || positions[0].AverageCost != 150m || positions[1].Symbol != "MSFT" || positions[1].Quantity != 5m || positions[1].AverageCost != 300m) throw new InvalidDataException("OCR quantities/costs did not match synthetic fixture");
                Console.WriteLine("NATIVE VISION OCR TABLE CHECK PASS"); return 0;
            }
            var builder = AppBuilder.Configure<MacApp>().UsePlatformDetect().LogToTrace();
            if ((args.Contains("--ui-smoke") || args.Contains("--launch-check")) && OperatingSystem.IsMacOS()) builder.With(new AvaloniaNativePlatformOptions { RenderingMode = new[] { AvaloniaNativeRenderingMode.Software } });
            return builder.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}

public sealed class MacApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var main = new MainWindow(Program.Args.Contains("--ui-smoke") || Program.Args.Contains("--launch-check"));
            desktop.MainWindow = main;
            bool smokeStarted = false;
            if (Program.Args.Contains("--ui-smoke"))
                main.Opened += async (_, _) =>
                {
                    if (smokeStarted) return;
                    smokeStarted = true;
                    try { await MacChecks.Ui(main); desktop.Shutdown(0); }
                    catch (Exception e) { Console.Error.WriteLine(e); desktop.Shutdown(1); }
                };
            if (Program.Args.Contains("--launch-check")) main.Opened += async (_, _) => { await Task.Delay(500); Console.WriteLine("SIGNED MAC APP LAUNCH PASS"); desktop.Shutdown(0); };
        }
        base.OnFrameworkInitializationCompleted();
    }
}

public static class Paths
{
    public static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "SlavesEncouragerStockBeta");
    public static string SettingsPath => Path.Combine(DirectoryPath, "settings.json");
    public static string PortfolioPath => Path.Combine(DirectoryPath, "portfolio.json");
}

public static class Ui
{
    public static IBrush Bg, Surface, Alt, Ink, Muted, Accent, Stroke;
    public static readonly IBrush Red = Brush.Parse("#BD465D");
    public static void Theme(string key)
    {
        var colors = key == "paper" ? new[] { "#F7F1E8", "#FFFCF7", "#F0E5D6", "#4B3A31", "#89796B", "#9B674B", "#DED2C3" } :
            key == "forest" ? new[] { "#242337", "#302D46", "#3C3755", "#F5F0FC", "#B7ACC9", "#D5BDEB", "#504969" } :
            new[] { "#F1F4FA", "#FFFFFF", "#E7EDF8", "#27364D", "#74839A", "#526FA8", "#D6DFEE" };
        Bg = Brush.Parse(colors[0]); Surface = Brush.Parse(colors[1]); Alt = Brush.Parse(colors[2]); Ink = Brush.Parse(colors[3]);
        Muted = Brush.Parse(colors[4]); Accent = Brush.Parse(colors[5]); Stroke = Brush.Parse(colors[6]);
        if (Application.Current != null) Application.Current.RequestedThemeVariant = key == "forest" ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light;
    }
    public static TextBlock Text(string text, double size = 13, IBrush color = null) => new() { Text = text, FontSize = size, Foreground = color ?? Ink, TextWrapping = TextWrapping.Wrap };
    public static TextBlock Label(string key, double size = 13) => Text(L.T(key), size);
    public static StackPanel Stack(params Control[] children)
    { var p = new StackPanel { Spacing = 10 }; foreach (var c in children) p.Children.Add(c); return p; }
    public static WrapPanel Row(params Control[] children)
    { var p = new WrapPanel { Orientation = Orientation.Horizontal }; foreach (var c in children) { c.Margin = new Thickness(0, 0, 8, 7); p.Children.Add(c); } return p; }
    public static Button Button(string key, Action action) => LiteralButton(L.T(key), action);
    public static Button LiteralButton(string caption, Action action)
    {
        var b = new Button { Content = caption, Padding = new Thickness(12, 8), CornerRadius = new CornerRadius(8), Foreground = Ink, Background = Surface, BorderBrush = Stroke, BorderThickness = new Thickness(1) };
        b.Click += (_, _) => { try { action(); } catch (Exception e) { _ = Alert(TopLevel.GetTopLevel(b) as Window, e.Message); } }; return b;
    }
    public static Button AsyncButton(string key, Func<Task> action)
    { var b = Button(key, () => { }); b.Click += async (_, _) => { b.IsEnabled = false; try { await action(); } catch (Exception e) { await Alert(TopLevel.GetTopLevel(b) as Window, e.Message); } finally { b.IsEnabled = true; } }; return b; }
    public static TextBox Input(string value, int max = 48) => new() { Text = value, MaxLength = max, HorizontalAlignment = HorizontalAlignment.Stretch };
    public static CheckBox Check(string key, bool value) => new() { Content = Label(key), IsChecked = value };
    public static Border Card(Control child, bool alt = false) => new() { Background = alt ? Alt : Surface, CornerRadius = new CornerRadius(15), Padding = new Thickness(18), BorderBrush = Stroke, BorderThickness = new Thickness(1), Child = child };
    public static ProgressBar Progress(double value, bool red = false) => new() { Minimum = 0, Maximum = 100, Value = Math.Clamp(value * 100, 0, 100), Height = 5, Foreground = red ? Red : Accent, Background = Alt };
    public static Window Window(string title, double width = 630, double height = 600)
    {
        var w = new Window { Title = title, Width = width, Height = height, MinWidth = Math.Min(440, width), MinHeight = Math.Min(300, height), WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Bg, Foreground = Ink };
        using var image = Assembly.GetExecutingAssembly().GetManifestResourceStream("AppIcon.png"); w.Icon = new WindowIcon(image);
        return w;
    }
    public static Control DialogBody(Control body, Control actions)
    {
        var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Margin = new Thickness(24) };
        grid.Children.Add(new ScrollViewer { Content = body, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled });
        Grid.SetRow(actions, 1); actions.Margin = new Thickness(0, 16, 0, 0); grid.Children.Add(actions); return grid;
    }
    public static async Task Alert(Window owner, string message)
    {
        var w = Window(L.T("app.name"), 540, 260);
        w.Content = DialogBody(Text(message), Button("action.cancel", () => w.Close()));
        if (owner != null) await w.ShowDialog(owner); else w.Show();
    }
    public static async Task<bool> Confirm(Window owner, string message)
    {
        var w = Window(L.T("app.name"), 540, 280);
        w.Content = DialogBody(Text(message), Row(Button("action.cancel", w.Close), Button("action.save", () => w.Close(true))));
        return await w.ShowDialog<bool>(owner);
    }
    public static async Task<bool?> StockPrompt(Window owner)
    {
        return await new StockWelcomeWindow().ShowDialog<bool?>(owner);
    }
    public static decimal Number(string value, string key)
    { if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) throw new ArgumentException(L.T(key)); return v; }
    public static async Task<decimal?> Income(Window owner, Settings settings)
    {
        var now = DateTime.Now; var period = Engine.PayPeriod(now, settings); var estimate = Engine.Calculate(now, settings).Month;
        var w = Window(L.T("income.title"), 590, 470); var input = Input(estimate.ToString("F2", CultureInfo.InvariantCulture), 16); var error = Text("", 12, Red);
        w.Content = DialogBody(Stack(Label("income.title", 23), Text(L.T("income.period", period.Start.ToString("yyyy-MM-dd"), period.End.AddDays(-1).ToString("yyyy-MM-dd"))),
            Text(L.T("income.estimate", Engine.Money(estimate, settings)), 20), Label("income.help"), Text(L.T("income.amount", settings.Currency)), input, error), Row(
            Button("action.cancel", () => w.Close((decimal?)null)), Button("income.zero", () => input.Text = "0"), Button("income.confirm", () =>
            { try { var value = Number(input.Text, "validation.opening"); if (value < 0 || value > 100000000m) throw new ArgumentException(L.T("validation.opening")); w.Close((decimal?)value); } catch (ArgumentException e) { error.Text = e.Message; } })));
        return await w.ShowDialog<decimal?>(owner);
    }
}

public sealed class StockWelcomeWindow : Window
{
    public readonly Button Yes, No, Cancel;
    public StockWelcomeWindow()
    {
        Title = L.T("stock.welcomeTitle"); Width = 590; Height = 320; CanResize = false; WindowStartupLocation = WindowStartupLocation.CenterOwner; Background = Ui.Bg; Foreground = Ui.Ink;
        Yes = Ui.Button("stock.welcomeYes", () => Close((bool?)true)); No = Ui.Button("stock.welcomeNo", () => Close((bool?)false)); Cancel = Ui.Button("action.cancel", () => Close((bool?)null));
        Content = Ui.DialogBody(Ui.Stack(Ui.Label("stock.welcomeTitle", 24), Ui.Label("stock.welcomeHelp")), Ui.Row(Cancel, No, Yes));
    }
}

public sealed class MainWindow : Window
{
    public Settings Settings;
    public readonly StockService Stocks;
    public readonly bool Samples;
    bool privacy, mini, demo;
    int goalIndex;
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(120) };
    public TrayIcon MenuBarIcon { get; private set; }
    FlipNumber amount;
    TextBlock status, rate, countdown, month, hourly, hours, stockPnl, stockHint;
    StackPanel goalHost;
    ProgressBar progress;
    string lastStatus = "";
    public MainWindow(bool samples)
    {
        Samples = samples;
        Settings = samples ? new Settings { Configured = true, StocksEnabled = true, ExpensesEnabled = true } : Settings.Load(Paths.SettingsPath, out _);
        L.Use(Settings.Language); Ui.Theme(Settings.Theme); demo = samples || !Settings.Configured;
        Stocks = new StockService(Paths.PortfolioPath, samples, new YahooQuoteProvider(), Settings.StocksEnabled);
        if (samples) Stocks.Demo(); Stocks.Changed += Tick;
        Title = L.T("app.name"); Width = 550; MinWidth = 420; MinHeight = 280;
        SystemDecorations = SystemDecorations.None; CanResize = false; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        using var icon = Assembly.GetExecutingAssembly().GetManifestResourceStream("AppIcon.png"); Icon = new WindowIcon(icon);
        AddHandler(PointerPressedEvent, (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.ClickCount == 1 && e.Source is Visual v &&
                !v.GetSelfAndVisualAncestors().Any(n => n is Button || n is TextBox || n is ComboBox || n is CheckBox)) BeginMoveDrag(e);
        });
        KeyDown += async (_, e) =>
        {
            bool command = e.KeyModifiers.HasFlag(KeyModifiers.Meta) || e.KeyModifiers.HasFlag(KeyModifiers.Control);
            if (!command) return;
            if (e.Key == Key.M) { mini = !mini; Build(); e.Handled = true; }
            if (e.Key == Key.P && e.KeyModifiers.HasFlag(KeyModifiers.Shift)) { privacy = !privacy; Build(); e.Handled = true; }
            if (e.Key == Key.E) { await CopyEditor.Open(this, Settings, SaveSettings); e.Handled = true; }
        };
        timer.Tick += (_, _) => Tick(); timer.Start(); Closed += (_, _) => { timer.Stop(); Stocks.Dispose(); MenuBarIcon?.Dispose(); };
        Build(); Opened += async (_, _) => { Fit(); if (!Samples) await Stocks.RefreshAsync(); };
    }
    public void SaveSettings(Settings candidate)
    { if (!Samples) candidate.Save(Paths.SettingsPath); Settings = candidate; L.Use(candidate.Language); Ui.Theme(candidate.Theme); Build(); }
    Control CopyControl(Control control, string key)
    {
        var item = new MenuItem { Header = L.T("copy.edit") };
        item.Click += async (_, _) => await CopyEditor.Open(this, Settings, SaveSettings, key);
        control.ContextMenu = new ContextMenu { ItemsSource = new[] { item } };
        if (control is TextBlock) control.DoubleTapped += async (_, _) => await CopyEditor.Open(this, Settings, SaveSettings, key);
        return control;
    }
    TextBlock Home(string key, double size = 13, params object[] values) => (TextBlock)CopyControl(Ui.Text(HomeCopy.T(Settings, key, values), size), key);
    Button HomeButton(string key, Action action) => (Button)CopyControl(Ui.LiteralButton(HomeCopy.T(Settings, key), action), key);
    public void Build()
    {
        Ui.Theme(Settings.Theme); Background = Ui.Bg; Foreground = Ui.Ink; Topmost = Settings.Topmost;
        UpdateMenuBar();
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(22) };
        var head = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        head.Children.Add(Home("app.name", 14));
        var headButtons = Ui.Row();
        if (Settings.ExpensesEnabled) headButtons.Children.Add(HomeButton("main.expenses", async () => await Expenses()));
        headButtons.Children.Add(HomeButton("main.pin", () => { var d = Settings.Copy(); d.Topmost = !d.Topmost; SaveSettings(d); }));
        headButtons.Children.Add(Ui.LiteralButton("—", () => { if (MenuBarIcon != null) Hide(); else WindowState = WindowState.Minimized; }));
        headButtons.Children.Add(Ui.LiteralButton("×", Close)); Grid.SetColumn(headButtons, 1); head.Children.Add(headButtons); root.Children.Add(head);
        var body = new StackPanel { Width = 490, Spacing = 12 };
        if (!mini) { body.Children.Add(Ui.Row(Home("main.kicker", 10), Ui.Text(DateTime.Now.ToString("MM.dd / ddd", L.Culture), 10))); body.Children.Add(Home("main.headline", 27)); body.Children.Add(Home(Settings.Configured ? "main.subtitle" : "main.setupHint", 11)); }
        amount = new FlipNumber(); status = Home("main.live", 10); rate = Home("main.live", 11);
        body.Children.Add(Ui.Card(Ui.Stack(Home("main.today"), status, amount, rate)));
        if (!mini)
        {
            countdown = Home("main.countdownOff", 25); hours = Home("main.live", 11); progress = Ui.Progress(0);
            body.Children.Add(Ui.Card(Ui.Stack(countdown, Ui.Text(Settings.Start + " — " + Settings.End, 11), progress, hours)));
            hourly = Ui.Text(""); month = Ui.Text(""); body.Children.Add(Ui.Card(Ui.Stack(Home("main.hourly", 11), hourly, Home("main.month", 11), month)));
            goalHost = Ui.Stack(); body.Children.Add(Ui.Card(Ui.Stack(Ui.Row(Home("goal.heading", 11), HomeButton("goal.previous", () => { goalIndex--; UpdateGoal(); }), HomeButton("goal.next", () => { goalIndex++; UpdateGoal(); })), goalHost), true));
            body.Children.Add(Home("main.footnote", 9));
            if (Settings.StocksEnabled)
            {
                stockPnl = Ui.Text("", 26); stockHint = Ui.Text("", 11);
                body.Children.Add(Ui.Card(Ui.Stack(Ui.Row(Home("main.stock"), HomeButton("main.stockOpen", async () => await StockMonitor.Open(this))), stockPnl, stockHint)));
            }
        }
        var fit = new Viewbox { Child = body, Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 12, 0, 0) };
        Grid.SetRow(fit, 1); root.Children.Add(fit);
        var actions = Ui.Row(HomeButton("mode.privacy", () => { privacy = !privacy; Build(); }), HomeButton("mode.mini", () => { mini = !mini; Build(); }),
            HomeButton("main.demo", () => { demo = !demo; Tick(); }), HomeButton(Settings.Configured ? "settings.open" : "main.firstUse", async () => await OpenSettings()));
        Grid.SetRow(actions, 2); actions.Margin = new Thickness(0, 12, 0, 0); root.Children.Add(actions);
        Content = new Border { Child = root, CornerRadius = new CornerRadius(20), BorderBrush = Ui.Stroke, BorderThickness = new Thickness(1) };
        Fit(); Tick();
    }
    public void RestoreWindow() { Show(); WindowState = WindowState.Normal; Activate(); }
    void UpdateMenuBar()
    {
        NativeMenuItem Item(string key, Action action)
        { var item = new NativeMenuItem(L.T(key)); item.Click += (_, _) => action(); return item; }
        if (MenuBarIcon == null)
        {
            var menu = new NativeMenu();
            menu.Items.Add(Item("tray.show", RestoreWindow));
            menu.Items.Add(Item("tray.mini", () => { RestoreWindow(); mini = !mini; Build(); }));
            menu.Items.Add(Item("settings.open", async () => { RestoreWindow(); await OpenSettings(); }));
            menu.Items.Add(new NativeMenuItemSeparator()); menu.Items.Add(Item("tray.quit", Close));
            MenuBarIcon = new TrayIcon { Icon = Icon, Menu = menu, IsVisible = true };
            TrayIcon.SetIcons(Application.Current, new TrayIcons { MenuBarIcon });
        }
        MenuBarIcon.ToolTipText = L.T("app.name");
        // 保持原生菜单对象稳定，只更新文案 / Keep the native menu identity stable; update captions only.
        var keys = new[] { "tray.show", "tray.mini", "settings.open", "", "tray.quit" };
        for (int i = 0; i < keys.Length; i++) if (keys[i].Length > 0) ((NativeMenuItem)MenuBarIcon.Menu.Items[i]).Header = L.T(keys[i]);
    }
    void Fit()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        var max = screen == null ? 1000 : screen.WorkingArea.Height / screen.Scaling - 30;
        Height = Math.Min(max, mini ? 330 : Settings.StocksEnabled ? 1010 : 850);
    }
    public bool Privacy => privacy;
    public Snapshot Snapshot => Engine.Calculate(demo ? DateTime.Today.AddHours(14.3) : DateTime.Now, Settings);
    public async Task OpenSettings()
    {
        bool? enable = null;
        if (!Settings.Configured) { enable = await Ui.StockPrompt(this); if (!enable.HasValue) return; }
        var draft = Settings.Copy(); if (enable.HasValue) draft.StocksEnabled = enable.Value;
        var editor = new SettingsWindow(this, draft, Stocks.State, enable == true ? 5 : !Settings.Configured ? 1 : 0);
        await editor.ShowDialog(this); L.Use(Settings.Language); Ui.Theme(Settings.Theme); Build();
    }
    public void Apply(Settings candidate, PortfolioState portfolio)
    {
        if (Stocks.Busy) throw new ArgumentException(L.T("stock.busy"));
        var old = Settings.Copy(); candidate.Configured = true;
        if (!Samples) candidate.Save(Paths.SettingsPath);
        try { if (portfolio != null) Stocks.Save(portfolio); }
        catch { if (!Samples) old.Save(Paths.SettingsPath); throw; }
        Settings = candidate; Stocks.SetFeatureEnabled(candidate.StocksEnabled); L.Use(candidate.Language); Ui.Theme(candidate.Theme); Build();
        _ = Stocks.RefreshAsync();
    }
    void Tick()
    {
        if (amount == null) return;
        var s = Snapshot; amount.Set(privacy ? "••••••" : Engine.Format(s.Today, Settings), Settings);
        var key = demo ? "main.demoBadge" : s.StatusKey;
        if (key != lastStatus || status.Text != HomeCopy.T(Settings, key)) { status.Text = HomeCopy.T(Settings, key); lastStatus = key; }
        rate.Text = privacy ? "••••" : HomeCopy.T(Settings, "main.rate", Engine.Money(s.Rate, Settings));
        if (mini) return;
        countdown.Text = HomeCopy.T(Settings, s.CountdownKey) + "  " + Engine.Duration(s.Countdown);
        progress.Value = privacy ? 0 : s.Progress * 100; hours.Text = privacy ? "••••" : HomeCopy.T(Settings, "main.hours", s.Worked / 3600, s.Total / 3600);
        hourly.Text = privacy ? "••••" : Engine.Money(s.Hourly, Settings); month.Text = privacy ? "••••" : Engine.Money(s.Month, Settings);
        UpdateGoal();
        if (Settings.StocksEnabled && stockPnl != null)
        {
            var summary = StockMath.Summary(Stocks.State.Positions, Stocks.Quotes, DateTimeOffset.UtcNow);
            stockPnl.Text = privacy ? "••••" : summary.DayPnl.HasValue ? "$" + summary.DayPnl.Value.ToString("N2", CultureInfo.InvariantCulture) : "—";
            stockHint.Text = privacy ? HomeCopy.T(Settings, "main.stockHidden") : L.T(Stocks.StatusKey) + " · " + L.T("stock.tabSummary", summary.WatchTotal, summary.Total);
        }
    }
    void UpdateGoal()
    {
        if (mini || goalHost == null) return; goalHost.Children.Clear();
        var goals = Engine.Goals(Snapshot.Month, Settings);
        if (goals.Count == 0) { goalHost.Children.Add(Home("goal.emptyTitle")); return; }
        goalIndex = (goalIndex % goals.Count + goals.Count) % goals.Count; var g = goals[goalIndex];
        goalHost.Children.Add(Ui.Text(g.Goal.Name, 17)); goalHost.Children.Add(Ui.Text(privacy ? "••••" : Engine.Money(g.Goal.Amount, Settings)));
        goalHost.Children.Add(Ui.Progress(privacy ? 0 : g.Fraction));
        goalHost.Children.Add(Home(privacy ? "goal.hidden" : g.Fraction >= 1 ? "goal.done" : "goal.remaining", 11, Snapshot.Hourly > 0 ? g.RemainingToReach / Snapshot.Hourly : 0m));
    }
    public async Task Expenses()
    {
        var w = Ui.Window(L.T("expense.title"), 580, 600); var body = Ui.Stack();
        void Update()
        {
            body.Children.Clear(); body.Children.Add(Ui.Label("expense.explanation"));
            var coverage = Engine.ExpenseCoverage(Snapshot.Month, Settings);
            body.Children.Add(Ui.Text(privacy ? L.T("expense.hidden") : L.T("expense.summary", Engine.Money(coverage.Sum(x => x.Remaining), Settings), Engine.Money(Engine.ExpenseTotal(Settings), Settings))));
            body.Children.Add(Ui.Text(privacy ? "••••" : L.T("expense.rewardBalance") + "  " + Engine.Money(Engine.RewardIncome(Snapshot.Month, Settings), Settings)));
            foreach (var x in coverage) body.Children.Add(Ui.Card(Ui.Stack(Ui.Text(x.Item.Name),
                Ui.Text(privacy ? L.T("expense.hidden") : L.T("expense.remainingValue", Engine.Money(x.Remaining, Settings))), Ui.Progress(privacy ? 0 : x.RemainingFraction, true))));
        }
        Update(); var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) }; t.Tick += (_, _) => Update(); t.Start();
        w.Content = Ui.DialogBody(body, Ui.Button("action.cancel", w.Close)); w.Closed += (_, _) => t.Stop(); await w.ShowDialog(this);
    }
}
