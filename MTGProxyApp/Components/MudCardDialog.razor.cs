using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MTGProxyApp.Dtos;
using MTGProxyApp.Models;
using MudBlazor;

namespace MTGProxyApp.Components;

public partial class MudCardDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    [Parameter] public required CardDto Card { get; set; }
    [Parameter] public CardFilterOptions FilterOptions { get; set; } = new();

    private List<CardDto> _allCards = [];
    private List<CardDto> _cardList = [];

    // Filter state – each dimension tracks include/exclude per value
    private Dictionary<string, FilterMode> _setFilter = new();
    private Dictionary<string, FilterMode> _artistFilter = new();
    private Dictionary<string, FilterMode> _frameFilter = new();
    private Dictionary<string, FilterMode> _finishFilter = new();
    private readonly List<string> _artTags = [];
    private string _artTagInput = string.Empty;
    private readonly Dictionary<string, HashSet<string>> _artTagCache = new(StringComparer.OrdinalIgnoreCase);
    private bool _isLoadingTags;

    // Sort state – default: Release Date Descending
    private string _sortBy = "released_at";
    private bool _sortAscending;

    // Available options derived from the full unfiltered list
    private List<string> _availableSets = [];
    private List<string> _availableArtists = [];
    private List<string> _availableFrames = [];
    private List<string> _availableFinishes = [];

    private bool HasActiveFilters =>
        _setFilter.Count > 0 || _artistFilter.Count > 0 || _frameFilter.Count > 0 ||
        _finishFilter.Count > 0 || _artTags.Count > 0;

    private void Close() => MudDialog?.Cancel();
    private void SelectArt(CardDto card) => MudDialog?.Close(DialogResult.Ok(card));

    private static string GetImageUrl(CardDto card) =>
        card.ImageUris?.Png?.ToString() ??
        card.CardFaces?.FirstOrDefault()?.ImageUris?.Png?.ToString() ??
        "images/card-placeholder.png";

    protected override async Task OnInitializedAsync()
    {
        var oracleId = Card.EffectiveOracleId;
        if (oracleId != null)
            _allCards = ScryfallService.GetPrintsByOracleId(oracleId, FilterOptions.Language, FilterOptions.HighresOnly);

        ComputeAvailableOptions();
        ApplyFiltersAndSort();
        await base.OnInitializedAsync();
    }

    private void ComputeAvailableOptions()
    {
        _availableSets = _allCards
            .Where(c => c.Set != null)
            .Select(c => c.Set!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s)
            .ToList();

        _availableArtists = _allCards
            .Where(c => c.Artist != null)
            .Select(c => c.Artist!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(a => a)
            .ToList();

        _availableFrames = _allCards
            .Where(c => c.Frame != null)
            .Select(c => c.Frame!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f)
            .ToList();

        _availableFinishes = _allCards
            .Where(c => c.Finishes != null)
            .SelectMany(c => c.Finishes!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f)
            .ToList();
    }

    private void ApplyFiltersAndSort()
    {
        var filtered = _allCards.AsEnumerable();

        filtered = ApplyDimensionFilter(filtered, _setFilter, c => c.Set);
        filtered = ApplyDimensionFilter(filtered, _artistFilter, c => c.Artist);
        filtered = ApplyDimensionFilter(filtered, _frameFilter, c => c.Frame);
        filtered = ApplyFinishFilter(filtered, _finishFilter);

        if (_artTags.Count > 0)
        {
            var cachedTags = _artTags.Where(_artTagCache.ContainsKey).ToList();
            if (cachedTags.Count > 0)
            {
                var intersection = new HashSet<string>(_artTagCache[cachedTags[0]], StringComparer.OrdinalIgnoreCase);
                foreach (var t in cachedTags.Skip(1))
                    intersection.IntersectWith(_artTagCache[t]);
                filtered = filtered.Where(c => c.IllustrationId != null && intersection.Contains(c.IllustrationId));
            }
        }

        filtered = _sortBy == "artist"
            ? (_sortAscending
                ? filtered.OrderBy(c => c.Artist ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                : filtered.OrderByDescending(c => c.Artist ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            : (_sortAscending
                ? filtered.OrderBy(c => c.ReleasedAt ?? string.Empty)
                : filtered.OrderByDescending(c => c.ReleasedAt ?? string.Empty));

        _cardList = [.. filtered];
    }

    // Applies include/exclude filter for a single-valued string field (Set, Artist, Frame).
    private static IEnumerable<CardDto> ApplyDimensionFilter(
        IEnumerable<CardDto> source,
        Dictionary<string, FilterMode> filter,
        Func<CardDto, string?> selector)
    {
        var included = filter
            .Where(kv => kv.Value == FilterMode.Include)
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var excluded = filter
            .Where(kv => kv.Value == FilterMode.Exclude)
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (included.Count > 0)
            source = source.Where(c => { var v = selector(c); return v != null && included.Contains(v); });
        if (excluded.Count > 0)
            source = source.Where(c => { var v = selector(c); return v == null || !excluded.Contains(v); });

        return source;
    }

    // Finish is an array field, so include/exclude logic differs slightly.
    private static IEnumerable<CardDto> ApplyFinishFilter(
        IEnumerable<CardDto> source,
        Dictionary<string, FilterMode> filter)
    {
        var included = filter
            .Where(kv => kv.Value == FilterMode.Include)
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var excluded = filter
            .Where(kv => kv.Value == FilterMode.Exclude)
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (included.Count > 0)
            source = source.Where(c => c.Finishes != null && c.Finishes.Any(f => included.Contains(f)));
        if (excluded.Count > 0)
            source = source.Where(c => c.Finishes == null || !c.Finishes.Any(f => excluded.Contains(f)));

        return source;
    }

    private async Task AddArtTagAsync()
    {
        var tag = _artTagInput.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(tag) || _artTags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
        {
            _artTagInput = string.Empty;
            return;
        }

        _artTags.Add(tag);
        _artTagInput = string.Empty;

        if (!_artTagCache.ContainsKey(tag))
        {
            _isLoadingTags = true;
            StateHasChanged();

            var oracleId = Card.EffectiveOracleId;
            _artTagCache[tag] = oracleId != null
                ? await ScryfallService.GetIllustrationIdsByArtTagAsync(oracleId, tag)
                : [];

            _isLoadingTags = false;
        }

        ApplyFiltersAndSort();
    }

    private void RemoveArtTagChip(MudChip<string> chip)
    {
        var tag = chip.Text;
        if (tag != null)
        {
            _artTags.Remove(tag);
            ApplyFiltersAndSort();
        }
    }

    private void ClearAllFilters()
    {
        _setFilter = new();
        _artistFilter = new();
        _frameFilter = new();
        _finishFilter = new();
        _artTags.Clear();
        _artTagInput = string.Empty;
        _sortBy = "released_at";
        _sortAscending = false;
        ApplyFiltersAndSort();
    }

    private void ToggleSortDirection()
    {
        _sortAscending = !_sortAscending;
        ApplyFiltersAndSort();
    }

    private void OnSortByChanged(string value)
    {
        _sortBy = value;
        ApplyFiltersAndSort();
    }

    private void OnSetFilterChanged(Dictionary<string, FilterMode> state) { _setFilter = state; ApplyFiltersAndSort(); }
    private void OnArtistFilterChanged(Dictionary<string, FilterMode> state) { _artistFilter = state; ApplyFiltersAndSort(); }
    private void OnFrameFilterChanged(Dictionary<string, FilterMode> state) { _frameFilter = state; ApplyFiltersAndSort(); }
    private void OnFinishFilterChanged(Dictionary<string, FilterMode> state) { _finishFilter = state; ApplyFiltersAndSort(); }

    private async Task OnTagInputKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await AddArtTagAsync();
    }

    private string GetSetDisplay(string setCode)
    {
        var setName = _allCards
            .FirstOrDefault(c => string.Equals(c.Set, setCode, StringComparison.OrdinalIgnoreCase))
            ?.SetName;
        return setName != null ? $"{setCode.ToUpperInvariant()} – {setName}" : setCode.ToUpperInvariant();
    }

    private static string FormatFrameName(string frame) => frame switch
    {
        "1993" => "1993 (Alpha/Beta)",
        "1997" => "1997 (Classic)",
        "2003" => "2003 (Modern)",
        "2015" => "2015 (Current)",
        "future" => "Future Sight",
        _ => char.ToUpperInvariant(frame[0]) + frame[1..]
    };

    private static string FormatFinishName(string finish) => finish switch
    {
        "nonfoil" => "Nonfoil",
        "foil" => "Foil",
        "etched" => "Etched Foil",
        "glossy" => "Glossy",
        _ => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(finish)
    };
}
