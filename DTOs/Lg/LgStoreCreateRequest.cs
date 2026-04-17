using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QLS.Backend.DTOs.Lg
{
    public class LgStoreCreateRequest
    {
        [JsonPropertyName("request")]
        public LgStoreCreateData Request { get; set; } = new();
    }

    public class LgStoreCreateData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "VND";

        [JsonPropertyName("address1")]
        public string Address1 { get; set; } = string.Empty;

        [JsonPropertyName("address2")]
        public string Address2 { get; set; } = string.Empty;

        [JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;

        [JsonPropertyName("zipcode")]
        public string Zipcode { get; set; } = string.Empty;

        [JsonPropertyName("states")]
        public string States { get; set; } = string.Empty;

        [JsonPropertyName("lTime")]
        public string LTime { get; set; } = "Asia/Saigon";

        [JsonPropertyName("emails")]
        public List<string> Emails { get; set; } = new();

        [JsonPropertyName("introduction")]
        public string Introduction { get; set; } = string.Empty;

        [JsonPropertyName("information")]
        public string Information { get; set; } = string.Empty;

        [JsonPropertyName("photos")]
        public List<string> Photos { get; set; } = new();

        [JsonPropertyName("isReservationEnable")]
        public bool IsReservationEnable { get; set; } = true;

        [JsonPropertyName("options")]
        public LgStoreCreateOptions Options { get; set; } = new();

        [JsonPropertyName("storeName")]
        public string StoreName { get; set; } = string.Empty;

        [JsonPropertyName("longitude")]
        public List<string> Longitude { get; set; } = new(); // Contains [Lat, Lon] based on example

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }

    public class LgStoreCreateOptions
    {
        [JsonPropertyName("nation")]
        public string Nation { get; set; } = "VNM";

        [JsonPropertyName("reservationCount")]
        public int ReservationCount { get; set; } = 0;

        [JsonPropertyName("photos")]
        public List<string> Photos { get; set; } = new();
    }
}
