using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TinyPdf;

namespace PerformanceTests;

class Program
{
    static async Task Main(string[] args)
    {
        const int iterations = 1000;
        Console.WriteLine($"Starting Performance Tests - {iterations} iterations per example in parallel...");
        Console.WriteLine();

        var results = new ConcurrentBag<TestResult>();

        var tests = new[]
        {
            ("Invoice", (Action<int>)(i => Invoice.GenerateInvoice(i, false)), "Methods: ~320"),
            ("Letter", (Action<int>)(i => Letter.GenerateLetter(i, false)), "Markdown: ~60 lines"),
            ("PieChart", (Action<int>)(i => PieChart.GeneratePieChart(i, false)), "Methods: ~20"),
            ("Receipt", (Action<int>)(i => Receipt.GenerateReceipt(i, false)), "Methods: ~18"),
            ("Report", (Action<int>)(i => Report.GenerateReport(i, false)), "Methods: ~56"),
            ("Resume", (Action<int>)(i => Resume.GenerateResume(i, false)), "Markdown: ~100 lines")
        };

        foreach (var (name, action, complexity) in tests)
        {
            Console.WriteLine($"Testing {name}...");
            var sw = Stopwatch.StartNew();

            Parallel.For(1, iterations + 1, i =>
            {
                action(i);
            });

            sw.Stop();
            var totalMs = sw.Elapsed.TotalMilliseconds;
            results.Add(new TestResult(name, iterations, totalMs, complexity));
        }

        PrintResults(results);
    }

    static void PrintResults(ConcurrentBag<TestResult> results)
    {
        Console.WriteLine("| Example | Iterations | Total Time (ms) | Avg Time (ms) | Complexity |");
        Console.WriteLine("|---------|------------|-----------------|---------------|------------|");

        foreach (var res in results.OrderBy(r => r.Name))
        {
            Console.WriteLine($"| {res.Name} | {res.Iterations} | {res.TotalTimeMs:F2} | {res.AvgTimeMs:F4} | {res.Complexity} |");
        }
    }

    record TestResult(string Name, int Iterations, double TotalTimeMs, string Complexity)
    {
        public double AvgTimeMs => TotalTimeMs / Iterations;
    }
}
