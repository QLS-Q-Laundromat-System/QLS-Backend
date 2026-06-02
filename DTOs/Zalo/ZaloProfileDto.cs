using System.Text.Json.Serialization;

namespace QLS.Backend.DTOs.Zalo
{
    public class ZaloProfileDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture")]
        public ZaloPictureDto? Picture { get; set; }

        [JsonPropertyName("error")]
        public int Error { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        public string? AvatarUrl => Picture?.Data?.Url;
    }

    public class ZaloPictureDto
    {
        [JsonPropertyName("data")]
        public ZaloPictureDataDto? Data { get; set; }
    }

    public class ZaloPictureDataDto
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
