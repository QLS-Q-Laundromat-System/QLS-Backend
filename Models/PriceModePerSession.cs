using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Models;

public class PriceModePerSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid PriceListId { get; set; }
    public PriceList? PriceList { get; set; }

    public MachineType MachineType { get; set; } = MachineType.Washer;

    [Precision(5, 2)]
    public required decimal MachineCapacityKg { get; set; }

    [Precision(12, 0)]
    public required decimal Price { get; set; }

    public required int DurationMinutes { get; set; }

    // --- LIÊN KẾT TỚI TIMESLOT (RÀNG BUỘC THỜI GIAN) ---
    // Khóa ngoại Nullable (Guid?) vì như bạn nói, nó là Optional
    // Nếu TimeSlotId = null -> Áp dụng mọi lúc mọi nơi
    // Nếu có giá trị -> Chỉ có hiệu lực trong khung giờ đó
    public Guid? TimeSlotId { get; set; }
    public TimeSlot? TimeSlot { get; set; }
    // ---------------------------------------------------
}
