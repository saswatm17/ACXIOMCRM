namespace AcxiomCRM.DTOs
{
    public class StageSummaryDto
    {
        public string StageName { get; set; } = string.Empty;
        public int DealsCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class PipelineReportSummaryDto
    {
        public int TotalDealsCount { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal TotalWeightedValue { get; set; }
        public decimal TotalWonRevenue { get; set; }
        public List<StageSummaryDto> StageBreakdown { get; set; } = new List<StageSummaryDto>();
    }
}
