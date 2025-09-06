using BD.FeishuApproval.Abstractions.Services;
using BD.FeishuApproval.Extensions;
using FeishuApproval.SampleWeb.FeishuApprovals.DemoApproval;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace FeishuApproval.SampleWeb.Controllers;

/// <summary>
/// Demo审批控制器
/// 展示新架构：统一的ApprovalService入口，自动分发到对应Handler
/// 第三方开发者只需关注业务逻辑，不用直接调用底层API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DemoApprovalController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly ILogger<DemoApprovalController> _logger;

    public DemoApprovalController(
        IApprovalService approvalService,
        ILogger<DemoApprovalController> logger)
    {
        _approvalService = approvalService;
        _logger = logger;
    }

    /// <summary>
    /// 创建Demo审批 - 使用新架构：统一ApprovalService
    /// 自动分发到DemoApprovalHandler，包含完整的生命周期管理
    /// </summary>
    /// <param name="request">Demo审批数据</param>
    /// <returns>创建结果</returns>
    /// <response code="200">创建成功，返回实例代码</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateDemoApproval([FromBody] DemoApprovalRequest request)
    {
        try
        {
            _logger.LogInformation("创建Demo审批请求 - 姓名: {Name}, 年龄: {Age}", request.姓名, request.年龄_岁);

            // 🎯 新架构的核心优势：
            // 1. 统一入口，自动分发到DemoApprovalHandler
            // 2. Handler包含完整的生命周期管理：验证 → 预处理 → API调用 → 后处理
            // 3. 第三方开发者只需实现Handler的业务方法，无需关心底层API细节
            var result = await _approvalService.CreateApprovalAsync(request);

            _logger.LogInformation("Demo审批创建成功 - 实例ID: {InstanceCode}", result.InstanceCode);
            
            return Ok(new
            {
                Success = true,
                Message = "Demo审批创建成功",
                Data = new
                {
                    InstanceCode = result.InstanceCode, 
                    CreateTime = DateTime.Now
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Demo审批创建失败: {Message}", ex.Message);
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Demo审批参数错误: {Message}", ex.Message);
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建Demo审批时发生系统异常");
            return StatusCode(500, new
            {
                Success = false,
                Message = "系统内部错误，请稍后重试"
            });
        }
    }

    /// <summary>
    /// 获取所有支持的审批类型
    /// 展示Registry的管理功能
    /// </summary>
    /// <returns>支持的审批类型列表</returns>
    [HttpGet("types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetSupportedApprovalTypes()
    {
        var types = _approvalService.GetSupportedApprovalTypes();
        
        return Ok(new
        {
            Success = true,
            Data = new
            {
                Count = types.Length,
                ApprovalTypes = types,
                Message = "当前系统支持的审批类型"
            }
        });
    }

    /// <summary>
    /// 检查指定审批类型是否支持
    /// </summary>
    /// <param name="approvalType">审批类型</param>
    /// <returns>是否支持</returns>
    [HttpGet("types/{approvalType}/supported")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult IsApprovalTypeSupported(string approvalType)
    {
        var isSupported = _approvalService.IsApprovalTypeSupported(approvalType);
        
        return Ok(new
        {
            Success = true,
            Data = new
            {
                ApprovalType = approvalType,
                IsSupported = isSupported,
                Message = isSupported ? "支持此审批类型" : "不支持此审批类型"
            }
        });
    }
}