using Microsoft.AspNetCore.Mvc;
using MindSprintAI_ASP.NET_MVC.Services;

namespace MindSprintAI_ASP.NET_MVC.Controllers;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class ImportController(ExcelImportService excel) : AppController
{
    public async Task<IActionResult> Index(int id)
    {
        await excel.EnsureCanImportAsync(id, UserId);
        return View(id);
    }

    public async Task<IActionResult> Template(int id)
    {
        await excel.EnsureCanImportAsync(id, UserId);
        return File(ExcelTemplate.Create(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mau-nhap-cau-hoi-va-the-ghi-nho.xlsx");
    }

    [HttpPost, RequestSizeLimit(6 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> Preview(int id, IFormFile? file)
    {
        await excel.EnsureCanImportAsync(id, UserId);
        if (file == null || file.Length == 0) throw new BusinessException("Hãy chọn file Excel.");
        if (file.Length > ExcelTemplate.MaxBytes) throw new BusinessException("File tối đa 5 MB.");
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer);
        return View(await excel.PreviewAsync(id, buffer.ToArray(), file.FileName, UserId));
    }

    // Trang xem trước chỉ có sau khi gửi tệp; mở lại bằng địa chỉ (tải lại, quay lại) thì về trang chọn tệp thay vì trang lỗi.
    [HttpGet, ActionName("Preview")]
    public IActionResult PreviewAgain(int id) => RedirectToAction(nameof(Index), new { id });

    // Lỗi khi kiểm tra/lưu: hiện lại bảng đang sửa (tới 500 dòng) thay vì chuyển trang làm mất hết.
    [HttpPost, RequestSizeLimit(12 * 1024 * 1024), RequestFormLimits(ValueCountLimit = 30000, ValueLengthLimit = 32768)]
    public Task<IActionResult> Recheck(ImportPreview input) => KeepFormOnErrorAsync(async () =>
    {
        if (!ModelState.IsValid) throw new BusinessException("Bảng xem trước có ô chưa đúng định dạng. Hãy kiểm tra lại các ô vừa sửa.");
        return View("Preview", await excel.RecheckAsync(input, UserId));
    }, () => Task.FromResult<IActionResult>(View("Preview", input)));

    [HttpPost, RequestSizeLimit(12 * 1024 * 1024), RequestFormLimits(ValueCountLimit = 30000, ValueLengthLimit = 32768)]
    public Task<IActionResult> Save(ImportPreview input) => KeepFormOnErrorAsync(async () =>
    {
        if (!ModelState.IsValid) throw new BusinessException("Bảng xem trước có ô chưa đúng định dạng. Hãy kiểm tra lại các ô vừa sửa.");
        var checkedInput = await excel.RecheckAsync(input, UserId);
        if (checkedInput.Rows.Any(r => r.Include && r.Error != null))
        {
            TempData["Error"] = ExcelImportService.RowsHaveErrors;
            return View("Preview", checkedInput);
        }
        var count = await excel.SaveAsync(checkedInput, UserId);
        Success($"Đã thêm {count} dòng hợp lệ.");
        return RedirectToAction("Quiz", "StudySets", new { id = input.QuizId });
    }, () => Task.FromResult<IActionResult>(View("Preview", input)));
}
