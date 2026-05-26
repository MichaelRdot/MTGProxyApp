using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MTGProxyApp.Components;

public partial class SplitDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int MaxCount { get; set; }
    [Parameter] public string CardName { get; set; } = "";

    private int _splitCount = 1;

    private bool IsValid => _splitCount >= 1 && _splitCount < MaxCount;

    private string ErrorText => _splitCount < 1
        ? "Must split off at least 1"
        : _splitCount >= MaxCount
            ? $"Must be less than {MaxCount}"
            : "";

    private void Confirm()
    {
        if (IsValid) MudDialog.Close(DialogResult.Ok(_splitCount));
    }

    private void Cancel() => MudDialog.Cancel();
}
