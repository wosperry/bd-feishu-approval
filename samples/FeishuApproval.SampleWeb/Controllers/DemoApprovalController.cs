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
    private readonly int _fakeUserId = 1;

    public DemoApprovalController(
        IApprovalService approvalService,
        ILogger<DemoApprovalController> logger)
    {
        _approvalService = approvalService;
        _logger = logger;
    }

    /// <summary>
    /// 创建Demo审批 - 简化版本
    /// 用户只需要实现参数类和处理类，无需关注流程细节
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
            var result = await _approvalService.CreateApprovalAsync(request, _fakeUserId);

            _logger.LogInformation("Demo审批创建成功 - 实例ID: {InstanceCode}", result.InstanceCode);
            
            return Ok(new
            {
                Success = true,
                Message = "Demo审批创建成功",
                Data = new
                {
                    InstanceCode = result.InstanceCode,
                    ApprovalCode = "6A109ECD-3578-4243-93F9-DBDCF89515AF", // 从特性获取
                    UserId = _fakeUserId,
                    CreateTime = DateTime.Now,
                    FormData = new
                    {
                        Name = request.姓名,
                        Age = request.年龄_岁
                    }
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Demo审批创建失败: {Message}", ex.Message);
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message,
                ErrorType = "BusinessError"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Demo审批参数错误: {Message}", ex.Message);
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message,
                ErrorType = "ValidationError"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建Demo审批时发生系统异常");
            return StatusCode(500, new
            {
                Success = false,
                Message = "系统内部错误，请稍后重试",
                ErrorType = "SystemError"
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
        var types = _approvalService.GetSupportedApprovalCodes();
        
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
        var isSupported = _approvalService.IsApprovalCodeSupported(approvalType);
        
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

    /// <summary>
    /// 快速测试Demo审批创建
    /// 展示最简单的使用方式
    /// </summary>
    /// <returns>测试结果</returns>
    [HttpPost("test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestDemoApproval()
    {
        try
        {
            // 创建测试数据
            var testRequest = new DemoApprovalRequest
            {
                姓名 = "张三",
                年龄_岁 = 25
            };

            _logger.LogInformation("开始测试Demo审批创建");

            // 调用审批服务
            var result = await _approvalService.CreateApprovalAsync(testRequest, _fakeUserId);

            _logger.LogInformation("Demo审批测试成功 - 实例ID: {InstanceCode}", result.InstanceCode);

            return Ok(new
            {
                Success = true,
                Message = "Demo审批测试成功",
                Data = new
                {
                    InstanceCode = result.InstanceCode,
                    ApprovalCode = "6A109ECD-3578-4243-93F9-DBDCF89515AF",
                    UserId = _fakeUserId,
                    TestData = new
                    {
                        Name = testRequest.姓名,
                        Age = testRequest.年龄_岁
                    },
                    CreateTime = DateTime.Now
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo审批测试失败");
            return StatusCode(500, new
            {
                Success = false,
                Message = "测试失败",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// 获取Demo审批的详细信息
    /// 展示参数类和处理类的结构
    /// </summary>
    /// <returns>Demo审批信息</returns>
    [HttpGet("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetDemoApprovalInfo()
    {
        return Ok(new
        {
            Success = true,
            Message = "Demo审批信息",
            Data = new
            {
                ApprovalCode = "6A109ECD-3578-4243-93F9-DBDCF89515AF",
                Description = "这是一个Demo审批流程，用于展示SDK的使用方式",
                RequiredFiles = new[]
                {
                    "DemoApprovalRequest.cs - 参数类，定义审批表单字段",
                    "DemoApprovalHandler.cs - 处理类，实现审批回调逻辑"
                },
                Usage = new
                {
                    Step1 = "实现DemoApprovalHandler中的回调方法",
                    Step2 = "调用 /api/DemoApproval/create 接口创建审批",
                    Step3 = "系统自动处理用户ID映射、OpenId缓存等细节"
                },
                Example = new
                {
                    Request = new
                    {
                        姓名 = "张三",
                        年龄_岁 = 25
                    },
                    Response = new
                    {
                        Success = true,
                        Message = "Demo审批创建成功",
                        Data = new
                        {
                            InstanceCode = "实例代码",
                            ApprovalCode = "6A109ECD-3578-4243-93F9-DBDCF89515AF",
                            UserId = _fakeUserId,
                            CreateTime = "2024-01-01T00:00:00Z"
                        }
                    }
                }
            }
        });
    }
}