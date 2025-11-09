using System.Text.Json.Serialization;

namespace MTGProxyApp.Dtos;

public class CardDto
{
    [JsonPropertyName("card_faces")] public CardFaceDto[]? CardFaces { get; set; }
    [JsonPropertyName("image_uris")] public CardImageUrisDto? ImageUris { get; set; }
    [JsonPropertyName("card_back_id")] public string? CardBackId { get; set; }
    [JsonPropertyName("prints_search_uri")] public Uri? PrintsSearchUri { get; set; }
    [JsonPropertyName("set")] public string? Set { get; set; }
    [JsonPropertyName("set_id")] public  string? SetId { get; set; }
    [JsonPropertyName("image_status")] public string? ImageStatus { get; set; }
    [JsonPropertyName("id")] 
    public string? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
}