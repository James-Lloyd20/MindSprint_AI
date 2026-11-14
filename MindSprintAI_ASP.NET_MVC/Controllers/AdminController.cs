using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindSprintAI_ASP.NET_MVC.Data.Entities;
using MindSprintAI_ASP.NET_MVC.Models;
using MindSprintAI_ASP.NET_MVC.Services;

namespace MindSprintAI_ASP.NET_MVC.Controllers;

[Authorize(Policy = "SystemAdmin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdminController(AdminService admin, AccountLockService locks, ContentReportService reports, ClassTransferService transfers,
    UnhideRequestService unhide) : AppController
{
    // (NK5) Cùng guard SystemAdmin và AdminTwoFactorMiddleware với toàn khu Admin.
    [HttpGet]
    public async Task<IActionResult> Audit([FromQuery] AuditFilter filter, [FromServices] AuditQueryService logs) =>
        View(await logs.QueryAsync(UserId, filter));

    public async Task<IActionResult> ClassTransfers() => View(await transfers.AdminClassesAsync(UserId));

    [HttpPost]
    public async Task<IActionResult> TransferClass(int id, string email)
    {
        await transfers.AdminOfferAsync(id, email, UserId);
        Success(InvitationService.SentMessage);
        return RedirectToAction(nameof(ClassTransfers));
    }
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        ViewData["PendingUnhideCount"] = await unhide.PendingCountAsync(UserId);
        return View(await admin.UsersAsync(UserId, search, page));
    }

    public async Task<IActionResult> UnhideRequests() => View(await unhide.PendingAsync(UserId));

    // Ghi rõ tên view: DecideUnhide gọi lại hàm này để hiện lại form khi bị từ chối.
    public async Task<IActionResult> UnhideRequest(int id) => View(nameof(UnhideRequest), await unhide.GetAsync(id, UserId));

    [HttpPost]
    public Task<IActionResult> DecideUnhide(int id, bool approve, string? note) => KeepFormOnErrorAsync(async () =>
    {
        await unhide.DecideAsync(id, approve, note, UserId);
        Success(approve ? "Đã bỏ ẩn bộ học." : "Đã từ chối yêu cầu.");
        return RedirectToAction(nameof(UnhideRequests));
    }, async () =>
    {
        ViewData[approve ? "ApprovalNote" : "RejectionNote"] = note;
        return await UnhideRequest(id);
    });

    public async Task<IActionResult> Lock(string id, int? reportId)
    {
        if (reportId != null) id = await reports.PendingOwnerAsync(reportId.Value, UserId);
        var target = await admin.UserAsync(id, UserId);
        return View(new AdminLockModel { UserId = target.Id, DisplayName = target.DisplayName,
            ReportId = reportId, Kind = reportId == null ? AccountLockKind.Protective : AccountLockKind.Violation });
    }

    [HttpPost]
    public async Task<IActionResult> Lock(AdminLockModel model)
    {
        if (!ModelState.IsValid) throw new BusinessException("Ngày giờ không hợp lệ. Nhập theo giờ Việt Nam.");
        if (model.ReportId is { } rid)
            await reports.LockReportedUserAsync(rid, model.Reason, VnTime.FromInput(model.EndsAt), UserId, locks);
        else await locks.LockAsync(model.UserId, model.Kind, model.Reason, VnTime.FromInput(model.EndsAt), UserId);
        Success("Đã khóa tài khoản và dừng các lượt đang làm.");
        return RedirectToAction(nameof(Index));
    }

    // Pháp lý C9: xóa thay khi người dùng gửi yêu cầu qua email; cùng điều kiện chặn như tự xóa.
    public async Task<IActionResult> DeleteUser(string id, [FromServices] AccountDeletionService deletion, string? reason = null)
    {
        var target = await admin.UserAsync(id, UserId);
        ViewData["Block"] = target.DeletedAt == null ? await deletion.BlockReasonAsync(id, byAdmin: true) : "Tài khoản này đã được xóa.";
        return View(new AdminDeleteUserModel { UserId = target.Id, DisplayName = target.DisplayName, Email = target.Email, Reason = reason });
    }

    [HttpPost]
    public Task<IActionResult> DeleteUser(AdminDeleteUserModel model, [FromServices] AccountDeletionService deletion) => KeepFormOnErrorAsync(async () =>
    {
        await deletion.DeleteByAdminAsync(model.UserId, model.Reason, UserId);
        Success("Đã xóa tài khoản.");
        return RedirectToAction(nameof(Index));
    }, () => DeleteUser(model.UserId, deletion, model.Reason)); // giữ lý do vừa nhập khi bị chặn

    [HttpPost]
    public async Task<IActionResult> Unlock(string id)
    {
        await locks.UnlockAsync(id, UserId);
        Success("Đã mở khóa tài khoản.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Role(string id, bool grant, string? reason)
    {
        await admin.ChangeRoleAsync(id, grant, reason, UserId);
        Success("Đã cập nhật quyền quản trị viên.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> History()
    {
        var rows = await admin.HistoryAsync(UserId);
        ViewData["Names"] = await admin.UserLabelsAsync(rows.SelectMany(r => new[] { r.AdminId, r.TargetUserId }).Distinct().ToList());
        return View(rows);
    }
    public async Task<IActionResult> Reports(int page = 1)
    {
        ViewData["Page"] = Math.Max(1, page);
        return View(await reports.QueueAsync(UserId, page));
    }
    public async Task<IActionResult> Report(int id) => View(await reports.ViewAsync(id, UserId));

    [HttpPost]
    public async Task<IActionResult> HandleReport(int id, bool hide, string? reason)
    {
        await reports.HandleAsync(id, hide, reason, UserId);
        Success(hide ? "Đã ẩn bộ học và đóng ngay các bài giao đang chạy của bộ học này." : "Đã bác bỏ báo cáo.");
        return RedirectToAction(nameof(Reports));
    }
}
