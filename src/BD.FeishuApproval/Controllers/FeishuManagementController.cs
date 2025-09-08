#if NET8_0_OR_GREATER
using BD.FeishuApproval.Abstractions.Health;
using BD.FeishuApproval.Abstractions.Management;
using BD.FeishuApproval.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BD.FeishuApproval.Controllers;

/// <summary>
/// 飞书管理 API 控制器
/// 提供所有管理界面功能的 REST API 接口，方便第三方开发自己的管理界面
/// </summary>
[ApiController]
[Route("api/feishu-management")]
[Produces("application/json")]
public class FeishuManagementController : ControllerBase
{
    private readonly IFeishuManagementService _managementService;
    private readonly ILogger<FeishuManagementController> _logger;

    public FeishuManagementController(
        IFeishuManagementService managementService,
        ILogger<FeishuManagementController> logger)
    {
        _managementService = managementService;
        _logger = logger;
    }

    #region 原有系统管理接口
    /// <summary>
    /// 获取系统配置状态
    /// </summary>
    [HttpGet("configuration-status")]
    public async Task<ActionResult<ConfigurationStatus>> GetConfigurationStatus()
    {
        var result = await _managementService.GetConfigurationStatusAsync();
        return Ok(result);
    }

    /// <summary>
    /// 设置管理员密码
    /// </summary>
    [HttpPost("admin-password")]
    public async Task<ActionResult<ManagementOperationResult>> SetAdminPassword([FromBody] SetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ManagementOperationResult.Failure("请求参数无效"));
        }

        var result = await _managementService.SetAdminPasswordAsync(request.Password);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// 验证管理员密码
    /// </summary>
    [HttpPost("admin-password/verify")]
    public async Task<ActionResult<bool>> VerifyAdminPassword([FromBody] VerifyPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var result = await _managementService.VerifyAdminPasswordAsync(request.Password);
        return Ok(result);
    }

    /// <summary>
    /// 检查是否已设置管理员密码
    /// </summary>
    [HttpGet("admin-password/status")]
    public async Task<ActionResult<AdminPasswordStatusResult>> GetAdminPasswordStatus()
    {
        var status = await _managementService.GetConfigurationStatusAsync();
        return Ok(new AdminPasswordStatusResult
        {
            HasPassword = status.HasAdminPassword,
            IsInitialized = status.HasAdminPassword
        });
    }

    /// <summary>
    /// 保存飞书应用配置
    /// </summary>
    [HttpPost("feishu-config")]
    public async Task<ActionResult<ManagementOperationResult>> SaveFeishuConfig([FromBody] SaveConfigRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ManagementOperationResult.Failure("请求参数无效"));
        }

        var config = new FeishuConfigRequest
        {
            AppId = request.AppId,
            AppSecret = request.AppSecret,
            EncryptKey = request.EncryptKey,
            VerificationToken = request.VerificationToken
        };

        var result = await _managementService.SaveFeishuConfigAsync(config, request.AdminPassword);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// 登记审批流程
    /// </summary>
    [HttpPost("approvals")]
    public async Task<ActionResult<ManagementOperationResult<long>>> RegisterApproval([FromBody] RegisterApprovalRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ManagementOperationResult<long>.Failure("请求参数无效"));
        }

        var registration = new ApprovalRegistrationRequest
        {
            ApprovalCode = request.ApprovalCode,
            DisplayName = request.DisplayName,
            Description = request.Description
        };

        var result = await _managementService.RegisterApprovalAsync(registration, request.AdminPassword);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// 获取已登记的审批流程列表
    /// </summary>
    [HttpGet("approvals")]
    public async Task<ActionResult<System.Collections.Generic.List<Shared.Models.FeishuApprovalRegistration>>> GetRegisteredApprovals()
    {
        var result = await _managementService.GetRegisteredApprovalsAsync();
        return Ok(result);
    }

    /// <summary>
    /// 生成审批实体类代码
    /// </summary>
    [HttpGet("code-generation/{approvalCode}")]
    public async Task<ActionResult<string>> GenerateEntityCode([Required] string approvalCode)
    {
        if (string.IsNullOrWhiteSpace(approvalCode))
        {
            return BadRequest("审批代码不能为空");
        }

        var result = await _managementService.GenerateEntityCodeAsync(approvalCode);
        return Ok(result);
    }

    /// <summary>
    /// 生成TypeScript接口代码
    /// </summary>
    [HttpGet("typescript-generation/{approvalCode}")]
    public async Task<ActionResult<string>> GenerateTypeScriptCode([Required] string approvalCode, [FromQuery] string interfaceName = null)
    {
        if (string.IsNullOrWhiteSpace(approvalCode))
        {
            return BadRequest("审批代码不能为空");
        }

        var result = await _managementService.GenerateTypeScriptCodeAsync(approvalCode, interfaceName);
        return Ok(result);
    }

    /// <summary>
    /// 订阅审批定义更新
    /// </summary>
    [HttpPost("subscribe/{approvalCode}")]
    public async Task<ActionResult<ManagementOperationResult>> SubscribeApproval([Required] string approvalCode, [FromBody] SubscribeRequest request)
    {
        if (string.IsNullOrWhiteSpace(approvalCode))
        {
            return BadRequest("审批代码不能为空");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _managementService.SubscribeApprovalAsync(approvalCode, request.AdminPassword);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// 获取审批定义详情
    /// </summary>
    [HttpGet("definitions/{approvalCode}")]
    public async Task<ActionResult<Shared.Dtos.Definitions.ApprovalDefinitionDetail>> GetApprovalDefinition([Required] string approvalCode)
    {
        if (string.IsNullOrWhiteSpace(approvalCode))
        {
            return BadRequest("审批代码不能为空");
        }

        try
        {
            var result = await _managementService.GetApprovalDefinitionAsync(approvalCode);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 查询失败任务列表
    /// </summary>
    [HttpGet("failed-jobs")]
    public async Task<ActionResult<PagedResult<Shared.Models.FailedJob>>> GetFailedJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _managementService.GetFailedJobsAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// 标记失败任务为成功
    /// </summary>
    [HttpPost("failed-jobs/{jobId}/resolve")]
    public async Task<ActionResult<ManagementOperationResult>> ResolveFailedJob([Required] int jobId)
    {
        var result = await _managementService.ResolveFailedJobAsync(jobId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// 查询请求日志
    /// </summary>
    [HttpGet("logs/requests")]
    public async Task<ActionResult<PagedResult<Shared.Models.FeishuRequestLog>>> GetRequestLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _managementService.GetRequestLogsAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// 查询响应日志
    /// </summary>
    [HttpGet("logs/responses")]
    public async Task<ActionResult<PagedResult<Shared.Models.FeishuResponseLog>>> GetResponseLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _managementService.GetResponseLogsAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// 查询管理操作日志
    /// </summary>
    [HttpGet("logs/management")]
    public async Task<ActionResult<PagedResult<Shared.Models.FeishuManageLog>>> GetManageLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _managementService.GetManageLogsAsync(page, pageSize);
        return Ok(result);
    }

    #endregion

    #region 飞书URL验证/事件接收接口（POST请求）
    public class EncryptedRequest
    {
        public string Encrypt { get; set; }
    }

    /// <summary>
    /// 飞书开放平台回调接口
    /// 处理URL验证和事件推送
    /// </summary>
    [HttpPost("callback")]
    public async Task<IActionResult> FeishuEventCallback()
    {
        try
        {
            // 启用缓冲，保证后续能再次读取
            Request.EnableBuffering();

            if (!Request.Body.CanSeek)
            {
                return BadRequest("请求流不支持重置位置");
            }

            // 重置流位置到起始点
            Request.Body.Position = 0;

            // === 读取请求体（一次即可）===
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0; // 重置流，保证后续中间件可读
            }

            _logger.LogInformation("request body : {RawBody}", rawBody);

            // === 获取配置 ===
            var config = await _managementService.GetConfigurationStatusAsync(false);
            if (config?.FeishuConfig == null)
            {
                _logger.LogError("飞书配置未找到");
                return StatusCode(500, "配置错误");
            }

            // === 反序列化加密请求 ===
            EncryptedRequest bodyObj;
            try
            {
                bodyObj = JsonConvert.DeserializeObject<EncryptedRequest>(rawBody);
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

            // === 解密数据 ===
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

            JObject jsonData;
            try
            {
                jsonData = JObject.Parse(decryptedData);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "解密后JSON解析失败，解密内容: {DecryptedData}", decryptedData);
                return BadRequest("数据格式错误");
            }

            // === 验签 ===
            bool isSignatureValid = VerifySignature(rawBody, config.FeishuConfig.EncryptKey);
            if (!isSignatureValid)
            {
                _logger.LogWarning("签名验证失败，原始内容: {RawBody}", rawBody);

                // ⚠️ 配置 URL 验证时，即使签名失败也要返回 challenge，否则飞书不通过
                if (jsonData.ContainsKey("challenge"))
                {
                    return Ok(new { result = "failed", challenge = jsonData["challenge"]?.ToString() });
                }

                return BadRequest("签名验证失败");
            }

            // === 处理 URL 验证请求 ===
            if (jsonData.ContainsKey("challenge"))
            {
                _logger.LogInformation("飞书URL验证通过，challenge: {Challenge}", jsonData["challenge"]);
                return Ok(new { result = "success", challenge = jsonData["challenge"]?.ToString() });
            }

            // === 处理事件回调请求 ===
            if (jsonData.ContainsKey("event"))
            {
                var eventType = jsonData["event"]?["type"]?.ToString() ?? "未知类型";
                _logger.LogInformation("收到飞书事件: {EventType}, 数据: {DecryptedData}", eventType, decryptedData);

                // TODO: 在这里处理你的业务逻辑
                // 异步处理，避免阻塞飞书回调
                _ = Task.Run(() => ProcessFeishuEvent(jsonData));

                // 飞书要求返回200空响应
                return Ok();
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理飞书回调时发生未预期错误");
            return StatusCode(500, "服务器内部错误");
        }
    }

    /// <summary>
    /// 单独的事件处理方法
    /// </summary>
    private async Task ProcessFeishuEvent(JObject eventData)
    {
        try
        {
            // 实际的事件处理逻辑（如保存数据库、调用其他服务等）
            await Task.Delay(100); // 示例：模拟处理
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理飞书事件时出错，事件数据: {EventData}", eventData.ToString());
        }
    }


    /// <summary>
    /// 验证请求签名
    /// </summary>
    private bool VerifySignature(string requestBody, string encrytKey)
    {
        // 记录所有请求头
        var headers = new Dictionary<string, string>();
        foreach (var header in Request.Headers)
        {
            headers[header.Key] = header.Value;
            _logger.LogInformation($"Header: {header.Key} = {header.Value}");
        }


        // 从请求头获取时间戳、随机数和签名
        if (!Request.Headers.TryGetValue("X-Lark-Request-Timestamp", out var timestamp))
            return false;

        if (!Request.Headers.TryGetValue("X-Lark-Request-Nonce", out var nonce))
            return false;

        if (!Request.Headers.TryGetValue("X-Lark-Signature", out var signature))
            return false;

        // 拼接字符串
        string signatureBase = $"{timestamp}{nonce}{encrytKey}{requestBody}";

        // 计算SHA256哈希
        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(signatureBase));
            string computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            // 调试时可以输出计算过程以便排查
            _logger.LogDebug($"计算的签名: {computedSignature}");
            _logger.LogDebug($"收到的签名: {signature}");
            _logger.LogDebug($"签名基础字符串: {signatureBase}");

            // 比较签名
            return computedSignature == signature;
        }
    }
    #endregion
}

#region 请求DTO
/// <summary>
/// 设置密码请求
/// </summary>
public class SetPasswordRequest
{
    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    [MinLength(6, ErrorMessage = "密码长度至少6位")]
    public string Password { get; set; }
}

/// <summary>
/// 验证密码请求
/// </summary>
public class VerifyPasswordRequest
{
    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; }
}

/// <summary>
/// 保存配置请求
/// </summary>
public class SaveConfigRequest
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [Required(ErrorMessage = "应用ID不能为空")]
    public string AppId { get; set; }

    /// <summary>
    /// 应用密钥
    /// </summary>
    [Required(ErrorMessage = "应用密钥不能为空")]
    public string AppSecret { get; set; }

    /// <summary>
    /// 加密密钥（可选）
    /// </summary>
    public string EncryptKey { get; set; }

    /// <summary>
    /// 验证令牌（可选）
    /// </summary>
    public string VerificationToken { get; set; }

    /// <summary>
    /// 管理员密码
    /// </summary>
    [Required(ErrorMessage = "管理员密码不能为空")]
    public string AdminPassword { get; set; }
}

/// <summary>
/// 审批登记请求
/// </summary>
public class RegisterApprovalRequest
{
    /// <summary>
    /// 审批代码
    /// </summary>
    [Required(ErrorMessage = "审批代码不能为空")]
    public string ApprovalCode { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 管理员密码
    /// </summary>
    [Required(ErrorMessage = "管理员密码不能为空")]
    public string AdminPassword { get; set; }
}

/// <summary>
/// 订阅请求
/// </summary>
public class SubscribeRequest
{
    /// <summary>
    /// 管理员密码
    /// </summary>
    [Required(ErrorMessage = "管理员密码不能为空")]
    public string AdminPassword { get; set; }
}

/// <summary>
/// 管理员密码状态结果
/// </summary>
public class AdminPasswordStatusResult
{
    /// <summary>
    /// 是否已设置密码
    /// </summary>
    public bool HasPassword { get; set; }

    /// <summary>
    /// 是否已初始化（与HasPassword相同，为了兼容性）
    /// </summary>
    public bool IsInitialized { get; set; }
}
#endregion
#endif
