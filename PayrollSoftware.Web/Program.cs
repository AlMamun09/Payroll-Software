using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PayrollSoftware.Data;
using PayrollSoftware.Infrastructure.Application.Interfaces;
using PayrollSoftware.Infrastructure.Identity;
using PayrollSoftware.Infrastructure.Repositories;
using PayrollSoftware.Infrastructure.Services;
using PayrollSoftware.Infrastructure.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
 options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<PayrollUser>(options =>
{
 options.SignIn.RequireConfirmedAccount = false;
 // Turn off all password validation rules
 options.Password.RequireDigit = false;
 options.Password.RequiredLength =1;
 options.Password.RequireNonAlphanumeric = false;
 options.Password.RequireUppercase = false;
 options.Password.RequireLowercase = false;

options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
options.Lockout.MaxFailedAccessAttempts =5;
options.Lockout.AllowedForNewUsers = true;
})
 .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDesignationRepository, DesignationRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IShiftRepository, ShiftRepository>();

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();

builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
 app.UseMigrationsEndPoint();
}
else
{
 app.UseExceptionHandler("/Home/Error");
 // The default HSTS value is30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
 app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
 name: "default",
 pattern: "{controller=Home}/{action=Index}/{id?}")
 .WithStaticAssets();

app.MapRazorPages()
 .WithStaticAssets();

app.Run();
