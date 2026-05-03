# Implementation Plan - Tái cấu trúc MVVM Tuyệt đối cho HaengSungAOI2

Kế hoạch này thiết lập lộ trình để chuyển đổi dự án sang mô hình **Strict MVVM (MVVM Tuyệt đối)**, đảm bảo tính tách biệt hoàn toàn giữa Giao diện và Logic nghiệp vụ.

## 1. Nguyên tắc Cốt lõi (Core Principles)
- **View Integrity**: Giữ nguyên thiết kế và layout trong các file XAML hiện có. Không thay đổi giao diện trừ khi bắt buộc để phục vụ Data Binding.
- **Strict MVVM**: File code-behind (`.xaml.cs`) sẽ chỉ chứa hàm khởi tạo `InitializeComponent()`. 100% logic nghiệp vụ, xử lý sự kiện, và điều khiển máy sẽ được chuyển sang ViewModel và Service.
- **Dependency Injection (DI)**: Sử dụng DI Container để quản lý tập trung các dịch vụ như PLC, Database, Vision, và Logging.
- **Logic Preservation**: Bảo toàn mọi tính năng hiện có của máy, đồng thời dọn dẹp các hàm/biến không còn sử dụng (Dead Code).

## 2. Các thành phần công nghệ
- **UI Framework**: WPF (.NET Framework 4.8).
- **MVVM Engine**: `CommunityToolkit.Mvvm` (ObservableObject, RelayCommand).
- **DI Container**: `Microsoft.Extensions.DependencyInjection`.
- **App Lifecycle**: `Microsoft.Extensions.Hosting` (Generic Host).

---

## 3. Các thay đổi đề xuất theo tầng

### Tầng View (Giao diện)
#### [MODIFY] `MainWindow.xaml` & các UserControls
- Chuyển các sự kiện `Click`, `SelectionChanged` sang `Command` và `CommandParameter`.
- Thiết lập Data Binding cho tất cả các TextBlock, TextBox, Button state (IsEnabled, Visibility).
#### [MODIFY] `MainWindow.xaml.cs`
- Loại bỏ toàn bộ Timer, biến toàn cục, và các hàm xử lý logic.
- Code-behind chỉ còn nhiệm vụ duy nhất là nhận ViewModel qua DI và gán `DataContext`.

### Tầng Service (Logic Nghiệp vụ - Phân nhóm)
Dịch vụ được chia nhỏ và tổ chức theo thư mục để đảm bảo Single Responsibility:

#### 📂 Nhóm Machine
- `IPlcService`: Giao tiếp Modbus/PLC.
- `IMachineControlService`: Điều phối luồng chạy Auto/Manual.
- `IAlarmService`: Quản lý lỗi và cảnh báo.

#### 📂 Nhóm Vision
- `IVisionService`: Tích hợp VisionMaster API, quản lý việc thực thi Procedure.
- `IImageDisplayService`: Chuyên trách việc đẩy hình ảnh từ VisionMaster lên tầng UI (XAML Binding) một cách mượt mà, đảm bảo không treo UI.
- `IResultProcessingService`: Xử lý kết quả kiểm tra và hình ảnh.

#### 📂 Nhóm Data
- `IDataService`: Thao tác Database cơ sở.
- `IRecipeService`: Quản lý thông số model/recipe.
- `IHistoryService`: Quản lý dữ liệu lịch sử sản xuất.

#### 📂 Nhóm UI Support
- `ILoggingService`: Quản lý log hệ thống và log UI.
- `IDialogService`: Điều phối các cửa sổ popup và thông báo.

### Tầng ViewModel (Điều phối - Lean ViewModel)
- **Vai trò**: ViewModel chỉ chứa các thuộc tính cho Binding và gọi method từ Service. Không chứa logic xử lý chi tiết (ví dụ: không chứa logic tính toán kết quả Vision hay logic phân tích frame Modbus).

### Tầng Service (Logic Nghiệp vụ)
#### [NEW] `IPlcService` & `PlcService`
- Chịu trách nhiệm kết nối và trao đổi dữ liệu với PLC Modbus.
#### [NEW] `IVisionService` & `VisionService`
- Quản lý và thực thi các Procedure từ VisionMaster.
#### [NEW] `IDataService`
- Quản lý các thao tác với Database (SQLite/Oracle).

---

## 4. Lộ trình thực hiện (Roadmap)

### Giai đoạn 1: Thiết lập Nền tảng (Foundation)
- Cài đặt NuGet: `CommunityToolkit.Mvvm`, `Microsoft.Extensions.Hosting`.
- Refactor `App.xaml.cs` để cấu hình Generic Host và đăng ký DI.
- Xóa bỏ `ViewModelBase.cs` hiện tại (nếu có).

### Giai đoạn 2: Di cư Logic Dịch vụ & Dọn dẹp Code rác
- Phân tích `MainWindow.xaml.cs` và `Machine.PLC.cs` để xác định logic cần giữ lại.
- Xóa bỏ các hàm không có tham chiếu (Unused code).
- Chuyển logic PLC/DB sang các class Service tương ứng.

### Giai đoạn 3: Triển khai ViewModel & Binding
- Tạo `MainViewModel` và bóc tách logic xử lý UI từ MainWindow.
- Chuyển đổi các sự kiện Click sang Command.
- Kiểm tra tính toàn vẹn của Data Binding (đảm bảo hiển thị đúng như giao diện cũ).

### Giai đoạn 4: Kiểm tra & Nghiệm thu
- Kiểm tra vận hành máy thực tế.
- Kiểm tra log và lưu trữ dữ liệu.
- Hoàn thiện tài liệu `NOTEs.md`.

## 5. Kế hoạch xác minh

- **Xác minh MVVM**: Kiểm tra file `.xaml.cs`, nếu còn logic nghiệp vụ thì chưa đạt yêu cầu.
- **Xác minh Giao diện**: So sánh ảnh chụp màn hình trước và sau khi refactor để đảm bảo không có sự thay đổi về visual.
- **Xác minh Vận hành**: Chạy máy ở chế độ Auto, kiểm tra quy trình Quét mã -> Kiểm tra -> Lưu DB -> Xuất kết quả.
