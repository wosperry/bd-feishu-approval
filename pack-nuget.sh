#!/bin/bash
# NuGet 打包脚本 (Linux/macOS)

echo "🚀 开始构建和打包 BD.FeishuSDK.Approval 系列包..."

# 清理之前的构建输出
echo "🧹 清理之前的构建输出..."
dotnet clean

# 恢复依赖包
echo "📦 恢复 NuGet 包依赖..."
dotnet restore

# 构建解决方案
echo "🔨 构建解决方案..."
dotnet build --configuration Release --no-restore

if [ $? -ne 0 ]; then
    echo "❌ 构建失败!"
    exit 1
fi

# 创建包输出目录
mkdir -p ./packages

# 打包各个项目
projects=(
    "src/BD.FeishuApproval.Shared/BD.FeishuApproval.Shared.csproj"
    "src/BD.FeishuApproval.Abstractions/BD.FeishuApproval.Abstractions.csproj" 
    "src/BD.FeishuApproval/BD.FeishuApproval.csproj"
)

echo "📦 开始打包 NuGet 包..."

for project in "${projects[@]}"; do
    echo "正在打包: $project"
    dotnet pack "$project" --configuration Release --no-build --output "./packages"
    
    if [ $? -ne 0 ]; then
        echo "❌ 打包失败: $project"
        exit 1
    fi
done

echo "✅ 所有包打包完成!"
echo "📁 包文件位于: ./packages 目录"

# 列出生成的包文件
echo ""
echo "📋 生成的包文件:"
ls -la ./packages/*.nupkg

echo ""
echo "🔧 发布到 NuGet 的命令示例:"
echo "dotnet nuget push ./packages/*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json"

echo ""
echo "🎉 打包完成!"