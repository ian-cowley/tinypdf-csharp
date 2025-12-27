namespace TinyPdf;

public partial class TinyPdfCreate
{
    private record MarkdownItem(ReadOnlyMemory<char> Prefix, ReadOnlyMemory<char> Text, double Size, double Indent, double SpaceBefore, double SpaceAfter, bool Rule = false, string Color = "#111111");
    public record MarkdownOptions(double? Width = 612, double? Height = 792, double? Margin = 72, bool Compress = true);
}
