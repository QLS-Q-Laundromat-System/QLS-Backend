# Hướng Dẫn & Tiêu Chuẩn Ghi Log (Logging Standards)

Tài liệu này hướng dẫn cách sử dụng và các quy định ghi log trong dự án **QLS Backend** để đảm bảo việc giám sát hệ thống hiệu quả, bảo mật và đồng bộ.

---

## 1. Công Nghệ Sử Dụng

Hệ thống sử dụng **Serilog** làm thư viện ghi log cốt lõi cho ứng dụng ASP.NET Core với các đặc điểm:
* **Structured Logging (Log có cấu trúc)**: Cho phép lưu trữ log dưới dạng các đối tượng JSON có thuộc tính riêng biệt (ví dụ: `UserId`, `Path`, `StatusCode`, `ElapsedMilliseconds`) thay vì một chuỗi văn bản thuần. Điều này hỗ trợ việc truy vấn log cực kỳ nhanh trên các hệ thống giám sát log tập trung (ElasticSearch, Seq, Loki,...).
* **Console Sink**: Ghi nhận log ra màn hình Console khi chạy thử nghiệm hoặc kiểm tra trực tiếp.
* **File Rolling Sink**: Ghi nhận log ra các file văn bản đặt tại thư mục `logs/app-log-yyyyMMdd.txt` và tự động tạo file mới mỗi ngày. Hệ thống lưu trữ tối đa **31 ngày** log gần nhất để tiết kiệm dung lượng ổ cứng.

---

## 2. Ghi Log Request Tự Động (API Logging)

Hệ thống đã được thiết lập [RequestLoggingMiddleware](file:///w:/DevPool/QLS-Backend/Middlewares/RequestLoggingMiddleware.cs) hoạt động tự động để bắt các cuộc gọi API. 

Mỗi khi người dùng hoặc hệ thống gọi một API, một dòng log dạng `Information` sẽ được ghi lại tự động với định dạng:
`[yyyy-MM-dd HH:mm:ss.fff zzz] [INF] [UserId] User <UserId> called <Method> <Path> - Response: <StatusCode> in <Duration>ms`

* **UserId**: Sẽ tự động trích xuất từ JWT Token của người dùng đang đăng nhập. Nếu chưa đăng nhập hoặc API public, hệ thống sẽ lưu là `Anonymous`.
* **Latency**: Thời gian xử lý của API (tính bằng mili-giây).

---

## 3. Tiêu Chuẩn & Quy Tắc Ghi Log Cho Nhà Phát Triển (Developer Logging Guide)

### Quy tắc 1: Luôn dùng Structured Logging (Ghi log có cấu trúc)
**KHÔNG** dùng kỹ thuật cộng chuỗi hay String Interpolation (`$""`) khi truyền tham số vào hàm log của `ILogger`. 

* **❌ Tránh viết**:
  ```csharp
  _logger.LogInformation($"Khách hàng {userId} đã thanh toán đơn hàng {orderId} số tiền {amount} VNĐ");
  ```
* **✅ Viết đúng**:
  ```csharp
  _logger.LogInformation("Khách hàng {UserId} đã thanh toán đơn hàng {OrderId} số tiền {Amount} VNĐ", userId, orderId, amount);
  ```
* **Lý do**: Khi ghi log có cấu trúc, các giá trị `UserId`, `OrderId`, `Amount` sẽ được lưu thành các trường (fields) dữ liệu JSON độc lập. Bạn có thể dễ dàng chạy các câu lệnh query lọc log như: `UserId == "123"` hoặc `Amount > 500000` trên giao diện quản trị log.

---

### Quy tắc 2: Phân cấp độ Log (Log Level) chính xác
Hãy chọn cấp độ log phù hợp để tránh làm loãng file log (Log Noise) hoặc làm mất mát các log lỗi quan trọng:

| Cấp độ | Ý nghĩa | Ví dụ thực tế |
| :--- | :--- | :--- |
| **Debug** | Chỉ dùng khi dev hoặc sửa lỗi ở môi trường local, log rất chi tiết từng bước chạy trong hàm. | `_logger.LogDebug("[LG Auth Step 2] Nhận serverDate: {Date}", date);` |
| **Information** | Các sự kiện chạy bình thường, đánh dấu các bước quan trọng của nghiệp vụ. | `_logger.LogInformation("Máy {DeviceId} đã bắt đầu chu trình giặt {Course}", deviceId, course);` |
| **Warning** | Các sự kiện không mong muốn hoặc bất thường nhưng hệ thống tự phục hồi hoặc tự bỏ qua được. | `_logger.LogWarning("Không thể kết nối đến máy chủ phụ trợ. Đang thử lại lần {RetryCount}", retry);` |
| **Error** | Lỗi xảy ra làm gián đoạn luồng xử lý hiện tại của người dùng. Có kèm Exception Stack Trace. | `_logger.LogError(ex, "Lỗi xảy ra khi trừ điểm loyalty của User: {UserId}", userId);` |
| **Critical** | Lỗi cực kỳ nghiêm trọng có thể làm sập hệ thống (Crash app, mất kết nối DB chính, hết dung lượng đĩa). | `_logger.LogCritical(ex, "Không thể khởi động service MQTT Listener!");` |

---

### Quy tắc 3: Tuyệt đối bảo mật thông tin nhạy cảm
**KHÔNG** ghi nhận các thông tin cá nhân và thông tin bảo mật vào file log:
* Mật khẩu dạng plain text (`Password`).
* Mã OTP (`OtpToken`).
* Các Token bảo mật (`AccessToken`, `RefreshToken`, `ClientSecret`, `ApiKey`).
* Số thẻ tín dụng, số tài khoản ngân hàng chi tiết.
* Dữ liệu cá nhân (PII) của người dùng như số điện thoại, CCCD trừ khi thông tin đó đã được mã hóa hoặc mask (che bớt dạng `098***1234`).

---

### Quy tắc 4: Ghi nhận Exception đúng chuẩn
Khi bắt Exception và ghi log `Error`, hãy luôn truyền đối tượng Exception làm tham số đầu tiên để Serilog có thể phân tích thông tin Stack Trace và hiển thị một cách tường minh nhất.

* **❌ Tránh viết**:
  ```csharp
  catch (Exception ex)
  {
      _logger.LogError($"Có lỗi xảy ra: {ex.Message}");
  }
  ```
* **✅ Viết đúng**:
  ```csharp
  catch (Exception ex)
  {
      _logger.LogError(ex, "Có lỗi xảy ra khi cập nhật trạng thái máy {DeviceId}", deviceId);
  }
  ```

---

## 4. Quản lý thư mục Log trên Server

* Toàn bộ log file được ghi vào thư mục `logs/` nằm ở thư mục chạy ứng dụng gốc của dự án.
* Đảm bảo rằng User chạy dịch vụ Backend trên server Production có quyền **Write** trên thư mục `logs/` này.
