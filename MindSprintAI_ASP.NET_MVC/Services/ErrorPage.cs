using System.Net;

namespace MindSprintAI_ASP.NET_MVC.Services;

// (NK1, G0) Trang lỗi khi có lỗi chưa xử lý: viết thẳng ra, KHÔNG chạy lại pipeline.
// Chạy lại /Home/Error cần cơ sở dữ liệu (kiểm tra phiên, menu, số thông báo); khi chính
// cơ sở dữ liệu sập thì trang lỗi cũng sập và người dùng mất mã tra cứu.
public static class ErrorPage
{
    public static string Html(string requestId) =>
        "<!DOCTYPE html><html lang=\"vi\"><head><meta charset=\"utf-8\" />"
        + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />"
        + $"<title>Có lỗi xảy ra - {WebUtility.HtmlEncode(Brand.Name)}</title>"
        + "<link rel=\"stylesheet\" href=\"/lib/bootstrap/dist/css/bootstrap.min.css\" /></head>"
        + "<body><main class=\"container py-4\" style=\"max-width:40rem\">"
        + $"<p><a href=\"/\" class=\"fw-bold text-decoration-none\">{WebUtility.HtmlEncode(Brand.Name)}</a></p>"
        + "<h1 class=\"h3 text-danger\">Có lỗi xảy ra.</h1>"
        + "<p class=\"text-danger\">Chưa thực hiện được thao tác của bạn.</p>"
        + $"<p><strong>Mã tra cứu lỗi:</strong> <code>{WebUtility.HtmlEncode(requestId)}</code></p>"
        + "<p>Hãy thử lại. Nếu vẫn lỗi, gửi mã tra cứu lỗi ở trên cho quản trị viên.</p>"
        + "<p><a href=\"/\">Về trang chủ</a></p></main></body></html>";

    public static async Task WriteAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsync(Html(AuditLog.RequestId(context)));
    }
}
