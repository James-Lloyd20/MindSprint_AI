using MindSprintAI_ASP.NET_MVC.Services;

namespace MindSprintAI_ASP.NET_MVC.Tests;

// Tách từ Tang5bTests.cs của QuizEduManager để test chỉ-service dùng được (TestHarness). Khi mang Tang5bTests sang thì xóa bản trùng trong đó.
internal sealed class SafetyEmail : IEmailSender
{
    public List<string> Messages { get; } = [];
    public List<string> Subjects { get; } = [];
    public Task SendAsync(string to, string subject, string htmlBody) { Messages.Add(htmlBody); Subjects.Add(subject); return Task.CompletedTask; }
}
