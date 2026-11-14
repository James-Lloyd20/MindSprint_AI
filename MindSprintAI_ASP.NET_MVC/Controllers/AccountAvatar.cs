using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MindSprintAI_ASP.NET_MVC.Services;

namespace MindSprintAI_ASP.NET_MVC.Controllers;

public partial class AccountController
{
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("account-post"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GoogleAvatarVisibility(bool enabled, [FromServices] AvatarService avatars)
    {
        await avatars.SetVisibleAsync(UserId, enabled);
        Success("Đã lưu tùy chọn ảnh đại diện.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("account-post"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> RemoveAvatar([FromServices] AvatarService avatars)
    {
        await avatars.RemoveAsync(UserId);
        Success("Đã gỡ ảnh đại diện.");
        return RedirectToAction(nameof(Index));
    }
}
