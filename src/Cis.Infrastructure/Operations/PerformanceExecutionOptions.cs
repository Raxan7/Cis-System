namespace Cis.Infrastructure.Operations;

public sealed class PerformanceExecutionOptions
{
    public const string SectionName = "Performance";

    public int BackgroundQueueCapacity { get; init; } = 256;

    public int HeavyImportLineThreshold { get; init; } = 1000;

    public int HeavyReconciliationLineThreshold { get; init; } = 500;

    public int HeavyNavInstrumentThreshold { get; init; } = 100;

    public int HeavyReportOutputThreshold { get; init; } = 2;

    public string[] HeavyReportCodePrefixes { get; init; } = ["REG-", "MGT-", "NAV-"];
}
