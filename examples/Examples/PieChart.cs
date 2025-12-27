using TinyPdf;

internal class PieChart
{
    public static void GeneratePieChart()
    {
        var builder = TinyPdfCreate.Create();

        builder.Page(612, 792, ctx =>
        {
            ctx.Text(ReadOnlyMemory<char>.Empty, "Sales by Region - Q1 2024".AsMemory(), 156, 750, 24);

            double cx = 306;
            double cy = 550;
            double radius = 120;

            var data = new[]
            {
                (Label: "North", Value: 35.0, Color: "#3498db"),
                (Label: "South", Value: 25.0, Color: "#e74c3c"),
                (Label: "East", Value: 20.0, Color: "#2ecc71"),
                (Label: "West", Value: 20.0, Color: "#f39c12")
            };

            double total = 0;
            foreach (var item in data) total += item.Value;

            double currentAngle = 0;
            foreach (var item in data)
            {
                double percentage = item.Value / total;
                double sweepAngle = percentage * 360;
                
                ctx.Wedge(cx, cy, radius, currentAngle, currentAngle + sweepAngle, item.Color, "#ffffff", 2);
                
                double midAngle = (currentAngle + currentAngle + sweepAngle) / 2;
                double labelRadius = radius + 40;
                double labelX = cx + labelRadius * Math.Cos(midAngle * Math.PI / 180);
                double labelY = cy + labelRadius * Math.Sin(midAngle * Math.PI / 180);
                
                string label = $"{item.Label}: {item.Value:F0}%";
                ctx.Text(ReadOnlyMemory<char>.Empty, label.AsMemory(), labelX - 30, labelY - 5, 11);
                
                currentAngle += sweepAngle;
            }

            double legendX = 450;
            double legendY = 700;
            ctx.Circle(legendX, legendY, 8, "#3498db");
            ctx.Text(ReadOnlyMemory<char>.Empty, "North (35%)".AsMemory(), legendX + 15, legendY - 4, 10);
            
            ctx.Circle(legendX, legendY - 20, 8, "#e74c3c");
            ctx.Text(ReadOnlyMemory<char>.Empty, "South (25%)".AsMemory(), legendX + 15, legendY - 24, 10);
            
            ctx.Circle(legendX, legendY - 40, 8, "#2ecc71");
            ctx.Text(ReadOnlyMemory<char>.Empty, "East (20%)".AsMemory(), legendX + 15, legendY - 44, 10);
            
            ctx.Circle(legendX, legendY - 60, 8, "#f39c12");
            ctx.Text(ReadOnlyMemory<char>.Empty, "West (20%)".AsMemory(), legendX + 15, legendY - 64, 10);

            ctx.Text(ReadOnlyMemory<char>.Empty, "Total Sales: $1,000,000".AsMemory(), 240, 150, 14);
        });

        File.WriteAllBytes("pie-chart.pdf", builder.Build());
        Console.WriteLine("Generated: pie-chart.pdf");
    }
}
