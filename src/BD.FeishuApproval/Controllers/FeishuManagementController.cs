#if NET8_0_OR_GREATER
using BD.FeishuApproval.Abstractions.Health;
using BD.FeishuApproval.Abstractions.Management;
using BD.FeishuApproval.Shared.Models;
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
    private class EncryptedRequest
    {
        public string Encrypt { get; set; }
    }
    /// <summary>
    /// 飞书开放平台回调接口
    /// 处理URL验证和事件推送
    /// </summary>
    [HttpPost("health")]
    public async Task<IActionResult> FeishuEventCallback()
    {
        // TODO: 分发到事件的处理器（好像还有一个
        try
        {
            // 读取原始请求体
            string requestBody;
            using (var reader = new System.IO.StreamReader(Request.Body))
            {
                requestBody = await reader.ReadToEndAsync();
                _logger.LogInformation($"request body : {requestBody}");
            }

            var config = await _managementService.GetConfigurationStatusAsync(false);

            // 解析请求体获取加密字符串
            var requestData = JsonConvert.DeserializeObject<EncryptedRequest>(requestBody);
            if (string.IsNullOrEmpty(requestData.Encrypt))
            {
                return BadRequest("缺少encrypt字段");
            }

            // 解密数据
            string decryptedData = _managementService.DecryptFeishuData(requestData.Encrypt, config.FeishuConfig.EncryptKey);
            if (string.IsNullOrEmpty(decryptedData))
            {
                return BadRequest("解密失败");
            }

            // 解析解密后的数据
            var jsonData = JObject.Parse(decryptedData);

            // 验证签名
            bool isSignatureValid = VerifySignature(requestBody, config.FeishuConfig.VerificationToken);
            if (!isSignatureValid)
            {
                return Ok(new { result = "failed", challenge = jsonData["challenge"].ToString() });
            }

            // 处理URL验证请求
            if (jsonData.ContainsKey("challenge"))
            {
                return Ok(new { result = "success", challenge = jsonData["challenge"].ToString() });
            }

            // 处理事件回调请求
            if (jsonData.ContainsKey("event"))
            {
                // 这里可以添加事件处理逻辑
                var eventType = jsonData["header"]["event_type"].ToString();
                // 记录事件日志
                Console.WriteLine($"收到飞书事件: {eventType}, 数据: {decryptedData}");

                // 飞书要求回调接口返回200空响应
                return Ok();
            }

            return Ok();
        }
        catch (Exception ex)
        {
            // 记录错误日志
            Console.WriteLine($"处理飞书回调出错: {ex.Message}, 堆栈: {ex.StackTrace}");
            return StatusCode(500, "服务器内部错误");
        }
    }

    /// <summary>
    /// 验证请求签名
    /// </summary>
    private bool VerifySignature(string requestBody, string verificationToken)
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
        string signatureBase = $"{timestamp}{nonce}{verificationToken}{requestBody}";

        // 计算SHA256哈希
        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(signatureBase));
            string computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

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
#endregion
#endif
