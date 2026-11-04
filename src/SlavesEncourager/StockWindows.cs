using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SlavesEncourager
{
    public static class StockUi
    {
        public static Window Create(Window owner, string key, double width, double height)
        {
            var window = new Window { Owner = owner, Title = L.T(key), Width = width, Height = Math.Min(height, SystemParameters.WorkArea.Height - 12), MinWidth = Math.Min(width, 650), MinHeight = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Icon = owner.Icon, FontFamily = owner.FontFamily, FontSize = 12 };
            foreach (object k in owner.Resources.Keys) window.Resources[k] = owner.Resources[k];
            window.Resources.Remove(typeof(System.Windows.Controls.Primitives.ScrollBar));
            L.Apply(window.Resources); window.SetResourceReference(Control.BackgroundProperty, "BackgroundBrush"); window.SetResourceReference(Control.ForegroundProperty, "Ink"); return window;
        }
        public static TextBlock Label(string key, double size)
        { return new TextBlock { Text = L.T(key), FontSize = size, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 8) }; }
        public static Button Button(string key, Action click)
        {
            var button = new Button { Content = L.T(key), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 7, 7) };
            button.Click += delegate { click(); }; return button;
        }
        public static TextBox Input(bool multiline)
        {
            var input = new TextBox { Padding = new Thickness(9), AcceptsReturn = multiline, AcceptsTab = multiline, TextWrapping = TextWrapping.NoWrap, VerticalScrollBarVisibility = multiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
            input.SetResourceReference(Control.BackgroundProperty, "Surface"); input.SetResourceReference(Control.ForegroundProperty, "Ink"); return input;
        }
        public static DataGrid Grid()
        {
            var grid = new DataGrid { IsReadOnly = true, AutoGenerateColumns = false, CanUserAddRows = false, CanUserDeleteRows = false, HeadersVisibility = DataGridHeadersVisibility.Column, RowHeaderWidth = 0, GridLinesVisibility = DataGridGridLinesVisibility.Horizontal, Margin = new Thickness(0, 8, 0, 8) };
            grid.SetResourceReference(Control.BackgroundProperty, "Surface"); grid.SetResourceReference(Control.ForegroundProperty, "Ink"); grid.SetResourceReference(DataGrid.RowBackgroundProperty, "Surface"); grid.SetResourceReference(DataGrid.AlternatingRowBackgroundProperty, "SurfaceAlt");
            var header = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader)); header.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension("SurfaceAlt"))); header.Setters.Add(new Setter(Control.ForegroundProperty, new DynamicResourceExtension("Ink"))); header.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(9, 7, 9, 7))); grid.ColumnHeaderStyle = header;
            var headerText = new FrameworkElementFactory(typeof(TextBlock)); headerText.SetBinding(TextBlock.TextProperty, new Binding()); headerText.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap); headerText.SetValue(TextBlock.FontSizeProperty, 11.0);
            header.Setters.Add(new Setter(ContentControl.ContentTemplateProperty, new DataTemplate { VisualTree = headerText }));
            header.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            grid.Loaded += delegate { FitColumns(grid); }; grid.SizeChanged += delegate { FitColumns(grid); };
            return grid;
        }
        static void FitColumns(DataGrid grid)
        {
            double total = grid.Columns.Sum(c => c.Width.Value), available = grid.ActualWidth - 22;
            if (total <= 0 || available <= 0) return;
            foreach (var column in grid.Columns) column.Width = new DataGridLength(Math.Max(80, available * column.Width.Value / total));
        }
        public static void Column(DataGrid grid, string key, string property, double width)
        {
            var text = new Style(typeof(TextBlock)); text.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis)); text.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(5, 4, 5, 4)));
            text.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, new Binding("Text") { RelativeSource = RelativeSource.Self }));
            grid.Columns.Add(new DataGridTextColumn { Header = L.T(key), Binding = new Binding(property), ElementStyle = text, MinWidth = 80, Width = new DataGridLength(width * 120) });
        }
        public static string Dollars(decimal value) { return (value > 0m ? "+" : value < 0m ? "-" : "") + "$" + Math.Abs(value).ToString("N2", CultureInfo.InvariantCulture); }
    }

    public sealed class StockWelcomeWindow
    {
        public Window Window { get; private set; }
        public bool? Choice { get; private set; }
        public readonly Button Enable, Later, Cancel;
        public StockWelcomeWindow(Window owner)
        {
            Window = StockUi.Create(owner, "stock.welcomeTitle", 590, 320); Window.MinHeight = 260; Window.MinWidth = 520; Window.ResizeMode = ResizeMode.NoResize;
            var root = new DockPanel { Margin = new Thickness(24) };
            var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
            Cancel = StockUi.Button("action.cancel", delegate { Window.Close(); });
            Later = StockUi.Button("stock.welcomeNo", delegate { Choice = false; Window.DialogResult = true; });
            Enable = StockUi.Button("stock.welcomeYes", delegate { Choice = true; Window.DialogResult = true; });
            actions.Children.Add(Cancel); actions.Children.Add(Later); actions.Children.Add(Enable); DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
            var body = new StackPanel(); body.Children.Add(StockUi.Label("stock.welcomeTitle", 23)); body.Children.Add(StockUi.Label("stock.welcomeHelp", 13)); root.Children.Add(body); Window.Content = root;
        }
    }

    // 设置内使用独立草稿，关闭或取消不会写入持仓 / Settings use an isolated draft; closing or cancelling never writes holdings.
    public sealed class StockSettingsPanel : StackPanel
    {
        sealed class DraftRow
        { public string Symbol { get; set; } public string Mode { get; set; } public string Quantity { get; set; } public string Cost { get; set; } }
        readonly Window owner;
        readonly DataGrid grid = StockUi.Grid();
        readonly TextBlock summary = new TextBlock { TextWrapping = TextWrapping.Wrap }, error = new TextBlock { TextWrapping = TextWrapping.Wrap };
        PortfolioState draft;
        public bool Changed { get; private set; }
        public PortfolioState Draft { get { return draft.Copy(); } }
        public StockSettingsPanel(Window window, PortfolioState initial)
        {
            owner = window; draft = initial.Copy();
            var actions = new WrapPanel();
            actions.Children.Add(StockUi.Button("watch.add", delegate { Edit(null); }));
            actions.Children.Add(StockUi.Button("watch.edit", delegate { var row = grid.SelectedItem as DraftRow; if (row == null) { error.Text = L.T("watch.select"); return; } Edit(row.Symbol); }));
            actions.Children.Add(StockUi.Button("watch.remove", delegate
            { var row = grid.SelectedItem as DraftRow; if (row == null) { error.Text = L.T("watch.select"); return; } var next = Draft; next.Positions.RemoveAll(p => p.Symbol == row.Symbol); if (next.Positions.Count == 0) next.Online = false; Stage(next); }));
            actions.Children.Add(StockUi.Button("stock.import", delegate { var editor = new StockImportWindow(owner, draft); if (editor.Window.ShowDialog() == true) Stage(editor.Result); }));
            string[] actionKeys = { "watch.add", "watch.edit", "watch.remove", "stock.import" };
            for (int i = 0; i < actions.Children.Count; i++) ((Button)actions.Children[i]).SetResourceReference(ContentControl.ContentProperty, actionKeys[i]);
            Children.Add(actions); Children.Add(summary); grid.Height = 155; grid.SelectionMode = DataGridSelectionMode.Single;
            StockUi.Column(grid, "stock.symbol", "Symbol", 1); StockUi.Column(grid, "watch.mode", "Mode", 1); StockUi.Column(grid, "stock.quantity", "Quantity", 1); StockUi.Column(grid, "stock.cost", "Cost", 1.4);
            Children.Add(grid); var help = StockUi.Label("stock.tabHelp", 11); help.SetResourceReference(TextBlock.TextProperty, "stock.tabHelp"); Children.Add(help); error.SetResourceReference(TextBlock.ForegroundProperty, "ExpenseRed"); Children.Add(error); RefreshLanguage();
        }
        public void Stage(PortfolioState next) { next.Validate(); draft = next.Copy(); Changed = true; error.Text = ""; RenderRows(); }
        void Edit(string symbol) { var editor = new StockManualWindow(owner, draft, symbol); if (editor.Window.ShowDialog() == true) Stage(editor.Result); }
        void RenderRows()
        {
            summary.Text = L.T("stock.tabSummary", draft.Positions.Count(p => p.WatchOnly), draft.Positions.Count(p => !p.WatchOnly));
            grid.ItemsSource = draft.Positions.Select(p => new DraftRow { Symbol = p.Symbol, Mode = L.T(p.WatchOnly ? "watch.watching" : "watch.holding"), Quantity = p.WatchOnly ? "—" : p.Quantity.ToString(CultureInfo.InvariantCulture), Cost = p.AverageCost.HasValue ? p.AverageCost.Value.ToString(CultureInfo.InvariantCulture) : "—" }).ToList();
        }
        public void RefreshLanguage()
        {
            // 文案刷新保留输入数据和草稿 / Refresh labels without changing draft data.
            string[] keys = { "stock.symbol", "watch.mode", "stock.quantity", "stock.cost" };
            for (int i = 0; i < grid.Columns.Count; i++) grid.Columns[i].Header = L.T(keys[i]);
            RenderRows();
        }
    }

    public sealed class StockManualWindow
    {
        public Window Window { get; private set; }
        public PortfolioState Result { get; private set; }
        public readonly TextBox Codes = StockUi.Input(true), Shares = StockUi.Input(false), Cost = StockUi.Input(false);
        public readonly CheckBox Watch = new CheckBox(), Online = new CheckBox();
        public readonly Button Save;
        readonly TextBlock error = new TextBlock { TextWrapping = TextWrapping.Wrap };
        public StockManualWindow(Window owner, PortfolioState state, string editing)
        {
            PortfolioState original = state.Copy();
            StockPosition existing = editing == null ? null : original.Positions.Single(p => p.Symbol == editing);
            Window = StockUi.Create(owner, "watch.addTitle", 690, 700);
            var root = new DockPanel { Margin = new Thickness(24) };
            var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
            actions.Children.Add(StockUi.Button("action.cancel", delegate { Window.Close(); }));
            Save = StockUi.Button("watch.save", delegate
            {
                try
                {
                    decimal shares = 0m, cost = 0m; bool watch = Watch.IsChecked == true;
                    if (!watch && (!HoldingImport.Number(Shares.Text, out shares) || (Cost.Text.Trim().Length > 0 && !HoldingImport.Number(Cost.Text, out cost)))) throw new ArgumentException(L.T("stock.invalidPosition"));
                    var candidate = StockMath.Manual(original, Codes.Text, watch, watch ? null : (decimal?)shares, watch || Cost.Text.Trim().Length == 0 ? null : (decimal?)cost, editing);
                    candidate.Online = Online.IsChecked == true; candidate.ConfirmedUtc = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                    Result = candidate; Window.DialogResult = true;
                }
                catch (ArgumentException ex) { error.Text = ex.Message; }
            }); actions.Children.Add(Save); DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
            var body = new StackPanel(); body.Children.Add(StockUi.Label("watch.addTitle", 23)); body.Children.Add(StockUi.Label("watch.help", 12)); body.Children.Add(StockUi.Label("watch.codes", 12));
            Codes.Height = 65; Codes.MaxLength = 1000; Codes.Text = existing == null ? "" : existing.Symbol; body.Children.Add(Codes);
            Watch.Content = StockUi.Label("watch.only", 12); Watch.IsChecked = existing == null || existing.WatchOnly; Watch.Margin = new Thickness(0, 10, 0, 2); body.Children.Add(Watch);
            body.Children.Add(StockUi.Label("stock.quantity", 12)); Shares.Text = existing == null || existing.WatchOnly ? "" : existing.Quantity.ToString(CultureInfo.InvariantCulture); Shares.MaxLength = 32; body.Children.Add(Shares);
            body.Children.Add(StockUi.Label("stock.cost", 12)); Cost.Text = existing == null || !existing.AverageCost.HasValue ? "" : existing.AverageCost.Value.ToString(CultureInfo.InvariantCulture); Cost.MaxLength = 32; body.Children.Add(Cost);
            Action mode = delegate { Shares.IsEnabled = Cost.IsEnabled = Watch.IsChecked != true; };
            Watch.Checked += delegate { mode(); }; Watch.Unchecked += delegate { mode(); }; mode();
            Online.Content = StockUi.Label("stock.onlineConsent", 12); Online.IsChecked = original.Online; Online.Margin = new Thickness(0, 12, 0, 8); body.Children.Add(Online);
            body.Children.Add(StockUi.Label("watch.manualRule", 11)); error.SetResourceReference(TextBlock.ForegroundProperty, "ExpenseRed"); body.Children.Add(error);
            root.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); Window.Content = root;
        }
    }

    public sealed class StockImportWindow
    {
        public Window Window { get; private set; }
        public PortfolioState Result { get; private set; }
        readonly TextBox raw = StockUi.Input(true), headerRow = StockUi.Input(false);
        readonly ComboBox symbol = new ComboBox(), quantity = new ComboBox(), cost = new ComboBox(), currency = new ComboBox();
        readonly CheckBox header = new CheckBox(), usd = new CheckBox(), online = new CheckBox();
        readonly TextBlock error = new TextBlock { TextWrapping = TextWrapping.Wrap }, note = new TextBlock { TextWrapping = TextWrapping.Wrap };
        readonly DataGrid preview = StockUi.Grid();
        readonly Button confirm;
        readonly PortfolioState original;
        List<List<string>> table;
        List<StockPosition> positions;
        string mappedText;
        bool updating, ocrBusy;

        public StockImportWindow(Window owner, PortfolioState state)
        {
            original = state.Copy(); Window = StockUi.Create(owner, "stock.importTitle", 930, 900);
            var root = new Grid { Margin = new Thickness(22) };
            foreach (var h in new[] { GridLength.Auto, GridLength.Auto, GridLength.Auto, new GridLength(150), GridLength.Auto, GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto, GridLength.Auto }) root.RowDefinitions.Add(new RowDefinition { Height = h });
            Add(root, StockUi.Label("stock.importTitle", 23), 0); Add(root, StockUi.Label("stock.importHelp", 11), 1);
            var routes = new WrapPanel();
            routes.Children.Add(StockUi.Button("stock.file", delegate { FileImport(); }));
            routes.Children.Add(StockUi.Button("stock.paste", delegate
            {
                try
                {
                    string text = Clipboard.ContainsText(TextDataFormat.Html) ? Clipboard.GetText(TextDataFormat.Html) : null;
                    if (text == null || text.IndexOf("<table", StringComparison.OrdinalIgnoreCase) < 0) text = Clipboard.ContainsText() ? Clipboard.GetText() : null;
                    if (text == null) throw new ArgumentException(L.T("stock.noText")); raw.Text = text; ParsePreview();
                }
                catch (Exception ex) { ShowError(ex); }
            }));
            routes.Children.Add(StockUi.Button("stock.image", async delegate { await ChooseImage(false); }));
            routes.Children.Add(StockUi.Button("stock.imagePaste", async delegate { await ChooseImage(true); }));
            routes.Children.Add(StockUi.Button("stock.extension", delegate { MessageBox.Show(Window, L.T("stock.extensionHelp"), L.T("stock.importTitle")); }));
            Add(root, routes, 2); raw.MaxLength = HoldingImport.MaxText; Add(root, raw, 3);
            raw.TextChanged += delegate { positions = null; if (confirm != null) confirm.IsEnabled = false; };
            var headerControls = new WrapPanel { Margin = new Thickness(0, 8, 0, 5), VerticalAlignment = VerticalAlignment.Center };
            header.Content = L.T("stock.hasHeader"); header.IsChecked = true; header.Margin = new Thickness(0, 6, 12, 0); headerControls.Children.Add(header);
            headerControls.Children.Add(StockUi.Label("stock.headerRow", 11)); headerRow.Text = "1"; headerRow.Width = 50; headerRow.Margin = new Thickness(8, 0, 12, 0); headerControls.Children.Add(headerRow);
            headerControls.Children.Add(StockUi.Button("stock.parse", delegate { ParsePreview(); })); Add(root, headerControls, 4);
            var mapping = new Grid();
            var names = new[] { "stock.symbol", "stock.quantity", "stock.cost", "stock.currency" }; var boxes = new[] { symbol, quantity, cost, currency };
            for (int i = 0; i < 4; i++)
            {
                mapping.ColumnDefinitions.Add(new ColumnDefinition()); var group = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
                group.Children.Add(StockUi.Label(names[i], 11)); boxes[i].MinHeight = 31; group.Children.Add(boxes[i]); Grid.SetColumn(group, i); mapping.Children.Add(group);
                boxes[i].SelectionChanged += delegate { if (!updating) { positions = null; confirm.IsEnabled = false; } };
            }
            Add(root, mapping, 5); usd.Content = StockUi.Label("stock.assumeUsd", 12); usd.Margin = new Thickness(0, 8, 0, 1); Add(root, usd, 6);
            StockUi.Column(preview, "stock.symbol", "Symbol", 1); StockUi.Column(preview, "stock.quantity", "Quantity", 1); StockUi.Column(preview, "stock.cost", "AverageCost", 1); StockUi.Column(preview, "stock.currency", "Currency", 1); Add(root, preview, 7);
            var messages = new StackPanel(); online.Content = StockUi.Label("stock.onlineConsent", 12); online.IsChecked = state.Online; online.Margin = new Thickness(0, 5, 0, 8); messages.Children.Add(online);
            note.Text = L.T("stock.snapshotRule"); note.Margin = new Thickness(0, 0, 0, 5); messages.Children.Add(note); error.SetResourceReference(TextBlock.ForegroundProperty, "ExpenseRed"); messages.Children.Add(error); Add(root, messages, 8);
            var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            actions.Children.Add(StockUi.Button("action.cancel", delegate { Window.Close(); }));
            confirm = StockUi.Button("stock.confirmSnapshot", delegate
            {
                try
                {
                    if (positions == null) throw new ArgumentException(L.T("stock.previewFirst"));
                    if (currency.SelectedIndex <= 0 && usd.IsChecked != true) throw new ArgumentException(L.T("stock.usdRequired"));
                    var imported = new HashSet<string>(positions.Select(p => p.Symbol));
                    var candidate = new PortfolioState { Positions = positions.Concat(original.Positions.Where(p => p.WatchOnly && !imported.Contains(p.Symbol))).ToList(), Online = online.IsChecked == true, ConfirmedUtc = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture) }; candidate.Validate(); Result = candidate; Window.DialogResult = true;
                }
                catch (Exception ex) { ShowError(ex); }
            }); confirm.IsEnabled = false; actions.Children.Add(confirm); Add(root, actions, 9); Window.Content = root;
            header.Checked += delegate { positions = null; confirm.IsEnabled = false; };
            header.Unchecked += delegate { positions = null; confirm.IsEnabled = false; };
            headerRow.TextChanged += delegate { positions = null; confirm.IsEnabled = false; };
            if (state.Positions.Any(p => !p.WatchOnly))
                raw.Text = "Symbol,Quantity,AverageCost,Currency\n" + String.Join("\n", state.Positions.Where(p => !p.WatchOnly).Select(p => p.Symbol + "," + p.Quantity.ToString(CultureInfo.InvariantCulture) + "," + (p.AverageCost.HasValue ? p.AverageCost.Value.ToString(CultureInfo.InvariantCulture) : "") + ",USD"));
        }
        static void Add(Grid grid, UIElement element, int row) { Grid.SetRow(element, row); grid.Children.Add(element); }
        void ShowError(Exception ex)
        {
            if (!(ex is ArgumentException || ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is System.Runtime.InteropServices.ExternalException || ex is FormatException || ex is InvalidCastException || ex is System.Xml.XmlException || ex is System.ComponentModel.Win32Exception || ex is KeyNotFoundException || ex is IndexOutOfRangeException)) throw ex;
            error.Text = ex is ArgumentException ? ex.Message : L.T("stock.importFailed"); confirm.IsEnabled = false;
        }
        void FileImport()
        {
            var dialog = new OpenFileDialog { Filter = "Holdings (*.csv;*.tsv;*.txt;*.xlsx;*.json)|*.csv;*.tsv;*.txt;*.xlsx;*.json", CheckFileExists = true };
            if (dialog.ShowDialog(Window) != true) return;
            try
            {
                if (new FileInfo(dialog.FileName).Length > 5000000) throw new ArgumentException(L.T("stock.fileLimit"));
                if (Path.GetExtension(dialog.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    var rows = HoldingImport.ReadXlsx(dialog.FileName);
                    raw.Text = String.Join("\n", rows.Select(row => String.Join("\t", row.Select(v => v.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ')))));
                    note.Text = L.T("stock.firstSheet") + " " + L.T("stock.snapshotRule");
                }
                else raw.Text = new UTF8Encoding(false, true).GetString(File.ReadAllBytes(dialog.FileName));
                ParsePreview();
            }
            catch (Exception ex) { ShowError(ex); }
        }
        void ParsePreview()
        {
            try
            {
                if (mappedText != raw.Text)
                {
                    table = HoldingImport.ParseText(raw.Text); if (table.Count == 0) throw new ArgumentException(L.T("stock.noPositions"));
                    int h = table.FindIndex(row => HoldingImport.GuessColumn(row, "symbol") >= 0 && HoldingImport.GuessColumn(row, "quantity") >= 0);
                    header.IsChecked = h >= 0; headerRow.Text = (h >= 0 ? h + 1 : 1).ToString(CultureInfo.InvariantCulture);
                    var labels = table[h >= 0 ? h : 0]; var boxes = new[] { symbol, quantity, cost, currency }; var kinds = new[] { "symbol", "quantity", "cost", "currency" };
                    updating = true;
                    for (int i = 0; i < boxes.Length; i++)
                    {
                        boxes[i].Items.Clear(); boxes[i].Items.Add(L.T("stock.none"));
                        for (int c = 0; c < table.Max(row => row.Count); c++) boxes[i].Items.Add((c + 1).ToString(CultureInfo.InvariantCulture) + " · " + (c < labels.Count ? labels[c].Substring(0, Math.Min(28, labels[c].Length)) : ""));
                        boxes[i].SelectedIndex = h >= 0 ? HoldingImport.GuessColumn(labels, kinds[i]) + 1 : 0;
                    }
                    updating = false; mappedText = raw.Text;
                }
                int headerIndex;
                if (!Int32.TryParse(headerRow.Text, out headerIndex) || headerIndex < 1 || headerIndex > table.Count) throw new ArgumentException(L.T("stock.mappingRequired"));
                positions = HoldingImport.Map(table, header.IsChecked == true ? headerIndex - 1 : -1, symbol.SelectedIndex - 1, quantity.SelectedIndex - 1, cost.SelectedIndex - 1, currency.SelectedIndex - 1);
                preview.ItemsSource = positions; confirm.IsEnabled = true; error.Text = L.T("stock.previewCount", positions.Count, original.Positions.Count(p => !p.WatchOnly));
            }
            catch (Exception ex) { positions = null; ShowError(ex); }
        }
        async Task ChooseImage(bool fromClipboard)
        {
            if (ocrBusy) return; string image = null, temporary = null;
            try
            {
                if (fromClipboard)
                {
                    if (!Clipboard.ContainsImage()) throw new ArgumentException(L.T("stock.noImage"));
                    var bitmap = Clipboard.GetImage(); temporary = Path.Combine(Path.GetTempPath(), "slaves-ocr-" + Guid.NewGuid().ToString("N") + ".png");
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using (var file = File.Create(temporary)) encoder.Save(file); image = temporary;
                }
                else
                {
                    var dialog = new OpenFileDialog { Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp", CheckFileExists = true };
                    if (dialog.ShowDialog(Window) != true) return; image = dialog.FileName;
                }
                if (new FileInfo(image).Length > 10000000) throw new ArgumentException(L.T("stock.fileLimit"));
                ocrBusy = true; error.Text = L.T("stock.ocrWorking"); confirm.IsEnabled = false;
                string script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StockOcr.exe");
                if (!File.Exists(script)) throw new ArgumentException(L.T("stock.ocrUnavailable"));
                string result = await Task.Run(delegate
                {
                    var start = new ProcessStartInfo { FileName = script,
                        Arguments = "\"" + image + "\"", UseShellExecute = false, CreateNoWindow = true,
                        RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
                    using (var process = Process.Start(start))
                    {
                        Task<string> output = process.StandardOutput.ReadToEndAsync(), failure = process.StandardError.ReadToEndAsync();
                        if (!process.WaitForExit(30000)) { process.Kill(); throw new ArgumentException(L.T("stock.ocrUnavailable")); }
                        Task.WaitAll(output, failure); if (process.ExitCode != 0) throw new ArgumentException(L.T("stock.ocrUnavailable")); return output.Result;
                    }
                });
                var payload = (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(result); raw.Text = (string)payload["text"]; ParsePreview(); note.Text = L.T("stock.ocrReview");
            }
            catch (Exception ex) { ShowError(ex); }
            finally { ocrBusy = false; if (temporary != null && File.Exists(temporary)) File.Delete(temporary); }
        }
        public void Sample(string text) { raw.Text = text; ParsePreview(); }
    }

    public sealed class StockWindow
    {
        sealed class QuoteRow
        { public string Symbol { get; set; } public string Mode { get; set; } public string Quantity { get; set; } public string Price { get; set; } public string Previous { get; set; } public string Change { get; set; } public string Pnl { get; set; } public string Time { get; set; } public string Status { get; set; } }
        public Window Window { get; private set; }
        readonly StockService service;
        readonly Func<Settings> settings;
        readonly Func<bool> privacy;
        readonly TextBlock total = new TextBlock { FontSize = 40, FontWeight = FontWeights.SemiBold }, status = new TextBlock { TextWrapping = TextWrapping.Wrap }, detail = new TextBlock { TextWrapping = TextWrapping.Wrap }, motivation = new TextBlock { TextWrapping = TextWrapping.Wrap };
        readonly DataGrid grid = StockUi.Grid();
        readonly Button refresh, stop;
        readonly TextBlock totalLabel = StockUi.Label("stock.dayPnl", 12);
        Settings source;
        int version = -1;
        bool hidden;
        string language;
        public StockWindow(Window owner, StockService stock, Func<Settings> current, Func<bool> isPrivate)
        {
            service = stock; settings = current; privacy = isPrivate; Window = StockUi.Create(owner, "stock.title", 1000, 780);
            var root = new Grid { Margin = new Thickness(24) }; foreach (var h in new[] { GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto }) root.RowDefinitions.Add(new RowDefinition { Height = h });
            Add(root, StockUi.Label("stock.title", 25), 0); Add(root, StockUi.Label("stock.reviewBanner", 11), 1);
            var card = new StackPanel { Margin = new Thickness(0, 5, 0, 14) }; card.Children.Add(totalLabel); card.Children.Add(total); card.Children.Add(detail); card.Children.Add(motivation); Add(root, card, 2);
            var actions = new WrapPanel(); actions.Children.Add(StockUi.Button("watch.add", delegate { Manual(null); }));
            actions.Children.Add(StockUi.Button("watch.edit", delegate { var selected = grid.SelectedItem as QuoteRow; if (selected == null) { status.Text = L.T("watch.select"); return; } Manual(selected.Symbol); }));
            actions.Children.Add(StockUi.Button("watch.remove", delegate
            {
                if (service.Busy) { status.Text = L.T("stock.busy"); return; }
                var selected = grid.SelectedItem as QuoteRow; if (selected == null) { status.Text = L.T("watch.select"); return; }
                try { var draft = service.State.Copy(); draft.Positions.RemoveAll(p => p.Symbol == selected.Symbol); if (draft.Positions.Count == 0) draft.Online = false; service.Save(draft); }
                catch (Exception ex) { SaveError(ex); }
            }));
            actions.Children.Add(StockUi.Button("stock.import", delegate
            {
                if (service.Busy) return;
                var editor = new StockImportWindow(Window, service.State);
                if (editor.Window.ShowDialog() != true) return;
                try { service.Save(editor.Result); Refresh(); }
                catch (Exception ex) { if (!(ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)) throw; status.Text = L.T("stock.saveFailed"); }
            }));
            refresh = StockUi.Button("stock.refresh", delegate { Refresh(); }); actions.Children.Add(refresh);
            stop = StockUi.Button("stock.stop", delegate
            {
                try { service.StopOnline(); }
                catch (Exception ex) { if (!(ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)) throw; status.Text = L.T("stock.saveFailed"); }
            }); actions.Children.Add(stop); Add(root, actions, 3); Add(root, status, 4);
            actions.Children.Add(StockUi.Button("stock.clear", delegate
            {
                if (service.Busy) { status.Text = L.T("stock.busy"); return; }
                if (MessageBox.Show(Window, L.T("stock.clearConfirm"), L.T("stock.title"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                try { service.Save(new PortfolioState()); }
                catch (Exception ex) { if (!(ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)) throw; status.Text = L.T("stock.saveFailed"); }
            }));
            grid.SelectionMode = DataGridSelectionMode.Single; grid.SelectionUnit = DataGridSelectionUnit.FullRow;
            StockUi.Column(grid, "stock.symbol", "Symbol", 0.8); StockUi.Column(grid, "watch.mode", "Mode", 0.6); StockUi.Column(grid, "stock.quantity", "Quantity", 0.7); StockUi.Column(grid, "stock.price", "Price", 0.9); StockUi.Column(grid, "stock.previous", "Previous", 0.9); StockUi.Column(grid, "watch.change", "Change", 0.8); StockUi.Column(grid, "stock.dayPnlShort", "Pnl", 1); StockUi.Column(grid, "stock.quoteTime", "Time", 1.3); StockUi.Column(grid, "stock.status", "Status", 1.2); Add(root, grid, 5);
            Add(root, StockUi.Label("stock.footer", 11), 6); Window.Content = root; Update();
        }
        static void Add(Grid grid, UIElement element, int row) { Grid.SetRow(element, row); grid.Children.Add(element); }
        void SaveError(Exception ex)
        { if (!(ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)) throw ex; status.Text = ex is ArgumentException ? ex.Message : L.T("stock.saveFailed"); }
        void Manual(string editing)
        {
            if (service.Busy) { status.Text = L.T("stock.busy"); return; }
            var editor = new StockManualWindow(Window, service.State, editing);
            if (editor.Window.ShowDialog() != true) return;
            try { service.Save(editor.Result); Refresh(); } catch (Exception ex) { SaveError(ex); }
        }
        async void Refresh() { await service.RefreshAsync(); Update(true); }
        public void Update(bool force = false)
        {
            Settings current = settings(); bool mask = privacy();
            if (!force && version == service.Version && hidden == mask && Object.ReferenceEquals(source, current) && language == L.Code) return;
            version = service.Version; hidden = mask; source = current;
            if (language != L.Code)
            {
                language = L.Code;
                // 重建窗口由控制器处理；保留快照与行情 / The controller reopens localized windows while preserving state.
            }
            Palette.Apply(Window.Resources, current.Theme);
            var summary = StockMath.Summary(service.State.Positions, service.Quotes, DateTimeOffset.UtcNow);
            bool watchMode = summary.Total == 0 && summary.WatchTotal > 0;
            totalLabel.Text = L.T(watchMode ? "watch.monitor" : "stock.dayPnl");
            total.Text = mask ? "••••••" : watchMode ? L.T("main.watchCount", summary.WatchTotal) : summary.DayPnl.HasValue ? StockUi.Dollars(summary.DayPnl.Value) : "—";
            total.SetResourceReference(TextBlock.ForegroundProperty, mask || !summary.DayPnl.HasValue || summary.DayPnl == 0m ? "Ink" : summary.DayPnl > 0m ? "StockGain" : "ExpenseRed");
            detail.Text = mask ? L.T("stock.hidden") : L.T("stock.coverage", summary.Covered, summary.Total) + (summary.TradingDate == null ? "" : " · " + summary.TradingDate + " ET") + (summary.Simulated ? " · " + L.T("stock.simulated") : "");
            if (!mask && summary.WatchTotal > 0) detail.Text = (watchMode ? "" : detail.Text + "\n") + L.T("main.watchCoverage", summary.WatchCovered, summary.WatchTotal);
            motivation.Text = mask || !summary.DayPnl.HasValue ? "" : L.T(summary.DayPnl > 0m ? "stock.upMessage" : summary.DayPnl < 0m ? "stock.downMessage" : "stock.flatMessage");
            DateTimeOffset confirmed;
            if (DateTimeOffset.TryParse(service.State.ConfirmedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out confirmed)) detail.Text += "\n" + L.T("stock.snapshotAt", confirmed.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            status.Text = L.T(service.StatusKey); refresh.IsEnabled = !service.Busy && service.State.Online && service.State.Positions.Count > 0; stop.IsEnabled = service.State.Online;
            string selectedSymbol = (grid.SelectedItem as QuoteRow) == null ? null : ((QuoteRow)grid.SelectedItem).Symbol;
            var rows = service.State.Positions.Select(p =>
            {
                StockQuote quote; service.Quotes.TryGetValue(p.Symbol, out quote); bool usable = StockMath.Usable(quote, DateTimeOffset.UtcNow);
                return new QuoteRow { Symbol = p.Symbol, Mode = L.T(p.WatchOnly ? "watch.watching" : "watch.holding"), Quantity = mask ? "••••" : p.WatchOnly ? "—" : p.Quantity.ToString(CultureInfo.InvariantCulture), Price = mask ? "••••" : quote == null || quote.Price <= 0m ? "—" : "$" + quote.Price.ToString("N2", CultureInfo.InvariantCulture),
                    Previous = mask ? "••••" : quote == null || quote.PreviousClose <= 0m ? "—" : "$" + quote.PreviousClose.ToString("N2", CultureInfo.InvariantCulture), Change = mask ? "••••" : !usable ? "—" : (((double)quote.Price / (double)quote.PreviousClose - 1) * 100).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + "%", Pnl = mask ? "••••" : p.WatchOnly || !usable ? "—" : StockUi.Dollars(p.Quantity * (quote.Price - quote.PreviousClose)),
                    Time = quote == null || quote.Price <= 0m ? "—" : StockMath.InNewYork(quote.AsOfUtc).ToString("MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " ET", Status = quote == null ? L.T("stock.unavailable") : quote.Simulated ? L.T("stock.simulated") : quote.ErrorKey != null ? L.T(quote.ErrorKey) : usable ? L.T("stock.quoteEstimated") : StockMath.PriceUsable(quote, DateTimeOffset.UtcNow) ? L.T("watch.noBaseline") : L.T("stock.stale") };
            }).ToList();
            grid.ItemsSource = rows; if (selectedSymbol != null) grid.SelectedItem = rows.FirstOrDefault(p => p.Symbol == selectedSymbol);
        }
    }
}
