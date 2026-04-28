using System;

namespace QLS.Backend.DTOs.Store
{
    public class UpdateStoreTypeInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }
}
