using Microsoft.ML.Data;

namespace CareerAgent.Training;

public sealed class JobInteractionTrainingRow
{
    [LoadColumn(0)]
    public string UserId { get; set; } = string.Empty;

    [LoadColumn(1)]
    public string JobId { get; set; } = string.Empty;

    [LoadColumn(2)]
    public float SkillMatch { get; set; }

    [LoadColumn(3)]
    public float LocationMatch { get; set; }

    [LoadColumn(4)]
    public float ExperienceMatch { get; set; }

    [LoadColumn(5)]
    public float IndustryMatch { get; set; }

    [LoadColumn(6)]
    public float SalaryMatch { get; set; }

    [LoadColumn(7)]
    public float JobAgeDays { get; set; }

    [LoadColumn(8)]
    public float Clicked { get; set; }

    [LoadColumn(9)]
    public float Saved { get; set; }

    [LoadColumn(10)]
    public float Applied { get; set; }

    [LoadColumn(11), ColumnName("Label")]
    public float Rating { get; set; }
}

public sealed class JobPrediction
{
    [ColumnName("Score")]
    public float Score { get; set; }
}