# Walkthrough: Refactoring MainWindow sang MVVM Hoàn Chỉnh

Chúng ta đã hoàn thành việc chuyển đổi `MainWindow` từ mô hình code-behind imperative sang mô hình **Service-Oriented MVVM** một cách triệt để, đảm bảo tính phân tách hoàn toàn giữa giao diện và logic nghiệp vụ.

## Các Thay Đổi Chính

### 1. View: MainWindow.xaml & MainWindow.xaml.cs
- **Loại bỏ 100% Event Handlers**: Tất cả các sự kiện `Click`, `MouseDown`, `MouseUp` đã được gỡ bỏ khỏi code-behind.
- **Command Binding**: Sử dụng `RelayCommand` cho tất cả các nút chức năng (Sidebar, Sidebar Flags, Dashboard Edit).
- **HMI Behaviors**: Tích hợp `ButtonMouseCommandBehavior` để xử lý việc nhấn giữ nút HMI (PLC) mà không cần code-behind.
- **Reactive UI**: Sử dụng `LampBrushConverter` để tự động cập nhật màu sắc đèn HMI (`Auto`, `Manual`, `Start`, `Stop`, v.v.) dựa trên trạng thái từ `HmiViewModel`.
- **Lean Code-Behind**: `MainWindow.xaml.cs` hiện chỉ còn khoảng 40 dòng code, tập trung vào việc kết nối control `FrontEnd` (VisionMaster) vào hệ thống service.

### 2. ViewModel: MainViewModel & HmiViewModel
- **HmiViewModel**: Quản lý tập trung trạng thái của 6 đèn HMI chính và các thông số Dashboard (`PcbSlot`, `PcbTrayQuantity`, `BlankTrayQuantity`).
- **MainViewModel**: 
    - Quản lý trạng thái chung của máy (`IsRunning`, `IsInitialized`).
    - Xử lý chuyển đổi ngôn ngữ và giá trị EBR.
    - Cung cấp các lệnh điều hướng và cài đặt.
- **Decoupling**: ViewModel không còn phụ thuộc vào các control cụ thể của View, cho phép dễ dàng kiểm thử (Unit Test).

### 3. Services: IMachineService & IVisionService
- **Frontend Integration**: Cập nhật Interface để hỗ trợ truyền control `FrontEnd` từ View xuống lớp Service, đảm bảo VisionMaster có thể hiển thị hình ảnh bình thường trong kiến trúc mới.
- **Event-Driven**: Các service thông báo trạng thái qua các sự kiện (`LampStateChanged`, `QuantityChanged`), giúp UI luôn đồng bộ với dữ liệu thực tế từ PLC.

## Kết Quả Đạt Được

| Tính năng | Trước khi Refactor | Sau khi Refactor |
| :--- | :--- | :--- |
| **Logic HMI** | Polling bằng Timer trong Code-behind | Event-driven trong ViewModel |
| **Tương tác Nút** | Click/MouseDown events | RelayCommands & Behaviors |
| **Màu sắc đèn** | Gán thủ công trong Code-behind | Data Binding qua Converter |
| **MainWindow.xaml.cs** | ~1900 dòng | ~40 dòng |

## Hướng Dẫn Bảo Trì
- **Thêm nút HMI mới**: Chỉ cần thêm thuộc tính vào `HmiViewModel` và bind trong XAML bằng `ButtonMouseCommandBehavior`.
- **Thay đổi logic hiển thị**: Chỉnh sửa trong `LampBrushConverter` hoặc `HmiViewModel` mà không cần chạm vào View.
- **Mở rộng Dialog**: Có thể triển khai `IDialogService` và gọi từ `MainViewModel` để quản lý các cửa sổ popup.

---
*Hệ thống hiện đã sẵn sàng để tích hợp sâu hơn với các tính năng Vision và báo cáo mà không lo ngại về việc phình to code-behind.*
