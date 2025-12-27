namespace TinyPdf;

public partial class TinyPdfCreate
{
    public interface IPageContext
    {
        void Text(ReadOnlyMemory<char> prefix, ReadOnlyMemory<char> str, double x, double y, double size, TextOptions? opts = null);
        void Rect(double x, double y, double w, double h, string fill);
        void Line(double x1, double y1, double x2, double y2, string stroke, double lineWidth = 1);
        void Image(byte[] jpegBytes, double x, double y, double w, double h);
        void Link(string url, double x, double y, double w, double h, LinkOptions? opts = null);
        void Circle(double cx, double cy, double radius, string? fill = null, string? stroke = null, double lineWidth = 1);
        void Wedge(double cx, double cy, double radius, double startAngle, double endAngle, string? fill = null, string? stroke = null, double lineWidth = 1);
    }
}
