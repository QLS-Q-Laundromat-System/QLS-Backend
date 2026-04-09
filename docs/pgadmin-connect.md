# 🐘 Kết nối pgAdmin với PostgreSQL trên Oracle Cloud

> Cấu hình **1 lần duy nhất** — sau đó double-click là kết nối, không cần terminal.

---

## Thông tin server

| | |
|---|---|
| **Server IP** | `161.118.238.100` |
| **SSH User** | `ubuntu` |
| **SSH Key** | `C:\Users\Nguyen Quoc An\.ssh\oracle_key.pem` |
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
| **Identity file** | `C:\Users\Nguyen Quoc An\.ssh\oracle_key.pem` |

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
| `StoreSettings` | Cài đặt cửa hàng |
| `Stores` | Cửa hàng |
| `Users` | Người dùng |
| `__EFMigrationsHistory` | Lịch sử migration EF Core |

---

## Lưu ý

- SSH Key phải có đúng quyền. Nếu lỗi permissions, chạy lệnh sau trong PowerShell:
  ```powershell
  # Lệnh này tự động dùng username của máy bạn (không cần đổi gì)
  icacls "C:\Users\$env:USERNAME\.ssh\oracle_key.pem" /inheritance:r /grant:r "$env:USERNAME`:R"
  ```
- Port `5432` của postgres **không expose ra internet** (chỉ bind `127.0.0.1`) — pgAdmin kết nối qua SSH Tunnel nên an toàn.
