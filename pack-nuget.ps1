# NuGet 打包脚本
# PowerShell script to build and pack all NuGet packages

Write-Host "🚀 开始构建和打包 BD.FeishuSDK.Approval 系列包..." -ForegroundColor Green

# 清理之前的构建输出
Write-Host "🧹 清理之前的构建输出..." -ForegroundColor Yellow
dotnet clean

# 恢复依赖包
Write-Host "📦 恢复 NuGet 包依赖..." -ForegroundColor Yellow
dotnet restore

# 构建解决方案
Write-Host "🔨 构建解决方案..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 构建失败!" -ForegroundColor Red
    exit 1
}

# 打包各个项目
$projects = @(
    "src\BD.FeishuApproval.Shared\BD.FeishuApproval.Shared.csproj",
    "src\BD.FeishuApproval.Abstractions\BD.FeishuApproval.Abstractions.csproj", 
    "src\BD.FeishuApproval\BD.FeishuApproval.csproj"
)

Write-Host "📦 开始打包 NuGet 包..." -ForegroundColor Yellow

foreach ($project in $projects) {
    Write-Host "正在打包: $project" -ForegroundColor Cyan
    dotnet pack $project --configuration Release --no-build --output "./packages"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ 打包失败: $project" -ForegroundColor Red
        exit 1
    }
}

Write-Host "✅ 所有包打包完成!" -ForegroundColor Green
Write-Host "📁 包文件位于: ./packages 目录" -ForegroundColor Green

# 列出生成的包文件
Write-Host "`n📋 生成的包文件:" -ForegroundColor Yellow
Get-ChildItem "./packages" -Filter "*.nupkg" | Format-Table Name, Length, LastWriteTime

Write-Host "`n🔧 发布到 NuGet 的命令示例:" -ForegroundColor Cyan
Write-Host "dotnet nuget push ./packages/*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json" -ForegroundColor White

Write-Host "`n🎉 打包完成!" -ForegroundColor Green