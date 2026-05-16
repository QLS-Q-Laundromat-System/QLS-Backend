# Zalo Mini App Auth + Loyalty Point Flow

## 1. Mục tiêu

Luồng nghiệp vụ cần đáp ứng:

1. Người dùng thanh toán ngân hàng tại kiosk.
2. SePay xác nhận thanh toán thành công.
3. Backend cho máy chạy.
4. Kiosk hiển thị QR nhận điểm Zalo.
5. Người dùng quét QR bằng Zalo Mini App.
6. Backend cộng điểm cho tài khoản Zalo.

---

## 2. Auth Zalo (Mini App)

### 2.1 Dữ liệu FE lấy từ Zalo

FE Zalo Mini App gọi `getUserInfo()` và nhận:

- `id` (zaloUserId)
- `idByOA` (zaloOAUserId, có thể null)
- `name`
- `avatar`

### 2.2 API Backend login/auth

- Endpoint: `POST /api/zalo/auth/login`
- Mô tả: FE gửi thông tin user Zalo lên BE; BE lưu user và trả JWT backend.

Request body:

```json
{
  "zaloUserId": "123456789",
  "zaloOAUserId": "oa_user_id_or_null",
  "fullName": "Nguyen Van A",
  "avatarUrl": "https://..."
}
```

Response body:

```json
{
  "accessToken": "jwt_backend",
  "customerId": "uuid",
  "customerType": "Member",
  "studentVerificationStatus": "None",
  "totalPoints": 0
}
```

### 2.3 Nghiệp vụ xử lý BE

1. BE nhận request từ FE Mini App.
2. BE tìm `LoyaltyCustomer` theo `zaloUserId`.
3. Nếu chưa có:
   - Tạo mới `LoyaltyCustomer`.
   - Gán `customerType = Member`, `studentVerificationStatus = None`, `totalPoints = 0`.
4. Nếu đã có:
   - Update `fullName`, `avatarUrl`, `zaloOAUserId` (nếu thay đổi).
5. BE sinh JWT backend (`accessToken`) trả về FE.

> Ghi chú: nếu muốn giữ endpoint theo draft ban đầu thì có thể alias thêm `POST /api/zalo/auth/phone` trỏ cùng handler.

---

## 3. Luồng tích điểm sau thanh toán

### 3.1 Business flow

1. User thanh toán thành công tại kiosk.
2. SePay webhook gọi về backend.
3. Backend xác nhận `MachineSession` đã thanh toán.
4. Backend tạo `PointClaimToken`.
5. Kiosk hiển thị QR nhận điểm.
6. User mở Zalo Mini App quét QR.
7. FE Mini App gọi `POST /api/loyalty/claim`.
8. Backend kiểm tra token.
9. Backend cộng điểm cho `LoyaltyCustomer`.
10. Backend đánh dấu token đã dùng.

### 3.2 Dữ liệu session trả về cho kiosk

Sau khi thanh toán xong, kiosk gọi:

- `GET /api/v1/sessions/{id}`

Backend nên trả thêm phần loyalty:

```json
{
  "id": "session-id",
  "status": "Running",
  "loyalty": {
    "claimQrUrl": "https://zalo.me/s/miniapp?claimToken=ABC123XYZ",
    "expiredAt": "2026-05-12T10:30:00Z",
    "pointsToEarn": 5
  }
}
```

---

## 4. API loyalty

### 4.1 Nhận điểm

- Endpoint: `POST /api/loyalty/claim`
- Mô tả: nhận claim token sau khi user quét QR.

Request body (đề xuất):

```json
{
  "claimToken": "ABC123XYZ"
}
```

Response body (đề xuất):

```json
{
  "success": true,
  "pointsEarned": 5,
  "totalPoints": 125,
  "machineSessionId": "session-id"
}
```

### 4.2 Xem điểm hiện tại

- Endpoint: `GET /api/loyalty/me`
- Mô tả: lấy profile loyalty của user đang đăng nhập bằng JWT backend.

Response body (đề xuất):

```json
{
  "customerId": "uuid",
  "fullName": "Nguyen Van A",
  "customerType": "Member",
  "studentVerificationStatus": "None",
  "totalPoints": 125
}
```

### 4.3 Lịch sử tích điểm

- Endpoint: `GET /api/loyalty/points/history`
- Mô tả: trả lịch sử cộng/trừ điểm theo thời gian.

Response body (đề xuất):

```json
{
  "items": [
    {
      "id": "point_txn_id",
      "type": "Earn",
      "points": 5,
      "machineSessionId": "session-id",
      "description": "Tich diem sau thanh toan kiosk",
      "createdAt": "2026-05-12T10:15:00Z"
    }
  ]
}
```

---

## 5. Rule kiểm tra khi claim điểm (BE)

Khi gọi `POST /api/loyalty/claim`, backend cần kiểm tra:

1. Token tồn tại.
2. Token chưa hết hạn (`expiredAt > now`).
3. Token chưa dùng.
4. Session liên quan đã `PaymentConfirmed` và hợp lệ để cộng điểm.
5. Chưa từng cộng điểm cho session đó (anti-duplicate).

Nếu pass:

- Ghi transaction cộng điểm.
- Cập nhật `LoyaltyCustomer.totalPoints`.
- Mark token `isUsed = true`, `usedAt = now`, `usedByCustomerId`.

---

## 6. Notes triển khai

- `LoyaltyCustomer` là khách hàng dùng Zalo Mini App để tích điểm.
- FE Mini App giữ `accessToken` backend để gọi các API loyalty.
- QR chỉ chứa claim token (không chứa dữ liệu nhạy cảm).
- Nên dùng token ngắn hạn và một lần dùng (one-time token).

# QLS Loyalty + Zalo Mini App Specification

## Mục tiêu

Triển khai hệ thống Loyalty/Tích điểm cho Q-Laundry Station thông qua Zalo Mini App.

Luồng chính:

User thanh toán tại kiosk bằng ngân hàng  
→ SePay xác nhận thanh toán  
→ Backend trigger máy chạy  
→ Backend tạo QR nhận điểm  
→ User quét QR bằng Zalo Mini App  
→ Backend cộng điểm cho tài khoản Zalo

---

# Auth Zalo Flow

## Nghiệp vụ

1. User mở Zalo Mini App
2. FE gọi Zalo API: `getUserInfo()`
3. FE nhận:

- id
- idByOA
- name
- avatar

4. FE gọi:

POST /api/zalo/auth/login

5. Backend tìm theo `ZaloUserId`
6. Nếu chưa tồn tại → tạo LoyaltyCustomer
7. Nếu đã tồn tại → update thông tin
8. Backend trả JWT riêng

Request:

```json
{
  "zaloUserId": "123456789",
  "zaloOAUserId": "oa_user_id",
  "fullName": "Nguyen Van A",
  "avatarUrl": "https://..."
}
```

Response:

```json
{
  "accessToken": "jwt_backend",
  "customerId": "uuid",
  "customerType": "Member",
  "studentVerificationStatus": "None",
  "totalPoints": 0
}
```

---

# Loyalty Flow

1. User thanh toán tại kiosk
2. SePay webhook gọi backend
3. Backend xác nhận thanh toán
4. Trigger máy chạy
5. Tạo PointClaimToken
6. Kiosk hiển thị QR
7. User quét QR bằng Mini App
8. FE gọi:

POST /api/loyalty/claim

9. Backend validate token
10. Cộng điểm
11. Đánh dấu token đã dùng

---

# Rule nghiệp vụ

- QR sống 10 phút
- Mỗi QR chỉ dùng 1 lần
- Mỗi MachineSession chỉ cộng điểm 1 lần
- Điểm hết hạn sau 3 tháng
- 10.000 VNĐ = 1 điểm
- Điểm không có số lẻ
- Sinh viên giảm giá theo %
- Voucher loyalty tách riêng DiscountCode

Ví dụ:

19000đ => 1 điểm

25000đ => 2 điểm

50000đ => 5 điểm

Công thức:

```csharp
points = floor(amount / 10000)
```

---

# Entity: LoyaltyCustomer

```csharp
public class LoyaltyCustomer
{
    public Guid Id { get; set; }

    public string ZaloUserId { get; set; }

    public string? ZaloOAUserId { get; set; }

    public string? FullName { get; set; }

    public string? AvatarUrl { get; set; }

    public int TotalPoints { get; set; }

    public CustomerType CustomerType { get; set; }

    public StudentVerificationStatus StudentVerificationStatus { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

---

# Entity: PointClaimToken

```csharp
public class PointClaimToken
{
    public Guid Id { get; set; }

    public string Token { get; set; }

    public Guid MachineSessionId { get; set; }

    public Guid? PaymentTransactionId { get; set; }

    public decimal PaidAmount { get; set; }

    public int PointsToEarn { get; set; }

    public bool IsClaimed { get; set; }

    public DateTime ExpiredAt { get; set; }

    public Guid? ClaimedByCustomerId { get; set; }

    public DateTime? ClaimedAt { get; set; }
}
```

---

# Entity: LoyaltyPointTransaction

```csharp
public class LoyaltyPointTransaction
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid? MachineSessionId { get; set; }

    public PointTransactionType Type { get; set; }

    public int Points { get; set; }

    public int RemainingPoints { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

---

# APIs

## Auth

POST /api/zalo/auth/login

---

## Loyalty

POST /api/loyalty/claim

GET /api/loyalty/me

GET /api/loyalty/points/history

---

# Kiosk Session Response

GET /api/v1/sessions/{id}

```json
{
  "id": "session-id",
  "status": "Running",
  "loyalty": {
    "claimQrUrl": "https://zalo.me/s/miniapp?claimToken=ABC123XYZ",
    "expiredAt": "2026-05-12T10:30:00Z",
    "pointsToEarn": 5
  }
}
```

---

# Validation

Reject nếu:

- token không tồn tại
- token hết hạn
- token đã dùng
- session chưa thanh toán
- session cancelled
- session error
- customer không tồn tại
- giao dịch đã cộng điểm
- points = 0

---

# Database Constraint

```csharp
PointClaimToken.Token UNIQUE

MachineSessionId + Earn UNIQUE
```

Mục tiêu:

1 MachineSession chỉ được cộng điểm 1 lần

---

# Background Job

Mỗi ngày chạy:

- tìm điểm hết hạn
- tạo transaction Expire
- trừ TotalPoints

---

# TODO For AI Agent

- Create entities
- Create migrations
- Add DbSet
- Configure relations
- Create DTOs
- Create services
- Create APIs
- Create JWT auth
- Create claim flow
- Add background job
- Add unique index
- Add validations
