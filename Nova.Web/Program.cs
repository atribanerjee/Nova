using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Nova.DB;
using Nova.DB.Utitlity;
using Nova.Web.Interfaces;
using Nova.Web.Models;
using Nova.Web.Utitlity;

var builder = WebApplication.CreateBuilder(args);

// Configure DbContext with connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<NovaDBContext>(options =>
    options.UseSqlServer(connectionString));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddScoped<IUserServices, UserServices>();
builder.Services.AddScoped<IUtilityServices, UtilityServices>();
builder.Services.AddScoped<IuserRoleService, UserRole>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(5);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Accounts/Login";
        options.LogoutPath = "/Accounts/LogOut";
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // Set default expiration time
        options.SlidingExpiration = true;
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Use session
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Accounts}/{action=Login}/{id?}");

app.Run();
