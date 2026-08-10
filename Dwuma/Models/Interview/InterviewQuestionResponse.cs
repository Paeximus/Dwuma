namespace Dwuma.Models.Interview;

public sealed class InterviewQuestionResponse
{
    public int SessionId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public List<InterviewQuestionItem> Questions { get; set; } = [];
}

public sealed class InterviewQuestionItem
{
    public int Id { get; set; }

    public int Number { get; set; }

    public string Question { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Difficulty { get; set; } = string.Empty;

    public string WhatInterviewerLooksFor { get; set; } = string.Empty;
}
