using System.Collections.Concurrent;

namespace QLS.Backend.Services.Machine;

public interface IHardwareTrackerService
{
    void UpdateStatus(Guid sessionId, string status);
    string? GetStatus(Guid sessionId);
    void Remove(Guid sessionId);
}

public class HardwareTrackerService : IHardwareTrackerService
{
    // Lưu trữ tạm thời trạng thái phần cứng trong RAM
    private readonly ConcurrentDictionary<Guid, string> _statusCache = new();

    public void UpdateStatus(Guid sessionId, string status)
    {
        _statusCache[sessionId] = status;
    }

    public string? GetStatus(Guid sessionId)
    {
        return _statusCache.TryGetValue(sessionId, out var status) ? status : null;
    }

    public void Remove(Guid sessionId)
    {
        _statusCache.TryRemove(sessionId, out _);
    }
}
