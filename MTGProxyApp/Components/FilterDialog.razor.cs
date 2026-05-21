using Microsoft.AspNetCore.Components;
using MTGProxyApp.Models;
using MudBlazor;

namespace MTGProxyApp.Components;

public partial class FilterDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    [Parameter] public required CardFilterOptions Options { get; set; }

    private string? _language;
    private bool _highresOnly;

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

    protected override void OnInitialized()
    {
        _language = Options.Language;
        _highresOnly = Options.HighresOnly;
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

    private void Apply() => MudDialog.Close(DialogResult.Ok(new CardFilterOptions
    {
        Language = _language,
        HighresOnly = _highresOnly
    }));

    private void Cancel() => MudDialog.Cancel();
}
