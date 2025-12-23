using TinyPdf;

var doc = TinyPdf.TinyPdf.Create();

doc.Page(612, 792, (p) => {
    double margin = 40, pw = 532;

    // Header
    p.Rect(margin, 716, pw, 36, "#2563eb");
    p.Text("INVOICE", 55, 726, 24, new TinyPdf.TinyPdf.TextOptions(Color: "#fff", Font: TinyPdf.TinyPdf.PdfFont.Times));
    p.Text("#INV-2025-001", 472, 728, 12, new TinyPdf.TinyPdf.TextOptions(Color: "#fff"));

    // Company & billing info
    p.Text("Acme Corporation", margin, 670, 16);
    p.Text("123 Business Street", margin, 652, 11, new TinyPdf.TinyPdf.TextOptions(Color: "#666"));
    p.Text("New York, NY 10001", margin, 638, 11, new TinyPdf.TinyPdf.TextOptions(Color: "#666"));

    p.Text("Bill To:", 340, 670, 12, new TinyPdf.TinyPdf.TextOptions(Color: "#666"));
    p.Text("John Smith", 340, 652, 14);
    p.Text("456 Customer Ave", 340, 636, 11, new TinyPdf.TinyPdf.TextOptions(Color: "#666"));
    p.Text("Los Angeles, CA 90001", 340, 622, 11, new TinyPdf.TinyPdf.TextOptions(Color: "#666"));

    // Table
    p.Rect(margin, 560, pw, 25, "#f3f4f6");
    p.Text("Description", 50, 568, 11);
    p.Text("Qty", 310, 568, 11);
    p.Text("Price", 380, 568, 11);
    p.Text("Total", 480, 568, 11);

    var items = new[] {
        new[] {"Website Development", "1", "$5,000.00", "$5,000.00"},
        new[] {"Hosting (Annual)", "1", "$200.00", "$200.00"},
        new[] {"Maintenance Package", "12", "$150.00", "$1,800.00"},
    };

    double y = 535;
    foreach (var item in items) {
        string desc = item[0], qty = item[1], price = item[2], total = item[3];
        p.Text(desc, 50, y, 11);
        p.Text(qty, 310, y, 11);
        p.Text(price, 380, y, 11);
        p.Text(total, 480, y, 11);
        p.Line(margin, y - 15, margin + pw, y - 15, "#e5e7eb", 0.5);
        y -= 30;
    }

    // Totals
    p.Line(margin, y, margin + pw, y, "#000", 1);
    p.Text("Subtotal:", 380, y - 25, 11);
    p.Text("$7,000.00", 480, y - 25, 11);
    p.Text("Tax (8%):", 380, y - 45, 11);
    p.Text("$560.00", 480, y - 45, 11);
    p.Rect(370, y - 75, 202, 25, "#2563eb");
    p.Text("Total Due:", 380, y - 63, 12, new TinyPdf.TinyPdf.TextOptions(Color: "#fff", Font: TinyPdf.TinyPdf.PdfFont.Courier));
    p.Text("$7,560.00", 480, y - 63, 12, new TinyPdf.TinyPdf.TextOptions(Color: "#fff"));

    // Footer
    p.Text("Thank you for your business!", margin, 80, 12, new TinyPdf.TinyPdf.TextOptions(Align: "center", Width: pw, Color: "#666"));
    p.Text("Payment due within 30 days", margin, 62, 10, new TinyPdf.TinyPdf.TextOptions(Align: "center", Width: pw, Color: "#999"));
});

File.WriteAllBytes("invoice.pdf", doc.Build());
Console.WriteLine("invoice.pdf generated.");
