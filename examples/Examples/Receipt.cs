using System;
using System.IO;

namespace TinyPdf;

public static class Receipt
{
    public static byte[] GenerateReceipt(int iteration = 0, bool writeToFile = true)
    {
        var doc = TinyPdfCreate.Create();

        doc.Page(612, 792, (p) =>
        {
            double margin = 50, pw = 512;

            if (iteration > 0)
            {
                p.Text($"Performance Test Iteration: {iteration}", margin, 740, 10, new TinyPdfCreate.TextOptions(Color: "#666"));
            }

            p.Text("RECEIPT", margin, 720, 18, new TinyPdfCreate.TextOptions(Font: TinyPdfCreate.PdfFont.Times));

            p.Text("Order #: ORD-2025-078", margin, 700, 10);
            p.Text("Date: 2025-06-01", margin, 686, 10, new TinyPdfCreate.TextOptions(Color: "#6b7280"));

            p.Rect(margin, 640, pw, 25, "#f3f4f6");
            p.Text("Item", margin + 10, 648, 11);
            p.Text("Qty", 350, 648, 11, new TinyPdfCreate.TextOptions(Align: "right", Width: 60));
            p.Text("Price", 420, 648, 11, new TinyPdfCreate.TextOptions(Align: "right", Width: 120));

            var items = new[] {
                new[] {"Widget A", "2", "$10.00"},
                new[] {"Widget B", "1", "$25.00"},
            };

            double y = 616;
            foreach (var item in items)
            {
                p.Text(item[0], margin + 10, y, 11);
                p.Text(item[1], 350, y, 11, new TinyPdfCreate.TextOptions(Align: "right", Width: 60));
                p.Text(item[2], 420, y, 11, new TinyPdfCreate.TextOptions(Align: "right", Width: 120));
                y -= 24;
            }

            p.Line(margin, y + 6, margin + pw, y + 6, "#000", 1);
            p.Text("Total:", 350, y - 12, 12);
            p.Text("$45.00", 420, y - 12, 12, new TinyPdfCreate.TextOptions(Align: "right", Width: 120));

            p.Text("Thank you for your purchase!", margin, 120, 12, new TinyPdfCreate.TextOptions(Align: "center", Width: pw, Color: "#6b7280"));
        });

        var pdf = doc.Build();
        if (writeToFile)
        {
            File.WriteAllBytes("receipt.pdf", pdf);
            Console.WriteLine("receipt.pdf generated.");
        }
        return pdf;
    }
}
