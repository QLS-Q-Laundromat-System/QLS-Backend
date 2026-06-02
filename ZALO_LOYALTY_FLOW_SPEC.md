# Zalo Mini App Auth + Loyalty Flow

## 1. Mục tiêu

Mini App đăng nhập bằng access token Zalo. Backend không tin `zaloUserId`, tên hoặc avatar do frontend tự gửi.

Luồng chính:

1. Mini App gọi `getAccessToken()` từ `zmp-sdk`.
2. Mini App gửi access token Zalo về backend.
3. Backend tạo `appsecret_proof = HMAC-SHA256(accessToken, appSecretKey)`.
4. Backend gọi Zalo Graph API để lấy profile đã xác minh.
5. Backend tạo mới hoặc cập nhật `LoyaltyCustomer` theo Zalo user ID.
6. Backend trả JWT loyalty riêng để Mini App gọi API claim điểm.

## 2. Frontend Mini App

```ts
import { getAccessToken } from "zmp-sdk/apis";

const zaloAccessToken = await getAccessToken();

await fetch("/api/zalo/auth/login", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({
    brandId,
    accessToken: zaloAccessToken,
  }),
});
```

## 3. Backend login

`POST /api/zalo/auth/login`

Request:

```json
{
  "brandId": "uuid",
  "accessToken": "zalo_access_token"
}
```

Backend gọi:

`GET https://graph.zalo.me/v2.0/me?fields=id,name,picture`

Headers:

```text
access_token: zalo_access_token
appsecret_proof: hmac_sha256(zalo_access_token, zalo_app_secret_key)
```

Response:

```json
{
  "status": 200,
  "message": "Đăng nhập Zalo thành công",
  "data": {
    "accessToken": "jwt_backend",
    "customerId": "uuid",
    "customerType": "Member",
    "studentVerificationStatus": "None",
    "totalPoints": 0
  }
}
```

## 4. Profile và quyền truy cập

- Zalo user ID là định danh loyalty chính.
- Tên và avatar được cập nhật từ Graph API mỗi lần login.
- Nếu Mini App cần tên và avatar, frontend cần xin scope `userInfo` bằng `authorize`.
- Số điện thoại không bắt buộc cho luồng tích điểm hiện tại.
- Chỉ gọi `getPhoneNumber()` khi nghiệp vụ thật sự cần số điện thoại và phải giải thích rõ lý do xin quyền cho người dùng.

## 5. Loyalty API

JWT backend được gửi qua `Authorization: Bearer <accessToken>`.

- `POST /api/loyalty/claim`
- `GET /api/loyalty/me`
- `GET /api/loyalty/points/history`

### 5.1 Không dùng cookie

Zalo Mini App không hỗ trợ `LocalStorage`, `SessionStorage` và cookie theo cách ứng dụng web thông thường. Backend không được phụ thuộc vào cookie session để xác thực Mini App.

Frontend gửi JWT backend qua header:

```ts
await fetch(`${API_URL}/api/loyalty/me`, {
  headers: {
    Authorization: `Bearer ${backendAccessToken}`,
  },
});
```

Backend hiện đã dùng JWT bearer header cho các API loyalty.

## 6. Native Storage trên frontend

### 6.1 Vì sao cần dùng

Mini App phải dùng Native Storage của `zmp-sdk` để thay thế `LocalStorage`, `SessionStorage` và cookie. Mỗi Mini App được lưu tối đa khoảng `5 MB`; dữ liệu cũ có thể tự bị xóa khi đầy.

### 6.2 Nên lưu gì

- JWT backend để gọi API loyalty.
- `customerId`, profile cơ bản và `totalPoints` gần nhất để hiển thị nhanh.
- `claimToken` đang xử lý nếu cần khôi phục màn hình sau khi người dùng quay lại app.
- Cờ onboarding không nhạy cảm.

### 6.3 Không nên lưu gì

- `Zalo:AppSecretKey`.
- SePay secret hoặc JWT signing key.
- Zalo access token lâu dài nếu không cần thiết. Token này chỉ nên dùng để đổi lấy JWT backend khi login hoặc refresh session.
- Dữ liệu nhạy cảm không phục vụ trải nghiệm offline.

### 6.4 API storage nên dùng

Không triển khai mới bằng `setStorage`, `getStorage`, `removeStorage`. Các API này đã deprecated.

Với `zmp-sdk >= 2.43.0`, dùng `nativeStorage.setItem`, `nativeStorage.getItem`, `nativeStorage.removeItem`. API mới là synchronous và lưu giá trị dạng string.

### 6.5 Ví dụ lưu và đọc JWT backend

```ts
import { nativeStorage } from "zmp-sdk/apis";

const AUTH_STORAGE_KEY = "loyaltyAuth";

export const saveBackendAccessToken = (accessToken: string) => {
  nativeStorage.setItem(
    AUTH_STORAGE_KEY,
    JSON.stringify({
      accessToken,
      savedAt: new Date().toISOString(),
    }),
  );
};

export const loadBackendAccessToken = () => {
  const cachedValue = nativeStorage.getItem(AUTH_STORAGE_KEY);
  if (!cachedValue) return null;

  try {
    return JSON.parse(cachedValue).accessToken ?? null;
  } catch {
    nativeStorage.removeItem(AUTH_STORAGE_KEY);
    return null;
  }
};

export const clearBackendAccessToken = () => {
  nativeStorage.removeItem(AUTH_STORAGE_KEY);
};
```

Native Storage chỉ là cache phía client. Backend vẫn là nguồn dữ liệu chuẩn. Nếu JWT hết hạn hoặc storage bị xóa, frontend gọi lại `getAccessToken()` và `POST /api/zalo/auth/login`.

## 7. CORS cho Mini App

Mini App chạy trên domain hệ thống của Zalo. Backend phải cho phép origin:

```text
https://h5.zdn.vn
```

Backend đã thêm origin này vào `CorsSettings:AllowedOrigins`.

Header tối thiểu backend cần cho phép:

- `Authorization`
- `Content-Type`

Method cần dùng:

- `GET`
- `POST`

Backend hiện cho phép mọi header và method đối với các origin được khai báo.

## 8. Claim điểm

1. SePay webhook xác nhận thanh toán.
2. Backend tạo token QR sống 10 phút.
3. Kiosk polling `GET /api/v1/sessions/{id}` để lấy `loyalty.claimQrUrl`.
4. Mini App đọc `claimToken`.
5. Mini App gọi `POST /api/loyalty/claim`.

Rule:

- Mỗi QR chỉ dùng một lần.
- Mỗi machine session chỉ cộng điểm một lần.
- `10.000 VND = 1 điểm`, làm tròn xuống.
- Điểm hết hạn sau 3 tháng.
- Điểm tách riêng theo từng brand.

## 9. Cấu hình bắt buộc

```json
{
  "Zalo": {
    "AppId": "",
    "AppSecretKey": "",
    "GraphApiUrl": "https://graph.zalo.me/v2.0/me"
  }
}
```

Production phải truyền `Zalo:AppId`, `Zalo:AppSecretKey` và cập nhật `Loyalty:MiniAppClaimUrlTemplate` thành `https://zalo.me/s/<APP_ID>?claimToken={token}`. Không commit secret vào repository.

## 10. Flow frontend đề xuất

### 10.1 Khi mở Mini App

1. Đọc JWT backend từ Native Storage.
2. Nếu có JWT, gọi `GET /api/loyalty/me`.
3. Nếu nhận `401`, xóa JWT cache.
4. Gọi `getAccessToken()` từ `zmp-sdk`.
5. Gửi Zalo access token vào `POST /api/zalo/auth/login`.
6. Lưu JWT backend mới bằng Native Storage.
7. Gọi lại `GET /api/loyalty/me`.

### 10.2 Khi quét QR nhận điểm

1. Đọc `claimToken` từ query parameter.
2. Đảm bảo đã có JWT backend hợp lệ.
3. Gọi `POST /api/loyalty/claim`.
4. Cập nhật `totalPoints` cache sau khi claim thành công.
5. Nếu JWT hết hạn, login Zalo lại rồi retry claim đúng một lần.

### 10.3 Khi logout

1. Xóa JWT backend khỏi Native Storage.
2. Xóa profile cache.
3. Không cần gọi API revoke Zalo access token cho luồng loyalty hiện tại.

## 11. Phone number

Luồng tích điểm hiện tại không cần số điện thoại. Không gọi `getPhoneNumber()` ngay khi người dùng vừa mở Mini App.

Chỉ xin quyền số điện thoại khi có tính năng thực sự cần, ví dụ:

- Đăng ký thành viên có liên hệ.
- Tra cứu đơn hàng bằng số điện thoại.
- Nhận thông báo hoặc hỗ trợ khách hàng.

Khi xin quyền, frontend cần hiển thị onboarding giải thích mục đích sử dụng.

## 12. Migration

Migration `SecureZaloLoyaltyAuth` xóa dữ liệu loyalty cũ đã được tạo từ payload frontend chưa xác minh và bỏ cột `ZaloOAUserId`.

## 13. Checklist triển khai

### Backend

- Điền `Zalo:AppId`.
- Điền `Zalo:AppSecretKey` bằng environment variable hoặc secret manager.
- Đổi `Loyalty:MiniAppClaimUrlTemplate` sang App ID thật.
- Deploy migration `SecureZaloLoyaltyAuth`.
- Kiểm tra CORS cho `https://h5.zdn.vn`.

### Frontend Mini App

- Dùng `getAccessToken()` để login.
- Lưu JWT backend bằng Native Storage.
- Dùng `nativeStorage`, không dùng API storage cũ đã deprecated.
- Gửi JWT qua `Authorization: Bearer`.
- Xử lý `401` bằng login Zalo lại.
- Không dùng `LocalStorage`, `SessionStorage` hoặc cookie.
- Chỉ dùng `getPhoneNumber()` khi nghiệp vụ yêu cầu.
