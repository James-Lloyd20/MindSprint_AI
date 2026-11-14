using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MindSprintAI_ASP.NET_MVC.Controllers;
using MindSprintAI_ASP.NET_MVC.Data.Entities;
using MindSprintAI_ASP.NET_MVC.Services;

namespace MindSprintAI_ASP.NET_MVC.Tests;

[Collection("db")]
public class RekalDashboardTests
{
    [Fact]
    public async Task SoDangLamKhongGomNguoiTamDungMatKetNoiHoacHetHan()
    {
        using var h = new Harness(); var s = await h.ScenarioAsync(1);
        foreach (var state in new[] { "working", "paused", "disconnected", "expired", "ended" })
        {
            var p = new Participant { Assignment = s.Assignment, UserId = await h.UserAsync(state) };
            h.Db.Attempts.Add(new() { Participant = p, AttemptNumber = 1,
                Status = state == "paused" ? AttemptStatus.Paused : state == "ended" ? AttemptStatus.Ended : AttemptStatus.InProgress,
                DisconnectedAt = state == "disconnected" ? h.Clock.UtcNow.AddSeconds(-31) : null,
                DeadlineAt = state == "expired" ? h.Clock.UtcNow : h.Clock.UtcNow.AddHours(1) });
        }
        await h.Db.SaveChangesAsync();
        var item = Assert.Single((await h.Assignments.GetLecturerDashboardAsync(s.Lecturer, "Lan")).Open);
        Assert.Equal(1, item.Working); Assert.Equal(1, item.Submitted);
    }

    [Fact]
    public async Task TrangChuGiangVienVaHocVienGiuDungDich()
    {
        using var h = new Harness(); var s = await h.ScenarioAsync(1);
        using var users = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(h.Db), Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(), [], [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!, NullLogger<UserManager<ApplicationUser>>.Instance);
        var user = await users.FindByIdAsync(s.Lecturer); user!.CurrentMode = UserMode.Lecturer; await h.Db.SaveChangesAsync();
        var controller = new HomeController(users) { ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, s.Lecturer)], "test")) } } };
        var result = Assert.IsType<ViewResult>(await controller.Index(h.Assignments));
        Assert.Equal("Lecturer", result.ViewName);
        Assert.IsType<LecturerDashboard>(result.Model);
        user.CurrentMode = UserMode.Learner; await h.Db.SaveChangesAsync();
        Assert.Equal("Learn", Assert.IsType<RedirectToActionResult>(await controller.Index(h.Assignments)).ControllerName);
    }

    [Fact]
    public async Task ChiBaiCuaMinhDangMoVaToiDaNamBai()
    {
        using var h = new Harness(); var own = await h.ScenarioAsync(1); var other = await h.ScenarioAsync(1);
        for (var i = 0; i < 7; i++)
            await h.Assignments.CreatePracticeAsync(own.Room.Id, own.Quiz.Id, new(null, null, 3), own.Lecturer);
        var view = await h.Assignments.GetLecturerDashboardAsync(own.Lecturer, "Lan");
        Assert.Equal(5, view.Open.Count);
        Assert.DoesNotContain(view.Open, x => x.Id == other.Assignment.Id);
        var outsider = await h.UserAsync("Khác");
        Assert.Empty((await h.Assignments.GetLecturerDashboardAsync(outsider, "Khác")).Open);
        Assert.Empty((await h.Assignments.GetLecturerDashboardAsync(own.Student, "Minh")).Open);
    }
}

public class RekalDashboardPureTests
{
    [Theory]
    [InlineData(22, "Chào buổi sáng")]
    [InlineData(5, "Chào buổi chiều")]
    [InlineData(11, "Chào buổi tối")]
    [InlineData(21, "Chào buổi tối")]
    public void LoiChaoTheoGioVietNam(int utcHour, string expected) =>
        Assert.Equal(expected, AssignmentService.LecturerGreeting(new DateTime(2026, 11, 9, utcHour, 0, 0, DateTimeKind.Utc)));
}
