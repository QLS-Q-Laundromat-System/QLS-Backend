using System.Text.Json.Serialization;

namespace QLS.Backend.DTOs.Lg
{
    public class LgStoreCreateResponse
    {
        [JsonPropertyName("resultCode")]
        public string ResultCode { get; set; } = string.Empty;

        [JsonPropertyName("result")]
        public LgStoreCreateResult Result { get; set; } = new();
    }

    public class LgStoreCreateResult
    {
        [JsonPropertyName("storeId")]
        public string StoreId { get; set; } = string.Empty;

        [JsonPropertyName("pinCode")]
        public string PinCode { get; set; } = string.Empty;
    }
}
