# QLS-Backend

Backend .NET cho hệ thống `Q Laundry Station`.

Đây là repo trung tâm của toàn hệ sinh thái. Nó không chỉ là CRUD API thông thường mà đang giữ gần như toàn bộ luật nghiệp vụ chính:

- đa tenant theo `Brand`
- quản lý cửa hàng, tài khoản, máy giặt và máy sấy
- bảng giá và tính giá theo hạng cửa hàng, loại máy, khung giờ
- tạo machine session và xác nhận thanh toán
- loyalty qua Zalo Mini App
- tích hợp LG ThinQ để lấy trạng thái máy và đồng bộ cửa hàng
- tích hợp Zigbee/MQTT để kích hoạt phần cứng
- webhook SePay để xác thực thanh toán và kích hoạt máy
- dashboard và revenue analytics

README này được tổng hợp lại từ mã nguồn thực tế, các controller, service, entity, migration và tài liệu rời trong repo, để người mới đọc vào có thể hiểu:

- hệ thống này đang giải bài toán gì
- domain model là gì
- luồng thanh toán và vận hành máy chạy như thế nào
- mỗi nhóm API phục vụ phần nào
- cần cấu hình gì để chạy local hoặc deploy
- repo đang có những điểm mạnh, điểm rủi ro và phần nào còn cần dọn

## 1. Đọc nhanh trong 3 phút

Nếu bỏ hết chi tiết kỹ thuật và chỉ nhìn ở góc nghiệp vụ, backend này làm các việc sau:

1. quản lý hệ thống chuỗi giặt sấy theo `Brand`
2. mỗi `Brand` có nhiều `Store`
3. mỗi `Store` có nhiều `Machine`
4. người dùng nội bộ đăng nhập bằng JWT
5. kiosk/app chọn máy và gửi yêu cầu tạo session
6. backend tính tiền server-side dựa trên bảng giá hiện hành
7. backend sinh `PaymentCode` và link QR SePay
8. khi SePay webhook xác nhận đã nhận tiền, backend:
   - đánh dấu session đã thanh toán
   - tạo token loyalty claim nếu phù hợp
   - gửi lệnh Zigbee/MQTT để máy chạy
9. dữ liệu session sau đó được dùng cho:
   - loyalty
   - dashboard
   - revenue
   - tracking vận hành

Nếu cần mô tả trong một câu:

> `QLS-Backend` là lõi nghiệp vụ của hệ thống giặt sấy, nơi hợp nhất auth, pricing, payment, loyalty, LG machine status và IoT trigger thành một backend duy nhất.

## 2. Bức tranh tổng thể của hệ thống

Repo này đang ngồi giữa nhiều client và nhiều tích hợp ngoài:

```text
Kiosk App / Web Admin / Zalo Mini App
                |
                v
           QLS-Backend
                |
    ---------------------------------
    |        |         |            |
 PostgreSQL  LG      SePay      Zigbee/MQTT
```

Ý nghĩa của từng nhánh:

- `PostgreSQL`: lưu tenant, store, machine, account, pricing, session, loyalty, payment transaction
- `LG ThinQ`: lấy live status máy, đồng bộ store, đồng bộ machine setting
- `SePay`: xác nhận thanh toán thực tế qua webhook
- `Zigbee/MQTT`: kích xung cho thiết bị phần cứng sau khi thanh toán xong

## 3. Công nghệ và runtime

### 3.1 Stack chính

- ASP.NET Core Web API
- `net10.0`
- Entity Framework Core 10
- PostgreSQL qua `Npgsql`
- JWT Bearer Authentication
- Swagger / OpenAPI
- Serilog
- MQTT / Zigbee integration
- Docker Compose

### 3.2 Package đáng chú ý

- `Microsoft.EntityFrameworkCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Swashbuckle.AspNetCore`
- `Serilog.AspNetCore`
- `BCrypt.Net-Next`
- `MQTTnet`
- `Meadow.MQTT`
- `Scrutor`

### 3.3 Những gì xảy ra khi app khởi động

`Program.cs` làm các việc lớn sau:

- bật controller, swagger, http client, CORS, JWT auth
- đăng ký background services:
  - `MachineStatusMonitoringService`
  - `MqttListenerService`
  - `LoyaltyPointExpiryService`
- kết nối PostgreSQL
- quét và inject services bằng `Scrutor`
- tự chạy migration
- tự chạy seed dữ liệu khi DB trống
- kiểm tra khả năng kết nối DB ngay lúc startup
- bật swagger ở cả development lẫn production
- expose:
  - `GET /`
  - `GET /health`
  - `GET /db-status`

Điều này rất tiện cho local/dev nhưng cũng có hệ quả vận hành:

- startup side effect mạnh
- production boot phụ thuộc DB
- swagger public mặc định

## 4. Cấu trúc thư mục nên đọc theo thứ tự

Nếu bạn mới vào repo, thứ tự đọc hiệu quả nhất là:

1. `Program.cs`
2. `Extensions/ServiceExtensions.cs`
3. `Data/AppDbContext.cs`
4. `Data/DbSeeder.cs`
5. `Controllers/`
6. `Services/`
7. `Models/`
8. `DTOs/`
9. `Migrations/`
10. `docker-compose.yml` và `.env.example`

Các thư mục chính:

```text
Controllers/   HTTP API
Services/      nghiệp vụ và tích hợp ngoài
Interfaces/    contract cho service layer
Models/        entity domain + enum
DTOs/          request/response contract
Data/          DbContext + seed
Middlewares/   exception/logging pipeline
Migrations/    lịch sử schema
docs/          tài liệu vận hành chuyên biệt
scripts/       script setup server
```

## 5. Domain model: hệ thống lưu những gì

### 5.1 Tenant và tổ chức

#### `Brand`

Đây là tenant cấp chuỗi.

Một `Brand` có thể có:

- nhiều `Store`
- nhiều `Account`
- nhiều `StoreType`
- nhiều `PaymentConfig`
- 1 `BrandLgCredential`

#### `Store`

Đại diện cho một cửa hàng vật lý.

Thông tin chính:

- tên
- địa chỉ
- phone
- email
- `StoreId` bên LG
- `LgPinCode`
- `BrandId`
- `StoreTypeId`

#### `StoreType`

Dùng để phân hạng hoặc nhóm cửa hàng trong cùng brand. Phần pricing dựa khá nhiều vào `StoreType`.

### 5.2 Người dùng và tài khoản

#### `Account`

Đây là bản ghi đăng nhập.

Chứa:

- `Username`
- `PasswordHash`
- `Role`
- `BrandId`
- `StoreId`
- trạng thái active

#### `User`

Đây là profile nghiệp vụ. Không phải account nào cũng nhất thiết có đầy đủ profile như nhau, nhưng hệ thống dùng `User` để gắn dữ liệu nghiệp vụ như session.

### 5.3 Máy và vận hành

#### `Machine`

Đại diện cho máy giặt hoặc máy sấy.

Chứa các trường quan trọng:

- `StoreId`
- `Name`
- `Type`
- `Capacity`
- `LgDeviceId`
- `Esp32MacAddress`
- `ZigbeeNetworkId`

#### `MachineSetting`

Setting riêng theo machine, dùng cho việc cấu hình local và đồng bộ lên LG.

#### `MachineSession`

Đây là bản ghi quan trọng nhất của bài toán vận hành.

Nó đại diện cho một lần sử dụng máy và giữ:

- máy nào được chọn
- cửa hàng nào
- user nào khởi tạo
- số tiền phải trả
- tổng số phút
- giá được tính theo bảng giá nào
- pricing mode gì
- payment code nào
- transaction id nào
- trạng thái session hiện tại là gì
- có cần hoàn tiền hay không

### 5.4 Pricing

#### `PriceList`

Bảng giá cấp brand, có:

- code
- tên
- trạng thái
- priority
- validity
- currency

#### `PriceListStoreType`

Gán bảng giá cho hạng cửa hàng.

#### `PriceModePerKg`

Giá tính theo cân nặng.

#### `PriceModePerSession`

Giá tính theo lượt, phân thành:

- `WasherPriceMode`
- `DryerPriceMode`

#### `TimeSlot`

Khung giờ áp dụng giá theo ngày và giờ.

### 5.5 Thanh toán, discount, loyalty

#### `PaymentConfig`

Cấu hình nhận tiền theo brand, đặc biệt cho SePay.

#### `PaymentTransaction`

Audit log của webhook hoặc giao dịch gateway.

#### `DiscountCode`, `DiscountCodeStore`, `DiscountCodeUsage`

Hệ discount code theo brand, theo store, theo lịch sử sử dụng.

#### `LoyaltyCustomer`

Khách hàng loyalty của Zalo Mini App, khóa chính nghiệp vụ là `BrandId + ZaloUserId`.

#### `PointClaimToken`

Token dùng để claim điểm sau thanh toán.

#### `LoyaltyPointTransaction`

Lịch sử cộng/trừ/rollback điểm.

## 6. Quan hệ dữ liệu quan trọng

Những quan hệ chính được cấu hình trong `AppDbContext`:

- `Brand 1-n Store`
- `Brand 1-n StoreType`
- `Account n-1 Brand`
- `Account n-1 Store`
- `Store 1-n Machine`
- `User 1-n MachineSession`
- `MachineSession n-1 Machine`
- `MachineSession n-1 Store`
- `Brand 1-1 BrandLgCredential`
- `Machine 1-1 MachineSetting`
- `DiscountCode n-n Store` qua `DiscountCodeStore`
- `PriceModePerSession` dùng TPH discriminator theo `MachineType`
- unique index cho `LoyaltyCustomer (BrandId, ZaloUserId)`
- unique index cho `PointClaimToken.Token`
- unique index cho `PointClaimToken.MachineSessionId`

Đọc phần này rất quan trọng vì nó cho thấy hệ thống được thiết kế theo hướng:

- tenant first
- pricing nằm ở cấp brand/store-type
- session là trung tâm của payment và loyalty

## 7. Authentication và phân quyền

### 7.1 Cách login hoạt động

`AuthService.LoginAsync`:

1. tìm `Account` theo `Username`
2. verify password bằng `BCrypt`
3. lấy `User` profile nếu có
4. sinh JWT
5. trả `LoginResponse`

### 7.2 Claims hiện có trong JWT nội bộ

- `NameIdentifier`
- `Name`
- `Role`
- `FullName`
- `BrandId` nếu có
- `StoreId` nếu có

### 7.3 Role chính

Từ code và enum usage, các role hiện nổi bật gồm:

- `SystemAdmin`
- `BrandAdmin`
- `Manager`
- `Staff`
- `Customer`
- `LoyaltyCustomer`

### 7.4 Quy tắc tạo account

`CreateAdminAccountAsync` đang enforce hierarchy:

- `SystemAdmin` chỉ tạo được `BrandAdmin`
- `BrandAdmin` chỉ tạo được `Manager` hoặc `Staff` trong brand của chính mình

## 8. Flow nghiệp vụ quan trọng nhất: từ chọn máy tới máy chạy

Đây là flow quan trọng nhất của toàn hệ thống.

### 8.1 Bước 1: khởi tạo session

Client gọi:

- `POST /api/v1/sessions/init`

Backend trong `MachineService.InitSessionAsync` sẽ:

1. lấy machine từ DB
2. nạp brand và payment config liên quan
3. tính giá server-side qua `IPricingCalculatorService`
4. tính `totalAmount` và `totalMinutes`
5. sinh `PaymentCode`
6. lưu `MachineSession` với trạng thái `PendingPayment`
7. kiểm tra store/brand đã có SePay config active chưa
8. trả về:
   - `SessionId`
   - số tiền
   - số phút
   - `PaymentCode`
   - QR URL của SePay

Điểm rất đúng ở đây:

- client không tự quyết định giá
- backend luôn là nơi tính tiền

### 8.2 Bước 2: SePay webhook xác nhận tiền

SePay gọi:

- `POST /api/webhooks/sepay`

`SePayWebhookController` sẽ:

1. đọc raw request body
2. deserialize payload
3. xác thực HMAC hoặc token
4. tìm `MachineSession` bằng `PaymentCode` nằm trong nội dung chuyển khoản
5. chống xử lý trùng bằng `PaymentTransactions`
6. kiểm tra số tiền nhận vào có đủ không
7. gọi `_machineService.ConfirmPaymentAsync(...)`
8. gọi loyalty service để tạo claim token nếu cần
9. tìm machine và gửi lệnh Zigbee nếu có `ZigbeeNetworkId`

### 8.3 Bước 3: session chuyển trạng thái

Sau `ConfirmPaymentAsync`, session được chuyển sang:

- `PaidWaitingForStart`

Trong code comment, trạng thái này đóng vai trò cầu nối giữa "đã trả tiền" và "phần cứng đang/chuẩn bị chạy".

### 8.4 Bước 4: theo dõi session

Client có thể polling:

- `GET /api/v1/sessions/{id}`

Response hiện có:

- `sessionId`
- `status`
- `machineId`
- `hardwareStatus`
- `loyalty`

### 8.5 Bước 5: hoàn tất hoặc lỗi

Client hoặc hệ thống cập nhật:

- `PATCH /api/v1/sessions/{id}/status`

Các trạng thái chính:

- `Completed`
- `Error`
- `Cancelled`

Khi `Error` hoặc `Cancelled`, service còn rollback điểm loyalty đã cộng nếu có.

## 9. Pricing: hệ thống tính giá như thế nào

Pricing là phần khá quan trọng vì hệ thống này không dùng giá cố định duy nhất.

Các trục tính giá đang thấy trong code:

- brand nào
- store type nào
- loại máy giặt hay sấy
- capacity của máy
- cân nặng quần áo nếu là pricing theo kg
- time slot nếu có giá theo khung giờ
- số step với dryer

Về mặt cấu trúc hiện có hai lớp liên quan:

- `PricingService`
- `PricingCalculatorService`

Vì cả hai cùng tham gia logic pricing, đây là khu vực cần đọc cẩn thận khi bảo trì để tránh lệch behavior giữa:

- API quản trị pricing
- logic runtime tính giá session

## 10. Store và Machine: dữ liệu tĩnh trộn với dữ liệu live

`StoreService.GetMachinesWithStatusByStoreIdAsync` cho thấy cách backend phục vụ kiosk:

1. lấy danh sách machine từ DB
2. dựng trước danh sách mặc định với trạng thái:
   - `Chưa đồng bộ`
   - `Ngoại tuyến`
3. nếu store có `StoreId` LG và brand có LG credential hợp lệ:
   - gọi LG API lấy raw status
   - map raw status qua `LgMapper`
   - enrich từng machine local bằng trạng thái live

Đây là điểm rất quan trọng để hiểu client:

- kiosk không gọi trực tiếp LG
- kiosk chỉ gọi backend
- backend chịu trách nhiệm hòa trộn dữ liệu local và live status từ LG

## 11. Loyalty và Zalo Mini App

Backend có một nhánh riêng cho loyalty.

### 11.1 Login loyalty

Route:

- `POST /api/zalo/auth/login`

Mục tiêu:

- frontend Zalo Mini App gửi `accessToken`
- backend tự gọi Zalo Graph API để xác minh profile
- backend không tin `zaloUserId` hoặc tên/avatar do frontend tự gửi
- backend tạo hoặc cập nhật `LoyaltyCustomer`
- backend trả JWT loyalty riêng

### 11.2 API loyalty chính

- `POST /api/loyalty/claim`
- `GET /api/loyalty/me`
- `GET /api/loyalty/points/history`
- `GET /api/loyalty/claim-link/{token}`

### 11.3 Flow claim điểm

1. session thanh toán thành công
2. backend tạo `PointClaimToken`
3. kiosk lấy `claim-link`
4. Mini App mở link hoặc nhận token
5. khách hàng claim điểm

Rule nghiệp vụ nổi bật:

- mỗi session chỉ được cộng điểm một lần
- mỗi token chỉ dùng một lần
- điểm tách theo brand

## 12. LG ThinQ integration

Tích hợp LG không chỉ là đọc trạng thái.

Từ cấu trúc code, backend đang làm:

- link account LG vào từng brand
- refresh token LG
- sync store từ LG
- tạo store trên LG khi tạo store local
- lấy raw machine status
- sync machine setting

Các thành phần chính:

- `BrandLgService`
- `LgAuthTokenService`
- `LgApiClient`
- `LgMapper`
- `MachineDetailService`
- `LgMachineSettingSyncService`

Ý nghĩa thực tế:

- hệ thống này không chỉ quản lý nội bộ
- nó đang cố gắng bắc cầu giữa dữ liệu business local và hệ thống máy thật từ LG

## 13. Zigbee / MQTT integration

Sau khi payment được xác nhận, backend có thể kích phần cứng qua MQTT/Zigbee.

Các thành phần:

- `ZigbeeService`
- `MqttListenerService`
- `HardwareTrackerService`

Ý nghĩa:

- payment không phải bước cuối
- hệ thống phải đi tới mức "ra lệnh chạy máy"

Đây là lý do tại sao session endpoint trả thêm `hardwareStatus`.

## 14. Các nhóm API chính

### 14.1 Auth

- `POST /api/auth/login`
- `POST /api/auth/register`
- `POST /api/auth/create-account`

### 14.2 Brand

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

### 14.3 Brand LG

- `POST /api/brands/{brandId}/lg-auth/link`
- `POST /api/brands/{brandId}/lg-auth/refresh`
- `POST /api/brands/{brandId}/lg-auth/sync-stores`

### 14.4 Store

- `GET /api/store`
- `GET /api/store/count`
- `POST /api/store`
- `GET /api/store/{id}`
- `PUT /api/store/{id}`
- `GET /api/store/{id}/accounts`
- `GET /api/store/{id}/machines`
- `GET /api/store/me/machines`
- `PATCH /api/store/{id}/type`

### 14.5 Machine

- `GET /api/machine/status/{storeId}`
- `PATCH /api/machine/{id}/capacity`
- `GET /api/machine/{id}/detail`
- `POST /api/machine/setup-zigbee`
- `GET /api/machine/discovered-devices/{machineId}`
- `POST /api/machine/permit-join`

### 14.6 Machine setting

- `GET /api/machines/{machineId}/setting`
- `PUT /api/machines/{machineId}/setting`
- `DELETE /api/machines/{machineId}/setting`

### 14.7 Session và payment

- `POST /api/v1/sessions/init`
- `POST /api/v1/sessions/{id}/confirm-payment`
- `PATCH /api/v1/sessions/{id}/status`
- `GET /api/v1/sessions/{id}`
- `POST /api/webhooks/sepay`
- `POST /api/webhooks/sepay/test-pay`
- `GET /pulse/{count}`

### 14.8 Pricing

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

### 14.9 Discount

- `POST /api/discountcodes`
- `GET /api/discountcodes`
- `GET /api/discountcodes/{id}`
- `GET /api/discountcodes/overview`
- `GET /api/discountcodes/{id}/usages`
- `PUT /api/discountcodes/{id}`
- `POST /api/discountcodes/validate`
- `GET /api/discountcodes/debug-all`

### 14.10 Dashboard và revenue

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
- `GET /api/revenue/summary`
- `GET /api/revenue/daily`
- `GET /api/revenue/brand/{brandId}/stores`
- `GET /api/revenue/store/{storeId}/machines`

## 15. Dữ liệu seed mặc định

Khi DB chưa có brand, `DbSeeder` sẽ tạo dữ liệu mẫu gồm:

- 2 brand mẫu
- store type mẫu
- 1 store mẫu
- 4 machine mẫu
- user mẫu
- account mẫu:
  - `admin / 123456`
  - `owner@qls.com / 123456`
  - `sonadmin / 123456`
  - customer account mẫu
- time slot mẫu
- 1 price list active
- per-kg pricing mẫu
- per-session pricing mẫu

Điều này giúp demo nhanh, nhưng khi đưa lên môi trường thật cần xem lại vì seed account mặc định là rủi ro bảo mật nếu không thay đổi.

## 16. Cấu hình môi trường

### 16.1 `appsettings.json`

Repo hiện đang chứa sẵn các nhóm cấu hình:

- `ConnectionStrings`
- `Serilog`
- `LgApi`
- `CorsSettings`
- `Zigbee2Mqtt`
- `Jwt`
- `SePay`
- `Loyalty`
- `Zalo`

### 16.2 `.env.example`

Repo cũng có mẫu env cho Docker deploy:

- `STACK_NAME`
- `ASPNETCORE_ENVIRONMENT`
- `BACKEND_PORT`
- `DB_NAME`
- `DB_USER`
- `DB_PASSWORD`
- `JWT_KEY`
- `CORS_ORIGIN_*`
- `ZALO_APP_ID`
- `ZALO_APP_SECRET_KEY`
- `LOYALTY_MINI_APP_CLAIM_URL_TEMPLATE`
- `MQTT_HOST`
- `MQTT_PORT`

### 16.3 Cảnh báo bảo mật

Tại thời điểm đọc repo:

- `appsettings.json` chứa secret thật hoặc secret-looking values
- `LG API key`
- `JWT key`
- `SePay webhook values`
- `Zalo app secret key`
- `DB password`

Các giá trị này nên được coi là đã lộ và cần rotate nếu còn dùng thật.

## 17. Chạy local

### 17.1 Yêu cầu

- .NET SDK tương thích `net10.0`
- PostgreSQL
- nếu cần test đầy đủ thì thêm MQTT broker / Zigbee2MQTT

### 17.2 Chạy bằng CLI

```bash
dotnet restore
dotnet run
```

Khi chạy, app sẽ:

- migrate DB
- seed dữ liệu nếu cần
- bật swagger tại `/swagger`

### 17.3 Chạy bằng Docker Compose

```bash
cp .env.example .env
docker compose up -d --build
```

Stack gồm:

- `postgres`
- `qls-backend`

Compose hiện không dựng sẵn MQTT broker trong file này, nên nếu test trigger thật cần đảm bảo broker đã có sẵn ở host tương ứng.

## 18. Quan hệ với các repo client

Repo backend này phục vụ ít nhất các loại client sau:

- kiosk Flutter app
- web admin / dashboard
- Zalo Mini App loyalty

Ví dụ repo kiosk liên quan trực tiếp:

- `../QLS_Kiosk_App`

Khi kiosk đăng nhập bằng `Manager` hoặc `Staff`, JWT phải chứa `StoreId`, rồi kiosk sẽ gọi:

- `GET /api/store/me/machines`

Đây là route nối trực tiếp giữa hai repo.

## 19. Điểm mạnh hiện tại

- domain model khá rộng và bám nghiệp vụ thật
- backend đã gắn với payment, loyalty, LG và hardware thay vì chỉ CRUD
- session lifecycle tương đối rõ
- có sẵn seed data để demo nhanh
- có docs và script deploy nền tảng
- có logging, health và db-status endpoint

## 20. Rủi ro và điểm cần biết trước khi sửa

- secret nằm trong config repo là rủi ro lớn
- startup có side effect mạnh vì auto migrate + auto seed
- swagger bật cả production
- logic pricing phân tán ở nhiều lớp, dễ lệch behavior khi chỉnh sửa
- đường đi payment sang hardware phụ thuộc nhiều external integration, khó test nếu không có môi trường đầy đủ
- repo đang có tài liệu vận hành rời và dữ liệu generated/log, nên cần giữ kỷ luật khi commit

## 21. Tài liệu vận hành còn giữ lại

Những file dưới đây vẫn hữu ích vì chúng là tài liệu chuyên biệt, không chỉ là summary tổng quát:

- `docs/pgadmin-connect.md`
- `docs/logging.md`
- `docs/DEPLOY_TWO_SERVERS.md`

README này thay thế các tài liệu tổng hợp cấp repo, nhưng không thay thế các runbook vận hành chuyên sâu.
