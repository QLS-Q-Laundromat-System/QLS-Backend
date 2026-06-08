using System.Collections.Generic;

namespace QLS.Backend.Exceptions
{
    public static class ZaloErrors
    {
        private static readonly Dictionary<int, string> ErrorMap = new()
        {
            { 100, "Tham số không hợp lệ (Invalid parameter)." },
            { 110, "User ID không hợp lệ (Invalid user ID)." },
            { 111, "Không thể phân giải User ID hợp lệ (Can't resolve to a valid user ID)." },
            { 112, "Ứng dụng chưa liên kết với Official Account nào." },
            { 210, "Người dùng ẩn/không hiển thị (User not visible)." },
            { 289, "Cần cấp quyền truy cập nâng cao 'read_requests' để lấy thông tin kết bạn." },
            { 452, "Access token không hợp lệ hoặc đã hết hạn (Session key invalid)." },
            { 2004, "Tính năng gửi tin nhắn/yêu cầu tạm thời bị khóa đối với ứng dụng này." },
            { 2500, "Lỗi cú pháp yêu cầu (Syntax error)." },
            { 10000, "Yêu cầu gọi Zalo API thất bại (Call fail)." },
            { 10001, "Phương thức không được hỗ trợ cho API này (Method not supported)." },
            { 10002, "Lỗi hệ thống không xác định từ phía Zalo (Unknown exception)." },
            { 10003, "Mục dữ liệu/đối tượng không tồn tại (Item not exists)." },
            { 11004, "App ID Zalo đang sử dụng đã bị vô hiệu hóa hoặc khóa (App ID disabled/banned)." },
            { 12000, "Vượt quá giới hạn gọi API (Quota limit exceeded)." },
            { 12001, "Danh sách bạn bè vượt quá giới hạn (tối đa 50)." },
            { 12002, "Vượt quá giới hạn gọi API hàng ngày (Daily quota exceeded)." },
            { 12003, "Vượt quá giới hạn gọi API hàng tuần (Weekly quota exceeded)." },
            { 12004, "Vượt quá giới hạn gọi API hàng tháng (Monthly quota exceeded)." },
            { 12006, "Người dùng chưa chơi game trong vòng 30 ngày qua." },
            { 12007, "Người dùng chưa nhắn tin cho bạn bè trong vòng 30 ngày qua." },
            { 12008, "Người nhận đã vượt quá quota nhận tin nhắn (tối đa 1 tin nhắn mỗi 3 ngày)." },
            { 12009, "Người gửi và người nhận chưa kết bạn với nhau." },
            { 12010, "Vượt quá giới hạn gọi API hàng ngày đối với từng người dùng cụ thể." },
            { 12011, "Người bạn này chưa sử dụng ứng dụng." },
            { 12012, "Người bạn này đã sử dụng ứng dụng." }
        };

        /// <summary>
        /// Lấy thông báo lỗi tiếng Việt thân thiện dựa trên mã lỗi từ Zalo API.
        /// </summary>
        public static string GetFriendlyMessage(int errorCode, string? originalMessage = null)
        {
            if (ErrorMap.TryGetValue(errorCode, out var friendlyMessage))
            {
                return friendlyMessage;
            }

            return !string.IsNullOrWhiteSpace(originalMessage)
                ? $"Lỗi Zalo API [{errorCode}]: {originalMessage}"
                : $"Lỗi Zalo API không xác định [{errorCode}].";
        }
    }
}
