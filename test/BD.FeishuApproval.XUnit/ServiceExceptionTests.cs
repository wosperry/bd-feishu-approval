using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BD.FeishuApproval.Abstractions.Http;
using BD.FeishuApproval.Definitions;
using BD.FeishuApproval.Instances;
using BD.FeishuApproval.Shared.Dtos.Definitions;
using BD.FeishuApproval.Shared.Dtos.Instances;
using BD.FeishuApproval.Shared.Models;
using Moq;

namespace BD.FeishuApproval.XUnit;

public class ServiceExceptionTests
{
    private class StubClient : IFeishuApiClient
    {
        public Uri BaseAddress => new Uri("https://open.feishu.cn");
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            var json = "{\"code\":123,\"msg\":\"biz error\"}";
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(resp);
        }
    }

    [Fact]
    public async Task DefinitionService_OnBizError_ThrowsFeishuApiException()
    {
        var svc = new FeishuApprovalDefinitionService(new StubClient());
        await Assert.ThrowsAsync<FeishuApiException>(async () =>
        {
            _ = await svc.GetDefinitionDetailAsync("code");
        });
    }

    [Fact]
    public async Task InstanceService_OnBizError_ThrowsFeishuApiException()
    {
        var svc = new FeishuApprovalInstanceService(new StubClient());
        await Assert.ThrowsAsync<FeishuApiException>(async () =>
        {
            _ = await svc.CreateInstanceAsync(new CreateInstanceRequest());
        });
    }
}


