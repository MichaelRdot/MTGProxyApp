using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MTGProxyApp.Components;

public partial class UploadDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    private void Close() => MudDialog.Cancel();

}