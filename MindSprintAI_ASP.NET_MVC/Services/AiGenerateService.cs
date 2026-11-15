using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace MindSprintAI_ASP.NET_MVC.Services;

// "Tạo bằng AI" của nhóm (AiController extract-file/flashcards/quiz) gắn vào luồng nhập Excel có sẵn của QuizEdu:
// AI chỉ tạo bản nháp → trang xem trước (sửa, bỏ dòng, kiểm lại) → lưu bằng ExcelImportService.SaveAsync như nhập file.
// Dùng API key Gemini của chính người dùng (UserApiKeyService, cần đã đồng ý thông báo AI) như trợ lý AI của QuizEdu.
public sealed class AiGenerateService(ExcelImportService excel, UserApiKeyService keys, GeminiClient gemini)
{
    public const int MaxChars = 60_000, MaxCount = 30;
    public const long MaxFileBytes = 10 * 1024 * 1024;
    public static readonly string[] FileTypes = [".pdf", ".txt", ".md", ".csv", ".json"];

    public async Task<ImportPreview> GenerateAsync(int quizId, string? text, int count, bool cards, string userId)
    {
        await excel.EnsureCanImportAsync(quizId, userId);
        var source = Clean(text);
        if (source.Length < 50) throw new BusinessException("Hãy dán hoặc tải lên tài liệu dài ít nhất 50 ký tự.");
        if (source.Length > MaxChars) source = source[..MaxChars];
        count = Math.Clamp(count, 1, MaxCount);
        var items = await gemini.GenerateAsync(await keys.UseAsync(userId), source, count, cards);
        return ExcelImportService.ToPreview(quizId, cards ? "Thẻ ghi nhớ tạo bằng AI" : "Câu hỏi tạo bằng AI", ExcelTemplate.ValidateRows(ToRows(items, cards, count)));
    }

    // Mỗi mục AI → một dòng đúng cột file mẫu (sheet CauHoi / Flashcard); dữ liệu sai do AI sẽ hiện lỗi ở dòng đó trong trang xem trước.
    public static List<ImportEditRow> ToRows(JsonElement items, bool cards, int max)
    {
        var rows = new List<ImportEditRow>();
        foreach (var item in items.EnumerateArray().Take(max))
        {
            string Str(string name) => item.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()!.Trim() : "";
            if (cards) { rows.Add(new() { Sheet = "Flashcard", Row = ExcelTemplate.FirstDataRow + rows.Count, Values = [Str("front"), Str("back")] }); continue; }
            var options = item.TryGetProperty("options", out var o) && o.ValueKind == JsonValueKind.Array
                ? o.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!.Trim()).Take(ExcelTemplate.OptionCount).ToList() : [];
            var correct = item.TryGetProperty("correct", out var c) && c.ValueKind == JsonValueKind.Array
                ? c.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out _)).Select(x => x.GetInt32()).Distinct().ToList() : [];
            var values = Enumerable.Repeat("", ExcelTemplate.Headers.Length).ToList();
            values[ExcelTemplate.Prompt] = Str("question");
            values[ExcelTemplate.Type] = correct.Count > 1 ? "Chọn nhiều" : "Chọn một";
            for (var i = 0; i < options.Count; i++) values[ExcelTemplate.FirstOption + i] = options[i];
            values[ExcelTemplate.Correct] = string.Join(",", correct);
            values[ExcelTemplate.Explain] = Str("explanation");
            rows.Add(new() { Sheet = "CauHoi", Row = ExcelTemplate.FirstDataRow + rows.Count, Values = values });
        }
        if (rows.Count == 0) throw new BusinessException("AI chưa tạo được nội dung nào từ tài liệu này. Hãy thử đoạn văn rõ ràng hơn.");
        return rows;
    }

    // Đọc chữ từ tệp như nhóm: PDF (PdfPig) hoặc tệp chữ; PDF scan không có lớp chữ thì báo dùng Gia sư OCR/dán chữ.
    public static async Task<string> ExtractAsync(IFormFile file)
    {
        if (file.Length == 0) throw new BusinessException("Tệp trống.");
        if (file.Length > MaxFileBytes) throw new BusinessException("Tệp tối đa 10 MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!FileTypes.Contains(ext)) throw new BusinessException("Chỉ nhận tệp PDF hoặc tệp chữ (TXT, MD, CSV).");
        string text;
        try
        {
            if (ext == ".pdf")
            {
                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer);
                using var pdf = PdfDocument.Open(buffer.ToArray());
                text = string.Join("\n\n", pdf.GetPages().Select(p => p.Text));
            }
            else
            {
                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
                text = await reader.ReadToEndAsync();
            }
        }
        catch (Exception ex) when (ex is not BusinessException) { throw new BusinessException("Không đọc được tệp này."); }
        if (string.IsNullOrWhiteSpace(text)) throw new BusinessException("Không lấy được chữ từ tệp (có thể là bản scan). Hãy dán chữ vào ô bên dưới.");
        return text;
    }

    // Dọn chữ trích từ PDF như nhóm (CleanExtractedText): gộp khoảng trắng, thêm cách sau dấu chấm dính chữ hoa.
    public static string Clean(string? text) => string.IsNullOrWhiteSpace(text) ? ""
        : Regex.Replace(Regex.Replace(text, @"([a-zÀ-ỹ0-9])\.([A-ZÀ-Ỹ])", "$1. $2"), @"[ \t]+", " ").Trim();
}
