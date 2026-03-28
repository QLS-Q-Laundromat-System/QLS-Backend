using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // Giặt, Sấy, Giặt giày...

    [MaxLength(50)]
    public string Capacity { get; set; } = string.Empty; // Ví dụ: 10kg, 15kg
}
