# Visual Asset Hub — Bảng thuật ngữ

> Cập nhật lần cuối: 01/03/2026  
> Mục đích: Chuẩn hóa thuật ngữ để toàn bộ tài liệu nhất quán và dễ hiểu.

---

## 1. Thuật ngữ kiến trúc

| Thuật ngữ chuẩn | Diễn giải ngắn | Từ tiếng Anh tương ứng |
|---|---|---|
| Lát cắt tính năng | Tổ chức code theo từng tính năng hoàn chỉnh end-to-end | Vertical Slice |
| Ranh giới tầng | Điểm phân tách trách nhiệm giữa các tầng | Layer Boundary |
| Rò rỉ trừu tượng | Tầng này để lộ chi tiết tầng khác | Abstraction Leak |
| Điều phối truy vấn/lệnh | Tách luồng đọc và ghi theo mục đích | CQRS |
| Chiến lược nhân bản | Chọn cách xử lý duplicate theo ngữ cảnh | Strategy |
| Nhà máy chiến lược | Thành phần chọn/khởi tạo strategy phù hợp | Strategy Factory |
| Bộ điều phối ứng dụng | Lớp facade cho use-case phía controller | Application Service |
| Tài liệu nguồn sự thật | Tài liệu phản ánh trạng thái code hiện tại | Source of Truth |
| Tài liệu lưu trữ lịch sử | Tài liệu dùng để truy vết quá khứ | Archive/Legacy Report |
| Đường dẫn định danh an toàn kiểu | Đặt tên route tránh chuỗi hard-code | Type-safe Routing |

---

## 2. Thuật ngữ vận hành

| Thuật ngữ chuẩn | Diễn giải ngắn | Từ tiếng Anh tương ứng |
|---|---|---|
| Giới hạn tốc độ | Giới hạn số request theo thời gian | Rate Limiting |
| Theo dõi cấu trúc lỗi chuẩn | Phản hồi lỗi theo chuẩn RFC 7807 | ProblemDetails |
| Bộ nhớ đệm phân tán | Cache dùng chung ngoài process | Distributed Cache |
| Migrate tự động | Tự áp migration khi khởi động | Auto Migrate |
| Tương thích ngược | Giữ endpoint/luồng cũ vẫn hoạt động | Backward Compatibility |

---

## 3. Quy ước dùng từ trong docs

1. Luôn ưu tiên tiếng Việt trước, tiếng Anh đặt trong ngoặc khi cần.
2. Không dùng lẫn lộn nhiều biến thể cho cùng một khái niệm.
3. Với thuật ngữ quan trọng, giữ cố định cách viết đúng như bảng này.
4. Khi thêm thuật ngữ mới, cập nhật file này trước rồi mới dùng trong tài liệu khác.
