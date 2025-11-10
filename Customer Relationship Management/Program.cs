using Customer_Relationship_Management.Data;
using Customer_Relationship_Management.Mappings;
using Customer_Relationship_Management.Repositories.Implements;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Implements;
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



            // --- Services ---
            builder.Services.AddSession();

            builder.Services.AddRazorPages().AddJsonOptions(opt =>
            {
                opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
                opt.JsonSerializerOptions.WriteIndented = true;
            }); ;

            builder.Services.AddDbContext<B2BDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
     .AddCookie(options =>
     {
         options.LoginPath = "/Account/Login";
         options.ExpireTimeSpan = TimeSpan.FromHours(1);
     });
            builder.Services.AddScoped<IContractService, ContractService>();
            builder.Services.AddScoped<IContractService, Customer_Relationship_Management.Services.Implements.ContractService>();

            // ⚠️ Thêm đoạn này NGAY SAU phần cấu hình AddAuthentication ở trên
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnRedirectToLogin = ctx =>
                {
                    // Nếu là request tới API, thì trả về mã 401 thay vì redirect
                    if (ctx.Request.Path.StartsWithSegments("/api"))
                    {
                        ctx.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    }

                    // Các request thông thường vẫn redirect về Login như cũ
                    ctx.Response.Redirect(ctx.RedirectUri);
                    return Task.CompletedTask;
                };
            });


            builder.Services.AddAuthorization();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddAutoMapper(typeof(MappingProfile));





            // Repositories
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            builder.Services.AddScoped<IDealRepository, DealRepository>();
            builder.Services.AddScoped<IContractRepository, ContractRepository>();
            builder.Services.AddScoped<ITaskRepository, TaskRepository>();




            // Services
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ICustomerService, CustomerService>();
            builder.Services.AddScoped<IAuditLogService, AuditLogService>();
            builder.Services.AddScoped<IDealService, DealService>();
            builder.Services.AddScoped<ITaskService, TaskService>();



            ////chạy trên máy khác
            //builder.WebHost.UseUrls("http://localhost:5197", "http://172.16.71.57:5197");


            var app = builder.Build();

            // --- Seed dữ liệu ---
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<B2BDbContext>();
              //  db.Database.Migrate();
                db.EnsureSeedData();
            }

            // --- Middleware pipeline ---
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication(); // phải trước Authorization
            app.UseAuthorization();
            app.UseSession();

            // --- Redirect root "/" về Login ---
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
