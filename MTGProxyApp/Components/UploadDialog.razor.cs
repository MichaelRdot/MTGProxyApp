using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace MTGProxyApp.Components;

public partial class UploadDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private bool _loading;
    private readonly Dictionary<string, object> _folderAttributes = new()
    {
        { "webkitdirectory", true },
        { "multiple", true }
    };

    private async Task TriggerFilePickerAsync() =>
        await JS.InvokeVoidAsync("triggerClick", "fileInput");

    private async Task TriggerFolderPickerAsync() =>
        await JS.InvokeVoidAsync("triggerClick", "folderInput");

    private async Task HandleFilesAsync(InputFileChangeEventArgs e)
    {
        _loading = true;
        StateHasChanged();
        var results = new List<(string FileName, byte[] Data)>();
        foreach (var file in e.GetMultipleFiles(maximumFileCount: 500))
        {
            var ext = Path.GetExtension(file.Name).ToLowerInvariant();
            if (ext is not (".png" or ".jpg" or ".jpeg")) continue;
            try
            {
                using var ms = new MemoryStream();
                await (file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024)).CopyToAsync(ms);
                results.Add((file.Name, ms.ToArray()));
            }
            catch { /* skip unreadable files */ }
        }
        _loading = false;
        if (results.Count == 0) return;
        MudDialog!.Close(DialogResult.Ok(results));
    }

    private void Close() => MudDialog!.Cancel();
}
