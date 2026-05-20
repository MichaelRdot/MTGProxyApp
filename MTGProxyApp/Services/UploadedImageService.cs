namespace MTGProxyApp.Services;

public class UploadedImageService
{
    private readonly Dictionary<string, (byte[] Data, string MimeType)> _images = new();

    public string Store(byte[] data, string mimeType)
    {
        var id = Guid.NewGuid().ToString("N");
        _images[id] = (data, mimeType);
        return id;
    }

    public (byte[] Data, string MimeType)? Get(string id) =>
        _images.TryGetValue(id, out var img) ? img : null;
}
