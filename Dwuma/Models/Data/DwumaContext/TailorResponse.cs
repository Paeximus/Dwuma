using System;
using System.Collections.Generic;


public class TailorResponse
{
    public string TailoredCv { get; set; } = string.Empty;

    public int AtsScore { get; set; }

    public string AtsSummary { get; set; } = string.Empty;

    public List<string> MatchedKeywords { get; set; } = new();

    public List<string> MissingKeywords { get; set; } = new();

    public List<ChangeLogItem> Changelog { get; set; } = new();
}

public class ChangeLogItem
{
    public string Type { get; set; } = string.Empty;

    public string Section { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}