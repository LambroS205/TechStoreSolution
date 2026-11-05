using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TechStore.Tests.IntegrationTests;

public class StaffMarketingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StaffMarketingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateMarketingClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // 1. Get login page to extract CSRF token
        var getRes = await client.GetAsync("/auth/login");
        var html = await getRes.Content.ReadAsStringAsync();
        var match = Regex.Match(html, @"name=""__RequestVerificationToken""\s+type=""hidden""\s+value=""([^""]+)""");
        if (!match.Success)
        {
            match = Regex.Match(html, @"value=""([^""]+)""\s+name=""__RequestVerificationToken""");
        }
        string token = match.Success ? match.Groups[1].Value : string.Empty;

        // 2. Post login
        var loginData = new Dictionary<string, string>
        {
            { "Username", "staff_marketing" },
            { "Password", "Marketing@123" }
        };
        if (!string.IsNullOrEmpty(token))
        {
            loginData["__RequestVerificationToken"] = token;
        }

        var loginRes = await client.PostAsync("/auth/login", new FormUrlEncodedContent(loginData));
        Assert.True(loginRes.StatusCode == HttpStatusCode.Redirect, $"Login failed with status {loginRes.StatusCode}");
        return client;
    }

    [Fact]
    public async Task MarketingStaff_CanAccess_BannerIndex()
    {
        var client = await CreateMarketingClientAsync();
        var response = await client.GetAsync("/Admin/Banner");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MarketingStaff_CanAccess_CouponIndex()
    {
        var client = await CreateMarketingClientAsync();
        var response = await client.GetAsync("/Admin/Coupon");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MarketingStaff_CanAccess_ReviewIndex()
    {
        var client = await CreateMarketingClientAsync();
        var response = await client.GetAsync("/Admin/Review");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MarketingStaff_CanAccess_BlogIndex()
    {
        var client = await CreateMarketingClientAsync();
        var response = await client.GetAsync("/Admin/Blog");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MarketingStaff_CanAccess_BlogCategories()
    {
        var client = await CreateMarketingClientAsync();
        var response = await client.GetAsync("/Admin/Blog/Categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MarketingStaff_CanAccess_StaffProfile()
    {
        var client = await CreateMarketingClientAsync();
        var response = await client.GetAsync("/Admin/Profile");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MarketingStaff_GET_On_PostOnlyActions_ShouldNotReturn405()
    {
        var client = await CreateMarketingClientAsync();
        
        string[] postOnlyUrls = new[]
        {
            "/Admin/Banner/Save",
            "/Admin/Banner/Delete",
            "/Admin/Coupon/Save",
            "/Admin/Coupon/Delete",
            "/Admin/Review/ToggleStatus",
            "/Admin/Review/Delete"
        };

        foreach (var url in postOnlyUrls)
        {
            var res = await client.GetAsync(url);
            Assert.NotEqual(HttpStatusCode.MethodNotAllowed, res.StatusCode);
        }
    }

    [Fact]
    public async Task MarketingStaff_ProductIndex_HidesUnauthorizedActions()
    {
        var client = await CreateMarketingClientAsync();
        var response = await client.GetAsync("/Admin/Product");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("/Admin/Product/Create", html);
        Assert.DoesNotContain("/Admin/Product/Edit", html);
        Assert.DoesNotContain("/Admin/Product/Delete", html);
        Assert.DoesNotContain("toggleActive(", html);
        Assert.DoesNotContain("toggleFeatured(", html);
    }

    [Fact]
    public async Task MarketingStaff_CategoryIndex_HidesUnauthorizedActions()
    {
        var client = await CreateMarketingClientAsync();
        var response = await client.GetAsync("/Admin/Category");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("openAddCategoryModal()", html);
        Assert.DoesNotContain("openEditCategoryModal(", html);
        Assert.DoesNotContain("/Admin/Category/Delete", html);
        Assert.DoesNotContain("toggleCategoryActive(", html);
        Assert.DoesNotContain("id=\"categoryModal\"", html);
    }

    [Fact]
    public async Task MarketingStaff_BrandIndex_HidesUnauthorizedActions()
    {
        var client = await CreateMarketingClientAsync();
        var response = await client.GetAsync("/Admin/Brand");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("openAddBrandModal()", html);
        Assert.DoesNotContain("openEditBrandModal(", html);
        Assert.DoesNotContain("/Admin/Brand/Delete", html);
        Assert.DoesNotContain("toggleBrandActive(", html);
        Assert.DoesNotContain("id=\"brandModal\"", html);
    }

    [Fact]
    public async Task MarketingStaff_AnalyticsIndex_HidesOrderLinks()
    {
        var client = await CreateMarketingClientAsync();
        var response = await client.GetAsync("/Admin/Analytics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("/Admin/Order", html);
    }

    [Fact]
    public async Task MarketingStaff_ProfileIndex_HidesAuditLogLink()
    {
        var client = await CreateMarketingClientAsync();
        var response = await client.GetAsync("/Admin/Profile");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("/Admin/AuditLog", html);
    }

    [Fact]
    public async Task MarketingStaff_Layout_HidesNotificationBellAndDashboardBreadcrumbs()
    {
        var client = await CreateMarketingClientAsync();
        var response = await client.GetAsync("/Admin/Review");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("id=\"notifDropdownBtn\"", html);
        Assert.DoesNotContain("href=\"/Admin/Dashboard\"", html);
    }
}
