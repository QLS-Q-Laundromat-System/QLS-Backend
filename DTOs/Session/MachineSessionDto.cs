using System;

namespace QLS.Backend.DTOs.Session
{
    public class MachineSessionDto
    {
        public Guid Id { get; set; }
        public string StoreId { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Status { get; set; }
    }
}
