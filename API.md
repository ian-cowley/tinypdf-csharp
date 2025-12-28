# TinyPdf C# API Documentation

TinyPdf is a lightweight, dependency-free C# library for generating PDF documents. It provides both a low-level builder API for precise layout control and a high-level Markdown-to-PDF converter.

## Table of Contents

- [Getting Started](#getting-started)
- [PDF Initialization](#pdf-initialization)
- [Page Management](#page-management)
- [Drawing Content (IPageContext)](#drawing-content-ipagecontext)
    - [Text Rendering](#text-rendering)
    - [Shapes and Lines](#shapes-and-lines)
    - [Images](#images)
    - [Clickable Links](#clickable-links)
- [Markdown Rendering](#markdown-rendering)
- [Coordinate System](#coordinate-system)

---

## Getting Started

The simplest way to create a PDF is using the `TinyPdfCreate` static methods.

```csharp
using TinyPdf;

// Low-level builder API
var builder = TinyPdfCreate.Create();
builder.Page(ctx => {
    ctx.Text("Hello World!", 100, 700, 24);
});
byte[] pdfBytes = builder.Build();
File.WriteAllBytes("example.pdf", pdfBytes);

// High-level Markdown API
byte[] mdPdfBytes = TinyPdfCreate.Markdown("# Hello\nThis is a PDF from **Markdown**.");
File.WriteAllBytes("markdown.pdf", mdPdfBytes);
```

---

## PDF Initialization

The entry point for the builder API is `TinyPdfCreate.Create()`.

### `TinyPdfCreate.Create()`
Returns a new `Builder` instance.

---

## Page Management

The `Builder` class manages the overall document structure and compression.

### `builder.Compress`
A boolean property to enable or disable PDF object compression (default: `true`).

### `builder.Page(double width, double height, Action<IPageContext> drawFn)`
Adds a new page with custom dimensions.

### `builder.Page(Action<IPageContext> drawFn)`
Adds a new page with standard Letter size (612x792 points).

---

## Drawing Content (IPageContext)

Inside the `Page` action, you interact with the `IPageContext` to draw content. Coordinates start from the **bottom-left** (0,0).

### Text Rendering

#### `ctx.Text(string text, double x, double y, double size, TextOptions? opts = null)`
Renders a string of text.

- **`text`**: The string to display.
- **`x`, `y`**: Coordinates of the bottom-left of the first character.
- **`size`**: Font size in points.
- **`opts`**: Options for alignment, width, color, and font.

**Example: Styled Text**
```csharp
ctx.Text("Centered Title", 0, 750, 20, new TextOptions(
    Align: "center",
    Width: 612, // Page width for centering
    Color: "#0000FF", // Blue
    Font: PdfFont.Times
));
```

---

### Shapes and Lines

#### `ctx.Rect(double x, double y, double w, double h, string fill)`
Draws a filled rectangle.

#### `ctx.Line(double x1, double y1, double x2, double y2, string stroke, double lineWidth = 1)`
Draws a line between two points.

#### `ctx.Circle(double cx, double cy, double radius, string? fill = null, string? stroke = null, double lineWidth = 1)`
Draws a circle.

#### `ctx.Wedge(double cx, double cy, double radius, double startAngle, double endAngle, string? fill = null, string? stroke = null, double lineWidth = 1)`
Draws a pie-slice shape or an arc.

**Example: Drawing a simple Pie Chart**
```csharp
ctx.Wedge(100, 100, 50, 0, 90, fill: "#FF0000"); // Red quadrant
ctx.Wedge(100, 100, 50, 90, 180, fill: "#00FF00"); // Green quadrant
ctx.Wedge(100, 100, 50, 180, 270, fill: "#0000FF"); // Blue quadrant
ctx.Wedge(100, 100, 50, 270, 360, fill: "#FFFF00"); // Yellow quadrant
```

---

### Images

#### `ctx.Image(byte[] jpegBytes, double x, double y, double w, double h)`
Embeds a JPEG image.

**Example: Adding a Logo**
```csharp
byte[] logo = File.ReadAllBytes("logo.jpg");
ctx.Image(logo, 50, 700, 100, 50);
```

---

### Clickable Links

#### `ctx.Link(string url, double x, double y, double w, double h, LinkOptions? opts = null)`
Creates an invisible clickable area that opens the specified URL.

**Example: Link with Underline**
```csharp
string url = "https://github.com/ian-cowley/tinypdf-csharp";
ctx.Text("Visit Project GitHub", 50, 600, 12, new TextOptions(Color: "#0000EE"));
ctx.Link(url, 50, 600, 120, 12, new LinkOptions(Underline: "#0000EE"));
```

---

## Markdown Rendering

TinyPdf provides a high-level API to convert Markdown directly to PDF. It supports headers, lists, bold, italic, code spans, and horizontal rules.

### `TinyPdfCreate.Markdown(string md, MarkdownOptions? opts = null)`

**Supported Elements:**
- `#`, `##`, `###` For headers.
- `-` or `*` for unordered lists.
- `1.` for ordered lists.
- `***` or `---` for horizontal rules.
- `**bold**`, `_italic_`, and `` `code blocks` `` within paragraphs.

**Example:**
```csharp
string md = """
# Resume
## Skills
- **C#** and .NET
- PDF Generation
- Markdown Rendering

## Education
1. University of PDF
2. Graphics High

---
Created with `TinyPdf`
""";

byte[] pdf = TinyPdfCreate.Markdown(md, new MarkdownOptions(Margin: 36));
File.WriteAllBytes("resume.pdf", pdf);
```

---

## Coordinate System

TinyPdf uses the standard PDF coordinate system where the origin **(0,0)** is at the **bottom-left** corner of the page.
- **X** increases to the right.
- **Y** increases upwards.
- Units are in **points** (1/72 of an inch).

Standard Letter size is **612 x 792** points.
A4 size is approximately **595 x 842** points.
