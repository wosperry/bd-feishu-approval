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
    public FeishuCallbackController(
        IFeishuCallbackService callbackService,
        ILogger<FeishuCallbackController> logger) 
        : base(callbackService, logger)
    {
    }

    // 可以重写基类方法进行自定义处理
    // 例如：
    
    // protected override async Task LogCallbackReceived(object callbackData)
    // {
    //     // 自定义日志记录逻辑
    //     _logger.LogInformation("自定义日志: 收到飞书审批回调");
    //     await base.LogCallbackReceived(callbackData);
    // }

    // protected override async Task<IActionResult> HandleCallbackSuccess(FeishuCallbackEvent callbackEvent)
    // {
    //     // 自定义成功处理逻辑
    //     return Ok(new { success = true, message = "自定义成功响应", instanceCode = callbackEvent.InstanceCode });
    // }
}