using System.Buffers;

namespace TinyPdf;

public partial class TinyPdfCreate
{
    public partial class Builder
    {
        private class PageContextImpl : IPageContext
        {
            private readonly Builder _builder;
            private readonly ArrayBufferWriter<byte> _writer;
            private readonly List<(string Name, Ref Ref)> _images;
            private int _imageCount;
            private readonly Action<int> _updateImageCount;
            private readonly List<(string Url, double[] Rect)> _links;

            public PageContextImpl(Builder builder, ArrayBufferWriter<byte> writer, List<(string Name, Ref Ref)> images, int imageCount, Action<int> updateImageCount, List<(string Url, double[] Rect)> links)
            {
                _builder = builder;
                _writer = writer;
                _images = images;
                _imageCount = imageCount;
                _updateImageCount = updateImageCount;
                _links = links;
            }

            public void Text(ReadOnlyMemory<char> prefix, ReadOnlyMemory<char> str, double x, double y, double size, TextOptions? opts = null)
            {
                opts ??= new TextOptions();
                string align = opts.Align ?? "left";

                // If width provided, wrap text into multiple lines
                if (opts.Width.HasValue)
                {
                    double maxW = opts.Width.Value;
                    // Simple word wrap
                    var span = str.Span;
                    var lines = new List<ReadOnlyMemory<char>>();

                    double spaceW = TinyPdfCreate.MeasureText(" ", size);
                    int pos = 0; int lineStart = -1; int lineEnd = -1; double currentW = 0;
                    while (pos < span.Length)
                    {
                        int next = span[pos..].IndexOf(' ');
                        int wordStart = pos; int wordEnd = next == -1 ? span.Length : pos + next;
                        var wordSpan = span.Slice(wordStart, wordEnd - wordStart);
                        double w = TinyPdfCreate.MeasureText(wordSpan, size);

                        if (lineStart == -1)
                        {
                            lineStart = wordStart; lineEnd = wordEnd; currentW = w;
                        }
                        else
                        {
                            if (currentW + spaceW + w <= maxW) { lineEnd = wordEnd; currentW += spaceW + w; }
                            else { lines.Add(str.Slice(lineStart, lineEnd - lineStart)); lineStart = wordStart; lineEnd = wordEnd; currentW = w; }
                        }
                        pos = (next == -1) ? span.Length : wordEnd + 1;
                    }
                    if (lineStart != -1) lines.Add(str.Slice(lineStart, lineEnd - lineStart));
                    if (lines.Count == 0) lines.Add(ReadOnlyMemory<char>.Empty);

                    double lineHeight = size * 1.2; // simple line spacing

                    for (int i = 0; i < lines.Count; i++)
                    {
                        var line = lines[i];
                        string useAlign = align;
                        double tx = x;
                        if (useAlign != "left")
                        {
                            double textWidth;
                            if (i == 0 && !prefix.IsEmpty)
                                textWidth = TinyPdfCreate.MeasureText(prefix.Span, size) + TinyPdfCreate.MeasureText(line.Span, size);
                            else
                                textWidth = TinyPdfCreate.MeasureText(line.Span, size);

                            if (useAlign == "center") tx = x + (opts.Width.Value - textWidth) / 2;
                            if (useAlign == "right") tx = x + opts.Width.Value - textWidth;
                        }

                        var rgb = ParseColor(opts.Color);
                        if (rgb != null)
                        {
                            BufferWriteDouble(_writer, rgb[0], 3); BufferWriteAscii(_writer, " ");
                            BufferWriteDouble(_writer, rgb[1], 3); BufferWriteAscii(_writer, " ");
                            BufferWriteDouble(_writer, rgb[2], 3); BufferWriteAscii(_writer, " rg\n");
                        }

                        BufferWriteAscii(_writer, "BT\n");
                        string fontTag = opts.Font switch { PdfFont.Times => "/F2", PdfFont.Courier => "/F3", _ => "/F1" };
                        BufferWriteAscii(_writer, fontTag); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, size, 2); BufferWriteAscii(_writer, " Tf\n");

                        double py = y - i * lineHeight;
                        BufferWriteDouble(_writer, tx, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, py, 2); BufferWriteAscii(_writer, " Td\n");

                        if (i == 0)
                            BufferWritePdfString(_writer, prefix.Span, line.Span);
                        else
                            BufferWritePdfString(_writer, ReadOnlySpan<char>.Empty, line.Span);

                        BufferWriteAscii(_writer, " Tj\n");
                        BufferWriteAscii(_writer, "ET\n");
                    }

                    return;
                }

                double tx_default = x;
                if (align != "left" && opts.Width.HasValue)
                {
                    double textWidth = TinyPdfCreate.MeasureText(str.Span, size);
                    if (align == "center") tx_default = x + (opts.Width.Value - textWidth) / 2;
                    if (align == "right") tx_default = x + opts.Width.Value - textWidth;
                }

                var rgbDefault = ParseColor(opts.Color);
                if (rgbDefault != null)
                {
                    BufferWriteDouble(_writer, rgbDefault[0], 3); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, rgbDefault[1], 3); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, rgbDefault[2], 3); BufferWriteAscii(_writer, " rg\n");
                }

                BufferWriteAscii(_writer, "BT\n");
                string fontTagDefault = opts.Font switch { PdfFont.Times => "/F2", PdfFont.Courier => "/F3", _ => "/F1" };
                BufferWriteAscii(_writer, fontTagDefault); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, size, 2); BufferWriteAscii(_writer, " Tf\n");

                BufferWriteDouble(_writer, tx_default, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, y, 2); BufferWriteAscii(_writer, " Td\n");

                BufferWritePdfString(_writer, prefix.Span, str.Span);
                BufferWriteAscii(_writer, " Tj\n");
                BufferWriteAscii(_writer, "ET\n");
            }

            public void Rect(double x, double y, double w, double h, string fill)
            {
                var rgb = ParseColor(fill);
                if (rgb != null)
                {
                    BufferWriteDouble(_writer, rgb[0], 3); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, rgb[1], 3); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, rgb[2], 3); BufferWriteAscii(_writer, " rg\n");

                    BufferWriteDouble(_writer, x, 2); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, y, 2); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, w, 2); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, h, 2); BufferWriteAscii(_writer, " re\n");
                    BufferWriteAscii(_writer, "f\n");
                }
            }

            public void Line(double x1, double y1, double x2, double y2, string stroke, double lineWidth = 1)
            {
                var rgb = ParseColor(stroke);
                if (rgb != null)
                {
                    BufferWriteDouble(_writer, lineWidth, 2); BufferWriteAscii(_writer, " w\n");
                    BufferWriteDouble(_writer, rgb[0], 3); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, rgb[1], 3); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, rgb[2], 3); BufferWriteAscii(_writer, " RG\n");

                    BufferWriteDouble(_writer, x1, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, y1, 2); BufferWriteAscii(_writer, " m\n");
                    BufferWriteDouble(_writer, x2, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, y2, 2); BufferWriteAscii(_writer, " l\n");
                    BufferWriteAscii(_writer, "S\n");
                }
            }

            public void Image(byte[] jpegBytes, double x, double y, double w, double h)
            {
                int imgWidth = 0, imgHeight = 0;
                for (int i = 0; i < jpegBytes.Length - 1; i++)
                {
                    if (jpegBytes[i] == 0xFF && (jpegBytes[i+1] == 0xC0 || jpegBytes[i+1] == 0xC2))
                    {
                        imgHeight = (jpegBytes[i + 5] << 8) | jpegBytes[i + 6];
                        imgWidth = (jpegBytes[i + 7] << 8) | jpegBytes[i + 8];
                        break;
                    }
                }

                string imgName = $"/Im{_imageCount++}";
                _updateImageCount(_imageCount);

                var imgRef = _builder.AddObject(new Dictionary<string, object>
                {
                    ["Type"] = "/XObject",
                    ["Subtype"] = "/Image",
                    ["Width"] = imgWidth,
                    ["Height"] = imgHeight,
                    ["ColorSpace"] = "/DeviceRGB",
                    ["BitsPerComponent"] = 8,
                    ["Filter"] = "/DCTDecode",
                    ["Length"] = jpegBytes.Length
                }, jpegBytes);

                _images.Add((imgName, imgRef));

                BufferWriteAscii(_writer, "q\n");
                BufferWriteDouble(_writer, w, 2); BufferWriteAscii(_writer, " 0 0 "); BufferWriteDouble(_writer, h, 2); BufferWriteAscii(_writer, " ");
                BufferWriteDouble(_writer, x, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, y, 2); BufferWriteAscii(_writer, " cm\n");
                BufferWriteAscii(_writer, imgName); BufferWriteAscii(_writer, " Do\n"); BufferWriteAscii(_writer, "Q\n");
            }

            public void Link(string url, double x, double y, double w, double h, LinkOptions? opts = null)
            {
                _links.Add((url, new double[] { x, y, x + w, y + h }));
                if (opts?.Underline != null)
                {
                    var rgb = ParseColor(opts.Underline);
                    if (rgb != null)
                    {
                        BufferWriteAscii(_writer, "0.75 w\n");
                        BufferWriteDouble(_writer, rgb[0], 3); BufferWriteAscii(_writer, " ");
                        BufferWriteDouble(_writer, rgb[1], 3); BufferWriteAscii(_writer, " ");
                        BufferWriteDouble(_writer, rgb[2], 3); BufferWriteAscii(_writer, " RG\n");
                        BufferWriteDouble(_writer, x, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, y + 2, 2); BufferWriteAscii(_writer, " m\n");
                        BufferWriteDouble(_writer, x + w, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, y + 2, 2); BufferWriteAscii(_writer, " l\n");
                        BufferWriteAscii(_writer, "S\n");
                    }
                }
            }

            public void Circle(double cx, double cy, double radius, string? fill = null, string? stroke = null, double lineWidth = 1)
            {
                double k = 0.5522847498;
                double r = radius;
                double k_r = k * r;

                BufferWriteDouble(_writer, cx + r, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cy, 2); BufferWriteAscii(_writer, " m\n");
                BufferWriteDouble(_writer, cx + r, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cy + k_r, 2); BufferWriteAscii(_writer, " ");
                BufferWriteDouble(_writer, cx + k_r, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cy + r, 2); BufferWriteAscii(_writer, " ");
                BufferWriteDouble(_writer, cx, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cy + r, 2); BufferWriteAscii(_writer, " c\n");
                
                BufferWriteDouble(_writer, cx - k_r, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cy + r, 2); BufferWriteAscii(_writer, " ");
                BufferWriteDouble(_writer, cx - r, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cy + k_r, 2); BufferWriteAscii(_writer, " ");
                BufferWriteDouble(_writer, cx - r, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cy, 2); BufferWriteAscii(_writer, " c\n");
                
                BufferWriteDouble(_writer, cx - r, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cy - k_r, 2); BufferWriteAscii(_writer, " ");
                BufferWriteDouble(_writer, cx - k_r, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cy - r, 2); BufferWriteAscii(_writer, " ");
                BufferWriteDouble(_writer, cx, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cy - r, 2); BufferWriteAscii(_writer, " c\n");
                
                BufferWriteDouble(_writer, cx + k_r, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cy - r, 2); BufferWriteAscii(_writer, " ");
                BufferWriteDouble(_writer, cx + r, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cy - k_r, 2); BufferWriteAscii(_writer, " ");
                BufferWriteDouble(_writer, cx + r, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cy, 2); BufferWriteAscii(_writer, " c\n");

                if (fill != null)
                {
                    var rgbFill = ParseColor(fill);
                    if (rgbFill != null)
                    {
                        BufferWriteDouble(_writer, rgbFill[0], 3); BufferWriteAscii(_writer, " ");
                        BufferWriteDouble(_writer, rgbFill[1], 3); BufferWriteAscii(_writer, " ");
                        BufferWriteDouble(_writer, rgbFill[2], 3); BufferWriteAscii(_writer, " rg\n");
                    }
                }

                if (stroke != null)
                {
                    var rgbStroke = ParseColor(stroke);
                    if (rgbStroke != null)
                    {
                        BufferWriteDouble(_writer, lineWidth, 2); BufferWriteAscii(_writer, " w\n");
                        BufferWriteDouble(_writer, rgbStroke[0], 3); BufferWriteAscii(_writer, " ");
                        BufferWriteDouble(_writer, rgbStroke[1], 3); BufferWriteAscii(_writer, " ");
                        BufferWriteDouble(_writer, rgbStroke[2], 3); BufferWriteAscii(_writer, " RG\n");
                    }
                }

                if (fill != null && stroke != null) BufferWriteAscii(_writer, "B\n");
                else if (fill != null) BufferWriteAscii(_writer, "f\n");
                else if (stroke != null) BufferWriteAscii(_writer, "S\n");
            }

            public void Wedge(double cx, double cy, double radius, double startAngle, double endAngle, string? fill = null, string? stroke = null, double lineWidth = 1)
            {
                double k = 0.5522847498;
                
                BufferWriteDouble(_writer, cx, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cy, 2); BufferWriteAscii(_writer, " m\n");
                
                double startRad = startAngle * Math.PI / 180.0;
                double endRad = endAngle * Math.PI / 180.0;
                double startX = cx + radius * Math.Cos(startRad);
                double startY = cy + radius * Math.Sin(startRad);
                
                BufferWriteDouble(_writer, startX, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, startY, 2); BufferWriteAscii(_writer, " l\n");
                
                double sweepAngle = endRad - startRad;
                if (sweepAngle < 0) sweepAngle += 2 * Math.PI;
                
                int segments = (int)Math.Ceiling(Math.Abs(sweepAngle) / (Math.PI / 2));
                double segmentAngle = sweepAngle / segments;
                
                for (int i = 0; i < segments; i++)
                {
                    double a1 = startRad + i * segmentAngle;
                    double a2 = startRad + (i + 1) * segmentAngle;
                    
                    double x1 = cx + radius * Math.Cos(a1);
                    double y1 = cy + radius * Math.Sin(a1);
                    double x2 = cx + radius * Math.Cos(a2);
                    double y2 = cy + radius * Math.Sin(a2);
                    
                    // Standard approximation for a circular arc with Bezier curve:
                    // alpha = 4/3 * tan(theta / 4)
                    double alpha = (4.0 / 3.0) * Math.Tan(segmentAngle / 4.0);
                    
                    double cp1x = x1 - alpha * radius * Math.Sin(a1);
                    double cp1y = y1 + alpha * radius * Math.Cos(a1);
                    double cp2x = x2 + alpha * radius * Math.Sin(a2);
                    double cp2y = y2 - alpha * radius * Math.Cos(a2);
                    
                    BufferWriteDouble(_writer, cp1x, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cp1y, 2); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, cp2x, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, cp2y, 2); BufferWriteAscii(_writer, " ");
                    BufferWriteDouble(_writer, x2, 2); BufferWriteAscii(_writer, " "); BufferWriteDouble(_writer, y2, 2); BufferWriteAscii(_writer, " c\n");
                }
                
                BufferWriteAscii(_writer, "h\n");

                if (fill != null)
                {
                    var rgbFill = ParseColor(fill);
                    if (rgbFill != null)
                    {
                        BufferWriteDouble(_writer, rgbFill[0], 3); BufferWriteAscii(_writer, " ");
                        BufferWriteDouble(_writer, rgbFill[1], 3); BufferWriteAscii(_writer, " ");
                        BufferWriteDouble(_writer, rgbFill[2], 3); BufferWriteAscii(_writer, " rg\n");
                    }
                }

                if (stroke != null)
                {
                    var rgbStroke = ParseColor(stroke);
                    if (rgbStroke != null)
                    {
                        BufferWriteDouble(_writer, lineWidth, 2); BufferWriteAscii(_writer, " w\n");
                        BufferWriteDouble(_writer, rgbStroke[0], 3); BufferWriteAscii(_writer, " ");
                        BufferWriteDouble(_writer, rgbStroke[1], 3); BufferWriteAscii(_writer, " ");
                        BufferWriteDouble(_writer, rgbStroke[2], 3); BufferWriteAscii(_writer, " RG\n");
                    }
                }

                if (fill != null && stroke != null) BufferWriteAscii(_writer, "B\n");
                else if (fill != null) BufferWriteAscii(_writer, "f\n");
                else if (stroke != null) BufferWriteAscii(_writer, "S\n");
            }
        }
    }
}
