namespace MTGProxyApp.Models;

public class ChosenCardModel
{
    public DeckLineModel Source { get; set; } = default!;

    public string DisplayName => $"{Source.Count} {Source.Name}" +
                                 (Source.SetCode != null ? $" ({Source.SetCode}) {Source.CollectorNumber}" : "");

    public Uri? PngUri { get; set; } // currently chosen art
    public Uri? PrintsSearchUri { get; set; } // for opening art-picker
}