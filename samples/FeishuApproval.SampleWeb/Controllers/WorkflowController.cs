using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using BD.FeishuApproval.Abstractions.Management;
using BD.FeishuApproval.Abstractions.Definitions;
using BD.FeishuApproval.Abstractions.Instances;

namespace FeishuApproval.SampleWeb.Controllers;

/// <summary>
/// 工作流管理控制器
/// 演示正确的飞书审批集成工作流程：订阅 → 获取表单结构 → 生成代码 → 创建审批
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly IFeishuManagementService _managementService;
    private readonly IFeishuApprovalDefinitionService _definitionService;
    private readonly IFeishuApprovalInstanceService _instanceService;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        IFeishuManagementService managementService,
        IFeishuApprovalDefinitionService definitionService,
        IFeishuApprovalInstanceService instanceService,
        ILogger<WorkflowController> logger)
    {
        _managementService = managementService;
        _definitionService = definitionService;
        _instanceService = instanceService;
        _logger = logger;
    }

    /// <summary>
    /// 步骤1: 订阅审批定义更新
    /// </summary>
    /// <param name="approvalCode">审批代码</param>
    /// <param name="adminPassword">管理员密码</param>
    /// <returns>订阅结果</returns>
    [HttpPost("subscribe/{approvalCode}")]
    public async Task<IActionResult> SubscribeApproval([Required] string approvalCode, [FromBody] string adminPassword)
    {
        try
        {
            _logger.LogInformation("开始订阅审批: {ApprovalCode}", approvalCode);

            var result = await _managementService.SubscribeApprovalAsync(approvalCode, adminPassword);
            
            if (result.IsSuccess)
            {
                return Ok(new 
                { 
                    Success = true, 
                    Message = "审批订阅成功，现在可以获取表单结构",
                    ApprovalCode = approvalCode,
                    NextStep = $"/api/workflow/definition/{approvalCode}"
                });
            }
            else
            {
                return BadRequest(new 
                { 
                    Success = false, 
                    Message = result.ErrorMessage 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订阅审批时发生异常: {ApprovalCode}", approvalCode);
            return StatusCode(500, new 
            { 
                Success = false, 
                Message = "系统内部错误" 
            });
        }
    }

    /// <summary>
    /// 步骤2: 获取审批定义详情和表单结构
    /// </summary>
    /// <param name="approvalCode">审批代码</param>
    /// <returns>审批定义详情</returns>
    [HttpGet("definition/{approvalCode}")]
    public async Task<IActionResult> GetApprovalDefinition([Required] string approvalCode)
    {
        try
        {
            _logger.LogInformation("获取审批定义详情: {ApprovalCode}", approvalCode);

            var result = await _definitionService.GetDefinitionDetailAsync(approvalCode);
            
            if (result.IsSuccess)
            {
                return Ok(new 
                { 
                    Success = true, 
                    Message = "获取审批定义成功",
                    Data = result.Data,
                    ApprovalCode = approvalCode,
                    NextSteps = new
                    {
                        GenerateCSharpCode = $"/api/workflow/generate-csharp/{approvalCode}",
                        GenerateTypeScriptCode = $"/api/workflow/generate-typescript/{approvalCode}"
                    }
                });
            }
            else
            {
                return NotFound(new 
                { 
                    Success = false, 
                    Message = result.Message 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取审批定义时发生异常: {ApprovalCode}", approvalCode);
            return StatusCode(500, new 
            { 
                Success = false, 
                Message = "系统内部错误" 
            });
        }
    }

    /// <summary>
    /// 步骤3a: 生成C#实体类代码
    /// </summary>
    /// <param name="approvalCode">审批代码</param>
    /// <returns>生成的C#代码</returns>
    [HttpGet("generate-csharp/{approvalCode}")]
    public async Task<IActionResult> GenerateCSharpCode([Required] string approvalCode)
    {
        try
        {
            _logger.LogInformation("生成C#代码: {ApprovalCode}", approvalCode);

            var code = await _managementService.GenerateEntityCodeAsync(approvalCode);
            
            return Ok(new 
            { 
                Success = true, 
                Message = "C#代码生成成功",
                Code = code,
                Language = "csharp",
                Usage = "将此代码复制到您的项目中，然后使用类型安全的扩展方法创建审批"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成C#代码时发生异常: {ApprovalCode}", approvalCode);
            return StatusCode(500, new 
            { 
                Success = false, 
                Message = "系统内部错误" 
            });
        }
    }

    /// <summary>
    /// 步骤3b: 生成TypeScript接口代码
    /// </summary>
    /// <param name="approvalCode">审批代码</param>
    /// <param name="interfaceName">接口名称（可选）</param>
    /// <returns>生成的TypeScript代码</returns>
    [HttpGet("generate-typescript/{approvalCode}")]
    public async Task<IActionResult> GenerateTypeScriptCode([Required] string approvalCode, [FromQuery] string interfaceName = null)
    {
        try
        {
            _logger.LogInformation("生成TypeScript代码: {ApprovalCode}", approvalCode);

            var code = await _managementService.GenerateTypeScriptCodeAsync(approvalCode, interfaceName);
            
            return Ok(new 
            { 
                Success = true, 
                Message = "TypeScript代码生成成功",
                Code = code,
                Language = "typescript",
                Usage = "将此代码复制到您的前端项目中使用"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成TypeScript代码时发生异常: {ApprovalCode}", approvalCode);
            return StatusCode(500, new 
            { 
                Success = false, 
                Message = "系统内部错误" 
            });
        }
    }

    /// <summary>
    /// 完整工作流程演示：从订阅到代码生成
    /// </summary>
    /// <param name="approvalCode">审批代码</param>
    /// <param name="adminPassword">管理员密码</param>
    /// <returns>完整工作流程结果</returns>
    [HttpPost("complete-workflow/{approvalCode}")]
    public async Task<IActionResult> CompleteWorkflow([Required] string approvalCode, [FromBody] string adminPassword)
    {
        try
        {
            _logger.LogInformation("开始完整工作流程: {ApprovalCode}", approvalCode);

            var results = new Dictionary<string, object>();

            // 步骤1: 订阅审批
            var subscribeResult = await _managementService.SubscribeApprovalAsync(approvalCode, adminPassword);
            results["subscribe"] = new { Success = subscribeResult.IsSuccess, Message = subscribeResult.IsSuccess ? "订阅成功" : subscribeResult.ErrorMessage };
            
            if (!subscribeResult.IsSuccess)
            {
                return BadRequest(new { Success = false, Message = "订阅失败", Results = results });
            }

            // 步骤2: 获取审批定义
            var definitionResult = await _definitionService.GetDefinitionDetailAsync(approvalCode);
            results["definition"] = new { Success = definitionResult.IsSuccess, Message = definitionResult.IsSuccess ? "获取定义成功" : definitionResult.Message };
            
            if (!definitionResult.IsSuccess)
            {
                return BadRequest(new { Success = false, Message = "获取定义失败", Results = results });
            }

            // 步骤3a: 生成C#代码
            try 
            {
                var csharpCode = await _managementService.GenerateEntityCodeAsync(approvalCode);
                results["csharpCode"] = new { Success = true, Message = "C#代码生成成功", Code = csharpCode };
            }
            catch (Exception ex)
            {
                results["csharpCode"] = new { Success = false, Message = $"C#代码生成失败: {ex.Message}" };
            }

            // 步骤3b: 生成TypeScript代码
            try 
            {
                var tsCode = await _managementService.GenerateTypeScriptCodeAsync(approvalCode);
                results["typescriptCode"] = new { Success = true, Message = "TypeScript代码生成成功", Code = tsCode };
            }
            catch (Exception ex)
            {
                results["typescriptCode"] = new { Success = false, Message = $"TypeScript代码生成失败: {ex.Message}" };
            }

            return Ok(new 
            { 
                Success = true, 
                Message = "完整工作流程执行完成",
                ApprovalCode = approvalCode,
                ApprovalName = definitionResult.Data?.ApprovalName,
                Results = results,
                NextStep = "现在您可以使用生成的代码在您的应用中创建审批实例"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行完整工作流程时发生异常: {ApprovalCode}", approvalCode);
            return StatusCode(500, new 
            { 
                Success = false, 
                Message = "系统内部错误" 
            });
        }
    }
}