using System.ComponentModel.DataAnnotations;

namespace QLS.Backend.DTOs.Zalo
{
    public class ZaloLoginRequestDto
    {
        public Guid? BrandId { get; set; }

        [Required]
        public string AccessToken { get; set; } = string.Empty;
    }
}
