using Microsoft.AspNetCore.Components;
using MTGProxyApp.Models;

namespace MTGProxyApp.Components;

public partial class YearFilterPicker : ComponentBase
{
    [Parameter] public string Label { get; set; } = "Year";
    [Parameter] public List<string> Items { get; set; } = [];
    [Parameter] public YearFilterState FilterState { get; set; } = new();
    [Parameter] public EventCallback<YearFilterState> FilterStateChanged { get; set; }

    private int? _rangeStart;
    private int? _rangeEnd;
    private string _searchText = "";

    private IEnumerable<string> FilteredItems => string.IsNullOrWhiteSpace(_searchText)
        ? Items
        : Items.Where(y => y.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

    private void OnSearchChanged(string value) => _searchText = value;

    protected override void OnParametersSet()
    {
        if (_rangeStart != FilterState.RangeStart) _rangeStart = FilterState.RangeStart;
        if (_rangeEnd != FilterState.RangeEnd) _rangeEnd = FilterState.RangeEnd;
    }

    private bool HasActive => FilterState.IsActive;

    private string ButtonLabel
    {
        get
        {
            if (!HasActive) return Label;
            var parts = new List<string>();
            if (FilterState.RangeStart.HasValue || FilterState.RangeEnd.HasValue)
            {
                var start = FilterState.RangeStart?.ToString() ?? "…";
                var end = FilterState.RangeEnd?.ToString() ?? "…";
                parts.Add($"{start}–{end}");
            }
            var incCount = FilterState.Years.Count(kv => kv.Value == FilterMode.Include);
            var excCount = FilterState.Years.Count(kv => kv.Value == FilterMode.Exclude);
            if (incCount > 0) parts.Add($"+{incCount}");
            if (excCount > 0) parts.Add($"−{excCount}");
            return $"{Label} ({string.Join(", ", parts)})";
        }
    }

    private FilterMode GetMode(string year) =>
        FilterState.Years.TryGetValue(year, out var mode) ? mode : FilterMode.None;

    private async Task ToggleYear(string year)
    {
        var next = GetMode(year) switch
        {
            FilterMode.None => FilterMode.Include,
            FilterMode.Include => FilterMode.Exclude,
            _ => FilterMode.None
        };

        var updatedYears = new Dictionary<string, FilterMode>(FilterState.Years);
        if (next == FilterMode.None)
            updatedYears.Remove(year);
        else
            updatedYears[year] = next;

        await FilterStateChanged.InvokeAsync(new YearFilterState
        {
            Years = updatedYears,
            RangeStart = FilterState.RangeStart,
            RangeEnd = FilterState.RangeEnd
        });
    }

    private async Task OnRangeStartChanged(int? value)
    {
        _rangeStart = value;
        await FilterStateChanged.InvokeAsync(new YearFilterState
        {
            Years = new Dictionary<string, FilterMode>(FilterState.Years),
            RangeStart = _rangeStart,
            RangeEnd = FilterState.RangeEnd
        });
    }

    private async Task OnRangeEndChanged(int? value)
    {
        _rangeEnd = value;
        await FilterStateChanged.InvokeAsync(new YearFilterState
        {
            Years = new Dictionary<string, FilterMode>(FilterState.Years),
            RangeStart = FilterState.RangeStart,
            RangeEnd = _rangeEnd
        });
    }

    private async Task ClearFilter()
    {
        _rangeStart = null;
        _rangeEnd = null;
        _searchText = "";
        await FilterStateChanged.InvokeAsync(new YearFilterState());
    }
}
