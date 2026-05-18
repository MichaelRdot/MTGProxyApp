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
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private bool _started;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _started) return;
        _started = true;
        var html = BuildPrintHtml(PreparePages());
        await Js.InvokeVoidAsync("openPrintWindow", html);
        if (OnFinished.HasDelegate) await OnFinished.InvokeAsync();
    }

    private List<List<List<string?>>> PreparePages()
    {
        var result = new List<List<List<string?>>>();

        result.Add(CardUrls[0].Chunk(9).Select(c => c.Cast<string?>().ToList()).ToList());

        if (!PrintFlipCardsSeparate || CardUrls[1].Count == 0) return result;

        result.Add(CardUrls[1].Chunk(9).Select(c => c.Cast<string?>().ToList()).ToList());

        // Flip backs: each row of 3 is left-padded then reversed for duplex alignment.
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

    private string BuildPrintHtml(List<List<List<string?>>> pageGroups)
    {
        var body = new StringBuilder();
        foreach (var group in pageGroups)
            foreach (var page in group)
                body.Append(BuildPage(page));

        var css = CssTemplate.Replace("BORDER_VALUE", Borders ? "0.35mm" : "0px");

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="UTF-8">
            <title>Card Print Sheet</title>
            <base href="{Nav.BaseUri}">
            <style>
            {css}
            </style>
            {PrintScript}
            </head>
            <body>
            {body}
            </body>
            </html>
            """;
    }

    private string BuildPage(List<string?> cards)
    {
        var corners = BlackCorners
            ? """
              <div class="corner tl"><div class="cross"></div></div>
              <div class="corner tr"><div class="cross"></div></div>
              <div class="corner bl"><div class="cross"></div></div>
              <div class="corner br"><div class="cross"></div></div>
              """
            : "";

        var cardHtml = new StringBuilder();
        for (var i = 0; i < 9; i++)
        {
            var url = i < cards.Count ? cards[i] : null;
            var blackBg = BlackCorners && url != null ? " black-bg" : "";
            var img = url != null ? $"""<img src="{url}" alt="">""" : "";
            cardHtml.Append($"""
                <div class="card{blackBg}">
                  {corners}
                  {img}
                </div>
                """);
        }

        return $"""
            <div class="page">
              <div class="hline hline-0"></div>
              <div class="hline hline-1"></div>
              <div class="hline hline-2"></div>
              <div class="hline hline-3"></div>
              <div class="vline vline-0"></div>
              <div class="vline vline-1"></div>
              <div class="vline vline-2"></div>
              <div class="vline vline-3"></div>
              <div class="sheet">
                {cardHtml}
              </div>
            </div>
            """;
    }

    // Auto-print after all images finish loading (or immediately if no images).
    private const string PrintScript = """
        <script>
        window.addEventListener('load', function() {
          var imgs = document.querySelectorAll('img');
          var total = imgs.length, done = 0;
          if (!total) { window.print(); return; }
          function check() { if (++done >= total) window.print(); }
          imgs.forEach(function(img) {
            if (img.complete && img.naturalHeight) check();
            else { img.addEventListener('load', check); img.addEventListener('error', check); }
          });
        });
        </script>
        """;

    private const string CssTemplate = """
        :root {
          --page-w:    215.9mm;
          --page-h:    279.4mm;
          --card-w:    63mm;
          --card-h:    88mm;
          --grid-w:    calc(3 * var(--card-w));
          --grid-h:    calc(3 * var(--card-h));
          --margin-h:  calc((var(--page-w) - var(--grid-w)) / 2);
          --margin-v:  calc((var(--page-h) - var(--grid-h)) / 2);
          --line:      0.35mm;
          --cross-arm: 2.82mm;
          --border:    BORDER_VALUE;
        }

        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

        @page { size: 215.9mm 279.4mm; margin: 0; }

        .page {
          background: #fff;
          width: var(--page-w);
          height: var(--page-h);
          position: relative;
          -webkit-print-color-adjust: exact;
          print-color-adjust: exact;
          overflow: hidden;
          page-break-after: always;
          break-after: page;
        }

        .page:last-child {
          page-break-after: avoid;
          break-after: avoid;
        }

        .hline {
          position: absolute;
          left: 0;
          width: var(--page-w);
          height: var(--line);
          background: #000;
        }
        .hline-0 { top: calc(var(--margin-v) + 0 * var(--card-h)); }
        .hline-1 { top: calc(var(--margin-v) + 1 * var(--card-h)); }
        .hline-2 { top: calc(var(--margin-v) + 2 * var(--card-h)); }
        .hline-3 { top: calc(var(--margin-v) + 3 * var(--card-h)); }

        .vline {
          position: absolute;
          top: 0;
          height: var(--page-h);
          width: var(--line);
          background: #000;
        }
        .vline-0 { left: calc(var(--margin-h) + 0 * var(--card-w)); }
        .vline-1 { left: calc(var(--margin-h) + 1 * var(--card-w)); }
        .vline-2 { left: calc(var(--margin-h) + 2 * var(--card-w)); }
        .vline-3 { left: calc(var(--margin-h) + 3 * var(--card-w)); }

        .sheet {
          position: absolute;
          top: var(--margin-v);
          left: var(--margin-h);
          display: grid;
          grid-template-columns: repeat(3, var(--card-w));
          grid-template-rows: repeat(3, var(--card-h));
        }

        .card {
          width: var(--card-w);
          height: var(--card-h);
          background: #fff;
          position: relative;
          border: var(--border) solid #000;
        }

        .card.black-bg { background: #000; }

        .card img {
          width: 100%;
          height: 100%;
          display: block;
          object-fit: fill;
        }

        .corner { position: absolute; z-index: 2; }
        .corner.tl { top: 0;    left: 0; }
        .corner.tr { top: 0;    right: 0; }
        .corner.bl { bottom: 0; left: 0; }
        .corner.br { bottom: 0; right: 0; }

        .cross { position: absolute; transform: translate(-50%, -50%); }

        .cross::before {
          content: '';
          position: absolute;
          background: #fff;
          width: var(--cross-arm);
          height: var(--line);
          top: 0;
          left: calc(var(--cross-arm) / -2);
        }

        .cross::after {
          content: '';
          position: absolute;
          background: #fff;
          width: var(--line);
          height: var(--cross-arm);
          left: 0;
          top: calc(var(--cross-arm) / -2);
        }
        """;
}
