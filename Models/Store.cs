using System;
using System.Collections.Generic;

namespace QLS.Backend.Models
{
    public class Store
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid BrandId { get; set; }
        public Brand? Brand { get; set; }
        public ICollection<Machine> Machines { get; set; } = new List<Machine>();
        public StoreSetting? StoreSetting { get; set; }
    }
}
