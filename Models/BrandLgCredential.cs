using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLS.Backend.Models
{

    public class BrandLgCredential
    {
        /// <summary>Khóa chính kiêm Khóa ngoại trỏ tới Brand (quan hệ 1-1).</summary>
        [Key]
        public Guid BrandId { get; set; }

        /// <summary>Email đăng nhập tài khoản LG của Brand.</summary>
        [Required]
        [MaxLength(255)]
        public string LgEmail { get; set; } = string.Empty;

    
        [Required]
        public string UserAuthHash { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? LgUserNo { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }

       
        public DateTime? TokenExpiresAt { get; set; }
        public string? Oauth2BackendUrl { get; set; }

        /// <summary>Thời điểm cập nhật thông tin gần nhất (UTC).</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation Property ───────────────────────────────────────────────
        [ForeignKey(nameof(BrandId))]
        public Brand? Brand { get; set; }
    }
}
