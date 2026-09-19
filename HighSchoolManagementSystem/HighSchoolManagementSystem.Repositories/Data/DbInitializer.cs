using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Áp dụng migration tự động nếu có
        await context.Database.MigrateAsync();

        // 2. Khởi tạo 5 Roles chuẩn theo yêu cầu nghiệp vụ
        var roles = new List<Role>
        {
            new Role { RoleName = "Principal", Description = "Hiệu trưởng - Giám sát, thống kê và báo cáo toàn trường" },
            new Role { RoleName = "Staff", Description = "Nhân viên - Hành chính nhân sự, cán bộ đoàn, quản trị nội bộ" },
            new Role { RoleName = "Teacher", Description = "Giáo viên - Quản lý giảng dạy, điểm số, điểm danh học sinh" },
            new Role { RoleName = "Student", Description = "Học sinh - Tra cứu điểm số, thời khóa biểu, chuyên cần" },
            new Role { RoleName = "Parent", Description = "Phụ huynh - Theo dõi tình hình học tập của con em" }
        };

        foreach (var role in roles)
        {
            if (!await context.Roles.AnyAsync(r => r.RoleName == role.RoleName))
            {
                await context.Roles.AddAsync(role);
            }
        }
        await context.SaveChangesAsync();

        // Lấy lại các roles đã lưu để lấy RoleId
        var principalRole = await context.Roles.FirstAsync(r => r.RoleName == "Principal");
        var staffRole = await context.Roles.FirstAsync(r => r.RoleName == "Staff");
        var teacherRole = await context.Roles.FirstAsync(r => r.RoleName == "Teacher");
        var studentRole = await context.Roles.FirstAsync(r => r.RoleName == "Student");
        var parentRole = await context.Roles.FirstAsync(r => r.RoleName == "Parent");

        // 3. Khởi tạo 5 tài khoản mẫu
        // Mật khẩu được mã hóa an toàn bằng BCrypt
        if (!await context.Users.AnyAsync(u => u.Username == "principal"))
        {
            var principalUser = new User
            {
                Username = "principal",
                Email = "principal@highschool.edu.vn",
                FullName = "Thầy Nguyễn Văn Hiệu Trưởng",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Principal@123", 11),
                PhoneNumber = "0901234567",
                Gender = "Nam",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                RoleId = principalRole.RoleId
            };
            await context.Users.AddAsync(principalUser);
        }

        if (!await context.Users.AnyAsync(u => u.Username == "staff"))
        {
            var staffUser = new User
            {
                Username = "staff",
                Email = "staff@highschool.edu.vn",
                FullName = "Cô Trần Thị Quản Trị",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123", 11),
                PhoneNumber = "0912345678",
                Gender = "Nữ",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                RoleId = staffRole.RoleId
            };
            await context.Users.AddAsync(staffUser);
        }

        if (!await context.Users.AnyAsync(u => u.Username == "teacher"))
        {
            var teacherUser = new User
            {
                Username = "teacher",
                Email = "teacher@highschool.edu.vn",
                FullName = "Thầy Lê Văn Giáo Viên",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Teacher@123", 11),
                PhoneNumber = "0923456789",
                Gender = "Nam",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                RoleId = teacherRole.RoleId
            };
            await context.Users.AddAsync(teacherUser);
            await context.SaveChangesAsync();

            var teacherProfile = new Teacher
            {
                UserId = teacherUser.Id,
                TeacherCode = "GV001",
                Specialization = "Toán Học",
                Qualification = "Thạc sĩ Sư phạm Toán",
                HireDate = DateTime.UtcNow,
                Status = "Active"
            };
            await context.Teachers.AddAsync(teacherProfile);
        }

        Parent? parentEntity = null;
        if (!await context.Users.AnyAsync(u => u.Username == "parent"))
        {
            var parentUser = new User
            {
                Username = "parent",
                Email = "parent@highschool.edu.vn",
                FullName = "Phụ huynh Phạm Văn Hải",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Parent@123", 11),
                PhoneNumber = "0945678901",
                Gender = "Nam",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                RoleId = parentRole.RoleId
            };
            await context.Users.AddAsync(parentUser);
            await context.SaveChangesAsync();

            parentEntity = new Parent
            {
                UserId = parentUser.Id,
                Occupation = "Kỹ sư công nghệ",
                EmergencyContact = "0945678901"
            };
            await context.Parents.AddAsync(parentEntity);
            await context.SaveChangesAsync();
        }
        else
        {
            parentEntity = await context.Parents.FirstOrDefaultAsync();
        }

        if (!await context.Users.AnyAsync(u => u.Username == "student"))
        {
            var studentUser = new User
            {
                Username = "student",
                Email = "student@highschool.edu.vn",
                FullName = "Học sinh Phạm Minh Đức",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123", 11),
                PhoneNumber = "0934567890",
                Gender = "Nam",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
                RoleId = studentRole.RoleId
            };
            await context.Users.AddAsync(studentUser);
            await context.SaveChangesAsync();

            var studentProfile = new Student
            {
                UserId = studentUser.Id,
                StudentCode = "HS001",
                Status = "Active",
                AdmissionDate = DateTime.UtcNow,
                Address = "Số 123 Đường Giải Phóng, Hà Nội",
                ParentId = parentEntity?.ParentId
            };
            await context.Students.AddAsync(studentProfile);
        }

        await context.SaveChangesAsync();
    }
}
