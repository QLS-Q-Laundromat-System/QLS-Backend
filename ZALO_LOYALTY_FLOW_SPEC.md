# Loyalty Mini App Flow

## 1. Mục tiêu

Zalo Mini App chỉ là giao diện loyalty. Backend không còn đăng nhập bằng Zalo ID.

Luồng chính:

1. Người dùng đăng ký loyalty bằng email hoặc số điện thoại.
2. Backend gửi OTP để xác minh quyền sở hữu email hoặc số điện thoại.
3. Người dùng đăng nhập bằng password hoặc OTP.
4. Sau thanh toán kiosk, SePay xác nhận giao dịch.
5. Backend tạo QR nhận điểm.
6. Mini App quét QR và gọi API claim bằng JWT loyalty.

## 2. Auth loyalty

### 2.1 Yêu cầu OTP

`POST /api/loyalty/auth/otp/request`

```json
{
  "brandId": "uuid",
  "identifier": "0901234567",
  "purpose": "Register"
}
```

`identifier` nhận email hoặc số điện thoại. `purpose` nhận `Register` hoặc `Login`.

### 2.2 Đăng ký

`POST /api/loyalty/auth/register`

```json
{
  "brandId": "uuid",
  "identifier": "0901234567",
  "password": "Password123!",
  "otpCode": "123456",
  "fullName": "Nguyen Van A"
}
```

Đăng ký chỉ thành công sau khi OTP hợp lệ. Email hoặc số điện thoại được xác minh theo identifier đã dùng.

### 2.3 Đăng nhập password

`POST /api/loyalty/auth/login/password`

```json
{
  "brandId": "uuid",
  "identifier": "0901234567",
  "password": "Password123!"
}
```

### 2.4 Đăng nhập OTP

Gọi `POST /api/loyalty/auth/otp/request` với `purpose = "Login"`, sau đó gọi:

`POST /api/loyalty/auth/login/otp`

```json
{
  "brandId": "uuid",
  "identifier": "0901234567",
  "otpCode": "123456"
}
```

### 2.5 Response auth

```json
{
  "status": 200,
  "message": "Đăng nhập thành công",
  "data": {
    "accessToken": "jwt_backend",
    "customerId": "uuid",
    "customerType": "Member",
    "studentVerificationStatus": "None",
    "totalPoints": 0
  }
}
```

JWT được gửi trong header `Authorization: Bearer <accessToken>`.

## 3. Rule OTP

- OTP gồm 6 chữ số.
- OTP mặc định sống 5 phút.
- Mỗi identifier chỉ yêu cầu OTP mới sau 60 giây.
- OTP chỉ dùng một lần.
- OTP bị khóa sau 5 lần nhập sai.
- Production phải thay service log OTP bằng nhà cung cấp SMS và email thực tế.

## 4. Loyalty API

- `POST /api/loyalty/claim`
- `GET /api/loyalty/me`
- `GET /api/loyalty/points/history`

## 5. Claim điểm

1. SePay webhook xác nhận thanh toán.
2. Backend tạo token QR sống 10 phút.
3. Kiosk polling `GET /api/v1/sessions/{id}` để lấy `loyalty.claimQrUrl`.
4. Mini App đọc `claimToken`.
5. Mini App gọi `POST /api/loyalty/claim`.

```json
{
  "claimToken": "ABC123XYZ"
}
```

Rule:

- Mỗi QR chỉ dùng một lần.
- Mỗi machine session chỉ cộng điểm một lần.
- `10.000 VND = 1 điểm`, làm tròn xuống.
- Điểm hết hạn sau 3 tháng.
- Điểm tách riêng theo từng brand.

## 6. Migration

Migration `ReplaceZaloLoyaltyAuthWithOtp` xóa toàn bộ dữ liệu loyalty cũ trước khi bỏ cột Zalo và tạo schema auth mới.
