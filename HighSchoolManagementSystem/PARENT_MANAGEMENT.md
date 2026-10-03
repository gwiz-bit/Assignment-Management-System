# Quản lý phụ huynh

Đăng nhập bằng tài khoản **Staff**, chọn **Quản lý đào tạo → Danh sách phụ huynh**
hoặc truy cập `/ParentManagement`.

## Chức năng

| Yêu cầu | Trang và thao tác |
| --- | --- |
| 7.1 Danh sách | Tìm theo họ tên, tên đăng nhập, email, số điện thoại; lọc tài khoản đang hoạt động/đã khóa. |
| 7.2 Chi tiết | Xem thông tin cá nhân, liên hệ, nghề nghiệp, liên hệ khẩn cấp, trạng thái tài khoản và học sinh đã liên kết. |
| 7.3 Tạo | Tạo hồ sơ và tài khoản Parent đang hoạt động trong cùng một lần lưu; mật khẩu được băm bằng BCrypt. |
| 7.4 Cập nhật | Sửa thông tin cá nhân/liên hệ; giữ nguyên tên đăng nhập, mật khẩu, vai trò và các liên kết. |
| 7.5 Liên kết | Từ hồ sơ chọn **Liên kết học sinh**, tìm theo mã/họ tên, chọn một hoặc nhiều học sinh rồi lưu. |

## Quy tắc nghiệp vụ

- Chỉ Staff được truy cập các trang và thao tác quản lý phụ huynh.
- Tên đăng nhập và email phải duy nhất trên toàn hệ thống, không phân biệt hoa thường.
- Tên đăng nhập gồm 3–50 ký tự: chữ không dấu, số, dấu chấm, gạch dưới.
- Mật khẩu khởi tạo bắt buộc, 6–64 ký tự và tối đa 72 byte UTF-8; xác nhận phải khớp.
- Số điện thoại nếu nhập phải có 10 chữ số, bắt đầu bằng 0. Ngày sinh từ năm 1920 và trước ngày hiện tại.
- Tận dụng quan hệ hiện có `Parent.Students` / `Student.ParentId`: một phụ huynh có nhiều học sinh; một học sinh có tối đa một phụ huynh.
- Staff xác minh quan hệ phụ huynh–học sinh trước khi liên kết. Tài khoản nhận liên kết phải đang hoạt động và có vai trò Parent.
- Chỉ hiển thị học sinh chưa liên kết. Server kiểm tra lại toàn bộ lựa chọn trong giao dịch `Serializable`; không ghi đè phụ huynh khác. Nếu một học sinh không hợp lệ, cả đợt liên kết không được lưu.
- Liên kết mới bổ sung vào các học sinh hiện có. Lưu lựa chọn trước khi thay đổi từ khóa tìm kiếm.
- Quản lý khóa tài khoản tiếp tục dùng chức năng quản lý tài khoản hiện có.

Không cần migration mới. Ứng dụng cần database SQL Server đã áp dụng các migration hiện có.

## Kiểm tra

Từ thư mục gốc repository:

```powershell
dotnet build HighSchoolManagementSystem/HighSchoolManagementSystem.slnx
dotnet run --project HighSchoolManagementSystem/HighSchoolManagementSystem.ParentChecks
```

`ParentChecks` là chương trình kiểm tra tích hợp chạy bằng `dotnet run`, không cần thêm thư viện test.
Chương trình cũng chạy các ca quản lý học sinh trong `StudentChecks.cs`; xem `STUDENT_MANAGEMENT.md`.
Mặc định dùng SQL Server LocalDB `(localdb)\MSSQLLocalDB` với Windows Authentication.
Có thể chọn SQL Server kiểm thử khác bằng `$env:PARENT_CHECKS_SERVER = "."`.
Tài khoản chạy kiểm tra cần quyền tạo database.

Bộ kiểm tra tạo database tên ngẫu nhiên `ParentChecks_<guid>`, chạy migration và seed trên database đó,
kiểm tra repository/service và HTTP thực tế (cookie đăng nhập, Razor, model validation, antiforgery, phân quyền),
rồi dừng ứng dụng kiểm thử và xóa đúng database vừa tạo trong `finally`.
Database trong `appsettings.json` không được sử dụng bởi bộ kiểm tra.

Các ca chính: tạo tài khoản và xác minh mật khẩu băm; trùng tên/email; ngày sinh/số điện thoại sai;
cập nhật giữ nguyên thông tin đăng nhập; tìm kiếm; ID không tồn tại; liên kết nhiều học sinh;
chặn chuyển học sinh đã liên kết; không lưu một phần khi có ID sai; tài khoản bị khóa/sai vai trò;
render cả năm trang; POST thiếu antiforgery; xác nhận mật khẩu sai; đăng nhập bằng tài khoản vừa tạo;
Parent không có quyền quản lý.
