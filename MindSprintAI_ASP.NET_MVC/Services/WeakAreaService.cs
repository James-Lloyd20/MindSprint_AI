using Microsoft.EntityFrameworkCore;
using MindSprintAI_ASP.NET_MVC.Data;
using MindSprintAI_ASP.NET_MVC.Data.Entities;

namespace MindSprintAI_ASP.NET_MVC.Services;

public enum WeakKind { Cards, Quiz }
public record WeakArea(WeakKind Kind, string Topic, int Wrong, int Total, int? StudySetId, int? AssignmentId)
{
    public int Accuracy => Total == 0 ? 0 : (Total - Wrong) * 100 / Total;
    public string Reason => $"{Wrong}/{Total} lần sai · {Accuracy}% chính xác";
}
public record WeakAreaPage(IReadOnlyList<WeakArea> Areas, int CheckedTopics);

// Điểm yếu (MindSprint-AI-D-n WeakAreaService): giữ quy tắc nhóm — một chủ đề là điểm yếu khi có ≥ 3 lần thử và chính xác < 70%;
// yếu nhất (chính xác thấp, sai nhiều) lên trước. Khác nhóm: tính ngay từ dữ liệu thật mỗi lần mở trang (không cần bảng + nút
// "Phân tích lại"), chủ đề = bộ thẻ đã ôn (FlashcardReview) và bài quiz đã làm (câu trả lời lần đầu, bỏ câu hết giờ "{}").
public sealed class WeakAreaService(AppDbContext db)
{
    public const int MinAttempts = 3;
    public const int WeakBelow = 70;

    public async Task<WeakAreaPage> GetAsync(string userId)
    {
        var cards = await db.FlashcardReviews.Where(r => r.UserId == userId)
            .GroupBy(r => new { r.StudySetId, r.StudySet.Title, AssignmentId = r.Participant == null ? (int?)null : r.Participant.AssignmentId })
            .Select(g => new WeakArea(WeakKind.Cards, g.Key.Title, g.Count(r => r.Rating == FlashcardRating.NotRemembered), g.Count(), g.Key.StudySetId, g.Key.AssignmentId))
            .ToListAsync();
        var quiz = await db.AnswerSubmissions.Where(a => a.IsFirstAnswer && a.PayloadJson != "{}" && a.IsCorrect != null
                && a.AttemptQuestion.Attempt.Participant.UserId == userId)
            .GroupBy(a => new { a.AttemptQuestion.Attempt.Participant.AssignmentId, Title = a.AttemptQuestion.Attempt.Participant.Assignment.Quiz!.Title })
            .Select(g => new WeakArea(WeakKind.Quiz, g.Key.Title, g.Count(a => a.IsCorrect == false), g.Count(), null, g.Key.AssignmentId))
            .ToListAsync();
        var all = cards.Concat(quiz).ToList();
        return new(Pick(all), all.Count);
    }

    public static List<WeakArea> Pick(IEnumerable<WeakArea> topics) => topics
        .Where(t => t.Total >= MinAttempts && t.Accuracy < WeakBelow)
        .OrderBy(t => t.Accuracy).ThenByDescending(t => t.Wrong).ToList();
}
