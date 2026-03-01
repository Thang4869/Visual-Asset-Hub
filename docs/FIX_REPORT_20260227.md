# Visual Asset Hub — Báo cáo Thay đổi & Sửa lỗi (Chi tiết theo Session)

> Cập nhật lần cuối: 01/03/2026  
> Mục đích: Lưu trữ đầy đủ lịch sử kỹ thuật để truy vết dài hạn
> Thuật ngữ dùng chung: `docs/GLOSSARY.md`

---

## 1. Tóm tắt các phase phát triển (legacy log)

| Phase | Tên | Trạng thái | Hạng mục |
|---|---|---|---|
| 1 | Backend Foundation | ✅ Hoàn thành | 5/5 |
| 2 | Frontend Core | ✅ Hoàn thành | 6/6 |
| 3 | Advanced Features | ✅ Hoàn thành | 8/8 |
| 4 | Enhancement & Polish | ✅ Hoàn thành | 7/7 |

---

## 2. Session #2 — Sửa lỗi gốc cho ContentType + hiển thị giao diện

### Vấn đề gốc

`AssetFactory` tạo subtype nhưng không set `ContentType` tương ứng.

Hệ quả:

- API trả sai `contentType`
- Frontend không render đúng image/link/color/color-group

### Sửa chữa chính

- Set `ContentType` cho toàn bộ factory methods
- Thêm SQL startup fix dữ liệu cũ trong DB
- Cải thiện hiển thị ảnh với chain fallback thumbnail
- Link hiển thị clickable URL
- Bỏ nút xóa đỏ theo yêu cầu UX, chuyển trọng tâm sang multi-select bulk

---

## 3. Session #3 — ColorBoard nâng cấp thao tác nâng cao

### Tính năng đã thêm

- Click để copy mã màu
- Drag handle tách riêng khỏi vùng click
- Drop indicator để thấy chính xác vị trí chèn
- Multi-select drag nhiều màu cùng lúc
- API bulk move group hỗ trợ insert-before

### Kết quả

- Trải nghiệm kéo thả trực quan hơn
- Giảm lỗi reorder sai vị trí
- Tối ưu thao tác hàng loạt với màu

---

## 4. Session #4 — Context menu, confirm system, tree panel, clipboard, access control

### Frontend

- ContextMenu component dùng chung
- ConfirmDialog + ConfirmContext thay `window.confirm/prompt/alert`
- TreeViewPanel hiển thị cấu trúc sâu
- Clipboard flow copy/cut/paste cho assets
- Pinned items và điều hướng nhanh
- Ctrl+V paste ảnh từ clipboard hệ điều hành

### Backend

- Củng cố shared-collection access control
- Chuẩn hóa permission checks khi CRUD/bulk
- Cải thiện cache invalidation sau grant/revoke
- Thêm duplicate endpoint và luồng xử lý liên quan

---

## 5. Session #5 — Rename collection + DTO partial update an toàn

### Vấn đề

Rename collection chỉ là stub ở frontend, chưa gọi API thật.

### Sửa chữa

- Bổ sung API update collection phía frontend
- Kết nối end-to-end rename flow
- Thêm `UpdateCollectionDto` nullable để tránh overwrite dữ liệu ngoài ý muốn
- Mở rộng global keyboard shortcuts

---

## 6. Session #6.x — Đợt refactor kiến trúc lớn

## 6.1 SRP tách controller + CreateAssetDto + factory duplicate

- Tách bulk operations ra controller riêng
- Đưa create asset sang DTO thay vì raw entity
- Duplicate logic gom vào factory để giảm copy logic rải rác

## 6.2 Chuẩn hóa REST semantics

- Bổ sung GET by id còn thiếu
- Chuẩn hóa 201/204 cho create/reorder
- RPC-like routes chuyển về noun routes
- Thêm PATCH song song với PUT alias tương thích ngược

## 6.3 Chuẩn hóa metadata + cancellation + DRY

- Bổ sung response metadata cho endpoint
- Bổ sung XML docs
- Thêm `CancellationToken` xuyên controller/service
- Loại bỏ body trùng lặp ở các endpoint alias

## 6.4 Áp chuẩn toàn hệ service/controller

- Mở rộng chuẩn hóa cho collection/auth/tag/search/smart/permission/health
- Đưa create collection sang DTO để tránh expose raw entity

## 6.5 C# 12 modern syntax

- Áp dụng primary constructor cho controllers
- Áp dụng expression-bodied members ở các action phù hợp

## 6.6 Domain separation + policy auth

- Tách specialized asset creators theo domain controller
- Chuyển authorize tổng quát sang policy read/write chi tiết

## 6.7 DTO boundary + API v1 + layout separation

- Chuẩn hóa API response sang DTO boundary
- Route versioning `api/v1/*`
- Tách `AssetLayoutController` cho concerns layout

## 6.8 CQRS + GlobalExceptionHandler + validation

- Áp dụng MediatR CQRS cho Assets
- Dùng `IExceptionHandler` + ProblemDetails (RFC 7807)
- Validation pagination bằng DataAnnotations

## 6.9 Frontend runtime fix

- Sửa lỗi TDZ tại AppProvider (`Cannot access ... before initialization`)

## 6.10 API version mismatch fix

- Sửa base URL `/api` vs `/api/v1` gây 404
- Đồng bộ SignalR derivation và docker compose config

## 6.11 Pinned navigation fix

- Bỏ flow setTimeout gây stale state
- Điều hướng pinned item theo target context rõ ràng

---

## 7. Đợt refactor kiến trúc mới nhất (01/03/2026)

### Kết quả nổi bật

- Assets đã chuyển sang lát cắt tính năng (Vertical Slice) hoàn chỉnh
- File mapper + user context cũng được kéo vào phạm vi feature
- Duplicate strategies/factory chuyển về feature namespace
- Controller và application boundary được làm sạch abstraction leak

### Build validation

- `dotnet build VAH.Backend.csproj` thành công

---

## 8. Danh sách lỗi điển hình đã xử lý xuyên suốt

- 401 do thiếu bearer token
- Seed collection mặc định
- Memory leak thumbnail streams
- SignalR CORS
- Orphan file cleanup
- Tag duplicate normalization
- Pending model changes/FK self-reference
- Enum serialization mismatch
- API version mismatch
- Runtime TDZ trong AppContext
- Navigation stale state khi mở pinned item

---

## 9. Cách dùng tài liệu này

- Dùng file này khi cần truy vết sâu theo session.
- Dùng `CHANGELOG.md` khi chỉ cần summary ngắn.
- Dùng `PROJECT_DOCUMENTATION.md` cho trạng thái code hiện tại.

---

## 10. Ghi chú cuối

Tài liệu này được giữ cố định theo triết lý bảo tồn tri thức lịch sử kỹ thuật. Khi có refactor mới, chỉ append mục mới thay vì thay thế toàn bộ nội dung cũ.
