using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MindSprintAI_ASP.NET_MVC.Data.Entities;
using MindSprintAI_ASP.NET_MVC.Services;

namespace MindSprintAI_ASP.NET_MVC.Tests;

// Dữ liệu mẫu lớp học (17/11/2026): chạy trọn kịch bản 20 ngày qua đúng các service thật; giữ cho nút ở trang Quản trị
// không hỏng âm thầm khi luật nghiệp vụ đổi về sau.
[Collection("db")]
public class DuLieuMauLopTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("demo-seed-").FullName;
    public void Dispose() { try { Directory.Delete(root, true); } catch (IOException) { } }

    private sealed class Env(string root) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = root;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "test";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Development";
    }

    private sealed class NoEmail : IEmailSender { public Task SendAsync(string to, string subject, string htmlBody) => Task.CompletedTask; }

    [Fact]
    public async Task TaoDuLieuMau_DuCacTruongHop_RoiXoaSach()
    {
        using var h = new Harness();
        var lecturer = await h.UserAsync("Giảng viên thử");
        using var users = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(h.Db), Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(), [], [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!, NullLogger<UserManager<ApplicationUser>>.Instance);
        var env = new Env(root);
        var links = new EmailLinks(new ConfigurationBuilder().Build(), new HttpContextAccessor());
        var store = new PaperFileStore(env, new ConfigurationBuilder().Build(), NullLogger<PaperFileStore>.Instance);
        var demo = new ClassDemoSeedService(h.Db, users, [new ChoiceGrader()], store, new NoEmail(), links, env, h.Clock);

        var r = await demo.SeedAsync(lecturer);
        Assert.Equal((36, 4), (r.Students, r.Classes));
        Assert.True(r.Assignments >= 12 && r.Attempts > 150, $"{r.Assignments} bài, {r.Attempts} lượt");
        Assert.True(File.Exists(Path.Combine(root, "App_Data", ClassDemoSeedService.AccountsFile)));
        h.Db.ChangeTracker.Clear();

        var rooms = await h.Db.Classrooms.Where(c => c.Name.EndsWith(ClassDemoSeedService.ClassSuffix) && c.Members.Any(m => m.UserId == lecturer)).ToListAsync();
        Assert.Equal(3, rooms.Count);
        var members = await h.Db.ClassMembers.Where(m => rooms.Select(x => x.Id).Contains(m.ClassroomId) && m.Role == ClassRole.Student).ToListAsync();
        Assert.Equal(2, members.Count(m => m.Status == ClassMemberStatus.PendingApproval));   // lớp duyệt: 2 người chờ
        Assert.Equal(1, members.Count(m => m.Status == ClassMemberStatus.Left));              // 1 người rời lớp
        Assert.Single(rooms, c => !c.LeaderboardEnabled);                                    // lớp tắt xếp hạng

        var list = await h.Db.Assignments.Where(a => rooms.Select(x => x.Id).Contains(a.ClassroomId!.Value)).ToListAsync();
        Assert.Contains(list, a => a.Mode == AssignmentMode.Test && a.ScoresPublishedAt != null);
        Assert.Contains(list, a => a.Mode == AssignmentMode.Test && a.TestStartedAt != null && a.ScoresPublishedAt == null);   // chờ công bố
        Assert.Contains(list, a => a.Mode == AssignmentMode.Test && a.TestStartedAt == null);   // đã lên lịch, chưa mở
        Assert.Contains(list, a => a.Form == AssignmentForm.FlashcardOnly);
        Assert.Contains(list, a => a.Form == AssignmentForm.Combined);
        var paper = Assert.Single(list, a => a.Form == AssignmentForm.Document);
        Assert.True(await h.Db.RetakeRequests.AnyAsync(x => list.Select(a => a.Id).Contains(x.Participant.AssignmentId) && x.Status == RetakeRequestStatus.Used));
        Assert.True(await h.Db.Participants.AnyAsync(p => list.Select(a => a.Id).Contains(p.AssignmentId) && p.ExtraMinutes > 0));
        Assert.True(await h.Db.ScreenLeaves.AnyAsync(x => list.Select(a => a.Id).Contains(x.Attempt.Participant.AssignmentId)));
        var papers = await h.Db.PaperSubmissions.Where(s => s.Attempt.Participant.AssignmentId == paper.Id).ToListAsync();
        Assert.Contains(papers, s => s.Score != null);
        Assert.Contains(papers, s => s.Score == null);   // còn bài chưa chấm
        // Giảng viên mẫu: gv01 là giảng viên phụ lớp Lập trình Web (đã nhận lời, chấm một phần bài theo đề); gv02 có lớp riêng, đang đề nghị chuyển lớp.
        var gv01 = (await users.FindByEmailAsync("gv01@" + ClassDemoSeedService.Domain))!.Id;
        var gv02 = (await users.FindByEmailAsync("gv02@" + ClassDemoSeedService.Domain))!.Id;
        Assert.True(await h.Db.ClassMembers.AnyAsync(m => m.UserId == gv01 && m.Role == ClassRole.AssistantLecturer && m.Status == ClassMemberStatus.Active));
        Assert.Contains(papers, s => s.GradedById == gv01);
        var c4 = await h.Db.ClassMembers.Where(m => m.UserId == gv02 && m.Role == ClassRole.MainLecturer).Select(m => m.ClassroomId).SingleAsync();
        Assert.True(await h.Db.ClassMembers.CountAsync(m => m.ClassroomId == c4 && m.Role == ClassRole.Student) == 8);
        Assert.True(await h.Db.Invitations.AnyAsync(i => i.ClassroomId == c4 && i.Kind == InvitationKind.Transfer && i.Status == InvitationStatus.Pending));
        // Trợ lý AI có trích dẫn đúng văn bản nguồn; sổ tay AI; thông báo cho học viên (có đọc, có chưa đọc).
        var demoIds = await h.Db.Users.Where(u => u.Email!.EndsWith("@" + ClassDemoSeedService.Domain)).Select(u => u.Id).ToListAsync();
        var chats = await h.Db.AiConversations.Include(c => c.Messages).ThenInclude(m => m.Citations).Where(c => demoIds.Contains(c.UserId)).ToListAsync();
        Assert.True(chats.Count >= 15, chats.Count + " hội thoại");
        Assert.Contains(chats, c => c.QuestionId != null);
        var sources = await h.Db.SourceTexts.Where(s => chats.Select(c => c.StudySetId).Contains(s.StudySetId)).ToDictionaryAsync(s => s.StudySetId, s => s.Content);
        Assert.All(chats.SelectMany(c => c.Messages.Where(m => m.Role == "assistant").SelectMany(m => m.Citations).Select(x => (c.StudySetId, x))),
            p => Assert.Equal(p.x.ExcerptText, sources[p.StudySetId][p.x.StartOffset..p.x.EndOffset]));
        Assert.True(await h.Db.Notebooks.CountAsync(n => demoIds.Contains(n.UserId) && n.Sources.Any() && n.Notes.Any()) >= 8);
        var notices = await h.Db.Notifications.Where(n => demoIds.Contains(n.UserId)).ToListAsync();
        Assert.Contains(notices, n => n.Type == NotificationType.Reminder && n.ReadAt != null);
        Assert.Contains(notices, n => n.Type == NotificationType.Reminder && n.ReadAt == null);
        Assert.Contains(notices, n => n.Type == NotificationType.Retake);
        Assert.Contains(notices, n => n.Type == NotificationType.ExtraMinutes);
        // Mọi thứ diễn ra trong quá khứ, không có gì "ở tương lai".
        Assert.True(await h.Db.Attempts.Where(x => list.Select(a => a.Id).Contains(x.Participant.AssignmentId)).AllAsync(x => x.StartedAt <= h.Clock.UtcNow));

        // Xếp hạng lớp 1 có đủ học viên; học viên mẫu đăng nhập được bằng mật khẩu chung.
        var ranking = await new RankingService(h.Db, h.Classrooms).ForClassAsync(rooms.Single(c => c.Name.StartsWith("Lập trình Web")).Id, lecturer);
        Assert.True(ranking.Rows.Count >= 25);
        var first = await users.FindByEmailAsync("hv01@" + ClassDemoSeedService.Domain);
        Assert.True(await users.CheckPasswordAsync(first!, r.Password));

        await Assert.ThrowsAsync<BusinessException>(() => demo.SeedAsync(lecturer));   // không tạo trùng

        (await h.Db.Users.SingleAsync(u => u.Id == lecturer)).IsSystemAdmin = true;   // nút xóa ở trang Quản trị: người bấm luôn là quản trị viên
        await h.Db.SaveChangesAsync();
        var deletion = new AccountDeletionService(h.Db, users, new NoEmail(), SecurityRound1Fixtures.Emails(h, users, new NoEmail(), links), h.Clock);
        var removed = await demo.RemoveAsync(lecturer, deletion, h.Classrooms, h.Assignments, h.Library);
        Assert.Equal(38, removed);   // 36 học viên + 2 giảng viên mẫu
        Assert.False(await h.Db.Classrooms.AnyAsync(c => c.Id == c4 && c.DeletedAt == null));
        h.Db.ChangeTracker.Clear();
        Assert.False(await demo.ExistsAsync(lecturer));
        Assert.False(await h.Db.Users.AnyAsync(u => u.Email != null && u.Email.EndsWith("@" + ClassDemoSeedService.Domain)));
        Assert.False(File.Exists(Path.Combine(root, "App_Data", ClassDemoSeedService.AccountsFile)));
    }
}
