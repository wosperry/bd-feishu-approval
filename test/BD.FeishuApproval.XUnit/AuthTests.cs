using System.Net;
using System.Net.Http;
using System.Text;
using BD.FeishuApproval.Abstractions.Configs;
using BD.FeishuApproval.Auth;
using BD.FeishuApproval.Shared.Options;
using Moq;
using RichardSzalay.MockHttp;

namespace BD.FeishuApproval.XUnit;

public class AuthTests
{
    [Fact]
    public async Task GetTenantAccessTokenAsync_ReturnsToken()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal")
            .Respond("application/json", "{\"code\":0,\"tenant_access_token\":\"tkn\",\"expire\":7200}");

        var factory = new Mock<IHttpClientFactory>();
        var httpClient = new HttpClient(mockHttp) { BaseAddress = new Uri("https://open.feishu.cn") };
        factory.Setup(f => f.CreateClient(nameof(FeishuAuthService))).Returns(httpClient);

        var config = new Mock<IFeishuConfigProvider>();
        config.Setup(c => c.GetApiOptions()).Returns(new FeishuApiOptions
        {
            AppId = "app",
            AppSecret = "secret",
            BaseUrl = "https://open.feishu.cn"
        });

        var svc = new FeishuAuthService(config.Object, factory.Object);
        var token = await svc.GetTenantAccessTokenAsync();
        Assert.Equal("tkn", token.TenantAccessToken);
        Assert.True(token.ExpireSeconds > 0);
    }
}


