# NuGet 打包 + 发布脚本
# PowerShell script to build, pack and publish all NuGet packages
# Usage: .\pack-nuget.ps1 -ApiKey "your-api-key" -Version "1.2.3"

param(
    [string]$ApiKey,  # 不再强制必填（可以从环境变量读取）
    [Parameter(Mandatory=$true)]
    [string]$Version
)

# 如果没传 ApiKey，则尝试从环境变量读取
if (-not $ApiKey) {
    $ApiKey = $env:NUGET_API_KEY
}

if (-not $ApiKey ) {
    Write-Host "? 错误: 未提供 ApiKey，也未在环境变量 NUGET_API_KEY 中找到" -ForegroundColor Red 
}

Write-Host "? 开始构建和打包 BD.FeishuSDK.Approval 系列包..." -ForegroundColor Green

# 清理之前的构建输出
Write-Host "? 清理之前的构建输出..." -ForegroundColor Yellow
dotnet clean

# 恢复依赖包
Write-Host "? 恢复 NuGet 包依赖..." -ForegroundColor Yellow
dotnet restore

# 构建解决方案
Write-Host "? 构建解决方案..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "? 构建失败!" -ForegroundColor Red 
}

# 打包各个项目
$projects = @(
    "src\BD.FeishuApproval.Shared\BD.FeishuApproval.Shared.csproj",
    "src\BD.FeishuApproval.Abstractions\BD.FeishuApproval.Abstractions.csproj", 
    "src\BD.FeishuApproval\BD.FeishuApproval.csproj"
)

Write-Host "? 开始打包 NuGet 包 (版本号: $Version) ..." -ForegroundColor Yellow

foreach ($project in $projects) {
    Write-Host "正在打包: $project" -ForegroundColor Cyan
    dotnet pack $project --configuration Release --no-build --output "./packages" /p:PackageVersion=$Version
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? 打包失败: $project" -ForegroundColor Red
    }
}

Write-Host "? 所有包打包完成!" -ForegroundColor Green
Write-Host "? 包文件位于: ./packages 目录" -ForegroundColor Green

# 列出生成的包文件
Write-Host "`n? 生成的包文件:" -ForegroundColor Yellow
Get-ChildItem "./packages" | Format-Table Name, Length, LastWriteTime

# 推送所有包（包含 .nupkg 和 .snupkg）
$packages = Get-ChildItem "./packages" | Where-Object { $_.Extension -in ".nupkg", ".snupkg" }

Write-Host "`n? 开始发布到 NuGet..." -ForegroundColor Green
foreach ($package in $packages) {
    Write-Host "正在发布: $($package.Name)" -ForegroundColor Cyan
    
    dotnet nuget push "$($package.FullName)" `
        --source "https://api.nuget.org/v3/index.json" `
        --api-key "$ApiKey" `
        --skip-duplicate
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? 发布失败: $($package.Name)" -ForegroundColor Red 
    }
}

Write-Host "? 所有包发布完成!" -ForegroundColor Green
