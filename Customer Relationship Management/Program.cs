using Customer_Relationship_Management.Data;
using Customer_Relationship_Management.Mappings;
// Lưu ý: Sắp xếp lại using để tránh nhầm lẫn giữa Implementations và Implements
using Customer_Relationship_Management.Repositories.Implements;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Implementations; // Chứa BackupService
using Customer_Relationship_Management.Services.Implements;      // Chứa UserService, AdminService...
using Customer_Relationship_Management.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Customer_Relationship_Management
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // --- 1. Cấu hình DB Context ---
            builder.Services.AddDbContext<B2BDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // --- 2. Cấu hình Session & Razor Pages ---
            builder.Services.AddSession();
            builder.Services.AddRazorPages().AddJsonOptions(opt =>
            {
                opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
                opt.JsonSerializerOptions.WriteIndented = true;
            });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddAutoMapper(typeof(MappingProfile));

            // --- 3. Cấu hình Authentication ---
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromHours(1);
                });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnRedirectToLogin = ctx =>
                {
                    if (ctx.Request.Path.StartsWithSegments("/api"))
                    {
                        ctx.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    }
                    ctx.Response.Redirect(ctx.RedirectUri);
                    return Task.CompletedTask;
                };
            });

            builder.Services.AddAuthorization();

            // --- 4. Đăng ký Repositories (Data Layer) ---
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            builder.Services.AddScoped<IDealRepository, DealRepository>();
            builder.Services.AddScoped<IContractRepository, ContractRepository>();
            builder.Services.AddScoped<ITaskRepository, TaskRepository>();
            builder.Services.AddScoped<IBackupRepository, BackupRepository>();

            // --- 5. Đăng ký Services (Business Layer) ---
            // Đã xóa dòng IContractService bị trùng
            builder.Services.AddScoped<IContractService, Customer_Relationship_Management.Services.Implements.ContractService>();

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ICustomerService, CustomerService>();
            builder.Services.AddScoped<IAuditLogService, AuditLogService>();
            builder.Services.AddScoped<IDealService, DealService>();
            builder.Services.AddScoped<ITaskService, TaskService>();
            builder.Services.AddScoped<IBackupService, BackupService>();

            // Đã kiểm tra: AdminService đã được đăng ký tại đây
            builder.Services.AddScoped<IAdminService, AdminService>();

            // ⚠️ LƯU Ý QUAN TRỌNG:
            // Nếu bạn CHƯA tạo file NotificationService.cs, hãy comment dòng dưới lại để tránh lỗi Build
            // builder.Services.AddScoped<INotificationService, NotificationService>();
            // Nếu đã có file đó rồi thì mở comment ra:
            builder.Services.AddScoped<INotificationService, NotificationService>();

            var app = builder.Build();

            // --- 6. Seed Data ---
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var db = services.GetRequiredService<B2BDbContext>();
                    // db.Database.Migrate(); // Mở nếu muốn chạy migration tự động
                    db.EnsureSeedData();
                }
                catch (Exception ex)
                {
                    // Log lỗi nếu Seed Data thất bại để dễ debug
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Lỗi xảy ra khi khởi tạo dữ liệu (Seeding DB).");
                }
            }

            // --- 7. Middleware Pipeline ---
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            app.MapGet("/", context =>
            {
                context.Response.Redirect("/Account/Login");
                return Task.CompletedTask;
            });

            app.MapRazorPages();
            app.MapControllers();

            app.Run();
        }
    }
}