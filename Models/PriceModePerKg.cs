using Microsoft.EntityFrameworkCore;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Models;

public class PriceModePerKg
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Liên kết 1-N về Bảng giá (PriceList)
    public required Guid PriceListId { get; set; }
    public PriceList? PriceList { get; set; }

    public MachineType MachineType { get; set; } = MachineType.Washer;

    // Khoảng cân nặng (Ví dụ: Min = 0, Max = 3)
    [Precision(5, 2)] // Yêu cầu EF Core tạo cột decimal(5,2)
    public required decimal MinKg { get; set; }

    [Precision(5, 2)] 
    public decimal? MaxKg { get; set; } // Nullable để đại diện cho vô cực (∞)

    // Đơn giá
    [Precision(12, 0)] // Yêu cầu EF Core tạo cột decimal(12,0) như bạn thiết kế
    public required decimal UnitPrice { get; set; }

    // Phương thức tính giá (Trọn gói hay Tính theo Kg)
    public PricePerType PricePer { get; set; } = PricePerType.PerKg;

    // Thứ tự sắp xếp / Thứ tự ưu tiên tính toán bậc thang
    public int SortOrder { get; set; } = 0;
}
