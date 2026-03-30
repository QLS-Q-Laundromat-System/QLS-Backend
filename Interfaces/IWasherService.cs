using QLS.Backend.DTOs;

namespace QLS.Backend.Interfaces;

public interface IWasherService
{
    Task<List<WasherStatusDto>> GetWasherStatusAsync(string storeId);
}
