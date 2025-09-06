# 📋 NuGet 发布检查清单

## ✅ 发布前必检项目

### 🔍 **安全检查**
- [x] 移除硬编码的数据库连接字符串
- [x] 配置文件使用环境变量或配置系统
- [x] 所有敏感信息通过安全方式传输
- [x] 密码使用哈希存储（SHA256）
- [x] SQL注入防护（ORM参数化查询）

### 📦 **包配置检查**
- [x] 所有项目已配置正确的 NuGet 元数据
- [x] 版本号统一设置为 1.0.0
- [x] 包描述、标签、许可证信息完整
- [x] README.md 包含在主包中
- [x] MIT 许可证文件存在

### 🔨 **构建验证**
- [x] 项目可以成功构建（`dotnet build`）
- [x] 所有测试通过（如有）
- [x] 文档生成无警告
- [x] 多目标框架支持正确

### 📚 **文档完整性**
- [x] README.md 内容详细完整
- [x] 快速开始指南清晰易懂
- [x] API 文档和示例代码准确
- [x] 安全最佳实践说明完备
- [x] 升级指南和故障排除信息

## 🚀 发布步骤

### 1. 预发布验证
```bash
# 构建并打包
./pack-nuget.ps1  # Windows
# 或
./pack-nuget.sh   # Linux/macOS

# 检查生成的包
ls ./packages/
```

### 2. 测试包安装
```bash
# 在测试项目中安装包
dotnet add package BD.FeishuSDK.Approval --source ./packages
dotnet build
dotnet run
```

### 3. 正式发布
```bash
# 发布到 NuGet.org
dotnet nuget push ./packages/*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

## 🔧 发布后任务

### 📈 监控和维护
- [ ] 监控包下载统计
- [ ] 关注用户反馈和问题报告
- [ ] 定期更新依赖包版本
- [ ] 维护文档和示例代码

### 📢 推广和支持
- [ ] 在 GitHub 创建 Release
- [ ] 更新项目 Wiki 和文档网站
- [ ] 社区分享和技术博客
- [ ] 设置问题追踪和支持渠道

## ⚠️ 重要提醒

1. **包名唯一性**：确保 `BD.FeishuSDK.Approval` 在 NuGet.org 上未被占用
2. **GitHub 仓库**：更新 csproj 中的仓库 URL 为实际地址
3. **API Key 安全**：NuGet API Key 不要泄露，发布后及时回收
4. **版本管理**：后续版本遵循语义化版本规则（SemVer）

## 📋 包信息概览

| 包名 | 描述 | 依赖 |
|------|------|------|
| `BD.FeishuSDK.Approval` | 主包，完整功能 | Abstractions + Shared |
| `BD.FeishuSDK.Approval.Abstractions` | 抽象接口定义 | Shared |
| `BD.FeishuSDK.Approval.Shared` | 共享模型和DTO | 无 |

## 🎯 发布目标

- **易用性**：一行代码集成飞书审批功能
- **企业级**：生产环境可用的稳定性和安全性
- **可扩展**：丰富的扩展点和自定义选项
- **多数据库**：支持主流数据库系统
- **零依赖冲突**：最小化外部依赖，避免版本冲突

---

**✅ 所有检查项目完成后，即可进行正式发布！**