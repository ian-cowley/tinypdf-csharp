namespace TinyPdf;

public partial class TinyPdfCreate
{
    /// <summary>
    /// Provides methods for drawing content on a PDF page.
    /// </summary>
    public interface IPageContext
    {
        /// <summary>
        /// Renders text on the page at the specified coordinates.
        /// </summary>
        /// <param name="prefix">Text to render BEFORE the main string (no space added). Useful for styling parts of a line.</param>
        /// <param name="str">The main text to render.</param>
        /// <param name="x">The x-coordinate of the bottom-left of the text.</param>
        /// <param name="y">The y-coordinate of the bottom-left of the text.</param>
        /// <param name="size">The font size.</param>
        /// <param name="opts">Additional text options such as alignment, width, color, and font.</param>
        /// <example>
        /// <code>
        /// ctx.Text("Hello", "World", 100, 100, 12, new TextOptions(Color: "#FF0000"));
        /// </code>
        /// </example>
        void Text(ReadOnlyMemory<char> prefix, ReadOnlyMemory<char> str, double x, double y, double size, TextOptions? opts = null);

        /// <summary>
        /// Draws a filled rectangle on the page.
        /// </summary>
        /// <param name="x">The x-coordinate of the bottom-left corner.</param>
        /// <param name="y">The y-coordinate of the bottom-left corner.</param>
        /// <param name="w">The width of the rectangle.</param>
        /// <param name="h">The height of the rectangle.</param>
        /// <param name="fill">The fill color in hex format (e.g., "#0000FF").</param>
        void Rect(double x, double y, double w, double h, string fill);

        /// <summary>
        /// Draws a line between two points.
        /// </summary>
        /// <param name="x1">The x-coordinate of the starting point.</param>
        /// <param name="y1">The y-coordinate of the starting point.</param>
        /// <param name="x2">The x-coordinate of the ending point.</param>
        /// <param name="y2">The y-coordinate of the ending point.</param>
        /// <param name="stroke">The stroke color in hex format (e.g., "#000000").</param>
        /// <param name="lineWidth">The width of the line.</param>
        void Line(double x1, double y1, double x2, double y2, string stroke, double lineWidth = 1);

        /// <summary>
        /// Embeds a JPEG image on the page.
        /// </summary>
        /// <param name="jpegBytes">The raw bytes of the JPEG image.</param>
        /// <param name="x">The x-coordinate of the bottom-left corner.</param>
        /// <param name="y">The y-coordinate of the bottom-left corner.</param>
        /// <param name="w">The width to render the image.</param>
        /// <param name="h">The height to render the image.</param>
        void Image(byte[] jpegBytes, double x, double y, double w, double h);

        /// <summary>
        /// Adds a clickable URL link to a rectangular area on the page.
        /// </summary>
        /// <param name="url">The target URL.</param>
        /// <param name="x">The x-coordinate of the bottom-left corner of the link area.</param>
        /// <param name="y">The y-coordinate of the bottom-left corner of the link area.</param>
        /// <param name="w">The width of the link area.</param>
        /// <param name="h">The height of the link area.</param>
        /// <param name="opts">Link options, such as underlining.</param>
        void Link(string url, double x, double y, double w, double h, LinkOptions? opts = null);

        /// <summary>
        /// Draws a circle on the page.
        /// </summary>
        /// <param name="cx">The x-coordinate of the center.</param>
        /// <param name="cy">The y-coordinate of the center.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="fill">The fill color in hex format (e.g., "#FF0000"). If null, the circle is not filled.</param>
        /// <param name="stroke">The stroke color in hex format. If null, the circle is not outlined.</param>
        /// <param name="lineWidth">The width of the outline.</param>
        void Circle(double cx, double cy, double radius, string? fill = null, string? stroke = null, double lineWidth = 1);

        /// <summary>
        /// Draws a wedge (pie slice) on the page.
        /// </summary>
        /// <param name="cx">The x-coordinate of the center.</param>
        /// <param name="cy">The y-coordinate of the center.</param>
        /// <param name="radius">The radius of the wedge.</param>
        /// <param name="startAngle">The starting angle in degrees.</param>
        /// <param name="endAngle">The ending angle in degrees.</param>
        /// <param name="fill">The fill color in hex format. If null, the wedge is not filled.</param>
        /// <param name="stroke">The stroke color in hex format. If null, the wedge is not outlined.</param>
        /// <param name="lineWidth">The width of the outline.</param>
        void Wedge(double cx, double cy, double radius, double startAngle, double endAngle, string? fill = null, string? stroke = null, double lineWidth = 1);
    }
}
