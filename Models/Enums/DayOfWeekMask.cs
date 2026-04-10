using System;

namespace QLS.Backend.Models.Enums;

[Flags]
public enum DayOfWeekMask : short
{
    None = 0,
    Monday    = 1 << 0, // Giá trị: 1
    Tuesday   = 1 << 1, // Giá trị: 2
    Wednesday = 1 << 2, // Giá trị: 4
    Thursday  = 1 << 3, // Giá trị: 8
    Friday    = 1 << 4, // Giá trị: 16
    Saturday  = 1 << 5, // Giá trị: 32 (T7)
    Sunday    = 1 << 6, // Giá trị: 64 (CN)
    
    // Tiện ích gộp sẵn
    Weekend = Saturday | Sunday,                     // Trị giá: 96
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday, // Trị giá: 31
    AllDays = Weekend | Weekdays                     // Trị giá: 127
}
