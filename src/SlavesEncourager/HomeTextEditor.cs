using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SlavesEncourager
{
    // 在独立副本中编辑，保存成功后才应用 / Edit an isolated copy and apply only after a successful save.
    public sealed class HomeTextEditor
    {
        public Window Window { get; private set; }
        public Settings Result { get; private set; }
        readonly ComboBox choices = new ComboBox { Margin = new Thickness(0, 10, 0, 12), FontSize = 13 };
        readonly TextBox input = new TextBox { TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, MaxLength = 300, FontSize = 16, Padding = new Thickness(10), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        readonly TextBlock original = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 12) };
        readonly TextBlock error = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 10) };
        string current;
        bool updating;

        public HomeTextEditor(Settings settings, Window owner, string selected)
        {
            Result = settings.Copy();
            Window = new Window { Owner = owner, Title = L.T("copy.edit"), Width = 580, Height = 560,
                MinWidth = 500, MinHeight = 440, WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FontFamily = owner.FontFamily, FontSize = 12, Icon = owner.Icon };
            foreach (object key in owner.Resources.Keys) Window.Resources[key] = owner.Resources[key];
            Window.SetResourceReference(Control.BackgroundProperty, "BackgroundBrush");
            Window.SetResourceReference(Control.ForegroundProperty, "Ink");
            Window.Height = Math.Min(Window.Height, SystemParameters.WorkArea.Height - 12);
            var panel = new Grid { Margin = new Thickness(22) };
            foreach (var height in new[] { GridLength.Auto, GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto, GridLength.Auto })
                panel.RowDefinitions.Add(new RowDefinition { Height = height });
            var help = new TextBlock { Text = L.T("copy.help"), TextWrapping = TextWrapping.Wrap };
            Add(panel, help, 0); Add(panel, choices, 1); Add(panel, original, 2); Add(panel, input, 3); Add(panel, error, 4);
            input.SetResourceReference(Control.BackgroundProperty, "Surface"); input.SetResourceReference(Control.ForegroundProperty, "Ink");
            error.SetResourceReference(TextBlock.ForegroundProperty, "Accent");
            var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
            actions.Children.Add(Button("copy.reset", delegate { HomeCopy.Reset(Result, current); LoadCurrent(); }));
            actions.Children.Add(Button("copy.resetAll", delegate { HomeCopy.ResetLanguage(Result); LoadCurrent(); }));
            var cancel = Button("action.cancel", delegate { Window.Close(); }); cancel.IsCancel = true; actions.Children.Add(cancel);
            actions.Children.Add(Button("action.save", delegate { if (Commit()) { Result.Validate(); Window.DialogResult = true; } }));
            Add(panel, actions, 5); Window.Content = panel;
            foreach (string key in HomeCopy.Keys) choices.Items.Add(new ComboBoxItem { Tag = key, Content = L.T(key) });
            choices.SelectionChanged += delegate
            {
                if (updating) return;
                var item = choices.SelectedItem as ComboBoxItem;
                if (item == null) return;
                string next = (string)item.Tag;
                if (current != null && !Commit())
                {
                    updating = true; choices.SelectedItem = choices.Items.Cast<ComboBoxItem>().First(i => (string)i.Tag == current); updating = false; return;
                }
                current = next; LoadCurrent();
            };
            choices.SelectedItem = choices.Items.Cast<ComboBoxItem>().First(i => (string)i.Tag == (HomeCopy.Keys.Contains(selected) ? selected : "main.headline"));
        }
        static void Add(Grid panel, UIElement element, int row) { Grid.SetRow(element, row); panel.Children.Add(element); }
        static Button Button(string key, Action action)
        {
            var button = new Button { Content = L.T(key), Margin = new Thickness(5, 4, 0, 0), Padding = new Thickness(10, 7, 10, 7) };
            button.Click += delegate { action(); }; return button;
        }
        void LoadCurrent()
        {
            input.Text = HomeCopy.Raw(Result, current); original.Text = L.T("copy.original", L.T(current)); error.Text = "";
        }
        bool Commit()
        {
            try { HomeCopy.Set(Result, current, input.Text); error.Text = ""; return true; }
            catch (ArgumentException ex) { error.Text = ex.Message; return false; }
        }
    }
}
