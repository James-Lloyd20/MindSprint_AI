namespace MindSprintAI_ASP.NET_MVC.Models;

public record PagerModel(int Page, int TotalPages, Func<int, string> Url, string Label)
{
    public static IReadOnlyList<int?> Numbers(int page, int last)
    {
        var numbers = new[] { 1, last, page - 1, page, page + 1 }
            .Where(n => n >= 1 && n <= last).Distinct().Order().ToList();
        var result = new List<int?>();
        var previous = 0;
        foreach (var number in numbers)
        {
            if (previous > 0 && number - previous > 1) result.Add(null);
            result.Add(number);
            previous = number;
        }
        return result;
    }
}
