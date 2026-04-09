using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Models;

public class Machine
{
    [Key]
    public string MachineId { get; set; } = string.Empty;

    [Required]
    public Guid StoreId { get; set; }

    [Required]
    public MachineType Type { get; set; } = MachineType.Washer; // Giặt, Sấy

    [MaxLength(50)]
    public string Capacity { get; set; } = string.Empty; // Ví dụ: 10kg, 15kg
}
