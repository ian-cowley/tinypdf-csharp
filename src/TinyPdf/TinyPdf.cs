using System.Buffers;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace TinyPdf;

public class TinyPdf
{
    public static Builder Create() => new Builder();

    public enum PdfFont { Helvetica, Times, Courier }

    public record TextOptions(string? Align = "left", double? Width = null, string Color = "#000000", PdfFont Font = PdfFont.Helvetica);

    private static readonly Dictionary<PdfFont, int[]> FontWidths = new()
    {
        [PdfFont.Helvetica] = new int[] {
            278,278,355,556,556,889,667,191,333,333,389,584,278,333,278,278,
            556,556,556,556,556,556,556,556,556,556,278,278,584,584,584,556,
            1015,667,667,722,722,667,611,778,722,278,500,667,556,833,722,778,
            667,778,722,667,611,722,667,944,667,667,611,278,278,278,469,556,
            333,556,556,500,556,556,278,556,556,222,222,500,222,833,556,556,
            556,556,333,500,278,556,500,722,500,500,500,334,260,334,584
        },
        [PdfFont.Times] = new int[] {
            250,333,408,500,500,833,778,180,333,333,500,564,250,333,250,278,
            500,500,500,500,500,500,500,500,500,500,278,278,564,564,564,444,
            921,722,667,667,722,611,556,722,722,333,389,722,611,889,722,722,
            556,722,667,556,611,722,722,944,722,722,611,333,278,333,469,500,
            333,444,500,444,500,444,333,500,500,278,278,500,278,778,500,500,
            500,500,333,389,278,500,500,722,500,500,444,480,200,480,541
        },
        [PdfFont.Courier] = Enumerable.Repeat(600, 95).ToArray()
    };

    public interface IPageContext
    {
        void Text(ReadOnlyMemory<char> prefix, ReadOnlyMemory<char> str, double x, double y, double size, TextOptions? opts = null);
        void Rect(double x, double y, double w, double h, string fill);
        void Line(double x1, double y1, double x2, double y2, string stroke, double lineWidth = 1);
        void Image(byte[] jpegBytes, double x, double y, double w, double h);
    }

    public class Ref
    {
        public int Id { get; }
        public Ref(int id) { Id = id; }
    }

    private class PdfObject
    {
        public int Id { get; set; }
        public Dictionary<string, object> Dict { get; set; } = new Dictionary<string, object>();
        public byte[]? Stream { get; set; }
    }

    // Pooled growable buffer writer that exposes written chunks so we can stream compressed output
    private sealed class PooledBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private const int DefaultBlockSize = 8192;
        private readonly List<byte[]> _blocks = new List<byte[]>();
        private int _posInLast = 0;
        private int _written = 0;

        public int WrittenCount => _written;

        public void Advance(int count)
        {
            _posInLast += count;
            _written += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            var span = GetSpan(sizeHint);
            // return a Memory copy only for API conformance; callers should use GetSpan where possible
            var copy = new byte[span.Length];
            span.CopyTo(copy);
            return new Memory<byte>(copy);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (_blocks.Count == 0 || _posInLast == _blocks[_blocks.Count - 1].Length)
            {
                int newSize = Math.Max(DefaultBlockSize, Math.Max(1, sizeHint));
                var buf = ArrayPool<byte>.Shared.Rent(newSize);
                _blocks.Add(buf);
                _posInLast = 0;
            }
            var last = _blocks[_blocks.Count - 1];
            return new Span<byte>(last, _posInLast, last.Length - _posInLast);
        }

        // Write all stored chunks into destination stream
        public void CopyTo(Stream dest)
        {
            for (int i = 0; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                int count = (i == _blocks.Count - 1) ? _posInLast : block.Length;
                if (count > 0) dest.Write(block, 0, count);
            }
        }

        public void Dispose()
        {
            foreach (var b in _blocks)
            {
                ArrayPool<byte>.Shared.Return(b);
            }
            _blocks.Clear();
            _posInLast = 0;
            _written = 0;
        }
    }

    private sealed class PooledBufferStream : Stream
    {
        private readonly PooledBufferWriter _writer;
        public PooledBufferStream(PooledBufferWriter writer) { _writer = writer; }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count)
        {
            int remaining = count;
            int src = offset;
            while (remaining > 0)
            {
                var span = _writer.GetSpan(remaining);
                int toCopy = Math.Min(span.Length, remaining);
                buffer.AsSpan(src, toCopy).CopyTo(span);
                _writer.Advance(toCopy);
                src += toCopy;
                remaining -= toCopy;
            }
        }
    }

    // public API measure (string)
    public static double MeasureText(string str, double size, PdfFont font = PdfFont.Helvetica)
    {
        double width = 0;
        var widths = FontWidths[font];
        foreach (char c in str)
        {
            int code = c;
            int w = (code >= 32 && code <= 126) ? widths[code - 32] : 556;
            width += w;
        }
        return (width * size) / 1000;
    }

    // memory/span-based measure
    private static double MeasureText(ReadOnlySpan<char> span, double size, PdfFont font = PdfFont.Helvetica)
    {
        double width = 0;
        var widths = FontWidths[font];
        for (int i = 0; i < span.Length; i++)
        {
            int code = span[i];
            int w = (code >= 32 && code <= 126) ? widths[code - 32] : 556;
            width += w;
        }
        return (width * size) / 1000;
    }

    private static double MeasureText(ReadOnlyMemory<char> mem, double size, PdfFont font = PdfFont.Helvetica) => MeasureText(mem.Span, size, font);

    private static double[]? ParseColor(string? hex)
    {
        if (string.IsNullOrEmpty(hex) || hex == "none") return null;
        hex = hex.Replace("#", "");
        if (hex.Length == 3) hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
        if (hex.Length != 6) return null;
        try
        {
            double r = int.Parse(hex[..2], NumberStyles.HexNumber) / 255.0;
            double g = int.Parse(hex[2..4], NumberStyles.HexNumber) / 255.0;
            double b = int.Parse(hex[4..6], NumberStyles.HexNumber) / 255.0;
            return new double[] { r, g, b };
        }
        catch { return null; }
    }

    private static void BufferWriteAscii(IBufferWriter<byte> w, string s)
    {
        var span = w.GetSpan(s.Length);
        for (int i = 0; i < s.Length; i++) span[i] = (byte)s[i];
        w.Advance(s.Length);
    }

    private static void BufferWriteUtf8(IBufferWriter<byte> w, ReadOnlySpan<char> chars)
    {
        int byteCount = Encoding.UTF8.GetByteCount(chars);
        var span = w.GetSpan(byteCount);
        int written = Encoding.UTF8.GetBytes(chars, span);
        w.Advance(written);
    }

    private static void BufferWriteDouble(IBufferWriter<byte> w, double val, byte precision)
    {
        var span = w.GetSpan(32);
        if (System.Buffers.Text.Utf8Formatter.TryFormat(val, span, out int bytesWritten, new System.Buffers.StandardFormat('F', precision)))
        {
            w.Advance(bytesWritten);
            return;
        }
        BufferWriteUtf8(w, val.ToString(CultureInfo.InvariantCulture));
    }

    private static void BufferWritePdfString(IBufferWriter<byte> w, ReadOnlySpan<char> prefix, ReadOnlySpan<char> content)
    {
        BufferWriteAscii(w, "(");
        void writeEsc(ReadOnlySpan<char> s)
        {
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '\\') { BufferWriteAscii(w, "\\\\"); i++; }
                else if (c == '(') { BufferWriteAscii(w, "\\("); i++; }
                else if (c == ')') { BufferWriteAscii(w, "\\)"); i++; }
                else if (c == '\r') { BufferWriteAscii(w, "\\r"); i++; }
                else if (c == '\n') { BufferWriteAscii(w, "\\n"); i++; }
                else if (c <= 127)
                {
                    var span = w.GetSpan(1);
                    span[0] = (byte)c;
                    w.Advance(1);
                    i++;
                }
                else
                {
                    int j = i + 1;
                    while (j < s.Length && s[j] > 127) j++;
                    BufferWriteUtf8(w, s.Slice(i, j - i));
                    i = j;
                }
            }
        }

        if (!prefix.IsEmpty) writeEsc(prefix);
        if (!content.IsEmpty) writeEsc(content);
        BufferWriteAscii(w, ")");
    }

    private static void SerializeTo(Stream stream, object? val)
    {
        var writer = new ArrayBufferWriter<byte>(256);
        SerializeToWriter(writer, val);
        var span = writer.WrittenSpan;
        stream.Write(span);
    }

    private static void SerializeToWriter(IBufferWriter<byte> writer, object? val)
    {
        static void WriteAscii(IBufferWriter<byte> w, string s)
        {
            var span = w.GetSpan(s.Length);
            for (int i = 0; i < s.Length; i++) span[i] = (byte)s[i];
            w.Advance(s.Length);
        }

        static void WriteUtf8String(IBufferWriter<byte> w, ReadOnlySpan<char> chars)
        {
            int byteCount = Encoding.UTF8.GetByteCount(chars);
            var span = w.GetSpan(byteCount);
            int written = Encoding.UTF8.GetBytes(chars, span);
            w.Advance(written);
        }

        static void WritePdfString(IBufferWriter<byte> w, string s)
        {
            WriteAscii(w, "(");
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '\\') { WriteAscii(w, "\\\\"); i++; }
                else if (c == '(') { WriteAscii(w, "\\("); i++; }
                else if (c == ')') { WriteAscii(w, "\\)"); i++; }
                else if (c == '\r') { WriteAscii(w, "\\r"); i++; }
                else if (c == '\n') { WriteAscii(w, "\\n"); i++; }
                else if (c <= 127)
                {
                    var span = w.GetSpan(1);
                    span[0] = (byte)c;
                    w.Advance(1);
                    i++;
                }
                else
                {
                    int j = i + 1;
                    while (j < s.Length && s[j] > 127) j++;
                    WriteUtf8String(w, s.AsSpan(i, j - i));
                    i = j;
                }
            }
            WriteAscii(w, ")");
        }

        if (val == null) { WriteAscii(writer, "null"); return; }
        if (val is bool bb) { WriteAscii(writer, bb ? "true" : "false"); return; }
        if (val is int ii)
        {
            var span = writer.GetSpan(16);
            if (System.Buffers.Text.Utf8Formatter.TryFormat(ii, span, out int bytesWritten)) writer.Advance(bytesWritten);
            return;
        }
        if (val is double dd)
        {
            var span = writer.GetSpan(32);
            if (System.Buffers.Text.Utf8Formatter.TryFormat(dd, span, out int bytesWritten)) writer.Advance(bytesWritten);
            return;
        }
        if (val is float ff)
        {
            var span = writer.GetSpan(32);
            if (System.Buffers.Text.Utf8Formatter.TryFormat(ff, span, out int bytesWritten)) writer.Advance(bytesWritten);
            return;
        }
        if (val is string s)
        {
            if (s.StartsWith('/')) { WriteUtf8String(writer, s); return; }
            if (s.StartsWith('(')) { WriteUtf8String(writer, s); return; }
            WritePdfString(writer, s);
            return;
        }
        if (val is Ref r) {
            var span = writer.GetSpan(16);
            if (System.Buffers.Text.Utf8Formatter.TryFormat(r.Id, span, out int bytesWritten)) writer.Advance(bytesWritten);
            WriteAscii(writer, " 0 R");
            return;
        }
        if (val is IDictionary<string, object> dict)
        {
            WriteAscii(writer, "<<\n");
            foreach (var kvp in dict)
            {
                WriteAscii(writer, "/"); WriteAscii(writer, kvp.Key); WriteAscii(writer, " ");
                SerializeToWriter(writer, kvp.Value);
                WriteAscii(writer, "\n");
            }
            WriteAscii(writer, ">>");
            return;
        }
        if (val is System.Collections.IEnumerable enumerable && val is not string)
        {
            WriteAscii(writer, "[");
            bool first = true;
            foreach (var item in enumerable)
            {
                if (!first) WriteAscii(writer, " ");
                SerializeToWriter(writer, item);
                first = false;
            }
            WriteAscii(writer, "]");
            return;
        }

        var str = val.ToString() ?? "null";
        WriteUtf8String(writer, str);
    }

    public class Builder
    {
        public bool Compress { get; set; } = true;
        private List<PdfObject> _objects = new List<PdfObject>();
        private List<Ref> _pages = new List<Ref>();
        private int _nextId = 1;

        public Ref AddObject(Dictionary<string, object> dict, byte[]? streamBytes = null)
        {
            var id = _nextId++;
            var obj = new PdfObject { Id = id, Dict = dict, Stream = streamBytes };
            _objects.Add(obj);
            return new Ref(id);
        }

        public void Page(double width, double height, Action<IPageContext> fn)
        {
            var writer = new ArrayBufferWriter<byte>(1024);
            var images = new List<(string Name, Ref Ref)>();
            int imageCount = 0;

            var ctx = new PageContextImpl(this, writer, images, imageCount, (newCount) => imageCount = newCount);
            fn(ctx);

            var contentBytes = writer.WrittenSpan.ToArray();
            var contentRef = AddObject(new Dictionary<string, object> { ["Length"] = contentBytes.Length }, contentBytes);

            var xobjects = new Dictionary<string, object>();
            foreach (var img in images) xobjects[img.Name[1..]] = img.Ref;

            var pageRef = AddObject(new Dictionary<string, object>
            {
                ["Type"] = "/Page",
                ["Parent"] = null!, // Will be set in build
                ["MediaBox"] = new List<double> { 0, 0, width, height },
                ["Contents"] = contentRef,
                ["Resources"] = new Dictionary<string, object>
                {
                    ["Font"] = new Dictionary<string, object> { ["F1"] = null!, ["F2"] = null!, ["F3"] = null! },
                    ["XObject"] = xobjects.Count > 0 ? xobjects : null!
                }
            });

            _pages.Add(pageRef);
        }

        public void Page(Action<IPageContext> fn) => Page(612, 792, fn);

        public double MeasureText(string str, double size) => TinyPdf.MeasureText(str, size);

        public byte[] Build()
        {
            var fontMap = new Dictionary<PdfFont, Ref>();
            fontMap[PdfFont.Helvetica] = AddObject(new Dictionary<string, object> { ["Type"] = "/Font", ["Subtype"] = "/Type1", ["BaseFont"] = "/Helvetica" });
            fontMap[PdfFont.Times] = AddObject(new Dictionary<string, object> { ["Type"] = "/Font", ["Subtype"] = "/Type1", ["BaseFont"] = "/Times-Roman" });
            fontMap[PdfFont.Courier] = AddObject(new Dictionary<string, object> { ["Type"] = "/Font", ["Subtype"] = "/Type1", ["BaseFont"] = "/Courier" });

            var pagesRef = AddObject(new Dictionary<string, object> { ["Type"] = "/Pages", ["Kids"] = _pages, ["Count"] = _pages.Count });

            foreach (var obj in _objects)
            {
                if (obj.Dict.TryGetValue("Type", out var type) && type.ToString() == "/Page")
                {
                    obj.Dict["Parent"] = pagesRef;
                    if (obj.Dict.TryGetValue("Resources", out var resObj) && resObj is Dictionary<string, object> resources)
                    {
                        if (resources.TryGetValue("Font", out var fontObj) && fontObj is Dictionary<string, object> fonts)
                        {
                            fonts["F1"] = fontMap[PdfFont.Helvetica];
                            fonts["F2"] = fontMap[PdfFont.Times];
                            fonts["F3"] = fontMap[PdfFont.Courier];
                        }
                    }
                }
            }

            var catalogRef = AddObject(new Dictionary<string, object> { ["Type"] = "/Catalog", ["Pages"] = pagesRef });

            var offsets = new int[_objects.Count + 1];
            var headerBytes = new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-', (byte)'1', (byte)'.', (byte)'4', (byte)'\n', (byte)'%', 0xFF, 0xFF, 0xFF, 0xFF, (byte)'\n' };

            using var ms = new MemoryStream();
            ms.Write(headerBytes, 0, headerBytes.Length);

            foreach (var obj in _objects)
            {
                offsets[obj.Id] = (int)ms.Position;

                if (obj.Stream != null)
                {
                    byte[] streamData = obj.Stream;
                    PooledBufferWriter? pbw = null;
                    if (Compress)
                    {
                        pbw = new PooledBufferWriter();
                        using (var zs = new ZLibStream(new PooledBufferStream(pbw), CompressionLevel.Optimal, leaveOpen: true))
                        {
                            zs.Write(streamData, 0, streamData.Length);
                        }
                        int compressedLen = pbw.WrittenCount;
                        obj.Dict["Filter"] = "/FlateDecode";
                        obj.Dict["Length"] = compressedLen;
                    }
                    else obj.Dict["Length"] = streamData.Length;

                    var header = Encoding.UTF8.GetBytes($"{obj.Id} 0 obj\n");
                    ms.Write(header, 0, header.Length);

                    SerializeTo(ms, obj.Dict);

                    var streamMarker = Encoding.UTF8.GetBytes("\nstream\n");
                    ms.Write(streamMarker, 0, streamMarker.Length);

                    if (Compress && pbw is not null)
                    {
                        pbw.CopyTo(ms);
                        pbw.Dispose();
                    }
                    else ms.Write(streamData, 0, streamData.Length);

                    var suffixBytes = Encoding.UTF8.GetBytes("\nendstream\nendobj\n");
                    ms.Write(suffixBytes, 0, suffixBytes.Length);
                }
                else
                {
                    var header = Encoding.UTF8.GetBytes($"{obj.Id} 0 obj\n");
                    ms.Write(header, 0, header.Length);
                    SerializeTo(ms, obj.Dict);
                    var endBytes = Encoding.UTF8.GetBytes("\nendobj\n");
                    ms.Write(endBytes, 0, endBytes.Length);
                }
            }

            int xrefOffset = (int)ms.Position;

            var xrefHeader = Encoding.UTF8.GetBytes($"xref\n0 {_objects.Count + 1}\n0000000000 65535 f \n");
            ms.Write(xrefHeader, 0, xrefHeader.Length);
            for (int i = 1; i <= _objects.Count; i++)
            {
                var line = Encoding.UTF8.GetBytes($"{offsets[i]:D10} 00000 n \n");
                ms.Write(line, 0, line.Length);
            }

            var trailerHeader = Encoding.UTF8.GetBytes("trailer\n");
            ms.Write(trailerHeader, 0, trailerHeader.Length);
            SerializeTo(ms, new Dictionary<string, object> { ["Size"] = _objects.Count + 1, ["Root"] = catalogRef });
            ms.Write(Encoding.UTF8.GetBytes("\n"), 0, 1);

            var startxref = Encoding.UTF8.GetBytes($"startxref\n{xrefOffset}\n%%EOF\n");
            ms.Write(startxref, 0, startxref.Length);

            return ms.ToArray();
        }

        private class PageContextImpl : IPageContext
        {
            private readonly Builder _builder;
            private readonly ArrayBufferWriter<byte> _writer;
            private readonly List<(string Name, Ref Ref)> _images;
            private int _imageCount;
            private readonly Action<int> _updateImageCount;

            public PageContextImpl(Builder builder, ArrayBufferWriter<byte> writer, List<(string Name, Ref Ref)> images, int imageCount, Action<int> updateImageCount)
            {
                _builder = builder;
                _writer = writer;
                _images = images;
                _imageCount = imageCount;
                _updateImageCount = updateImageCount;
            }

            public void Text(ReadOnlyMemory<char> prefix, ReadOnlyMemory<char> str, double x, double y, double size, TextOptions? opts = null)
            {
                opts ??= new TextOptions();
                string align = opts.Align ?? "left";
                double tx = x;
                if (align != "left" && opts.Width.HasValue)
                {
                    double textWidth = TinyPdf.MeasureText(str.Span, size);
                    if (align == "center") tx = x + (opts.Width.Value - textWidth) / 2;
                    if (align == "right") tx = x + opts.Width.Value - textWidth;
                }

                var rgb = ParseColor(opts.Color);
                if (rgb != null)
                {
                    BufferWriteDouble(_writer, rgb[0], 3); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, rgb[1], 3); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, rgb[2], 3); BufferWriteAscii(_writer, " rg\n");
                }

                BufferWriteAscii(_writer, "BT\n");
                string fontTag = opts.Font switch { PdfFont.Times => "/F2", PdfFont.Courier => "/F3", _ => "/F1" };
                BufferWriteAscii(_writer, fontTag); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, size, 2); BufferWriteAscii(_writer, " Tf\n");

                BufferWriteDouble(_writer, tx, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, y, 2); BufferWriteAscii(_writer, " Td\n");

                BufferWritePdfString(_writer, prefix.Span, str.Span);
                BufferWriteAscii(_writer, " Tj\n");
                BufferWriteAscii(_writer, "ET\n");
            }

            public void Rect(double x, double y, double w, double h, string fill)
            {
                var rgb = ParseColor(fill);
                if (rgb != null)
                {
                    BufferWriteDouble(_writer, rgb[0], 3); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, rgb[1], 3); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, rgb[2], 3); BufferWriteAscii(_writer, " rg\n");

                    BufferWriteDouble(_writer, x, 2); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, y, 2); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, w, 2); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, h, 2); BufferWriteAscii(_writer, " re\n");
                    BufferWriteAscii(_writer, "f\n");
                }
            }

            public void Line(double x1, double y1, double x2, double y2, string stroke, double lineWidth = 1)
            {
                var rgb = ParseColor(stroke);
                if (rgb != null)
                {
                    BufferWriteDouble(_writer, lineWidth, 2); BufferWriteAscii(_writer, " w\n");
                    BufferWriteDouble(_writer, rgb[0], 3); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, rgb[1], 3); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, rgb[2], 3); BufferWriteAscii(_writer, " RG\n");

                    BufferWriteDouble(_writer, x1, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, y1, 2); BufferWriteAscii(_writer, " m\n");
                    BufferWriteDouble(_writer, x2, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, y2, 2); BufferWriteAscii(_writer, " l\n");
                    BufferWriteAscii(_writer, "S\n");
                }
            }

            public void Image(byte[] jpegBytes, double x, double y, double w, double h)
            {
                int imgWidth = 0, imgHeight = 0;
                for (int i = 0; i < jpegBytes.Length - 1; i++)
                {
                    if (jpegBytes[i] == 0xFF && (jpegBytes[i+1] == 0xC0 || jpegBytes[i+1] == 0xC2))
                    {
                        imgHeight = (jpegBytes[i + 5] << 8) | jpegBytes[i + 6];
                        imgWidth = (jpegBytes[i + 7] << 8) | jpegBytes[i + 8];
                        break;
                    }
                }

                string imgName = $"/Im{_imageCount++}";
                _updateImageCount(_imageCount);

                var imgRef = _builder.AddObject(new Dictionary<string, object>
                {
                    ["Type"] = "/XObject",
                    ["Subtype"] = "/Image",
                    ["Width"] = imgWidth,
                    ["Height"] = imgHeight,
                    ["ColorSpace"] = "/DeviceRGB",
                    ["BitsPerComponent"] = 8,
                    ["Filter"] = "/DCTDecode",
                    ["Length"] = jpegBytes.Length
                }, jpegBytes);

                _images.Add((imgName, imgRef));

                BufferWriteAscii(_writer, "q\n");
                BufferWriteDouble(_writer, w, 2); BufferWriteAscii(_writer, " 0 0 "); BufferWriteDouble(_writer, h, 2); BufferWriteAscii(_writer, " ");
                BufferWriteDouble(_writer, x, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, y, 2); BufferWriteAscii(_writer, " cm\n");
                BufferWriteAscii(_writer, imgName); BufferWriteAscii(_writer, " Do\n"); BufferWriteAscii(_writer, "Q\n");
            }
        }
    }

    private record MarkdownItem(ReadOnlyMemory<char> Prefix, ReadOnlyMemory<char> Text, double Size, double Indent, double SpaceBefore, double SpaceAfter, bool Rule = false, string Color = "#111111");

    public record MarkdownOptions(double? Width = 612, double? Height = 792, double? Margin = 72, bool Compress = true);

    public static byte[] Markdown(string md, MarkdownOptions? opts = null)
    {
        opts ??= new MarkdownOptions();
        double W = opts.Width ?? 612, H = opts.Height ?? 792, M = opts.Margin ?? 72;
        var builder = new Builder();
        builder.Compress = opts.Compress;
        double textW = W - M * 2;
        double bodySize = 11;

        var items = new List<MarkdownItem>();

        static ReadOnlyMemory<char> TrimStart(ReadOnlyMemory<char> m)
        {
            var s = m.Span; int i = 0; while (i < s.Length && char.IsWhiteSpace(s[i])) i++; return m.Slice(i);
        }
        static ReadOnlyMemory<char> TrimEnd(ReadOnlyMemory<char> m)
        {
            var s = m.Span; int i = s.Length - 1; while (i >= 0 && char.IsWhiteSpace(s[i])) i--; return m.Slice(0, i + 1);
        }

        List<ReadOnlyMemory<char>> Wrap(ReadOnlyMemory<char> textMem, double size, double maxW)
        {
            var lines = new List<ReadOnlyMemory<char>>();
            var span = textMem.Span;
            if (span.Length == 0) { lines.Add(ReadOnlyMemory<char>.Empty); return lines; }

            double spaceW = MeasureText(" ", size);
            int pos = 0; int lineStart = -1; int lineEnd = -1; double currentW = 0;
            while (pos < span.Length)
            {
                int next = span[pos..].IndexOf(' ');
                int wordStart = pos; int wordEnd = next == -1 ? span.Length : pos + next;
                var wordSpan = span.Slice(wordStart, wordEnd - wordStart);
                double w = MeasureText(wordSpan, size);
                if (lineStart == -1)
                {
                    lineStart = wordStart; lineEnd = wordEnd; currentW = w;
                }
                else
                {
                    if (currentW + spaceW + w <= maxW) { lineEnd = wordEnd; currentW += spaceW + w; }
                    else { lines.Add(textMem.Slice(lineStart, lineEnd - lineStart)); lineStart = wordStart; lineEnd = wordEnd; currentW = w; }
                }
                pos = (next == -1) ? span.Length : wordEnd + 1;
            }
            if (lineStart != -1) lines.Add(textMem.Slice(lineStart, lineEnd - lineStart));
            if (lines.Count == 0) lines.Add(ReadOnlyMemory<char>.Empty);
            return lines;
        }

        var mem = md.AsMemory();
        string prevType = "start";
        int idx = 0;
        while (idx <= mem.Length)
        {
            int nl = idx >= mem.Length ? -1 : mem.Span.Slice(idx).IndexOf('\n');
            ReadOnlyMemory<char> raw = nl == -1 ? mem.Slice(idx) : mem.Slice(idx, nl);
            idx = (nl == -1) ? mem.Length + 1 : idx + nl + 1;
            if (!raw.IsEmpty && raw.Span[^1] == '\r') raw = raw.Slice(0, raw.Length - 1);
            var lineTrim = TrimEnd(raw);
            var tstart = TrimStart(lineTrim);

            if (!tstart.IsEmpty && tstart.Span[0] == '#')
            {
                int lvl = 0; while (lvl < tstart.Length && tstart.Span[lvl] == '#') lvl++;
                if (lvl >= 1 && lvl <= 3 && (lvl == tstart.Length || char.IsWhiteSpace(tstart.Span[lvl])))
                {
                    var headerText = TrimStart(tstart.Slice(Math.Min(lvl, tstart.Length)));
                    int li = Math.Min(lvl, 3) - 1;
                    double size = (new double[] { 22, 16, 13 })[li];
                    double before = (prevType == "start") ? 0 : (new double[] { 14, 12, 10 })[li];
                    var wrapped = Wrap(headerText, size, textW);
                    for (int i = 0; i < wrapped.Count; i++) items.Add(new MarkdownItem(ReadOnlyMemory<char>.Empty, wrapped[i], size, 0, i == 0 ? before : 0, 4, Color: "#111111"));
                    prevType = "header"; continue;
                }
            }

            if (!tstart.IsEmpty && (tstart.Span[0] == '-' || tstart.Span[0] == '*') && tstart.Length > 1 && char.IsWhiteSpace(tstart.Span[1]))
            {
                var content = TrimStart(tstart.Slice(2));
                var wrapped = Wrap(content, bodySize, textW - 18);
                var p0 = "- ".AsMemory(); var p1 = "  ".AsMemory();
                for (int i = 0; i < wrapped.Count; i++) items.Add(new MarkdownItem(i == 0 ? p0 : p1, wrapped[i], bodySize, 12, 0, 2));
                prevType = "list"; continue;
            }

            if (!tstart.IsEmpty && char.IsDigit(tstart.Span[0]))
            {
                int p = 0; while (p < tstart.Length && char.IsDigit(tstart.Span[p])) p++;
                if (p > 0 && p + 1 < tstart.Length && tstart.Span[p] == '.' && char.IsWhiteSpace(tstart.Span[p + 1]))
                {
                    var numPref = tstart.Slice(0, p + 2);
                    var content = TrimStart(tstart.Slice(p + 2));
                    var wrapped = Wrap(content, bodySize, textW - 18);
                    var space3 = "   ".AsMemory();
                    for (int i = 0; i < wrapped.Count; i++) items.Add(new MarkdownItem(i == 0 ? numPref : space3, wrapped[i], bodySize, 12, 0, 2));
                    prevType = "list"; continue;
                }
            }

            if (!lineTrim.IsEmpty)
            {
                var t = TrimStart(lineTrim);
                if (t.Length >= 3)
                {
                    char c0 = t.Span[0];
                    if (c0 == '-' || c0 == '*' || c0 == '_')
                    {
                        bool allSame = true; for (int i = 1; i < t.Length; i++) if (t.Span[i] != c0) { allSame = false; break; }
                        if (allSame) { items.Add(new MarkdownItem(ReadOnlyMemory<char>.Empty, ReadOnlyMemory<char>.Empty, bodySize, 0, 8, 8, Rule: true)); prevType = "rule"; continue; }
                    }
                }
            }

            if (lineTrim.IsEmpty)
            {
                if (prevType != "start" && prevType != "blank") items.Add(new MarkdownItem(ReadOnlyMemory<char>.Empty, ReadOnlyMemory<char>.Empty, bodySize, 0, 0, 4));
                prevType = "blank"; continue;
            }

            var wrappedPara = Wrap(lineTrim, bodySize, textW);
            for (int i = 0; i < wrappedPara.Count; i++) items.Add(new MarkdownItem(ReadOnlyMemory<char>.Empty, wrappedPara[i], bodySize, 0, 0, 4, Color: "#111111"));
            prevType = "para";
        }

        var pages = new List<(List<MarkdownItem> Items, List<double> Ys)>();
        double currentY = H - M;
        var pgItems = new List<MarkdownItem>();
        var pgYs = new List<double>();

        foreach (var item in items)
        {
            double needed = item.SpaceBefore + item.Size + item.SpaceAfter;
            if (currentY - needed < M)
            {
                pages.Add((pgItems, pgYs)); pgItems = new List<MarkdownItem>(); pgYs = new List<double>(); currentY = H - M;
            }
            currentY -= item.SpaceBefore; pgYs.Add(currentY); pgItems.Add(item); currentY -= item.Size + item.SpaceAfter;
        }
        if (pgItems.Count > 0) pages.Add((pgItems, pgYs));

        foreach (var pageData in pages)
        {
            builder.Page(W, H, ctx =>
            {
                for (int i = 0; i < pageData.Items.Count; i++)
                {
                    var it = pageData.Items[i]; double py = pageData.Ys[i];
                    if (it.Rule) ctx.Line(M, py, W - M, py, "#e0e0e0", 0.5);
                    else if (!it.Text.IsEmpty) ctx.Text(it.Prefix, it.Text, M + it.Indent, py, it.Size, new TextOptions(Color: it.Color));
                }
            });
        }

        return builder.Build();
    }
}
