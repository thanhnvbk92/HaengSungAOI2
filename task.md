# Tình trạng công việc (Task Progress)

## 🏁 Giai đoạn 1: Thiết lập nền tảng (Foundation)
- [x] Cài đặt các thư viện NuGet cần thiết (MVVM Toolkit, DI, Hosting).
- [x] Tạo cấu trúc thư mục chuẩn (`Services`, `ViewModels`, `Models`, `Views`).
- [x] Refactor `App.xaml.cs` để sử dụng `IHost` (Generic Host).
- [x] Xóa bỏ `ViewModelBase.cs` cũ.
- [x] Thiết lập `IGlobalStateService` thay thế cho biến static.

## ⚙️ Giai đoạn 2: Tách biệt Service (Machine & Data)
- [x] Xây dựng `IPlcService` và di chuyển logic Modbus từ `Machine.PLC.cs`.
- [x] Triển khai `IScanOutService` để quản lý máy quét mã vạch.
- [x] Triển khai `IMachineService` (Orchestrator) quản lý luồng Auto/Manual.
- [x] Tích hợp `AutoVisionDbService` vào DI Container.
- [x] Loại bỏ dead code trong quá trình bóc tách logic.

## 👁️ Giai đoạn 3: Tích hợp Vision Service
- [x] Xây dựng `IVisionService` bọc VisionMaster SDK.
- [x] Triển khai `IImageDisplayService` để đẩy hình ảnh lên UI.
- [x] Chuyển logic xử lý kết quả Vision sang `MachineService` và `VisionService`.

## 🖥️ Giai đoạn 4: Hoàn thiện MVVM cho MainWindow
- [x] Tạo `MainViewModel.cs` kế thừa `ObservableObject`.
- [x] Thiết lập Data Binding và Commands trong `MainWindow.xaml`.
- [x] Dọn dẹp hoàn toàn logic trong `MainWindow.xaml.cs`.
- [x] Kiểm tra vận hành tổng thể (End-to-End) và đảm bảo tính reactive.

---
*Ghi chú: [ ] chưa làm, [/] đang làm, [x] đã hoàn thành.*
