using System;
using System.Buffers.Binary;
using System.Text;
using Xunit;
using TinyPdf;

namespace TinyPdf.Tests;

public class TinyPdfTests
{
    [Fact]
    public void TestMeasureText()
    {
        double width = TinyPdfCreate.MeasureText("Hello World", 12);
        Assert.Equal(62.004, width, 3);
    }

    [Fact]
    public void TestMeasureText_EmptyString()
    {
        Assert.Equal(0, TinyPdfCreate.MeasureText("", 12));
    }

    [Fact]
    public void TestMeasureText_NonAscii()
    {
        // CJK Ideographs have fullwidth em advance (1000 units)
        double cjkWidth = TinyPdfCreate.MeasureText("中", 12);
        Assert.Equal(1000 * 12 / 1000.0, cjkWidth, 3);

        // WinAnsi Typographic Em-dash has 1000 units
        double emDashWidth = TinyPdfCreate.MeasureText("—", 12);
        Assert.Equal(1000 * 12 / 1000.0, emDashWidth, 3);

        // WinAnsi Euro symbol has 556 units
        double euroWidth = TinyPdfCreate.MeasureText("€", 12);
        Assert.Equal(556 * 12 / 1000.0, euroWidth, 3);

        // WinAnsi Curly quotation mark has 333 units
        double quoteWidth = TinyPdfCreate.MeasureText("“", 12);
        Assert.Equal(333 * 12 / 1000.0, quoteWidth, 3);
    }

    [Fact]
    public void TestMinimalPdf()
    {
        var builder = TinyPdfCreate.Create();
        builder.Compress = false;
        builder.Page(ctx => {
            ctx.Text(ReadOnlyMemory<char>.Empty, "Hello World".AsMemory(), 50, 700, 12);
        });
        byte[] pdf = builder.Build();

        string content = Encoding.Latin1.GetString(pdf);
        Assert.StartsWith("%PDF-1.4", content);
        Assert.Contains("xref", content);
        Assert.Contains("trailer", content);
        Assert.Contains("%%EOF", content);
        Assert.Contains("/MediaBox [0 0 612 792]", content);
    }

    [Fact]
    public void TestCustomPageSize()
    {
        var builder = TinyPdfCreate.Create();
        builder.Compress = false;
        builder.Page(400, 600, ctx => {});
        byte[] pdf = builder.Build();
        string content = Encoding.Latin1.GetString(pdf);
        Assert.Contains("/MediaBox [0 0 400 600]", content);
    }

    [Fact]
    public void TestTextAlignment()
    {
        var builder = TinyPdfCreate.Create();
        builder.Compress = false;
        builder.Page(ctx => {
            ctx.Text(ReadOnlyMemory<char>.Empty, "Hi".AsMemory(), 50, 700, 12, new TinyPdfCreate.TextOptions(Align: "center", Width: 100));
        });
        byte[] pdf = builder.Build();
        string content = Encoding.Latin1.GetString(pdf);
        Assert.Contains("94.34 700.00 Td", content);
    }

    [Fact]
    public void TestTextEscaping()
    {
        var builder = TinyPdfCreate.Create();
        builder.Compress = false;
        builder.Page(ctx => {
            ctx.Text(ReadOnlyMemory<char>.Empty, "Hello (world) \\".AsMemory(), 50, 700, 12);
        });
        byte[] pdf = builder.Build();
        string content = Encoding.Latin1.GetString(pdf);
        Assert.Contains("(Hello \\(world\\) \\\\) Tj", content);
    }

    [Fact]
    public void TestColors()
    {
        var builder = TinyPdfCreate.Create();
        builder.Compress = false;
        builder.Page(ctx => {
            ctx.Rect(0, 0, 100, 100, "#aabbcc");
        });
        byte[] pdf = builder.Build();
        string content = Encoding.Latin1.GetString(pdf);
        Assert.Contains("0.667 0.733 0.800 rg", content);
    }

    [Fact]
    public void TestMarkdown_Header()
    {
        string md = "# Header";
        byte[] pdf = TinyPdfCreate.Markdown(md, new TinyPdfCreate.MarkdownOptions(Compress: false));
        string content = Encoding.Latin1.GetString(pdf);
        Assert.Contains("Header", content);
        Assert.Contains("/F1 22.00 Tf", content);
    }

    [Fact]
    public void TestMarkdown_List()
    {
        string md = "- Item 1";
        byte[] pdf = TinyPdfCreate.Markdown(md, new TinyPdfCreate.MarkdownOptions(Compress: false));
        string content = Encoding.Latin1.GetString(pdf);
        // The renderer now emits the list bullet and the item text in separate text operators,
        // so assert presence of the item text and the bullet prefix rather than the combined string.
        Assert.Contains("Item 1", content);
        Assert.Contains("(\x95 ", content);
    }

    [Fact]
    public void TestMarkdown_Rule()
    {
        string md = "---";
        byte[] pdf = TinyPdfCreate.Markdown(md, new TinyPdfCreate.MarkdownOptions(Compress: false));
        string content = Encoding.Latin1.GetString(pdf);
        Assert.Contains("S", content); 
    }

    [Fact]
    public void TestMarkdownPagination()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 100; i++) sb.AppendLine($"Line {i}");
        byte[] pdf = TinyPdfCreate.Markdown(sb.ToString(), new TinyPdfCreate.MarkdownOptions(Compress: false));
        string content = Encoding.Latin1.GetString(pdf);
        int pageCount = System.Text.RegularExpressions.Regex.Matches(content, "/Type /Page").Count;
        Assert.True(pageCount > 1);
    }

    [Fact]
    public void TestImageHeaderParsing()
    {
        byte[] jpeg = new byte[20];
        jpeg[0] = 0xFF; jpeg[1] = 0xD8;
        jpeg[2] = 0xFF; jpeg[3] = 0xC0;
        jpeg[4] = 0x00; jpeg[5] = 0x11;
        jpeg[6] = 0x08;
        jpeg[7] = 0x01; jpeg[8] = 0x2C;
        jpeg[9] = 0x00; jpeg[10] = 0xC8;

        var builder = TinyPdfCreate.Create();
        builder.Compress = false;
        builder.Page(ctx => {
            ctx.Image(jpeg, 50, 500, 100, 150);
        });
        byte[] pdf = builder.Build();
        
        string content = Encoding.UTF8.GetString(pdf);
        Assert.Contains("/Width 200", content);
        Assert.Contains("/Height 300", content);
    }
    [Fact]
    public void TestMultiFontRendering()
    {
        var builder = TinyPdfCreate.Create();
        builder.Compress = false;
        builder.Page(ctx => {
            ctx.Text(ReadOnlyMemory<char>.Empty, "Helvetica".AsMemory(), 50, 700, 12, new TinyPdfCreate.TextOptions(Font: TinyPdfCreate.PdfFont.Helvetica));
            ctx.Text(ReadOnlyMemory<char>.Empty, "Times".AsMemory(), 50, 650, 12, new TinyPdfCreate.TextOptions(Font: TinyPdfCreate.PdfFont.Times));
            ctx.Text(ReadOnlyMemory<char>.Empty, "Courier".AsMemory(), 50, 600, 12, new TinyPdfCreate.TextOptions(Font: TinyPdfCreate.PdfFont.Courier));
        });
        byte[] pdf = builder.Build();
        string content = Encoding.Latin1.GetString(pdf);

        Assert.Contains("/F1 12.00 Tf", content);
        Assert.Contains("/F2 12.00 Tf", content);
        Assert.Contains("/F3 12.00 Tf", content);
        Assert.Contains("/BaseFont /Times-Roman", content);
        Assert.Contains("/BaseFont /Courier", content);
    }

    [Fact]
    public void TestStreamCompression()
    {
        var builder = TinyPdfCreate.Create();
        builder.Compress = true;
        builder.Page(ctx => {
            ctx.Text(ReadOnlyMemory<char>.Empty, ("This should be compressed " + new string('x', 100)).AsMemory(), 50, 700, 12);
        });
        byte[] compressedPdf = builder.Build();
        string compressedContent = Encoding.Latin1.GetString(compressedPdf);

        builder.Compress = false;
        byte[] uncompressedPdf = builder.Build();

        Assert.Contains("/Filter /FlateDecode", compressedContent);
        Assert.True(compressedPdf.Length < uncompressedPdf.Length, "Compressed PDF should be smaller than uncompressed PDF");
    }
    [Fact]
    public void TestClickableLink()
    {
        var builder = TinyPdfCreate.Create();
        builder.Compress = false;
        builder.Page(ctx => {
            ctx.Text(ReadOnlyMemory<char>.Empty, "Click here".AsMemory(), 50, 700, 12);
            ctx.Link("https://github.com", 50, 700, 50, 12, new TinyPdfCreate.LinkOptions(Underline: "#0000ff"));
        });
        byte[] pdf = builder.Build();
        string content = Encoding.Latin1.GetString(pdf);

        Assert.Contains("/Type /Annot", content);
        Assert.Contains("/Subtype /Link", content);
        Assert.Contains("/URI (https://github.com)", content);
        Assert.Contains("/Rect [50 700 100 712]", content);
        Assert.Contains("0.000 0.000 1.000 RG", content); // Underline color
    }

    [Fact]
    public void TestPooledBufferWriter_GetMemory_DirectWrite_ZeroDefensiveCopy()
    {
        using var writer = new TinyPdfCreate.PooledBufferWriter();

        // 1. GetMemory and write directly into it
        var mem = writer.GetMemory(128);
        Assert.True(mem.Length >= 128);

        byte[] expected = [1, 2, 3, 4, 5, 6, 7, 8];
        expected.CopyTo(mem.Span);
        writer.Advance(expected.Length);

        Assert.Equal(8, writer.WrittenCount);

        // 2. Stream into memory stream and verify identical content
        using var ms = new MemoryStream();
        writer.CopyTo(ms);

        byte[] actual = ms.ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestPooledBufferWriter_GetMemory_LargeAllocationRespectsSizeHint()
    {
        using var writer = new TinyPdfCreate.PooledBufferWriter();

        // Request 16384 bytes, exceeding the default 8192 block size
        var mem = writer.GetMemory(16384);
        Assert.True(mem.Length >= 16384);

        mem.Span.Slice(0, 100).Fill(0xAA);
        writer.Advance(100);

        Assert.Equal(100, writer.WrittenCount);

        using var ms = new MemoryStream();
        writer.CopyTo(ms);
        Assert.Equal(100, ms.Length);
        Assert.Equal(0xAA, ms.ToArray()[50]);
    }

    [Fact]
    public void TestMarkdown_BoldAndItalic_AuthenticType1Fonts()
    {
        string md = "Normal **Bold Text** and _Italic Text_";
        byte[] pdf = TinyPdfCreate.Markdown(md, new TinyPdfCreate.MarkdownOptions(Compress: false));
        string content = Encoding.Latin1.GetString(pdf);

        // Verify Standard 14 Type 1 font dictionaries are registered
        Assert.Contains("/BaseFont /Helvetica-Bold", content);
        Assert.Contains("/BaseFont /Helvetica-Oblique", content);

        // Verify bold uses font tag /F4 and authentic size (11.00 Tf, not inflated 1.05x like 11.55)
        Assert.Contains("/F4 11.00 Tf", content);
        // Verify italic uses font tag /F5 and authentic size (11.00 Tf, without #111111 color override)
        Assert.Contains("/F5 11.00 Tf", content);
    }

    [Fact]
    public void TestTrueTypeFontMetricsParser_ParsesSfntTables()
    {
        // Construct a minimal valid SFNT binary font with head, hhea, hmtx, cmap tables
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // SFNT Header (12 bytes)
        bw.Write(new byte[] { 0x00, 0x01, 0x00, 0x00 }); // Version 1.0
        bw.Write((byte)0x00); bw.Write((byte)0x04);       // numTables = 4
        bw.Write((byte)0x00); bw.Write((byte)0x40);       // searchRange = 64
        bw.Write((byte)0x00); bw.Write((byte)0x02);       // entrySelector = 2
        bw.Write((byte)0x00); bw.Write((byte)0x20);       // rangeShift = 32

        int tableDirOffset = 12;
        int tableDataOffset = tableDirOffset + (4 * 16); // 12 + 64 = 76

        // Table 1: head (54 bytes)
        byte[] headData = new byte[54];
        BinaryPrimitives.WriteUInt16BigEndian(headData.AsSpan(18, 2), 1000); // unitsPerEm = 1000 at OpenType spec offset 18
        BinaryPrimitives.WriteInt16BigEndian(headData.AsSpan(36, 2), -1361);  // xMin = -1361 at offset 36 (distinct from unitsPerEm)

        // Table 2: hhea (36 bytes)
        byte[] hheaData = new byte[36];
        BinaryPrimitives.WriteInt16BigEndian(hheaData.AsSpan(4, 2), 800);   // ascender = 800
        BinaryPrimitives.WriteInt16BigEndian(hheaData.AsSpan(6, 2), -200);  // descender = -200
        BinaryPrimitives.WriteUInt16BigEndian(hheaData.AsSpan(34, 2), 2);   // numberOfHMetrics = 2

        // Table 3: hmtx (8 bytes for 2 metrics: advance=600, lsb=0, advance=750, lsb=0)
        byte[] hmtxData = new byte[8];
        BinaryPrimitives.WriteUInt16BigEndian(hmtxData.AsSpan(0, 2), 600); // Glyph 0 (.notdef) advance = 600
        BinaryPrimitives.WriteUInt16BigEndian(hmtxData.AsSpan(4, 2), 750); // Glyph 1 advance = 750

        // Table 4: cmap Format 4 (subtable with segCount=2)
        // cmap index: version=0, numTables=1 -> platform=0, encoding=3, offset=12
        byte[] cmapData = new byte[44];
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(0, 2), 0);  // version = 0
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(2, 2), 1);  // numTables = 1
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(4, 2), 0);  // platformId = 0 (Unicode)
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(6, 2), 3);  // encodingId = 3
        BinaryPrimitives.WriteInt32BigEndian(cmapData.AsSpan(8, 4), 12);  // subtable offset = 12

        // Format 4 header at offset 12
        int f4 = 12;
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4, 2), 4);      // format = 4
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 2, 2), 32); // length = 32
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 4, 2), 0);  // language = 0
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 6, 2), 4);  // segCountX2 = 4 (2 segments)
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 8, 2), 4);  // searchRange
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 10, 2), 1); // entrySelector
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 12, 2), 0); // rangeShift
        // endCode: segment 0 = 0x41 ('A'), segment 1 = 0xFFFF
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 14, 2), 0x41);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 16, 2), 0xFFFF);
        // reservedPad
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 18, 2), 0);
        // startCode: segment 0 = 0x41 ('A'), segment 1 = 0xFFFF
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 20, 2), 0x41);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 22, 2), 0xFFFF);
        // idDelta: segment 0 = (1 - 0x41) = -64 = 0xFFC0 (maps 'A' to glyph 1), segment 1 = 1
        BinaryPrimitives.WriteInt16BigEndian(cmapData.AsSpan(f4 + 24, 2), unchecked((short)(1 - 0x41)));
        BinaryPrimitives.WriteInt16BigEndian(cmapData.AsSpan(f4 + 26, 2), 1);
        // idRangeOffset: 0, 0
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 28, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 30, 2), 0);

        // Write Directory
        void WriteDirEntry(string tag, int offset, int length)
        {
            var tagBytes = Encoding.ASCII.GetBytes(tag);
            bw.Write(tagBytes);
            bw.Write(0); // checksum dummy
            bw.Write(BinaryPrimitives.ReverseEndianness(offset));
            bw.Write(BinaryPrimitives.ReverseEndianness(length));
        }

        int curOff = tableDataOffset;
        WriteDirEntry("cmap", curOff, cmapData.Length); curOff += cmapData.Length;
        WriteDirEntry("head", curOff, headData.Length); curOff += headData.Length;
        WriteDirEntry("hhea", curOff, hheaData.Length); curOff += hheaData.Length;
        WriteDirEntry("hmtx", curOff, hmtxData.Length); curOff += hmtxData.Length;

        // Write Data
        bw.Write(cmapData);
        bw.Write(headData);
        bw.Write(hheaData);
        bw.Write(hmtxData);
        bw.Flush();

        byte[] fontBytes = ms.ToArray();
        var parser = new TrueTypeFontMetricsParser(fontBytes);

        Assert.Equal(1000, parser.UnitsPerEm);
        Assert.Equal(800, parser.Ascender);
        Assert.Equal(-200, parser.Descender);
        Assert.Equal(2, parser.NumberOfHMetrics);

        // Verify glyph mapping: 'A' maps to glyph 1
        int glyphA = parser.GetGlyphIndex('A');
        Assert.Equal(1, glyphA);
        Assert.Equal(750, parser.GetAdvanceWidth(glyphA));
        Assert.Equal(750, parser.GetPdfWidth('A'));

        // Measure text with parser
        double measuredWidth = parser.MeasureText("A", 12);
        Assert.Equal(750 * 12 / 1000.0, measuredWidth, 3);
    }

    [Fact]
    public void TestTrueTypeFontMetricsParser_HeadTableUnitsPerEmVsXMinDistinction()
    {
        // Explicitly tests that UnitsPerEm is read from OpenType specification offset 18
        // and NOT from offset 36 (which stores xMin).
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // SFNT Header (12 bytes)
        bw.Write(new byte[] { 0x00, 0x01, 0x00, 0x00 }); // Version 1.0
        bw.Write((byte)0x00); bw.Write((byte)0x04);       // numTables = 4
        bw.Write((byte)0x00); bw.Write((byte)0x40);       // searchRange = 64
        bw.Write((byte)0x00); bw.Write((byte)0x02);       // entrySelector = 2
        bw.Write((byte)0x00); bw.Write((byte)0x20);       // rangeShift = 32

        int tableDirOffset = 12;
        int tableDataOffset = tableDirOffset + (4 * 16); // 76

        // Table 1: head (54 bytes)
        byte[] headData = new byte[54];
        // OpenType spec offset 18: unitsPerEm = 2048
        BinaryPrimitives.WriteUInt16BigEndian(headData.AsSpan(18, 2), 2048);
        // OpenType spec offset 36: xMin = -1361 (0xFAAF = 64175 if misinterpreted as unsigned uint16)
        BinaryPrimitives.WriteInt16BigEndian(headData.AsSpan(36, 2), -1361);

        // Table 2: hhea (36 bytes)
        byte[] hheaData = new byte[36];
        BinaryPrimitives.WriteInt16BigEndian(hheaData.AsSpan(4, 2), 1854);   // ascender
        BinaryPrimitives.WriteInt16BigEndian(hheaData.AsSpan(6, 2), -434);   // descender
        BinaryPrimitives.WriteUInt16BigEndian(hheaData.AsSpan(34, 2), 2);    // numberOfHMetrics = 2

        // Table 3: hmtx (8 bytes for 2 metrics)
        byte[] hmtxData = new byte[8];
        BinaryPrimitives.WriteUInt16BigEndian(hmtxData.AsSpan(0, 2), 1024); // Glyph 0 (.notdef) advance = 1024
        BinaryPrimitives.WriteUInt16BigEndian(hmtxData.AsSpan(4, 2), 2048); // Glyph 1 ('A') advance = 2048 (full em)

        // Table 4: cmap Format 4 (mapping 'A' to glyph 1)
        byte[] cmapData = new byte[44];
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(0, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(2, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(4, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(6, 2), 3);
        BinaryPrimitives.WriteInt32BigEndian(cmapData.AsSpan(8, 4), 12);

        int f4 = 12;
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4, 2), 4);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 2, 2), 32);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 4, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 6, 2), 4);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 8, 2), 4);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 10, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 12, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 14, 2), 0x41);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 16, 2), 0xFFFF);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 18, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 20, 2), 0x41);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 22, 2), 0xFFFF);
        BinaryPrimitives.WriteInt16BigEndian(cmapData.AsSpan(f4 + 24, 2), unchecked((short)(1 - 0x41)));
        BinaryPrimitives.WriteInt16BigEndian(cmapData.AsSpan(f4 + 26, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 28, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(cmapData.AsSpan(f4 + 30, 2), 0);

        void WriteDirEntry(string tag, int offset, int length)
        {
            var tagBytes = Encoding.ASCII.GetBytes(tag);
            bw.Write(tagBytes);
            bw.Write(0);
            bw.Write(BinaryPrimitives.ReverseEndianness(offset));
            bw.Write(BinaryPrimitives.ReverseEndianness(length));
        }

        int curOff = tableDataOffset;
        WriteDirEntry("cmap", curOff, cmapData.Length); curOff += cmapData.Length;
        WriteDirEntry("head", curOff, headData.Length); curOff += headData.Length;
        WriteDirEntry("hhea", curOff, hheaData.Length); curOff += hheaData.Length;
        WriteDirEntry("hmtx", curOff, hmtxData.Length); curOff += hmtxData.Length;

        bw.Write(cmapData);
        bw.Write(headData);
        bw.Write(hheaData);
        bw.Write(hmtxData);
        bw.Flush();

        byte[] fontBytes = ms.ToArray();
        var parser = new TrueTypeFontMetricsParser(fontBytes);

        // Verification: UnitsPerEm MUST be 2048 from offset 18, NOT 64175 (0xFAAF) from offset 36
        Assert.Equal(2048, parser.UnitsPerEm);
        Assert.NotEqual(64175, parser.UnitsPerEm);

        // With UnitsPerEm=2048 and glyph advance 2048, standard PDF width (scaled to 1000) must be 1000
        int pdfWidth = parser.GetPdfWidth('A');
        Assert.Equal(1000, pdfWidth);

        // If UnitsPerEm were incorrectly 64175, GetPdfWidth would be (2048 * 1000) / 64175 = 32 (collapsed by 31.3x)
        Assert.NotEqual(32, pdfWidth);

        // Text measurement at 12pt: 1000 * 12 / 1000 = 12.0 pt
        double measured = parser.MeasureText("A", 12.0);
        Assert.Equal(12.0, measured, 3);
    }

    [Fact]
    public void TestTrueTypeFontMetricsParser_AuthenticFont_ArialIfAvailable()
    {
        string arialPath = @"C:\Windows\Fonts\arial.ttf";
        if (!File.Exists(arialPath))
            return; // Skip on environments where arial.ttf is not installed

        byte[] fontBytes = File.ReadAllBytes(arialPath);
        var parser = new TrueTypeFontMetricsParser(fontBytes);

        // Arial is authored with 2048 unitsPerEm; offset 36 contains xMin (-1361 = 64175)
        Assert.Equal(2048, parser.UnitsPerEm);
        Assert.True(parser.Ascender > 0, $"Expected Ascender > 0, got {parser.Ascender}");
        Assert.True(parser.Descender < 0, $"Expected Descender < 0, got {parser.Descender}");
        Assert.True(parser.NumberOfHMetrics > 0, $"Expected NumberOfHMetrics > 0, got {parser.NumberOfHMetrics}");

        int glyphA = parser.GetGlyphIndex('A');
        int glyphSpace = parser.GetGlyphIndex(' ');
        Assert.True(glyphA > 0, "Expected glyph index for 'A' to be > 0");
        Assert.True(glyphSpace > 0, "Expected glyph index for ' ' to be > 0");

        ushort advA = parser.GetAdvanceWidth(glyphA);
        ushort advSpace = parser.GetAdvanceWidth(glyphSpace);
        Assert.True(advA > advSpace, $"Expected glyph 'A' advance ({advA}) to be wider than space ({advSpace})");

        // Advance scaled to PDF 1000pt
        int pdfWidthA = parser.GetPdfWidth('A');
        Assert.True(pdfWidthA > 500 && pdfWidthA < 1000, $"Expected PDF width for 'A' in [500, 1000], got {pdfWidthA}");

        // Text measurement at 12pt
        double widthHello = parser.MeasureText("Hello World", 12.0);
        Assert.True(widthHello > 40.0 && widthHello < 100.0, $"Expected width of 'Hello World' at 12pt to be ~60-70pt, got {widthHello}");

        // Emoji surrogate pair handling
        string emojiString = "Hello \uD83D\uDE00 World";
        double widthWithEmoji = parser.MeasureText(emojiString, 12.0);
        Assert.True(widthWithEmoji > widthHello, $"Expected text with emoji ({widthWithEmoji}) to be wider than without ({widthHello})");
    }
}
