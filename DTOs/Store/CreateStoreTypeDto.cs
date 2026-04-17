using System;

namespace QLS.Backend.DTOs.Store
{
    public class CreateStoreTypeDto
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public Guid? BrandId { get; set; }
    }
}
