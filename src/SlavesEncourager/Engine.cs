using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SlavesEncourager
{
    [DataContract]
    public sealed class RewardGoal
    {
        [DataMember] public string Name;
        [DataMember] public decimal Amount;
        public RewardGoal(string name, decimal amount) { Name = name; Amount = amount; }
    }

    public sealed class GoalProgress
    {
        public RewardGoal Goal;
        public decimal Allocated, RemainingToReach;
        public double Fraction;
    }

    public sealed class ExpenseProgress
    {
        public RewardGoal Item;
        public decimal Covered, Remaining;
        public double RemainingFraction;
    }

    public enum DayOverride { Schedule, Rest, Work }

    public sealed class CalendarOverrides
    {
        public readonly HashSet<DateTime> Rest;
        public readonly HashSet<DateTime> Work;
        public CalendarOverrides(string rest, string work) { Rest = Settings.ParseDates(rest); Work = Settings.ParseDates(work); }
        public void Click(DateTime date, DayOverride mode)
        {
            date = date.Date;
            bool selected = mode == DayOverride.Rest ? Rest.Contains(date) : mode == DayOverride.Work && Work.Contains(date);
            // 单日互斥，再点一次恢复排班 / Overrides are exclusive; clicking the same mode again restores the schedule.
            Rest.Remove(date); Work.Remove(date);
            if (!selected && mode == DayOverride.Rest) Rest.Add(date);
            if (!selected && mode == DayOverride.Work) Work.Add(date);
        }
        public static string Serialize(IEnumerable<DateTime> dates)
        { return String.Join(",", dates.OrderBy(d => d).Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))); }
    }

    [DataContract]
    public sealed class Settings
    {
        [DataMember] public decimal MonthlySalary = 18000m;
        [DataMember] public string Start = "09:00";
        [DataMember] public string End = "18:00";
        [DataMember] public string LunchStart = "12:00";
        [DataMember] public string LunchEnd = "13:30";
        [DataMember] public bool LunchEnabled = true;
        [DataMember] public string Weekdays = "1,2,3,4,5";
        [DataMember] public string RestDates = "";
        [DataMember] public string WorkDates = "";
        [DataMember] public string Basis = "actual";
        [DataMember] public int Decimals = 3;
        [DataMember] public bool Grouping = true;
        [DataMember] public string Currency = "CNY";
        [DataMember] public string Pattern = "{symbol}{amount}";
        [DataMember] public string Animation = "flip";
        [DataMember] public string Font = "DIN";
        [DataMember] public string Theme = "midnight";
        [DataMember] public int Speed = 420;
        [DataMember] public string GoalName = L.T("goal.default");
        [DataMember] public decimal GoalAmount = 3000m;
        [DataMember(EmitDefaultValue = false)] public List<RewardGoal> Goals;
        [DataMember] public bool Topmost;
        [DataMember] public bool Configured;
        [DataMember(EmitDefaultValue = false)] public string Language = "auto";
        [DataMember(EmitDefaultValue = false)] public Dictionary<string, string> HomeText;
        [DataMember] public bool ExpensesEnabled;
        [DataMember(EmitDefaultValue = false)] public List<RewardGoal> Expenses;
        [DataMember(EmitDefaultValue = false)] public int? SettlementDay;
        [DataMember(EmitDefaultValue = false)] public string OpeningAt;
        [DataMember(EmitDefaultValue = false)] public decimal? OpeningIncome;
        public int CycleDay { get { return SettlementDay ?? 1; } }

        public List<RewardGoal> ExpenseItems
        {
            get { return Expenses ?? new List<RewardGoal> { new RewardGoal(L.T("expense.rent"), 3000m), new RewardGoal(L.T("expense.living"), 2000m), new RewardGoal(L.T("expense.essential"), 1000m) }; }
        }

        public List<RewardGoal> GoalItems
        {
            get { return Goals ?? new List<RewardGoal> { new RewardGoal(GoalName, GoalAmount) }; }
        }

        public Settings Copy()
        {
            using (var stream = new MemoryStream())
            {
                Serializer().WriteObject(stream, this);
                stream.Position = 0;
                return (Settings)Serializer().ReadObject(stream);
            }
        }

        static DataContractJsonSerializer Serializer() { return new DataContractJsonSerializer(typeof(Settings)); }
        public static string DefaultPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NiumaFlip", "settings.json"); } }

        public static Settings Load(string path, out string notice)
        {
            notice = "";
            if (!File.Exists(path)) return new Settings();
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var value = (Settings)Serializer().ReadObject(stream);
                    value.Validate();
                    return value;
                }
            }
            catch (Exception ex)
            {
                if (!(ex is IOException || ex is SerializationException || ex is ArgumentException || ex is FormatException || ex is NullReferenceException || ex is UnauthorizedAccessException)) throw;
                notice = L.T("error.load");
                return new Settings();
            }
        }

        public void Save(string path)
        {
            Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp";
            using (var stream = File.Create(temporary)) Serializer().WriteObject(stream, this);
            // 原子替换并保留上次设置 / Atomically replace and retain the previous settings.
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }

        public void Validate()
        {
            HomeCopy.Validate(HomeText);
            if (CycleDay < 1 || CycleDay > 31) throw new ArgumentException(L.T("validation.settlement"));
            DateTime opening;
            if ((OpeningIncome.HasValue != !String.IsNullOrEmpty(OpeningAt)) ||
                (OpeningIncome.HasValue && (OpeningIncome < 0m || OpeningIncome > 100000000m || !TryOpening(out opening))))
                throw new ArgumentException(L.T("validation.opening"));
            if (MonthlySalary <= 0 || MonthlySalary > 100000000m) throw new ArgumentException(L.T("validation.salary"));
            TimeSpan start = Engine.Clock(Start), end = Engine.Clock(End);
            if (start == end) throw new ArgumentException(L.T("validation.shift"));
            if (LunchEnabled)
            {
                TimeSpan a = Engine.Clock(LunchStart), b = Engine.Clock(LunchEnd);
                if (a == b) throw new ArgumentException(L.T("validation.breakEqual"));
                var shift = Engine.RawShift(new DateTime(2026, 1, 5), this);
                var lunch = Engine.Lunch(new DateTime(2026, 1, 5), this);
                if (lunch.Start < shift.Start || lunch.End > shift.End || lunch.End - lunch.Start >= shift.End - shift.Start)
                    throw new ArgumentException(L.T("validation.breakBounds"));
            }
            var days = ParseDays();
            if (days.Count == 0) throw new ArgumentException(L.T("validation.weekdays"));
            ParseDates(RestDates); ParseDates(WorkDates);
            if (ParseDates(RestDates).Overlaps(ParseDates(WorkDates))) throw new ArgumentException(L.T("validation.dateConflict"));
            if (Basis != "actual" && Basis != "fixed") throw new ArgumentException(L.T("validation.basis"));
            if (Decimals < 0 || Decimals > 4) throw new ArgumentException(L.T("validation.decimals"));
            if (!new[] { "CNY", "USD", "EUR", "JPY" }.Contains(Currency)) throw new ArgumentException(L.T("validation.currency"));
            if (!new[] { "flip", "slide", "none" }.Contains(Animation)) throw new ArgumentException(L.T("validation.animation"));
            if (!new[] { "DIN", "Mono", "Classic" }.Contains(Font)) throw new ArgumentException(L.T("validation.font"));
            if (!new[] { "midnight", "forest", "paper" }.Contains(Theme)) throw new ArgumentException(L.T("validation.theme"));
            if (Speed < 160 || Speed > 700) throw new ArgumentException(L.T("validation.speed"));
            if (GoalItems.Count > 20) throw new ArgumentException(L.T("validation.goalCount"));
            foreach (RewardGoal goal in GoalItems)
            {
                if (goal == null || String.IsNullOrWhiteSpace(goal.Name) || goal.Name.Length > 28) throw new ArgumentException(L.T("validation.goalName"));
                if (goal.Amount <= 0 || goal.Amount > 100000000m) throw new ArgumentException(L.T("validation.goalAmount"));
            }
            if (ExpenseItems.Count > 20) throw new ArgumentException(L.T("validation.expenseCount"));
            foreach (RewardGoal expense in ExpenseItems)
            {
                if (expense == null || String.IsNullOrWhiteSpace(expense.Name) || expense.Name.Length > 28) throw new ArgumentException(L.T("validation.expenseName"));
                if (expense.Amount <= 0 || expense.Amount > 100000000m) throw new ArgumentException(L.T("validation.expenseAmount"));
            }
            if (String.IsNullOrEmpty(Pattern) || Pattern.Length > 48 || !Pattern.Contains("{amount}") || Pattern.Count(c => c == '{') > 3)
                throw new ArgumentException(L.T("validation.pattern"));
            if (Pattern.IndexOf("{amount}", StringComparison.Ordinal) != Pattern.LastIndexOf("{amount}", StringComparison.Ordinal))
                throw new ArgumentException(L.T("validation.patternOnce"));
            string rest = Pattern.Replace("{amount}", "").Replace("{symbol}", "").Replace("{code}", "");
            if (rest.IndexOfAny(new[] { '{', '}', '\r', '\n', '\t' }) >= 0) throw new ArgumentException(L.T("validation.patternTokens"));
        }

        public HashSet<int> ParseDays()
        {
            var result = new HashSet<int>();
            foreach (string part in (Weekdays ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int day;
                if (!int.TryParse(part.Trim(), out day) || day < 0 || day > 6) throw new ArgumentException(L.T("validation.day"));
                result.Add(day);
            }
            return result;
        }

        public bool TryOpening(out DateTime value)
        { return DateTime.TryParseExact(OpeningAt, "yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture, DateTimeStyles.None, out value); }

        public void ConfirmIncome(DateTime now, decimal amount)
        {
            OpeningAt = now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
            OpeningIncome = amount;
        }

        public static HashSet<DateTime> ParseDates(string value)
        {
            var result = new HashSet<DateTime>();
            foreach (var token in (value ?? "").Split(new[] { ',', '，', ';', '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                DateTime date;
                if (!DateTime.TryParseExact(token.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    throw new ArgumentException(L.T("validation.date"));
                result.Add(date);
            }
            return result;
        }
    }

    public sealed class Interval
    {
        public DateTime Start, End;
        public Interval(DateTime start, DateTime end) { Start = start; End = end; }
        public double Seconds { get { return (End - Start).TotalSeconds; } }
    }

    public sealed class Snapshot
    {
        public decimal Today, Month, Hourly, Daily, Rate;
        public double Worked, Total, Progress;
        public int MonthDays;
        public string Status;
        public string CountdownLabel;
        public string StatusKey, CountdownKey;
        public TimeSpan Countdown;
        public DateTime WorkDate;
        public DateTime PeriodStart, PeriodEnd;
    }

    public static class Engine
    {
        public static List<GoalProgress> Goals(decimal income, Settings settings)
        {
            decimal cycleIncome = Math.Max(0m, income);
            income = RewardIncome(income, settings);
            decimal before = 0m;
            var result = new List<GoalProgress>();
            foreach (RewardGoal goal in settings.GoalItems)
            {
                decimal allocated = Math.Max(0m, Math.Min(goal.Amount, income - before));
                before += goal.Amount;
                result.Add(new GoalProgress { Goal = goal, Allocated = allocated,
                    Fraction = (double)(allocated / goal.Amount), RemainingToReach = Math.Max(0m, before + ExpenseTotal(settings) - cycleIncome) });
            }
            return result;
        }

        // 同一份周期收入先覆盖必要开支，再进入奖励 / Cover expenses before rewards using each cycle's income once.
        public static decimal ExpenseTotal(Settings settings)
        { return settings.ExpensesEnabled ? settings.ExpenseItems.Sum(item => item.Amount) : 0m; }
        public static decimal RewardIncome(decimal income, Settings settings)
        { return Math.Max(0m, income - ExpenseTotal(settings)); }
        public static List<ExpenseProgress> ExpenseCoverage(decimal income, Settings settings)
        {
            var result = new List<ExpenseProgress>();
            if (!settings.ExpensesEnabled) return result;
            decimal available = Math.Max(0m, income);
            foreach (RewardGoal expense in settings.ExpenseItems)
            {
                decimal covered = Math.Min(available, expense.Amount);
                available -= covered;
                decimal remaining = expense.Amount - covered;
                result.Add(new ExpenseProgress { Item = expense, Covered = covered, Remaining = remaining,
                    RemainingFraction = (double)(remaining / expense.Amount) });
            }
            return result;
        }

        public static TimeSpan Clock(string value)
        {
            DateTime time;
            if (!DateTime.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                throw new ArgumentException(L.T("validation.time"));
            return time.TimeOfDay;
        }

        public static bool IsWorkday(DateTime date, Settings settings)
        {
            if (Settings.ParseDates(settings.WorkDates).Contains(date.Date)) return true;
            if (Settings.ParseDates(settings.RestDates).Contains(date.Date)) return false;
            return settings.ParseDays().Contains((int)date.DayOfWeek);
        }

        public static Interval RawShift(DateTime date, Settings settings)
        {
            DateTime start = date.Date + Clock(settings.Start), end = date.Date + Clock(settings.End);
            if (end <= start) end = end.AddDays(1);
            return new Interval(start, end);
        }

        public static Interval Lunch(DateTime date, Settings settings)
        {
            Interval shift = RawShift(date, settings);
            DateTime start = date.Date + Clock(settings.LunchStart);
            if (start < shift.Start) start = start.AddDays(1);
            DateTime end = start.Date + Clock(settings.LunchEnd);
            if (end <= start) end = end.AddDays(1);
            return new Interval(start, end);
        }

        public static List<Interval> Segments(DateTime date, Settings settings)
        {
            Interval shift = RawShift(date, settings);
            if (!settings.LunchEnabled) return new List<Interval> { shift };
            Interval lunch = Lunch(date, settings);
            return new List<Interval> { new Interval(shift.Start, lunch.Start), new Interval(lunch.End, shift.End) };
        }

        static double Elapsed(DateTime now, IEnumerable<Interval> segments)
        {
            return segments.Sum(s => Math.Max(0, (Math.Min(now.Ticks, s.End.Ticks) - s.Start.Ticks) / (double)TimeSpan.TicksPerSecond));
        }

        public static int MonthDays(DateTime date, Settings settings)
        {
            Interval period = PayPeriod(date, settings);
            return Enumerable.Range(0, (period.End - period.Start).Days).Count(i => IsWorkday(period.Start.AddDays(i), settings));
        }

        static DateTime Settlement(DateTime month, int day)
        { return new DateTime(month.Year, month.Month, Math.Min(day, DateTime.DaysInMonth(month.Year, month.Month))); }

        public static Interval PayPeriod(DateTime date, Settings settings)
        {
            DateTime month = new DateTime(date.Year, date.Month, 1);
            if (date.Date < Settlement(month, settings.CycleDay)) month = month.AddMonths(-1);
            return new Interval(Settlement(month, settings.CycleDay), Settlement(month.AddMonths(1), settings.CycleDay));
        }

        // 夜班按开班日归属周期；重置发生在结算日零点 / Night shifts belong to their start date; cycles reset at midnight.
        static decimal PeriodIncome(DateTime now, Settings settings)
        {
            Interval period = PayPeriod(now, settings);
            decimal daily = DayPay(now, settings), income = 0m;
            for (DateTime day = period.Start; day <= now.Date; day = day.AddDays(1))
            {
                if (!IsWorkday(day, settings)) continue;
                var segments = Segments(day, settings);
                income += daily * (decimal)(Elapsed(now, segments) / segments.Sum(s => s.Seconds));
            }
            return income;
        }

        public static decimal DayPay(DateTime date, Settings settings)
        {
            int count = MonthDays(date, settings);
            return settings.MonthlySalary / (settings.Basis == "fixed" ? 21.75m : Math.Max(1, count));
        }

        public static Snapshot Calculate(DateTime now, Settings settings)
        {
            // 跨午夜班次归属开班日 / Overnight shifts belong to their starting date.
            DateTime workDate = now.Date;
            Interval previous = RawShift(now.Date.AddDays(-1), settings);
            if (IsWorkday(now.Date.AddDays(-1), settings) && now < previous.End) workDate = now.Date.AddDays(-1);
            var segments = Segments(workDate, settings);
            Interval shift = RawShift(workDate, settings);
            bool workingDay = IsWorkday(workDate, settings);
            double total = segments.Sum(s => s.Seconds);
            double elapsed = workingDay ? Elapsed(now, segments) : 0;
            decimal daily = DayPay(workDate, settings);
            decimal rate = daily / (decimal)total;
            var snapshot = new Snapshot { WorkDate = workDate, Total = total, Worked = elapsed, Progress = elapsed / total,
                Daily = daily, Rate = rate, Hourly = rate * 3600m, Today = rate * (decimal)elapsed, MonthDays = MonthDays(workDate, settings) };
            snapshot.StatusKey = !workingDay ? "status.rest" : now < shift.Start ? "status.before" : now >= shift.End ? "status.after" :
                segments.Any(s => now >= s.Start && now < s.End) ? "status.earning" : "status.break";
            snapshot.Status = L.T(snapshot.StatusKey);
            if (workingDay && now >= shift.Start && now < shift.End)
            {
                snapshot.CountdownKey = "main.countdownOff"; snapshot.CountdownLabel = L.T(snapshot.CountdownKey);
                snapshot.Countdown = shift.End - now;
            }
            else
            {
                DateTime next = workDate;
                for (int i = 0; i < 370; i++, next = next.AddDays(1))
                {
                    DateTime start = RawShift(next, settings).Start;
                    if (start > now && IsWorkday(next, settings))
                    {
                        snapshot.CountdownKey = "main.countdownOn"; snapshot.CountdownLabel = L.T(snapshot.CountdownKey); snapshot.Countdown = start - now; break;
                    }
                }
            }
            Interval period = PayPeriod(now, settings);
            snapshot.PeriodStart = period.Start; snapshot.PeriodEnd = period.End;
            snapshot.MonthDays = MonthDays(now, settings);
            snapshot.Month = PeriodIncome(now, settings);
            DateTime anchor;
            if (settings.OpeningIncome.HasValue && settings.TryOpening(out anchor) && now >= anchor && anchor >= period.Start && anchor < period.End)
                snapshot.Month = settings.OpeningIncome.Value + Math.Max(0m, snapshot.Month - PeriodIncome(anchor, settings));
            return snapshot;
        }

        public static string Symbol(string currency)
        {
            return currency == "USD" ? "$" : currency == "EUR" ? "€" : "¥";
        }

        public static string Amount(decimal value, Settings settings)
        {
            decimal rounded = Math.Round(value, settings.Decimals, MidpointRounding.AwayFromZero);
            return rounded.ToString((settings.Grouping ? "N" : "F") + settings.Decimals, CultureInfo.InvariantCulture);
        }

        public static string Format(decimal value, Settings settings)
        {
            return settings.Pattern.Replace("{amount}", Amount(value, settings)).Replace("{symbol}", Symbol(settings.Currency)).Replace("{code}", settings.Currency);
        }

        public static string Money(decimal value, Settings settings)
        {
            return Symbol(settings.Currency) + value.ToString("N2", CultureInfo.InvariantCulture);
        }

        public static string Duration(TimeSpan time)
        {
            int seconds = (int)Math.Ceiling(Math.Max(0, time.TotalSeconds));
            return String.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}", seconds / 3600, seconds / 60 % 60, seconds % 60);
        }
    }
}
