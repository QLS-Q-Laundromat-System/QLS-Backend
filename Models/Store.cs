using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.Models;

public class Store
{
    [Key]
    public string StoreId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string StoreName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh"; 

    // Quan hệ 1-N: 1 Store có nhiều Machines
    public ICollection<Machine> Machines { get; set; } = new List<Machine>();
}
