using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Models;

public class TimeSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(100)]
    public required string Name { get; set; } // Ví dụ: "Giờ cao điểm", "Happy Hour Mùa Hè"

    // TimeOnly rất hoàn hảo để mapping với kiểu 'time' trong SQL
    // Để Nullable (?) trong trường hợp slot này chỉ ràng buộc ngày, không ràng buộc giờ
    public TimeOnly? StartTime { get; set; } 
    public TimeOnly? EndTime { get; set; }

    // Sử dụng lại Enum [Flags] chúng ta đã định nghĩa ở trước
    public DayOfWeekMask DayMask { get; set; } = DayOfWeekMask.AllDays;

    public Guid BrandId { get; set; }
    public Brand? Brand { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation property 1:N
    public ICollection<PriceModePerSession> PriceModesPerSession { get; set; } = new List<PriceModePerSession>();
}
