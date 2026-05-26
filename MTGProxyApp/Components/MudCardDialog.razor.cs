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

    // Filter state
    private IEnumerable<string> _selectedSets = [];
    private IEnumerable<string> _selectedArtists = [];
    private IEnumerable<string> _selectedFrames = [];
    private IEnumerable<string> _selectedFinishes = [];
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
        _selectedSets.Any() || _selectedArtists.Any() || _selectedFrames.Any() ||
        _selectedFinishes.Any() || _artTags.Count > 0;

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

        var sets = _selectedSets.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (sets.Count > 0)
            filtered = filtered.Where(c => c.Set != null && sets.Contains(c.Set));

        var artists = _selectedArtists.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (artists.Count > 0)
            filtered = filtered.Where(c => c.Artist != null && artists.Contains(c.Artist));

        var frames = _selectedFrames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (frames.Count > 0)
            filtered = filtered.Where(c => c.Frame != null && frames.Contains(c.Frame));

        var finishes = _selectedFinishes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (finishes.Count > 0)
            filtered = filtered.Where(c => c.Finishes != null && c.Finishes.Any(f => finishes.Contains(f)));

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
        _selectedSets = [];
        _selectedArtists = [];
        _selectedFrames = [];
        _selectedFinishes = [];
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

    private void OnSelectedSetsChanged(IEnumerable<string> values)
    {
        _selectedSets = values.ToList();
        ApplyFiltersAndSort();
    }

    private void OnSelectedArtistsChanged(IEnumerable<string> values)
    {
        _selectedArtists = values.ToList();
        ApplyFiltersAndSort();
    }

    private void OnSelectedFramesChanged(IEnumerable<string> values)
    {
        _selectedFrames = values.ToList();
        ApplyFiltersAndSort();
    }

    private void OnSelectedFinishesChanged(IEnumerable<string> values)
    {
        _selectedFinishes = values.ToList();
        ApplyFiltersAndSort();
    }

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
