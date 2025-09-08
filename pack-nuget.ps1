# NuGet ��� + �����ű�
# PowerShell script to build, pack and publish all NuGet packages
# Usage: .\pack-nuget.ps1 -ApiKey "your-api-key" -Version "1.2.3"

param(
    [string]$ApiKey,  # ����ǿ�Ʊ�����Դӻ���������ȡ��
    [Parameter(Mandatory=$true)]
    [string]$Version
)

# ���û�� ApiKey�����Դӻ���������ȡ
if (-not $ApiKey) {
    $ApiKey = $env:NUGET_API_KEY
}

if (-not $ApiKey ) {
    Write-Host "? ����: δ�ṩ ApiKey��Ҳδ�ڻ������� NUGET_API_KEY ���ҵ�" -ForegroundColor Red 
}

Write-Host "? ��ʼ�����ʹ�� BD.FeishuSDK.Approval ϵ�а�..." -ForegroundColor Green

# ����֮ǰ�Ĺ������
Write-Host "? ����֮ǰ�Ĺ������..." -ForegroundColor Yellow
dotnet clean

# �ָ�������
Write-Host "? �ָ� NuGet ������..." -ForegroundColor Yellow
dotnet restore

# �����������
Write-Host "? �����������..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "? ����ʧ��!" -ForegroundColor Red 
}

# ���������Ŀ
$projects = @(
    "src\BD.FeishuApproval.Shared\BD.FeishuApproval.Shared.csproj",
    "src\BD.FeishuApproval.Abstractions\BD.FeishuApproval.Abstractions.csproj", 
    "src\BD.FeishuApproval\BD.FeishuApproval.csproj"
)

Write-Host "? ��ʼ��� NuGet �� (�汾��: $Version) ..." -ForegroundColor Yellow

foreach ($project in $projects) {
    Write-Host "���ڴ��: $project" -ForegroundColor Cyan
    dotnet pack $project --configuration Release --no-build --output "./packages" /p:PackageVersion=$Version
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? ���ʧ��: $project" -ForegroundColor Red
    }
}

Write-Host "? ���а�������!" -ForegroundColor Green
Write-Host "? ���ļ�λ��: ./packages Ŀ¼" -ForegroundColor Green

# �г����ɵİ��ļ�
Write-Host "`n? ���ɵİ��ļ�:" -ForegroundColor Yellow
Get-ChildItem "./packages" | Format-Table Name, Length, LastWriteTime

# �������а������� .nupkg �� .snupkg��
$packages = Get-ChildItem "./packages" | Where-Object { $_.Extension -in ".nupkg", ".snupkg" }

Write-Host "`n? ��ʼ������ NuGet..." -ForegroundColor Green
foreach ($package in $packages) {
    Write-Host "���ڷ���: $($package.Name)" -ForegroundColor Cyan
    
    dotnet nuget push "$($package.FullName)" `
        --source "https://api.nuget.org/v3/index.json" `
        --api-key "$ApiKey" `
        --skip-duplicate
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? ����ʧ��: $($package.Name)" -ForegroundColor Red 
    }
}

Write-Host "? ���а��������!" -ForegroundColor Green
