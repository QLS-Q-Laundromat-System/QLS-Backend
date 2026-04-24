# 🐘 Kết nối pgAdmin với PostgreSQL trên Oracle Cloud

> Cấu hình **1 lần duy nhất** — sau đó double-click là kết nối, không cần terminal.

---

## Thông tin server

| | |
|---|---|
| **Server IP** | `161.118.238.100` |
| **SSH User** | `ubuntu` |
| **SSH Key** | `C:\Users\{TÊN_MÁY_BẠN}\.ssh\oracle_key.pem` |
| **DB Name** | `QLS` |
| **DB User** | `qls_user` |
| **DB Password** | `QLS@Oracle2026!` |

---

## Các bước cấu hình

### Bước 1 — Mở pgAdmin → Register Server

Chuột phải vào **Servers** → **Register** → **Server...**

---

### Bước 2 — Tab "General"

| Trường | Giá trị |
|---|---|
| **Name** | `QLS Oracle Cloud` |

---

### Bước 3 — Tab "Connection"

| Trường | Giá trị |
|---|---|
| **Host name/address** | `localhost` |
| **Port** | `5432` |
| **Maintenance database** | `QLS` |
| **Username** | `qls_user` |
| **Password** | `QLS@Oracle2026!` |
| **Save password** | ✅ Tick vào |

---

### Bước 4 — Tab "SSH Tunnel" ⬅️ Quan trọng!

| Trường | Giá trị |
|---|---|
| **Use SSH tunneling** | ✅ Bật ON |
| **Tunnel host** | `161.118.238.100` |
| **Tunnel port** | `22` |
| **Username** | `ubuntu` |
| **Authentication** | `Identity file` |
| **Identity file** | Đường dẫn đến file `.pem` trên máy bạn |

---

### Bước 5 — Nhấn **Save**

Sau đó **double-click** vào `QLS Oracle Cloud` trong danh sách Servers là kết nối ngay!

---

## Danh sách bảng đã có trong DB

| Bảng | Mô tả |
|---|---|
| `Accounts` | Tài khoản |
| `Brands` | Thương hiệu |
| `MachineSessions` | Phiên máy giặt |
| `Machines` | Máy giặt |
| `Stores` | Cửa hàng |
| `Users` | Người dùng |
| `__EFMigrationsHistory` | Lịch sử migration EF Core |

---

- SSH Key phải có đúng quyền (Chỉ mình bạn được đọc). Nếu báo lỗi "Permissions are too open", hãy mở PowerShell và chạy:
  ```powershell
  # 1. Di chuyển vào thư mục chứa key (Ví dụ: Downloads hoặc .ssh)
  cd C:\Users\$env:USERNAME\.ssh

  # 2. Chạy lệnh phân quyền (Đảm bảo file oracle_key.pem nằm trong thư mục này)
  icacls .\oracle_key.pem /inheritance:r /grant:r "$($env:USERNAME):R"
  ```
- Port `5432` của postgres **không expose ra internet** (chỉ bind `127.0.0.1`) — pgAdmin kết nối qua SSH Tunnel nên cực kỳ an toàn.
