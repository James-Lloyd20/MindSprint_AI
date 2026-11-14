using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MindSprintAI_ASP.NET_MVC.Data.Entities;
using MindSprintAI_ASP.NET_MVC.Models;
using MindSprintAI_ASP.NET_MVC.Services;

namespace MindSprintAI_ASP.NET_MVC.Controllers;

[AllowAnonymous]
public class HomeController(UserManager<ApplicationUser> users) : Controller
{
    // Đã đăng nhập thì vào màn hình chính theo vai đang chọn (37B, 38A).
    public async Task<IActionResult> Index([FromServices] AssignmentService? assignments = null)
    {
        if (User.Identity?.IsAuthenticated != true) return View();
        var user = await users.GetUserAsync(User);
        if (user == null) return View();
        if (user.CurrentMode != UserMode.Lecturer) return RedirectToAction("Index", "Learn");
        assignments ??= HttpContext.RequestServices.GetRequiredService<AssignmentService>();
        return View("Lecturer", await assignments.GetLecturerDashboardAsync(user.Id, user.DisplayName));
    }

    [HttpGet("chinh-sach"), AllowGuest, AllowWithoutPolicy]
    public IActionResult Privacy([FromServices] IConfiguration config) => View(LegalInfo.From(config));

    [HttpGet("dieu-khoan"), AllowGuest, AllowWithoutPolicy]
    public IActionResult Terms([FromServices] IConfiguration config) => View(LegalInfo.From(config));

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true), AllowWithoutPolicy]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
