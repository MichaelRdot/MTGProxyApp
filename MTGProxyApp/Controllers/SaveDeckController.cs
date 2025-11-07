using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace MTGProxyApp.Controllers;

[ApiController]
[Route("api/save-deck")]
public class SaveDeckController : ControllerBase
{
    public class SaveDeckRequest
    {
        public string DeckName { get; set; } = "";
        public List<string> ImageUrls { get; set; } = new();
    }

    [HttpPost]
    public async Task<IActionResult> SaveDeck([FromBody] SaveDeckRequest req, [FromServices] IHttpClientFactory factory)
    {
        if (string.IsNullOrWhiteSpace(req.DeckName) || req.ImageUrls.Count == 0)
            return BadRequest("Missing deck name or images.");

        var deckRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "decks", Sanitize(req.DeckName));
        Directory.CreateDirectory(deckRoot);

        var client = factory.CreateClient();
        var i = 1;
        foreach (var url in req.ImageUrls)
        {
            var bytes = await client.GetByteArrayAsync(url);
            var filename = Path.Combine(deckRoot, $"{i:D3}.png");
            await System.IO.File.WriteAllBytesAsync(filename, bytes);
            i++;
        }

        // Create a zip in-memory and return it
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var file in Directory.GetFiles(deckRoot, "*.png"))
            {
                var entry = zip.CreateEntry(Path.GetFileName(file), CompressionLevel.Optimal);
                await using var src = System.IO.File.OpenRead(file);
                await using var dst = entry.Open();
                await src.CopyToAsync(dst);
            }
        }

        ms.Position = 0;
        var zipName = $"{Sanitize(req.DeckName)}.zip";
        return File(ms.ToArray(), "application/zip", zipName);
    }

    private static string Sanitize(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Trim();
    }
}