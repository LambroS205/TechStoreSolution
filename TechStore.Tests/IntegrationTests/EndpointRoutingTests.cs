using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TechStore.Tests.IntegrationTests;

public class EndpointRoutingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointRoutingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/products")]
    [InlineData("/blogs")]
    [InlineData("/about")]
    [InlineData("/contact")]
    [InlineData("/auth/login")]
    [InlineData("/auth/register")]
    [InlineData("/order/tracking")]
    [InlineData("/warranty-check")]
    public async Task PublicPages_ReturnSuccessStatusCode(string url)
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.NotEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Redirect);
    }

    [Theory]
    [InlineData("/Admin")]
    [InlineData("/Admin/Banner")]
    [InlineData("/Admin/Blog")]
    [InlineData("/Admin/Coupon")]
    [InlineData("/Admin/Review")]
    [InlineData("/Admin/Profile")]
    [InlineData("/Admin/Profile/UpdateInfo")]
    [InlineData("/Admin/Profile/ChangePassword")]
    [InlineData("/account/profile")]
    [InlineData("/account/orders")]
    [InlineData("/account/update-profile")]
    [InlineData("/account/change-password")]
    [InlineData("/checkout/place-order")]
    [InlineData("/product/test-slug/review")]
    public async Task UnauthenticatedAccess_ShouldNeverReturn405(string url)
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act: GET
        var getResponse = await client.GetAsync(url);

        // Assert: It should redirect to login or show view, NEVER return 405 MethodNotAllowed
        Assert.NotEqual(HttpStatusCode.MethodNotAllowed, getResponse.StatusCode);
    }
    [Theory]
    [InlineData("/checkout")]
    [InlineData("/checkout/success/TEST-ORDER")]
    [InlineData("/product/sony-wh-1000xm5")]
    [InlineData("/product/sony-wh-1000xm5/review")]
    public async Task PostToPublicRoutes_ShouldNeverReturn405(string url)
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act: POST
        var postResponse = await client.PostAsync(url, new StringContent(""));

        // Assert: It should redirect or succeed, NEVER return 405 MethodNotAllowed
        Assert.NotEqual(HttpStatusCode.MethodNotAllowed, postResponse.StatusCode);
    }

    [Fact]
    public async Task ProductDetail_ContainsAllRequiredInteractiveFunctions()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/product/sony-wh-1000xm5");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var html = await response.Content.ReadAsStringAsync();
        
        Assert.Contains("selectVariant", html);
        Assert.Contains("adjustPdpQuantity", html);
        Assert.Contains("handleAddCurrentToCart", html);
        Assert.Contains("pdpBuyNowBtn", html);
        Assert.Contains("pdpAddToCartBtn", html);
        Assert.DoesNotContain("[Thu Cũ Đổi Mới] Máy cũ:  - Ước tính trừ: ;", html);
    }
}
