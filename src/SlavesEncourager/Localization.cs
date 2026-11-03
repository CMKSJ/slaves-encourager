using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;

namespace SlavesEncourager
{
    public static class L
    {
        public static readonly string[] Languages = { "zh-Hans", "zh-Hant", "en", "ja", "es", "ko" };
        static readonly Dictionary<string, Dictionary<string, string>> maps = new Dictionary<string, Dictionary<string, string>>();
        public static string Code { get; private set; }
        public static CultureInfo Culture { get; private set; }

        static L()
        {
            foreach (string language in Languages) maps.Add(language, new Dictionary<string, string>());
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Locales.tsv"))
            {
                if (stream == null) throw new InvalidOperationException("Missing localization catalog.");
                using (var reader = new StreamReader(stream, new System.Text.UTF8Encoding(false, true)))
                {
                    string line; int number = 0;
                    while ((line = reader.ReadLine()) != null)
                    {
                        number++;
                        if (number == 1 || String.IsNullOrWhiteSpace(line)) continue;
                        string[] columns = line.Split('\t');
                        if (columns.Length != 7) throw new InvalidDataException("Invalid catalog row " + number);
                        for (int i = 0; i < Languages.Length; i++)
                        {
                            if (String.IsNullOrWhiteSpace(columns[i + 1])) throw new InvalidDataException("Empty translation: " + columns[0]);
                            maps[Languages[i]].Add(columns[0], columns[i + 1]);
                        }
                    }
                }
            }
            Use("auto");
        }

        public static string Resolve(string choice)
        {
            string value = String.IsNullOrEmpty(choice) || choice == "auto" ? CultureInfo.CurrentUICulture.Name : choice;
            value = value.ToLowerInvariant();
            if (value.StartsWith("zh")) return value.Contains("hant") || value.Contains("tw") || value.Contains("hk") || value.Contains("mo") ? "zh-Hant" : "zh-Hans";
            foreach (string language in new[] { "ja", "es", "ko", "en" }) if (value.StartsWith(language)) return language;
            return "en";
        }

        public static void Use(string choice)
        {
            Code = Resolve(choice);
            string name = Code == "zh-Hans" ? "zh-CN" : Code == "zh-Hant" ? "zh-TW" : Code == "en" ? "en-US" : Code == "ja" ? "ja-JP" : Code == "es" ? "es-ES" : "ko-KR";
            Culture = CultureInfo.GetCultureInfo(name);
        }

        public static string T(string key, params object[] values)
        {
            string result;
            if (!maps[Code].TryGetValue(key, out result)) throw new KeyNotFoundException("Unknown translation key: " + key);
            return values.Length == 0 ? result : String.Format(Culture, result, values);
        }

        public static void Apply(ResourceDictionary resources)
        { foreach (var pair in maps[Code]) resources[pair.Key] = pair.Value; }

        public static IEnumerable<string> Keys { get { return maps["en"].Keys.OrderBy(k => k); } }
    }

    // 主页文案按语言独立保存；不覆盖全局翻译 / Store home copy per language without changing global translations.
    public static class HomeCopy
    {
        public static IEnumerable<string> Keys
        {
            get { return L.Keys.Where(k => k.StartsWith("main.") || k.StartsWith("mode.") || k.StartsWith("status.") ||
                new[] { "app.name", "settings.open", "goal.heading", "goal.index", "goal.emptyTitle", "goal.emptyHint", "goal.hidden", "goal.allDone", "goal.done", "goal.remaining", "goal.estimateHelp", "goal.previous", "goal.next" }.Contains(k)); }
        }
        public static string Raw(Settings settings, string key)
        {
            string value;
            return settings.HomeText != null && settings.HomeText.TryGetValue(L.Code + "/" + key, out value) ? value : L.T(key);
        }
        public static string T(Settings settings, string key, params object[] values)
        { return values.Length == 0 ? Raw(settings, key) : String.Format(L.Culture, Raw(settings, key), values); }
        public static void Set(Settings settings, string key, string value)
        {
            Check(key, value);
            if (settings.HomeText == null) settings.HomeText = new Dictionary<string, string>();
            if (value == L.T(key)) settings.HomeText.Remove(L.Code + "/" + key);
            else settings.HomeText[L.Code + "/" + key] = value;
        }
        public static void Reset(Settings settings, string key)
        { if (settings.HomeText != null) settings.HomeText.Remove(L.Code + "/" + key); }
        public static void ResetLanguage(Settings settings)
        {
            if (settings.HomeText == null) return;
            foreach (string key in settings.HomeText.Keys.Where(k => k.StartsWith(L.Code + "/", StringComparison.Ordinal)).ToArray()) settings.HomeText.Remove(key);
        }
        static string[] Tokens(string value)
        { return Regex.Matches(value, @"\{\d+(?::[^{}]+)?\}").Cast<Match>().Select(m => m.Value).OrderBy(v => v).ToArray(); }
        static void Check(string key, string value)
        {
            if (!Keys.Contains(key) || value == null || value.Length > 300) throw new ArgumentException(L.T("copy.invalid"));
            if (!Tokens(value).SequenceEqual(Tokens(L.T(key))) || Regex.Replace(value, @"\{\d+(?::[^{}]+)?\}", "").IndexOfAny(new[] { '{', '}' }) >= 0)
                throw new ArgumentException(L.T("copy.tokens"));
        }
        public static void Validate(Dictionary<string, string> values)
        {
            if (values == null) return;
            if (values.Count > Keys.Count() * L.Languages.Length) throw new ArgumentException(L.T("copy.invalid"));
            foreach (var pair in values)
            {
                int split = pair.Key.IndexOf('/');
                if (split <= 0 || !L.Languages.Contains(pair.Key.Substring(0, split))) throw new ArgumentException(L.T("copy.invalid"));
                Check(pair.Key.Substring(split + 1), pair.Value);
            }
        }
        public static void Apply(Settings settings, ResourceDictionary resources)
        { foreach (string key in Keys) resources[key] = Raw(settings, key); }
    }
}
