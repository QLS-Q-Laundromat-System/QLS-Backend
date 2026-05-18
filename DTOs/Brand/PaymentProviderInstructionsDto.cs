using System.Collections.Generic;

namespace QLS.Backend.DTOs.Brand
{
    public class PaymentProviderInstructionsDto
    {
        public string Provider { get; set; } = string.Empty;
        public string WebhookUrl { get; set; } = string.Empty;
        public List<InstructionStepDto> Steps { get; set; } = new();
    }

    public class InstructionStepDto
    {
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
