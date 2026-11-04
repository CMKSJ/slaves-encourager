using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Layout;
using Avalonia.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace SlavesEncourager.Mac;

// 所有编辑只修改草稿，外部对象不共享 / All edits stay in a draft without sharing caller objects.
public sealed class StockDraft
{
    public PortfolioState State { get; private set; }
    public bool Changed { get; private set; }
    public event Action Updated;
    public StockDraft(PortfolioState state) => State = state.Copy();
    public void Stage(PortfolioState candidate) { candidate.Validate(); State = candidate.Copy(); Changed = true; Updated?.Invoke(); }
}

public sealed class StockPanel : StackPanel
{
    readonly StockDraft draft;
    readonly Window owner;
    readonly ListBox list = new() { Height = 185 };
    readonly TextBlock summary = Ui.Text("");
    readonly Func<bool> redact;
    public StockPanel(Window owner, StockDraft draft, Func<bool> redact = null)
    {
        this.owner = owner; this.draft = draft; this.redact = redact; Spacing = 10;
        Children.Add(Ui.Row(Ui.AsyncButton("watch.add", async () => await Manual(null)), Ui.AsyncButton("watch.edit", async () => await Manual(Selected())),
            Ui.Button("watch.remove", () =>
            {
                try { var selected = Selected(); var next = draft.State.Copy(); next.Positions.RemoveAll(p => p.Symbol == selected.Symbol); draft.Stage(next); }
                catch (ArgumentException e) { _ = Ui.Alert(owner, e.Message); }
            }), Ui.AsyncButton("stock.import", async () => { var next = await StockImport.Open(owner, draft.State); if (next != null) draft.Stage(next); })));
        Children.Add(summary); Children.Add(list); draft.Updated += Refresh; DetachedFromVisualTree += (_, _) => draft.Updated -= Refresh; Refresh();
    }
    StockPosition Selected()
    { if (list.SelectedIndex < 0 || list.SelectedIndex >= draft.State.Positions.Count) throw new ArgumentException(L.T("watch.select")); return draft.State.Positions[list.SelectedIndex]; }
    async Task Manual(StockPosition selected)
    { var next = await StockManual.Open(owner, draft.State, selected); if (next != null) draft.Stage(next); }
    public void Refresh()
    {
        summary.Text = L.T("stock.tabSummary", draft.State.Positions.Count(p => p.WatchOnly), draft.State.Positions.Count(p => !p.WatchOnly));
        list.ItemsSource = draft.State.Positions.Select(p => p.Symbol + "   ·   " + L.T(p.WatchOnly ? "watch.watching" : "watch.holding") +
            (p.WatchOnly ? "" : redact?.Invoke() == true ? "   ••••" : "   " + p.Quantity.ToString(CultureInfo.InvariantCulture) + "   @ " + p.AverageCost?.ToString(CultureInfo.InvariantCulture))).ToArray();
    }
}

public static class StockManual
{
    public static async Task<PortfolioState> Open(Window owner, PortfolioState original, StockPosition selected)
    {
        var w = Ui.Window(L.T("watch.addTitle"), 650, 650);
        var codes = Ui.Input(selected?.Symbol ?? "", 500); var only = Ui.Check("watch.only", selected?.WatchOnly ?? true);
        var quantity = Ui.Input(selected?.Quantity.ToString(CultureInfo.InvariantCulture) ?? "", 24);
        var cost = Ui.Input(selected?.AverageCost?.ToString(CultureInfo.InvariantCulture) ?? "", 24);
        void State() { quantity.IsEnabled = cost.IsEnabled = only.IsChecked != true; } only.IsCheckedChanged += (_, _) => State(); State();
        var online = Ui.Check("stock.onlineConsent", original.Online); var error = Ui.Text("", 12, Ui.Red);
        w.Content = Ui.DialogBody(Ui.Stack(Ui.Label("watch.help"), Ui.Label("watch.codes"), codes, only, Ui.Label("stock.quantity"), quantity,
            Ui.Label("stock.cost"), cost, online, Ui.Label("watch.manualRule", 11), error), Ui.Row(Ui.Button("action.cancel", w.Close), Ui.Button("watch.save", () =>
            {
                try
                {
                    var next = StockMath.Manual(original, codes.Text, only.IsChecked == true,
                        only.IsChecked == true ? null : Ui.Number(quantity.Text, "stock.invalidPosition"),
                        only.IsChecked == true || string.IsNullOrWhiteSpace(cost.Text) ? null : Ui.Number(cost.Text, "stock.invalidPosition"), selected?.Symbol);
                    next.Online = online.IsChecked == true; next.ConfirmedUtc = DateTimeOffset.UtcNow.ToString("o"); next.Validate(); w.Close(next);
                }
                catch (ArgumentException e) { error.Text = e.Message; }
            })));
        return await w.ShowDialog<PortfolioState>(owner);
    }
}

public sealed class StockImport : Window
{
    readonly PortfolioState original;
    readonly TextBox raw = new() { AcceptsReturn = true, Height = 125, MaxLength = HoldingImport.MaxText };
    readonly CheckBox hasHeader = Ui.Check("stock.hasHeader", true), usd = Ui.Check("stock.assumeUsd", false);
    readonly TextBox header = Ui.Input("1", 4);
    readonly ComboBox[] mapping = new ComboBox[4];
    readonly CheckBox online;
    readonly TextBlock notice = Ui.Text("", 11), error = Ui.Text("", 11, Ui.Red);
    readonly ListBox preview = new() { Height = 140 };
    readonly Button confirm;
    List<List<string>> table;
    List<StockPosition> positions;
    string mappedText;
    public StockImport(PortfolioState state)
    {
        original = state.Copy(); online = Ui.Check("stock.onlineConsent", state.Online);
        Title = L.T("stock.importTitle"); Width = 820; Height = 850; MinWidth = 650; MinHeight = 600; Background = Ui.Bg; Foreground = Ui.Ink; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var body = Ui.Stack(Ui.Label("stock.importHelp"));
        body.Children.Add(Ui.Row(Ui.AsyncButton("stock.file", FileImport), Ui.AsyncButton("stock.paste", Paste), Ui.AsyncButton("stock.image", () => Image(false)), Ui.AsyncButton("stock.imagePaste", () => Image(true)), Ui.AsyncButton("stock.extension", () => Ui.Alert(this, L.T("stock.extensionHelp")))));
        body.Children.Add(raw); header.Width = 55;
        body.Children.Add(Ui.Row(hasHeader, Ui.Label("stock.headerRow"), header, Ui.Button("stock.parse", Parse)));
        var columns = Ui.Row(); var keys = new[] { "stock.symbol", "stock.quantity", "stock.cost", "stock.currency" };
        for (int i = 0; i < mapping.Length; i++)
        {
            mapping[i] = new ComboBox { Width = 160 }; mapping[i].SelectionChanged += (_, _) => InvalidatePreview(); columns.Children.Add(Ui.Stack(Ui.Label(keys[i]), mapping[i]));
        }
        body.Children.Add(columns); body.Children.Add(usd); body.Children.Add(preview); body.Children.Add(online); body.Children.Add(Ui.Label("stock.snapshotRule", 11)); body.Children.Add(notice); body.Children.Add(error);
        confirm = Ui.Button("stock.confirmSnapshot", Confirm); confirm.IsEnabled = false;
        Content = Ui.DialogBody(body, Ui.Row(Ui.Button("action.cancel", Close), confirm));
        raw.PropertyChanged += (_, e) => { if (e.Property == TextBox.TextProperty) InvalidatePreview(); }; hasHeader.IsCheckedChanged += (_, _) => InvalidatePreview(); header.PropertyChanged += (_, e) => { if (e.Property == TextBox.TextProperty) InvalidatePreview(); };
        if (state.Positions.Any(p => !p.WatchOnly)) raw.Text = "Symbol,Quantity,AverageCost,Currency\n" + string.Join("\n", state.Positions.Where(p => !p.WatchOnly).Select(p => p.Symbol + "," + p.Quantity.ToString(CultureInfo.InvariantCulture) + "," + p.AverageCost?.ToString(CultureInfo.InvariantCulture) + ",USD"));
    }
    void InvalidatePreview() { positions = null; if (confirm != null) confirm.IsEnabled = false; }
    async Task<string> Pick(params string[] patterns)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false, FileTypeFilter = new[] { new FilePickerFileType(L.T("stock.file")) { Patterns = patterns } } });
        return files.FirstOrDefault()?.TryGetLocalPath();
    }
    async Task FileImport()
    {
        string path = await Pick("*.csv", "*.tsv", "*.txt", "*.html", "*.xlsx", "*.json"); if (path == null) return;
        if (new FileInfo(path).Length > 5000000) throw new ArgumentException(L.T("stock.fileLimit"));
        if (Path.GetExtension(path).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        { SetTable(HoldingImport.ReadXlsx(path)); notice.Text = L.T("stock.firstSheet"); }
        else raw.Text = new UTF8Encoding(false, true).GetString(await File.ReadAllBytesAsync(path)); Parse();
    }
    async Task Paste()
    {
        var html = await Clipboard.TryGetValueAsync(DataFormat.CreateStringPlatformFormat(OperatingSystem.IsMacOS() ? "public.html" : "HTML Format"));
        raw.Text = html != null && html.Contains("<table", StringComparison.OrdinalIgnoreCase) ? html : await Clipboard.TryGetTextAsync() ?? throw new ArgumentException(L.T("stock.noText")); Parse();
    }
    async Task Image(bool clipboard)
    {
        string path = null, temporary = null;
        try
        {
            if (clipboard)
            {
                using var bitmap = await Clipboard.TryGetBitmapAsync() ?? throw new ArgumentException(L.T("stock.noImage"));
                temporary = Path.Combine(Path.GetTempPath(), "slaves-ocr-" + Guid.NewGuid().ToString("N") + ".png"); bitmap.Save(temporary); path = temporary;
            }
            else path = await Pick("*.png", "*.jpg", "*.jpeg", "*.tiff");
            if (path == null) return; notice.Text = L.T("stock.ocrWorking"); raw.Text = await MacOcr.Read(path); notice.Text = L.T("stock.ocrReview"); Parse();
        }
        finally { if (temporary != null) File.Delete(temporary); }
    }
    void SetTable(List<List<string>> rows) => raw.Text = string.Join("\n", rows.Select(row => string.Join("\t", row.Select(v => v.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ')))));
    public void SetRawForCheck(string text) => raw.Text = text;
    public int PreviewCount => positions?.Count ?? 0;
    public void Parse()
    {
        try
        {
            if (mappedText != raw.Text)
            {
                table = HoldingImport.ParseText(raw.Text); if (table.Count == 0) throw new ArgumentException(L.T("stock.noPositions"));
                int h = table.FindIndex(r => HoldingImport.GuessColumn(r, "symbol") >= 0 && HoldingImport.GuessColumn(r, "quantity") >= 0);
                hasHeader.IsChecked = h >= 0; header.Text = (h >= 0 ? h + 1 : 1).ToString(); var row = table[h >= 0 ? h : 0];
                var kinds = new[] { "symbol", "quantity", "cost", "currency" };
                for (int i = 0; i < mapping.Length; i++)
                {
                    mapping[i].ItemsSource = new[] { L.T("stock.none") }.Concat(Enumerable.Range(0, table.Max(r => r.Count)).Select(c => (c + 1) + " · " + (c < row.Count ? row[c] : ""))).ToArray();
                    mapping[i].SelectedIndex = h >= 0 ? HoldingImport.GuessColumn(row, kinds[i]) + 1 : 0;
                }
                mappedText = raw.Text;
            }
            if (!int.TryParse(header.Text, out int start) || start < 1 || start > table.Count) throw new ArgumentException(L.T("stock.mappingRequired"));
            positions = HoldingImport.Map(table, hasHeader.IsChecked == true ? start - 1 : -1, mapping[0].SelectedIndex - 1, mapping[1].SelectedIndex - 1, mapping[2].SelectedIndex - 1, mapping[3].SelectedIndex - 1);
            preview.ItemsSource = positions.Select(p => p.Symbol + "   " + p.Quantity.ToString(CultureInfo.InvariantCulture) + "   " + p.AverageCost?.ToString(CultureInfo.InvariantCulture) + "   USD").ToArray();
            confirm.IsEnabled = true; error.Text = L.T("stock.previewCount", positions.Count, original.Positions.Count(p => !p.WatchOnly));
        }
        catch (Exception e) { InvalidatePreview(); error.Text = e.Message; }
    }
    void Confirm()
    {
        try
        {
            if (positions == null) throw new ArgumentException(L.T("stock.previewFirst"));
            if (mapping[3].SelectedIndex <= 0 && usd.IsChecked != true) throw new ArgumentException(L.T("stock.usdRequired"));
            var symbols = positions.Select(p => p.Symbol).ToHashSet();
            var next = new PortfolioState { Positions = positions.Concat(original.Positions.Where(p => p.WatchOnly && !symbols.Contains(p.Symbol))).ToList(), Online = online.IsChecked == true, ConfirmedUtc = DateTimeOffset.UtcNow.ToString("o") };
            next.Validate(); Close(next);
        }
        catch (Exception e) { error.Text = e.Message; }
    }
    public static async Task<PortfolioState> Open(Window owner, PortfolioState state) => await new StockImport(state).ShowDialog<PortfolioState>(owner);
}

public static class MacOcr
{
    public static async Task<string> Read(string path)
    {
        if (new FileInfo(path).Length > 15000000) throw new ArgumentException(L.T("stock.ocrUnavailable"));
        var helper = Path.Combine(AppContext.BaseDirectory, "StockOcr");
        if (!OperatingSystem.IsMacOS() || !File.Exists(helper)) throw new ArgumentException(L.T("stock.ocrUnavailable"));
        var start = new ProcessStartInfo(helper) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; start.ArgumentList.Add(path);
        using var process = Process.Start(start); using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var output = process.StandardOutput.ReadToEndAsync(timeout.Token); var errors = process.StandardError.ReadToEndAsync(timeout.Token);
        try { await process.WaitForExitAsync(timeout.Token); var text = await output; _ = await errors; if (process.ExitCode != 0 || text.Length > HoldingImport.MaxText || string.IsNullOrWhiteSpace(text)) throw new ArgumentException(L.T("stock.ocrUnavailable")); return text; }
        catch (OperationCanceledException) { process.Kill(true); throw new ArgumentException(L.T("stock.ocrUnavailable")); }
    }
}

public static class StockMonitor
{
    public static async Task Open(MainWindow main)
    {
        var w = Ui.Window(L.T("stock.title"), 800, 740); var draft = new StockDraft(main.Stocks.State); var rows = Ui.Stack();
        var panel = new StockPanel(w, draft, () => main.Privacy); var message = Ui.Text("", 11);
        bool restoring = false;
        draft.Updated += () =>
        {
            if (restoring) return;
            try { main.Stocks.Save(draft.State); _ = main.Stocks.RefreshAsync(); }
            catch (Exception e) { restoring = true; draft.Stage(main.Stocks.State); restoring = false; _ = Ui.Alert(w, e.Message); }
        };
        var online = Ui.Check("stock.onlineConsent", main.Stocks.State.Online);
        online.IsCheckedChanged += (_, _) =>
        { try { if (online.IsChecked != true) main.Stocks.StopOnline(); else { var next = main.Stocks.State.Copy(); next.Online = true; main.Stocks.Save(next); _ = main.Stocks.RefreshAsync(); } } catch (Exception e) { _ = Ui.Alert(w, e.Message); } };
        void Update()
        {
            panel.IsEnabled = !main.Stocks.Busy; panel.Refresh();
            rows.Children.Clear(); message.Text = L.T(main.Stocks.StatusKey);
            foreach (var p in main.Stocks.State.Positions)
            {
                main.Stocks.Quotes.TryGetValue(p.Symbol, out var q); bool price = StockMath.PriceUsable(q, DateTimeOffset.UtcNow), usable = StockMath.Usable(q, DateTimeOffset.UtcNow);
                string value = main.Privacy ? "••••" : price ? "$" + q.Price.ToString("N2", CultureInfo.InvariantCulture) : "—";
                string change = main.Privacy ? "••••" : usable ? ((q.Price / q.PreviousClose - 1) * 100).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + "%" : "—";
                string qty = main.Privacy ? "••••" : p.Quantity.ToString(CultureInfo.InvariantCulture);
                string pnl = main.Privacy ? "••••" : usable ? "$" + (p.Quantity * (q.Price - q.PreviousClose)).ToString("N2", CultureInfo.InvariantCulture) : "—";
                string cost = main.Privacy ? "••••" : p.AverageCost?.ToString("N2", CultureInfo.InvariantCulture) ?? "—";
                string prior = main.Privacy ? "••••" : usable ? q.PreviousClose.ToString("N2", CultureInfo.InvariantCulture) : "—";
                rows.Children.Add(Ui.Card(Ui.Stack(Ui.Text(p.Symbol + "   " + L.T(p.WatchOnly ? "watch.watching" : "watch.holding") + "   " + value + "   " + change, 16),
                    Ui.Text(p.WatchOnly ? L.T("watch.manualRule") : L.T("stock.quantity") + ": " + qty + "  ·  " + L.T("stock.dayPnlShort") + ": " + pnl, 11),
                    Ui.Text(L.T("stock.previous") + ": " + prior + (p.WatchOnly ? "" : "  ·  " + L.T("stock.cost") + ": " + cost), 11),
                    Ui.Text(q == null ? L.T("stock.unavailable") : (q.Simulated ? L.T("stock.simulated") : L.T(price ? "stock.quoteEstimated" : "stock.stale")) + "  ·  " + StockMath.InNewYork(q.AsOfUtc).ToString("yyyy-MM-dd HH:mm") + " ET", 10))));
            }
        }
        var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) }; t.Tick += (_, _) => Update(); t.Start();
        w.Content = Ui.DialogBody(Ui.Stack(Ui.Label("stock.footer", 11), panel, online, Ui.Row(Ui.AsyncButton("stock.refresh", main.Stocks.RefreshAsync), Ui.Button("stock.stop", () => { main.Stocks.StopOnline(); online.IsChecked = false; }),
            Ui.AsyncButton("stock.clear", async () => { if (main.Stocks.Busy) throw new ArgumentException(L.T("stock.busy")); if (await Ui.Confirm(w, L.T("stock.clearConfirm"))) { draft.Stage(new PortfolioState()); online.IsChecked = false; } })), message, rows), Ui.Button("action.cancel", w.Close));
        w.Closed += (_, _) => t.Stop(); Update(); await w.ShowDialog(main);
    }
}
