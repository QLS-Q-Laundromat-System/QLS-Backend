using System.Threading.Tasks;

namespace QLS.Backend.Services.Payment
{
    public interface IPaymentProviderValidator
    {
        string ProviderName { get; }
        Task<bool> VerifyCredentialsAsync(string apiKey);
        Task<bool> VerifyBankAccountAsync(string apiKey, string accountNumber);
    }
}
