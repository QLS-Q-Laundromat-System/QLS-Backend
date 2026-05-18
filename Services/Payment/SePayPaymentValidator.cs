using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace QLS.Backend.Services.Payment
{
    public class SePayPaymentValidator : IPaymentProviderValidator
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string ProviderName => "SEPAY";

        public SePayPaymentValidator(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> VerifyCredentialsAsync(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return false;

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                
                // 1. Thử endpoint v2 Production của SePay
                var response = await client.GetAsync("https://userapi.sepay.vn/v2/transactions?per_page=1");
                if (response.IsSuccessStatusCode) return true;

                // 2. Thử endpoint v2 Sandbox của SePay (Kiểm thử)
                var responseSandbox = await client.GetAsync("https://userapi-sandbox.sepay.vn/v2/transactions?per_page=1");
                if (responseSandbox.IsSuccessStatusCode) return true;

                // 3. Dự phòng: Thử endpoint v1 cũ
                var responseV1 = await client.GetAsync("https://my.sepay.vn/api/transactions/list?limit=1");
                return responseV1.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> VerifyBankAccountAsync(string apiKey, string accountNumber)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(accountNumber)) return false;

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                // 1. Thử endpoint v2 Production để lấy danh sách tài khoản ngân hàng
                var response = await client.GetAsync("https://userapi.sepay.vn/v2/bank-accounts");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (CheckAccountNumberInJson(json, accountNumber)) return true;
                }

                // 2. Thử endpoint v2 Sandbox
                var responseSandbox = await client.GetAsync("https://userapi-sandbox.sepay.vn/v2/bank-accounts");
                if (responseSandbox.IsSuccessStatusCode)
                {
                    var json = await responseSandbox.Content.ReadAsStringAsync();
                    if (CheckAccountNumberInJson(json, accountNumber)) return true;
                }

                // 3. Dự phòng: Thử endpoint v1 cũ
                var responseV1 = await client.GetAsync("https://my.sepay.vn/userapi/bankaccounts/list");
                if (responseV1.IsSuccessStatusCode)
                {
                    var json = await responseV1.Content.ReadAsStringAsync();
                    if (CheckAccountNumberInJson(json, accountNumber)) return true;
                }
            }
            catch
            {
                // Bỏ qua lỗi mạng
            }

            return false;
        }

        private bool CheckAccountNumberInJson(string json, string targetAccountNumber)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return FindAccountNumber(doc.RootElement, targetAccountNumber.Trim());
            }
            catch
            {
                // Dự phòng tìm chuỗi trực tiếp nếu không parse được JSON
                return targetAccountNumber.Length >= 5 && json.Contains(targetAccountNumber);
            }
        }

        private bool FindAccountNumber(JsonElement element, string targetAccountNumber)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (FindAccountNumber(item, targetAccountNumber)) return true;
                }
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    var name = prop.Name.ToLower();
                    if (name == "account_number" || name == "accountnumber" || name == "bank_number" || name == "banknumber")
                    {
                        var val = prop.Value.ToString().Trim();
                        if (val == targetAccountNumber) return true;
                    }
                    else if (FindAccountNumber(prop.Value, targetAccountNumber))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
