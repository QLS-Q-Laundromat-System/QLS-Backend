using System;

namespace QLS.Backend.DTOs.Store
{
    public class StoreSettingDto
    {
        public int Id { get; set; }
        public Guid StoreId { get; set; }

        // --- MÁY SẤY ---
        public int DryerStepMinutes { get; set; }
        public decimal DryerStepPrice { get; set; }
        public int DryerMinInitialMinutes { get; set; }
        public int DryerGracePeriodMinutes { get; set; }
    }
}
