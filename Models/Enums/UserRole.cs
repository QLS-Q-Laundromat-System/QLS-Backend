namespace QLS.Backend.Models.Enums
{
    public enum UserRole
    {
        SuperAdmin = 0, // Admin tổng hệ thống (bạn)
        AdminBranch = 1,      // Chủ chuỗi cửa hàng
        Manager = 2,    // Quản lý chi nhánh
        Staff = 3,      // Nhân viên cửa hàng
        Customer = 4    // Khách hàng dùng app người dùng
    }
}
