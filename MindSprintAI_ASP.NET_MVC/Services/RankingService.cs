using Microsoft.EntityFrameworkCore;
using MindSprintAI_ASP.NET_MVC.Data;
using MindSprintAI_ASP.NET_MVC.Data.Entities;

namespace MindSprintAI_ASP.NET_MVC.Services;

public record RankRow(int Rank, string Key, string Name, decimal Percent, TimeSpan? Time, bool Me, int? Done = null, int? Total = null);

// Rows: GV thấy đủ; học viên thấy top 10 (Mine = hạng của chính mình nếu ngoài top 10). Hidden: lý do học viên chưa xem được.
public record Leaderboard(List<RankRow> Rows, RankRow? Mine, int Count, string? Hidden);

// Xếp hạng (người dùng chốt 17/11/2026, kiểu Quizizz):
// - Từng bài: điểm (đúng điểm bảng điểm đang tính) cao trước, bằng điểm thì làm nhanh hơn trước. Ôn tập: học viên thấy ngay;
//   Kiểm tra / bài theo đề: chỉ sau khi GV công bố điểm. Bài chỉ có thẻ ghi nhớ không có điểm nên không xếp.
// - Cả lớp: trung bình % các bài đã giao cho học viên đó và đã hiện điểm; bài chưa làm tính 0.
// - Học viên thấy top 10 + hạng của mình; GV luôn thấy cả bảng và tắt được bảng của học viên theo từng lớp.
public sealed class RankingService(AppDbContext db, ClassroomService classrooms)
{
    public const int TopForLearners = 10;

    public static bool Ranked(Assignment a) => a.Form != AssignmentForm.FlashcardOnly;
    public static bool VisibleToLearners(Assignment a) => a.Mode == AssignmentMode.Practice || a.ScoresPublishedAt != null;

    public async Task<Leaderboard> ForAssignmentAsync(int assignmentId, string userId)
    {
        var a = await db.Assignments.AsNoTracking().Include(x => x.Classroom).SingleOrDefaultAsync(x => x.Id == assignmentId)
            ?? throw new BusinessException("Không tìm thấy bài.");
        var lecturer = a.ClassroomId != null && await classrooms.IsLecturerAsync(a.ClassroomId.Value, userId);
        var people = await db.Participants.AsNoTracking().Include(p => p.Attempts).Include(p => p.User)
            .Where(p => p.AssignmentId == assignmentId).ToListAsync();
        if (!lecturer && !people.Any(p => p.UserId == userId)
            && !(a.ClassroomId != null && await classrooms.GetActiveMembershipAsync(a.ClassroomId.Value, userId) != null))
            throw new ForbiddenException();
        if (!lecturer && Hidden(a) is { } reason) return new([], null, 0, reason);
        var rows = Order(people.Select(p => Score(p, a) is { } s
            ? (Key: p.UserId ?? "g" + p.Id, Name: p.User?.DisplayName ?? p.GuestDisplayName ?? "Khách", s.Percent, s.Time, Me: p.UserId == userId)
            : default).Where(r => r.Key != null).ToList());
        return Cut(rows, lecturer);
    }

    public async Task<Leaderboard> ForClassAsync(int classroomId, string userId)
    {
        var room = await db.Classrooms.AsNoTracking().SingleOrDefaultAsync(c => c.Id == classroomId && c.DeletedAt == null)
            ?? throw new BusinessException("Không tìm thấy lớp.");
        var lecturer = await classrooms.IsLecturerAsync(classroomId, userId);
        var members = await db.ClassMembers.AsNoTracking().Include(m => m.User)
            .Where(m => m.ClassroomId == classroomId && m.Role == ClassRole.Student && m.Status == ClassMemberStatus.Active).ToListAsync();
        if (!lecturer && !members.Any(m => m.UserId == userId)) throw new ForbiddenException();
        if (!lecturer && !room.LeaderboardEnabled) return new([], null, 0, "Giảng viên đã tắt bảng xếp hạng của lớp này.");
        var list = await db.Assignments.AsNoTracking().Where(a => a.ClassroomId == classroomId && a.Status != AssignmentStatus.Draft).ToListAsync();
        list = list.Where(a => Ranked(a) && VisibleToLearners(a)).ToList();
        var ids = list.Select(a => a.Id).ToList();
        var people = await db.Participants.AsNoTracking().Include(p => p.Attempts)
            .Where(p => ids.Contains(p.AssignmentId) && p.UserId != null).ToListAsync();
        var rows = new List<(string Key, string Name, decimal Percent, TimeSpan? Time, bool Me, int Done, int Total)>();
        foreach (var m in members)
        {
            var mine = list.Where(a => AssignmentService.IsAssignedTo(a, m)).ToList();
            if (mine.Count == 0) continue;
            var scores = mine.Select(a => people.FirstOrDefault(p => p.AssignmentId == a.Id && p.UserId == m.UserId) is { } p ? Score(p, a) : null).ToList();
            rows.Add((m.UserId, m.User.DisplayName, Math.Round(scores.Sum(s => s?.Percent ?? 0) / mine.Count, 1), null, m.UserId == userId,
                scores.Count(s => s != null), mine.Count));
        }
        var ordered = rows.OrderByDescending(r => r.Percent).ThenBy(r => r.Name, StringComparer.CurrentCulture).ToList();
        var ranked = ordered.Select((r, i) => new RankRow(RankOf(ordered.Select(x => (x.Percent, (TimeSpan?)null)).ToList(), i),
            r.Key, r.Name, r.Percent, null, r.Me, r.Done, r.Total)).ToList();
        return Cut(ranked, lecturer);
    }

    public async Task SetEnabledAsync(int classroomId, bool enabled, string userId)
    {
        await classrooms.EnsureLecturerAsync(classroomId, userId);
        var room = await db.Classrooms.SingleAsync(c => c.Id == classroomId);
        room.LeaderboardEnabled = enabled;
        await db.SaveChangesAsync();
    }

    public static string? Hidden(Assignment a) =>
        !Ranked(a) ? "Bài chỉ có thẻ ghi nhớ nên không xếp hạng."
        : a.Classroom is { LeaderboardEnabled: false } ? "Giảng viên đã tắt bảng xếp hạng của lớp này."
        : !VisibleToLearners(a) ? "Bảng xếp hạng hiện sau khi giảng viên công bố điểm." : null;

    // % điểm theo đúng lựa chọn của bảng điểm; thời gian = trung bình thời gian làm của các lượt được tính.
    internal static (decimal Percent, TimeSpan? Time)? Score(Participant p, Assignment a)
    {
        var s = GradebookService.SelectForAssignment(p.Attempts, a);
        if (s == null || s.Max <= 0) return null;
        var counted = a.Mode == AssignmentMode.Practice
            ? GradebookService.ScoredAttempts(p.Attempts, a.ScoreSelection)
            : p.Attempts.Where(x => x.Status == AttemptStatus.Ended && !x.ExcludeFromGradebook).OrderByDescending(x => x.AttemptNumber).Take(1).ToList();
        var times = counted.Where(x => x.EndedAt != null).Select(x => x.EndedAt!.Value - x.StartedAt).ToList();
        return (Math.Round(s.Raw * 100 / s.Max, 1), times.Count == 0 ? null : TimeSpan.FromTicks((long)times.Average(t => t.Ticks)));
    }

    private static List<RankRow> Order(List<(string Key, string Name, decimal Percent, TimeSpan? Time, bool Me)> rows)
    {
        var ordered = rows.OrderByDescending(r => r.Percent).ThenBy(r => r.Time ?? TimeSpan.MaxValue).ThenBy(r => r.Name, StringComparer.CurrentCulture).ToList();
        var keys = ordered.Select(r => (r.Percent, r.Time)).ToList();
        return ordered.Select((r, i) => new RankRow(RankOf(keys, i), r.Key, r.Name, r.Percent, r.Time, r.Me)).ToList();
    }

    // Đồng hạng kiểu 1, 2, 2, 4: cùng điểm và cùng thời gian thì cùng hạng.
    private static int RankOf(List<(decimal Percent, TimeSpan? Time)> keys, int i)
    {
        while (i > 0 && keys[i - 1] == keys[i]) i--;
        return i + 1;
    }

    public static Leaderboard Cut(List<RankRow> rows, bool lecturer)
    {
        if (lecturer) return new(rows, null, rows.Count, null);
        var top = rows.Take(TopForLearners).ToList();
        var mine = rows.FirstOrDefault(r => r.Me);
        return new(top, mine != null && !top.Contains(mine) ? mine : null, rows.Count, null);
    }
}
