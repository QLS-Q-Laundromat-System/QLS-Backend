using QLS.Backend.Models.Enums;
using System;

namespace QLS.Backend.DTOs.Machine
{
    public class UpdateSessionStatusDto
    {
        public MachineSessionStatus Status { get; set; }
    }
}
