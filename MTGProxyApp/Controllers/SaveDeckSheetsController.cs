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
    }

    [HttpPost]
    public async Task<IActionResult> SaveDeckSheets(
        [FromBody] SaveDeckRequest req,
        [FromServices] IHttpClientFactory factory)
    {
        if (string.IsNullOrWhiteSpace(req.DeckName) || req.ImageUrls.Count == 0)
            return BadRequest("Missing deck name or images.");

        var deckNameSafe = Sanitize(req.DeckName);

        // ======= Tunable layout (inches) =======
        const float outerMarginIn = 0.25f; // printable edge margin
        const float gutterIn      = 0.00f; // no gaps between cards
        const float shrinkFactor  = 0.99f; // 1% smaller to compensate printer/rounding

        // Paper size
        var pageSize = PageSizes.Letter;    // or PageSizes.A4
        float pageWpt = pageSize.Width;
        float pageHpt = pageSize.Height;

        // ======= Geometry =======
        const float ptPerInch = 72f;
        float outer  = outerMarginIn * ptPerInch;
        float gutter = gutterIn      * ptPerInch;   // 0

        // MTG card aspect (2.5" x 3.5")
        const float trimAspect = 3.5f / 2.5f;       // height / width ≈ 1.4

        // Compute the LARGEST card size that fits 3x3 inside the inner area:
        float innerW = pageWpt - 2 * outer;
        float innerH = pageHpt - 2 * outer;

        float maxTwByWidth  = (innerW - 2 * gutter) / 3f;
        float maxThByHeight = (innerH - 2 * gutter) / 3f;
        float maxTwByHeight = maxThByHeight / trimAspect;

        float cardW = MathF.Min(maxTwByWidth, maxTwByHeight); // card width (pt)
        float cardH = cardW * trimAspect;                     // card height (pt)

        // Apply a small shrink to avoid “printer creep” and rounding collisions
        cardW *= shrinkFactor;
        cardH *= shrinkFactor;

        // Fixed grid size (used to center perfectly so left/right look even)
        float gridW = cardW * 3 + gutter * 2; // gutter is zero today but keeps formula generic
        float gridH = cardH * 3 + gutter * 2;

        // ======= Partition list into pages of 9 =======
        var urls = req.ImageUrls;
        var pages = urls
            .Select((u, i) => new { u, i })
            .GroupBy(x => x.i / 9)
            .Select(g => g.Select(x => x.u).ToList())
            .ToList();

        // ======= Prefetch all images (no async in layout) =======
        var client = factory.CreateClient();
        var cache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in urls.Distinct())
            cache[u] = await client.GetByteArrayAsync(u);

        // ======= Build PDF =======
        var pdfBytes = Document
            .Create(doc =>
            {
                foreach (var pageUrls in pages)
                {
                    doc.Page(page =>
                    {
                        page.Size(pageSize);
                        page.Margin(0);
                        page.DefaultTextStyle(t => t.FontSize(10));

                        // Inner printable area with outer margin
                        page.Content().Padding(outer)
                            // Center the entire fixed-size 3x3 grid so side space is equal
                            .AlignCenter().AlignMiddle()
                            .Width(gridW).Height(gridH)
                            .Column(col =>
                            {
                                for (int r = 0; r < 3; r++)
                                {
                                    // Fix row height to card height (prevents vertical flex)
                                    col.Item().Height(cardH).Row(row =>
                                    {
                                        row.Spacing(0); // absolutely no gaps between cols
                                        for (int c = 0; c < 3; c++)
                                        {
                                            int idx = r * 3 + c;

                                            // Fixed column width (prevents horizontal flex)
                                            row.ConstantItem(cardW).Element(cell =>
                                            {
                                                // Single-stroke grid: avoid double lines
                                                var borderThickness = 0.5f;
                                                bool drawLeft   = (c == 0);
                                                bool drawTop    = (r == 0);
                                                bool drawRight  = true;  // covers internal and outer right
                                                bool drawBottom = true;  // covers internal and outer bottom

                                                cell.Padding(0)
                                                    .BorderColor("#000000")
                                                    .BorderLeft(drawLeft ? borderThickness : 0)
                                                    .BorderTop(drawTop ? borderThickness : 0)
                                                    .BorderRight(drawRight ? borderThickness : 0)
                                                    .BorderBottom(drawBottom ? borderThickness : 0)
                                                    .AlignCenter().AlignMiddle()
                                                    .Element(e =>
                                                    {
                                                        if (idx < pageUrls.Count)
                                                        {
                                                            var u = pageUrls[idx];
                                                            var bytes = cache[u];
                                                            // Keep aspect and fill as much as possible
                                                            e.Image(bytes).FitArea();
                                                        }
                                                        else
                                                        {
                                                            // Keep grid shape if final page has < 9
                                                            e.Border(0.25f).BorderColor("#EEEEEE");
                                                        }
                                                    });
                                            });
                                        }
                                    });
                                }
                            });
                    });
                }
            })
            .GeneratePdf();

        return File(pdfBytes, "application/pdf", $"{deckNameSafe}.pdf");
    }

    private static string Sanitize(string s)
    {
        foreach (var ch in Path.GetInvalidFileNameChars())
            s = s.Replace(ch, '_');
        return s.Trim();
    }
}
