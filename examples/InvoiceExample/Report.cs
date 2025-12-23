using System;
using System.IO;

namespace TinyPdf;

internal static class Report
{
    public static void GenerateReport()
    {
        var doc = TinyPdf.Create();

        doc.Page(612, 792, (p) =>
        {
            double margin = 50, pw = 512;

            p.Text("Quarterly Report", margin, 740, 18, new TinyPdf.TextOptions(Font: TinyPdf.PdfFont.Times));
            p.Text("Q1 2025", margin, 722, 12, new TinyPdf.TextOptions(Color: "#4b5563"));

            p.Line(margin, 710, margin + pw, 710, "#e5e7eb", 1);

            p.Text("Executive Summary", margin, 690, 12, new TinyPdf.TextOptions(Font: TinyPdf.PdfFont.Courier));
            p.Text("This quarter saw strong growth across our core metrics. Revenue increased by 12% year-over-year.", margin, 672, 10, new TinyPdf.TextOptions(Width: pw, Color: "#374151"));

            p.Text("Key Metrics", margin, 640, 12, new TinyPdf.TextOptions(Font: TinyPdf.PdfFont.Courier));

            // Draw a bar chart on the left and a trend line chart on the right
            double chartTop = 600;
            double chartHeight = 180;
            double chartWidth = 240;

            // Bar chart data (visual example) - normalized to 0..1
            string[] barLabels = new[] { "Revenue", "Active Customers", "Churn %" };
            double[] barValues = new[] { 1.0, 0.7, 0.05 }; // normalized sample values for display
            double[] barDisplay = new double[barValues.Length];
            for (int i = 0; i < barValues.Length; i++) barDisplay[i] = Math.Round(barValues[i] * 100); // display scale 0..100

            double barGap = 12;
            double barAreaX = margin;
            double barAreaY = chartTop; // top coordinate
            double barAreaW = chartWidth;
            double barAreaH = chartHeight;

            // Draw bar chart background (from bottom to top)
            p.Rect(barAreaX, barAreaY - barAreaH, barAreaW, barAreaH, "#ffffff");

            // Y axis and ticks
            double axisX = barAreaX + 30;
            double axisBottom = barAreaY - barAreaH + 8;
            double axisTop = barAreaY - 8;
            p.Line(axisX, axisBottom, axisX, axisTop, "#d1d5db", 1);
            // ticks and labels (0 and max)
            double axisMax = 100; // using 0..100 scale for these examples
            p.Line(axisX - 4, axisBottom, axisX + 4, axisBottom, "#d1d5db", 1);
            p.Text("0", axisX - 18, axisBottom - 6, 8, new TinyPdf.TextOptions(Align: "right", Width: 12, Color: "#374151"));
            p.Line(axisX - 4, axisTop, axisX + 4, axisTop, "#d1d5db", 1);
            p.Text(axisMax.ToString("0"), axisX - 18, axisTop - 6, 8, new TinyPdf.TextOptions(Align: "right", Width: 12, Color: "#374151"));

            int bars = barValues.Length;
            double availableW = barAreaW - 40; // leave space for y axis
            double barW = (availableW - (bars - 1) * barGap) / bars;
            double barBase = axisBottom; // baseline for bars

            for (int i = 0; i < bars; i++)
            {
                double x = barAreaX + 30 + i * (barW + barGap);
                double h = Math.Max(2, barValues[i] * (barAreaH - 24));
                double y = barBase; // draw from baseline upward
                string fill = i == 0 ? "#2563eb" : (i == 1 ? "#10b981" : "#f59e0b");
                p.Rect(x, y, barW, h, fill);

                // value text above bar
                p.Text(barDisplay[i].ToString("0"), x, y + h + 6, 9, new TinyPdf.TextOptions(Width: barW, Align: "center", Color: "#374151"));

                // label under bar area
                p.Text(barLabels[i], x, barAreaY + 6, 9, new TinyPdf.TextOptions(Width: barW, Align: "center", Color: "#374151"));
            }

            // Trend line chart on the right
            double lineAreaX = margin + chartWidth + 30;
            double lineAreaY = chartTop;
            double lineAreaW = 220;
            double lineAreaH = chartHeight;

            // Sample monthly revenue trend (normalized)
            double[] trend = new[] { 0.65, 0.72, 0.80, 0.88, 0.95, 1.0 };
            int points = trend.Length;
            double stepX = (lineAreaW - 16) / (points - 1);

            // draw background and axes
            p.Rect(lineAreaX, lineAreaY - lineAreaH, lineAreaW, lineAreaH, "#ffffff");
            double lineAxisLeft = lineAreaX + 8;
            double lineAxisBottom = lineAreaY - 8;
            double lineAxisTop = lineAreaY - lineAreaH + 8;
            p.Line(lineAxisLeft, lineAxisBottom, lineAxisLeft, lineAxisTop, "#e5e7eb", 1);
            p.Line(lineAxisLeft, lineAxisBottom, lineAxisLeft + lineAreaW - 16, lineAxisBottom, "#e5e7eb", 1);

            // y-axis ticks for trend (0..100%)
            p.Text("100", lineAxisLeft - 18, lineAxisTop - 6, 8, new TinyPdf.TextOptions(Align: "right", Width: 12, Color: "#374151"));
            p.Text("0", lineAxisLeft - 18, lineAxisBottom - 6, 8, new TinyPdf.TextOptions(Align: "right", Width: 12, Color: "#374151"));

            double prevX = 0, prevY = 0;
            for (int i = 0; i < points; i++)
            {
                double px = lineAreaX + 8 + i * stepX;
                double py = (lineAreaY - 8) - (trend[i] * (lineAreaH - 24));
                if (i > 0) p.Line(prevX, prevY, px, py, "#2563eb", 1.5);
                // draw small marker as tiny rect
                p.Rect(px - 2, py - 2, 4, 4, "#2563eb");
                prevX = px; prevY = py;
                // x label
                p.Text(("M" + (i + 1)), px - 8, lineAreaY + 6, 8, new TinyPdf.TextOptions(Color: "#374151"));
            }

            // Add a metrics table under charts
            double tableY = chartTop - chartHeight - 30;
            double ty = tableY - 20;
            p.Text("Key Metrics Details", margin, ty, 12, new TinyPdf.TextOptions(Font: TinyPdf.PdfFont.Courier));
            ty -= 18;

            var metrics = new[] {
                ("Revenue", "$1,200,000", "12% YoY"),
                ("Active Customers", "8,450", "+9% QoQ"),
                ("Churn Rate", "1.2%", "-0.3pp"),
            };

            foreach (var m in metrics)
            {
                p.Text(m.Item1, margin, ty, 11);
                p.Text(m.Item2, margin + 220, ty, 11, new TinyPdf.TextOptions(Align: "right", Width: 120));
                p.Text(m.Item3, margin + 360, ty, 11, new TinyPdf.TextOptions(Align: "right", Width: 120));
                ty -= 18;
            }

            // Notes (fill remaining space)
            double notesY = ty - 10;
            p.Text("Notes", margin, notesY, 12, new TinyPdf.TextOptions(Font: TinyPdf.PdfFont.Courier));
            notesY -= 16;

            string longNotes = "Recommendations: Continue investment in core product features, evaluate pricing tiers for enterprise customers, and prioritize internationalization efforts for Q3. Consider A/B testing on onboarding flows to improve activation rates.\n\nRisks: Monitor server capacity and operational costs as user growth continues.";
            p.Text(longNotes, margin, notesY, 10, new TinyPdf.TextOptions(Width: pw, Color: "#6b7280"));
        });

        File.WriteAllBytes("report.pdf", doc.Build());
        Console.WriteLine("report.pdf generated.");
    }
}
