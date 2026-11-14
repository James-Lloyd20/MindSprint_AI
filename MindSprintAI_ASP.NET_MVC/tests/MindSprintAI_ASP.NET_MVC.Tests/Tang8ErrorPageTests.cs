using Microsoft.AspNetCore.Http;
using MindSprintAI_ASP.NET_MVC.Services;
using Xunit;

namespace MindSprintAI_ASP.NET_MVC.Tests;

// (NK1) Trang lỗi phải hiện được cả khi cơ sở dữ liệu sập: không dùng DI, DB hay layout.
public class Tang8ErrorPageTests
{
    [Fact]
    public async Task TrangLoiVietTrucTiepCoMaTraCuuVaKhongCanDichVu()
    {
        var context = new DefaultHttpContext(); // không có RequestServices: chạm DB/DI là ném lỗi
        context.TraceIdentifier = "ma-<thu>";
        context.Response.Body = new MemoryStream();
        await ErrorPage.WriteAsync(context);
        context.Response.Body.Position = 0;
        var html = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Contains("Mã tra cứu lỗi", html);
        Assert.Contains("ma-&lt;thu&gt;", html); // mã được mã hóa HTML
        Assert.Contains("gửi mã tra cứu lỗi ở trên cho quản trị viên", html);
        Assert.False(PlainLanguageAudit.HasJargon(html));
    }
}
