using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;

namespace SlavesEncourager
{
    [DataContract]
    public sealed class StockPosition
    {
        [DataMember] public string Symbol { get; set; }
        [DataMember] public decimal Quantity { get; set; }
        [DataMember] public decimal? AverageCost { get; set; }
        [DataMember] public string Currency { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WatchOnly { get; set; }
        public StockPosition() { Currency = "USD"; }
    }

    [DataContract]
    public sealed class PortfolioState
    {
        [DataMember] public List<StockPosition> Positions = new List<StockPosition>();
        [DataMember] public bool Online;
        [DataMember] public string ConfirmedUtc;
        public static string DefaultPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SlavesEncouragerStockBeta", "portfolio.json"); } }
        public PortfolioState Copy()
        { return new PortfolioState { Online = Online, ConfirmedUtc = ConfirmedUtc, Positions = Positions.Select(p => new StockPosition { Symbol = p.Symbol, Quantity = p.Quantity, AverageCost = p.AverageCost, Currency = p.Currency, WatchOnly = p.WatchOnly }).ToList() }; }
        public void Validate()
        {
            if (Positions == null || Positions.Count > 50) throw new ArgumentException(L.T("stock.limit"));
            var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in Positions)
            {
                if (p == null || !StockMath.ValidSymbol(p.Symbol) || (!p.WatchOnly && p.Quantity == 0m) || (p.WatchOnly && (p.Quantity != 0m || p.AverageCost.HasValue)) || Math.Abs(p.Quantity) > 100000000m || p.AverageCost < 0m || p.AverageCost > 100000000m || p.Currency != "USD")
                    throw new ArgumentException(L.T("stock.invalidPosition"));
                if (!symbols.Add(p.Symbol.ToUpperInvariant())) throw new ArgumentException(L.T("stock.duplicate", p.Symbol));
            }
        }
        public void Save(string path)
        {
            Validate(); Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = File.Create(path + ".tmp")) new DataContractJsonSerializer(typeof(PortfolioState)).WriteObject(stream, this);
            if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak"); else File.Move(path + ".tmp", path);
        }
        public static PortfolioState Load(string path)
        {
            if (!File.Exists(path)) return new PortfolioState();
            using (var stream = File.OpenRead(path))
            { var state = (PortfolioState)new DataContractJsonSerializer(typeof(PortfolioState)).ReadObject(stream); state.Validate(); return state; }
        }
    }

    public sealed class StockQuote
    {
        public string Symbol, Provider, TradingDate, PreviousDate;
        public decimal Price, PreviousClose;
        public DateTimeOffset AsOfUtc, FetchedUtc;
        public bool Simulated;
        public string ErrorKey;
    }
    public sealed class PortfolioSummary
    {
        public decimal PartialPnl, PartialValue;
        public decimal? DayPnl;
        public int Covered, Total, WatchTotal, WatchCovered;
        public string TradingDate;
        public bool Simulated;
    }
    public static class StockMath
    {
        public static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        public static DateTimeOffset InNewYork(DateTimeOffset value) { return TimeZoneInfo.ConvertTime(value, NewYork); }
        public static bool ValidSymbol(string symbol)
        { return symbol != null && Regex.IsMatch(symbol, @"\A[A-Z][A-Z0-9]{0,5}(?:[.-][A-Z])?\z") && !new[] { "CASH", "TOTAL", "USD", "BALANCE" }.Contains(symbol); }
        public static string NormalizeSymbol(string symbol)
        {
            string value = (symbol ?? "").Trim().ToUpperInvariant();
            foreach (string prefix in new[] { "NASDAQ:", "NYSE:", "AMEX:" }) if (value.StartsWith(prefix, StringComparison.Ordinal)) value = value.Substring(prefix.Length).Trim();
            return value.Replace('.', '-');
        }
        public static bool PriceUsable(StockQuote quote, DateTimeOffset now)
        {
            if (quote == null || quote.ErrorKey != null || quote.Price <= 0m || quote.Price > 1000000000m || quote.AsOfUtc > now.AddMinutes(10) || quote.FetchedUtc > now.AddMinutes(2) || now - quote.FetchedUtc > TimeSpan.FromMinutes(10) || now - quote.AsOfUtc > TimeSpan.FromDays(7)) return false;
            DateTime date;
            if (!DateTime.TryParseExact(quote.TradingDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) return false;
            if (InNewYork(quote.AsOfUtc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) != quote.TradingDate) return false;
            DateTimeOffset marketNow = InNewYork(now);
            bool weekday = marketNow.DayOfWeek != DayOfWeek.Saturday && marketNow.DayOfWeek != DayOfWeek.Sunday;
            if (weekday && marketNow.TimeOfDay >= new TimeSpan(9, 30, 0) && marketNow.TimeOfDay < new TimeSpan(16, 0, 0) && date.Date < marketNow.Date) return false;
            if (weekday && marketNow.TimeOfDay >= new TimeSpan(9, 30, 0) && marketNow.TimeOfDay < new TimeSpan(16, 0, 0) && now - quote.AsOfUtc > TimeSpan.FromMinutes(20)) return false;
            return true;
        }
        public static bool Usable(StockQuote quote, DateTimeOffset now)
        {
            if (!PriceUsable(quote, now) || quote.PreviousClose <= 0m || quote.PreviousClose > 1000000000m) return false;
            DateTime previous;
            return DateTime.TryParseExact(quote.PreviousDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out previous) && String.CompareOrdinal(quote.PreviousDate, quote.TradingDate) < 0;
        }
        // 只改本地副本；自选不虚构持仓股数 / Edit a local draft; watchlist entries never imply owned shares.
        public static PortfolioState Manual(PortfolioState original, string codes, bool watchOnly, decimal? quantity, decimal? cost, string editing)
        {
            var symbols = Regex.Split((codes ?? "").Trim(), @"[\s,，;；]+").Where(s => s.Length > 0).Select(NormalizeSymbol).ToList();
            if (symbols.Count == 0 || symbols.Any(s => !ValidSymbol(s))) throw new ArgumentException(L.T("watch.codesInvalid"));
            if ((!watchOnly || editing != null) && symbols.Count != 1) throw new ArgumentException(L.T("watch.single"));
            if (!watchOnly && !quantity.HasValue) throw new ArgumentException(L.T("stock.invalidPosition"));
            var draft = original.Copy(); int editedIndex = -1;
            if (editing != null)
            {
                editedIndex = draft.Positions.FindIndex(p => p.Symbol == editing);
                if (editedIndex < 0) throw new ArgumentException(L.T("watch.select"));
                draft.Positions.RemoveAt(editedIndex);
            }
            foreach (string code in symbols)
            {
                var entry = new StockPosition { Symbol = code, Currency = "USD", WatchOnly = watchOnly, Quantity = watchOnly ? 0m : quantity.Value, AverageCost = watchOnly ? null : cost };
                if (editedIndex >= 0) draft.Positions.Insert(editedIndex, entry); else draft.Positions.Add(entry);
            }
            draft.Validate(); return draft;
        }
        public static PortfolioSummary Summary(IEnumerable<StockPosition> positions, IDictionary<string, StockQuote> quotes, DateTimeOffset now)
        {
            var result = new PortfolioSummary(); var dates = new HashSet<string>(); var kinds = new HashSet<bool>();
            foreach (StockPosition p in positions)
            {
                StockQuote q;
                if (p.WatchOnly)
                {
                    result.WatchTotal++;
                    if (quotes.TryGetValue(p.Symbol, out q) && q != null && q.Symbol == p.Symbol && PriceUsable(q, now)) result.WatchCovered++;
                    continue;
                }
                result.Total++;
                if (!quotes.TryGetValue(p.Symbol, out q) || q == null || q.Symbol != p.Symbol || !Usable(q, now)) continue;
                result.Covered++; result.PartialPnl += p.Quantity * (q.Price - q.PreviousClose); result.PartialValue += p.Quantity * q.Price;
                dates.Add(q.TradingDate); kinds.Add(q.Simulated);
            }
            if (result.Total > 0 && result.Covered == result.Total && dates.Count == 1 && kinds.Count == 1)
            { result.DayPnl = result.PartialPnl; result.TradingDate = dates.Single(); result.Simulated = kinds.Single(); }
            return result;
        }
    }

    public static class HoldingImport
    {
        public const int MaxText = 2000000;
        public static List<List<string>> ParseText(string input)
        {
            if (input == null || input.Length > MaxText) throw new ArgumentException(L.T("stock.fileLimit"));
            string value = input.TrimStart('\uFEFF').Trim();
            // 扩展只传已选表格；不解析网页脚本或账户会话 / The extension passes selected tables, never page scripts or account sessions.
            if (value.StartsWith("{", StringComparison.Ordinal))
            {
                var json = new JavaScriptSerializer { MaxJsonLength = MaxText, RecursionLimit = 15 }.DeserializeObject(value) as Dictionary<string, object>;
                if (json == null || !json.ContainsKey("schema") || (string)json["schema"] != "slaves-holdings-table-v1" || !json.ContainsKey("rows")) throw new ArgumentException(L.T("stock.badFormat"));
                var raw = json["rows"] as object[]; if (raw == null || raw.Length > 1000) throw new ArgumentException(L.T("stock.fileLimit"));
                if (raw.Any(r => !(r is object[]))) throw new ArgumentException(L.T("stock.badFormat"));
                return Limit(raw.Select(r => ((object[])r).Select(c => Convert.ToString(c, CultureInfo.InvariantCulture)).ToList()).ToList());
            }
            if (Regex.IsMatch(value, @"<table\b", RegexOptions.IgnoreCase))
            {
                var result = new List<List<string>>();
                foreach (Match row in Regex.Matches(value, @"<tr\b[^>]*>(.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                {
                    var cells = Regex.Matches(row.Groups[1].Value, @"<t[dh]\b[^>]*>(.*?)</t[dh]>", RegexOptions.IgnoreCase | RegexOptions.Singleline).Cast<Match>().Select(c => WebUtility.HtmlDecode(Regex.Replace(c.Groups[1].Value, "<[^>]*>", " ")).Trim()).ToList();
                    if (cells.Count > 0) result.Add(cells);
                }
                return Limit(result);
            }
            string first = value.Split('\n').FirstOrDefault(line => line.Trim().Length > 0) ?? "";
            char separator = value.Contains('\t') ? '\t' : first.Count(c => c == ';') > first.Count(c => c == ',') ? ';' : ',';
            if (!value.Contains(separator))
                return Limit(value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line => Regex.Split(line.Trim(), @"\s{2,}").ToList()).ToList());
            var rows = new List<List<string>>(); var cellsRow = new List<string>(); var cell = new StringBuilder(); bool quoted = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '"')
                {
                    if (quoted && i + 1 < value.Length && value[i + 1] == '"') { cell.Append('"'); i++; }
                    else quoted = !quoted;
                }
                else if (!quoted && (c == separator || c == '\n' || c == '\r'))
                {
                    cellsRow.Add(cell.ToString().Trim()); cell.Clear();
                    if (c != separator) { if (cellsRow.Any(x => x.Length > 0)) rows.Add(cellsRow); cellsRow = new List<string>(); if (c == '\r' && i + 1 < value.Length && value[i + 1] == '\n') i++; }
                }
                else cell.Append(c);
                if (cell.Length > 10000 || rows.Count > 1000 || cellsRow.Count > 200) throw new ArgumentException(L.T("stock.fileLimit"));
            }
            if (quoted) throw new ArgumentException(L.T("stock.badFormat"));
            cellsRow.Add(cell.ToString().Trim()); if (cellsRow.Any(x => x.Length > 0)) rows.Add(cellsRow); return Limit(rows);
        }
        static List<List<string>> Limit(List<List<string>> rows)
        {
            if (rows.Count > 1000 || rows.Any(r => r.Count > 200 || r.Any(c => c.Length > 10000))) throw new ArgumentException(L.T("stock.fileLimit"));
            return rows;
        }
        public static int GuessColumn(List<string> header, string kind)
        {
            var aliases = kind == "symbol" ? new[] { "symbol", "ticker", "stockcode", "代码", "股票代码", "证券代码", "代碼", "股票代碼", "证券代号", "銘柄", "ティッカー", "종목코드", "símbolo" } :
                kind == "quantity" ? new[] { "quantity", "qty", "shares", "position", "数量", "持仓数量", "股数", "數量", "持倉數量", "保有数量", "수량", "cantidad" } :
                kind == "cost" ? new[] { "averagecost", "avgcost", "costpershare", "costbasisperunit", "成本价", "平均成本", "成本價", "平均取得単価", "평균단가", "costemedio" } :
                new[] { "currency", "币种", "幣別", "幣種", "通貨", "통화", "moneda" };
            for (int i = 0; i < header.Count; i++)
            { string normalized = Regex.Replace(header[i].ToLowerInvariant(), @"[\s_/-]", ""); if (aliases.Contains(normalized)) return i; }
            return -1;
        }
        public static bool Number(string input, out decimal value)
        {
            string cleaned = (input ?? "").Trim().Replace("$", "").Replace("USD", "").Trim();
            if (cleaned.StartsWith("(") && cleaned.EndsWith(")")) cleaned = "-" + cleaned.Substring(1, cleaned.Length - 2);
            if (!Regex.IsMatch(cleaned, @"\A[+-]?(?:\d{1,3}(?:,\d{3})+|\d+)(?:\.\d+)?\z")) { value = 0m; return false; }
            return Decimal.TryParse(cleaned, NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
        }
        public static List<StockPosition> Map(List<List<string>> rows, int header, int symbol, int quantity, int cost, int currency)
        {
            if (symbol < 0 || quantity < 0 || symbol == quantity || header < -1 || header >= rows.Count) throw new ArgumentException(L.T("stock.mappingRequired"));
            var result = new List<StockPosition>(); var failures = new List<int>();
            for (int i = header + 1; i < rows.Count; i++)
            {
                var row = rows[i]; if (row.All(String.IsNullOrWhiteSpace)) continue;
                Func<int, string> at = index => index < 0 || index >= row.Count ? "" : row[index].Trim();
                string ticker = StockMath.NormalizeSymbol(at(symbol)), money = currency < 0 ? "USD" : at(currency).ToUpperInvariant();
                decimal qty, average = 0m; bool hasCost = cost >= 0 && at(cost).Length > 0;
                if (!StockMath.ValidSymbol(ticker) || !Number(at(quantity), out qty) || (hasCost && !Number(at(cost), out average)) || money != "USD") { failures.Add(i + 1); continue; }
                result.Add(new StockPosition { Symbol = ticker, Quantity = qty, AverageCost = hasCost ? (decimal?)average : null, Currency = money });
            }
            if (failures.Count > 0) throw new ArgumentException(L.T("stock.badRows", String.Join(", ", failures.Take(12))));
            var draft = new PortfolioState { Positions = result }; draft.Validate(); if (result.Count == 0) throw new ArgumentException(L.T("stock.noPositions")); return result;
        }
        static XDocument Xml(ZipArchive archive, string name)
        {
            var entry = archive.GetEntry(name); if (entry == null || entry.Length > 20000000) throw new ArgumentException(L.T("stock.badFormat"));
            using (var stream = entry.Open()) using (var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 20000000 })) return XDocument.Load(reader);
        }
        public static List<List<string>> ReadXlsx(string path)
        {
            if (new FileInfo(path).Length > 5000000) throw new ArgumentException(L.T("stock.fileLimit"));
            using (var archive = ZipFile.OpenRead(path))
            {
                if (archive.Entries.Count > 1000 || archive.Entries.Sum(e => e.Length) > 50000000) throw new ArgumentException(L.T("stock.fileLimit"));
                XNamespace n = "http://schemas.openxmlformats.org/spreadsheetml/2006/main", r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
                var sheet = Xml(archive, "xl/workbook.xml").Descendants(n + "sheet").First();
                string rid = (string)sheet.Attribute(r + "id");
                var rel = Xml(archive, "xl/_rels/workbook.xml.rels").Root.Elements().Single(e => (string)e.Attribute("Id") == rid);
                string target = ((string)rel.Attribute("Target") ?? "").Replace('\\', '/');
                if ((string)rel.Attribute("TargetMode") == "External" || target.Contains("..")) throw new ArgumentException(L.T("stock.badFormat"));
                target = target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target;
                if (!target.StartsWith("xl/worksheets/", StringComparison.Ordinal)) throw new ArgumentException(L.T("stock.badFormat"));
                var shared = archive.GetEntry("xl/sharedStrings.xml") == null ? new List<string>() : Xml(archive, "xl/sharedStrings.xml").Descendants(n + "si").Select(si => String.Concat(si.Descendants(n + "t").Select(t => t.Value))).ToList();
                var rows = new List<List<string>>();
                foreach (var row in Xml(archive, target).Descendants(n + "row"))
                {
                    if (rows.Count >= 1000) throw new ArgumentException(L.T("stock.fileLimit"));
                    var values = new List<string>();
                    foreach (var cell in row.Elements(n + "c"))
                    {
                        if (cell.Element(n + "f") != null) throw new ArgumentException(L.T("stock.formula"));
                        string reference = (string)cell.Attribute("r") ?? ""; int column = 0;
                        foreach (char ch in reference.TakeWhile(Char.IsLetter)) { column = column * 26 + Char.ToUpperInvariant(ch) - 'A' + 1; if (column > 200) throw new ArgumentException(L.T("stock.fileLimit")); }
                        if (column < 1) throw new ArgumentException(L.T("stock.badFormat"));
                        while (values.Count < column) values.Add("");
                        string value = (string)cell.Element(n + "v") ?? "", type = (string)cell.Attribute("t");
                        if (type == "s") { int index; if (!Int32.TryParse(value, out index) || index < 0 || index >= shared.Count) throw new ArgumentException(L.T("stock.badFormat")); value = shared[index]; }
                        else if (type == "inlineStr") value = String.Concat(cell.Descendants(n + "t").Select(t => t.Value));
                        values[column - 1] = value;
                    }
                    if (values.Any(x => x.Length > 0)) rows.Add(values);
                }
                return Limit(rows);
            }
        }
    }

    public interface IStockQuoteProvider
    { Task<StockQuote> FetchAsync(string symbol, CancellationToken cancellation); }

    public sealed class YahooQuoteProvider : IStockQuoteProvider, IDisposable
    {
        readonly HttpClient client;
        static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public YahooQuoteProvider()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(12), MaxResponseContentBufferSize = 2000000 };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SlavesEncourager-LocalBeta/0.1");
        }
        public async Task<StockQuote> FetchAsync(string symbol, CancellationToken cancellation)
        {
            if (!StockMath.ValidSymbol(symbol)) throw new ArgumentException("symbol");
            string url = "https://query1.finance.yahoo.com/v8/finance/chart/" + Uri.EscapeDataString(symbol) + "?range=5d&interval=1d&includePrePost=false";
            using (var response = await client.GetAsync(url, cancellation))
            {
                response.EnsureSuccessStatusCode();
                await response.Content.LoadIntoBufferAsync(2000000);
                string json = await response.Content.ReadAsStringAsync(); cancellation.ThrowIfCancellationRequested();
                return Parse(json, symbol, DateTimeOffset.UtcNow);
            }
        }
        static Dictionary<string, object> Object(object value) { return (Dictionary<string, object>)value; }
        static decimal Money(object value) { return Convert.ToDecimal(value, CultureInfo.InvariantCulture); }
        public static StockQuote Parse(string json, string symbol, DateTimeOffset fetched)
        {
            var root = Object(new JavaScriptSerializer { MaxJsonLength = 2000000, RecursionLimit = 30 }.DeserializeObject(json));
            var chart = Object(root["chart"]); if (chart["error"] != null) throw new ArgumentException(L.T("stock.quoteInvalid"));
            var result = Object(((object[])chart["result"])[0]); var meta = Object(result["meta"]);
            string actual = Convert.ToString(meta["symbol"], CultureInfo.InvariantCulture), currency = Convert.ToString(meta["currency"], CultureInfo.InvariantCulture), exchange = Convert.ToString(meta["exchangeName"], CultureInfo.InvariantCulture), type = Convert.ToString(meta["instrumentType"], CultureInfo.InvariantCulture);
            if (StockMath.NormalizeSymbol(actual) != symbol || currency != "USD" || !new[] { "EQUITY", "ETF" }.Contains(type) || !new[] { "NMS", "NYQ", "NGM", "NCM", "ASE", "PCX", "BTS", "BATS" }.Contains(exchange)) throw new ArgumentException(L.T("stock.usOnly"));
            var quote = new StockQuote { Symbol = symbol, Provider = "Yahoo Finance · experimental", Price = Money(meta["regularMarketPrice"]), AsOfUtc = Epoch.AddSeconds(Convert.ToDouble(meta["regularMarketTime"], CultureInfo.InvariantCulture)), FetchedUtc = fetched };
            quote.TradingDate = StockMath.InNewYork(quote.AsOfUtc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var timestamps = (object[])result["timestamp"]; var bars = Object(((object[])Object(result["indicators"])["quote"])[0]); var close = (object[])bars["close"];
            // 多日 chartPreviousClose 是区间基准，不能当昨收 / Multi-day chartPreviousClose is a range baseline, never the prior session close.
            var previous = new SortedDictionary<string, decimal>();
            for (int i = 0; i < Math.Min(timestamps.Length, close.Length); i++)
            {
                if (timestamps[i] == null || close[i] == null) continue;
                string day = StockMath.InNewYork(Epoch.AddSeconds(Convert.ToDouble(timestamps[i], CultureInfo.InvariantCulture))).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (String.CompareOrdinal(day, quote.TradingDate) < 0 && Money(close[i]) > 0m) previous[day] = Money(close[i]);
            }
            if (quote.Price <= 0m || quote.AsOfUtc > fetched.AddMinutes(10)) throw new ArgumentException(L.T("stock.quoteInvalid"));
            if (previous.Count == 0) return quote;
            var last = previous.Last(); quote.PreviousDate = last.Key; quote.PreviousClose = last.Value; return quote;
        }
        public void Dispose() { client.Dispose(); }
    }

    public sealed class StockService : IDisposable
    {
        public PortfolioState State { get; private set; }
        public readonly Dictionary<string, StockQuote> Quotes = new Dictionary<string, StockQuote>();
        public int Version { get; private set; }
        public bool Busy { get; private set; }
        public bool FeatureEnabled { get; private set; }
        public string StatusKey = "stock.offline";
        public DateTimeOffset LastAttempt;
        public event Action Changed;
        readonly string path;
        readonly bool sampleOnly;
        readonly IStockQuoteProvider provider;
        readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        readonly CancellationTokenSource cancel = new CancellationTokenSource();
        CancellationTokenSource batch;
        public StockService(string file, bool samples, IStockQuoteProvider quotes, bool enabled = true)
        {
            path = file; sampleOnly = samples; provider = quotes; FeatureEnabled = enabled;
            try { State = samples ? new PortfolioState() : PortfolioState.Load(file); }
            catch (Exception ex)
            {
                if (!(ex is IOException || ex is SerializationException || ex is ArgumentException || ex is UnauthorizedAccessException || ex is NullReferenceException)) throw;
                State = new PortfolioState(); StatusKey = "stock.loadFailed";
            }
            timer.Tick += async delegate { await RefreshAsync(); }; if (!sampleOnly) timer.Start();
        }
        void Notify() { Version++; if (Changed != null) Changed(); }
        // 隐藏功能暂停联网，保留持仓及先前的联网选择 / Hiding suspends requests while preserving holdings and prior quote consent.
        public void SetFeatureEnabled(bool enabled)
        {
            if (FeatureEnabled == enabled) return;
            FeatureEnabled = enabled;
            if (!enabled && batch != null) batch.Cancel();
            StatusKey = enabled && State.Online ? "stock.ready" : "stock.offline"; Notify();
        }
        public void Save(PortfolioState candidate)
        {
            if (Busy) throw new ArgumentException(L.T("stock.busy"));
            candidate.Validate(); if (!sampleOnly) candidate.Save(path);
            State = candidate.Copy(); Quotes.Clear(); LastAttempt = DateTimeOffset.MinValue; StatusKey = candidate.Online ? "stock.ready" : "stock.offline"; Notify();
        }
        public async Task RefreshAsync()
        {
            if (sampleOnly || !FeatureEnabled || Busy || !State.Online || State.Positions.Count == 0) return;
            if (DateTimeOffset.UtcNow - LastAttempt < TimeSpan.FromMinutes(1)) { StatusKey = "stock.cooldown"; Notify(); return; }
            LastAttempt = DateTimeOffset.UtcNow; Busy = true; StatusKey = "stock.fetching"; Notify();
            var limit = new SemaphoreSlim(3); batch = CancellationTokenSource.CreateLinkedTokenSource(cancel.Token);
            try
            {
                var tasks = State.Positions.Select(async position =>
                {
                    await limit.WaitAsync(batch.Token);
                    try { return await provider.FetchAsync(position.Symbol, batch.Token); }
                    catch (Exception ex)
                    {
                        if (!(ex is HttpRequestException || ex is TaskCanceledException || ex is ArgumentException || ex is InvalidOperationException || ex is KeyNotFoundException || ex is InvalidCastException || ex is FormatException || ex is OverflowException || ex is IndexOutOfRangeException || ex is NullReferenceException)) throw;
                        StockQuote old;
                        if (Quotes.TryGetValue(position.Symbol, out old)) return new StockQuote { Symbol = old.Symbol, Provider = old.Provider, Price = old.Price, PreviousClose = old.PreviousClose, PreviousDate = old.PreviousDate, TradingDate = old.TradingDate, AsOfUtc = old.AsOfUtc, FetchedUtc = old.FetchedUtc, ErrorKey = "stock.stale" };
                        return new StockQuote { Symbol = position.Symbol, ErrorKey = "stock.unavailable" };
                    }
                    finally { limit.Release(); }
                }).ToArray();
                var fetched = await Task.WhenAll(tasks);
                if (!State.Online || !FeatureEnabled) return;
                foreach (var quote in fetched) Quotes[quote.Symbol] = quote;
                var summary = StockMath.Summary(State.Positions, Quotes, DateTimeOffset.UtcNow);
                StatusKey = (summary.Total == 0 || summary.DayPnl.HasValue) && summary.WatchCovered == summary.WatchTotal ? "stock.updated" : "stock.partial";
            }
            catch (OperationCanceledException) { StatusKey = "stock.offline"; }
            finally { limit.Dispose(); batch.Dispose(); batch = null; Busy = false; Notify(); }
        }
        public void StopOnline()
        {
            if (batch != null) batch.Cancel();
            var draft = State.Copy(); draft.Online = false;
            State.Online = false; Quotes.Clear(); StatusKey = "stock.offline"; Notify();
            if (!sampleOnly) draft.Save(path);
        }
        public void Demo()
        {
            if (!sampleOnly) throw new InvalidOperationException("Sample-only service required");
            State = new PortfolioState { Positions = new List<StockPosition> { new StockPosition { Symbol = "AAPL", Quantity = 10m, AverageCost = 150m }, new StockPosition { Symbol = "MSFT", Quantity = 5m, AverageCost = 300m } } };
            var now = DateTimeOffset.UtcNow; string day = StockMath.InNewYork(now).ToString("yyyy-MM-dd"), previous = StockMath.InNewYork(now).AddDays(-1).ToString("yyyy-MM-dd");
            Quotes["AAPL"] = new StockQuote { Symbol = "AAPL", Price = 202m, PreviousClose = 200m, TradingDate = day, PreviousDate = previous, AsOfUtc = now, FetchedUtc = now, Provider = "SIMULATED", Simulated = true };
            Quotes["MSFT"] = new StockQuote { Symbol = "MSFT", Price = 398m, PreviousClose = 400m, TradingDate = day, PreviousDate = previous, AsOfUtc = now, FetchedUtc = now, Provider = "SIMULATED", Simulated = true }; StatusKey = "stock.simulated"; Notify();
        }
        public void Dispose() { timer.Stop(); cancel.Cancel(); var disposable = provider as IDisposable; if (disposable != null) disposable.Dispose(); }
    }
}
