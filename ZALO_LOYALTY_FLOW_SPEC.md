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

