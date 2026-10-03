using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Implementations;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Implementations;
using HighSchoolManagementSystem.Services.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

// Integration checks use the real SQL Server provider, services, authentication and Razor views.
// Only the unique database created by this process is removed. No application database is used.
var database = "ParentChecks_" + Guid.NewGuid().ToString("N");
var server = Environment.GetEnvironmentVariable("PARENT_CHECKS_SERVER") ?? @"(localdb)\MSSQLLocalDB";
var connection = new SqlConnectionStringBuilder
{
    DataSource = server, InitialCatalog = database, IntegratedSecurity = true,
    TrustServerCertificate = true, ConnectTimeout = 15
}.ConnectionString;
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options;
var passed = 0;
Process? app = null;
var created = false;
var log = new System.Collections.Concurrent.ConcurrentQueue<string>();
void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    passed++;
}
AppDbContext Db() => new(options);
ParentService Service(AppDbContext db) => new(new ParentRepository(db), new UserRepository(db),
    new BCryptPasswordHasher(), NullLogger<ParentService>.Instance);
CreateParentDto ParentInput(string username) => new()
{
    Username = username, FullName = "Phụ huynh kiểm thử", Email = username + "@example.test",
    Password = "ParentTest@123", PhoneNumber = "0912345678", Occupation = "Giáo viên"
};

try
{
    // Create explicitly so cleanup never targets a pre-existing database.
    await using (var master = new SqlConnection(new SqlConnectionStringBuilder(connection) { InitialCatalog = "master" }.ConnectionString))
    {
        await master.OpenAsync();
        await using var command = master.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{database}]";
        await command.ExecuteNonQueryAsync();
        created = true;
    }
    await using (var db = Db()) await DbInitializer.SeedAsync(db);

    int parentId;
    int otherParentId;
    await using (var db = Db())
    {
        var result = await Service(db).CreateParentAsync(ParentInput("parent_check"));
        Check(result.Success && result.Data!.User.Role.RoleName == "Parent" && result.Data.User.IsActive, "Create parent and active Parent account");
        parentId = result.Data!.ParentId;
        Check(new BCryptPasswordHasher().VerifyPassword("ParentTest@123", result.Data.User.PasswordHash), "Stored password is a valid BCrypt hash");
        otherParentId = (await Service(db).CreateParentAsync(ParentInput("other_parent"))).Data!.ParentId;
    }
    await using (var db = Db())
    {
        var service = Service(db);
        var count = await db.Users.CountAsync();
        Check(!(await service.CreateParentAsync(ParentInput("PARENT_CHECK"))).Success, "Reject duplicate username case-insensitively");
        var duplicate = ParentInput("unique_name");
        duplicate.Email = "PARENT_CHECK@example.test";
        Check(!(await service.CreateParentAsync(duplicate)).Success, "Reject duplicate email case-insensitively");
        var invalid = ParentInput("invalid_parent");
        invalid.DateOfBirth = DateTime.UtcNow.AddDays(1);
        Check(!(await service.CreateParentAsync(invalid)).Success, "Reject future date of birth");
        invalid.DateOfBirth = null;
        invalid.PhoneNumber = "123";
        Check(!(await service.CreateParentAsync(invalid)).Success, "Reject malformed phone number");
        Check(await db.Users.CountAsync() == count, "Invalid create leaves no orphan account");
        Check((await service.GetParentsAsync("parent_check")).Data!.Count == 1, "Search parents by username");
        Check((await service.GetParentsAsync("does-not-exist")).Data!.Count == 0, "Empty search result");
        Check(!(await service.GetParentAsync(int.MaxValue)).Success, "Missing parent returns failure");
        var before = await db.Parents.Include(p => p.User).SingleAsync(p => p.ParentId == parentId);
        var hash = before.User.PasswordHash;
        var update = new UpdateParentDto { FullName = "Tên đã cập nhật", Email = "updated@example.test", EmergencyContact = "0987654321" };
        Check((await service.UpdateParentAsync(parentId, update)).Success, "Update profile and emergency contact");
        Check(before.User.PasswordHash == hash && before.User.Username == "parent_check" && before.User.Role.RoleName == "Parent", "Editing preserves credentials and role");
        update.Email = "other_parent@example.test";
        Check(!(await service.UpdateParentAsync(parentId, update)).Success && before.User.Email == "updated@example.test", "Duplicate edit email does not change profile");
    }

    int[] ids;
    await using (var db = Db())
    {
        var role = await db.Roles.SingleAsync(r => r.RoleName == "Student");
        var students = Enumerable.Range(1, 4).Select(i => new Student
        {
            StudentCode = "CHECK" + i,
            User = new User { Username = "student_check" + i, Email = $"student_check{i}@example.test", FullName = "Học sinh " + i, PasswordHash = "unused", RoleId = role.RoleId }
        }).ToArray();
        db.Students.AddRange(students);
        await db.SaveChangesAsync();
        ids = students.Select(s => s.Id).ToArray();
    }
    await using (var db = Db())
    {
        var service = Service(db);
        Check(!(await service.LinkStudentsAsync(parentId, Array.Empty<int>())).Success, "Reject empty selection");
        Check((await service.LinkStudentsAsync(parentId, new[] { ids[0], ids[1], ids[0] })).Success, "Link multiple students and deduplicate IDs");
        Check((await service.GetParentAsync(parentId)).Data!.Students.Count == 2, "Details include linked students");
    }
    await using (var db = Db())
    {
        var service = Service(db);
        Check(!(await service.LinkStudentsAsync(otherParentId, new[] { ids[0], ids[2] })).Success, "Reject reassignment of another parent's student");
        Check(await db.Students.Where(s => s.Id == ids[2]).Select(s => s.ParentId).SingleAsync() == null, "Conflicting batch is atomic");
        Check(!(await service.LinkStudentsAsync(parentId, new[] { ids[2], int.MaxValue })).Success, "Reject non-existent student in batch");
        Check(await db.Students.Where(s => s.Id == ids[2]).Select(s => s.ParentId).SingleAsync() == null, "Missing student batch is atomic");
        Check((await service.GetAvailableStudentsAsync("CHECK")).Data!.Count == 2, "Available search excludes already linked students");
        var parent = await db.Parents.Include(p => p.User).SingleAsync(p => p.ParentId == otherParentId);
        parent.User.IsActive = false;
        await db.SaveChangesAsync();
        Check(!(await service.LinkStudentsAsync(otherParentId, new[] { ids[2] })).Success, "Locked parent cannot receive links");
        parent.User.IsActive = true;
        parent.User.RoleId = (await db.Roles.SingleAsync(r => r.RoleName == "Teacher")).RoleId;
        await db.SaveChangesAsync();
        Check(!(await service.LinkStudentsAsync(otherParentId, new[] { ids[2] })).Success, "Wrong role cannot receive links");
    }

    var studentClassId = await StudentChecks.Services(Db, Check);

    // Exercise actual HTTP middleware, antiforgery, binding and Razor rendering.
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    var url = $"http://127.0.0.1:{port}";
    var mvcDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../HighSchoolManagementSystem.MVC"));
    var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
    var assembly = Path.Combine(mvcDirectory, "bin", configuration, "net8.0", "HighSchoolManagementSystem.MVC.dll");
    var start = new ProcessStartInfo("dotnet") { WorkingDirectory = mvcDirectory, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    start.ArgumentList.Add(assembly);
    start.ArgumentList.Add("--urls");
    start.ArgumentList.Add(url);
    start.Environment["ConnectionStrings__DefaultConnection"] = connection;
    start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    app = Process.Start(start)!;
    app.OutputDataReceived += (_, e) => { if (e.Data != null) log.Enqueue(e.Data); };
    app.ErrorDataReceived += (_, e) => { if (e.Data != null) log.Enqueue(e.Data); };
    app.BeginOutputReadLine();
    app.BeginErrorReadLine();
    HttpClient Client() => new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(10) };
    using var staff = Client();
    for (var attempt = 0; ; attempt++)
    {
        try { await staff.GetAsync("/Auth/Login"); break; }
        catch (HttpRequestException) when (attempt < 40) { await Task.Delay(250); }
    }
    async Task<string> Token(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        return WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    }
    async Task Login(HttpClient client, string username, string password)
    {
        var token = await Token(client, "/Auth/Login");
        var response = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["UsernameOrEmail"] = username, ["Password"] = password, ["__RequestVerificationToken"] = token }));
        Check(response.StatusCode == HttpStatusCode.Redirect, "Login: " + username);
    }
    Check((await staff.GetAsync("/ParentManagement")).Headers.Location?.OriginalString.Contains("/Auth/Login") == true, "Anonymous access requires login");
    await Login(staff, "staff", "Staff@123");
    foreach (var path in new[] { "/ParentManagement", "/ParentManagement/Create", $"/ParentManagement/Details/{parentId}", $"/ParentManagement/Edit/{parentId}", $"/ParentManagement/LinkStudents/{parentId}" })
        Check((await staff.GetAsync(path)).StatusCode == HttpStatusCode.OK, "Render " + path);
    Check((await staff.GetAsync("/ParentManagement/Details/2147483647")).StatusCode == HttpStatusCode.NotFound, "Missing detail returns HTTP 404");
    Check((await staff.PostAsync("/ParentManagement/Create", new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode == HttpStatusCode.BadRequest, "Reject POST without antiforgery token");
    var createForm = new Dictionary<string, string>
    {
        ["Username"] = "http_parent", ["Password"] = "ParentTest@123", ["ConfirmPassword"] = "different",
        ["FullName"] = "HTTP Parent", ["Email"] = "http_parent@example.test", ["__RequestVerificationToken"] = await Token(staff, "/ParentManagement/Create")
    };
    var invalidResponse = await staff.PostAsync("/ParentManagement/Create", new FormUrlEncodedContent(createForm));
    Check(invalidResponse.StatusCode == HttpStatusCode.OK && WebUtility.HtmlDecode(await invalidResponse.Content.ReadAsStringAsync()).Contains("Mật khẩu xác nhận không khớp"), "MVC rejects mismatched password confirmation");
    createForm["ConfirmPassword"] = "ParentTest@123";
    var createdResponse = await staff.PostAsync("/ParentManagement/Create", new FormUrlEncodedContent(createForm));
    Check(createdResponse.StatusCode == HttpStatusCode.Redirect && createdResponse.Headers.Location!.OriginalString.Contains("Details"), "Create form saves and redirects to details");
    var linkFields = new[]
    {
        new KeyValuePair<string, string>("selectedStudentIds", ids[2].ToString()),
        new KeyValuePair<string, string>("selectedStudentIds", ids[3].ToString()),
        new KeyValuePair<string, string>("__RequestVerificationToken", await Token(staff, $"/ParentManagement/LinkStudents/{parentId}"))
    };
    Check((await staff.PostAsync($"/ParentManagement/LinkStudents/{parentId}", new FormUrlEncodedContent(linkFields))).StatusCode == HttpStatusCode.Redirect, "Link form binds multiple selections");
    await using (var db = Db()) Check(await db.Students.CountAsync(s => s.ParentId == parentId) == 4, "HTTP link persists both selected students");
    using var parentClient = Client();
    await Login(parentClient, "http_parent", "ParentTest@123");
    Check((await parentClient.GetAsync("/ParentManagement")).Headers.Location?.OriginalString.Contains("/Auth/AccessDenied") == true, "Parent account cannot access staff management");
    await StudentChecks.Http(Db, staff, Client, Token, Login, studentClassId, Check);
    Console.WriteLine($"All {passed} checks passed.");
}
catch
{
    foreach (var line in log.TakeLast(30)) Console.Error.WriteLine(line);
    throw;
}
finally
{
    if (app is { HasExited: false }) { app.Kill(entireProcessTree: true); await app.WaitForExitAsync(); }
    app?.Dispose();
    SqlConnection.ClearAllPools();
    if (created)
    {
        await using var master = new SqlConnection(new SqlConnectionStringBuilder(connection) { InitialCatalog = "master" }.ConnectionString);
        await master.OpenAsync();
        await using var command = master.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]";
        await command.ExecuteNonQueryAsync();
    }
}
