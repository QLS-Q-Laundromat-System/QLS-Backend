namespace QLS.Backend.DTOs.Dryer
{
    public class DryerOptionResponseDto
    {
        public bool IsExtendSession { get; set; }
        public int MinMinutesAllowed { get; set; }
        public int StepMinutes { get; set; }
        public decimal PricePerStep { get; set; }
    }
}
