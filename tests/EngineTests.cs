using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using SlavesEncourager;

public static class EngineTests
{
    static int checks;
    static void Check(bool condition, string description)
    {
        checks++; if (!condition) throw new Exception("FAIL: " + description);
        Console.WriteLine("PASS " + description);
    }
    static void Near(decimal actual, decimal expected, string description)
    { Check(Math.Abs(actual - expected) < 0.00001m, description); }
    static void Invalid(Action action, string description)
    {
        try { action(); } catch (ArgumentException) { Check(true, description); return; }
        throw new Exception("FAIL: expected rejection: " + description);
    }
    public static int Main()
    {
        try
        {
            var s = new Settings { MonthlySalary = 21750m, Basis = "fixed", Start = "09:00", End = "18:00", LunchStart = "12:00", LunchEnd = "13:00" };
            s.Validate();
            Near(Engine.Calculate(new DateTime(2026, 11, 4, 8, 59, 59), s).Today, 0, "Before shift earns zero");
            Near(Engine.Calculate(new DateTime(2026, 11, 4, 9, 0, 0), s).Today, 0, "Shift start boundary");
            Near(Engine.Calculate(new DateTime(2026, 11, 4, 10, 0, 0), s).Today, 125, "One hour at fixed daily pay");
            Near(Engine.Calculate(new DateTime(2026, 11, 4, 12, 0, 0), s).Today, 375, "Lunch start boundary");
            Near(Engine.Calculate(new DateTime(2026, 11, 4, 12, 59, 59), s).Today, 375, "Lunch does not accrue");
            Check(Engine.Calculate(new DateTime(2026, 11, 4, 12, 30, 0), s).Status == L.T("status.break"), "Lunch status");
            Near(Engine.Calculate(new DateTime(2026, 11, 4, 18, 0, 0), s).Today, 1000, "Full shift daily cap");
            Near(Engine.Calculate(new DateTime(2026, 11, 4, 23, 59, 59), s).Today, 1000, "After shift no extra pay");
            Near(Engine.Calculate(new DateTime(2026, 11, 7, 11, 0, 0), s).Today, 0, "Weekend zero");
            Near(Engine.Calculate(new DateTime(2026, 11, 4, 15, 0, 0), s).Today, 625, "Time-derived recovery after sleep");
            Check(Engine.Calculate(new DateTime(2026, 11, 4, 9, 0, 0), s).Countdown.TotalHours == 9, "Countdown includes lunch");
            Check(Engine.Calculate(new DateTime(2026, 11, 4, 18, 0, 0), s).Countdown.TotalHours == 15, "After shift countdown to next day");
            var month = s.Copy(); month.Basis = "actual";
            Near(Engine.Calculate(new DateTime(2026, 11, 30, 23, 59, 59), month).Month, 21750m, "Actual days month reconciles salary");
            Near(Engine.Calculate(new DateTime(2026, 12, 1, 8, 0, 0), month).Month, 0, "Month rollover clears total");
            month.RestDates = "2026-11-04"; month.WorkDates = "2026-11-07"; month.Validate();
            Check(!Engine.IsWorkday(new DateTime(2026, 11, 4), month), "Manual holiday override");
            Check(Engine.IsWorkday(new DateTime(2026, 11, 7), month), "Weekend makeup override");
            Near(Engine.Calculate(new DateTime(2026, 11, 30, 23, 59, 59), month).Month, 21750m, "Overrides preserve month reconciliation");
            month.WorkDates = "2026-11-04"; Invalid(month.Validate, "Conflicting date overrides rejected");
            var night = s.Copy(); night.Start = "22:00"; night.End = "06:00"; night.LunchStart = "02:00"; night.LunchEnd = "03:00"; night.Validate();
            var n = Engine.Calculate(new DateTime(2026, 11, 7, 1, 0, 0), night);
            Check(n.WorkDate == new DateTime(2026, 11, 6), "Friday night attributed to Friday on Saturday");
            Near(n.Today, 1000m * 3m / 7m, "Overnight earnings");
            Near(Engine.Calculate(new DateTime(2026, 11, 7, 2, 30, 0), night).Today, 1000m * 4m / 7m, "Overnight lunch excluded");
            Check(Engine.Calculate(new DateTime(2026, 11, 7, 6, 0, 0), night).Today == 0, "Completed prior shift ends at exact boundary");
            var noLunch = s.Copy(); noLunch.LunchEnabled = false;
            Near(Engine.Calculate(new DateTime(2026, 11, 4, 12, 30, 0), noLunch).Today, 1000m * 3.5m / 9m, "No-lunch schedule");
            s.Decimals = 2; s.Grouping = true; Near(s.MonthlySalary, 21750, "Copy leaves original unchanged");
            Check(Engine.Format(1234.565m, s) == "¥1,234.57", "Grouped currency and midpoint rounding");
            s.Decimals = 0; Check(Engine.Format(999.9m, s) == "¥1,000", "Digit carry and grouping boundary");
            s.Decimals = 4; s.Grouping = false; s.Pattern = "{amount} 元";
            Check(Engine.Format(1234.56789m, s) == "1234.5679 元", "Custom suffix with four decimal places");
            s.Currency = "USD"; s.Pattern = "{symbol}{amount} {code}";
            Check(Engine.Format(10m, s) == "$10.0000 USD", "Currency template");
            s.Pattern = "{amount}{amount}"; Invalid(s.Validate, "Duplicate amount token rejected");
            s.Pattern = "{unknown}{amount}"; Invalid(s.Validate, "Unknown format token rejected");
            s.Pattern = "{amount}"; s.Decimals = 7; Invalid(s.Validate, "Invalid precision rejected");
            s.Decimals = 2; s.Start = "25:00"; Invalid(s.Validate, "Invalid time rejected");
            s.Start = "09:00"; s.LunchStart = "08:00"; Invalid(s.Validate, "Lunch outside shift rejected");
            s.LunchStart = "09:00"; s.LunchEnd = "18:00"; Invalid(s.Validate, "Zero paid hours rejected");
            s.LunchStart = "12:00"; s.LunchEnd = "13:00"; s.Weekdays = ""; Invalid(s.Validate, "Empty workweek rejected");
            Check(Engine.Duration(TimeSpan.FromSeconds(90061)) == "25:01:01", "Countdown does not wrap at 24 hours");
            Check(Engine.Duration(TimeSpan.FromMilliseconds(100)) == "00:00:01", "Countdown rounds up");
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"), "settings.json");
            var saved = new Settings { Configured = true, GoalName = "周末出发" }; saved.Save(path);
            string notice; var loaded = Settings.Load(path, out notice);
            Check(loaded.GoalName == "周末出发" && loaded.Configured && notice == "", "Chinese settings JSON roundtrip");
            saved.MonthlySalary = 20000; saved.Save(path);
            Check(File.Exists(path + ".bak") && Settings.Load(path, out notice).MonthlySalary == 20000, "Atomic settings backup");
            byte[] bytes = File.ReadAllBytes(path); Check(!(bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191), "UTF-8 without BOM");
            File.WriteAllText(path, "{broken", new UTF8Encoding(false));
            Check(!Settings.Load(path, out notice).Configured && notice.Length > 0 && File.ReadAllText(path) == "{broken", "Corrupt settings preserved, demo fallback");
            var dates = new CalendarOverrides("2026-11-06", "2026-11-07");
            dates.Click(new DateTime(2026, 11, 6), DayOverride.Work);
            Check(!dates.Rest.Contains(new DateTime(2026, 11, 6)) && dates.Work.Contains(new DateTime(2026, 11, 6)), "Calendar work replaces rest exclusively");
            dates.Click(new DateTime(2026, 11, 6), DayOverride.Work);
            Check(!dates.Work.Contains(new DateTime(2026, 11, 6)), "Calendar second click toggles override off");
            dates.Click(new DateTime(2026, 11, 7), DayOverride.Schedule);
            Check(dates.Rest.Count == 0 && dates.Work.Count == 0, "Calendar restore removes both overrides");
            dates.Click(new DateTime(2026, 12, 5), DayOverride.Rest);
            dates.Click(new DateTime(2026, 11, 6), DayOverride.Rest);
            Check(CalendarOverrides.Serialize(dates.Rest) == "2026-11-06,2026-12-05", "Calendar preserves months in sorted serialized dates");
            var adjusted = new Settings { RestDates = CalendarOverrides.Serialize(dates.Rest), WorkDates = "2026-11-07" };
            adjusted.Validate();
            Check(!Engine.IsWorkday(new DateTime(2026, 11, 6), adjusted) && Engine.IsWorkday(new DateTime(2026, 11, 7), adjusted), "Calendar changes feed payroll calculation");
            var legacy = new Settings { GoalName = "旧版旅行目标", GoalAmount = 2500m }; legacy.Save(path);
            var migrated = Settings.Load(path, out notice);
            Check(migrated.Goals == null && migrated.GoalItems.Count == 1 && migrated.GoalItems[0].Name == "旧版旅行目标" && migrated.GoalItems[0].Amount == 2500m, "Version 1.0 settings retain their old goal");
            var multiple = new Settings { Goals = new List<RewardGoal> { new RewardGoal("咖啡", 150m), new RewardGoal("旅行", 3000m), new RewardGoal("耳机", 1600m) } };
            multiple.Validate();
            var allocation = Engine.Goals(3000m, multiple);
            Near(allocation[0].Allocated, 150m, "First goal receives its full amount");
            Near(allocation[1].Allocated, 2850m, "Second goal receives remaining income once");
            Near(allocation[2].Allocated, 0m, "Later goal waits for prior goals");
            Near(allocation.Sum(g => g.Allocated), 3000m, "Multiple goals never double count income");
            Near(allocation[1].RemainingToReach, 150m, "Goal completion uses cumulative threshold");
            Near(Engine.Goals(0m, multiple).Sum(g => g.Allocated), 0m, "New month clears all goal allocations");
            multiple.Save(path); var restored = Settings.Load(path, out notice);
            Check(restored.Goals.Count == 3 && restored.Goals[2].Name == "耳机", "Multiple goals persist in order");
            var cloned = multiple.Copy(); cloned.Goals[0].Name = "已编辑";
            Check(multiple.Goals[0].Name == "咖啡", "Cancel editing cannot mutate original goals");
            var empty = new Settings { Goals = new List<RewardGoal>() }; empty.Validate(); empty.Save(path);
            Check(Settings.Load(path, out notice).GoalItems.Count == 0 && Engine.Goals(3000m, empty).Count == 0, "Empty goal list persists without restoring deleted goal");
            var badGoal = new Settings { Goals = new List<RewardGoal> { new RewardGoal("无效", 0m) } };
            Invalid(badGoal.Validate, "Non-positive multi-goal amounts rejected");
            badGoal.Goals = Enumerable.Range(0, 21).Select(i => new RewardGoal("目标", 10m)).ToList();
            Invalid(badGoal.Validate, "Goal count limit enforced");
            Check(L.Resolve("zh-HK") == "zh-Hant" && L.Resolve("zh-CN") == "zh-Hans", "Chinese region language resolution");
            Check(L.Resolve("es-MX") == "es" && L.Resolve("ja-JP") == "ja" && L.Resolve("ko-KR") == "ko", "Regional language resolution");
            Check(L.Resolve("fr-FR") == "en", "Unsupported languages fall back to English");
            string previousLanguage = L.Code;
            foreach (string language in L.Languages)
            {
                L.Use(language);
                new Settings().Validate(); Check(true, "Localized default settings validate: " + language);
                Check(L.Code == language && L.Keys.Count() >= 180, "Complete catalog: " + language);
                Check(L.Keys.All(key => !String.IsNullOrWhiteSpace(L.T(key))), "No empty translations: " + language);
                Check(L.T("main.hours", 1.5, 8.0).Length > 10 && L.T("goal.index", 2, 3).Contains("2"), "Localized format substitution: " + language);
                var invalidSalary = new Settings { MonthlySalary = 0m };
                try { invalidSalary.Validate(); throw new Exception("Expected salary rejection"); }
                catch (ArgumentException ex) { Check(ex.Message == L.T("validation.salary"), "Localized validation: " + language); }
                var languageSettings = new Settings { Language = language, Goals = new List<RewardGoal> { new RewardGoal("用户自己的目标", 1200m) }, Pattern = "{amount} 自定" };
                languageSettings.Save(path); var languageLoaded = Settings.Load(path, out notice);
                Check(languageLoaded.Language == language && languageLoaded.Goals[0].Name == "用户自己的目标" && languageLoaded.Pattern == "{amount} 自定", "Language persistence preserves user text: " + language);
            }
            var oldLanguage = new Settings { Language = null }; oldLanguage.Save(path);
            Check(Settings.Load(path, out notice).Language == null && notice == "", "Old settings without language remain valid");
            L.Use("zh-Hans");
            var copy = new Settings();
            HomeCopy.Set(copy, "main.headline", "今天也在为自己赚钱。");
            HomeCopy.Set(copy, "mode.privacy", "保护金额");
            HomeCopy.Set(copy, "main.hours", "努力 {0:F2} / {1:F2} 小时");
            copy.Save(path); var copyLoaded = Settings.Load(path, out notice);
            Check(HomeCopy.T(copyLoaded, "main.headline") == "今天也在为自己赚钱。" && HomeCopy.T(copyLoaded, "mode.privacy") == "保护金额", "Home captions survive restart");
            Check(HomeCopy.T(copyLoaded, "main.hours", 2.5, 8.0).Contains("2.50"), "Custom copy keeps live numeric values");
            var copyDraft = copyLoaded.Copy(); HomeCopy.Set(copyDraft, "main.headline", "临时修改");
            Check(HomeCopy.T(copyLoaded, "main.headline") != "临时修改", "Cancel copy editing leaves original intact");
            L.Use("en");
            Check(HomeCopy.Raw(copyLoaded, "main.headline") == L.T("main.headline"), "Custom copy is isolated by language");
            HomeCopy.Set(copyLoaded, "main.headline", "Earn your next adventure.");
            HomeCopy.ResetLanguage(copyLoaded);
            L.Use("zh-Hans");
            Check(HomeCopy.Raw(copyLoaded, "main.headline") == "今天也在为自己赚钱。", "Reset language preserves other languages");
            HomeCopy.Reset(copyLoaded, "main.headline");
            Check(HomeCopy.Raw(copyLoaded, "main.headline") == L.T("main.headline"), "Reset current copy restores translation");
            Invalid(delegate { HomeCopy.Set(copy, "main.hours", "努力 {0:F2}"); }, "Missing copy placeholder rejected");
            Invalid(delegate { HomeCopy.Set(copy, "main.hours", "{0:F2} {1:F2} {2}"); }, "Extra copy placeholder rejected");
            Invalid(delegate { HomeCopy.Set(copy, "main.rate", "{{0}}"); }, "Escaped placeholder cannot disable live value");
            Invalid(delegate { HomeCopy.Set(copy, "main.headline", new string('a', 301)); }, "Overlong copy rejected");
            HomeCopy.Set(copy, "main.subtitle", "");
            Check(HomeCopy.Raw(copy, "main.subtitle") == "", "Optional fixed copy can be hidden");
            Check(new Settings().HomeText == null && HomeCopy.Raw(new Settings(), "main.headline") == L.T("main.headline"), "Legacy settings use default home copy");
            Near(Engine.Calculate(new DateTime(2026, 11, 4, 10, 0, 0), copy).Today, Engine.Calculate(new DateTime(2026, 11, 4, 10, 0, 0), new Settings()).Today, "Copy edits never change earnings");
            // 周期、期初确认和开支共用收入池 / Pay cycles, confirmed opening balances and expenses share one income pool.
            var costs = new Settings { ExpensesEnabled = true, Expenses = new List<RewardGoal> { new RewardGoal("租金", 3000m), new RewardGoal("生活费", 2000m) }, Goals = new List<RewardGoal> { new RewardGoal("旅行", 1000m) } };
            costs.Validate();
            var coverage = Engine.ExpenseCoverage(3500m, costs);
            Near(coverage[0].Remaining, 0m, "Rent is covered before living costs");
            Near(coverage[1].Covered, 500m, "Only remaining income covers the second expense");
            Check(coverage[1].RemainingFraction == 0.75 && Engine.ExpenseCoverage(0m, costs)[0].RemainingFraction == 1, "Red bar tracks the uncovered fraction from full to zero");
            Near(Engine.Goals(3500m, costs)[0].Allocated, 0m, "Rewards wait until expenses are covered");
            Near(Engine.Goals(3500m, costs)[0].RemainingToReach, 2500m, "Goal time estimate includes uncovered expenses");
            Near(Engine.Goals(5500m, costs)[0].Allocated, 500m, "Only surplus income reaches rewards");
            Near(Engine.ExpenseCoverage(5500m, costs).Sum(e => e.Covered) + Engine.Goals(5500m, costs).Sum(g => g.Allocated), 5500m, "Expenses and rewards never double count income");
            Check(Engine.ExpenseCoverage(6000m, costs).All(e => e.RemainingFraction == 0), "Covered red bars reach zero");
            Near(Engine.ExpenseCoverage(-1m, costs).Sum(e => e.Covered), 0m, "Negative income cannot create negative coverage");
            var costsCopy = costs.Copy(); costsCopy.Expenses[0].Name = "draft";
            Check(costs.Expenses[0].Name == "租金", "Cancel editing expenses preserves original names");
            costs.Save(path); var persistedCosts = Settings.Load(path, out notice);
            Check(persistedCosts.ExpensesEnabled && persistedCosts.Expenses[1].Amount == 2000m, "Expenses persist in saved order");
            costs.ExpensesEnabled = false;
            Check(Engine.ExpenseCoverage(3500m, costs).Count == 0 && Engine.ExpenseTotal(costs) == 0m, "Disabled expenses are excluded entirely");
            Near(Engine.Goals(500m, costs)[0].Allocated, 500m, "Disabled expenses do not reduce rewards");
            Check(costs.Expenses.Count == 2 && !new Settings().ExpensesEnabled, "Default off and disabling preserves the saved list");
            costs.ExpensesEnabled = true; costs.Expenses = new List<RewardGoal>(); costs.Save(path);
            Check(Settings.Load(path, out notice).ExpenseItems.Count == 0, "Empty expense list stays empty after reload");
            costs.Expenses.Add(new RewardGoal("invalid", 0m)); Invalid(costs.Validate, "Zero expense rejected");
            costs.Expenses = Enumerable.Range(0, 21).Select(i => new RewardGoal("cost", 1m)).ToList(); Invalid(costs.Validate, "Expense limit enforced");
            var cycle = new Settings { SettlementDay = 15, MonthlySalary = 21750m, Basis = "actual", LunchEnd = "13:00" };
            var period = Engine.PayPeriod(new DateTime(2026, 11, 14), cycle);
            Check(period.Start == new DateTime(2026, 10, 15) && period.End == new DateTime(2026, 11, 15), "Before settlement belongs to the previous cycle");
            period = Engine.PayPeriod(new DateTime(2026, 11, 15), cycle);
            Check(period.Start == new DateTime(2026, 11, 15) && period.End == new DateTime(2026, 12, 15), "Settlement starts a new cycle at midnight");
            Near(Engine.Calculate(new DateTime(2026, 11, 14, 23, 59, 59), cycle).Month, 21750m, "Actual schedule reconciles a complete noncalendar cycle");
            Near(Engine.Calculate(new DateTime(2026, 11, 15, 0, 0, 0), cycle).Month, 0m, "Settlement resets period income");
            cycle.ExpensesEnabled = true; cycle.Expenses = new List<RewardGoal> { new RewardGoal("Rent", 3000m) };
            Check(Engine.ExpenseCoverage(Engine.Calculate(new DateTime(2026, 11, 15), cycle).Month, cycle)[0].RemainingFraction == 1, "Expenses reset to fully uncovered at settlement");
            Check(Engine.Goals(Engine.Calculate(new DateTime(2026, 11, 15), cycle).Month, cycle)[0].Allocated == 0m, "Goals reset at settlement");
            cycle.SettlementDay = 31;
            period = Engine.PayPeriod(new DateTime(2026, 2, 28), cycle);
            Check(period.Start == new DateTime(2026, 2, 28) && period.End == new DateTime(2026, 3, 31), "Day 31 clamps to February end without drifting next month");
            Check(Engine.PayPeriod(new DateTime(2028, 2, 29), cycle).Start == new DateTime(2028, 2, 29), "Leap-year last day supported");
            Check(Engine.PayPeriod(new DateTime(2026, 1, 1), cycle).Start == new DateTime(2025, 12, 31), "Pay cycle crosses the year correctly");
            cycle.SettlementDay = 0; Invalid(cycle.Validate, "Day zero rejected"); cycle.SettlementDay = 32; Invalid(cycle.Validate, "Day 32 rejected");
            var anchored = new Settings { MonthlySalary = 21750m, Basis = "fixed", LunchEnd = "13:00" };
            var confirmedAt = new DateTime(2026, 11, 4, 10, 0, 0);
            anchored.ConfirmIncome(confirmedAt, 40m); anchored.Validate();
            Near(Engine.Calculate(confirmedAt, anchored).Month, 40m, "Confirmed prior income replaces the attendance estimate");
            Near(Engine.Calculate(confirmedAt.AddHours(1), anchored).Month, 165m, "Future earned income continues from confirmed amount");
            Near(Engine.Calculate(new DateTime(2026, 11, 5, 10, 0, 0), anchored).Month, 1040m, "Reopening later recovers elapsed income without timer state");
            anchored.ConfirmIncome(confirmedAt, 0m);
            Near(Engine.Calculate(confirmedAt, anchored).Month, 0m, "Start from zero excludes prior earnings");
            Near(Engine.Calculate(new DateTime(2026, 12, 1, 10, 0, 0), anchored).Month, 125m, "Opening balance expires at the next cycle");
            anchored.Save(path); var loadedAnchor = Settings.Load(path, out notice);
            Check(loadedAnchor.OpeningIncome == 0m && loadedAnchor.OpeningAt == anchored.OpeningAt, "Zero opening balance survives JSON roundtrip");
            anchored.OpeningIncome = -1m; Invalid(anchored.Validate, "Negative confirmed income rejected");
            anchored.OpeningIncome = 0m; anchored.OpeningAt = "broken"; Invalid(anchored.Validate, "Malformed anchor rejected");
            anchored.OpeningAt = null; Invalid(anchored.Validate, "Unpaired opening balance rejected");
            var oldJson = new Settings(); oldJson.Save(path);
            string oldText = File.ReadAllText(path).Replace(",\"ExpensesEnabled\":false", "").Replace("\"ExpensesEnabled\":false,", "");
            File.WriteAllText(path, oldText, new UTF8Encoding(false)); var oldLoaded = Settings.Load(path, out notice);
            Check(oldLoaded.CycleDay == 1 && !oldLoaded.ExpensesEnabled && oldLoaded.OpeningIncome == null && notice == "", "Old JSON defaults to natural month, expenses off and no anchor");
            var nightCycle = new Settings { SettlementDay = 15, Start = "22:00", End = "06:00", LunchEnabled = false, Basis = "fixed", MonthlySalary = 21750m, Weekdays = "0,1,2,3,4,5,6" };
            Near(Engine.Calculate(new DateTime(2026, 11, 15, 1, 0, 0), nightCycle).Month, 0m, "Prior-cycle overnight shift does not leak into new cycle");
            Near(Engine.Calculate(new DateTime(2026, 11, 15, 23, 0, 0), nightCycle).Month, 125m, "New-cycle overnight shift accrues from its starting date");
            L.Use(previousLanguage);
            Console.WriteLine("ALL " + checks.ToString(CultureInfo.InvariantCulture) + " CHECKS PASSED"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
