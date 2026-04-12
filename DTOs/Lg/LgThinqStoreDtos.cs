using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QLS.Backend.DTOs.Lg
{
    public class LgThinqStoreListResponse
    {
        public string ResultCode { get; set; } = string.Empty;
        public LgThinqStoreResult Result { get; set; } = new();
    }

    public class LgThinqStoreResult
    {
        public List<LgThinqStoreItem> Stores { get; set; } = new();
    }

    public class LgThinqStoreItem
    {
        public string StoreId { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public LgThinqAddress Address { get; set; } = new();
    }

    public class LgThinqAddress
    {
        public string Zipcode { get; set; } = string.Empty;
        public string Address1 { get; set; } = string.Empty;
        public string Address2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string States { get; set; } = string.Empty;

        public string FullAddress => $"{Address1} {Address2}, {States}, {City}".Trim().Trim(',').Trim();
    }
}
