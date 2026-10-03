using System.Net;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Implementations;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Implementations;
using HighSchoolManagementSystem.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

internal static class StudentChecks
{
    private static StudentService Service(AppDbContext db) => new(new StudentRepository(db), new UserRepository(db),
        new BCryptPasswordHasher(), NullLogger<StudentService>.Instance);

    private static CreateStudentDto Input(string suffix) => new()
    {
        StudentCode = "ST" + suffix, Username = "student_test_" + suffix, Email = $"student_test_{suffix}@example.test",
        FullName = "Học sinh kiểm thử " + suffix, Password = "StudentTest@123", PhoneNumber = "0912345678",
        DateOfBirth = new DateTime(2010, 1, 1), AdmissionDate = DateTime.UtcNow.Date, Address = "Hà Nội"
    };

    internal static async Task<int> Services(Func<AppDbContext> database, Action<bool, string> check)
    {
        int studentId, secondId, concurrentId, currentClassId, otherClassId, oldClassId;
        await using (var db = database())
        {
            var service = Service(db);
            var created = await service.CreateStudentAsync(Input("001"));
            check(created.Success && created.Data!.User.Role.RoleName == "Student", "Student: create profile and Student account");
            studentId = created.Data!.Id;
            check(new BCryptPasswordHasher().VerifyPassword("StudentTest@123", created.Data.User.PasswordHash), "Student: BCrypt password verifies");
            secondId = (await service.CreateStudentAsync(Input("002"))).Data!.Id;
            concurrentId = (await service.CreateStudentAsync(Input("003"))).Data!.Id;
            var count = await db.Users.CountAsync();
            var invalid = Input("004");
            invalid.StudentCode = "st001";
            check(!(await service.CreateStudentAsync(invalid)).Success, "Student: reject duplicate student code ignoring case");
            invalid.StudentCode = "ST004";
            invalid.Username = "STUDENT_TEST_001";
            check(!(await service.CreateStudentAsync(invalid)).Success, "Student: reject duplicate username");
            invalid.Username = "student_test_004";
            invalid.Email = "STUDENT_TEST_001@example.test";
            check(!(await service.CreateStudentAsync(invalid)).Success, "Student: reject duplicate email");
            invalid.Email = "student_test_004@example.test";
            invalid.AdmissionDate = invalid.DateOfBirth!.Value.AddDays(-1);
            check(!(await service.CreateStudentAsync(invalid)).Success, "Student: reject admission before birth");
            invalid.AdmissionDate = DateTime.UtcNow.AddDays(2);
            check(!(await service.CreateStudentAsync(invalid)).Success, "Student: reject future admission");
            invalid.AdmissionDate = DateTime.UtcNow.Date;
            invalid.PhoneNumber = "123";
            check(!(await service.CreateStudentAsync(invalid)).Success, "Student: reject invalid phone");
            invalid.PhoneNumber = null;
            invalid.Status = "arbitrary";
            check(!(await service.CreateStudentAsync(invalid)).Success, "Student: reject invalid status");
            check(await db.Users.CountAsync() == count, "Student: rejected registrations create no accounts");
            check((await service.GetStudentsAsync("st001")).Data!.Single().Id == studentId, "Student: search by code");
            check((await service.GetStudentsAsync("student_test_001@example.test")).Data!.Count == 1, "Student: search by email");
            check((await service.GetStudentsAsync("no_such_student")).Data!.Count == 0, "Student: empty search");
            check(!(await service.GetStudentAsync(int.MaxValue)).Success, "Student: missing profile");

            var year = await db.AcademicYears.SingleAsync(y => y.IsCurrent);
            var oldYear = await db.AcademicYears.FirstAsync(y => !y.IsCurrent);
            var gradeId = await db.GradeLevels.Select(g => g.Id).FirstAsync();
            var classes = new[]
            {
                new Class { ClassName = "TEST-A", AcademicYearId = year.Id, GradeLevelId = gradeId },
                new Class { ClassName = "TEST-B", AcademicYearId = year.Id, GradeLevelId = gradeId },
                new Class { ClassName = "TEST-OLD", AcademicYearId = oldYear.Id, GradeLevelId = gradeId }
            };
            db.Classes.AddRange(classes);
            await db.SaveChangesAsync();
            currentClassId = classes[0].Id; otherClassId = classes[1].Id; oldClassId = classes[2].Id;
            check((await service.GetCurrentClassesAsync()).Data!.All(c => c.AcademicYear.IsCurrent), "Student: only current-year class options");
            check(!(await service.AssignClassAsync(studentId, oldClassId)).Success, "Student: reject non-current-year class");
            check(!(await service.AssignClassAsync(studentId, int.MaxValue)).Success, "Student: reject missing class");
            check(!(await service.AssignClassAsync(int.MaxValue, currentClassId)).Success, "Student: reject missing student");
            check(!(await service.AssignClassAsync(studentId, 0)).Success, "Student: require class selection");
            check((await service.AssignClassAsync(studentId, currentClassId)).Success, "Student: assign current-year class");
            check(!(await service.AssignClassAsync(studentId, currentClassId)).Success, "Student: reject duplicate assignment");
            check(!(await service.AssignClassAsync(studentId, otherClassId)).Success, "Student: assignment cannot transfer class");
        }
        await using (var db = database())
        {
            var service = Service(db);
            check(await db.StudentClasses.CountAsync(sc => sc.StudentId == studentId) == 1, "Student: repeated assignment leaves one membership");
            check((await service.GetStudentsAsync(classId: currentClassId)).Data!.Any(s => s.Id == studentId), "Student: filter by current class");
            check((await service.GetStudentsAsync(unassignedOnly: true)).Data!.All(s => s.Id != studentId), "Student: unassigned filter excludes assigned students");
            var student = (await service.GetStudentAsync(studentId)).Data!;
            student.ParentId = await db.Parents.OrderBy(p => p.ParentId).Select(p => p.ParentId).FirstAsync();
            await db.SaveChangesAsync();
            var parentId = student.ParentId;
            var password = student.User.PasswordHash;
            var update = Input("001");
            update.StudentCode = "ST002";
            check(!(await service.UpdateStudentAsync(studentId, update)).Success, "Student: reject duplicate code on edit");
            update.StudentCode = "ST001";
            update.Email = "student_test_002@example.test";
            check(!(await service.UpdateStudentAsync(studentId, update)).Success, "Student: reject duplicate email on edit");
            update.Email = "student_test_001@example.test";
            update.FullName = "Tên học sinh đã cập nhật";
            update.Address = "Địa chỉ mới";
            check((await service.UpdateStudentAsync(studentId, update)).Success, "Student: update profile");
            check(student.ParentId == parentId && student.User.PasswordHash == password && student.User.Role.RoleName == "Student"
                && student.StudentClasses.Single().ClassId == currentClassId, "Student: edit preserves parent, credentials, role and class");
            var inactive = await db.Students.SingleAsync(s => s.Id == secondId);
            inactive.Status = "Inactive";
            await db.SaveChangesAsync();
            check(!(await service.AssignClassAsync(secondId, currentClassId)).Success, "Student: inactive student cannot be assigned");
            check((await service.GetStudentsAsync(status: "Inactive")).Data!.Any(s => s.Id == secondId), "Student: filter by status");
            inactive.Status = "Active";
            db.StudentClasses.Add(new StudentClass { StudentId = secondId, ClassId = oldClassId, Status = "Enrolled" });
            await db.SaveChangesAsync();
            check((await service.AssignClassAsync(secondId, currentClassId)).Success, "Student: prior-year membership does not block new-year assignment");
        }
        // Independent contexts simulate two staff requests racing to assign the same student.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Assign(int classId)
        {
            await gate.Task;
            await using var db = database();
            return (await Service(db).AssignClassAsync(concurrentId, classId)).Success;
        }
        var requests = new[] { Assign(currentClassId), Assign(otherClassId) };
        gate.SetResult();
        var outcomes = await Task.WhenAll(requests);
        check(outcomes.Count(success => success) == 1, "Student: exactly one simultaneous assignment succeeds");
        await using (var db = database())
        {
            check(await db.StudentClasses.CountAsync(sc => sc.StudentId == concurrentId) == 1, "Student: concurrent assignment persists only one class");
            var membership = await db.StudentClasses.SingleAsync(sc => sc.StudentId == concurrentId);
            membership.Status = "Left"; membership.LeftAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            check(!(await Service(db).AssignClassAsync(concurrentId, currentClassId)).Success, "Student: closed membership cannot bypass transfer restriction");
            var year = await db.AcademicYears.SingleAsync(y => y.IsCurrent);
            year.IsCurrent = false;
            await db.SaveChangesAsync();
            check((await Service(db).GetCurrentClassesAsync()).Data!.Count == 0 && (await Service(db).GetCurrentYearAsync()).Data == null, "Student: handle missing current year");
            year.IsCurrent = true;
            await db.SaveChangesAsync();
        }
        return currentClassId;
    }

    internal static async Task Http(Func<AppDbContext> database, HttpClient staff, Func<HttpClient> client,
        Func<HttpClient, string, Task<string>> token, Func<HttpClient, string, string, Task> login,
        int classId, Action<bool, string> check)
    {
        using var anonymous = client();
        check((await anonymous.GetAsync("/StudentManagement")).Headers.Location?.OriginalString.Contains("/Auth/Login") == true, "Student HTTP: anonymous requires login");
        var form = new Dictionary<string, string>
        {
            ["StudentCode"] = "STHTTP", ["FullName"] = "HTTP Student", ["Email"] = "student_http@example.test",
            ["Username"] = "student_http", ["Password"] = "StudentTest@123", ["ConfirmPassword"] = "wrong",
            ["Status"] = "Active", ["__RequestVerificationToken"] = await token(staff, "/StudentManagement/Create")
        };
        check((await staff.GetAsync("/StudentManagement")).StatusCode == HttpStatusCode.OK, "Student HTTP: list renders");
        var invalid = await staff.PostAsync("/StudentManagement/Create", new FormUrlEncodedContent(form));
        check(invalid.StatusCode == HttpStatusCode.OK && WebUtility.HtmlDecode(await invalid.Content.ReadAsStringAsync()).Contains("Mật khẩu xác nhận không khớp"), "Student HTTP: mismatched password validation");
        form["ConfirmPassword"] = "StudentTest@123";
        var response = await staff.PostAsync("/StudentManagement/Create", new FormUrlEncodedContent(form));
        check(response.StatusCode == HttpStatusCode.Redirect, "Student HTTP: create form succeeds");
        int id;
        await using (var db = database()) id = await db.Students.Where(s => s.StudentCode == "STHTTP").Select(s => s.Id).SingleAsync();
        foreach (var action in new[] { "Details", "Edit", "AssignClass" })
            check((await staff.GetAsync($"/StudentManagement/{action}/{id}")).StatusCode == HttpStatusCode.OK, "Student HTTP: render " + action);
        foreach (var action in new[] { "Create", "Edit", "AssignClass" })
            check((await staff.PostAsync($"/StudentManagement/{action}/{id}", new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode == HttpStatusCode.BadRequest, "Student HTTP: antiforgery on " + action);
        check((await staff.GetAsync("/StudentManagement/Details/2147483647")).StatusCode == HttpStatusCode.NotFound, "Student HTTP: unknown ID is 404");
        var edit = new Dictionary<string, string>
        {
            ["StudentCode"] = "STHTTP", ["FullName"] = "HTTP Student Edited", ["Email"] = "student_http@example.test", ["Status"] = "Active",
            ["Address"] = "Edited address", ["RoleId"] = "1", ["ParentId"] = "1", ["IsActive"] = "false", ["Username"] = "tampered",
            ["__RequestVerificationToken"] = await token(staff, $"/StudentManagement/Edit/{id}")
        };
        check((await staff.PostAsync($"/StudentManagement/Edit/{id}", new FormUrlEncodedContent(edit))).StatusCode == HttpStatusCode.Redirect, "Student HTTP: update form succeeds");
        await using (var db = database())
        {
            var student = await db.Students.Include(s => s.User).ThenInclude(u => u.Role).SingleAsync(s => s.Id == id);
            check(student.User.FullName == "HTTP Student Edited" && student.Address == "Edited address", "Student HTTP: edited values persist");
            check(student.User.Username == "student_http" && student.User.IsActive && student.User.Role.RoleName == "Student" && student.ParentId == null, "Student HTTP: ignore overposted account/parent fields");
        }
        var assignment = new Dictionary<string, string>
        {
            ["ClassId"] = "0", ["__RequestVerificationToken"] = await token(staff, $"/StudentManagement/AssignClass/{id}")
        };
        var missing = await staff.PostAsync($"/StudentManagement/AssignClass/{id}", new FormUrlEncodedContent(assignment));
        check(missing.StatusCode == HttpStatusCode.OK && WebUtility.HtmlDecode(await missing.Content.ReadAsStringAsync()).Contains("Vui lòng chọn lớp học"), "Student HTTP: require class selection");
        assignment["ClassId"] = classId.ToString();
        check((await staff.PostAsync($"/StudentManagement/AssignClass/{id}", new FormUrlEncodedContent(assignment))).StatusCode == HttpStatusCode.Redirect, "Student HTTP: assignment form succeeds");
        var roster = await staff.GetStringAsync($"/Staff/ClassStudents/{classId}");
        check(roster.Contains("STHTTP") && roster.Contains("HTTP Student Edited"), "Student HTTP: assigned student appears in existing class roster");
        var detail = WebUtility.HtmlDecode(await staff.GetStringAsync($"/StudentManagement/Details/{id}"));
        check(detail.Contains("TEST-A"), "Student HTTP: detail shows assigned class");
        using var studentClient = client();
        await login(studentClient, "student_http", "StudentTest@123");
        check((await studentClient.GetAsync("/StudentManagement")).Headers.Location?.OriginalString.Contains("/Auth/AccessDenied") == true, "Student HTTP: student cannot access staff management");
        using var parentClient = client();
        await login(parentClient, "http_parent", "ParentTest@123");
        check((await parentClient.GetAsync("/StudentManagement")).Headers.Location?.OriginalString.Contains("/Auth/AccessDenied") == true, "Student HTTP: parent cannot access staff management");
    }
}
