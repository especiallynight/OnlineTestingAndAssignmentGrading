using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using WebClient;

namespace OnlineTesting.Tests;

/// <summary>
/// Интеграционные тесты статического клиента: проверяют, что WebClient
/// отдаёт страницы, стили и скрипты и что скрипты подключены в верном порядке.
/// </summary>
public class WebClientIntegrationTests : IClassFixture<WebApplicationFactory<WebClientMarker>>
{
    private readonly HttpClient _client;

    public WebClientIntegrationTests(WebApplicationFactory<WebClientMarker> factory)
    {
        _client = factory.CreateClient();
    }

    public static TheoryData<string> Pages => new()
    {
        "/",
        "/index.html",
        "/pages/login.html",
        "/pages/courses.html",
        "/pages/course-details.html",
        "/pages/my-results.html",
        "/pages/grading.html",
        "/pages/test-editor.html",
        "/pages/test-taking.html",
        "/pages/code-submissions.html",
        "/pages/analytics.html",
        "/pages/admin-users.html"
    };

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Page_IsServedAsHtml(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("OnlineTesting", html);
    }

    [Theory]
    [InlineData("/css/style.css", "text/css")]
    [InlineData("/js/app.js", "text/javascript")]
    [InlineData("/js/data.js", "text/javascript")]
    [InlineData("/js/layout.js", "text/javascript")]
    [InlineData("/js/pages/home.js", "text/javascript")]
    [InlineData("/js/pages/login.js", "text/javascript")]
    public async Task Asset_IsServed(string url, string expectedMediaType)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith(expectedMediaType, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AppJs_ContainsHelpersUsedByPages()
    {
        var js = await _client.GetStringAsync("/js/app.js");

        foreach (var helper in new[]
                 {
                     "renderTable", "renderCards", "renderBadge", "statusLabel",
                     "getUser", "setUser", "clearSession", "requireRole",
                     "getCourseById", "getTestById", "getUserById", "bindForm", "barWidth"
                 })
        {
            Assert.Contains(helper, js);
        }
    }

    [Theory]
    [InlineData("/index.html", "./js/app.js")]
    [InlineData("/pages/login.html", "../js/app.js")]
    public async Task AppJs_IsLoadedBeforePageScript(string url, string appScript)
    {
        var html = await _client.GetStringAsync(url);

        var appIndex = html.IndexOf(appScript, StringComparison.Ordinal);
        var pageIndex = html.IndexOf("/js/pages/", StringComparison.Ordinal);

        Assert.True(appIndex >= 0, "app.js не подключён");
        Assert.True(pageIndex >= 0, "скрипт страницы не подключён");
        Assert.True(appIndex < pageIndex, "app.js должен загружаться раньше скрипта страницы");
    }
}
