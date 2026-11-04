using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Globalization;

namespace SlavesEncourager.Mac;

public static class MacChecks
{
    static void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException("FAIL " + message); Console.WriteLine("PASS " + message); }
    public static int Run()
    {
        var original = new PortfolioState(); var draft = new StockDraft(original);
        var edit = StockMath.Manual(draft.State, "AAPL, MSFT", true, null, null, null); draft.Stage(edit); edit.Positions.Clear();
        Check(original.Positions.Count == 0 && draft.State.Positions.Count == 2, "mac stock draft deep copy/cancel isolation");
        Check(!draft.State.Online, "mac symbol entry does not silently opt in to quotes");
        Check(Paths.SettingsPath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) && Paths.SettingsPath.Contains(Path.Combine("Library", "Application Support")), "mac Application Support storage is explicit");
        foreach (var language in L.Languages)
        {
            L.Use(language); var s = new Settings(); s.Validate();
            Check(L.T("stock.welcomeTitle").Length > 0 && HomeCopy.Keys.All(k => HomeCopy.Raw(s, k).Length > 0), "mac translations " + language);
        }
        L.Use("en"); Console.WriteLine("ALL MAC CORE CHECKS PASS"); return 0;
    }
    public static async Task Ui(MainWindow main)
    {
        var index = Array.IndexOf(Program.Args, "--output"); string output = index >= 0 ? Program.Args[index + 1] : ".build/macos-preview";
        Directory.CreateDirectory(output);
        Check(main.MenuBarIcon?.Menu?.Items.Count == 5, "native menu bar has restore/mini/settings/quit");
        main.Hide(); main.RestoreWindow(); Check(main.IsVisible, "hidden window can be restored");
        foreach (var language in L.Languages)
        {
            main.Settings.Language = language; L.Use(language); main.Build(); await Task.Delay(150);
            Capture(main, Path.Combine(output, language + "-main.png"));
            foreach (bool? choice in new bool?[] { null, false, true })
            {
                var prompt = new StockWelcomeWindow(); var response = prompt.ShowDialog<bool?>(main); await Task.Delay(60);
                if (choice == true) Capture(prompt, Path.Combine(output, language + "-stock-welcome.png"));
                (choice == true ? prompt.Yes : choice == false ? prompt.No : prompt.Cancel).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(await response == choice, "actual stock prompt yes/no/cancel " + language + " " + choice);
            }
            foreach (var pair in new[] { (true, 5), (false, 1) })
            {
                var initial = new Settings { Language = language, StocksEnabled = pair.Item1, Configured = true };
                var editor = new SettingsWindow(main, initial, new PortfolioState(), pair.Item2);
                var task = editor.ShowDialog(main); await Task.Delay(150);
                Check(editor.Tabs.SelectedIndex == pair.Item2, "first-use routing " + language + " " + pair.Item1);
                if (pair.Item1)
                {
                    var candidate = StockMath.Manual(editor.Stock.State, "AAPL, MSFT", true, null, null, null); editor.Stock.Stage(candidate);
                    Capture(editor, Path.Combine(output, language + "-settings-stocks.png"));
                }
                editor.Close(); await task;
                Check(editor.Result == null && initial.StocksEnabled == pair.Item1 && initial.OpeningIncome == null, "settings cancel isolation " + language);
            }
            var importer = new StockImport(new PortfolioState()); var importTask = importer.ShowDialog(main); await Task.Delay(80);
            importer.SetRawForCheck("Symbol,Quantity,AverageCost,Currency\nAAPL,10,150,USD\nMSFT,5,300,USD"); importer.Parse();
            Check(importer.PreviewCount == 2, "actual import preview " + language); importer.SetRawForCheck("changed"); Check(importer.PreviewCount == 0, "preview invalidates on edit " + language);
            importer.Close(); await importTask;
            await SaveFlow(main, language);
        }
        var themeEditor = new SettingsWindow(main, main.Settings, main.Stocks.State, 0);
        var themeTask = themeEditor.ShowDialog(main); await Task.Delay(60);
        foreach (string theme in new[] { "forest", "paper", "midnight" })
        {
            themeEditor.PreviewTheme(theme); await Task.Delay(100);
            var title = themeEditor.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == L.T("settings.headline"));
            Check(title.Foreground.Equals(SlavesEncourager.Mac.Ui.Ink), "theme preview keeps text contrast " + theme);
            Capture(themeEditor, Path.Combine(output, "theme-" + theme + "-settings.png"));
        }
        themeEditor.Close(); await themeTask;
        File.WriteAllText(Path.Combine(output, "ui-result.txt"), "Six-language mac UI + stock tab routing + cancel isolation + import preview PASS\n");
        Console.WriteLine("ALL MAC UI CHECKS PASS");
    }
    static async Task SaveFlow(MainWindow main, string language)
    {
        var saved = main.Settings.Copy(); var portfolio = main.Stocks.State.Copy();
        main.Settings = new Settings { Language = language, Configured = false };
        var initial = main.Settings.Copy(); initial.StocksEnabled = true;
        var editor = new SettingsWindow(main, initial, new PortfolioState(), 5);
        editor.Stock.Stage(StockMath.Manual(editor.Stock.State, "AAPL", true, null, null, null));
        var shown = editor.ShowDialog(main); await Task.Delay(50);
        editor.PortfolioSaveReady = () => false; await editor.Save();
        Check(editor.IsVisible && editor.Result == null && editor.Stock.State.Positions.Count == 1, "busy save retains draft " + language);
        editor.PortfolioSaveReady = () => true;
        var cancel = editor.Save(); await Task.Delay(50);
        Check(editor.OwnedWindows.Count == 1, "first-use opening income dialog " + language);
        editor.OwnedWindows[0].Close(); await cancel;
        Check(editor.IsVisible && main.Settings.OpeningIncome == null && !main.Settings.Configured, "opening income cancel leaves settings untouched " + language);
        var confirm = editor.Save(); await Task.Delay(50);
        var income = editor.OwnedWindows.Single();
        income.GetVisualDescendants().OfType<TextBox>().Single().Text = "123.45";
        income.GetVisualDescendants().OfType<Button>().Single(b => (string)b.Content == L.T("income.confirm")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await confirm; await shown;
        Check(main.Settings.Configured && main.Settings.OpeningIncome == 123.45m && main.Settings.StocksEnabled && main.Stocks.State.Positions.Single().WatchOnly, "settings save commits confirmed income and stock draft " + language);
        main.Settings = saved; main.Stocks.Save(portfolio); main.Stocks.Demo(); main.Build();
    }
    static void Capture(Window window, string path)
    {
        window.UpdateLayout(); var size = window.ClientSize;
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height)), new Vector(96, 96));
        bitmap.Render(window); bitmap.Save(path);
    }
}
