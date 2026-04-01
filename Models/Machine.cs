using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QLS.Backend.Models.Enums;

namespace QLS.Backend.Models;

public class Machine
{
    [Key]
    public string MachineId { get; set; } = string.Empty;

    [Required]
    public string StoreId { get; set; } = string.Empty;

    [ForeignKey("StoreId")]
    public Store? Store { get; set; }

    [Required]
    public MachineType Type { get; set; } = MachineType.Giat; // Giặt, Sấy

    [MaxLength(50)]
    public string Capacity { get; set; } = string.Empty; // Ví dụ: 10kg, 15kg

    public ICollection<MachineSession> Sessions { get; set; } = new List<MachineSession>();
}
