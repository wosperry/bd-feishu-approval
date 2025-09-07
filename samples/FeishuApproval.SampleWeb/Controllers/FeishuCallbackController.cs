using BD.FeishuApproval.Callbacks;
using BD.FeishuApproval.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FeishuApproval.SampleWeb.Controllers;

/// <summary>
/// 飞书审批回调处理控制器示例实现
/// 继承自SDK提供的基类，可以根据需要重写相关方法
/// </summary>
[Route("api/feishu/approval/callback")]
public class FeishuCallbackController : FeishuCallbackControllerBase
{
    public FeishuCallbackController(IServiceProvider provider) 
        : base(provider)
    {
    }
}