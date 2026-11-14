using System.Text.RegularExpressions;

namespace MindSprintAI_ASP.NET_MVC.Tests;

// File-only checks: these never instantiate a database fixture or start the application.
public class GiaoDienTests
{
    private static string Root => Tang8PureTests.Root;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));
    private static IEnumerable<string> Files(string dir) => Directory.EnumerateFiles(Path.Combine(Root, dir), "*", SearchOption.AllDirectories);

    [Theory]
    [InlineData("Views/Shared/_Layout.cshtml")]
    [InlineData("Views/Shared/JoinQr.cshtml")]
    public void KhoiTaoTruocCssVaDocLuaChonAnToan(string path)
    {
        var source = Read(path);
        Assert.True(source.IndexOf("mindsprint-theme", StringComparison.Ordinal) < source.IndexOf("<link", StringComparison.Ordinal));
        Assert.True(source.IndexOf("bootstrap.min.css", StringComparison.Ordinal) < source.IndexOf("mindsprint.css", StringComparison.Ordinal));
        Assert.Matches(@"try\s*\{[^}]*localStorage\.getItem\('mindsprint-theme'\)[^}]*\}\s*catch", source);
        Assert.Contains("prefers-color-scheme: dark", source);
        Assert.Contains("setAttribute('data-bs-theme', theme)", source);
        Assert.Contains("~/js/theme.js", source);
    }

    [Theory]
    [InlineData("Views/Shared/_Layout.cshtml")]
    [InlineData("Views/Shared/JoinQr.cshtml")]
    [InlineData("Views/Account/Index.cshtml")]
    public void NutDoiGiaoDienCoNhanVaTrangThai(string path)
    {
        // Header has a short visual label on narrow phones; its accessible name stays fixed.
        Assert.Matches("<button[^>]*type=\"button\"[^>]*data-theme-toggle[^>]*aria-pressed=\"false\"", Read(path));
        Assert.Contains("Giao diện tối", Read(path));
        if (path == "Views/Shared/_Layout.cshtml") Assert.Contains("aria-label=\"Giao diện tối\"", Read(path));
        var layout = Read("Views/Shared/_Layout.cshtml");
        Assert.True(layout.IndexOf("id=\"theme-toggle\"", StringComparison.Ordinal) < layout.IndexOf("<div class=\"navbar-collapse", StringComparison.Ordinal));
    }

    [Fact]
    public void MoiLanDungBoNhoLuaChonDeuCoTryCatch()
    {
        foreach (var path in new[] { "Views/Shared/_Layout.cshtml", "Views/Shared/JoinQr.cshtml", "wwwroot/js/theme.js" })
            foreach (var line in Read(path).Split('\n').Where(l => l.Contains("localStorage.")))
                Assert.Matches(@"try\s*\{.*localStorage\.(getItem|setItem).*\}\s*catch", line);
        Assert.DoesNotContain("textContent", Read("wwwroot/js/theme.js")); // nhãn cố định, chỉ aria-pressed đổi
        Assert.Contains("setAttribute('aria-pressed'", Read("wwwroot/js/theme.js"));
    }

    [Fact]
    public void FontNamTrongDuAnCoGiayPhepVaTapTiengViet()
    {
        var faces = Files("wwwroot/css").SelectMany(path => Regex.Matches(File.ReadAllText(path), @"@font-face\s*\{[^}]+\}")
            .Cast<Match>().Select(m => (Path: path, Css: m.Value))).ToList();
        Assert.NotEmpty(faces);
        foreach (var face in faces)
        {
            Assert.Contains("font-display: swap", face.Css);
            Assert.Contains("unicode-range:", face.Css);
            var urls = Regex.Matches(face.Css, @"url\(['"" ]*([^)'"" ]+)");
            Assert.NotEmpty(urls.Cast<Match>());
            foreach (Match match in urls)
            {
                var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(face.Path)!, match.Groups[1].Value));
                Assert.StartsWith(Path.Combine(Root, "wwwroot", "lib", "fonts") + Path.DirectorySeparatorChar, path);
                Assert.True(File.Exists(path), path);
                var bytes = File.ReadAllBytes(path);
                Assert.True(bytes.Length > 100);
                Assert.Equal("wOF2", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
                Assert.Contains("SIL OPEN FONT LICENSE", File.ReadAllText(Path.Combine(Path.GetDirectoryName(path)!, "OFL.txt")));
            }
        }
        foreach (var family in new[] { "space-grotesk", "jetbrains-mono" })
        {
            Assert.Contains("vietnamese", Read($"wwwroot/lib/fonts/{family}/metadata.json"));
            Assert.Contains(family + "-vietnamese-", Read("wwwroot/css/mindsprint-fonts.css"));
        }
        Assert.Contains("5.3.0", Read("wwwroot/lib/fonts/README.md"));
        Assert.Contains("space-grotesk@5.3.0", Read("wwwroot/lib/fonts/README.md"));
        foreach (var weight in new[] { 500, 700 })
            foreach (var subset in new[] { "latin", "vietnamese" })
                Assert.True(File.Exists(Path.Combine(Root, $"wwwroot/lib/fonts/space-grotesk/space-grotesk-{subset}-{weight}-normal.woff2")));
        Assert.False(Directory.Exists(Path.Combine(Root, "wwwroot/lib/fonts/bricolage-grotesque")));
        Assert.False(Directory.Exists(Path.Combine(Root, "wwwroot/lib/fonts/dela-gothic-one")));
    }

    [Fact]
    public void KhongTaiGoogleFontsKhiChay()
    {
        foreach (var path in new[] { "Views", "wwwroot/css", "wwwroot/js" }.SelectMany(Files))
        {
            var source = File.ReadAllText(path);
            Assert.DoesNotContain("fonts.googleapis.com", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("fonts.gstatic.com", source, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TokenVaChuyenDongTheoBanDuyet()
    {
        var css = Read("wwwroot/css/mindsprint.css");
        foreach (var token in new[] { "#D2E823", "#F3F7DC", "#E6EDC4", "#09090B", "#131317", "#E6E1D6", "#AEBD3A", "#9A958A", "#4A4740" }) Assert.Contains(token, css);
        Assert.Contains("[data-bs-theme=\"dark\"]", css);
        Assert.Contains("prefers-reduced-motion: reduce", css);
        Assert.Contains("outline: 3px solid var(--focus)", css);
        Assert.Contains(".mindsprint-still *", css);
        Assert.Contains("animation: none !important", css);
        Assert.Contains(".home-page .home-hero", css);
        Assert.Contains("home-marquee", css); // vòng lặp mới chỉ ở dải môn học có nút dừng
        Assert.DoesNotContain("transition: all", css);
    }

    [Fact]
    public void ViewVaJsKhongConLopMauCoDinh()
    {
        foreach (var path in Files("Views").Where(p => p.EndsWith(".cshtml")).Concat(Files("wwwroot/js")))
            Assert.DoesNotMatch(@"\b(bg-white|bg-light|text-dark|text-black|navbar-light|table-light)\b", File.ReadAllText(path));
    }

    [Fact]
    public void KhongCoMauVietCungTrongStyleCuaView()
    {
        // No allow-list: remaining inline styles only describe layout, wrapping and touch behavior.
        foreach (var path in Files("Views").Where(p => p.EndsWith(".cshtml")))
            foreach (Match style in Regex.Matches(File.ReadAllText(path), "style=\"([^\"]*)\"", RegexOptions.IgnoreCase))
                Assert.DoesNotMatch(@"#|rgba?\s*\(|hsla?\s*\(|\b(color|background(?:-color)?|border(?:-color)?)\s*:\s*(?:[a-z]+)\b", style.Groups[1].Value);
    }

    [Fact]
    public void ChuMoiVanTheoG0()
    {
        Assert.Empty(PlainLanguageAudit.Violations(Root));
    }

    [Fact]
    public void TrangThaiDapAnCoDauVaNhanNgoaiMauSac()
    {
        var css = Read("wwwroot/css/mindsprint.css");
        Assert.Contains(".option.btn-success::before { content: '✓ '", css);
        Assert.Contains(".option.btn-danger::before { content: '✗ '", css);
        Assert.Contains(".nav-q.flagged::after", css);
        Assert.Contains("border-style: dashed", css);
        var script = Read("wwwroot/js/play.js");
        Assert.Contains("— Đáp án đúng", script);
        Assert.Contains("— Bạn chọn, chưa đúng", script);
        Assert.Contains("— Đã chọn", script);
        Assert.Contains("đã đánh dấu", script);
    }

    [Fact]
    public void KhongConFontCuTrongTrangVaTaiNguyen()
    {
        // MindSprint không mang docs/design/preview.html (trang xem trước thiết kế Rekal).
        foreach (var path in new[] { "Views", "wwwroot/css", "wwwroot/js" }.SelectMany(Files))
        {
            var source = File.ReadAllText(path);
            Assert.DoesNotContain("Bricolage", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotMatch(@"(?i)dela[ +_-]+gothic", source);
        }
        Assert.Contains("Space Grotesk", Read("wwwroot/css/mindsprint-fonts.css"));
    }

    [Fact]
    public void TrangChuCoMotTieuDeVaNamPhanCungMoTaChiaSe()
    {
        var home = Read("Views/Home/Index.cshtml");
        Assert.Single(Regex.Matches(home, @"<h1\b", RegexOptions.IgnoreCase).Cast<Match>());
        Assert.Equal(5, Regex.Matches(home, @"<section\b").Count);
        Assert.Contains("ViewData[\"Description\"]", home);
        Assert.Contains("ViewData[\"OgImage\"]", home);
        foreach (var text in new[] { "Ôn tập và kiểm tra bằng", "Bài ôn của học viên", "Bài giao của giảng viên", "Ba bước vào bài học", "Bắt đầu buổi ôn tập" }) Assert.Contains(text, home);
        var layout = Read("Views/Shared/_Layout.cshtml");
        Assert.Contains("ViewData[\"Description\"] is string description", layout);
        Assert.Contains("<meta name=\"description\"", layout);
        foreach (var property in new[] { "og:title", "og:description", "og:image" }) Assert.Contains(property, layout);
        Assert.Contains("RenderSectionAsync(\"PhotoCredits\"", layout);
    }

    [Fact]
    public void AnhCoKichThuocChuThayTheVaNguonCongKhai()
    {
        var sources = new[] { "Views/Home/Index.cshtml", "Views/Account/Login.cshtml", "Views/Account/Register.cshtml" }.Select(Read).ToArray();
        foreach (var source in sources)
        {
            Assert.Contains("Nguồn ảnh", source);
            Assert.Contains("https://creativecommons.org/publicdomain/zero/1.0/", source);
            var images = Regex.Matches(source, @"<img\b[^>]*>", RegexOptions.IgnoreCase).Cast<Match>().ToList();
            Assert.NotEmpty(images);
            foreach (var image in images)
            {
                Assert.Matches("alt=\"[^\"]+\"", image.Value);
                Assert.Matches("width=\"[1-9][0-9]*\"", image.Value);
                Assert.Matches("height=\"[1-9][0-9]*\"", image.Value);
                Assert.Matches("loading=\"(lazy|eager)\"", image.Value); // hero tải ngay, ảnh dưới màn đầu tải sau
            }
        }
        var home = sources[0];
        var credits = home[(home.IndexOf("@section PhotoCredits", StringComparison.Ordinal))..];
        foreach (Match image in Regex.Matches(home, "<img[^>]*src=\"~/img/landing/([^\"]+)\"")) Assert.Contains(image.Groups[1].Value, credits);
        Assert.Contains("Jan Kahánek", home);
        Assert.All(sources.Skip(1), s => { Assert.Contains("Joanna Kosinska", s); Assert.Contains("d-none d-lg-block", s); });
        Assert.True(Files("wwwroot/img/landing").Sum(p => new FileInfo(p).Length) <= 2400L * 1024);
        foreach (Match path in Regex.Matches(string.Join('\n', sources), "~/img/landing/([^\"]+\\.webp)")) Assert.True(File.Exists(Path.Combine(Root, "wwwroot/img/landing", path.Groups[1].Value)));
    }

    [Fact]
    public void MenuGomNhomVanGiuBieuMauPost()
    {
        var layout = Read("Views/Shared/_Layout.cshtml");
        Assert.Contains("id=\"admin-menu\" data-bs-toggle=\"dropdown\"", layout);
        Assert.Contains("id=\"account-menu\" data-bs-toggle=\"dropdown\"", layout);
        Assert.Contains("@me.DisplayName</span>", layout);
        foreach (var action in new[] { "SwitchMode", "Logout" }) Assert.Matches($"<form[^>]*method=\"post\"[^>]*asp-controller=\"Account\"[^>]*asp-action=\"{action}\"", layout);
        Assert.DoesNotContain("asp-antiforgery=\"false\"", layout);
        Assert.Contains("asp-action=\"Audit\"", layout);
        Assert.Contains(".btn.theme-toggle { white-space: nowrap", Read("wwwroot/css/mindsprint.css"));
    }

    [Fact]
    public void ChuyenDongTrangTriChiOHeroVaMomentVaTatKhiGiamChuyenDong()
    {
        var css = Read("wwwroot/css/mindsprint.css");
        var animated = new[] { "wwwroot/css", "wwwroot/js" }.SelectMany(Files)
            .SelectMany(p => Regex.Matches(File.ReadAllText(p), @"([^{}]+)\{([^{}]*\banimation(?:-name)?\s*:[^{}]*)\}").Cast<Match>())
            .Where(m => !Regex.IsMatch(m.Groups[2].Value, @"animation(?:-name)?\s*:\s*none\b")).ToList();
        Assert.Equal(2, animated.Count);
        Assert.Contains(animated, m => m.Value.Contains("home-arrive 320ms"));
        Assert.Contains(animated, m => m.Value.Contains("home-marquee"));
        Assert.All(animated, m => Assert.Contains(".home-page", m.Groups[1].Value));
        var reduced = css[css.IndexOf("@media (prefers-reduced-motion: reduce)", StringComparison.Ordinal)..];
        Assert.Contains("animation: none !important", reduced);
        Assert.Contains("transition: none !important", reduced);
        Assert.DoesNotContain("animation:", Read("wwwroot/js/quiz-editor.js"));
    }

    [Fact]
    public void FocusVaCanhBaoDungToken()
    {
        var css = Read("wwwroot/css/mindsprint.css");
        foreach (var control in new[] { ".nav-link", ".btn", ".dropdown-item", ".page-link", ".form-range", ".form-control-plaintext" })
            Assert.Contains(control, css[..css.IndexOf(".skip-link", StringComparison.Ordinal)]);
        Assert.Contains("outline: 3px solid var(--focus) !important; outline-offset: 2px !important; box-shadow: none !important", css);
        Assert.Contains(".alert-warning { color: var(--text)", css);
        Assert.Contains("--bs-table-color: var(--text)", css);
        Assert.Contains("max-width: 70ch", css);
    }

    [Fact]
    public void NhanONhapMoiHienRoVaGiuLienKetKhiThemBotLuaChon()
    {
        foreach (var (file, ids) in new[] {
            ("Views/Classrooms/Index.cshtml", new[] { "new-class-name", "new-class-policy" }),
            ("Views/Classrooms/Join.cshtml", new[] { "join-code" }),
            ("Views/Assignments/Details.cshtml", new[] { "assignment-due", "assignment-due-reason" }),
            ("Views/StudySets/Index.cshtml", new[] { "new-set-title", "set-title-@set.Id", "quiz-title-@set.Id" }),
            ("Views/StudySets/Quiz.cshtml", new[] { "option-text-@i", "option-correct-@i", "blank-answer-@i" }),
            ("Views/Import/Preview.cshtml", new[] { "include-@i", "import-@i-@col" }) })
        {
            var source = Read(file);
            foreach (var id in ids)
            {
                Assert.Contains($"id=\"{id}\"", source);
                Assert.Matches($"<label[^>]*for=\"{Regex.Escape(id)}\"[^>]*>", source);
            }
        }
        var script = Read("wwwroot/js/quiz-editor.js");
        Assert.Contains("label.htmlFor = text.id", script);
        Assert.Contains(".htmlFor = input.id", script);
    }

    [Fact]
    public void ViewportVaMauTrinhDuyetTheoSangToi()
    {
        foreach (var path in new[] { "Views/Shared/_Layout.cshtml", "Views/Shared/JoinQr.cshtml" })
        {
            var source = Read(path);
            Assert.Contains("viewport-fit=cover", source);
            Assert.Matches("<meta name=\"theme-color\" content=\"#F3F7DC\" media=\"\\(prefers-color-scheme: light\\)\"", source);
            Assert.Matches("<meta name=\"theme-color\" content=\"#131317\" media=\"\\(prefers-color-scheme: dark\\)\"", source);
        }
        Assert.Contains("meta[name=\"theme-color\"]", Read("wwwroot/js/theme.js"));
        Assert.Contains("setAttribute('content'", Read("wwwroot/js/theme.js"));
    }

    [Fact]
    public void KhoangTranhTaiThoDungBonBienVaDonViManHinhMoi()
    {
        var css = Read("wwwroot/css/mindsprint.css");
        foreach (var side in new[] { "top", "right", "bottom", "left" })
            Assert.Contains($"--safe-{side}: env(safe-area-inset-{side}, 0px)", css);
        Assert.DoesNotContain("100vh", css);
        Assert.Contains("max(16px, var(--safe-left))", css);
        Assert.Contains("max(16px, var(--safe-right))", css);
        foreach (var path in Files("wwwroot/css")) Assert.DoesNotContain("100vh", File.ReadAllText(path));
        Assert.Contains("var(--safe-top)", css[css.IndexOf(".play-top", StringComparison.Ordinal)..]);
        Assert.Contains("var(--safe-bottom)", css[css.IndexOf(".mindsprint-footer", StringComparison.Ordinal)..]);
    }

    [Fact]
    public void MomentChiTangCuongKhiHoTroVaCoNutDung()
    {
        var script = Read("wwwroot/js/home-moments.js");
        foreach (var text in new[] { "prefers-reduced-motion", "CSS.supports", "animation-timeline", "visibilitychange", "document.hidden", "requestAnimationFrame", "passive: true", "min-width: 992px", "min-height: 700px" }) Assert.Contains(text, script);
        var home = Read("Views/Home/Index.cshtml");
        Assert.Contains("data-moment=\"pinned-chapters\"", home);
        Assert.Contains("data-moment=\"marquee-row\"", home);
        Assert.Matches("<button[^>]*type=\"button\"[^>]*data-marquee-pause[^>]*aria-pressed=\"false\"", home);
        Assert.Contains("~/js/home-moments.js", home);
        Assert.DoesNotContain("home-moments.js", Read("Views/Shared/_Layout.cshtml"));
        foreach (var path in Files("wwwroot").Where(p => !p.EndsWith(".woff2") && !p.EndsWith(".webp") && !p.EndsWith(".ico")))
        {
            var source = File.ReadAllText(path);
            Assert.DoesNotContain("lenis", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("gsap", source, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TieuDeCoDungMotDaiAcidVaNhanNgan()
    {
        var home = Read("Views/Home/Index.cshtml");
        Assert.Single(Regex.Matches(home, "class=\"acid-highlight\"").Cast<Match>());
        Assert.Matches("<h1[^>]*>[^<]*<span class=\"acid-highlight\">[^<]+</span></h1>", home);
        foreach (Match label in Regex.Matches(home, "class=\"eyebrow\">([^<]+)<")) Assert.InRange(label.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length, 1, 4);
        Assert.Contains("#i-check", home); Assert.Contains("<symbol id=\"i-check\"", Read("Views/Shared/_Icons.cshtml")); // icon Lucide dùng chung thay ký tự ✓ ✗ (2026-11-09)
        Assert.DoesNotContain("asp-action=\"Login\"", home);
        Assert.DoesNotMatch(@"font-weight:\s*[89]00", Read("wwwroot/css/mindsprint.css"));
    }

    [Fact]
    public void MoiAnhLandingCoTrongDanhMucVaKhoiNguonAnh()
    {
        var manifest = Read("wwwroot/img/landing/credits.json");
        var home = Read("Views/Home/Index.cshtml");
        var credits = home[home.IndexOf("@section PhotoCredits", StringComparison.Ordinal)..];
        foreach (var path in Files("wwwroot/img/landing").Where(p => Regex.IsMatch(p, @"\.(webp|png|jpe?g|svg)$", RegexOptions.IgnoreCase)))
        {
            var name = Path.GetFileName(path);
            Assert.Contains(name, manifest);
            Assert.Contains(name, credits);
            if (name.StartsWith("study-ai-", StringComparison.Ordinal)) Assert.InRange(new FileInfo(path).Length, 1, 250 * 1024);
        }
        Assert.Contains("Tranh minh họa tạo bằng AI", manifest);
        Assert.Contains("Tranh minh họa tạo bằng AI", credits);
    }
}
