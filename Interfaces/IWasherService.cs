using QLS.Backend.DTOs;

namespace QLS.Backend.Interfaces;

public interface IWasherService
{
    Task<WasherStatusDto> GetWasherStatusAsync(string deviceId);
}
