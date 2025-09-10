using BD.FeishuApproval.Extensions;
using BD.FeishuApproval.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddFeishuApproval("Data Source=app.db", "sqlite");


var app = builder.Build();

// 一行代码集成飞书审批

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapFeishuDashboardV2(options =>
{
    // TODO：这里的配置项没有测试
});

app.Run();
