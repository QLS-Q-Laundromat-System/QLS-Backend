using System;

namespace QLS.Backend.DTOs.Store
{
    public class BranchSettingDto
    {
        public int Id { get; set; }
        public Guid BranchId { get; set; }

        // --- MÁY SẤY ---
        public int DryerStepMinutes { get; set; }
        public decimal DryerStepPrice { get; set; }
        public int DryerMinInitialMinutes { get; set; }
        public int DryerGracePeriodMinutes { get; set; }
    }
}
