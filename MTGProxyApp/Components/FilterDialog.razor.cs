using Microsoft.AspNetCore.Components;
using MTGProxyApp.Models;
using MTGProxyApp.Services;
using MudBlazor;

namespace MTGProxyApp.Components;

public partial class FilterDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    [Parameter] public required CardFilterOptions Options { get; set; }
    [Inject] private ScryfallService ScryfallService { get; set; } = default!;

    private string? _language;
    private bool _highresOnly;
    private Dictionary<string, FilterMode> _setFilter = new();
    private Dictionary<string, FilterMode> _artistFilter = new();
    private Dictionary<string, FilterMode> _frameFilter = new();
    private Dictionary<string, FilterMode> _finishFilter = new();
    private YearFilterState _yearFilter = new();

    private List<string> _availableSets = [];
    private List<string> _availableArtists = [];

    internal static readonly List<(string Code, string Name)> Languages =
    [
        ("en", "English"),
        ("es", "Spanish"),
        ("fr", "French"),
        ("de", "German"),
        ("it", "Italian"),
        ("pt", "Portuguese"),
        ("ja", "Japanese"),
        ("ko", "Korean"),
        ("ru", "Russian"),
        ("zhs", "Simplified Chinese"),
        ("zht", "Traditional Chinese"),
        ("he", "Hebrew"),
        ("la", "Latin"),
        ("grc", "Ancient Greek"),
        ("ar", "Arabic"),
        ("sa", "Sanskrit"),
        ("ph", "Phyrexian"),
    ];

    internal static readonly List<string> Frames =
        ["1993", "1997", "2003", "2015", "future", "tombstone"];

    internal static readonly List<string> Finishes =
        ["nonfoil", "foil", "etched", "glossy"];

    private static readonly List<string> _emptyList = [];

    protected override void OnInitialized()
    {
        _language = Options.Language;
        _highresOnly = Options.HighresOnly;
        _setFilter = new Dictionary<string, FilterMode>(Options.SetFilter);
        _artistFilter = new Dictionary<string, FilterMode>(Options.ArtistFilter);
        _frameFilter = new Dictionary<string, FilterMode>(Options.FrameFilter);
        _finishFilter = new Dictionary<string, FilterMode>(Options.FinishFilter);
        _yearFilter = new YearFilterState
        {
            Years = new Dictionary<string, FilterMode>(Options.YearFilter.Years),
            RangeStart = Options.YearFilter.RangeStart,
            RangeEnd = Options.YearFilter.RangeEnd
        };

        _availableSets = ScryfallService.AllSets.Keys
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _availableArtists = [.. ScryfallService.AllArtists];
    }

    internal static string? MapBrowserLanguage(string? browserLang)
    {
        if (string.IsNullOrEmpty(browserLang)) return null;

        var lower = browserLang.ToLowerInvariant();
        var primary = lower.Split('-')[0];

        // Chinese needs special handling: zh-CN/zh-SG/zh-Hans → Simplified; zh-TW/zh-HK/zh-Hant → Traditional
        if (primary == "zh")
        {
            if (lower.Contains("hant") || lower is "zh-tw" or "zh-hk" or "zh-mo") return "zht";
            return "zhs";
        }

        return Languages.Any(l => l.Code == primary) ? primary : null;
    }

    internal static string FormatFrameName(string frame) => frame switch
    {
        "1993" => "1993 (Alpha/Beta)",
        "1997" => "1997 (Classic)",
        "2003" => "2003 (Modern)",
        "2015" => "2015 (Current)",
        "future" => "Future Sight",
        "tombstone" => "Tombstone",
        _ => char.ToUpperInvariant(frame[0]) + frame[1..]
    };

    internal static string FormatFinishName(string finish) => finish switch
    {
        "nonfoil" => "Nonfoil",
        "foil" => "Foil",
        "etched" => "Etched Foil",
        "glossy" => "Glossy",
        _ => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(finish)
    };

    private string FormatSetName(string setCode) =>
        ScryfallService.AllSets.TryGetValue(setCode, out var name)
            ? $"{setCode.ToUpperInvariant()} – {name}"
            : setCode.ToUpperInvariant();

    private void OnSetFilterChanged(Dictionary<string, FilterMode> state) => _setFilter = state;
    private void OnArtistFilterChanged(Dictionary<string, FilterMode> state) => _artistFilter = state;
    private void OnFrameFilterChanged(Dictionary<string, FilterMode> state) => _frameFilter = state;
    private void OnFinishFilterChanged(Dictionary<string, FilterMode> state) => _finishFilter = state;
    private void OnYearFilterChanged(YearFilterState state) => _yearFilter = state;

    private void Apply() => MudDialog.Close(DialogResult.Ok(new CardFilterOptions
    {
        Language = _language,
        HighresOnly = _highresOnly,
        SetFilter = _setFilter,
        ArtistFilter = _artistFilter,
        FrameFilter = _frameFilter,
        FinishFilter = _finishFilter,
        YearFilter = _yearFilter
    }));

    private void Cancel() => MudDialog.Cancel();
}
