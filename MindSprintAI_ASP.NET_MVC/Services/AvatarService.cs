using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MindSprintAI_ASP.NET_MVC.Data;
using MindSprintAI_ASP.NET_MVC.Data.Entities;

namespace MindSprintAI_ASP.NET_MVC.Services;

public class AvatarService(AppDbContext db, TimeProvider clock)
{
    public const string GooglePictureClaim = "urn:google:picture";

    public static string? GoogleUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 500
            || !Uri.TryCreate(value, UriKind.Absolute, out var uri) || !uri.IsWellFormedOriginalString()
            || uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length != 0
            || Regex.IsMatch(value, @"^https://[^/?#]*@", RegexOptions.IgnoreCase)
            || !uri.Host.EndsWith(".googleusercontent.com", StringComparison.OrdinalIgnoreCase)) return null;
        // Chỉ thay cỡ khi Google đã trả dạng hỗ trợ =s…; giữ phần cắt ảnh và chuỗi truy vấn.
        var path = Regex.Replace(uri.AbsolutePath, @"=s[0-9]+(?=(?:-[a-zA-Z0-9]+)*$)", "=s128");
        var url = uri.GetLeftPart(UriPartial.Authority) + path + uri.Query + uri.Fragment;
        return url.Length <= 500 ? url : null;
    }

    public static bool CaptureGoogle(ApplicationUser user, string provider, ClaimsPrincipal principal, DateTime now)
    {
        if (provider != "Google" || GoogleUrl(principal.FindFirstValue(GooglePictureClaim)) is not { } url) return false;
        user.AvatarUrl = url;
        user.AvatarSource = AvatarSource.Google;
        user.AvatarUpdatedAt = now;
        return true;
    }

    public static void Clear(ApplicationUser user, DateTime now)
    {
        user.AvatarUrl = null;
        user.AvatarSource = AvatarSource.None;
        user.AvatarUpdatedAt = now;
    }

    public async Task SetVisibleAsync(string userId, bool enabled)
    {
        var user = await OwnerAsync(userId);
        user.AvatarHidden = !enabled;
        await db.SaveChangesAsync();
    }

    public async Task RemoveAsync(string userId)
    {
        Clear(await OwnerAsync(userId), clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync();
    }

    private async Task<ApplicationUser> OwnerAsync(string userId) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == userId && !u.IsGuest) ?? throw new ForbiddenException();
}
