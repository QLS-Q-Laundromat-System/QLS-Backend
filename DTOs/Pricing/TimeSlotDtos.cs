using QLS.Backend.Models.Enums;

namespace QLS.Backend.DTOs.Pricing;

public class TimeSlotDto
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public DayOfWeekMask DayMask { get; set; }
    public bool IsActive { get; set; }
}

public class CreateTimeSlotDto
{
    public Guid BrandId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public DayOfWeekMask DayMask { get; set; } = DayOfWeekMask.AllDays;
}
