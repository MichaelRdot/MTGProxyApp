namespace MTGProxyApp.Models;

public class YearFilterState
{
    public Dictionary<string, FilterMode> Years { get; set; } = new();
    public int? RangeStart { get; set; }
    public int? RangeEnd { get; set; }

    public bool IsActive => Years.Count > 0 || RangeStart.HasValue || RangeEnd.HasValue;
}
