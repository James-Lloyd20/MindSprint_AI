using Microsoft.EntityFrameworkCore;
using MindSprintAI_ASP.NET_MVC.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Tắt MARS (AuditConnectionString) để EF dùng được savepoint khi ghi nhật ký.
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(AppDbContextFactory.AuditConnectionString(builder.Configuration.GetConnectionString("Default"))));
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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


app.Run();
