using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

[MemoryDiagnoser]
public class Benchmarks
{
    private string largeText = string.Empty;

    [Params(100, 1000, 5000)]
    public int LinesCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < LinesCount; i++) sb.AppendLine("Line " + i);
        largeText = sb.ToString();
    }

    [Benchmark]
    public byte[] MarkdownDefault()
    {
        return TinyPdf.TinyPdfCreate.Markdown(largeText, new TinyPdf.TinyPdfCreate.MarkdownOptions(Compress: false));
    }

    [Benchmark]
    public byte[] MarkdownCompressed()
    {
        return TinyPdf.TinyPdfCreate.Markdown(largeText, new TinyPdf.TinyPdfCreate.MarkdownOptions(Compress: true));
    }
}
