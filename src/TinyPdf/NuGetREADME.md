# TinyPdf NuGet Package

TinyPdf is a self-contained PDF generation library targeting .NET 10. It exposes a single `TinyPdfCreate.Builder` API for drawing text, rectangles, lines, and embedding JPEG images, along with a convenience `TinyPdfCreate.Markdown` helper.

## Highlights
- Zero external dependencies
- Optional Flate compression via `builder.Compress` (defaults to enabled)
- Markdown rendering that respects page size and pagination
- Three built-in fonts with simple alignment support
- Designed for embedding directly in console, desktop, and server apps

## Getting started
```csharp
using TinyPdf;

var builder = TinyPdfCreate.Create();
builder.Page(ctx => ctx.Text("Hello from NuGet!", 50, 700, 18));
byte[] pdf = builder.Build();
```

Set `builder.Compress = false` or pass `new TinyPdfCreate.MarkdownOptions(Compress: false)` to `TinyPdfCreate.Markdown` if you need uncompressed output.
