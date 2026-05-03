# Project Refactoring Notes - HaengSungAOI2

## 📅 Nhật ký hệ thống (System Logs)

### 2026-05-03: Khởi tạo kế hoạch tái cấu trúc
- **Trạng thái**: Đã lập kế hoạch (Implementation Plan).
- **Quyết định quan trọng**:
    - Sử dụng `Microsoft.Extensions.Hosting` (Generic Host) cho ứng dụng WPF.
    - Áp dụng `CommunityToolkit.Mvvm` để thay thế toàn bộ logic INotifyPropertyChanged thủ công.
    - Chuyển đổi mô hình Singleton tĩnh sang DI Managed Services.
    - **Nguyên tắc Refactor**: Bảo toàn tuyệt đối logic nghiệp vụ hiện có, đồng thời loại bỏ các hàm/biến không còn sử dụng (Dead Code).
    - **Kiến trúc Service**: Chia nhỏ logic thành các Service chuyên biệt theo nhóm (Machine, Vision, Data, UI) để giữ ViewModel cực mỏng (Lean ViewModel).

---

## 🛠 Danh sách công việc (Task List)

- [ ] **Giai đoạn 1**: Cấu hình DI & MVVM Foundation.
- [ ] **Giai đoạn 2**: Bóc tách logic PLC & Database Services.
- [ ] **Giai đoạn 3**: Tích hợp Vision Service (VisionMaster).
- [ ] **Giai đoạn 4**: Refactor MainWindow & UI Binding.

---

## 💡 Ghi chú kỹ thuật (Technical Notes)
- `MainWindow.xaml.cs` hiện tại chứa rất nhiều logic xử lý sự kiện trực tiếp từ UI. Cần cẩn thận khi chuyển sang Command để không làm mất logic validation.
- Hệ thống VisionMaster (MVS) có thể yêu cầu Thread-Safety đặc biệt khi giao tiếp từ Background Thread tới UI.
- Các Brush và Resource màu sắc nên được đưa vào `App.xaml` hoặc một `DesignSystem.xaml` riêng thay vì tạo thủ công trong code-behind.
- **Vision Image Display**: Sử dụng cơ chế chuyển đổi `Bitmap` sang `BitmapSource` (WPF-friendly) trong `ImageDisplayService` để ViewModel có thể Bind trực tiếp, đảm bảo tách biệt hoàn toàn View và Vision SDK.
