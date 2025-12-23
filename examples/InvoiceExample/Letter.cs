using System;
using System.IO;
using System.Text;

namespace TinyPdf;

internal static class Letter
{
    public static void GenerateLetter()
    {
        // Build a markdown document so the library's Markdown renderer handles wrapping and pagination
        var sb = new StringBuilder();
        sb.AppendLine("Acme Corporation");
        sb.AppendLine("123 Business Street");
        sb.AppendLine("New York, NY 10001");
        sb.AppendLine();
        sb.AppendLine("June 1, 2025");
        sb.AppendLine();
        sb.AppendLine("John Smith");
        sb.AppendLine("456 Customer Ave");
        sb.AppendLine("Los Angeles, CA 90001");
        sb.AppendLine();
        sb.AppendLine("Dear John Smith:");
        sb.AppendLine();

        // Create a long body to demonstrate wrapping + pagination
        for (int i = 0; i < 20; i++)
        {
            sb.AppendLine("Thank you for your continued partnership. We are writing to inform you of upcoming changes to our service terms. Please review the details and reach out with any questions.");
            sb.AppendLine();
        }

        sb.AppendLine("Sincerely,");
        sb.AppendLine();
        sb.AppendLine("Acme Corporation");

        var md = sb.ToString();

        var opts = new TinyPdf.MarkdownOptions(Width: 468, Height: 792, Margin: 72, Compress: true);
        var pdf = TinyPdf.Markdown(md, opts);

        File.WriteAllBytes("letter.pdf", pdf);
        Console.WriteLine("letter.pdf generated.");
    }
}
