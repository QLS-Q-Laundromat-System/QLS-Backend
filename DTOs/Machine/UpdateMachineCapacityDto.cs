using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.DTOs.Machine;

public class UpdateMachineCapacityDto
{
    [Required(ErrorMessage = "Công suất máy (Capacity) không được để trống")]
    [MaxLength(50)]
    public string Capacity { get; set; } = string.Empty;
}
