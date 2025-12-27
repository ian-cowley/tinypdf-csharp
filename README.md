# TinyPdf C#
[![NuGet version](https://img.shields.io/nuget/v/TinyPdf.svg)](https://www.nuget.org/packages/TinyPdf/)
A minimal PDF creation library for .NET 10, ported from the original [tinypdf](https://github.com/Lulzx/tinypdf) by Lulzx.

## Features
- 1027 lines of code
- Zero external dependencies
- Text rendering (`Helvetica`, `Times`, `Courier`) with alignment support
- Clickable links with optional underlining
- Shapes (Rectangles, Lines, Circles, Wedges)
- JPEG images
- Optional Flate (deflate) compression for PDF streams
- Markdown to PDF conversion
- Multiple pages with custom sizes

## Supported framework
This project targets `.NET 10`.

## Installation
Install the package via the .NET CLI:

```bash
dotnet add package TinyPdf
```

Alternatively, you can add the `TinyPdf.csproj` to your solution and reference it from your project:

```bash
dotnet add reference ../src/TinyPdf/TinyPdf.csproj
```

## Usage

### Simple PDF
```csharp
using TinyPdf;

var builder = TinyPdfCreate.Create();
// disable compression if you need uncompressed streams
// builder.Compress = false;

builder.Page(ctx => {
    ctx.Text("Hello World", 50, 700, 24);
    ctx.Rect(50, 650, 100, 20, "#FF0000");
});

byte[] pdf = builder.Build();
File.WriteAllBytes("output.pdf", pdf);
```

### Markdown to PDF
```csharp
using TinyPdf;

// disable compression via options
var options = new TinyPdfCreate.MarkdownOptions(Compress: false);
string md = "# Header\n\nThis is a paragraph.\n\n- List item";
byte[] pdf = TinyPdfCreate.Markdown(md, options);
File.WriteAllBytes("markdown.pdf", pdf);
```

### Advanced Page Options
```csharp
builder.Page(842, 595, ctx => { // A4 Landscape
    ctx.Line(0, 0, 842, 595, "#0000FF", 2);
});
```

### Charts and Shapes
```csharp
builder.Page(ctx => {
    // ctx.Circle(cx, cy, radius, fill, stroke, lineWidth)
    ctx.Circle(100, 700, 50, "#3498db");

    // ctx.Wedge(cx, cy, radius, startAngle, endAngle, fill, stroke, lineWidth)
    ctx.Wedge(300, 700, 50, 0, 90, "#e74c3c", "#000000", 2);
});
```

### Clickable Links
```csharp
builder.Page(ctx => {
    ctx.Text("Visit our website", 50, 700, 12);
    // Link(url, x, y, width, height, options)
    ctx.Link("https://github.com/ian-cowley/tinypdf-csharp", 50, 700, 100, 12, new TinyPdfCreate.LinkOptions(Underline: "#2563eb"));
});
```

## Running tests
From the repository root run:

```bash
dotnet test
```

## Benchmarks
To run the benchmarks project:

```bash
dotnet run -p benchmarks/TinyPdf.Benchmarks
```

## Publishing (Maintainers)
To release a new version to NuGet and GitHub:
1. Open PowerShell in the repository root.
2. Run: `./release.ps1`
3. Follow the prompts for the commit message.

The script will automatically increment the patch version, commit, push, and create a GitHub tag, which triggers the CI/CD workflow.

## Notes
This C# port preserves the core logic of the original TypeScript library (font width tables and PDF object serialization) so generated PDFs are compatible in structure. The library is intentionally small and dependency-free to keep it easy to embed in small projects.

By default output streams are compressed (FlateDecode) — set `builder.Compress = false` or pass `new TinyPdfCreate.MarkdownOptions(Compress: false)` to `TinyPdfCreate.Markdown` to disable compression.

Security: do not commit secrets or sensitive configuration to this repository. Use `dotnet user-secrets` or environment variables for local development.


