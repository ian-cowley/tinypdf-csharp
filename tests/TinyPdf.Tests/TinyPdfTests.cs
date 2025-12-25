using System;
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
        double width = TinyPdfCreate.MeasureText("中", 12);
        Assert.Equal(556 * 12 / 1000.0, width, 3);
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
        Assert.Contains("(- ", content);
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
}
