using System.ComponentModel.DataAnnotations;

namespace Dwuma.Models.Jobs;

public sealed class GhanaJobSearchRequest
{
    [StringLength(120)]
    public string Keywords { get; set; } = string.Empty;

    [StringLength(120)]
    public string Location { get; set; } = "Ghana";

    [Range(1, 1000)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;

    public bool CompanySearch { get; set; } = false;
}