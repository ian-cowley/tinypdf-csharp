# TinyPdf — Developer Manual

**TinyPdf** is a lightweight, zero-dependency C# library designed for high-performance, low-allocation PDF generation. It features a low-level vector drawing builder API and a high-level Markdown-to-PDF parser.

---

## 1. System Architecture & Memory Management

Unlike traditional PDF engines that pull in massive third-party rendering libraries, TinyPdf is written from scratch in pure C# with a focus on memory-efficient stream processing.

```
       +---------------------------------------------+
       |               TinyPdf Builder               |
       +----------------------+----------------------+
                              |
                     PageContext Builder
                              |
               +--------------+--------------+
               |                             |
     PooledBufferStream             PooledBufferWriter
      (Memory Pool Stream)            (ArrayPool<byte>)
               |                             |
               +--------------+--------------+
                              |
                       [PDF Output Stream]
```

### Memory Pooling Strategy
To avoid garbage collection spikes when rendering large, asset-heavy investment reports (e.g. including charts, logos, and long tables), TinyPdf implements custom memory management:
- **`PooledBufferStream`**: A stream implementation that writes blocks to arrays requested from .NET's `System.Buffers.ArrayPool<byte>`. This avoids large heap allocations and LOH (Large Object Heap) fragmentation.
- **`PooledBufferWriter`**: A helper that formats strings and numbers directly into UTF-8 byte buffers without allocating temporary strings.
- **Flushing**: When `Build()` is called, the pooled buffers are flushed to a single byte array, and the underlying blocks are instantly returned to the shared `ArrayPool`.

---

## 2. API Reference & Interface Specifications

### TinyPdfCreate
The main static entry point.

```csharp
namespace TinyPdf;

public static class TinyPdfCreate
{
    public static Builder Create();
    public static byte[] Markdown(string markdownText, MarkdownOptions? opts = null);
}
```

### IPageContext
Defines drawing actions on a single page coordinate space.

```csharp
public interface IPageContext
{
    void Text(string text, double x, double y, double size, TextOptions? opts = null);
    void Line(double x1, double y1, double x2, double y2, string strokeColor, double lineWidth = 1);
    void Rect(double x, double y, double w, double h, string fillColor);
    void Circle(double cx, double cy, double radius, string? fillColor = null, string? strokeColor = null, double lineWidth = 1);
    void Wedge(double cx, double cy, double radius, double startAngle, double endAngle, string? fillColor = null, string? strokeColor = null, double lineWidth = 1);
    void Image(byte[] jpegBytes, double x, double y, double w, double h);
    void Link(string url, double x, double y, double w, double h, LinkOptions? opts = null);
}
```

* **Coordinates**: Origin `(0,0)` is at the **bottom-left** corner of the page. Units are in points (1/72 inch).
* **Colors**: Color strings are specified as `#RRGGBB` hex tokens.

---

## 3. Core Capabilities & Drawing Guide

### 3.1 Text Layout & Styling
Use `TextOptions` to configure font family, text alignment, box width, and colors.

```csharp
ctx.Text("Investment Prospectus", 50, 720, 24, new TextOptions(
    Align: "center",
    Width: 512,
    Color: "#0284c7",
    Font: PdfFont.HelveticaBold
));
```

Supported fonts (`PdfFont`):
- `PdfFont.Helvetica` / `PdfFont.HelveticaBold` / `PdfFont.HelveticaOblique`
- `PdfFont.Times` / `PdfFont.TimesBold` / `PdfFont.TimesItalic`
- `PdfFont.Courier` / `PdfFont.CourierBold`

### 3.2 Vector Shapes & Charts
TinyPdf supports standard vector lines, circles, and wedges (pie segments), enabling lightweight chart plotting without external chart dependencies.

```csharp
// Draw background grid line
ctx.Line(50, 400, 550, 400, strokeColor: "#e2e8f0", lineWidth: 1);

// Draw circular indicators
ctx.Circle(100, 300, 30, fillColor: "#38bdf8", strokeColor: "#0284c7", lineWidth: 2);

// Draw Pie Chart Wedges (Angles are in degrees, 0 to 360)
ctx.Wedge(300, 300, 60, 0, 120, fillColor: "#10b981");   // Segment 1 (120 deg)
ctx.Wedge(300, 300, 60, 120, 360, fillColor: "#fbbf24"); // Segment 2 (240 deg)
```

### 3.3 Embedding Images & Clickable Links
Images must be JPEG format. Link boxes define clickable coordinates on the page.

```csharp
// Embed Logo
byte[] logoBytes = File.ReadAllBytes("logo.jpg");
ctx.Image(logoBytes, 50, 650, 120, 40);

// Add Clickable Web Reference
ctx.Text("View full data on website", 50, 200, 10, new TextOptions(Color: "#0284c7"));
ctx.Link("https://example.com/listings", 50, 200, 150, 12, new LinkOptions(Underline: "#0284c7"));
```

---

## 4. Markdown-to-PDF Conversion

The high-level Markdown parser automatically layout paragraphs, lists, and headers, maintaining top-to-bottom page flow and adding page breaks as content exceeds margins.

### Usage Example
```csharp
string markdownContent = """
# Portfolio Summary
## Overview
This report contains listings resolved by **Glacier Polaris** and audited by **Glacier Graph**.

### Core Attributes
- **Safety Rating**: High
- **Transit Hops**: 2 hops to Central
- **Vibe**: Cozy Modern

---
*Report compiled automatically by Prospectus Designer.*
""";

byte[] pdfReportBytes = TinyPdfCreate.Markdown(markdownContent, new MarkdownOptions(
    Margin: 54, // 0.75 in margins
    PageWidth: 612,
    PageHeight: 792
));

File.WriteAllBytes("portfolio.pdf", pdfReportBytes);
```

---

## 5. Performance Best Practices

To maximize throughput and minimize memory allocations:
1. **Reuse JPEG buffers**: If you are embedding the same logo image on multiple pages, load the byte array once and pass the same reference to `ctx.Image()` to avoid multiple image object dictionary entries in the PDF resources.
2. **Batch page additions**: The builder compiles objects dynamically. For optimal memory usage, perform all drawings within the `Page(ctx => { ... })` action scope so page stream buffers can be compressed and pooled efficiently.
3. **Turn on compression**: The `builder.Compress` property (default `true`) compresses text and vector stream data using FlateDecode (deflate). Keep this on for production files, and disable it only for local debugging/inspecting raw PDF content.
