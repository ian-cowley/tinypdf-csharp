# TinyPdf C#
A minimal PDF creation library for .NET 10, ported from the original [tinypdf](https://github.com/Lulzx/tinypdf) by Lulzx.

## Features
- 840 lines of code
- Zero external dependencies
- Text rendering (`Helvetica`, `Times`, `Courier`) with alignment support
- Shapes (Rectangles, Lines)
- JPEG images
- Optional Flate (deflate) compression for PDF streams
- Markdown to PDF conversion
- Multiple pages with custom sizes

## Supported framework
This project targets `.NET 10`.

## Installation
Add the `TinyPdf.csproj` to your solution and reference it from your project. Example using the CLI (from your consuming project directory):

```bash
dotnet add reference ../src/TinyPdf/TinyPdf.csproj
```

There is no published NuGet package — include the project in your solution or build it locally.

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

Recent local benchmark results (machine: .NET 10, DEBUG run). These show Markdown conversion speed for different input sizes; "Default" = no compression, "Compressed" = with Flate compression enabled.

- 100 lines
  - Default: 42.6 µs (mean)
  - Compressed: 62.0 µs (mean)
- 1,000 lines
  - Default: 455.1 µs (mean)
  - Compressed: 616.3 µs (mean)
- 5,000 lines
  - Default: 2.40 ms (mean)
  - Compressed: 3.56 ms (mean)

Allocations grow with input size (the benchmark output includes per-test managed allocations). Note: the benchmarks were executed in a DEBUG build on the local machine; running in RELEASE will generally produce lower times.

## Notes
This C# port preserves the core logic of the original TypeScript library (font width tables and PDF object serialization) so generated PDFs are compatible in structure. The library is intentionally small and dependency-free to keep it easy to embed in small projects.

By default output streams are compressed (FlateDecode) — set `builder.Compress = false` or pass `new TinyPdfCreate.MarkdownOptions(Compress: false)` to `TinyPdfCreate.Markdown` to disable compression.

Security: do not commit secrets or sensitive configuration to this repository. Use `dotnet user-secrets` or environment variables for local development.

