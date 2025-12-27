using TinyPdf;

internal class Program
{
    private static void Main(string[] args)
    {
        Invoice.GenerateInvoice();
        Resume.GenerateResume();
        Report.GenerateReport();
        Letter.GenerateLetter();
        Receipt.GenerateReceipt();
        PieChart.GeneratePieChart();
    }
}