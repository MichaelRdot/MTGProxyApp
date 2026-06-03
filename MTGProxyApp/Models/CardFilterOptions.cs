namespace MTGProxyApp.Models;

public class CardFilterOptions
{
    public string? Language { get; set; }
    public bool HighresOnly { get; set; }
    public Dictionary<string, FilterMode> SetFilter { get; set; } = new();
    public Dictionary<string, FilterMode> ArtistFilter { get; set; } = new();
    public Dictionary<string, FilterMode> FrameFilter { get; set; } = new();
    public Dictionary<string, FilterMode> FinishFilter { get; set; } = new();
    public YearFilterState YearFilter { get; set; } = new();
}
