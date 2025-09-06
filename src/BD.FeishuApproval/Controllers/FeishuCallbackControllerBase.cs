using BD.FeishuApproval.Callbacks;
using BD.FeishuApproval.Shared.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Controllers;

/// <summary>
/// 飞书审批回调处理控制器基类
/// 统一接收飞书审批状态变更回调，并分发到对应的处理器
/// 用户可以继承此类并重写相关方法进行自定义处理
/// </summary>
[ApiController]
public abstract class FeishuCallbackControllerBase : ControllerBase
{
    protected readonly IFeishuCallbackService _callbackService;
    protected readonly ILogger _logger;

    protected FeishuCallbackControllerBase(
        IFeishuCallbackService callbackService,
        ILogger logger)
    {
        _callbackService = callbackService;
        _logger = logger;
    }

    /// <summary>
    /// 处理飞书审批状态变更回调
    /// 子类可以重写此方法进行自定义处理
    /// </summary>
    /// <param name="callbackData">回调数据</param>
    /// <returns>处理结果</returns>
    [HttpPost]
    public virtual async Task<IActionResult> HandleApprovalCallback([FromBody] object callbackData)
    {
        try
        {
            // 记录回调日志（可被子类重写）
            await LogCallbackReceived(callbackData);

            // 解析回调数据（可被子类重写）
            var callbackEvent = await ParseCallbackData(callbackData);
            
            if (callbackEvent == null)
            {
                _logger.LogWarning("无法解析回调数据");
                return await HandleParseFailure(callbackData);
            }

            // 验证回调数据（可被子类重写）
            var validationResult = await ValidateCallback(callbackEvent);
            if (!validationResult.IsValid)
            {
                return await HandleValidationFailure(callbackEvent, validationResult.ErrorMessage);
            }

            // 处理回调前的预处理（可被子类重写）
            await PreProcessCallback(callbackEvent);

            // 处理回调
            var success = await _callbackService.HandleApprovalCallbackAsync(callbackEvent);

            // 处理回调后的后处理（可被子类重写）
            await PostProcessCallback(callbackEvent, success);

            if (success)
            {
                return await HandleCallbackSuccess(callbackEvent);
            }
            else
            {
                return await HandleCallbackFailure(callbackEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理飞书审批回调失败");
            return await HandleException(ex, callbackData);
        }
    }

    /// <summary>
    /// 飞书回调验证端点（用于配置回调URL时的验证）
    /// 子类可以重写此方法进行自定义验证逻辑
    /// </summary>
    [HttpPost("verify")]
    public virtual async Task<IActionResult> VerifyCallback([FromBody] CallbackVerificationRequest request)
    {
        try
        {
            _logger.LogInformation("收到飞书回调验证请求: {Challenge}", request?.Challenge);
            
            // 验证请求（可被子类重写）
            var validationResult = await ValidateVerificationRequest(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ErrorMessage);
            }

            // 处理验证逻辑（可被子类重写）
            var response = await ProcessVerificationRequest(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理回调验证失败");
            return await HandleVerificationException(ex, request);
        }
    }

    #region 可重写的钩子方法

    /// <summary>
    /// 记录回调接收日志（子类可重写）
    /// </summary>
    protected virtual async Task LogCallbackReceived(object callbackData)
    {
        _logger.LogInformation("收到飞书审批回调: {CallbackData}", callbackData);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 解析回调数据（子类可重写）
    /// </summary>
    protected virtual async Task<FeishuCallbackEvent?> ParseCallbackData(object callbackData)
    {
        try
        {
            var callbackJson = JsonSerializer.Serialize(callbackData);
            var callbackEvent = JsonSerializer.Deserialize<FeishuCallbackEvent>(callbackJson);
            return await Task.FromResult(callbackEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析回调数据失败");
            return null;
        }
    }

    /// <summary>
    /// 验证回调数据（子类可重写）
    /// </summary>
    protected virtual async Task<ValidationResult> ValidateCallback(FeishuCallbackEvent callbackEvent)
    {
        if (string.IsNullOrEmpty(callbackEvent.InstanceCode))
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "回调数据缺少实例代码" };
        }
        
        return await Task.FromResult(new ValidationResult { IsValid = true });
    }

    /// <summary>
    /// 验证验证请求（子类可重写）
    /// </summary>
    protected virtual async Task<ValidationResult> ValidateVerificationRequest(CallbackVerificationRequest? request)
    {
        if (request?.Challenge == null)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "缺少Challenge码" };
        }
        
        return await Task.FromResult(new ValidationResult { IsValid = true });
    }

    /// <summary>
    /// 处理验证请求（子类可重写）
    /// </summary>
    protected virtual async Task<object> ProcessVerificationRequest(CallbackVerificationRequest request)
    {
        return await Task.FromResult(new { challenge = request.Challenge });
    }

    /// <summary>
    /// 回调前预处理（子类可重写）
    /// </summary>
    protected virtual async Task PreProcessCallback(FeishuCallbackEvent callbackEvent)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 回调后后处理（子类可重写）
    /// </summary>
    protected virtual async Task PostProcessCallback(FeishuCallbackEvent callbackEvent, bool success)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理解析失败（子类可重写）
    /// </summary>
    protected virtual async Task<IActionResult> HandleParseFailure(object callbackData)
    {
        return await Task.FromResult(BadRequest("无法解析回调数据"));
    }

    /// <summary>
    /// 处理验证失败（子类可重写）
    /// </summary>
    protected virtual async Task<IActionResult> HandleValidationFailure(FeishuCallbackEvent callbackEvent, string errorMessage)
    {
        return await Task.FromResult(BadRequest(errorMessage));
    }

    /// <summary>
    /// 处理回调成功（子类可重写）
    /// </summary>
    protected virtual async Task<IActionResult> HandleCallbackSuccess(FeishuCallbackEvent callbackEvent)
    {
        return await Task.FromResult(Ok(new { success = true, message = "回调处理成功" }));
    }

    /// <summary>
    /// 处理回调失败（子类可重写）
    /// </summary>
    protected virtual async Task<IActionResult> HandleCallbackFailure(FeishuCallbackEvent callbackEvent)
    {
        return await Task.FromResult(BadRequest(new { success = false, message = "回调处理失败" }));
    }

    /// <summary>
    /// 处理异常（子类可重写）
    /// </summary>
    protected virtual async Task<IActionResult> HandleException(Exception ex, object callbackData)
    {
        return await Task.FromResult(StatusCode(500, new { success = false, message = "回调处理失败", error = ex.Message }));
    }

    /// <summary>
    /// 处理验证异常（子类可重写）
    /// </summary>
    protected virtual async Task<IActionResult> HandleVerificationException(Exception ex, CallbackVerificationRequest? request)
    {
        return await Task.FromResult(StatusCode(500, new { success = false, message = "回调验证失败", error = ex.Message }));
    }

    #endregion
}

/// <summary>
/// 回调验证请求模型
/// </summary>
public class CallbackVerificationRequest
{
    /// <summary>
    /// 挑战码
    /// </summary>
    public string? Challenge { get; set; }

    /// <summary>
    /// 令牌
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    public string? Type { get; set; }
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 是否验证通过
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }
}