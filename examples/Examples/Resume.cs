using System;
using System.IO;
using System.Text;

namespace TinyPdf;

internal static class Resume
{
    public static void GenerateResume()
    {
        var sb = new StringBuilder();

        sb.AppendLine("# John Doe");
        sb.AppendLine();
        sb.AppendLine("_Staff Software Engineer - Distributed Systems & Platform_");
        sb.AppendLine();

        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("Technical leader with 12+ years building scalable backend systems, observability platforms, and developer tooling. Experienced in designing microservices, leading cross-functional teams, and shipping high-impact features that drive growth and reliability.");
        sb.AppendLine();

        sb.AppendLine("## Experience");
        sb.AppendLine();

        sb.AppendLine("### Staff Software Engineer — Nimbus Cloud (Remote)");
        sb.AppendLine();
        sb.AppendLine("**Jan 2022 - Present**");
        sb.AppendLine();
        sb.AppendLine("Lead development of the core distributed job scheduler used by 500+ enterprise customers. Shipped autoscaling, multi-tenant isolation, and a new placement algorithm that reduced job latency by 42% and infrastructure costs by 18%.");
        sb.AppendLine();
        sb.AppendLine("**Key achievements:**");
        sb.AppendLine();
        sb.AppendLine("- Designed and rolled out a zero-downtime migration to a sharded control plane serving 10k+ jobs/sec.");
        sb.AppendLine("- Implemented service-level telemetry and alerting; reduced time to detect by 3x.");
        sb.AppendLine("- Mentored a team of 6 engineers; introduced quarterly reviews and an RFC process.");
        sb.AppendLine();

        sb.AppendLine("### Senior Software Engineer — Atlas Payments");
        sb.AppendLine();
        sb.AppendLine("**May 2018 - Dec 2021**");
        sb.AppendLine();
        sb.AppendLine("Built high-throughput payment ingestion pipelines (5k TPS) and an event-driven reconciliation system. Led initiatives on fault-tolerant design and security reviews improving uptime to 99.995%.");
        sb.AppendLine();
        sb.AppendLine("**Key achievements:**");
        sb.AppendLine();
        sb.AppendLine("- Architected a streaming ingestion stack using Kafka and idempotent processors; reduced duplicate processing by 95%.");
        sb.AppendLine("- Created a developer-facing SDK that decreased integration time for partners by 60%.");
        sb.AppendLine();

        sb.AppendLine("### Software Engineer — BrightApps");
        sb.AppendLine();
        sb.AppendLine("**Jun 2014 - Apr 2018**");
        sb.AppendLine();
        sb.AppendLine("Developed core features for the company's SaaS platform including multi-tenant data models, role-based access control, and performance optimizations across SQL and NoSQL stores.");
        sb.AppendLine();

        sb.AppendLine("### Principal Engineer (Contract) — CoreAnalytics");
        sb.AppendLine();
        sb.AppendLine("**2013 - 2014**");
        sb.AppendLine();
        sb.AppendLine("Implemented data ingestion adapters and ETL pipelines to support analytics workloads; optimized large batch jobs to run 4x faster.");
        sb.AppendLine();

        sb.AppendLine("## Technical Skills");
        sb.AppendLine();
        sb.AppendLine("- Languages: C#, F#, Python, TypeScript");
        sb.AppendLine("- Systems: Kubernetes, Docker, Kafka, Redis, PostgreSQL, Cassandra");
        sb.AppendLine("- Practices: Distributed systems, Observability, CI/CD, TDD, DDD");
        sb.AppendLine();

        sb.AppendLine("## Open Source & Contributions");
        sb.AppendLine();
        sb.AppendLine("- Maintainer of `example/scale-runner`: job execution lib used by 300+ projects.");
        sb.AppendLine("- Contributor to several tools; authored a widely-used PDF generation library.");
        sb.AppendLine();

        sb.AppendLine("## Awards & Recognition");
        sb.AppendLine();
        sb.AppendLine("- Engineering Excellence Award, Nimbus Cloud (2023)");
        sb.AppendLine("- Best Infrastructure Project, Atlas Payments (2020)");
        sb.AppendLine();

        sb.AppendLine("## Education & Certifications");
        sb.AppendLine();
        sb.AppendLine("- B.S. Computer Science, University of Example — 2014");
        sb.AppendLine("- Certified Kubernetes Application Developer (CKAD)");
        sb.AppendLine("- AWS Certified Solutions Architect – Associate");
        sb.AppendLine();

        sb.AppendLine("References available upon request.");

        var md = sb.ToString();

        var opts = new TinyPdfCreate.MarkdownOptions(Width: 512, Height: 792, Margin: 50, Compress: true);
        var pdf = TinyPdfCreate.Markdown(md, opts);

        File.WriteAllBytes("resume.pdf", pdf);
        Console.WriteLine("resume.pdf generated.");
    }
}
