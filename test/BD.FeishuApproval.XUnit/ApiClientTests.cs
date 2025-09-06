using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BD.FeishuApproval.Abstractions.Auth;
using BD.FeishuApproval.Abstractions.Persistence;
using BD.FeishuApproval.Http;
using BD.FeishuApproval.Shared.Models;
using Moq;

namespace BD.FeishuApproval.XUnit;

public class ApiClientTests
{
    private class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
            return Task.FromResult(resp);
        }
    }

    [Fact]
    public async Task SendAsync_LogsRequestAndResponse()
    {
        var httpClient = new HttpClient(new StubHandler()) { BaseAddress = new Uri("https://open.feishu.cn") };
        var auth = new Mock<IFeishuAuthService>();
        auth.Setup(a => a.GetTenantAccessTokenAsync(false)).ReturnsAsync(new FeishuToken { TenantAccessToken = "tkn", ExpireSeconds = 7200 });

        var repo = new Mock<IFeishuApprovalRepository>();
        repo.Setup(r => r.SaveRequestLogAsync(It.IsAny<FeishuRequestLog>(), default)).Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveResponseLogAsync(It.IsAny<FeishuResponseLog>(), default)).Returns(Task.CompletedTask);

        var client = new FeishuApiClient(httpClient, auth.Object, repo.Object);
        var resp = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/open-apis/ping"));
        Assert.True(resp.IsSuccessStatusCode);
        repo.Verify(r => r.SaveRequestLogAsync(It.IsAny<FeishuRequestLog>(), default), Times.Once);
        repo.Verify(r => r.SaveResponseLogAsync(It.IsAny<FeishuResponseLog>(), default), Times.Once);
    }
}


