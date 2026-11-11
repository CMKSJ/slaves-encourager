using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace SlavesEncourager
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            string previewDirectory = null;
            try
            {
                bool preview = args.Contains("--preview-dir");
                int previewIndex = Array.IndexOf(args, "--preview-dir");
                if (preview && previewIndex + 1 < args.Length) previewDirectory = Path.GetFullPath(args[previewIndex + 1]);
                var application = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
                application.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
                {
                    if (previewDirectory != null)
                    {
                        Directory.CreateDirectory(previewDirectory);
                        File.WriteAllText(Path.Combine(previewDirectory, "render-error.txt"), e.Exception.ToString(), new UTF8Encoding(false));
                        application.Shutdown(1);
                    }
                    else MessageBox.Show(L.T("error.operation", e.Exception.Message), L.T("app.name"));
                    e.Handled = true;
                };
                string path = Settings.DefaultPath;
                int settingFlag = Array.IndexOf(args, "--settings-file");
                if (settingFlag >= 0 && settingFlag + 1 < args.Length) path = Path.GetFullPath(args[settingFlag + 1]);
                int languageFlag = Array.IndexOf(args, "--language");
                string language = languageFlag >= 0 && languageFlag + 1 < args.Length ? args[languageFlag + 1] : null;
                var controller = new MainController(path, preview || args.Contains("--demo"), language);
                application.MainWindow = controller.Window;
                if (preview)
                {
                    int index = Array.IndexOf(args, "--preview-dir");
                    if (index + 1 >= args.Length) throw new ArgumentException("--preview-dir requires a directory");
                    string directory = Path.GetFullPath(args[index + 1]);
                    Directory.CreateDirectory(directory);
                    controller.Window.Show();
                    application.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate
                    {
                        try { controller.Preview(directory); }
                        catch (Exception ex)
                        {
                            File.WriteAllText(Path.Combine(directory, "render-error.txt"), ex.ToString(), new UTF8Encoding(false));
                            application.Shutdown(1);
                        }
                        finally { controller.Window.Close(); }
                    }));
                    return application.Run();
                }
                controller.Window.Show(); return application.Run();
            }
            catch (Exception ex)
            {
                if (previewDirectory != null)
                {
                    Directory.CreateDirectory(previewDirectory);
                    File.WriteAllText(Path.Combine(previewDirectory, "render-error.txt"), ex.ToString(), new UTF8Encoding(false));
                }
                else MessageBox.Show(L.T("error.startup", ex.Message), L.T("app.name"));
                return 1;
            }
        }

        public static Window LoadWindow(string name)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (stream == null) throw new InvalidOperationException("Missing embedded resource " + name);
                var window = (Window)XamlReader.Load(stream);
                L.Apply(window.Resources); window.Icon = LoadIcon();
                return window;
            }
        }

        public static BitmapSource LoadIcon()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AppIcon.png"))
            {
                var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream; image.EndInit(); image.Freeze(); return image;
            }
        }

        public static void Render(Window window, string path)
        {
            window.UpdateLayout();
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * 1.5), (int)Math.Ceiling(window.ActualHeight * 1.5), 144, 144, PixelFormats.Pbgra32);
            bitmap.Render(window);
            var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
            using (Stream file = File.Create(path)) png.Save(file);
        }
    }

    public static class Palette
    {
        public static void Apply(ResourceDictionary resources, string theme)
        {
            string[] names = { "BackgroundBrush", "Surface", "SurfaceAlt", "Stroke", "Ink", "Muted", "Accent", "AccentInk", "TileTop", "TileBottom" };
            string[] colors = theme == "paper" ? new[] { "#F7F1E8", "#FFFCF7", "#F0E5D6", "#DED2C3", "#4B3A31", "#89796B", "#9B674B", "#FFFFFF", "#EEE2D2", "#E7D8C6" } :
                theme == "forest" ? new[] { "#242337", "#302D46", "#3C3755", "#504969", "#F5F0FC", "#B7ACC9", "#D5BDEB", "#302441", "#45405C", "#3A354F" } :
                new[] { "#F1F4FA", "#FFFFFF", "#E7EDF8", "#D6DFEE", "#27364D", "#74839A", "#526FA8", "#FFFFFF", "#DEE7F4", "#D2DFF0" };
            for (int i = 0; i < names.Length; i++) resources[names[i]] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i]));
            resources["ExpenseRed"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme == "forest" ? "#F298AA" : "#BD465D"));
            resources["ExpenseTrack"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme == "forest" ? "#533747" : "#F1DEE4"));
        }
    }

    public sealed class MainController
    {
        public Window Window { get; private set; }
        Settings settings;
        readonly string settingsPath;
        readonly bool previewOnly;
        readonly FlipDisplay display = new FlipDisplay();
        readonly DispatcherTimer timer = new DispatcherTimer();
        Forms.NotifyIcon tray;
        ExpenseWindow expenseWindow;
        bool demo, privacy, mini;
        int selectedGoal = -1;
        readonly Stopwatch demoWatch = Stopwatch.StartNew();
        readonly DateTime demoStart = new DateTime(2026, 11, 4, 14, 32, 18);
        string loadNotice;

        T Find<T>(string name) where T : class { return (T)Window.FindName(name); }
        void Text(string name, string value) { Find<TextBlock>(name).Text = value; Find<TextBlock>(name).Tag = null; }
        string Copy(string key, params object[] values) { return HomeCopy.T(settings, key, values); }
        void Home(string name, string key, params object[] values)
        { Text(name, Copy(key, values)); Find<TextBlock>(name).Tag = key; }
        void Caption(string name, string key)
        { Find<Button>(name).Content = Copy(key); Find<Button>(name).Tag = key; }

        public MainController(string path, bool preview, string language)
        {
            settingsPath = path; previewOnly = preview;
            settings = preview ? new Settings() : Settings.Load(path, out loadNotice);
            if (language != null) settings.Language = language;
            L.Use(settings.Language);
            if (!settings.Configured && settings.Goals == null) settings.GoalName = L.T("goal.default");
            demo = preview || !settings.Configured;
            Window = Program.LoadWindow("Main.xaml");
            HomeCopy.Apply(settings, Window.Resources);
            InstallCopyEditor();
            Find<Image>("BrandIcon").Source = Program.LoadIcon();
            Palette.Apply(Window.Resources, settings.Theme);
            Window.Topmost = settings.Topmost;
            Window.Loaded += delegate { FitHomeWindow(); };
            Find<Grid>("FlipHost").Children.Add(display);
            Window.PreviewMouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.ChangedButton != MouseButton.Left || Mouse.LeftButton != MouseButtonState.Pressed) return;
                DependencyObject origin = e.OriginalSource as DependencyObject;
                string copyKey = CopyKey(origin);
                if (e.ClickCount == 2 && copyKey != null && !InsideButton(origin))
                { e.Handled = true; OpenCopyEditor(copyKey); return; }
                // 所有非按钮区域可拖动，按钮内部文字也排除 / Drag every non-button area; exclude button content as well.
                while (origin != null)
                {
                    if (origin is System.Windows.Controls.Primitives.ButtonBase) return;
                    var content = origin as FrameworkContentElement;
                    origin = content != null ? content.Parent :
                        origin is Visual || origin is System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(origin) : LogicalTreeHelper.GetParent(origin);
                }
                Window.DragMove(); e.Handled = true;
            };
            Find<Button>("PinButton").Click += delegate
            {
                bool next = !Window.Topmost;
                if (!previewOnly)
                {
                    Settings candidate = settings.Copy(); candidate.Topmost = next;
                    if (!Persist(candidate)) return;
                    settings = candidate;
                }
                Window.Topmost = next; Refresh(false);
            };
            Find<Button>("CloseButton").Click += delegate { Window.Close(); };
            Find<Button>("MinimizeButton").Click += delegate { if (tray != null) Window.Hide(); else Window.WindowState = WindowState.Minimized; };
            Find<Button>("MiniButton").Click += delegate { ToggleMini(); };
            Find<Button>("PrivacyButton").Click += delegate { privacy = !privacy; display.Reset(); Refresh(false); };
            Find<Button>("DemoButton").Click += delegate { demo = !demo; display.Reset(); Refresh(false); };
            Find<Button>("SettingsButton").Click += delegate { OpenSettings(); };
            Find<Button>("ExpensesButton").Click += delegate { OpenExpenses(); };
            Find<Button>("PreviousGoalButton").Click += delegate { ChangeGoal(-1); };
            Find<Button>("NextGoalButton").Click += delegate { ChangeGoal(1); };
            Window.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.M && Keyboard.Modifiers == ModifierKeys.Control) { ToggleMini(); e.Handled = true; }
                if (e.Key == Key.P && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                { privacy = !privacy; display.Reset(); Refresh(false); e.Handled = true; }
                if (e.Key == Key.Escape && mini) { ToggleMini(); e.Handled = true; }
            };
            Window.Closed += delegate { timer.Stop(); if (tray != null) tray.Dispose(); };
            if (!previewOnly) CreateTray();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += delegate { Refresh(true); }; timer.Start(); Refresh(false);
        }

        void CreateTray()
        {
            tray = new Forms.NotifyIcon { Text = L.T("app.name"), Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location) ?? System.Drawing.SystemIcons.Application, Visible = true };
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add(L.T("tray.show"), null, delegate { Show(); });
            menu.Items.Add(L.T("tray.mini"), null, delegate { Show(); ToggleMini(); });
            menu.Items.Add(L.T("settings.open"), null, delegate { Show(); OpenSettings(); });
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(L.T("tray.quit"), null, delegate { Window.Close(); });
            tray.ContextMenuStrip = menu; tray.DoubleClick += delegate { Show(); };
        }

        void Show() { Window.Show(); Window.WindowState = WindowState.Normal; Window.Activate(); }

        static DependencyObject Parent(DependencyObject node)
        {
            var content = node as FrameworkContentElement;
            return content != null ? content.Parent : node is Visual || node is System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);
        }
        static bool InsideButton(DependencyObject node)
        {
            while (node != null) { if (node is System.Windows.Controls.Primitives.ButtonBase) return true; node = Parent(node); }
            return false;
        }
        static string CopyKey(DependencyObject node)
        {
            while (node != null)
            {
                var element = node as FrameworkElement;
                string key = element == null ? null : element.Tag as string;
                if (key != null && HomeCopy.Keys.Contains(key)) return key;
                node = Parent(node);
            }
            return null;
        }
        void InstallCopyEditor()
        {
            string selectedKey = null;
            var menu = new ContextMenu();
            var edit = new MenuItem(); edit.SetResourceReference(MenuItem.HeaderProperty, "copy.edit");
            edit.Click += delegate { OpenCopyEditor(selectedKey); }; menu.Items.Add(edit);
            Window.ContextMenu = menu;
            Window.ContextMenuOpening += delegate { selectedKey = CopyKey(Mouse.DirectlyOver as DependencyObject); };
            Window.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control) { e.Handled = true; OpenCopyEditor(null); }
            };
        }
        void OpenCopyEditor(string key)
        {
            var editor = new HomeTextEditor(settings, Window, key);
            if (editor.Window.ShowDialog() != true) return;
            if (!previewOnly && !Persist(editor.Result)) return;
            settings = editor.Result; ApplyLanguage(settings.Language);
        }

        void ToggleMini()
        {
            mini = !mini;
            Find<StackPanel>("IntroPanel").Visibility = mini ? Visibility.Collapsed : Visibility.Visible;
            Find<StackPanel>("DetailPanel").Visibility = mini ? Visibility.Collapsed : Visibility.Visible;
            Find<Grid>("RatePanel").Visibility = mini ? Visibility.Collapsed : Visibility.Visible;
            Window.Width = mini ? (L.Code.StartsWith("zh") ? 470 : 540) : 548;
            FitHomeWindow();
            Window.Top = Math.Max(SystemParameters.WorkArea.Top, Math.Min(Window.Top, SystemParameters.WorkArea.Bottom - Window.Height));
            Window.Left = Math.Max(SystemParameters.WorkArea.Left, Math.Min(Window.Left, SystemParameters.WorkArea.Right - Window.Width));
            Refresh(false); FitHomeWindow();
        }

        void FitHomeWindow()
        {
            // 先按自然高度拉长，屏幕不足时整体缩放正文，无需滚动 / Grow to natural height, then scale the body to fit short screens without scrolling.
            var body = Find<StackPanel>("HomeContent"); body.Width = Window.Width - 58;
            var header = Find<Grid>("DragBar"); var actions = Find<Grid>("HomeActions");
            header.Measure(new Size(Window.Width - 14, Double.PositiveInfinity)); actions.Measure(new Size(Window.Width - 14, Double.PositiveInfinity));
            body.Measure(new Size(body.Width, Double.PositiveInfinity));
            double desired = mini ? 310 : Math.Max(790, Math.Ceiling(body.DesiredSize.Height + header.DesiredSize.Height + actions.DesiredSize.Height + 20));
            Window.Height = Math.Min(desired, SystemParameters.WorkArea.Height - 12);
            if (!Double.IsNaN(Window.Top)) Window.Top = Math.Max(SystemParameters.WorkArea.Top, Math.Min(Window.Top, SystemParameters.WorkArea.Bottom - Window.Height));
        }

        bool Persist(Settings candidate)
        {
            try { candidate.Save(settingsPath); return true; }
            catch (Exception ex)
            {
                if (!(ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)) throw;
                MessageBox.Show(Window, L.T("error.save", ex.Message), L.T("app.name")); return false;
            }
        }

        public void Refresh(bool animate)
        {
            DateTime now = demo ? demoStart.Add(demoWatch.Elapsed) : DateTime.Now;
            Snapshot data = Engine.Calculate(now, settings);
            string money = privacy ? "••••••" : Engine.Format(data.Today, settings);
            display.Set(money, settings, animate);
            Text("DateText", now.ToString("MM.dd / ddd", L.Culture));
            Home("StatusText", demo ? "main.demoBadge" : data.StatusKey);
            if (!String.IsNullOrEmpty(loadNotice)) Text("SubtitleText", loadNotice); else Home("SubtitleText", !settings.Configured ? "main.setupHint" : "main.subtitle");
            if (privacy) Home("RateText", "main.hidden"); else Home("RateText", "main.rate", Engine.Symbol(settings.Currency) + data.Rate.ToString("F4", CultureInfo.InvariantCulture));
            Home("ModeText", "main.live");
            Home("CountdownLabel", data.CountdownKey ?? "main.countdownOn"); Text("CountdownText", Engine.Duration(data.Countdown));
            Text("ScheduleText", settings.Start + " — " + settings.End + (settings.LunchEnabled ? Copy("main.breakSuffix") : ""));
            Home("HoursText", "main.hours", data.Worked / 3600, data.Total / 3600);
            Text("ProgressText", data.Progress.ToString("P1", CultureInfo.InvariantCulture));
            Find<ProgressBar>("WorkProgress").Value = data.Progress * 100;
            Text("HourlyText", privacy ? "••••" : Engine.Money(data.Hourly, settings));
            Text("MonthText", privacy ? "••••" : Engine.Money(data.Month, settings));
            Find<TextBlock>("MonthText").ToolTip = L.T("income.period", data.PeriodStart.ToString("yyyy-MM-dd"), data.PeriodEnd.AddDays(-1).ToString("yyyy-MM-dd"));
            List<GoalProgress> goals = Engine.Goals(data.Month, settings);
            int goalIndex = GoalIndex(goals);
            GoalProgress goal = goals.Count == 0 ? null : goals[goalIndex];
            if (goal == null) Home("GoalHeadingText", "goal.heading"); else Home("GoalHeadingText", "goal.index", goalIndex + 1, goals.Count);
            if (goal == null) Home("GoalNameText", "goal.emptyTitle"); else Text("GoalNameText", goal.Goal.Name);
            Text("GoalAmountText", privacy ? "••••" : goal == null ? "＋" : Engine.Money(goal.Goal.Amount, settings));
            Text("GoalPercentText", privacy || goal == null ? "—" : goal.Fraction.ToString("P0", CultureInfo.InvariantCulture));
            Find<ProgressBar>("GoalProgress").Value = privacy || goal == null ? 0 : goal.Fraction * 100;
            if (privacy) Home("GoalHintText", "goal.hidden");
            else if (goal == null) Home("GoalHintText", "goal.emptyHint");
            else if (goal.Fraction >= 1) Home("GoalHintText", goalIndex == goals.Count - 1 ? "goal.allDone" : "goal.done");
            else Home("GoalHintText", "goal.remaining", goal.RemainingToReach / data.Hourly);
            Find<Button>("PreviousGoalButton").IsEnabled = goals.Count > 1;
            Find<Button>("NextGoalButton").IsEnabled = goals.Count > 1;
            if (settings.Basis == "actual") Home("FootnoteText", "main.actualFootnote", data.MonthDays); else Home("FootnoteText", "main.fixedFootnote");
            Caption("DemoButton", demo ? "main.demoActive" : "main.demo");
            Caption("PrivacyButton", "mode.privacy");
            Find<Button>("PrivacyButton").ToolTip = privacy ? Copy("mode.privacyOffTip") : Copy("mode.privacyOnTip");
            Find<Button>("PrivacyButton").SetResourceReference(Button.BackgroundProperty, privacy ? "Accent" : "BackgroundBrush");
            Find<Button>("PrivacyButton").SetResourceReference(Button.ForegroundProperty, privacy ? "AccentInk" : "Ink");
            Caption("MiniButton", "mode.mini");
            Find<Button>("MiniButton").ToolTip = mini ? Copy("mode.miniOffTip") : Copy("mode.miniOnTip");
            Find<Button>("MiniButton").SetResourceReference(Button.BackgroundProperty, mini ? "Accent" : "BackgroundBrush");
            Find<Button>("MiniButton").SetResourceReference(Button.ForegroundProperty, mini ? "AccentInk" : "Ink");
            Caption("PinButton", Window.Topmost ? "main.pinned" : "main.pin");
            Caption("SettingsButton", settings.Configured ? "settings.open" : "main.firstUse");
            Caption("ExpensesButton", "main.expenses");
            Find<Button>("ExpensesButton").Visibility = settings.ExpensesEnabled ? Visibility.Visible : Visibility.Collapsed;
            if (expenseWindow != null)
            {
                if (!settings.ExpensesEnabled) expenseWindow.Window.Close();
                else expenseWindow.Update(settings, data.Month, privacy, demo, animate);
            }
        }

        int GoalIndex(List<GoalProgress> goals)
        {
            if (goals.Count == 0) return 0;
            if (selectedGoal >= 0) return Math.Min(selectedGoal, goals.Count - 1);
            int next = goals.FindIndex(g => g.Fraction < 1);
            return next < 0 ? goals.Count - 1 : next;
        }

        void ChangeGoal(int direction)
        {
            DateTime now = demo ? demoStart.Add(demoWatch.Elapsed) : DateTime.Now;
            List<GoalProgress> goals = Engine.Goals(Engine.Calculate(now, settings).Month, settings);
            if (goals.Count == 0) return;
            selectedGoal = (GoalIndex(goals) + direction + goals.Count) % goals.Count;
            Refresh(false);
        }

        void OpenExpenses()
        {
            if (!settings.ExpensesEnabled) return;
            if (expenseWindow == null)
            {
                var opened = new ExpenseWindow(Window, delegate { OpenSettings(4); });
                expenseWindow = opened;
                opened.Window.Closed += delegate { if (expenseWindow == opened) expenseWindow = null; };
                Refresh(false); opened.Window.Show();
            }
            expenseWindow.Window.Activate();
        }

        SettingsEditor PrepareSettings(int tab)
        {
            Settings initial = settings.Copy();
            var editor = new SettingsEditor(initial, Window);
            if (tab >= 0) ((TabControl)editor.Window.FindName("SettingsTabs")).SelectedIndex = tab;
            return editor;
        }

        void OpenSettings(int tab = -1)
        {
            var editor = PrepareSettings(tab);
            editor.LanguageChanged += delegate(string choice) { ApplyLanguage(choice); };
            if (editor.Show() != true) { ApplyLanguage(settings.Language); return; }
            Settings candidate = editor.Result;
            candidate.Configured = true;
            if (!Persist(candidate)) { ApplyLanguage(settings.Language); return; }
            settings = candidate;
            demo = false; loadNotice = ""; selectedGoal = -1;
            Window.Topmost = settings.Topmost;
            Palette.Apply(Window.Resources, settings.Theme); ApplyLanguage(settings.Language);
        }

        void ApplyLanguage(string choice)
        {
            L.Use(choice); L.Apply(Window.Resources); HomeCopy.Apply(settings, Window.Resources);
            if (mini) Window.Width = L.Code.StartsWith("zh") ? 470 : 540;
            if (tray != null)
            {
                tray.Text = L.T("app.name");
                string[] keys = { "tray.show", "tray.mini", "settings.open", "", "tray.quit" };
                for (int i = 0; i < keys.Length; i++) if (keys[i].Length > 0) tray.ContextMenuStrip.Items[i].Text = L.T(keys[i]);
            }
            display.Reset(); Refresh(false); FitHomeWindow();
        }

        public void Preview(string directory)
        {
            timer.Stop();
            if (Find<Button>("ExpensesButton").Visibility != Visibility.Collapsed) throw new InvalidOperationException("Expenses must default to hidden");
            VerifySetupConfirmation();
            VerifyHomeFit(directory);
            settings.Goals = new List<RewardGoal> { new RewardGoal(L.T("sample.coffee"), 150m), new RewardGoal(L.T("goal.default"), 3000m), new RewardGoal(L.T("sample.headphones"), 1600m) };
            foreach (var theme in new[] { "midnight", "forest", "paper" })
            {
                settings.Theme = theme; Palette.Apply(Window.Resources, theme); display.Reset(); Refresh(false);
                Program.Render(Window, Path.Combine(directory, "preview-" + theme + ".png"));
            }
            settings.Theme = "midnight"; Palette.Apply(Window.Resources, settings.Theme); display.Reset();
            ToggleMini(); Program.Render(Window, Path.Combine(directory, "preview-mini.png")); ToggleMini();
            privacy = true; Refresh(false); Program.Render(Window, Path.Combine(directory, "preview-privacy.png")); privacy = false;
            var editor = new SettingsEditor(settings, Window);
            editor.Window.Show(); ((TabControl)editor.Window.FindName("SettingsTabs")).SelectedIndex = 0; editor.RefreshPreview(false);
            Program.Render(editor.Window, Path.Combine(directory, "preview-settings.png")); editor.Window.Close();
            var scheduleEditor = new SettingsEditor(settings, Window);
            if (((TabItem)scheduleEditor.Window.FindName("ExpensesTab")).Visibility != Visibility.Collapsed) throw new InvalidOperationException("Expenses tab must default to hidden");
            scheduleEditor.Window.Show(); ((TabControl)scheduleEditor.Window.FindName("SettingsTabs")).SelectedIndex = 1;
            Program.Render(scheduleEditor.Window, Path.Combine(directory, "preview-schedule.png")); scheduleEditor.Window.Close();
            var calendarEditor = new SettingsEditor(settings, Window);
            calendarEditor.Window.Show(); ((TabControl)calendarEditor.Window.FindName("SettingsTabs")).SelectedIndex = 2;
            Program.Render(calendarEditor.Window, Path.Combine(directory, "preview-calendar.png")); calendarEditor.Window.Close();
            var goalEditor = new SettingsEditor(settings, Window);
            goalEditor.Window.Show(); ((TabControl)goalEditor.Window.FindName("SettingsTabs")).SelectedIndex = 3;
            Program.Render(goalEditor.Window, Path.Combine(directory, "preview-goal.png")); goalEditor.Window.Close();
            Settings beforeExpenses = settings.Copy();
            settings.ExpensesEnabled = true; settings.SettlementDay = 15;
            settings.Expenses = settings.ExpenseItems;
            Refresh(false); Program.Render(Window, Path.Combine(directory, "preview-with-expenses.png"));
            var expenseEditor = new SettingsEditor(settings, Window);
            expenseEditor.Window.Show();
            var enabledBox = (CheckBox)expenseEditor.Window.FindName("ExpensesEnabledBox");
            enabledBox.IsChecked = false;
            if (((TabItem)expenseEditor.Window.FindName("ExpensesTab")).Visibility != Visibility.Collapsed) throw new InvalidOperationException("Expenses tab must hide when disabled");
            enabledBox.IsChecked = true;
            if (((TabItem)expenseEditor.Window.FindName("ExpensesTab")).Visibility != Visibility.Visible) throw new InvalidOperationException("Expenses tab must show when enabled");
            ((TabControl)expenseEditor.Window.FindName("SettingsTabs")).SelectedIndex = 4;
            Program.Render(expenseEditor.Window, Path.Combine(directory, "preview-expense-settings.png")); expenseEditor.Window.Close();
            var expensesPreview = new ExpenseWindow(Window, null); expensesPreview.Window.Show();
            expensesPreview.Update(settings, 3750m, false, true, false); Program.Render(expensesPreview.Window, Path.Combine(directory, "preview-expenses.png"));
            expensesPreview.Update(settings, 8000m, false, true, false); Program.Render(expensesPreview.Window, Path.Combine(directory, "preview-expenses-covered.png"));
            expensesPreview.Update(settings, 3750m, true, true, false); Program.Render(expensesPreview.Window, Path.Combine(directory, "preview-expenses-private.png"));
            var emptyExpenses = settings.Copy(); emptyExpenses.Expenses = new List<RewardGoal>();
            expensesPreview.Update(emptyExpenses, 3750m, false, true, false); Program.Render(expensesPreview.Window, Path.Combine(directory, "preview-expenses-empty.png")); expensesPreview.Window.Close();
            var openingPreview = new IncomeConfirmation(settings, Window, demoStart);
            openingPreview.Window.Show(); Program.Render(openingPreview.Window, Path.Combine(directory, "preview-income-confirmation.png")); openingPreview.Window.Close();
            settings = beforeExpenses; Refresh(false);
            var copyEditor = new HomeTextEditor(settings, Window, "main.headline");
            copyEditor.Window.Show(); Program.Render(copyEditor.Window, Path.Combine(directory, "preview-copy-editor.png")); copyEditor.Window.Close();
            Settings originalCopy = settings.Copy();
            HomeCopy.Set(settings, "main.headline", L.Code.StartsWith("zh") ? "每一秒，都在接近梦想。" : "Every second, a little closer.");
            HomeCopy.Apply(settings, Window.Resources); Refresh(false);
            Program.Render(Window, Path.Combine(directory, "preview-custom-copy.png"));
            settings = originalCopy; HomeCopy.Apply(settings, Window.Resources); Refresh(false);
            ToggleMini();
            display.Reset(); display.Set("¥461.599", settings, false);
            var frame = new DispatcherFrame();
            var animationTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(40) };
            int frameIndex = 0; Exception failure = null;
            animationTimer.Tick += delegate
            {
                try
                {
                    if (frameIndex == 2) display.Set("¥461.600", settings, true);
                    Program.Render(Window, Path.Combine(directory, "flip-frame-" + frameIndex.ToString("00", CultureInfo.InvariantCulture) + ".png"));
                    frameIndex++;
                    if (frameIndex >= 18) { animationTimer.Stop(); frame.Continue = false; }
                }
                catch (Exception ex) { failure = ex; animationTimer.Stop(); frame.Continue = false; }
            };
            animationTimer.Start(); Dispatcher.PushFrame(frame);
            if (failure != null) throw failure;
            var report = "XAML + home fit + setup confirmation + all settings PASS\n";
            File.WriteAllText(Path.Combine(directory, "render-result.txt"), report, new UTF8Encoding(false));
        }

        void VerifyHomeFit(string directory)
        {
            Refresh(false); FitHomeWindow(); Window.UpdateLayout(); CheckHomeBounds();
            ToggleMini();
            if (Find<StackPanel>("DetailPanel").Visibility != Visibility.Collapsed) throw new InvalidOperationException("Mini mode must collapse the detail panel");
            ToggleMini();
            // 短屏幕整体缩放正文，不得滚动或裁剪 / Short screens scale the body instead of scrolling or clipping.
            Window.Height = Math.Min(600, SystemParameters.WorkArea.Height - 12); Window.UpdateLayout(); CheckHomeBounds();
            Program.Render(Window, Path.Combine(directory, "preview-home-short-screen.png"));
        }

        void CheckHomeBounds()
        {
            StackPanel body = Find<StackPanel>("HomeContent");
            Rect bounds = body.TransformToAncestor(Window).TransformBounds(new Rect(new Point(), body.RenderSize));
            double actions = Find<Grid>("HomeActions").TransformToAncestor(Window).Transform(new Point()).Y;
            if (bounds.Height <= 0 || bounds.Bottom > actions + 0.5 || bounds.Top < 0) throw new InvalidOperationException("Home body is clipped or overlaps actions");
        }

        void VerifySetupConfirmation()
        {
            // 使用示例副本走真实模态保存流程，不读写用户文件 / Exercise modal save with sample drafts; never read or write user files.
            foreach (bool cancel in new[] { true, false })
            {
                var initial = new Settings();
                var editor = new SettingsEditor(initial, Window);
                ((CheckBox)editor.Window.FindName("ExpensesEnabledBox")).IsChecked = true;
                ((ComboBox)editor.Window.FindName("SettlementBox")).SelectedIndex = 14;
                editor.Window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate
                {
                    editor.Window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate
                    {
                        Window confirmation = Application.Current.Windows.Cast<Window>().Single(w => w.Owner == editor.Window && w.Title == L.T("income.title"));
                        var panel = (StackPanel)confirmation.Content;
                        panel.Children.OfType<TextBox>().Single().Text = "123.45";
                        var buttons = panel.Children.OfType<WrapPanel>().Single().Children.OfType<Button>();
                        buttons.Single(b => (string)b.Content == L.T(cancel ? "action.cancel" : "income.confirm")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        if (cancel) editor.Window.Close();
                    }));
                    ((Button)editor.Window.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }));
                bool? saved = editor.Show();
                if (cancel ? saved == true || editor.Result != null : saved != true || editor.Result.OpeningIncome != 123.45m || editor.Result.CycleDay != 15 || !editor.Result.ExpensesEnabled)
                    throw new InvalidOperationException("Setup confirmation/cancel failed");
                if (initial.OpeningIncome != null || initial.ExpensesEnabled || initial.CycleDay != 1) throw new InvalidOperationException("Setup mutated original settings");
            }
        }
    }

    public sealed class SettingsEditor
    {
        public Window Window { get; private set; }
        public Settings Result { get; private set; }
        readonly Settings original;
        readonly FlipDisplay preview = new FlipDisplay();
        readonly DispatcherTimer timer = new DispatcherTimer();
        readonly ScheduleCalendar calendar;
        readonly GoalListEditor goals, expenses;
        bool ready, confirmIncome;
        int ticks;
        string validationError = "";
        public event Action<string> LanguageChanged;
        T Find<T>(string name) where T : class { return (T)Window.FindName(name); }
        string GetText(string name) { return Find<TextBox>(name).Text.Trim(); }
        void Put(string name, string value) { Find<TextBox>(name).Text = value; }
        string Selected(string name)
        {
            var item = Find<ComboBox>(name).SelectedItem as ComboBoxItem; return item == null ? "" : (string)item.Tag;
        }
        void Select(string name, string tag)
        {
            var combo = Find<ComboBox>(name);
            combo.SelectedItem = combo.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == tag);
        }

        public SettingsEditor(Settings settings, Window owner)
        {
            original = settings.Copy(); Window = Program.LoadWindow("Settings.xaml"); Window.Owner = owner;
            foreach (object key in owner.Resources.Keys)
                if (!Window.Resources.Contains(key)) Window.Resources[key] = owner.Resources[key];
            Window.Height = Math.Min(Window.Height, SystemParameters.WorkArea.Height - 12);
            Find<Grid>("PreviewHost").Children.Add(preview);
            Select("LanguageBox", settings.Language ?? "auto");
            Put("SalaryText", settings.MonthlySalary.ToString(CultureInfo.InvariantCulture));
            var settlement = Find<ComboBox>("SettlementBox");
            for (int day = 1; day <= 31; day++) settlement.Items.Add(new ComboBoxItem { Content = day.ToString(CultureInfo.InvariantCulture), Tag = day.ToString(CultureInfo.InvariantCulture) });
            Select("SettlementBox", settings.CycleDay.ToString(CultureInfo.InvariantCulture));
            Find<ComboBox>("SettlementBox").SelectionChanged += delegate { validationError = ""; };
            Find<Button>("AdjustIncomeButton").Click += delegate { confirmIncome = true; Find<TextBlock>("IncomeAdjustmentHint").Visibility = Visibility.Visible; };
            Find<CheckBox>("ExpensesEnabledBox").IsChecked = settings.ExpensesEnabled;
            Put("StartText", settings.Start); Put("EndText", settings.End);
            Put("LunchStartText", settings.LunchStart); Put("LunchEndText", settings.LunchEnd);
            Find<CheckBox>("LunchBox").IsChecked = settings.LunchEnabled;
            Put("PatternText", settings.Pattern);
            var days = settings.ParseDays();
            for (int i = 0; i < 7; i++) Find<CheckBox>("Day" + i).IsChecked = days.Contains(i);
            calendar = new ScheduleCalendar(settings, d => Find<CheckBox>("Day" + d).IsChecked == true, delegate { validationError = ""; });
            Find<Grid>("CalendarHost").Children.Add(calendar);
            goals = new GoalListEditor(settings.GoalItems, delegate { validationError = ""; });
            Find<StackPanel>("GoalsHost").Children.Add(goals);
            expenses = new GoalListEditor(settings.ExpenseItems, delegate { validationError = ""; }, "expense");
            Find<StackPanel>("ExpensesHost").Children.Add(expenses);
            Find<Button>("AddExpenseButton").Click += delegate
            {
                if (expenses.Count >= 20) { validationError = L.T("validation.expenseCount"); Find<TextBlock>("ErrorText").Text = validationError; return; }
                expenses.Add(new RewardGoal(L.T("expense.new"), 1000m));
            };
            Find<Button>("OpenExpensesButton").Click += delegate { Find<TabControl>("SettingsTabs").SelectedIndex = 4; };
            Find<CheckBox>("ExpensesEnabledBox").Checked += delegate { UpdateExpensesTab(); };
            Find<CheckBox>("ExpensesEnabledBox").Unchecked += delegate { UpdateExpensesTab(); };
            UpdateExpensesTab();
            Find<Button>("AddGoalButton").Click += delegate
            {
                if (goals.Count >= 20) { validationError = L.T("validation.goalCount"); Find<TextBlock>("ErrorText").Text = validationError; return; }
                goals.Add(new RewardGoal(L.T("goal.new"), 1000m));
            };
            Find<Button>("OpenCalendarButton").Click += delegate { Find<TabControl>("SettingsTabs").SelectedIndex = 2; };
            for (int i = 0; i < 7; i++)
            {
                Find<CheckBox>("Day" + i).Checked += delegate { calendar.Refresh(); validationError = ""; };
                Find<CheckBox>("Day" + i).Unchecked += delegate { calendar.Refresh(); validationError = ""; };
            }
            Select("BasisBox", settings.Basis); Select("AnimationBox", settings.Animation); Select("ThemeBox", settings.Theme);
            Select("FontBox", settings.Font); Select("DecimalsBox", settings.Decimals.ToString(CultureInfo.InvariantCulture));
            Select("CurrencyBox", settings.Currency); Select("SpeedBox", settings.Speed.ToString(CultureInfo.InvariantCulture));
            bool preset = Find<ComboBox>("PatternBox").Items.Cast<ComboBoxItem>().Any(i => (string)i.Tag == settings.Pattern);
            Select("PatternBox", preset ? settings.Pattern : "custom");
            Find<CheckBox>("GroupingBox").IsChecked = settings.Grouping;
            foreach (var name in new[] { "AnimationBox", "ThemeBox", "FontBox", "DecimalsBox", "CurrencyBox", "SpeedBox", "PatternBox" })
                Find<ComboBox>(name).SelectionChanged += delegate { validationError = ""; RefreshPreview(false); };
            Find<TextBox>("PatternText").TextChanged += delegate { validationError = ""; RefreshPreview(false); };
            foreach (string name in new[] { "SalaryText", "StartText", "EndText", "LunchStartText", "LunchEndText" })
                Find<TextBox>(name).TextChanged += delegate { validationError = ""; };
            Find<CheckBox>("GroupingBox").Checked += delegate { RefreshPreview(false); };
            Find<CheckBox>("GroupingBox").Unchecked += delegate { RefreshPreview(false); };
            Find<Button>("SaveButton").Click += delegate
            {
                try
                {
                    Settings candidate = Read(true); candidate.Validate();
                    if (confirmIncome || !original.Configured || original.CycleDay != candidate.CycleDay || (!original.ExpensesEnabled && candidate.ExpensesEnabled))
                    {
                        if (original.CycleDay != candidate.CycleDay) { candidate.OpeningAt = null; candidate.OpeningIncome = null; }
                        DateTime now = DateTime.Now;
                        var confirmation = new IncomeConfirmation(candidate, Window, now);
                        if (confirmation.Window.ShowDialog() != true) return;
                        candidate.ConfirmIncome(now, confirmation.Amount);
                    }
                    Result = candidate; Window.DialogResult = true;
                }
                catch (Exception ex)
                {
                    if (!(ex is ArgumentException || ex is FormatException || ex is OverflowException)) throw;
                    validationError = ex.Message; Find<TextBlock>("ErrorText").Text = validationError;
                }
            };
            Find<Button>("CancelButton").Click += delegate { Window.Close(); };
            Find<ComboBox>("LanguageBox").SelectionChanged += delegate
            {
                if (!ready) return;
                string choice = Selected("LanguageBox"); L.Use(choice); L.Apply(Window.Resources);
                calendar.Refresh(); goals.RefreshLanguage(); expenses.RefreshLanguage(); validationError = "";
                if (LanguageChanged != null) LanguageChanged(choice);
                preview.Reset(); RefreshPreview(false);
            };
            Window.Closed += delegate { timer.Stop(); };
            if (!settings.Configured) Find<TabControl>("SettingsTabs").SelectedIndex = 1;
            ready = true; RefreshPreview(false);
            timer.Interval = TimeSpan.FromSeconds(1); timer.Tick += delegate { ticks++; RefreshPreview(true); }; timer.Start();
        }

        void UpdateExpensesTab()
        {
            bool enabled = Find<CheckBox>("ExpensesEnabledBox").IsChecked == true;
            if (!enabled && Find<TabControl>("SettingsTabs").SelectedIndex == 4) Find<TabControl>("SettingsTabs").SelectedIndex = 1;
            Find<TabItem>("ExpensesTab").Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            Find<Button>("OpenExpensesButton").IsEnabled = enabled; validationError = "";
        }

        Settings Read(bool all)
        {
            Settings result = original.Copy();
            result.Language = Selected("LanguageBox");
            result.ExpensesEnabled = Find<CheckBox>("ExpensesEnabledBox").IsChecked == true;
            result.Animation = Selected("AnimationBox"); result.Theme = Selected("ThemeBox"); result.Font = Selected("FontBox");
            result.Decimals = int.Parse(Selected("DecimalsBox"), CultureInfo.InvariantCulture);
            result.Currency = Selected("CurrencyBox"); result.Speed = int.Parse(Selected("SpeedBox"), CultureInfo.InvariantCulture);
            result.Grouping = Find<CheckBox>("GroupingBox").IsChecked == true;
            result.Pattern = Selected("PatternBox") == "custom" ? GetText("PatternText") : Selected("PatternBox");
            if (all)
            {
                decimal salary;
                if (!decimal.TryParse(GetText("SalaryText"), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out salary)) throw new ArgumentException(L.T("validation.salaryNumber"));
                result.MonthlySalary = salary;
                result.SettlementDay = int.Parse(Selected("SettlementBox"), CultureInfo.InvariantCulture);
                if (result.ExpensesEnabled) result.Expenses = expenses.Read();
                result.Goals = goals.Read();
                if (result.Goals.Count > 0) { result.GoalName = result.Goals[0].Name; result.GoalAmount = result.Goals[0].Amount; }
                result.Start = GetText("StartText"); result.End = GetText("EndText");
                result.LunchStart = GetText("LunchStartText"); result.LunchEnd = GetText("LunchEndText");
                result.LunchEnabled = Find<CheckBox>("LunchBox").IsChecked == true;
                result.RestDates = CalendarOverrides.Serialize(calendar.Dates.Rest); result.WorkDates = CalendarOverrides.Serialize(calendar.Dates.Work); result.Basis = Selected("BasisBox");
                result.Weekdays = String.Join(",", Enumerable.Range(0, 7).Where(i => Find<CheckBox>("Day" + i).IsChecked == true));
            }
            return result;
        }

        public bool? Show() { return Window.ShowDialog(); }

        public void RefreshPreview(bool animate)
        {
            if (!ready) return;
            bool custom = Selected("PatternBox") == "custom";
            Find<TextBox>("PatternText").IsEnabled = custom;
            if (!custom && GetText("PatternText") != Selected("PatternBox")) Put("PatternText", Selected("PatternBox"));
            try
            {
                Settings candidate = Read(false); candidate.Validate();
                Palette.Apply(Window.Resources, candidate.Theme);
                preview.Set(Engine.Format(1234.5678m + ticks * 0.037m, candidate), candidate, animate);
                Find<TextBlock>("ErrorText").Text = validationError;
            }
            catch (ArgumentException ex) { Find<TextBlock>("ErrorText").Text = ex.Message; }
        }
    }
}
