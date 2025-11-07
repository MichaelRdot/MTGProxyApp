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

        var safe = Sanitize(req.DeckName);
        var client = factory.CreateClient();

        // Build ZIP entirely in memory—no disk writes
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            int i = 1;
            foreach (var url in req.ImageUrls)
            {
                // Create ZIP entry name like 001.png, 002.png, ...
                var entry = zip.CreateEntry($"{i:D3}.png", CompressionLevel.NoCompression);
                await using var entryStream = entry.Open();

                // Stream the PNG from Scryfall directly into the ZIP entry
                await using var httpStream = await client.GetStreamAsync(url);
                await httpStream.CopyToAsync(entryStream);

                i++;
            }
        }

        ms.Position = 0; // reset for reading
        return File(ms, "application/zip", $"{safe}.zip");    
    }

    private static string Sanitize(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Trim();
    }
}