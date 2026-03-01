# Visual Asset Hub — Changelog

> Cập nhật lần cuối: 01/03/2026  
> Tài liệu này là bản tóm tắt thay đổi gần nhất.  
> Bản chi tiết theo session được giữ tại `docs/FIX_REPORT_20260227.md`.
> Thuật ngữ dùng chung: `docs/GLOSSARY.md`

---

## [2026-03-01] Đồng bộ kiến trúc + đồng bộ tài liệu

### Added

- Lát cắt tính năng (Vertical Slice) cho Assets tại `Features/Assets/*`
- Controller tách Command/Query cho Assets
- Request contracts cho upload và duplicate
- Application service theo feature
- Duplicate Strategy + Factory theo feature
- Infrastructure theo feature:
  - File mapper
  - User context provider

### Changed

- Upload mapping dịch chuyển khỏi controller
- Duplicate flow chuẩn hóa qua strategy factory
- Route naming chuẩn hóa qua `AssetRouteNames`
- DI registrations chuyển về namespace feature mới
- Một số controller vệ tinh cập nhật `CreatedAtRoute` theo route name chuẩn

### Removed / Replaced

- Các implementation legacy tương ứng ở thư mục cũ đã được thay thế bởi feature-scoped files

### Validation

- `dotnet build VAH.Backend.csproj` thành công sau mỗi đợt thay đổi chính

---

## [2026-03-01] Đợt đồng bộ docs tiếng Việt

### Changed

- Việt hóa toàn bộ tài liệu trong `docs/`
- Chuẩn hóa lại vai trò từng file docs
- Khôi phục file lịch sử chi tiết `FIX_REPORT_20260227.md`

### Result

- Dễ đọc hơn cho đội phát triển nội bộ
- Vẫn giữ được lịch sử chi tiết để truy vết sau commit
