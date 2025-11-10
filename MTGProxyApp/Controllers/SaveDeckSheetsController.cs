using System.Text;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace MTGProxyApp.Controllers;

[ApiController]
[Route("api/save-deck-sheets")]
public class SaveDeckSheetsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SaveDeckSheets(
        [FromBody] SaveDeckRequest request,
        [FromServices] IHttpClientFactory httpClientFactory,
        [FromServices] IWebHostEnvironment env)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.DeckName) || request.ImageUrls.Count == 0)
            return BadRequest("Missing deck name or images.");

        var deckNameSafe = Sanitize(request.DeckName);
        var useBlackCorners = request.UseBlackCorners;
        var fullPageCutLines = request.FullPageCutLines || request.UseBlackCorners;

        // ===== Layout =====
        const float ptPerInch = 72f;
        var pageSize = PageSizes.Letter;

        float pageW = pageSize.Width;
        float pageH = pageSize.Height;

        const float outerMarginIn = 0.25f;
        const float gutterIn = 0.00f;

        float outer = outerMarginIn * ptPerInch;
        float gutter = gutterIn * ptPerInch;

// --- Fixed physical card size (MTG): 2.5" x 3.5"

        var shrink = 0.99f;                  // Makes card smaller, if needed
        var cardW = 2.5f * ptPerInch * shrink;   // 180 pt
        var cardH = 3.5f * ptPerInch * shrink;   // 252 pt

        float innerW = pageW - 2 * outer;
        float innerH = pageH - 2 * outer;

// Safety clamp: if paper/margins change in the future, scale down uniformly to fit 3x3
        if (cardW * 3f > innerW || cardH * 3f > innerH)
        {
            var scale = MathF.Min(innerW / (cardW * 3f), innerH / (cardH * 3f));
            cardW *= scale;
            cardH *= scale;
        }

        float gridW = cardW * 3 + gutter * 2;
        float gridH = cardH * 3 + gutter * 2;

        // ===== Partition into sets of 9 =====
        var frontsPages = request.ImageUrls
            .Select((url, idx) => new { url, idx })
            .GroupBy(x => x.idx / 9)
            .Select(g => g.Select(x => x.url).ToList())
            .ToList();

        List<List<string>> backsPages;
        if (request.IncludeBackPages)
        {
            var backs = NormalizeBacks(request.ImageUrls, request.BackImageUrls, request.DefaultBackImageUrl);
            backsPages = backs
                .Select((url, idx) => new { url, idx })
                .GroupBy(x => x.idx / 9)
                .Select(g => g.Select(x => x.url).ToList())
                .ToList();
        }
        else
        {
            backsPages = new List<List<string>>();
        }

        // ===== Prefetch images (fronts + backs + default if needed) =====
        var client = httpClientFactory.CreateClient();
        var allUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var u in request.ImageUrls)
            if (!string.IsNullOrWhiteSpace(u))
                allUrls.Add(u);

        foreach (var u in request.BackImageUrls)
            if (!string.IsNullOrWhiteSpace(u))
                allUrls.Add(u);

        if (!string.IsNullOrWhiteSpace(request.DefaultBackImageUrl))
            allUrls.Add(request.DefaultBackImageUrl!);

        var cache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in allUrls)
            cache[u] = await LoadImageBytesAsync(u, client, env.WebRootPath);

        byte[]? GetBytes(string? url)
        {
            return string.IsNullOrWhiteSpace(url) ? null
                : cache.TryGetValue(url, out var b) ? b : null;
        }

        // ===== Build PDF =====
        var pdf = Document.Create(doc =>
        {
            for (var p = 0; p < frontsPages.Count; p++)
            {
                var pageFronts = frontsPages[p];

                // FRONT PAGE
                doc.Page(page =>
                {
                    page.Size(pageSize);
                    page.Margin(0);
                    page.DefaultTextStyle(t => t.FontSize(10));

                    // Cut lines as layers – always include PrimaryLayer
                    page.Background().Layers(layers =>
                    {
                        layers.PrimaryLayer().Element(_ => { });

                        if (!fullPageCutLines) return;

                        var gridLeft = outer + (pageW - 2 * outer - gridW) / 2f;
                        var gridTop = outer + (pageH - 2 * outer - gridH) / 2f;

                        var x0 = gridLeft;
                        var x1 = gridLeft + cardW;
                        var x2 = x1 + gutter + cardW;
                        var x3 = x2 + gutter + cardW;

                        var y0 = gridTop;
                        var y1 = gridTop + cardH;
                        var y2 = y1 + gutter + cardH;
                        var y3 = y2 + gutter + cardH;

                        const float lineThick = 0.5f;

                        layers.Layer().Element(e =>
                            e.PaddingLeft(x0).AlignLeft().Width(lineThick).Height(pageH).Background(Colors.Black));
                        layers.Layer().Element(e =>
                            e.PaddingLeft(x1).AlignLeft().Width(lineThick).Height(pageH).Background(Colors.Black));
                        layers.Layer().Element(e =>
                            e.PaddingLeft(x2).AlignLeft().Width(lineThick).Height(pageH).Background(Colors.Black));
                        layers.Layer().Element(e =>
                            e.PaddingLeft(x3).AlignLeft().Width(lineThick).Height(pageH).Background(Colors.Black));

                        layers.Layer().Element(e =>
                            e.PaddingTop(y0).AlignTop().Height(lineThick).Width(pageW).Background(Colors.Black));
                        layers.Layer().Element(e =>
                            e.PaddingTop(y1).AlignTop().Height(lineThick).Width(pageW).Background(Colors.Black));
                        layers.Layer().Element(e =>
                            e.PaddingTop(y2).AlignTop().Height(lineThick).Width(pageW).Background(Colors.Black));
                        layers.Layer().Element(e =>
                            e.PaddingTop(y3).AlignTop().Height(lineThick).Width(pageW).Background(Colors.Black));
                    });

                    page.Content().Padding(outer)
                        .AlignCenter().AlignMiddle()
                        .Width(gridW).Height(gridH)
                        .Column(col =>
                        {
                            for (var r = 0; r < 3; r++)
                                col.Item().Height(cardH).Row(row =>
                                {
                                    row.Spacing(gutter);
                                    for (var c = 0; c < 3; c++)
                                    {
                                        var idx = r * 3 + c;
                                        row.ConstantItem(cardW).Element(cell =>
                                        {
                                            cell.AlignCenter().AlignMiddle().Element(e =>
                                            {
                                                if (idx < pageFronts.Count &&
                                                    !string.IsNullOrWhiteSpace(pageFronts[idx]))
                                                {
                                                    var fbytes = GetBytes(pageFronts[idx]);
                                                    var card = e.Width(cardW).Height(cardH);

                                                    if (useBlackCorners)
                                                    {
                                                        card.Layers(layer =>
                                                        {
                                                            layer.Layer().Background(Colors.Black);
                                                            if (fbytes != null)
                                                            {
                                                                var img =
                                                                    fbytes; // make it non-nullable to pick the right overload
                                                                layer.PrimaryLayer().Image(img).FitArea();
                                                            }
                                                            else
                                                            {
                                                                layer.PrimaryLayer().Background("#eee");
                                                            }

                                                            const float nib = 4f; // shorter
                                                            const float bite = 0.50f; // thinner
                                                            layer.Layer().Element(x =>
                                                                x.AlignTop().AlignLeft().Width(nib).Height(bite)
                                                                    .Background(Colors.White));
                                                            layer.Layer().Element(x =>
                                                                x.AlignTop().AlignLeft().Width(bite).Height(nib)
                                                                    .Background(Colors.White));
                                                            layer.Layer().Element(x =>
                                                                x.AlignTop().AlignRight().Width(nib).Height(bite)
                                                                    .Background(Colors.White));
                                                            layer.Layer().Element(x =>
                                                                x.AlignTop().AlignRight().Width(bite).Height(nib)
                                                                    .Background(Colors.White));
                                                            layer.Layer().Element(x =>
                                                                x.AlignBottom().AlignLeft().Width(nib).Height(bite)
                                                                    .Background(Colors.White));
                                                            layer.Layer().Element(x =>
                                                                x.AlignBottom().AlignLeft().Width(bite).Height(nib)
                                                                    .Background(Colors.White));
                                                            layer.Layer().Element(x =>
                                                                x.AlignBottom().AlignRight().Width(nib).Height(bite)
                                                                    .Background(Colors.White));
                                                            layer.Layer().Element(x =>
                                                                x.AlignBottom().AlignRight().Width(bite).Height(nib)
                                                                    .Background(Colors.White));
                                                        });
                                                    }
                                                    else
                                                    {
                                                        if (fbytes != null) card.Image(fbytes).FitArea();
                                                        else card.Background("#000");
                                                    }
                                                }
                                                else
                                                {
                                                    e.Border(0.25f).BorderColor("#000").Width(cardW).Height(cardH);
                                                }
                                            });
                                        });
                                    }
                                });
                        });
                });

                // BACK PAGE (if enabled)
                if (request.IncludeBackPages)
                {
                    var pageBacksRaw = p < backsPages.Count ? backsPages[p] : new List<string>();

                    // Build a 9-slot list of backs, mirrored to align during duplex print.
                    // Only fill slots for positions that have a FRONT on this page.
                    var mirroredBacks = Enumerable.Repeat<string?>(null, 9).ToList();
                    var mirrorHoriz = request.MirrorBackHorizontally; // long-edge typical

                    for (var i = 0; i < pageFronts.Count; i++)
                    {
                        int r = i / 3, c = i % 3;

                        // Map front index -> back index (mirroring)
                        var j = mirrorHoriz
                            ? r * 3 + (2 - c) // mirror columns (long edge)
                            : (2 - r) * 3 + c; // mirror rows (short edge)

                        // Select back for this front (default back only when there IS a front)
                        var backUrl =
                            i < pageBacksRaw.Count && !string.IsNullOrWhiteSpace(pageBacksRaw[i])
                                ? pageBacksRaw[i]
                                : request.DefaultBackImageUrl;

                        mirroredBacks[j] = backUrl;
                    }

                    doc.Page(page =>
                    {
                        page.Size(pageSize);
                        page.Margin(0);
                        page.DefaultTextStyle(t => t.FontSize(10));

                        // Cut lines
                        page.Background().Layers(layers =>
                        {
                            layers.PrimaryLayer().Element(_ => { });

                            if (!fullPageCutLines) return;

                            var gridLeft = outer + (pageW - 2 * outer - gridW) / 2f;
                            var gridTop = outer + (pageH - 2 * outer - gridH) / 2f;

                            var x0 = gridLeft;
                            var x1 = gridLeft + cardW;
                            var x2 = x1 + gutter + cardW;
                            var x3 = x2 + gutter + cardW;

                            var y0 = gridTop;
                            var y1 = gridTop + cardH;
                            var y2 = y1 + gutter + cardH;
                            var y3 = y2 + gutter + cardH;

                            const float lineThick = 0.5f;

                            layers.Layer().Element(e =>
                                e.PaddingLeft(x0).AlignLeft().Width(lineThick).Height(pageH).Background(Colors.Black));
                            layers.Layer().Element(e =>
                                e.PaddingLeft(x1).AlignLeft().Width(lineThick).Height(pageH).Background(Colors.Black));
                            layers.Layer().Element(e =>
                                e.PaddingLeft(x2).AlignLeft().Width(lineThick).Height(pageH).Background(Colors.Black));
                            layers.Layer().Element(e =>
                                e.PaddingLeft(x3).AlignLeft().Width(lineThick).Height(pageH).Background(Colors.Black));

                            layers.Layer().Element(e =>
                                e.PaddingTop(y0).AlignTop().Height(lineThick).Width(pageW).Background(Colors.Black));
                            layers.Layer().Element(e =>
                                e.PaddingTop(y1).AlignTop().Height(lineThick).Width(pageW).Background(Colors.Black));
                            layers.Layer().Element(e =>
                                e.PaddingTop(y2).AlignTop().Height(lineThick).Width(pageW).Background(Colors.Black));
                            layers.Layer().Element(e =>
                                e.PaddingTop(y3).AlignTop().Height(lineThick).Width(pageW).Background(Colors.Black));
                        });

                        // Grid
                        page.Content().Padding(outer)
                            .AlignCenter().AlignMiddle()
                            .Width(gridW).Height(gridH)
                            .Column(col =>
                            {
                                for (var r = 0; r < 3; r++)
                                    col.Item().Height(cardH).Row(row =>
                                    {
                                        row.Spacing(gutter);
                                        for (var c = 0; c < 3; c++)
                                        {
                                            var idx = r * 3 + c;
                                            row.ConstantItem(cardW).Element(cell =>
                                            {
                                                cell.AlignCenter().AlignMiddle().Element(e =>
                                                {
                                                    // Only draw if this slot corresponds to a front
                                                    var backUrl = idx < mirroredBacks.Count ? mirroredBacks[idx] : null;
                                                    if (string.IsNullOrWhiteSpace(backUrl))
                                                    {
                                                        // leave empty slot
                                                        e.Width(cardW).Height(cardH);
                                                        return;
                                                    }

                                                    var bbytes = GetBytes(backUrl);
                                                    var card = e.Width(cardW).Height(cardH);

                                                    if (useBlackCorners)
                                                    {
                                                        card.Layers(layer =>
                                                        {
                                                            layer.Layer().Background(Colors.Black);
                                                            if (bbytes != null)
                                                            {
                                                                var img = bbytes;
                                                                layer.PrimaryLayer().Image(img).FitArea();
                                                            }
                                                            else
                                                            {
                                                                layer.PrimaryLayer().Background("#eee");
                                                            }

                                                            const float nib = 4f, bite = 0.50f;
                                                            layer.Layer().Element(x =>
                                                                x.AlignTop().AlignLeft().Width(nib).Height(bite)
                                                                    .Background(Colors.White));
                                                            layer.Layer().Element(x =>
                                                                x.AlignTop().AlignLeft().Width(bite).Height(nib)
                                                                    .Background(Colors.White));
                                                            layer.Layer().Element(x =>
                                                                x.AlignTop().AlignRight().Width(nib).Height(bite)
                                                                    .Background(Colors.White));
                                                            layer.Layer().Element(x =>
                                                                x.AlignTop().AlignRight().Width(bite).Height(nib)
                                                                    .Background(Colors.White));
                                                            layer.Layer().Element(x =>
                                                                x.AlignBottom().AlignLeft().Width(nib).Height(bite)
                                                                    .Background(Colors.White));
                                                            layer.Layer().Element(x =>
                                                                x.AlignBottom().AlignLeft().Width(bite).Height(nib)
                                                                    .Background(Colors.White));
                                                            layer.Layer().Element(x =>
                                                                x.AlignBottom().AlignRight().Width(nib).Height(bite)
                                                                    .Background(Colors.White));
                                                            layer.Layer().Element(x =>
                                                                x.AlignBottom().AlignRight().Width(bite).Height(nib)
                                                                    .Background(Colors.White));
                                                        });
                                                    }
                                                    else
                                                    {
                                                        if (bbytes != null)
                                                        {
                                                            var img = bbytes;
                                                            card.Layers(l =>
                                                            {
                                                                l.Layer().Background(Colors.Black);      // fills any sub-pixel sliver
                                                                l.PrimaryLayer().Image(img).FitArea();   // keep aspect, no visible white line
                                                            });
                                                        }
                                                        else
                                                        {
                                                            card.Background("#eee");
                                                        }
                                                    }
                                                });
                                            });
                                        }
                                    });
                            });
                    });
                }
            }
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"{deckNameSafe}.pdf");
    }

    private static async Task<byte[]> LoadImageBytesAsync(string url, HttpClient http, string webRootPath)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Array.Empty<byte>();

        // data: URI support (rare but easy to handle)
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return ParseDataUrl(url);

        // absolute http(s)
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return await http.GetByteArrayAsync(uri);

        // otherwise: treat as a webroot-relative path, e.g. "/img/proxy-back.png"
        var rel = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(webRootPath, rel);

        if (!System.IO.File.Exists(full))
            throw new FileNotFoundException($"Local image not found: {full}", full);

        return await System.IO.File.ReadAllBytesAsync(full);
    }

    private static byte[] ParseDataUrl(string dataUrl)
    {
        // format: data:[<mediatype>][;base64],<data>
        var comma = dataUrl.IndexOf(',');
        if (comma < 0) return Array.Empty<byte>();

        var meta = dataUrl.Substring(5, comma - 5); // after "data:"
        var payload = dataUrl[(comma + 1)..];

        return meta.Contains(";base64", StringComparison.OrdinalIgnoreCase)
            ? Convert.FromBase64String(payload)
            : Encoding.UTF8.GetBytes(payload);
    }

    private static List<string> NormalizeBacks(List<string> fronts, List<string> backs, string? defaultBack)
    {
        var result = new List<string>(fronts.Count);
        for (var i = 0; i < fronts.Count; i++)
        {
            var b = i < backs.Count ? backs[i] : null;
            if (string.IsNullOrWhiteSpace(b)) b = defaultBack ?? "";
            result.Add(b);
        }

        return result;
    }

    private static string Sanitize(string s)
    {
        foreach (var ch in Path.GetInvalidFileNameChars())
            s = s.Replace(ch, '_');
        return s.Trim();
    }

    public class SaveDeckRequest
    {
        public string DeckName { get; set; } = "";
        public List<string> ImageUrls { get; set; } = new(); // fronts (flattened by qty)

        public List<string> BackImageUrls { get; set; } = new();

        public bool IncludeBackPages { get; set; } = false;
        public string? DefaultBackImageUrl { get; set; }
        public bool MirrorBackHorizontally { get; set; } = true;

        // existing options
        public bool UseBlackCorners { get; set; } = false;
        public bool FullPageCutLines { get; set; } = false;
    }
}