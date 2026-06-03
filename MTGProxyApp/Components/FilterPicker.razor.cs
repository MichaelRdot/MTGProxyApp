using Microsoft.AspNetCore.Components;
using MTGProxyApp.Models;

namespace MTGProxyApp.Components;

public partial class FilterPicker : ComponentBase
{
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public List<string> Items { get; set; } = [];
    [Parameter] public Func<string, string>? FormatDisplay { get; set; }
    [Parameter] public Dictionary<string, FilterMode> FilterState { get; set; } = new();
    [Parameter] public EventCallback<Dictionary<string, FilterMode>> FilterStateChanged { get; set; }

    private int IncludeCount => FilterState.Count(kv => kv.Value == FilterMode.Include);
    private int ExcludeCount => FilterState.Count(kv => kv.Value == FilterMode.Exclude);
    private bool HasActive => FilterState.Count > 0;

    private string _searchText = "";

    private IEnumerable<string> FilteredItems => string.IsNullOrWhiteSpace(_searchText)
        ? Items
        : Items.Where(i => (FormatDisplay?.Invoke(i) ?? i).Contains(_searchText, StringComparison.OrdinalIgnoreCase));

    private string ButtonLabel
    {
        get
        {
            if (!HasActive) return Label;
            var parts = new List<string>();
            if (IncludeCount > 0) parts.Add($"+{IncludeCount}");
            if (ExcludeCount > 0) parts.Add($"−{ExcludeCount}");
            return $"{Label} ({string.Join(", ", parts)})";
        }
    }

    private FilterMode GetMode(string key) =>
        FilterState.TryGetValue(key, out var mode) ? mode : FilterMode.None;

    private async Task ToggleItem(string key)
    {
        var next = GetMode(key) switch
        {
            FilterMode.None => FilterMode.Include,
            FilterMode.Include => FilterMode.Exclude,
            _ => FilterMode.None
        };

        var updated = new Dictionary<string, FilterMode>(FilterState);
        if (next == FilterMode.None)
            updated.Remove(key);
        else
            updated[key] = next;

        await FilterStateChanged.InvokeAsync(updated);
    }

    private void OnSearchChanged(string value) => _searchText = value;

    private async Task ClearFilter()
    {
        _searchText = "";
        await FilterStateChanged.InvokeAsync(new Dictionary<string, FilterMode>());
    }
}
