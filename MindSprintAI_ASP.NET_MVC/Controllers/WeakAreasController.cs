using Microsoft.AspNetCore.Mvc;
using MindSprintAI_ASP.NET_MVC.Services;

namespace MindSprintAI_ASP.NET_MVC.Controllers;

// Trang "Điểm yếu" của nhóm. Khách bị NotGuestFilter chặn.
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class WeakAreasController(WeakAreaService weak) : AppController
{
    public async Task<IActionResult> Index() => View(await weak.GetAsync(UserId));
}
