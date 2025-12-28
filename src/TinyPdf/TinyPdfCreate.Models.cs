namespace TinyPdf;

public partial class TinyPdfCreate
{
    private record MarkdownItem(ReadOnlyMemory<char> Prefix, ReadOnlyMemory<char> Text, double Size, double Indent, double SpaceBefore, double SpaceAfter, bool Rule = false, string Color = "#111111");
    /// <summary>
    /// Options for the high-level Markdown-to-PDF rendering API.
    /// </summary>
    /// <param name="Width">The page width in points (default: 612).</param>
    /// <param name="Height">The page height in points (default: 792).</param>
    /// <param name="Margin">The page margin in points (default: 72).</param>
    /// <param name="Compress">Whether to compress the output PDF (default: true).</param>
    public record MarkdownOptions(double? Width = 612, double? Height = 792, double? Margin = 72, bool Compress = true);
}
