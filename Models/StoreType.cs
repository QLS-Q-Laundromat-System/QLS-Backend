using System;
using System.Collections.Generic;

namespace QLS.Backend.Models
{
    public class StoreType
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; } = 1; 
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- THÊM LIÊN KẾT TỚI BRAND ---
        // Ràng buộc hạng này thuộc về Brand nào
        public Guid BrandId { get; set; }
        public Brand? Brand { get; set; }

        public ICollection<Store> Stores { get; set; } = new List<Store>();
        public ICollection<PriceListStoreType> PriceListStoreTypes { get; set; } = new List<PriceListStoreType>();
    }
}
