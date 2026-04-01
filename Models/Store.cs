using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.Models;

public class Store
{
    [Key]
    [MaxLength(50)]
    public string StoreId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Machine> Machines { get; set; } = new List<Machine>();
    public StoreSetting? StoreSetting { get; set; }
    public ICollection<MachineSession> MachineSessions { get; set; } = new List<MachineSession>();
}
