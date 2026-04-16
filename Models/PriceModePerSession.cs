using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Models;

// 1. CLASS CHA (Bắt buộc phải là abstract)
// Chứa những thuộc tính mà CẢ MÁY GIẶT LẪN MÁY SẤY đều phải có.
public abstract class PriceModePerSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required Guid PriceListId { get; set; }
    public PriceList? PriceList { get; set; }

    // Lưu ý: Mình BỎ cột MachineType ở đây vì Entity Framework 
    // sẽ tự động quản lý nó qua cơ chế Discriminator (phân tích ở Bước 2).

    [Precision(5, 2)]
    public required decimal MachineCapacityKg { get; set; } // Giữ lại vì máy nào cũng có công suất

    [Precision(12, 0)]
    public required decimal Price { get; set; }

    public required int DurationMinutes { get; set; }

    public Guid? TimeSlotId { get; set; }
    public TimeSlot? TimeSlot { get; set; }
}

// 2. CLASS CON: MÁY GIẶT
public class WasherPriceMode : PriceModePerSession
{
    // Bê cột CycleName xuống đây vì chỉ máy giặt mới có chu trình vắt/xả/nóng
    [MaxLength(100)]
    public string? CycleName { get; set; } 
}

// 3. CLASS CON: MÁY SẤY
public class DryerPriceMode : PriceModePerSession
{
    // Thêm các luật chơi riêng của máy sấy vào đây
    // Dùng int? (Nullable) để các Brand không bắt buộc phải nhập
    public int? MinInitialSteps { get; set; } 
    public int? ExtensionTimeoutMinutes { get; set; }
}
