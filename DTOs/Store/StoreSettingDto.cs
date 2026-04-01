namespace QLS.Backend.DTOs.Store
{
    public class StoreSettingDto
    {
        public int Id { get; set; }
        public string StoreId { get; set; } = string.Empty;

        // --- MÁY SẤY ---
        public int DryerStepMinutes { get; set; }
        public decimal DryerStepPrice { get; set; }
        public int DryerMinInitialMinutes { get; set; }
        public int DryerGracePeriodMinutes { get; set; }
    }
}
