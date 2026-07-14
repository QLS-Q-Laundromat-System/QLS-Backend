using System;

namespace QLS.Backend.DTOs.Machine
{
    public class SetupZigbeeRequestDto
    {
        public Guid MachineId { get; set; }
        public string IeeeAddress { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
    }
}
