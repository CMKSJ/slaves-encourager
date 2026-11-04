using System.Text.Json;

// 兼容共享核心的旧框架接口，不包含 WPF / Adapt legacy core interfaces without including WPF.
namespace System.Windows
{
    public sealed class ResourceDictionary : Dictionary<string, object> { }
}
namespace System.Windows.Threading
{
    public sealed class DispatcherTimer
    {
        readonly Avalonia.Threading.DispatcherTimer timer = new();
        public TimeSpan Interval { get => timer.Interval; set => timer.Interval = value; }
        public event EventHandler Tick { add => timer.Tick += value; remove => timer.Tick -= value; }
        public void Start() => timer.Start();
        public void Stop() => timer.Stop();
    }
}
namespace System.Web.Script.Serialization
{
    public sealed class JavaScriptSerializer
    {
        public int MaxJsonLength { get; set; } = 2097152;
        public int RecursionLimit { get; set; } = 100;
        public object DeserializeObject(string value)
        {
            if (value.Length > MaxJsonLength) throw new ArgumentException("JSON size limit");
            try
            {
                using var document = JsonDocument.Parse(value, new JsonDocumentOptions { MaxDepth = RecursionLimit });
                return Convert(document.RootElement);
            }
            catch (JsonException e) { throw new ArgumentException("Invalid JSON", e); }
        }
        static object Convert(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => Convert(p.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(Convert).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetDecimal(out var number) ? number : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
        public string Serialize(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true, MaxDepth = RecursionLimit });
    }
}
