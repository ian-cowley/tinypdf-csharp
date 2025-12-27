using System;
using System.Collections.Generic;
using System.Text;

namespace TinyPdf;

public static class Invoice
{
    public static byte[] GenerateInvoice(int iteration = 0, bool writeToFile = true)
    {
        var doc = TinyPdfCreate.Create();

        double margin = 40, pw = 532;
        double headerY = 716;
        double headerHeight = 36;
        double rowHeight = 25;
        double footerLimit = 140; // leave space at bottom for totals/footer

        // Prepare items (make many to force pagination)
        var baseItems = new[] {
            new[] {"Website Development", "1", "$5,000.00", "$5,000.00"},
            new[] {"Hosting (Annual)", "1", "$200.00", "$200.00"},
            new[] {"Maintenance Package", "12", "$150.00", "$1,800.00"},
        };

        var items = new List<string[]>();
        // Add a larger list (e.g., repeat base items 20 times)
        for (int r = 0; r < 20; r++)
            foreach (var it in baseItems) items.Add(it);

        int idx = 0;

        int pageNumber = 0;
        while (idx < items.Count)
        {
            bool isFirstPage = pageNumber == 0;
            pageNumber++;

            doc.Page(612, 792, (p) =>
            {
                // Header on every page
                p.Rect(margin, headerY, pw, headerHeight, "#2563eb");
                p.Text("INVOICE", 55, headerY + 10, 24, new TinyPdfCreate.TextOptions(Color: "#fff", Font: TinyPdfCreate.PdfFont.Times));
                p.Text("#INV-2025-001", 472, headerY + 12, 12, new TinyPdfCreate.TextOptions(Color: "#fff"));

                if (iteration > 0)
                {
                    p.Text($"Performance Test Iteration: {iteration}", margin, headerY + headerHeight + 5, 10, new TinyPdfCreate.TextOptions(Color: "#666"));
                }

                if (isFirstPage)
                {
                    // Company & billing info (first page only)
                    p.Text("Acme Corporation", margin, 670, 16);
                    p.Link("https://github.com/ian-cowley/tinypdf-csharp", margin, 670, 150, 16, new TinyPdfCreate.LinkOptions(Underline: "#2563eb"));
                    p.Text("123 Business Street", margin, 652, 11, new TinyPdfCreate.TextOptions(Color: "#666"));
                    p.Text("New York, NY 10001", margin, 638, 11, new TinyPdfCreate.TextOptions(Color: "#666"));

                    p.Text("Bill To:", 340, 670, 12, new TinyPdfCreate.TextOptions(Color: "#666"));
                    p.Text("John Smith", 340, 652, 14);
                    p.Text("456 Customer Ave", 340, 636, 11, new TinyPdfCreate.TextOptions(Color: "#666"));
                    p.Text("Los Angeles, CA 90001", 340, 622, 11, new TinyPdfCreate.TextOptions(Color: "#666"));
                }
                else
                {
                    // Indicate continuation on subsequent pages
                    p.Text("Invoice (continued)", margin, headerY - 10, 10, new TinyPdfCreate.TextOptions(Color: "#fff"));
                }

                // Compute table header position. On continuation pages move the table up to use the freed space.
                double tableHeaderY;
                if (isFirstPage)
                {
                    tableHeaderY = 560; // original position when address block is shown
                }
                else
                {
                    // Move table header up under the header band and use the space where the address would be.
                    // Place it a bit below the bottom of the header band.
                    tableHeaderY = headerY - 36; // slightly below header band
                    // Ensure it's not too close to the top
                    if (tableHeaderY > 680) tableHeaderY = 680;
                    if (tableHeaderY < 600) tableHeaderY = 600;
                }

                double firstItemY = tableHeaderY - 25;

                // Table header (repeat on every page)
                p.Rect(margin, tableHeaderY, pw, 25, "#f3f4f6");
                p.Text("Description", margin + 10, tableHeaderY + 8, 11);
                p.Text("Qty", 300, tableHeaderY + 8, 11, new TinyPdfCreate.TextOptions(Align: "right", Width: 40));
                p.Text("Price", 350, tableHeaderY + 8, 11, new TinyPdfCreate.TextOptions(Align: "right", Width: 90));
                p.Text("Total", 450, tableHeaderY + 8, 11, new TinyPdfCreate.TextOptions(Align: "right", Width: 112));

                double y = firstItemY;
                // Fill rows until we hit footer limit or run out of items
                while (idx < items.Count && y - rowHeight >= footerLimit)
                {
                    var item = items[idx++];
                    string desc = item[0], qty = item[1], price = item[2], total = item[3];
                    p.Text(desc, margin + 10, y, 11);
                    p.Text(qty, 300, y, 11, new TinyPdfCreate.TextOptions(Align: "right", Width: 40));
                    p.Text(price, 350, y, 11, new TinyPdfCreate.TextOptions(Align: "right", Width: 90));
                    p.Text(total, 450, y, 11, new TinyPdfCreate.TextOptions(Align: "right", Width: 112));
                    p.Line(margin, y - 10, margin + pw, y - 10, "#e5e7eb", 0.5);
                    y -= rowHeight;
                }

                // If this is the last page (no more items), render totals and footer
                if (idx >= items.Count)
                {
                    // Totals block placed below last item
                    p.Line(margin, y + 5, margin + pw, y + 5, "#000", 1);
                    p.Text("Subtotal:", 350, y - 15, 11);
                    p.Text("$7,000.00", 450, y - 15, 11, new TinyPdfCreate.TextOptions(Align: "right", Width: 112));
                    p.Text("Tax (8%):", 350, y - 35, 11);
                    p.Text("$560.00", 450, y - 35, 11, new TinyPdfCreate.TextOptions(Align: "right", Width: 112));
                    p.Rect(370, y - 65, pw + margin - 370, 25, "#2563eb");
                    p.Text("Total Due:", 380, y - 53, 12, new TinyPdfCreate.TextOptions(Color: "#fff", Font: TinyPdfCreate.PdfFont.Courier));
                    p.Text("$7,560.00", 450, y - 53, 12, new TinyPdfCreate.TextOptions(Color: "#fff", Align: "right", Width: 112));

                    // Footer
                    p.Text("Thank you for your business!", margin, 80, 12, new TinyPdfCreate.TextOptions(Align: "center", Width: pw, Color: "#666"));
                    p.Text("Payment due within 30 days", margin, 62, 10, new TinyPdfCreate.TextOptions(Align: "center", Width: pw, Color: "#999"));
                }
                else
                {
                    // On non-final pages, show a small footer note
                    p.Text("Continued on next page", margin, 80, 10, new TinyPdfCreate.TextOptions(Align: "center", Width: pw, Color: "#999"));
                }
            });
        }

        var pdf = doc.Build();
        if (writeToFile)
        {
            File.WriteAllBytes("invoice.pdf", pdf);
            Console.WriteLine("invoice.pdf generated.");
        }
        return pdf;
    }
}
