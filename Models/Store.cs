using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.Models;

public class Store
{
    [Key]
    public string StoreId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Machine> Machines { get; set; } = new List<Machine>();
}
