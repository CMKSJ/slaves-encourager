using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

// 使用系统 WinRT 元数据，不安装 OCR 包或修改脚本策略 / Use system WinRT metadata without installing OCR or changing script policy.
public static class OcrHelper
{
    sealed class Word { public string Text; public double X, Y, Width, Height; }
    static Type Win(string name, string assembly)
    { return Type.GetType(name + ", " + assembly + ", ContentType=WindowsRuntime", true); }
    static object Get(object value, string name) { return value.GetType().GetProperty(name).GetValue(value, null); }
    static object Await(object operation, Type result)
    {
        var method = typeof(System.WindowsRuntimeSystemExtensions).GetMethods().Single(m => m.Name == "AsTask" && m.IsGenericMethod && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.Name == "IAsyncOperation`1");
        object task = method.MakeGenericMethod(result).Invoke(null, new[] { operation });
        ((Task)task).GetAwaiter().GetResult(); return task.GetType().GetProperty("Result").GetValue(task, null);
    }
    static void Close(object value) { var disposable = value as IDisposable; if (disposable != null) disposable.Dispose(); }
    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        try
        {
            if (args.Length != 1) return 2;
            Type storage = Win("Windows.Storage.StorageFile", "Windows.Storage"), streamType = Win("Windows.Storage.Streams.IRandomAccessStreamWithContentType", "Windows.Storage");
            Type decoderType = Win("Windows.Graphics.Imaging.BitmapDecoder", "Windows.Foundation"), bitmapType = Win("Windows.Graphics.Imaging.SoftwareBitmap", "Windows.Foundation");
            Type engineType = Win("Windows.Media.Ocr.OcrEngine", "Windows.Foundation"), resultType = Win("Windows.Media.Ocr.OcrResult", "Windows.Foundation");
            object file = Await(storage.GetMethod("GetFileFromPathAsync").Invoke(null, new object[] { System.IO.Path.GetFullPath(args[0]) }), storage);
            object stream = Await(storage.GetMethod("OpenReadAsync", Type.EmptyTypes).Invoke(file, null), streamType);
            try
            {
                object decoder = Await(decoderType.GetMethods().Single(m => m.Name == "CreateAsync" && m.GetParameters().Length == 1).Invoke(null, new[] { stream }), decoderType);
                uint max = Convert.ToUInt32(engineType.GetProperty("MaxImageDimension").GetValue(null, null), CultureInfo.InvariantCulture);
                if (Convert.ToUInt32(Get(decoder, "PixelWidth"), CultureInfo.InvariantCulture) > max || Convert.ToUInt32(Get(decoder, "PixelHeight"), CultureInfo.InvariantCulture) > max) return 3;
                Type format = Win("Windows.Graphics.Imaging.BitmapPixelFormat", "Windows.Foundation"), alpha = Win("Windows.Graphics.Imaging.BitmapAlphaMode", "Windows.Foundation");
                object bitmap = Await(decoderType.GetMethod("GetSoftwareBitmapAsync", new[] { format, alpha }).Invoke(decoder, new[] { Enum.Parse(format, "Bgra8"), Enum.Parse(alpha, "Premultiplied") }), bitmapType);
                try
                {
                    Type language = Win("Windows.Globalization.Language", "Windows.Globalization");
                    object engine = engineType.GetMethod("TryCreateFromLanguage").Invoke(null, new[] { Activator.CreateInstance(language, new object[] { "en-US" }) });
                    if (engine == null) engine = engineType.GetMethod("TryCreateFromUserProfileLanguages").Invoke(null, null);
                    if (engine == null) return 4;
                    object result = Await(engineType.GetMethod("RecognizeAsync").Invoke(engine, new[] { bitmap }), resultType);
                    var words = new List<Word>();
                    foreach (object line in (IEnumerable)Get(result, "Lines"))
                    {
                        foreach (object word in (IEnumerable)Get(line, "Words"))
                        {
                            object bounds = Get(word, "BoundingRect");
                            words.Add(new Word { Text = (string)Get(word, "Text"), X = Convert.ToDouble(Get(bounds, "X"), CultureInfo.InvariantCulture), Y = Convert.ToDouble(Get(bounds, "Y"), CultureInfo.InvariantCulture), Width = Convert.ToDouble(Get(bounds, "Width"), CultureInfo.InvariantCulture), Height = Convert.ToDouble(Get(bounds, "Height"), CultureInfo.InvariantCulture) });
                        }
                    }
                    // OCR 可按列返回文本，按几何位置重建行 / OCR may return column-major text; rebuild rows from geometry.
                    var groups = new List<List<Word>>();
                    foreach (var word in words.OrderBy(w => w.Y + w.Height / 2))
                    {
                        var row = groups.LastOrDefault();
                        if (row == null || Math.Abs(row.Average(w => w.Y + w.Height / 2) - (word.Y + word.Height / 2)) > Math.Max(8, word.Height * 0.6))
                        { row = new List<Word>(); groups.Add(row); }
                        row.Add(word);
                    }
                    var lines = new List<string>();
                    foreach (var row in groups)
                    {
                        var text = new StringBuilder(); double edge = 0;
                        foreach (var word in row.OrderBy(w => w.X))
                        {
                            if (text.Length > 0) text.Append(word.X - edge > Math.Max(18, word.Height * 1.1) ? "\t" : " ");
                            text.Append(word.Text); edge = word.X + word.Width;
                        }
                        lines.Add(text.ToString());
                    }
                    Console.WriteLine(new JavaScriptSerializer().Serialize(new Dictionary<string, string> { { "text", String.Join("\n", lines) } })); return 0;
                }
                finally { Close(bitmap); }
            }
            finally { Close(stream); }
        }
        catch
        {
            // 不泄漏原图、路径或识别文本 / Never log source image, path or recognized account text.
            Console.Error.WriteLine("Local OCR unavailable; no image was uploaded."); return 1;
        }
    }
}
