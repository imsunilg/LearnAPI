namespace LearnAPI.Application.DTOs;

/// <summary>Standard list parameters: search + filters + sort + paging.</summary>
public sealed class ListQuery
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }

    /// <summary>Any other query-string key (e.g. ?kycStatus=Verified) becomes an equality filter.</summary>
    public Dictionary<string, string> Filters { get; set; } = new();
}
