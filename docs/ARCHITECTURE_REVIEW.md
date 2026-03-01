# Visual Asset Hub — Đánh giá Kiến trúc (Hiện trạng)

> Cập nhật lần cuối: 01/03/2026  
> Phạm vi: Kiến trúc hiện tại sau đợt refactor Vertical Slice cho Assets
> Thuật ngữ dùng chung: `docs/GLOSSARY.md`

---

## 1. Tóm tắt điều hành

Hệ thống VAH hiện ở trạng thái đơn khối mô-đun lai (modular monolith):

- Năng lực dùng chung vẫn theo cấu trúc layer truyền thống: Controllers, Services, Data, Middleware.
- Domain Assets đã chuyển sang cấu trúc lát cắt tính năng (Vertical Slice).
- Ranh giới Presentation và Application được siết chặt:
  - Controller không tự mở stream file upload.
  - Mapping upload chuyển sang service chuyên trách.
  - Duplicate áp dụng Strategy + Factory.

Kết luận: kiến trúc đã tiến gần chuẩn enterprise 2025–2026 ở miền Assets, đồng thời vẫn giữ tương thích vận hành với phần còn lại của hệ thống.

---

## 2. Sơ đồ kiến trúc tổng thể

```text
Frontend React/Vite + SignalR Client
        |
        | HTTP + WebSocket
        v
ASP.NET Core 9 Backend
  ├─ Controllers + Feature Slices
  ├─ Middleware + Auth + Rate Limit
  ├─ SignalR Hub (/hubs/assets)
  ├─ EF Core (SQLite/PostgreSQL)
  ├─ Redis (optional)
  └─ Local file storage (wwwroot/uploads)
```

---

## 3. Pipeline runtime

Thứ tự middleware hiện tại:

1. Global exception handler (`UseExceptionHandler`)
2. CORS (`Frontend` policy)
3. Serilog request logging
4. Rate limiter
5. Static files
6. Swagger
7. Authentication
8. Authorization
9. Map controllers + map hub

Khởi động ứng dụng:

- Auto migrate CSDL
- Chạy SQL sửa dữ liệu discriminator legacy cho bảng Assets

---

## 4. Trọng tâm refactor: Vertical Slice Assets

## 4.1 Cấu trúc mới

```text
Features/Assets/
  Application/
    IAssetApplicationService
    AssetApplicationService
    Duplicate/
      IAssetDuplicateStrategy
      IAssetDuplicateStrategyFactory
      InPlaceDuplicateStrategy
      TargetFolderDuplicateStrategy
      AssetDuplicateStrategyFactory
  Commands/
    AssetsCommandController
  Queries/
    AssetsQueryController
  Contracts/
    UploadAssetsRequest
    DuplicateAssetRequest
  Common/
    AssetRouteNames
  Infrastructure/
    Files/
      IFileMapperService
      FileMapperService
    Contexts/
      IUserContextProvider
      UserContextProvider
```

## 4.2 Quyết định kiến trúc chính

- Tách Query/Command controller (CQRS endpoint surface)
- Type-safe route name qua `AssetRouteNames`
- Mapping file upload qua `IFileMapperService`
- User identity truy xuất qua `IUserContextProvider`
- Duplicate logic chọn chiến lược qua `IAssetDuplicateStrategyFactory`
- Giá trị mặc định collection quản lý bởi `AssetOptions`

---

## 5. Điểm mạnh và rủi ro còn lại

## 5.1 Điểm mạnh

- Giảm rõ rò rỉ trừu tượng (abstraction leak) giữa tầng trình bày và tầng ứng dụng.
- OCP/SRP/DIP cải thiện mạnh tại domain Assets.
- DI graph rõ ràng, đăng ký tập trung tại `ServiceCollectionExtensions`.
- Khả năng triển khai linh hoạt: SQLite dev, PostgreSQL prod, Redis tùy chọn.

## 5.2 Rủi ro còn lại

- Chưa toàn bộ domain được lát cắt tính năng, nên kiến trúc hiện vẫn ở trạng thái chuyển tiếp.
- Độ phủ test chưa tương xứng với mức thay đổi kiến trúc.
- Một số tài liệu lịch sử có độ dài lớn, cần duy trì quy ước cập nhật để không lệch.

---

## 6. Hướng phát triển kiến trúc tiếp theo

1. Bổ sung integration tests cho upload và duplicate.
2. Tiếp tục migrate `Tags` và `Collections` sang slice theo feature.
3. Thêm ADR (Architecture Decision Records) cho các quyết định quan trọng.
4. Thiết lập checklist kiến trúc bắt buộc khi thêm endpoint mới.

---

## 7. Tài liệu nguồn liên quan

- `docs/PROJECT_DOCUMENTATION.md`
- `docs/IMPLEMENTATION_GUIDE.md`
- `docs/OOP_ASSESSMENT.md`
- `docs/CHANGELOG.md`
- `docs/FIX_REPORT_20260227.md`
- `docs/PHASE1_ARCHIVE.md`
