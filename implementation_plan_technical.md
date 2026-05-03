# Kế hoạch Tái cấu trúc Hệ thống HaengSungAOI2 (Elite Edition)

Kế hoạch này tập trung vào việc hiện đại hóa codebase của dự án HaengSungAOI2, chuyển đổi từ mô hình "Spaghetti Code" trong Code-behind sang kiến trúc **Service-Oriented MVVM** sử dụng các công nghệ tiên tiến nhất của .NET.

## 1. Mục tiêu và Tầm nhìn
- **Decoupling (Bóc tách)**: Tách rời Logic nghiệp vụ (PLC, Vision, DB) khỏi Giao diện (WPF).
- **Dead Code Elimination**: Rà soát và loại bỏ triệt để các hàm, biến không sử dụng để làm gọn nhẹ dự án.
- **Logic Preservation**: Đảm bảo 100% logic vận hành của máy (PLC polling, Vision flows) được giữ nguyên vẹn khi chuyển sang cấu trúc mới.
- **Maintainability (Tính bảo trì)**: Giảm kích thước `MainWindow.xaml.cs` từ >2000 dòng xuống <100 dòng.

## 2. User Review Required

> [!IMPORTANT]
> **Thay đổi Cơ chế Khởi động (App Startup):** 
> Tôi sẽ chuyển đổi `App.xaml.cs` sang sử dụng **Generic Host (`IHost`)**. Đây là tiêu chuẩn công nghiệp hiện nay để quản lý Dependency Injection, Logging và Configuration một cách đồng nhất.
>
> **Thư viện NuGet cần cài đặt:**
> - `CommunityToolkit.Mvvm`: Thư viện MVVM chính thức từ Microsoft.
> - `Microsoft.Extensions.Hosting`: Quản lý vòng đời ứng dụng.
> - `Microsoft.Extensions.DependencyInjection`: Container cho DI.
> - `Microsoft.Extensions.Logging.Serilog`: (Đề xuất) Để có hệ thống log mạnh mẽ hơn.

## 3. Câu hỏi mở (Open Questions)
- Bạn có ưu tiên sử dụng một thư viện Logging cụ thể nào không (ví dụ: Serilog, NLog)? Hiện tại tôi thấy dự án đang dùng log thủ công.
- Các module VisionMaster có yêu cầu chạy trên Thread UI không? (Điều này quan trọng để thiết kế cơ chế Task/Async phù hợp).

---

## 4. Các thay đổi đề xuất

### A. Tầng Nền tảng & Cấu trúc (Infrastructure)

#### [MODIFY] [App.xaml.cs](file:///f:/Dev/Projects/AI%20Project/HaengSungAOI2/HaengSungAOI_WPF/App.xaml.cs)
- Implement `IHost` để quản lý DI.
- Đăng ký Singletons cho các Service dùng chung: `IPlcService`, `IVisionService`, `IDbService`.
- Đăng ký ViewModels: `MainViewModel`, `SettingViewModel`.

---

### 3. Tầng ViewModel (Presentation Layer)
#### [DELETE] [ViewModelBase.cs](file:///f:/Dev/Projects/AI%20Project/HaengSungAOI2/HaengSungAOI_WPF/ViewModels/ViewModelBase.cs)
- Loại bỏ lớp base tự viết.
#### [NEW] [MainViewModel.cs](file:///f:/Dev/Projects/AI%20Project/HaengSungAOI2/HaengSungAOI_WPF/ViewModels/MainViewModel.cs)
- Kế thừa trực tiếp từ `ObservableObject` (CommunityToolkit.Mvvm).
- Sử dụng Source Generators (`[ObservableProperty]`) để quản lý State.

### B. Tầng Dịch vụ (Core Services)

#### [NEW] [IPlcService.cs](file:///f:/Dev/Projects/AI%20Project/HaengSungAOI2/HaengSungAOI_WPF/Services/PLC/IPlcService.cs) & [PlcService.cs](file:///f:/Dev/Projects/AI%20Project/HaengSungAOI2/HaengSungAOI_WPF/Services/PLC/PlcService.cs)
- Đóng gói toàn bộ logic Modbus/PLC.
- Sử dụng `CancellationToken` để quản lý việc polling dữ liệu an toàn khi tắt app.

#### [NEW] [IVisionService.cs](file:///f:/Dev/Projects/AI%20Project/HaengSungAOI2/HaengSungAOI_WPF/Services/Vision/IVisionService.cs)
- Bọc các API của VisionMaster (IMVS...).
- Cung cấp các Event hoặc Observable để báo cáo kết quả kiểm tra về UI.

### C. Tầng Hiển thị (Presentation)

#### [MODIFY] [MainWindow.xaml.cs](file:///f:/Dev/Projects/AI%20Project/HaengSungAOI2/HaengSungAOI_WPF/Views/MainWindow.xaml.cs)
- Xóa bỏ 90% code hiện tại.
- Chỉ giữ lại logic gán `DataContext` từ DI container.

---

## 5. Lộ trình thực hiện (Roadmap)

### Giai đoạn 1: Thiết lập cấu trúc DI & MVVM Toolkit
1. Cài đặt các thư viện NuGet cần thiết.
2. Refactor `App.xaml.cs` để khởi tạo DI Container.
3. Xóa bỏ `ViewModelBase.cs` cũ.
- Cấu trúc thư mục mới: `Services`, `ViewModels`, `Models`, `Views`.
- Setup `App.xaml.cs` với Generic Host.

### Giai đoạn 2: Tách biệt Service Logic (PLC & DB)
- Chuyển code từ `Machine.PLC.cs` sang `PlcService`.
- Đảm bảo việc kết nối PLC không còn phụ thuộc vào UI.

### Giai đoạn 3: Hiện đại hóa Vision Module
- Tạo `VisionService` để quản lý các Procedure của VisionMaster.
- Tách biệt logic xử lý ảnh khỏi MainWindow.

### Giai đoạn 4: Refactor MainWindow sang MVVM
- Chuyển logic UI (Update timer, Error display) sang `MainViewModel`.
- Thiết lập DataBinding trong XAML.

---

## 6. Kế hoạch xác minh (Verification Plan)

### Kiểm tra tự động
- Sử dụng console output để trace quá trình khởi tạo Service qua DI.
- Viết thử nghiệm Unit Test nhỏ cho `PlcService` để đảm bảo đọc/ghi thanh ghi đúng.

### Kiểm tra thủ công
- Chạy phần mềm và kết nối với PLC thật (hoặc Simulator).
- Kiểm tra các nút nhấn trên HMI: Start, Stop, Reset.
- Kiểm tra việc lưu kết quả vào DB SQLite/Oracle.
- Quan sát Memory Leak (đảm bảo các Timer/Task được Dispose đúng cách).
