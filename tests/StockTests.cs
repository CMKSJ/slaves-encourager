using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using SlavesEncourager;

public static class StockTests
{
    static int checks;
    static void Check(bool value, string name) { checks++; if (!value) throw new Exception("FAIL " + name); Console.WriteLine("PASS " + name); }
    static void Invalid(Action action, string name)
    { try { action(); } catch (Exception ex) { if (!(ex is ArgumentException || ex is InvalidDataException || ex is System.Xml.XmlException)) throw; Check(true, name); return; } throw new Exception("Expected rejection: " + name); }
    static readonly DateTimeOffset Now = new DateTimeOffset(2026, 11, 3, 18, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
    static long Unix(DateTimeOffset time) { return (long)(time - Epoch).TotalSeconds; }
    static StockQuote Quote(string symbol, decimal price)
    { return new StockQuote { Symbol = symbol, Price = price, PreviousClose = 200m, TradingDate = "2026-11-03", PreviousDate = "2026-11-02", AsOfUtc = Now, FetchedUtc = Now, Provider = "synthetic" }; }
    static string YahooFixture(string currency, string type, string exchange, bool previous)
    {
        var meta = new Dictionary<string, object> { { "symbol", "AAPL" }, { "currency", currency }, { "instrumentType", type }, { "exchangeName", exchange }, { "regularMarketPrice", 202m }, { "regularMarketTime", Unix(Now) }, { "chartPreviousClose", 123m } };
        var result = new Dictionary<string, object> { { "meta", meta }, { "timestamp", previous ? new long[] { Unix(Now.AddDays(-4)), Unix(Now.AddDays(-1)), Unix(Now) } : new long[] { Unix(Now) } },
            { "indicators", new Dictionary<string, object> { { "quote", new object[] { new Dictionary<string, object> { { "close", previous ? new object[] { 190m, 200m, 202m } : new object[] { 202m } } } } } } } };
        return new JavaScriptSerializer().Serialize(new Dictionary<string, object> { { "chart", new Dictionary<string, object> { { "error", null }, { "result", new[] { result } } } } });
    }
    static void Xlsx(string path, string target, bool formula, bool shared)
    {
        if (File.Exists(path)) File.Delete(path);
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            Action<string, string> put = (name, text) => { using (var w = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false))) w.Write(text); };
            put("xl/workbook.xml", "<workbook xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'><sheets><sheet name='Holdings' sheetId='1' r:id='rId1'/></sheets></workbook>");
            put("xl/_rels/workbook.xml.rels", "<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='rId1' Target='" + target + "'/></Relationships>");
            string head = "<row r='1'><c r='A1' t='inlineStr'><is><t>Symbol</t></is></c><c r='B1' t='inlineStr'><is><t>Quantity</t></is></c><c r='C1' t='inlineStr'><is><t>AverageCost</t></is></c><c r='D1' t='inlineStr'><is><t>Currency</t></is></c></row>";
            string stock = shared ? "<c r='A2' t='s'><v>0</v></c>" : "<c r='A2' t='inlineStr'><is><t>AAPL</t></is></c>";
            put("xl/worksheets/sheet1.xml", "<worksheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><sheetData>" + head + "<row r='2'>" + stock + "<c r='B2'><v>10</v></c><c r='C2'>" + (formula ? "<f>1+149</f>" : "") + "<v>150</v></c><c r='D2' t='inlineStr'><is><t>USD</t></is></c></row></sheetData></worksheet>");
            if (shared) put("xl/sharedStrings.xml", "<sst xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><si><t>AAPL</t></si></sst>");
        }
    }
    sealed class FakeProvider : IStockQuoteProvider
    {
        public int Calls; public bool Block;
        public async Task<StockQuote> FetchAsync(string symbol, CancellationToken cancellation)
        {
            Calls++; if (Block) await Task.Delay(30000, cancellation);
            var q = Quote(symbol, 202m); q.AsOfUtc = q.FetchedUtc = DateTimeOffset.UtcNow; q.TradingDate = StockMath.InNewYork(q.AsOfUtc).ToString("yyyy-MM-dd"); q.PreviousDate = StockMath.InNewYork(q.AsOfUtc).AddDays(-1).ToString("yyyy-MM-dd"); return q;
        }
    }
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            L.Use("en");
            if (args.Length == 2 && args[0] == "--ocr-result")
            {
                var payload = (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(File.ReadAllText(args[1], Encoding.UTF8));
                var table = HoldingImport.ParseText((string)payload["text"]);
                int head = table.FindIndex(row => HoldingImport.GuessColumn(row, "symbol") >= 0 && HoldingImport.GuessColumn(row, "quantity") >= 0);
                var verified = HoldingImport.Map(table, head, HoldingImport.GuessColumn(table[head], "symbol"), HoldingImport.GuessColumn(table[head], "quantity"), HoldingImport.GuessColumn(table[head], "cost"), HoldingImport.GuessColumn(table[head], "currency"));
                Check(verified.Count == 2 && verified[0].Symbol == "AAPL" && verified[0].Quantity == 10m && verified[0].AverageCost == 150m && verified[1].Symbol == "MSFT" && verified[1].Quantity == 5m, "Native OCR result feeds full holdings parser with exact synthetic quantities and costs"); return 0;
            }
            if (args.Length == 1 && args[0] == "--public-quote-probe")
            {
                using (var provider = new YahooQuoteProvider())
                {
                    StockQuote probe = provider.FetchAsync("AAPL", CancellationToken.None).GetAwaiter().GetResult();
                    Check(probe.Symbol == "AAPL" && probe.PreviousClose > 0m && String.CompareOrdinal(probe.PreviousDate, probe.TradingDate) < 0, "Public AAPL quote plus preceding trading-day baseline");
                    Console.WriteLine(new JavaScriptSerializer().Serialize(new { symbol = probe.Symbol, quoteDate = probe.TradingDate, previousDate = probe.PreviousDate, provider = probe.Provider })); return 0;
                }
            }
            var csv = HoldingImport.ParseText("Symbol,Quantity,AverageCost,Currency\r\nAAPL,10,150,USD\r\nMSFT,5,,USD");
            var positions = HoldingImport.Map(csv, 0, 0, 1, 2, 3);
            Check(positions.Count == 2 && positions[0].Quantity == 10m && positions[1].AverageCost == null, "CSV supports optional per-share cost");
            var quoted = HoldingImport.ParseText("Symbol,Quantity,AverageCost,Currency\nAAPL,\"1,250\",\"150.25\",USD");
            Check(HoldingImport.Map(quoted, 0, 0, 1, 2, 3)[0].Quantity == 1250m, "RFC quoted grouping does not split share counts");
            Check(HoldingImport.ParseText("Symbol\tQuantity\tCurrency\nAAPL\t10\tUSD")[1].Count == 3, "Tab clipboard tables preserve columns");
            Check(HoldingImport.Map(HoldingImport.ParseText("SYNTHETIC SAMPLE\nSymbol\tQuantity\tAverageCost\tCurrency\nAAPL\t10\t150\tUSD"), 1, 0, 1, 2, 3)[0].Quantity == 10m, "OCR title preamble does not break tab-separated table detection");
            Check(HoldingImport.ParseText("Symbol;Quantity;Currency\nAAPL;10;USD")[1].Count == 3, "Semicolon export supported");
            Check(HoldingImport.ParseText("Symbol  Quantity  Currency\nAAPL    10        USD")[1][1] == "10", "OCR wide gaps preserve columns");
            Check(HoldingImport.ParseText("A,B\n\"line1\nline2\",\"a\"\"b\"")[1][1] == "a\"b", "Quoted newlines and escaped quotes parse");
            Invalid(delegate { HoldingImport.ParseText("A,B\n\"broken"); }, "Unterminated quoted field rejected");
            decimal number; Check(HoldingImport.Number("(1,250.5)", out number) && number == -1250.5m, "Signed short share quantities supported");
            Check(!HoldingImport.Number("1.234,50", out number) && !HoldingImport.Number("5%", out number), "Ambiguous locale and percent values rejected");
            var html = HoldingImport.ParseText("<table><tr><th>Symbol</th><th>Quantity</th></tr><tr><td>BRK.B</td><td>2.5</td></tr></table>");
            Check(HoldingImport.Map(html, 0, 0, 1, -1, -1)[0].Symbol == "BRK-B", "HTML and class-share symbols normalize");
            var extension = HoldingImport.ParseText("{\"schema\":\"slaves-holdings-table-v1\",\"rows\":[[\"Symbol\",\"Quantity\"],[\"AAPL\",\"10\"]]}");
            Check(HoldingImport.Map(extension, 0, 0, 1, -1, -1).Count == 1, "Extension JSON feeds the same preview pipeline");
            Invalid(delegate { HoldingImport.ParseText("{\"schema\":\"wrong\",\"rows\":[]}"); }, "Unknown extension schema rejected");
            Invalid(delegate { HoldingImport.ParseText("{\"schema\":\"slaves-holdings-table-v1\",\"rows\":[3]}"); }, "Malformed extension rows rejected");
            Invalid(delegate { HoldingImport.Map(HoldingImport.ParseText("Symbol,Quantity\nAAPL,10\nBAD OPTION,4"), 0, 0, 1, -1, -1); }, "Bad rows block the whole confirmation instead of silent skipping");
            Invalid(delegate { HoldingImport.Map(HoldingImport.ParseText("Symbol,Quantity,Currency\n0700.HK,10,HKD"), 0, 0, 1, -1, 2); }, "Other markets and currencies rejected");
            Invalid(delegate { HoldingImport.Map(HoldingImport.ParseText("Symbol,Quantity\nAAPL,0"), 0, 0, 1, -1, -1); }, "Zero share positions rejected");
            Invalid(delegate { HoldingImport.Map(HoldingImport.ParseText("Symbol,Quantity\nAAPL,1\nAAPL,2"), 0, 0, 1, -1, -1); }, "Duplicate symbols require explicit consolidation");
            Invalid(delegate { HoldingImport.Map(csv, 0, 1, 1, -1, -1); }, "Symbol and shares cannot map to the same column");
            Check(HoldingImport.GuessColumn(new List<string> { "代码", "持仓数量", "平均成本", "币种" }, "quantity") == 1, "Chinese header aliases supported");
            Check(HoldingImport.GuessColumn(new List<string> { "Cost Basis", "Market Value" }, "cost") == -1, "Total cost and market value are never mistaken for per-share cost");
            Check(StockMath.NormalizeSymbol("NASDAQ:aapl") == "AAPL" && !StockMath.ValidSymbol("AAPL261120C00200000"), "US prefixes supported and option codes rejected");
            // 测试数据不进入已签名程序目录 / Test data must stay outside the signed application directory.
            var directory = Path.Combine(Path.GetTempPath(), "SlavesEncourager-tests", "stock-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            string workbook = Path.Combine(directory, "sample.xlsx"), stateFile = Path.Combine(directory, "portfolio.json");
            Xlsx(workbook, "worksheets/sheet1.xml", false, false);
            Check(HoldingImport.Map(HoldingImport.ReadXlsx(workbook), 0, 0, 1, 2, 3)[0].AverageCost == 150m, "XLSX inline strings and numeric cells import");
            Xlsx(workbook, "worksheets/sheet1.xml", false, true); Check(HoldingImport.ReadXlsx(workbook)[1][0] == "AAPL", "XLSX shared strings import");
            Xlsx(workbook, "worksheets/sheet1.xml", true, false); Invalid(delegate { HoldingImport.ReadXlsx(workbook); }, "XLSX formulas are not executed or read as cached values");
            Xlsx(workbook, "../outside.xml", false, false); Invalid(delegate { HoldingImport.ReadXlsx(workbook); }, "XLSX traversal relationships rejected");
            var state = new PortfolioState { Positions = positions, Online = false }; state.Save(stateFile);
            Check(PortfolioState.Load(stateFile).Positions[0].AverageCost == 150m && !PortfolioState.Load(stateFile).Online, "Local portfolio persists without enabling quotes");
            var draft = state.Copy(); draft.Positions[0].Quantity = 999m; Check(state.Positions[0].Quantity == 10m, "Cancel isolation preserves original holdings");
            state.Online = true; state.Save(stateFile); Check(File.Exists(stateFile + ".bak"), "Portfolio saves atomically with backup");
            var empty = new PortfolioState(); empty.Save(stateFile); Check(PortfolioState.Load(stateFile).Positions.Count == 0, "Empty portfolio remains empty after restart");
            var q = YahooQuoteProvider.Parse(YahooFixture("USD", "EQUITY", "NMS", true), "AAPL", Now);
            Check(q.PreviousClose == 200m && q.PreviousDate == "2026-11-02", "Prior session daily bar used instead of chartPreviousClose 123");
            Check(q.TradingDate == "2026-11-03" && q.Price == 202m, "Regular-session quote date is New York date");
            var noBaseline = YahooQuoteProvider.Parse(YahooFixture("USD", "EQUITY", "NMS", false), "AAPL", Now);
            Check(StockMath.PriceUsable(noBaseline, Now) && !StockMath.Usable(noBaseline, Now), "Missing prior close keeps monitor price but never fabricates PnL");
            Invalid(delegate { YahooQuoteProvider.Parse(YahooFixture("HKD", "EQUITY", "HKG", true), "AAPL", Now); }, "Non-USD quote blocked");
            Invalid(delegate { YahooQuoteProvider.Parse(YahooFixture("USD", "CRYPTOCURRENCY", "CCC", true), "AAPL", Now); }, "Crypto quote blocked");
            Invalid(delegate { YahooQuoteProvider.Parse(YahooFixture("USD", "EQUITY", "NMS", true), "MSFT", Now); }, "Wrong returned symbol blocked");
            var quotes = new Dictionary<string, StockQuote> { { "AAPL", q }, { "MSFT", Quote("MSFT", 198m) } };
            var summary = StockMath.Summary(positions, quotes, Now);
            Check(summary.DayPnl == 10m && summary.Covered == 2, "Fixed-share daily PnL equals 10*2 plus 5*(-2)");
            positions[0].AverageCost = 1m; Check(StockMath.Summary(positions, quotes, Now).DayPnl == 10m, "Daily PnL does not confuse average cost with prior close");
            var shortShares = new List<StockPosition> { new StockPosition { Symbol = "AAPL", Quantity = -0.5m } };
            Check(StockMath.Summary(shortShares, quotes, Now).DayPnl == -1m, "Fractional short holdings retain PnL direction");
            quotes.Remove("MSFT"); summary = StockMath.Summary(positions, quotes, Now); Check(summary.DayPnl == null && summary.PartialPnl == 20m && summary.Covered == 1, "Partial coverage is not misreported as a complete total");
            quotes["MSFT"] = Quote("MSFT", 198m); quotes["MSFT"].Simulated = true;
            Check(StockMath.Summary(positions, quotes, Now).DayPnl == null, "Simulated and real quotes cannot mix in total");
            quotes["MSFT"].Simulated = false; quotes["MSFT"].ErrorKey = "stock.stale";
            Check(StockMath.Summary(positions, quotes, Now).DayPnl == null, "Failed refresh keeps stale values outside totals");
            q.FetchedUtc = Now.AddMinutes(-11); Check(!StockMath.Usable(q, Now), "Cache older than ten minutes excluded"); q.FetchedUtc = Now;
            q.AsOfUtc = Now.AddMinutes(-21); Check(!StockMath.Usable(q, Now), "Regular-session quote lag above twenty minutes excluded"); q.AsOfUtc = Now;
            q.AsOfUtc = Now.AddMinutes(11); Check(!StockMath.Usable(q, Now), "Future quote times rejected"); q.AsOfUtc = Now;
            q.PreviousDate = q.TradingDate; Check(!StockMath.Usable(q, Now), "Same-day price cannot masquerade as prior close"); q.PreviousDate = "2026-11-02";
            var weekend = Quote("AAPL", 202m); weekend.AsOfUtc = new DateTimeOffset(2026, 10, 30, 20, 0, 0, TimeSpan.Zero); weekend.TradingDate = "2026-10-30"; weekend.PreviousDate = "2026-10-29"; weekend.FetchedUtc = new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
            Check(StockMath.Usable(weekend, weekend.FetchedUtc), "Weekend latest-session quote remains explicitly dated and usable");
            weekend.FetchedUtc = Now; Check(!StockMath.Usable(weekend, Now), "Previous-session quote rejected during an active weekday session");
            Check(StockMath.InNewYork(new DateTimeOffset(2026, 7, 1, 18, 0, 0, TimeSpan.Zero)).Offset == TimeSpan.FromHours(-4) && StockMath.InNewYork(Now).Offset == TimeSpan.FromHours(-5), "New York DST uses Windows zone rules");
            var settings = new Settings(); decimal salary = Engine.Calculate(Now.LocalDateTime, settings).Month; StockMath.Summary(positions, quotes, Now);
            Check(Engine.Calculate(Now.LocalDateTime, settings).Month == salary && Engine.ExpenseTotal(settings) == 0m, "Portfolio PnL never mutates salary or expenses");
            var fake = new FakeProvider();
            using (var service = new StockService(stateFile, false, fake))
            {
                service.Save(new PortfolioState { Positions = new List<StockPosition> { new StockPosition { Symbol = "AAPL", Quantity = 1m } }, Online = false });
                service.RefreshAsync().GetAwaiter().GetResult(); Check(fake.Calls == 0, "No network calls without confirmed online opt-in");
                service.Save(new PortfolioState { Positions = new List<StockPosition> { new StockPosition { Symbol = "AAPL", Quantity = 1m } }, Online = true });
                service.RefreshAsync().GetAwaiter().GetResult(); Check(fake.Calls == 1 && service.Quotes.Count == 1, "Confirmed opt-in refreshes fixed symbols");
                service.RefreshAsync().GetAwaiter().GetResult(); Check(fake.Calls == 1 && service.StatusKey == "stock.cooldown", "Manual refresh is rate limited");
                service.StopOnline(); Check(!PortfolioState.Load(stateFile).Online && !service.State.Online, "Stop online persists across restarts");
            }
            fake = new FakeProvider { Block = true };
            using (var service = new StockService(stateFile, false, fake))
            {
                service.Save(new PortfolioState { Positions = new List<StockPosition> { new StockPosition { Symbol = "AAPL", Quantity = 1m } }, Online = true });
                var running = service.RefreshAsync(); service.StopOnline(); running.GetAwaiter().GetResult();
                Check(!service.Busy && !service.State.Online && service.Quotes.Count == 0, "Stop online cancels an active refresh without repopulating quotes");
            }
            // 自选股不代表持仓，也不能参与持仓盈亏 / Watchlist entries are not owned shares and never enter holding PnL.
            var originalWatch = new PortfolioState();
            var watch = StockMath.Manual(originalWatch, " aapl, NASDAQ:msft; BRK.B ", true, null, null, null);
            Check(watch.Positions.Count == 3 && watch.Positions.All(p => p.WatchOnly && p.Quantity == 0m && p.AverageCost == null) && watch.Positions[2].Symbol == "BRK-B", "Symbol-only batch watchlist normalizes symbols without invented shares");
            Check(originalWatch.Positions.Count == 0 && !watch.Online, "Manual add preserves original and defaults to no network");
            var fresh = new Dictionary<string, StockQuote> { { "AAPL", Quote("AAPL", 202m) }, { "MSFT", Quote("MSFT", 198m) } };
            var watched = StockMath.Summary(watch.Positions, fresh, Now);
            Check(watched.Total == 0 && watched.DayPnl == null && watched.WatchTotal == 3 && watched.WatchCovered == 2, "Watch-only dashboard has coverage counts and no zero-dollar PnL");
            var missingCloseWatch = new List<StockPosition> { new StockPosition { Symbol = "AAPL", WatchOnly = true } };
            Check(StockMath.Summary(missingCloseWatch, new Dictionary<string, StockQuote> { { "AAPL", noBaseline } }, Now).WatchCovered == 1, "Monitor can show a price without a prior-session baseline");
            var mixed = new List<StockPosition> { new StockPosition { Symbol = "AAPL", Quantity = 10m }, new StockPosition { Symbol = "BRK-B", WatchOnly = true } };
            var mixedSummary = StockMath.Summary(mixed, fresh, Now);
            Check(mixedSummary.DayPnl == 20m && mixedSummary.Total == 1 && mixedSummary.WatchCovered == 0, "Missing watch quote never contaminates complete holdings PnL");
            var converted = StockMath.Manual(watch, "AAPL", false, 2.5m, 150m, "AAPL");
            Check(converted.Positions.Count == 3 && converted.Positions[0].Symbol == "AAPL" && !converted.Positions.Single(p => p.Symbol == "AAPL").WatchOnly && converted.Positions.Single(p => p.Symbol == "AAPL").Quantity == 2.5m && watch.Positions.All(p => p.WatchOnly), "Converting a selected watch item preserves other entries and original draft");
            var reverted = StockMath.Manual(converted, "AAPL", true, null, null, "AAPL");
            Check(reverted.Positions.Single(p => p.Symbol == "AAPL").AverageCost == null && reverted.Positions.Single(p => p.Symbol == "AAPL").Quantity == 0m, "Converting back to watch-only clears owned shares and cost");
            Invalid(delegate { StockMath.Manual(watch, "AAPL", true, null, null, null); }, "Adding an existing symbol requires explicit edit");
            Invalid(delegate { StockMath.Manual(new PortfolioState(), "aapl,AAPL", true, null, null, null); }, "Duplicate normalized batch codes rejected");
            Invalid(delegate { StockMath.Manual(new PortfolioState(), "AAPL,MSFT", false, 1m, null, null); }, "Owned share additions require one symbol at a time");
            Invalid(delegate { StockMath.Manual(watch, "MSFT", true, null, null, "AAPL"); }, "Renaming to another existing symbol rejected");
            Invalid(delegate { StockMath.Manual(new PortfolioState(), "AAPL", false, null, null, null); }, "Holding mode requires explicit quantity");
            Invalid(delegate { StockMath.Manual(new PortfolioState(), "AAPL", false, 0m, null, null); }, "Zero quantity is not treated as an owned position");
            Invalid(delegate { StockMath.Manual(new PortfolioState(), String.Join(",", Enumerable.Range(0, 51).Select(i => "X" + i)), true, null, null, null); }, "Combined watch and holdings limit enforced");
            watch.Save(stateFile); var restoredWatch = PortfolioState.Load(stateFile);
            Check(restoredWatch.Positions.All(p => p.WatchOnly) && restoredWatch.Positions.Count == 3, "Watch-only mode persists across restarts");
            state.Save(stateFile); Check(PortfolioState.Load(stateFile).Positions.All(p => !p.WatchOnly), "Older entries without WatchOnly stay owned positions");
            Check(!new Settings().StocksEnabled, "Stock feature defaults off independently of saved portfolio consent");
            var featureSettings = new Settings { StocksEnabled = true }; string settingsFile = Path.Combine(directory, "settings.json"); featureSettings.Save(settingsFile);
            string settingsNotice; Check(Settings.Load(settingsFile, out settingsNotice).StocksEnabled, "Stock visibility setting persists");
            File.WriteAllText(settingsFile, File.ReadAllText(settingsFile).Replace(",\"StocksEnabled\":true", "").Replace("\"StocksEnabled\":true,", ""), new UTF8Encoding(false));
            Check(!Settings.Load(settingsFile, out settingsNotice).StocksEnabled && settingsNotice == "", "Older settings without stock visibility stay hidden");
            var visibilityDraft = featureSettings.Copy(); visibilityDraft.StocksEnabled = false;
            Check(featureSettings.StocksEnabled, "Cancelling stock visibility draft preserves original preference");
            fake = new FakeProvider();
            using (var service = new StockService(stateFile, false, fake, false))
            {
                service.Save(new PortfolioState { Online = true, Positions = new List<StockPosition> { new StockPosition { Symbol = "AAPL", Quantity = 1m } } });
                service.RefreshAsync().GetAwaiter().GetResult(); Check(fake.Calls == 0, "Hidden module blocks requests despite previously saved quote consent");
                service.SetFeatureEnabled(true); service.RefreshAsync().GetAwaiter().GetResult(); Check(fake.Calls == 1, "Enabling the module allows previously authorized quotes");
                service.SetFeatureEnabled(false); service.RefreshAsync().GetAwaiter().GetResult();
                Check(fake.Calls == 1 && service.State.Positions.Count == 1 && PortfolioState.Load(stateFile).Online, "Hiding pauses quotes while retaining holdings and separate quote consent");
            }
            fake = new FakeProvider { Block = true };
            using (var service = new StockService(stateFile, false, fake, true))
            {
                var running = service.RefreshAsync(); service.SetFeatureEnabled(false); running.GetAwaiter().GetResult();
                Check(!service.Busy && !service.FeatureEnabled && service.Quotes.Count == 0 && service.State.Online, "Hiding cancels an active refresh without deleting saved consent");
            }
            Console.WriteLine("ALL " + checks + " STOCK CHECKS PASSED"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
