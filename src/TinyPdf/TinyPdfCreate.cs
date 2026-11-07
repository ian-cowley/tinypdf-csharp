using System.Buffers;
using System.Globalization;
using System.Text;

namespace TinyPdf;

/// <summary>
/// The main entry point for creating PDF documents using TinyPdf.
/// </summary>
public partial class TinyPdfCreate
{
    /// <summary>
    /// Starts the creation of a new PDF document.
    /// </summary>
    /// <returns>A <see cref="Builder"/> instance to configure and build the PDF.</returns>
    public static Builder Create() => new Builder();

    /// <summary>
    /// Options for rendering text.
    /// </summary>
    /// <param name="Align">Text alignment: "left", "center", or "right".</param>
    /// <param name="Width">The optional maximum width for text wrapping.</param>
    /// <param name="Color">The text color in hex format (e.g., "#000000").</param>
    /// <param name="Font">The font to use.</param>
    public record TextOptions(string? Align = "left", double? Width = null, string Color = "#000000", PdfFont Font = PdfFont.Helvetica);

    /// <summary>
    /// Options for clickable links.
    /// </summary>
    /// <param name="Underline">Whether to underline the link. If not null, an underline will be drawn with the specified hex color.</param>
    public record LinkOptions(string? Underline = null);

    /// <summary>
    /// Measures the width of a text string in the specified font and size.
    /// </summary>
    /// <param name="str">The text to measure.</param>
    /// <param name="size">The font size.</param>
    /// <param name="font">The font to use.</param>
    /// <returns>The width of the text in PDF points.</returns>
    public static double MeasureText(string str, double size, PdfFont font = PdfFont.Helvetica)
    {
        if (string.IsNullOrEmpty(str)) return 0;
        double width = 0;
        foreach (char c in str)
        {
            width += AfmMetrics.GetGlyphWidth(c, font);
        }
        return (width * size) / 1000.0;
    }

    // memory/span-based measure
    private static double MeasureText(ReadOnlySpan<char> span, double size, PdfFont font = PdfFont.Helvetica)
    {
        if (span.IsEmpty) return 0;
        double width = 0;
        for (int i = 0; i < span.Length; i++)
        {
            width += AfmMetrics.GetGlyphWidth(span[i], font);
        }
        return (width * size) / 1000.0;
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

    private static void WriteWinAnsiEscaped(IBufferWriter<byte> w, ReadOnlySpan<char> s)
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
            else
            {
                int winAnsi = AfmMetrics.UnicodeToWinAnsi(c);
                if (winAnsi >= 0)
                {
                    var span = w.GetSpan(1);
                    span[0] = (byte)winAnsi;
                    w.Advance(1);
                    i++;
                }
                else
                {
                    switch (c)
                    {
                        case '\u2212': BufferWriteAscii(w, "-"); i++; break;
                        case '\u2192': BufferWriteAscii(w, "->"); i++; break;
                        case '\u2190': BufferWriteAscii(w, "<-"); i++; break;
                        case '\u2194': BufferWriteAscii(w, "<->"); i++; break;
                        case '\u2265': BufferWriteAscii(w, ">="); i++; break;
                        case '\u2264': BufferWriteAscii(w, "<="); i++; break;
                        case '\u2260': BufferWriteAscii(w, "!="); i++; break;
                        case '\u2248': BufferWriteAscii(w, "~="); i++; break;
                        default:
                            if (char.IsHighSurrogate(c) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
                            {
                                i += 2;
                            }
                            else
                            {
                                i++;
                            }
                            var fallbackSpan = w.GetSpan(1);
                            fallbackSpan[0] = (byte)'?';
                            w.Advance(1);
                            break;
                    }
                }
            }
        }
    }

    private static void BufferWritePdfString(IBufferWriter<byte> w, ReadOnlySpan<char> prefix, ReadOnlySpan<char> content)
    {
        BufferWriteAscii(w, "(");
        if (!prefix.IsEmpty) WriteWinAnsiEscaped(w, prefix);
        if (!content.IsEmpty) WriteWinAnsiEscaped(w, content);
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
        if (val == null) { BufferWriteAscii(writer, "null"); return; }
        if (val is bool bb) { BufferWriteAscii(writer, bb ? "true" : "false"); return; }
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
            if (s.StartsWith('/')) { BufferWriteUtf8(writer, s); return; }
            if (s.StartsWith('(')) { BufferWriteUtf8(writer, s); return; }
            BufferWritePdfString(writer, ReadOnlySpan<char>.Empty, s.AsSpan());
            return;
        }
        if (val is Ref r)
        {
            var span = writer.GetSpan(16);
            if (System.Buffers.Text.Utf8Formatter.TryFormat(r.Id, span, out int bytesWritten)) writer.Advance(bytesWritten);
            BufferWriteAscii(writer, " 0 R");
            return;
        }
        if (val is IDictionary<string, object> dict)
        {
            BufferWriteAscii(writer, "<<\n");
            foreach (var kvp in dict)
            {
                BufferWriteAscii(writer, "/"); BufferWriteAscii(writer, kvp.Key); BufferWriteAscii(writer, " ");
                SerializeToWriter(writer, kvp.Value);
                BufferWriteAscii(writer, "\n");
            }
            BufferWriteAscii(writer, ">>");
            return;
        }
        if (val is System.Collections.IEnumerable enumerable && val is not string)
        {
            BufferWriteAscii(writer, "[");
            bool first = true;
            foreach (var item in enumerable)
            {
                if (!first) BufferWriteAscii(writer, " ");
                SerializeToWriter(writer, item);
                first = false;
            }
            BufferWriteAscii(writer, "]");
            return;
        }

        var str = val.ToString() ?? "null";
        BufferWriteUtf8(writer, str);
    }

    /// <summary>
    /// Generates a PDF from a Markdown string.
    /// </summary>
    /// <param name="md">The Markdown source string.</param>
    /// <param name="opts">Options for the Markdown rendering, such as page size and margins.</param>
    /// <returns>The raw bytes of the generated PDF document.</returns>
    /// <example>
    /// <code>
    /// string md = "# Hello World\nThis is a PDF generated from **Markdown**.";
    /// byte[] pdfBytes = TinyPdfCreate.Markdown(md);
    /// File.WriteAllBytes("output.pdf", pdfBytes);
    /// </code>
    /// </example>
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
                var p0 = "\u2022 ".AsMemory(); var p1 = "  ".AsMemory();
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
                    else if (!it.Text.IsEmpty)
                    {
                        // Inline markdown rendering: support **bold**, _italic_, `code`
                        double x = M + it.Indent;
                        // Render prefix first (list bullets / numbers)
                        if (!it.Prefix.IsEmpty)
                        {
                            var pref = it.Prefix.Span;
                            string prefStr = new string(pref);
                            ctx.Text(ReadOnlyMemory<char>.Empty, prefStr.AsMemory(), x, py, it.Size);
                            // advance x by width of prefix
                            x += TinyPdfCreate.MeasureText(prefStr, it.Size);
                        }

                        var span = it.Text.Span;
                        int pos = 0;
                        while (pos < span.Length)
                        {
                            // detect code span
                            if (span[pos] == '`')
                            {
                                int end = pos + 1;
                                while (end < span.Length && span[end] != '`') end++;
                                var run = span.Slice(pos + 1, Math.Max(0, end - pos - 1));
                                string runStr = new string(run);
                                // render in monospaced font
                                ctx.Text(ReadOnlyMemory<char>.Empty, runStr.AsMemory(), x, py, it.Size, new TextOptions(Font: PdfFont.Courier));
                                x += TinyPdfCreate.MeasureText(runStr, it.Size, PdfFont.Courier);
                                pos = Math.Min(end + 1, span.Length);
                                continue;
                            }

                            // detect bold **
                            if (pos + 1 < span.Length && span[pos] == '*' && span[pos + 1] == '*')
                            {
                                int end = pos + 2;
                                while (end + 1 < span.Length && !(span[end] == '*' && span[end + 1] == '*')) end++;
                                int len = Math.Max(0, end - (pos + 2));
                                var run = span.Slice(pos + 2, len);
                                string runStr = new string(run);
                                ctx.Text(ReadOnlyMemory<char>.Empty, runStr.AsMemory(), x, py, it.Size, new TextOptions(Font: PdfFont.HelveticaBold, Color: it.Color));
                                x += TinyPdfCreate.MeasureText(runStr, it.Size, PdfFont.HelveticaBold);
                                pos = (end + 2 <= span.Length) ? end + 2 : span.Length;
                                continue;
                            }

                            // detect italic _
                            if (span[pos] == '_')
                            {
                                int end = pos + 1;
                                while (end < span.Length && span[end] != '_') end++;
                                var run = span.Slice(pos + 1, Math.Max(0, end - pos - 1));
                                string runStr = new string(run);
                                ctx.Text(ReadOnlyMemory<char>.Empty, runStr.AsMemory(), x, py, it.Size, new TextOptions(Font: PdfFont.HelveticaOblique, Color: it.Color));
                                x += TinyPdfCreate.MeasureText(runStr, it.Size, PdfFont.HelveticaOblique);
                                pos = Math.Min(end + 1, span.Length);
                                continue;
                            }

                            // normal text run until next special char
                            int j = pos;
                            while (j < span.Length && span[j] != '`' && !(j + 1 < span.Length && span[j] == '*' && span[j + 1] == '*') && span[j] != '_') j++;
                            var normal = span.Slice(pos, j - pos);
                            string normalStr = new string(normal);
                            ctx.Text(ReadOnlyMemory<char>.Empty, normalStr.AsMemory(), x, py, it.Size, new TextOptions(Color: it.Color, Font: PdfFont.Helvetica));
                            x += TinyPdfCreate.MeasureText(normalStr, it.Size);
                            pos = j;
                        }
                    }
                }
            });
        }

        return builder.Build();
    }
}
