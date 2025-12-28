namespace TinyPdf;

/// <summary>
/// Provides extension methods for <see cref="TinyPdfCreate.IPageContext"/> to simplify common operations.
/// </summary>
public static class PageContextExtensions
{
    /// <summary>
    /// Renders text on the page at the specified coordinates.
    /// </summary>
    /// <param name="ctx">The page context.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="x">The x-coordinate of the bottom-left of the text.</param>
    /// <param name="y">The y-coordinate of the bottom-left of the text.</param>
    /// <param name="size">The font size.</param>
    /// <param name="opts">Additional text options.</param>
    public static void Text(this TinyPdfCreate.IPageContext ctx, string text, double x, double y, double size, TinyPdfCreate.TextOptions? opts = null)
    {
        ctx.Text(ReadOnlyMemory<char>.Empty, text.AsMemory(), x, y, size, opts);
    }

    /// <summary>
    /// Renders text on the page with a prefix at the specified coordinates.
    /// </summary>
    /// <param name="ctx">The page context.</param>
    /// <param name="prefix">Text to render BEFORE the main string (no space added).</param>
    /// <param name="text">The main text to render.</param>
    /// <param name="x">The x-coordinate of the bottom-left of the text.</param>
    /// <param name="y">The y-coordinate of the bottom-left of the text.</param>
    /// <param name="size">The font size.</param>
    /// <param name="opts">Additional text options.</param>
    public static void Text(this TinyPdfCreate.IPageContext ctx, string prefix, string text, double x, double y, double size, TinyPdfCreate.TextOptions? opts = null)
    {
        ctx.Text(prefix.AsMemory(), text.AsMemory(), x, y, size, opts);
    }
}
