# Quản lý học sinh

Đăng nhập bằng tài khoản **Staff**, chọn **Quản lý đào tạo → Hồ sơ học sinh**,
hoặc truy cập `/StudentManagement`.

## Phạm vi

| Mục | Chức năng |
| --- | --- |
| 6.1 | Danh sách; tìm theo mã, họ tên, tên đăng nhập, email, số điện thoại; lọc trạng thái, lớp hiện hành hoặc chưa phân lớp. |
| 6.2 | Chi tiết hồ sơ, tài khoản, liên hệ, phụ huynh đã liên kết và lớp trong năm học hiện hành. |
| 6.3 | Tạo hồ sơ học sinh cùng tài khoản Student đang hoạt động; mã học sinh, tài khoản và email phải duy nhất. |
| 6.4 | Cập nhật mã học sinh, thông tin cá nhân, ngày nhập học, địa chỉ và trạng thái. |
| 6.6 | Phân học sinh đang học vào một lớp của năm học hiện hành; kết quả xuất hiện trong danh sách lớp có sẵn. |

Không triển khai **6.5 – lịch sử học tập** và **6.7 – chuyển lớp** theo phạm vi yêu cầu.

## Cách sử dụng

1. Chọn **Thêm học sinh**, nhập hồ sơ và thông tin đăng nhập, xác nhận mật khẩu rồi lưu.
2. Tại hồ sơ, chọn **Cập nhật** để sửa thông tin hoặc **Phân lớp** để chọn lớp tiếp nhận.
3. Để phân lớp, cần thiết lập một năm học hiện hành tại **Danh sách năm học** và tạo lớp thuộc năm đó.
4. Chọn lớp rồi nhấn **Xác nhận phân lớp**. Sau khi lưu, hồ sơ hiển thị lớp và liên kết **Xem danh sách lớp**.
5. Liên kết phụ huynh sử dụng chức năng quản lý phụ huynh đã có: chọn phụ huynh → **Liên kết học sinh**.

## Quy tắc

- Các trang và thao tác quản lý chỉ dành cho Staff; POST có kiểm tra anti-forgery.
- Mã học sinh tối đa 20 ký tự, gồm chữ không dấu, số, gạch ngang/gạch dưới, lưu dạng viết hoa.
- Mã học sinh, tên đăng nhập và email được kiểm tra trùng không phân biệt hoa thường.
- Mật khẩu bắt buộc 6–64 ký tự, tối đa 72 byte UTF-8, được băm bằng BCrypt.
- Ngày sinh từ năm 1920 và trước ngày hiện tại; ngày nhập học từ năm 1920, không ở tương lai và phải sau ngày sinh nếu có nhập cả hai.
- Các trạng thái học sinh: `Active` (Đang học), `Inactive` (Ngừng học), `Graduated` (Đã tốt nghiệp).
- Chỉ học sinh `Active` được phân lớp. Trạng thái học sinh độc lập với trạng thái khóa tài khoản và không tự sửa các bản ghi phân lớp.
- Mỗi học sinh chỉ được phân một lớp trong năm học hiện hành. Nếu đã có bản ghi phân lớp trong năm đó, kể cả bản ghi đã kết thúc, thao tác bị từ chối; không chuyển, xóa hoặc thay lớp cũ.
- Phân lớp ở năm trước không ngăn phân lớp ở năm hiện hành. Trang hồ sơ chỉ trình bày lớp hiện hành, không cung cấp lịch sử học tập.
- Quy tắc năm học dựa trên cờ `AcademicYear.IsCurrent` do Staff thiết lập.
- Phân lớp chạy trong transaction `Serializable` và khóa cập nhật trên học sinh để xử lý hai yêu cầu đồng thời. Kiểm tra lớp, năm học và trạng thái được thực hiện lại tại server khi lưu.
- Tạo tài khoản/hồ sơ dùng một lần `SaveChanges`; không tạo tài khoản mồ côi khi lưu thất bại.
- Cập nhật hồ sơ giữ nguyên tài khoản, mật khẩu, vai trò, phụ huynh và các bản ghi phân lớp. Các trường gửi thêm ngoài DTO không được ghi vào entity.

Tận dụng các bảng `students`, `users`, `student_classes`, `classes`, `academic_years` hiện có;
không cần migration mới.

## Build và kiểm thử

Từ thư mục gốc repository:

```powershell
dotnet build HighSchoolManagementSystem/HighSchoolManagementSystem.slnx
dotnet run --project HighSchoolManagementSystem/HighSchoolManagementSystem.ParentChecks
```

Bộ chạy `ParentChecks` đã được mở rộng với `StudentChecks.cs` để kiểm tra cả hai module.
Mặc định dùng SQL Server LocalDB `(localdb)\MSSQLLocalDB`, Windows Authentication;
có thể đổi máy chủ kiểm thử bằng `$env:PARENT_CHECKS_SERVER = "."`.
Tài khoản chạy cần quyền tạo database. Chương trình tạo database ngẫu nhiên `ParentChecks_<guid>`,
seed dữ liệu và chạy ứng dụng HTTP riêng; cuối cùng dừng ứng dụng và xóa đúng database vừa tạo.
Không sử dụng database cấu hình trong `appsettings.json`.

Đã kiểm tra 96 điều kiện: các luồng phụ huynh trước đó, tạo/cập nhật học sinh, dữ liệu không hợp lệ,
trùng mã/tài khoản/email, bộ lọc, bảo toàn phụ huynh và tài khoản, phân lớp trùng/sai năm/đồng thời,
tài khoản mới đăng nhập được, phân quyền Staff, anti-forgery, chống sửa trường ngoài DTO,
render các trang Razor, lưu form qua HTTP và hiển thị học sinh trong danh sách lớp.
