using BD.FeishuApproval.Abstractions.Management;
using BD.FeishuApproval.Callbacks;
using BD.FeishuApproval.Shared.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Controllers;

/// <summary>
/// 飞书审批回调处理控制器基类
/// 统一接收飞书审批状态变更回调，并分发到对应的处理器
/// 内置：读取请求体、解密、验签、challenge 响应
/// 子类可以重写钩子方法以扩展逻辑
/// </summary>
[ApiController]
public abstract class FeishuCallbackControllerBase : ControllerBase
{
    protected readonly IFeishuCallbackService _callbackService;
    protected readonly IFeishuManagementService _managementService;
    protected readonly ILogger _logger;
    // 配置System.Text.Json序列化选项
    protected readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true, // 忽略属性名大小写
        WriteIndented = true // 序列化时格式化输出（可选）
    };

    protected FeishuCallbackControllerBase(IServiceProvider provider)
    {
        _logger = provider.GetService<ILogger<FeishuCallbackControllerBase>>();
        _managementService = provider.GetService<IFeishuManagementService>();
        _callbackService = provider.GetService<IFeishuCallbackService>();
    }

    /// <summary>
    /// 飞书开放平台回调入口
    /// </summary>
    [HttpPost]
    public virtual async Task<IActionResult> HandleCallback()
    {
        try
        {
            // 启用缓冲，保证后续能读取
            Request.EnableBuffering();

            if (!Request.Body.CanSeek)
                return BadRequest("请求流不支持重置位置");

            Request.Body.Position = 0;

            // === 读取原始请求体 ===
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
            }

            _logger.LogInformation("收到飞书原始请求: {RawBody}", rawBody);

            // === 获取配置 ===
            var config = await _managementService.GetConfigurationStatusAsync(false);
            if (config?.FeishuConfig == null)
            {
                _logger.LogError("飞书配置未找到");
                return StatusCode(500, "配置错误");
            }

            // === 反序列化加密请求（替换Newtonsoft为System.Text.Json） ===
            EncryptedRequest bodyObj;
            try
            {
                bodyObj = JsonSerializer.Deserialize<EncryptedRequest>(rawBody, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON反序列化失败，原始内容: {RawBody}", rawBody);
                return BadRequest("请求格式错误");
            }

            if (bodyObj == null || string.IsNullOrEmpty(bodyObj.Encrypt))
            {
                _logger.LogError("加密数据为空，原始内容: {RawBody}", rawBody);
                return BadRequest("加密数据缺失");
            }

            // === 解密 ===
            string decryptedData;
            try
            {
                decryptedData = _managementService.DecryptFeishuData(
                    bodyObj.Encrypt,
                    config.FeishuConfig.EncryptKey
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解密失败，加密内容: {EncryptData}", bodyObj.Encrypt);
                return BadRequest("解密失败");
            }

            if (string.IsNullOrEmpty(decryptedData))
            {
                _logger.LogError("解密后数据为空，加密内容: {EncryptData}", bodyObj.Encrypt);
                return BadRequest("解密失败");
            }

            // === 解析解密后JSON（替换JObject为JsonNode） ===
            JsonNode? jsonData;
            try
            {
                jsonData = JsonNode.Parse(decryptedData);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "解密后JSON解析失败，解密内容: {DecryptedData}", decryptedData);
                return BadRequest("数据格式错误");
            }

            if (jsonData == null)
            {
                _logger.LogError("解密后JSON为空，解密内容: {DecryptedData}", decryptedData);
                return BadRequest("数据格式错误");
            }

            // === 验签 ===
            bool isSignatureValid = VerifySignature(rawBody, config.FeishuConfig.EncryptKey);
            if (!isSignatureValid)
            {
                _logger.LogWarning("签名验证失败，原始内容: {RawBody}", rawBody);

                // ⚠️ 配置 URL 验证时，即使签名失败也要返回 challenge
                if (jsonData["challenge"] != null)
                {
                    return Ok(new { result = "failed", challenge = jsonData["challenge"]?.GetValue<string>() });
                }

                return BadRequest("签名验证失败");
            }

            // === URL 验证 ===
            if (jsonData["challenge"] != null)
            {
                _logger.LogInformation("飞书URL验证通过，challenge: {Challenge}", jsonData["challenge"]);
                return Ok(new { result = "success", challenge = jsonData["challenge"]?.GetValue<string>() });
            }

            // === 事件回调 ===
            if (jsonData["event"] != null)
            {
                var callbackEvent = await ParseCallbackData(decryptedData);
                if (callbackEvent == null)
                {
                    return await HandleParseFailure(jsonData);
                }

                var validationResult = await ValidateCallback(callbackEvent);
                if (!validationResult.IsValid)
                {
                    return await HandleValidationFailure(callbackEvent, validationResult.ErrorMessage);
                }

                await PreProcessCallback(callbackEvent);

                var success = await _callbackService.HandleApprovalCallbackAsync(callbackEvent);

                await PostProcessCallback(callbackEvent, success);

                return success
                    ? await HandleCallbackSuccess(callbackEvent)
                    : await HandleCallbackFailure(callbackEvent);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理飞书回调时发生未预期错误");
            return await HandleException(ex, null);
        }
    }

    #region 可重写的钩子

    protected virtual async Task<FeishuCallbackEvent?> ParseCallbackData(string decryptedJson)
    {
        try
        {
            // 替换Newtonsoft反序列化为System.Text.Json
            var callbackEvent = JsonSerializer.Deserialize<FeishuCallbackEvent>(decryptedJson, _jsonOptions);
            return await Task.FromResult(callbackEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析回调数据失败: {Json}", decryptedJson);
            return null;
        }
    }

    protected virtual async Task<ValidationResult> ValidateCallback(FeishuCallbackEvent callbackEvent)
    {
        return await Task.FromResult(new ValidationResult { IsValid = true });
    }

    protected virtual async Task PreProcessCallback(FeishuCallbackEvent callbackEvent)
    {
        await Task.CompletedTask;
    }

    protected virtual async Task PostProcessCallback(FeishuCallbackEvent callbackEvent, bool success)
    {
        await Task.CompletedTask;
    }

    protected virtual async Task<IActionResult> HandleParseFailure(object callbackData)
    {
        return await Task.FromResult(BadRequest("无法解析回调数据"));
    }

    protected virtual async Task<IActionResult> HandleValidationFailure(FeishuCallbackEvent callbackEvent, string errorMessage)
    {
        return await Task.FromResult(BadRequest(errorMessage));
    }

    protected virtual async Task<IActionResult> HandleCallbackSuccess(FeishuCallbackEvent callbackEvent)
    {
        return await Task.FromResult(Ok(new { success = true, message = "回调处理成功" }));
    }

    protected virtual async Task<IActionResult> HandleCallbackFailure(FeishuCallbackEvent callbackEvent)
    {
        return await Task.FromResult(BadRequest(new { success = false, message = "回调处理失败" }));
    }

    protected virtual async Task<IActionResult> HandleException(Exception ex, object callbackData)
    {
        return await Task.FromResult(StatusCode(500, new { success = false, message = "回调处理失败", error = ex.Message }));
    }

    #endregion

    #region 内部模型

    public class EncryptedRequest
    {
        // 注意：如果JSON中的键名与属性名不同，需要添加[JsonPropertyName]特性
        // 例如：[JsonPropertyName("encrypt")]
        public string Encrypt { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion

    /// <summary>
    /// 验证请求签名（可被子类重写）
    /// </summary>
    protected virtual bool VerifySignature(string requestBody, string encryptKey)
    {
        // 默认空实现：子类可重写
        return true;
    }
}
