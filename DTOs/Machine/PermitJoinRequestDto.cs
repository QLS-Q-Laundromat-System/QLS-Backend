using System;

namespace QLS.Backend.DTOs.Machine
{
    public class PermitJoinRequestDto
    {
        public Guid StoreId { get; set; }
        public bool Permit { get; set; }
        public int DurationSeconds { get; set; } = 120;
    }
}
