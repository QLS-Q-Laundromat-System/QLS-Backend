using QLS.Backend.Models.Enums;
using System;

namespace QLS.Backend.DTOs.Machine
{
    public class UpdateSessionStatusDto
    {
        public MachineSessionStatus Status { get; set; }

        /// <summary>
        /// Ghi chú lý do (dùng khi Status = Error để giải thích sự cố).
        /// VD: "Máy dừng đột ngột sau 5 phút vận hành."
        /// </summary>
        public string? RefundNote { get; set; }
    }
}

