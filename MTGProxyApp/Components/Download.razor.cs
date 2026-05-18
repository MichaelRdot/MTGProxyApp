using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MTGProxyApp.Components;

public partial class Download(IJSRuntime Js) : ComponentBase
{
    [Parameter] public EventCallback OnFinished { get; set; }
    [Parameter] public required List<List<string>> CardUrls { get; set; }
    [Parameter] public string DeckName { get; set; } = "";
    [Parameter] public bool BlackCorners { get; set; }
    [Parameter] public bool Borders { get; set; }
    [Parameter] public bool PrintFlipCardsSeparate { get; set; }

    private bool _started;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _started) return;
        _started = true;
        var pages = PreparePages();
        var fileName = string.IsNullOrEmpty(DeckName) ? "deck" : DeckName;
        await Js.InvokeVoidAsync("generateProxyPdf", pages, new { blackCorners = BlackCorners, borders = Borders, fileName });
        if (OnFinished.HasDelegate) await OnFinished.InvokeAsync();
    }

    private List<List<List<string?>>> PreparePages()
    {
        var result = new List<List<List<string?>>>();

        result.Add(CardUrls[0].Chunk(9).Select(c => c.Cast<string?>().ToList()).ToList());

        if (!PrintFlipCardsSeparate || CardUrls[1].Count == 0) return result;

        result.Add(CardUrls[1].Chunk(9).Select(c => c.Cast<string?>().ToList()).ToList());

        // Flip backs: each row of 3 is left-padded then reversed so backs
        // align with fronts when the sheet is flipped over for duplex printing.
        result.Add(CardUrls[2].Chunk(9).Select(pageChunk =>
        {
            var page = new List<string?>();
            foreach (var row in pageChunk.Chunk(3))
            {
                for (var i = 0; i < 3 - row.Length; i++) page.Add(null);
                for (var i = row.Length - 1; i >= 0; i--) page.Add(row[i]);
            }
            return page;
        }).ToList());

        return result;
    }
}
