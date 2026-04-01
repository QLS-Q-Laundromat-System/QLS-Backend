namespace QLS.Backend.DTOs.Dryer
{
    public class DryerOptionResponseDto
    {
        public bool IsExtendSession { get; set; } // true = Sấy tiếp, false = Sấy mới
        public int MinMinutesAllowed { get; set; } // Tối thiểu được chọn (10 hoặc 30)
        public int StepMinutes { get; set; } // Bước nhảy (10)
        public decimal PricePerStep { get; set; } // Giá mỗi bước (13000)
    }
}
