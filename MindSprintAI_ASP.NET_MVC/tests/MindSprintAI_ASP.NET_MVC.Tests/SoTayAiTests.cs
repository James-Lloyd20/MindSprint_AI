using Microsoft.EntityFrameworkCore;
using MindSprintAI_ASP.NET_MVC.Data.Entities;
using MindSprintAI_ASP.NET_MVC.Services;

namespace MindSprintAI_ASP.NET_MVC.Tests;

// Sổ tay AI chuyển từ MindSprint-AI-D-n (NotebooksController). Phần gọi Gemini dùng lại GeminiClient.AskAsync đã có test riêng.
[Collection("db")]
public class SoTayAiTests
{
    private static NotebookService Service(Harness h) => new(h.Db, null!, null!, h.Library, null!, h.Clock);

    [Fact]
    public async Task NguonGhiChuVaGioiHanNhom()
    {
        using var h = new Harness();
        var u = await h.UserAsync("Minh");
        var service = Service(h);
        var nb = await service.CreateAsync("  ", u);
        Assert.Equal("Sổ tay mới", nb.Title);
        await service.AddSourceAsync(nb.Id, null, "Chương 1.Bài mở đầu", NotebookSourceKind.Text, null, u);
        await service.AddSourceAsync(nb.Id, "Rất dài", new string('a', NotebookService.MaxCharsPerSource + 10), NotebookSourceKind.File, "dai.txt", u);
        await Assert.ThrowsAsync<BusinessException>(() => service.AddSourceAsync(nb.Id, "Trống", "   ", NotebookSourceKind.Text, null, u));
        var loaded = await service.GetAsync(nb.Id, u);
        Assert.Equal(("Văn bản dán", "Chương 1. Bài mở đầu"), (loaded.Sources[0].Title, loaded.Sources[0].Content));
        Assert.Equal(NotebookService.MaxCharsPerSource, loaded.Sources[1].Content.Length); // cắt ở 200.000 ký tự như nhóm
        Assert.StartsWith("Văn bản dán\n\nChương 1.", NotebookService.Combined(loaded));
        for (var i = 2; i < NotebookService.MaxSources; i++) await service.AddSourceAsync(nb.Id, null, "x", NotebookSourceKind.Text, null, u);
        Assert.Equal($"Mỗi sổ tay có tối đa {NotebookService.MaxSources} nguồn.",
            (await Assert.ThrowsAsync<BusinessException>(() => service.AddSourceAsync(nb.Id, null, "y", NotebookSourceKind.Text, null, u))).Message);
        await service.AddNoteAsync(nb.Id, null, "Ý chính", u);
        Assert.Equal("Ghi chú", (await service.GetAsync(nb.Id, u)).Notes.Single().Title);
        Assert.Throws<BusinessException>(() => NotebookService.Combined(new Notebook()));
        // Gia sư OCR: chỉ ảnh PNG/JPG/WEBP ≤ 10 MB; sai loại bị chặn trước khi gửi gì sang Gemini.
        var gif = new Microsoft.AspNetCore.Http.FormFile(new MemoryStream([1, 2, 3]), 0, 3, "file", "de.gif");
        Assert.Equal("Ảnh phải là PNG, JPG hoặc WEBP.", (await Assert.ThrowsAsync<BusinessException>(() => service.AddImageSourceAsync(nb.Id, null, gif, u))).Message);
    }

    [Fact]
    public async Task NguoiKhacKhongXemSuaXoaDuocVaXoaSoXoaHet()
    {
        using var h = new Harness();
        var minh = await h.UserAsync("Minh"); var lan = await h.UserAsync("Lan");
        var service = Service(h);
        var nb = await service.CreateAsync("Riêng của Minh", minh);
        await service.AddSourceAsync(nb.Id, null, "Bí mật", NotebookSourceKind.Text, null, minh);
        foreach (var act in new Func<Task>[] {
            () => service.GetAsync(nb.Id, lan), () => service.AddSourceAsync(nb.Id, null, "chèn", NotebookSourceKind.Text, null, lan),
            () => service.AddNoteAsync(nb.Id, null, "chèn", lan), () => service.DeleteAsync(nb.Id, lan) })
            await Assert.ThrowsAsync<BusinessException>(act);
        Assert.Empty(await service.ListAsync(lan));
        Assert.Equal(1, Assert.Single(await service.ListAsync(minh)).Sources);
        await service.DeleteAsync(nb.Id, minh);
        Assert.Equal(0, await h.Db.NotebookSources.CountAsync(s => s.NotebookId == nb.Id));
    }
}
