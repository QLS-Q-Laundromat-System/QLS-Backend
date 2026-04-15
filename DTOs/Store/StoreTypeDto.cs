using System;

namespace QLS.Backend.DTOs.Store
{
    public class StoreTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public bool IsActive { get; set; }
        public Guid BrandId { get; set; }
    }
}
