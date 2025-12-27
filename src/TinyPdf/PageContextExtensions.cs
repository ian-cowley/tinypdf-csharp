namespace TinyPdf;

public static class PageContextExtensions
{
    public static void Text(this TinyPdfCreate.IPageContext ctx, string text, double x, double y, double size, TinyPdfCreate.TextOptions? opts = null)
    {
        ctx.Text(ReadOnlyMemory<char>.Empty, text.AsMemory(), x, y, size, opts);
    }

    public static void Text(this TinyPdfCreate.IPageContext ctx, string prefix, string text, double x, double y, double size, TinyPdfCreate.TextOptions? opts = null)
    {
        ctx.Text(prefix.AsMemory(), text.AsMemory(), x, y, size, opts);
    }
}
