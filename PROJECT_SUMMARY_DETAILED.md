# QLS-Backend - Tóm tắt dự án chi tiết

- Thời điểm đọc mã nguồn: 2026-05-13
- Phạm vi: toàn bộ mã nguồn trong `W:\DevPool\QLS-Backend` (trừ `bin/`, `obj/`)
- Quy mô quét:
  - 174 file C#
  - 16 Controllers
  - 17 Services
  - 26 Models
  - 42 DTO files
  - 15 Interfaces
  - 25 migration chính (không tính `*.Designer.cs`)

## 1) Tổng quan hệ thống

`QLS-Backend` là backend .NET cho hệ thống quản lý chuỗi giặt sấy (multi-tenant theo Brand) với các nhóm chức năng chính:

- Quản trị tài khoản, phân quyền theo vai trò (`SystemAdmin`, `BrandAdmin`, `Manager`, `Staff`, `Customer`)
- Quản lý Brand, Store, StoreType, Machine
- Cấu hình và tính giá (PriceList, PerKg, PerSession, TimeSlot)
- Quản lý phiên giặt/sấy và vòng đời thanh toán
- Tích hợp LG ThinQ (auth token, đồng bộ store, trạng thái máy, cài đặt máy)
- Tích hợp MQTT/Zigbee để kích hoạt thiết bị thực tế
- Nhận webhook SePay để đối soát giao dịch và tự động khởi động máy
- Dashboard + Revenue analytics
- Discount code theo Brand/Store/User

Ngăn xếp kiến trúc:

- ASP.NET Core Web API (`net10.0`)
- EF Core + PostgreSQL (`Npgsql`)
- JWT Bearer Authentication
- Swagger/OpenAPI
- MQTT (MQTTnet) cho IoT trigger
- Docker/Docker Compose cho môi trường chạy

## 2) Cấu trúc thư mục

- `Program.cs`: bootstrap app, middleware pipeline, JWT, DB migration/seed lúc startup
- `Extensions/ServiceExtensions.cs`: scan/register DI bằng Scrutor, CORS, Swagger config
- `Data/`:
  - `AppDbContext.cs`: DbSet và quan hệ dữ liệu
  - `DbSeeder.cs`: seed dữ liệu mẫu khi DB rỗng
- `Models/`: domain entities + enums
- `DTOs/`: request/response contract
- `Interfaces/`: contract service layer
- `Services/`: nghiệp vụ chính và tích hợp ngoài
- `Controllers/`: API endpoints
- `Middlewares/GlobalExceptionMiddleware.cs`: chuẩn hóa xử lý lỗi
- `Migrations/`: lịch sử thay đổi schema
- `docker-compose.yml`, `Dockerfile`, `.env.example`: triển khai
- `docs/pgadmin-connect.md`, `scripts/setup-server.sh`: vận hành

## 3) Luồng khởi động ứng dụng

Trong `Program.cs`:

- Add Controllers + Swagger + DI custom
- Kết nối PostgreSQL qua `ConnectionStrings:DefaultConnection`
- Cấu hình JWT validate issuer/audience/signature/lifetime
- Tạo `HttpClient` cho LG
- Khi startup:
  - `Database.MigrateAsync()`
  - `DbSeeder.SeedAsync(context)`
  - kiểm tra `CanConnect()` và in trạng thái DB
- Pipeline runtime:
  - `GlobalExceptionMiddleware`
  - `UseSwagger` + `UseSwaggerUI` (luôn bật)
  - `GET /health`
  - `GET /db-status`
  - `UseCors("AllowReactApp")`
  - `UseAuthentication` + `UseAuthorization`
  - `MapControllers`

## 4) Mô hình dữ liệu (Domain Model)

### 4.1 Nhóm tenant và tổ chức

- `Brand`: tenant cấp chuỗi, có `Stores`, `Accounts`, `StoreTypes`, 1-1 `BrandLgCredential`
- `Store`: cửa hàng thuộc Brand, có `StoreType`, chứa nhiều `Machines`
- `StoreType`: phân hạng cửa hàng theo Brand
- `Account`: thông tin đăng nhập và role hệ thống
- `User`: hồ sơ người dùng, liên kết phiên sử dụng máy

### 4.2 Nhóm máy và vận hành

- `Machine`:
  - Thuộc `Store`
  - Có `Type` (Washer/Dryer/Both), `Capacity`
  - Tích hợp IoT/LG: `LgDeviceId`, `Esp32MacAddress`, `ZigbeeNetworkId`
  - 1-1 với `MachineSetting`
- `MachineSetting`:
  - Chứa cấu hình máy giặt/sấy (coin, price array, washingTime, topOff, ratingMoney...)
- `MachineSession`:
  - Bản ghi phiên vận hành + thanh toán
  - Có `Status` (`PendingPayment`, `Running`, `Completed`, `Cancelled`, `Error`)
  - Chứa thông tin pricing snapshot (`PriceListId`, `PricingMode`, `WeightKg`, `CycleName`, `IsExtension`)
  - Có `PaymentCode`, `TransactionId`, trường refund tracking

### 4.3 Nhóm pricing

- `PriceList`: bảng giá theo Brand, status `Draft/Active/Expired`, priority, validity
- `PriceListStoreType`: bảng nối gán PriceList cho StoreType, có `OverridePriority`
- `PriceModePerKg`: cấu hình giá theo cân nặng
- `PriceModePerSession` (TPH inheritance):
  - `WasherPriceMode` (có `CycleName`)
  - `DryerPriceMode` (có `MinInitialSteps`, `ExtensionTimeoutMinutes`)
- `TimeSlot`: khung giờ áp giá theo `DayOfWeekMask`, `StartTime`, `EndTime`

### 4.4 Nhóm khuyến mãi và thanh toán

- `DiscountCode`: mã giảm giá theo Brand
- `DiscountCodeStore`: giới hạn mã theo Store
- `DiscountCodeUsage`: lịch sử dùng mã theo User/Session
- `PaymentTransaction`: log webhook/gateway transaction, trạng thái xử lý

## 5) Quan hệ dữ liệu chính trong `AppDbContext`

- Brand 1-n Store (cascade)
- Brand 1-n StoreType (cascade)
- Store n-1 StoreType (restrict)
- Account n-1 Brand (restrict), Account n-1 Store (restrict)
- Store 1-n Machine (cascade)
- User 1-n MachineSession (restrict)
- MachineSession n-1 Machine (cascade)
- MachineSession n-1 Store (restrict)
- Brand 1-1 BrandLgCredential (cascade)
- Machine 1-1 MachineSetting (cascade)
- DiscountCode nhiều-nhiều Store qua `DiscountCodeStore`
- TPH discriminator cho `PriceModePerSession` theo `MachineType`

## 6) Service layer và nghiệp vụ

### 6.1 Auth

- `AuthService`:
  - Login bằng BCrypt
  - Sinh JWT claims: user id, username, role, fullName, brandId, storeId
  - Register customer + user profile
  - `CreateAdminAccountAsync` kiểm soát hierarchy:
    - `SystemAdmin` chỉ tạo `BrandAdmin`
    - `BrandAdmin` chỉ tạo `Manager/Staff` trong brand của mình

### 6.2 Brand và Store

- `BrandService`: CRUD brand, danh sách admin, accounts, stores, store types
- `StoreService`:
  - CRUD store, check trùng tên trong cùng Brand
  - Có tích hợp tạo Store bên LG ThinQ khi Brand đã linked credential
  - API máy theo store có enrichment trạng thái live từ LG (nếu có token)

### 6.3 Máy và cài đặt máy

- `MachineDetailService`:
  - Lấy status máy từ LG theo LG StoreId
  - Auto upsert máy mới (theo `LgDeviceId`) vào DB
  - Cập nhật capacity
  - Lấy detail máy + setting + bảng giá active + trạng thái live LG
- `MachineSettingService`: CRUD setting local
- `LgMachineSettingSyncService`:
  - Nếu local chưa có setting thì fetch từ LG API rồi lưu
  - Update local và push setting lên LG
  - Parse khác nhau cho Washer/Dryer

### 6.4 Pricing

- `PricingService`:
  - CRUD TimeSlot
  - CRUD/tác vụ PriceList (status, assign store type, sync modes)
  - Tính giá theo ưu tiên:
    - Brand đúng + Active + trong hạn + không deleted
    - lọc theo StoreType (nếu có)
    - chọn theo priority + createdAt
  - Ưu tiên PerKg khi có `ClothingWeightKg`, fallback PerSession theo machine type/capacity/timeslot
- `PricingCalculatorService`:
  - Một service tính giá song song với `PricingService.CalculatePriceAsync`
  - Trả DTO có thêm thông tin `MinInitialSteps`, `DurationMinutes`, `ExtensionTimeoutMinutes` cho dryer

### 6.5 Session và Payment

- `DryerService`:
  - `InitSessionAsync`: tính tiền server-side, tạo session `PendingPayment`, sinh `PaymentCode`, trả QR SePay URL
  - `ConfirmPaymentAsync`: chuyển session sang `Running`
  - `UpdateSessionStatusAsync`: completed/error/cancelled + refund fields
- `SePayWebhookController`:
  - Idempotency check qua `PaymentTransactions`
  - Match session theo `PaymentCode` trong transfer content
  - Verify số tiền
  - Confirm payment nếu pending
  - Trigger Zigbee theo máy
  - Tính pulse cho dryer dựa `TotalMinutes / DurationMinutes`
- `PaymentController` (`/pulse/{count}`): endpoint test trigger trực tiếp (có thể lưu session nếu truyền đủ params)

### 6.6 Discount

- `DiscountCodeService`:
  - Tạo/sửa/lấy mã theo BrandAdmin
  - Validate theo: active, thời gian, usage limit, user usage limit, min order, store scope
  - Tính discount percentage/fixed + max discount cap
  - Record usage + overview + usage history

### 6.7 Dashboard và Revenue

- `RevenueService`: summary, daily, store comparison, machine ranking
- `DashboardService`: 10 nhóm chỉ số:
  - Wash count summary
  - Daily wash/dry count
  - Transaction volume (hourly/daily)
  - Brand growth
  - System overview
  - Brand overview
  - Machine utilization
  - Peak hours
  - Revenue trend (so với kỳ trước)
  - Machine status summary
  - Leaderboard
  - User growth

### 6.8 LG Integration

- `LgAuthTokenService` triển khai flow multi-step lấy token LG:
  - login session
  - lấy datetime
  - ký request
  - lấy OAuth code
  - exchange token
  - refresh token
- `BrandLgService`:
  - Link LG account cho Brand
  - Refresh token theo Brand
  - Sync stores từ LG
  - `GetValidCredentialAsync` auto refresh khi token sắp hết hạn
- `LgApiClient`: wrapper gọi LG endpoints status/settings/stores
- `LgMapper`: map raw LG JSON sang `MachineDetailDto`

### 6.9 Zigbee/MQTT

- `ZigbeeService` publish MQTT topic `zigbee2mqtt/{device}/set`
- Payload: `{ state: "ON", brightness: bagCount }`
- Clamp `bagCount` từ 1..20

## 7) API Endpoint map (theo nhóm)

### 7.1 Auth

- `POST /api/auth/login`
- `POST /api/auth/register`
- `POST /api/auth/create-account` (Authorize)

### 7.2 Brand

- `GET /api/brand`
- `GET /api/brand/{id}`
- `GET /api/brand/{id}/has-account`
- `POST /api/brand`
- `PUT /api/brand/{id}`
- `GET /api/brand/admins`
- `GET /api/brand/{id}/stores`
- `GET /api/brand/{id}/accounts`
- `GET /api/brand/{id}/store-types`
- `POST /api/brand/{id}/store-types`
- `PUT /api/brand/store-types/{storeTypeId}`
- `DELETE /api/brand/store-types/{storeTypeId}`

### 7.3 Brand LG

- `POST /api/brands/{brandId}/lg-auth/link`
- `POST /api/brands/{brandId}/lg-auth/refresh`
- `POST /api/brands/{brandId}/lg-auth/sync-stores`

### 7.4 Store

- `GET /api/store`
- `GET /api/store/count`
- `POST /api/store`
- `GET /api/store/{id}`
- `PUT /api/store/{id}`
- `GET /api/store/{id}/accounts`
- `GET /api/store/{id}/machines`
- `GET /api/store/me/machines`
- `PATCH /api/store/{id}/type`

### 7.5 Machine

- `GET /api/machine/status/{storeId}`
- `PATCH /api/machine/{id}/capacity`
- `GET /api/machine/{id}/detail`

### 7.6 Machine Settings

- `GET /api/machines/{machineId}/setting`
- `PUT /api/machines/{machineId}/setting`
- `DELETE /api/machines/{machineId}/setting`

### 7.7 Session / Payment

- `POST /api/v1/sessions/init`
- `POST /api/v1/sessions/{id}/confirm-payment`
- `PATCH /api/v1/sessions/{id}/status`
- `GET /api/v1/sessions/{id}`
- `GET /pulse/{count}` (test trigger)
- `POST /api/webhooks/sepay`

### 7.8 Pricing

- `POST /api/v1/pricing/calculate`
- `GET /api/v1/timeslots`
- `POST /api/v1/timeslots`
- `PUT /api/v1/timeslots/{id}`
- `DELETE /api/v1/timeslots/{id}`
- `GET /api/v1/pricelists`
- `GET /api/v1/pricelists/{id}`
- `POST /api/v1/pricelists`
- `PATCH /api/v1/pricelists/{id}/status`
- `POST /api/v1/pricelists/{id}/store-types`
- `PUT /api/v1/pricelists/{id}/modes/per-kg`
- `PUT /api/v1/pricelists/{id}/modes/per-session`
- `POST /api/v1/pricelists/calculate`

### 7.9 Discount

- `POST /api/discountcodes`
- `GET /api/discountcodes`
- `GET /api/discountcodes/{id}`
- `GET /api/discountcodes/overview`
- `GET /api/discountcodes/{id}/usages`
- `PUT /api/discountcodes/{id}`
- `POST /api/discountcodes/validate`

### 7.10 Revenue / Dashboard / LG Auth

- Revenue:
  - `GET /api/revenue/summary`
  - `GET /api/revenue/daily`
  - `GET /api/revenue/brand/{brandId}/stores`
  - `GET /api/revenue/store/{storeId}/machines`
- Dashboard:
  - `GET /api/dashboard/wash-count/summary`
  - `GET /api/dashboard/wash-count/daily`
  - `GET /api/dashboard/transaction-volume`
  - `GET /api/dashboard/brand-growth`
  - `GET /api/dashboard/system-overview`
  - `GET /api/dashboard/brand-overview/{brandId}`
  - `GET /api/dashboard/machine-utilization`
  - `GET /api/dashboard/peak-hours`
  - `GET /api/dashboard/revenue-trend`
  - `GET /api/dashboard/machine-status-summary`
  - `GET /api/dashboard/leaderboard`
  - `GET /api/dashboard/user-growth`
- LG Auth:
  - `POST /api/lg/auth/token`
  - `POST /api/lg/auth/refresh`
  - `GET /api/lg/auth/hash-check`

## 8) Migration timeline (25 migration chính)

1. `20260407093151_InitialCreate` - tạo schema nền tảng ban đầu
2. `20260409074058_AddStorePhoneAndEmail` - thêm phone/email cho store
3. `20260409081609_AddBrandAddressAndLogo` - thêm address/logo cho brand
4. `20260409082940_AddStoreLgStoreId` - thêm LG StoreId
5. `20260409091324_RenameMachineBranchIdToStoreId` - chuẩn hóa FK Branch->Store
6. `20260410074310_AddPriceListTable` - thêm bảng giá lõi
7. `20260410075617_AddPriceListStoreTypeTable` - gán bảng giá theo store type
8. `20260410080913_AddPriceModePerKgTable` - thêm mode tính giá theo kg
9. `20260410083056_AddPriceModePerSessionAndTimeSlotTables` - thêm mode theo lượt + timeslot
10. `20260410112332_AddProfileFieldsToAccount` - profile fields cho account
11. `20260412151532_AddBrandLgCredential` - lưu credential/token LG theo brand
12. `20260412152226_UpdateMachineModel` - mở rộng model machine + migrate kiểu dữ liệu liên quan
13. `20260412154141_AddBackendUrlToBrandLgCredential` - lưu OAuth backend URL
14. `20260413074502_AddLgPinCodeToStore` - pin code LG cho store
15. `20260413091929_EnhanceMachineSessionRevenue` - mở rộng cột doanh thu/session
16. `20260414032433_AddPricingEnhancements` - tăng cường FK/cột pricing
17. `20260416025411_AddDryerPriceModeProperties` - cột riêng cho dryer mode
18. `20260416030834_EnhanceMachineSessionWithPricing` - snapshot pricing trong session
19. `20260420064753_AddPaymentConfirmationAndRefundTracking` - payment confirmed + refund tracking
20. `20260423140703_RemoveStoreSettingTable` - bỏ bảng setting store cũ
21. `20260425050701_AddMachineSettingTable` - thêm setting theo machine
22. `20260425100756_AddSuperWashToMachineSetting` - thêm `AddSuperWash`
23. `20260425141136_AddRatingMoneyToMachineSetting` - thêm `RatingMoney`
24. `20260427030452_AddDiscountCodeTables` - thêm discount code subsystem
25. `20260512032419_AddPaymentIntegration` - thêm payment integration tables/columns

## 9) Cấu hình và triển khai

### 9.1 Package chính (`QLS.Backend.csproj`)

- `Microsoft.EntityFrameworkCore` + `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Swashbuckle.AspNetCore`
- `Scrutor`
- `BCrypt.Net-Next`
- `MQTTnet`, `Meadow.MQTT`

### 9.2 appsettings

- `ConnectionStrings:DefaultConnection`
- `Jwt` key/issuer/audience/ttl
- `CorsSettings:AllowedOrigins`
- `LgApi` header config
- `Zigbee2Mqtt` host/port
- `SePay` account info

### 9.3 Docker

- `Dockerfile` multi-stage: SDK build -> ASP.NET runtime
- `docker-compose.yml` gồm:
  - `postgres`
  - `pgadmin`
  - `qls-backend`
- Môi trường chạy backend qua biến env override (`ConnectionStrings__DefaultConnection`, `Jwt__Key`, ...)

### 9.4 Script/Docs vận hành

- `scripts/setup-server.sh`: setup Ubuntu server + Docker + `.env` + mở firewall
- `docs/pgadmin-connect.md`: hướng dẫn kết nối pgAdmin qua SSH tunnel tới Oracle VM
- `servers.json`: pre-config server pgAdmin nội bộ docker

## 10) Dữ liệu seed mặc định

Khi DB chưa có Brand, `DbSeeder.SeedAsync` sẽ tạo:

- 2 Brand mẫu (`QLS Premium Laundry`, `QLS1`)
- StoreType mẫu
- 1 Store mẫu
- 4 máy mẫu (2 washer, 2 dryer)
- User mẫu + account mẫu (`admin`, `owner@qls.com`, `sonadmin`, account customer)
- TimeSlot mẫu (Happy Hour)
- 1 PriceList active + mapping store type + mode PerKg/PerSession mẫu

## 11) Ghi chú kỹ thuật quan trọng

- Swagger đang bật cả production (`RoutePrefix = string.Empty`)
- App tự chạy migration + seed khi startup
- Có 2 service tính giá (`PricingService.CalculatePriceAsync` và `PricingCalculatorService.CalculatePriceAsync`) với logic tương đồng nhưng không hoàn toàn giống nhau
- Nhiều secret đang để trực tiếp trong file cấu hình mẫu/appsettings (DB password, JWT key, LG api key) nên cần kiểm soát khi triển khai thật
- Webhook SePay có cơ chế idempotency cơ bản qua `GatewayTransactionId`

## 12) Tóm tắt kiến trúc logic end-to-end

1. User chọn máy + thông tin dịch vụ.
2. Backend tính giá theo PriceList hiện hành + rules (store type, timeslot, machine type).
3. Backend tạo session `PendingPayment` và trả QR thanh toán.
4. SePay webhook gửi giao dịch vào backend.
5. Backend match theo `PaymentCode`, xác nhận tiền, chuyển session `Running`.
6. Backend gửi lệnh MQTT/Zigbee để máy chạy.
7. Khi hoàn tất/lỗi: session được cập nhật trạng thái; dashboard/revenue dùng dữ liệu này để tổng hợp KPI.

---

Nếu cần, có thể tách tiếp tài liệu này thành:

- API Spec dạng bảng (method/route/request/response)
- Data Dictionary chi tiết từng cột entity
- Sequence diagram cho luồng LG + Payment + Zigbee
