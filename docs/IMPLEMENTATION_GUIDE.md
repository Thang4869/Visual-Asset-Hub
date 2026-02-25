# Visual Asset Hub - Draggable Canvas Feature

## Tổng quan thay đổi

Tôi đã cải thiện ứng dụng của bạn với các tính năng sau:

### 1. Backend Changes (C# ASP.NET Core)
- **Thêm properties vị trí:** PositionX và PositionY vào model Asset để lưu trữ vị trí hình ảnh trên canvas
- **Tạo endpoint cập nhật vị trí:** `PUT /api/Assets/{id}/position` - Cho phép client gửi vị trí mới của hình ảnh
- **Tạo DTO:** AssetPositionDto để nhận dữ liệu từ client

### 2. Frontend Changes (React)
- **Tạo DraggableAssetCanvas component:** Cho phép kéo thả hình ảnh trên canvas
- **Thêm canvas view mode:** Hiển thị hình ảnh trong một không gian có thể kéo thả thay vì grid thông thường
- **Thêm grid view mode toggle:** Buttons để chuyển đổi giữa Canvas view và Grid view
- **Save position on drag:** Khi kéo thả xong, vị trí được lưu lên backend tự động
- **Restore position on load:** Khi tải lại web, hình ảnh sẽ hiển thị tại vị trí đã lưu

## Các file được thay đổi/tạo

### Backend:
1. [Models/Asset.cs](VAH.Backend/Models/Asset.cs) - Thêm PositionX, PositionY
2. [Controllers/AssetsController.cs](VAH.Backend/Controllers/AssetsController.cs) - Thêm UpdateAssetPosition endpoint

### Frontend:
1. [src/App.jsx](VAH.Frontend/src/App.jsx) - Thêm viewMode state, import DraggableAssetCanvas
2. [src/components/DraggableAssetCanvas.jsx](VAH.Frontend/src/components/DraggableAssetCanvas.jsx) - Component mới cho canvas
3. [src/components/DraggableAssetCanvas.css](VAH.Frontend/src/components/DraggableAssetCanvas.css) - Styles cho canvas
4. [src/App.css](VAH.Frontend/src/App.css) - Thêm styles cho view-mode-toggle

## Cách sử dụng

### 1. Chuẩn bị Database
Vì đã thêm các column mới (PositionX, PositionY), bạn cần xóa database cũ để nó tự động tạo lại schema mới:

```bash
cd b:\1A\VAH.Backend
# Xóa file database cũ
rm vah_database.db
# Hoặc trên Windows PowerShell:
Remove-Item vah_database.db -ErrorAction SilentlyContinue
```

### 2. Chạy Backend
```bash
cd b:\1A\VAH.Backend
dotnet run --configuration Development
```
- Backend sẽ khởi động trên http://localhost:5027
- Database sẽ tự động tạo với schema mới

### 3. Chạy Frontend
```bash
cd b:\1A\VAH.Frontend
npm install  # Nếu chưa cài dependencies
npm run dev
```
- Frontend sẽ khởi động trên http://localhost:5173

## Tính năng

### Canvas View (Mặc định):
- **Kéo thả hình ảnh:** Click và giữ để kéo hình ảnh đến vị trí mới
- **Save tự động:** Khi thả chuột, vị trí tự động được lưu lên backend
- **Grid nền:** Hiển thị lưới nền để giúp căn chỉnh
- **Tooltip:** Hiển thị tên file khi hover trên hình ảnh

### Grid View:
- Hiển thị hình ảnh dạng grid thông thường (giống như trước)

### Toggle View:
- Buttons trong header để chuyển đổi giữa Canvas ↔ Grid view

### Search & Tags:
- Tất cả chức năng tìm kiếm và lọc theo tags vẫn hoạt động bình thường

## Lưu ý

1. **Lần đầu khởi động:** Có thể mất vài giây để tạo database mới
2. **Vị trí lưu trữ:** Mỗi hình ảnh đã được kéo thả một lần sẽ có vị trí được lưu
3. **Images không hiển thị:** Đảm bảo backend đang chạy và đường dẫn `/uploads/` được mapping đúng
4. **Position reset:** Nếu upload hình ảnh mới, vị trí mặc định sẽ là (0, 0)

## API Endpoints

### Lấy danh sách assets (có vị trí lưu)
```
GET /api/Assets
```
Response: Array của Asset objects với PositionX, PositionY

### Cập nhật vị trí asset
```
PUT /api/Assets/{id}/position
Body: {
  "positionX": 100,
  "positionY": 150
}
```

### Upload file (vị trí mặc định 0,0)
```
POST /api/Assets/upload
Body: FormData với files
```
