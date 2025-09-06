using System.Threading;
using System.Threading.Tasks;
using BD.FeishuApproval.Abstractions.Hooks;
using BD.FeishuApproval.Shared.Models;

namespace BD.FeishuApproval.Hooks;

public class DefaultFailureHook : IFeishuFailureHook
{
    public Task OnApiFailureAsync(FeishuRequestContext context, FeishuResponse response, string errorMessage, CancellationToken cancellationToken = default)
    {
        // 默认空实现：用户可通过替换服务注册来自定义，例如短信/钉钉告警
        return Task.CompletedTask;
    }
}


