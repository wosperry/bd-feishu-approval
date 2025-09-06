using BD.FeishuApproval.Extensions;
using BD.FeishuApproval.Dashboard;
using FeishuApproval.SampleWeb.FeishuApprovals.DemoApproval;

var builder = WebApplication.CreateBuilder(args);

// 🔒 安全配置：从配置文件或环境变量读取连接字符串
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("数据库连接字符串未配置。请在 appsettings.json 中设置 ConnectionStrings:DefaultConnection");

// 添加飞书审批服务
builder.Services.AddFeishuApproval(connectionString, "sqlite");

// 手动注册Demo审批处理器（临时解决方案）
builder.Services.AddApprovalHandler<DemoApprovalHandler, DemoApprovalRequest>();

// 添加新的审批服务架构
builder.Services
    .AddFeishuApprovalCoreServices()  // 只注册核心服务，不自动扫描
    .AddFeishuDashboardTemplatesForProduction();

// 添加控制器和API服务
builder.Services.AddControllers();

// 添加Swagger文档生成
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "飞书审批API", 
        Version = "v1",
        Description = "飞书审批系统API接口文档，提供审批实例的创建、查询等功能"
    });
});

builder.Services.AddHostedService<FeishuApproval.SampleWeb.StartupMigrationHostedService>();

var app = builder.Build();

// 配置开发环境的Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "飞书审批API v1");
        c.RoutePrefix = "api/docs"; // 设置Swagger UI的路径为 /api/docs
    });
}

// 映射控制器路由
app.MapControllers();

// 使用新的V2 Dashboard (基于模板系统)
FeishuDashboardTemplateExtensions.MapFeishuDashboardV2(app, new FeishuDashboardOptions());

app.Run();

