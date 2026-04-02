using System;

namespace QLS.Backend.Models
{
    public class Branch
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid OwnerId { get; set; }
        public Owner? Owner { get; set; }

        public ICollection<UserAdmin> UserAdmins { get; set; } = new List<UserAdmin>();
        public ICollection<Machine> Machines { get; set; } = new List<Machine>();
        public BranchSetting? BranchSetting { get; set; }
    }
}
