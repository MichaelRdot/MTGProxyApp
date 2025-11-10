using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MTGProxyApp.Controllers;

[ApiController]
[Route("api/save-deck-sheets")]
public class SaveDeckSheetsController : ControllerBase
{
    public class SaveDeckRequest
    {
        public string DeckName { get; set; } = "";
        public List<string> ImageUrls { get; set; } = new();

        public bool UseBlackCorners { get; set; } = false;
        public bool FullPageCutLines { get; set; } = false;
    }

    [HttpPost]
    public async Task<IActionResult> SaveDeckSheets(
        [FromBody] SaveDeckRequest request,
        [FromServices] IHttpClientFactory httpClientFactory)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.DeckName) || request.ImageUrls.Count == 0)
            return BadRequest("Missing deck name or images.");

        var deckNameSafe = Sanitize(request.DeckName);
        var useBlackCorners = request.UseBlackCorners;
        var fullPageCutLines = request.FullPageCutLines || request.UseBlackCorners;

        // ===== Layout =====
        const float ptPerInch = 72f;
        var pageSize = PageSizes.Letter; // change to A4 if desired

        float pageW = pageSize.Width;
        float pageH = pageSize.Height;

        const float outerMarginIn = 0.25f;
        const float gutterIn = 0.00f;    // spacing between cards
        const float shrink = 0.99f;      // tiny shrink to avoid visual overlap due to print/rounding

        float outer = outerMarginIn * ptPerInch;
        float gutter = gutterIn * ptPerInch;

        const float cardAspect = 3.5f / 2.5f; // H/W

        float innerW = pageW - 2 * outer;
        float innerH = pageH - 2 * outer;

        float maxCardWByWidth = (innerW - 2 * gutter) / 3f;
        float maxCardHByHeight = (innerH - 2 * gutter) / 3f;
        float maxCardWByHeight = maxCardHByHeight / cardAspect;

        float cardW = MathF.Min(maxCardWByWidth, maxCardWByHeight) * shrink;
        float cardH = cardW * cardAspect;

        float gridW = cardW * 3 + gutter * 2;
        float gridH = cardH * 3 + gutter * 2;

        // ===== Partition into pages of 9 =====
        var all = request.ImageUrls;
        var pages = all.Select((url, idx) => new { url, idx })
                       .GroupBy(x => x.idx / 9)
                       .Select(g => g.Select(x => x.url).ToList())
                       .ToList();

        // ===== Fetch images =====
        var client = httpClientFactory.CreateClient();
        var cache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in all.Distinct())
            cache[u] = await client.GetByteArrayAsync(u);

        // ===== Build PDF =====
        var pdf = Document.Create(doc =>
        {
            foreach (var pageUrls in pages)
            {
                doc.Page(page =>
                {
                    page.Size(pageSize);
                    page.Margin(0);
                    page.DefaultTextStyle(t => t.FontSize(10));

                    // 1) Edge-to-edge cut lines (use Layers so we can add multiple children safely)
                    page.Background().Layers(layers =>
                    {
                        // ✅ QuestPDF requires exactly one PrimaryLayer – include it even if we draw nothing
                        layers.PrimaryLayer().Element(_ => { });

                        if (!fullPageCutLines)
                            return;

                        // Compute grid origin inside the page (centered within the margin-padded area)
                        float gridLeft = outer + ((pageW - 2 * outer) - gridW) / 2f;
                        float gridTop  = outer + ((pageH - 2 * outer) - gridH) / 2f;

                        // Vertical x-positions
                        float x0 = gridLeft;
                        float x1 = gridLeft + cardW;
                        float x2 = x1 + gutter + cardW;
                        float x3 = x2 + gutter + cardW;

                        // Horizontal y-positions
                        float y0 = gridTop;
                        float y1 = gridTop + cardH;
                        float y2 = y1 + gutter + cardH;
                        float y3 = y2 + gutter + cardH;

                        const float lineThick = 0.5f;

                        // Draw vertical lines to page edges (each in its own layer)
                        layers.Layer().Element(e => e
                            .PaddingLeft(x0).AlignLeft().Width(lineThick).Height(pageH).Background(Colors.Black));
                        layers.Layer().Element(e => e
                            .PaddingLeft(x1).AlignLeft().Width(lineThick).Height(pageH).Background(Colors.Black));
                        layers.Layer().Element(e => e
                            .PaddingLeft(x2).AlignLeft().Width(lineThick).Height(pageH).Background(Colors.Black));
                        layers.Layer().Element(e => e
                            .PaddingLeft(x3).AlignLeft().Width(lineThick).Height(pageH).Background(Colors.Black));

                        // Draw horizontal lines to page edges (each in its own layer)
                        layers.Layer().Element(e => e
                            .PaddingTop(y0).AlignTop().Height(lineThick).Width(pageW).Background(Colors.Black));
                        layers.Layer().Element(e => e
                            .PaddingTop(y1).AlignTop().Height(lineThick).Width(pageW).Background(Colors.Black));
                        layers.Layer().Element(e => e
                            .PaddingTop(y2).AlignTop().Height(lineThick).Width(pageW).Background(Colors.Black));
                        layers.Layer().Element(e => e
                            .PaddingTop(y3).AlignTop().Height(lineThick).Width(pageW).Background(Colors.Black));
                    });

                    // 2) Centered 3×3 grid of cards
                    page.Content().Padding(outer)
                        .AlignCenter().AlignMiddle()
                        .Width(gridW).Height(gridH)
                        .Column(col =>
                        {
                            for (int r = 0; r < 3; r++)
                            {
                                col.Item().Height(cardH).Row(row =>
                                {
                                    row.Spacing(gutter);
                                    for (int c = 0; c < 3; c++)
                                    {
                                        int idx = r * 3 + c;
                                        row.ConstantItem(cardW).Element(cell =>
                                        {
                                            cell.AlignCenter().AlignMiddle().Element(e =>
                                            {
                                                if (idx < pageUrls.Count)
                                                {
                                                    var bytes = cache[pageUrls[idx]];
                                                    var card = e.Width(cardW).Height(cardH);

                                                    if (useBlackCorners)
                                                    {
                                                        card.Layers(layer =>
                                                        {
                                                            // Background black rectangle
                                                            layer.Layer().Background(Colors.Black);

                                                            // Card image
                                                            layer.PrimaryLayer().Image(bytes).FitArea();

                                                            // Corner bites: each bite segment gets its own layer (no repeats on a single container)
                                                            const float nib = 5f;     // bite length
                                                            const float bite = 0.5f; // bite thickness

                                                            // top-left
                                                            layer.Layer().Element(x => x.AlignTop().AlignLeft().Width(nib).Height(bite).Background(Colors.White));
                                                            layer.Layer().Element(x => x.AlignTop().AlignLeft().Width(bite).Height(nib).Background(Colors.White));

                                                            // top-right
                                                            layer.Layer().Element(x => x.AlignTop().AlignRight().Width(nib).Height(bite).Background(Colors.White));
                                                            layer.Layer().Element(x => x.AlignTop().AlignRight().Width(bite).Height(nib).Background(Colors.White));

                                                            // bottom-left
                                                            layer.Layer().Element(x => x.AlignBottom().AlignLeft().Width(nib).Height(bite).Background(Colors.White));
                                                            layer.Layer().Element(x => x.AlignBottom().AlignLeft().Width(bite).Height(nib).Background(Colors.White));

                                                            // bottom-right
                                                            layer.Layer().Element(x => x.AlignBottom().AlignRight().Width(nib).Height(bite).Background(Colors.White));
                                                            layer.Layer().Element(x => x.AlignBottom().AlignRight().Width(bite).Height(nib).Background(Colors.White));
                                                        });
                                                    }
                                                    else
                                                    {
                                                        card.Image(bytes).FitArea();
                                                    }
                                                }
                                                else
                                                {
                                                    e.Border(0.25f).BorderColor("#EEEEEE").Width(cardW).Height(cardH);
                                                }
                                            });
                                        });
                                    }
                                });
                            }
                        });
                });
            }
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"{deckNameSafe}.pdf");
    }

    private static string Sanitize(string s)
    {
        foreach (var ch in Path.GetInvalidFileNameChars())
            s = s.Replace(ch, '_');
        return s.Trim();
    }
}
