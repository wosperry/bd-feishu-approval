using BD.FeishuApproval.Abstractions.Persistence;
using BD.FeishuApproval.Shared.Models;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BD.FeishuApproval.Persistence;

public class SqlSugarFeishuApprovalRepository : IFeishuApprovalRepository
{
    private readonly ISqlSugarClient _db;

    public SqlSugarFeishuApprovalRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var tablesToCreate = new[]
        {
            (Type: typeof(FeishuRequestLog), TableName: "FeishuRequestLog"),
            (Type: typeof(FeishuResponseLog), TableName: "FeishuResponseLog"),
            (Type: typeof(FailedJob), TableName: "FailedJob"),
            (Type: typeof(FeishuAccessConfig), TableName: "FeishuAccessConfig"),
            (Type: typeof(FeishuAdminAccessKey), TableName: "FeishuAdminAccessKey"),
            (Type: typeof(FeishuApprovalRegistration), TableName: "FeishuApprovalRegistration"),
            (Type: typeof(FeishuManageLog), TableName: "FeishuManageLog"),
            (Type: typeof(FeishuUser), TableName: "FeishuUser"),
            (Type: typeof(FeishuOpenIdCache), TableName: "FeishuOpenIdCache"),
            (Type: typeof(FeishuCallbackRecord), TableName: "FeishuCallbackRecord")
        };

        var codeFirst = _db.CodeFirst.SetStringDefaultLength(4000);
        
        foreach (var table in tablesToCreate)
        {
            if (!_db.DbMaintenance.IsAnyTable(table.TableName))
            {
                codeFirst.InitTables(table.Type);
            }
        }

        // 检查是否需要创建默认用户
        var userCount = await _db.Queryable<FeishuUser>().CountAsync();
        if (userCount == 0)
        {
            // 创建默认测试用户
            await _db.Insertable(new FeishuUser
            {
                Id = 1,
                Name = "测试用户",
                Age = 25,
                Phone = "13800138000",  // 默认手机号
                FeishuOpenId = "" // 将在第一次调用时通过飞书API获取
            }).ExecuteCommandAsync();
        }
        
        await Task.CompletedTask;
    }

    public async Task DropAndRecreateTablesAsync(CancellationToken cancellationToken = default)
    {
        var tablesToRecreate = new[]
        {
            (Type: typeof(FeishuRequestLog), TableName: "FeishuRequestLog"),
            (Type: typeof(FeishuResponseLog), TableName: "FeishuResponseLog"),
            (Type: typeof(FailedJob), TableName: "FailedJob"),
            (Type: typeof(FeishuAccessConfig), TableName: "FeishuAccessConfig"),
            (Type: typeof(FeishuAdminAccessKey), TableName: "FeishuAdminAccessKey"),
            (Type: typeof(FeishuApprovalRegistration), TableName: "FeishuApprovalRegistration"),
            (Type: typeof(FeishuManageLog), TableName: "FeishuManageLog"),
            (Type: typeof(FeishuUser), TableName: "FeishuUser"),
            (Type: typeof(FeishuCallbackRecord), TableName: "FeishuCallbackRecord")
        };

        // Backup critical configuration data
        FeishuAccessConfig accessConfigBackup = null;
        List<FeishuAdminAccessKey> adminKeysBackup = new List<FeishuAdminAccessKey>();
        
        if (_db.DbMaintenance.IsAnyTable("FeishuAccessConfig"))
        {
            accessConfigBackup = await _db.Queryable<FeishuAccessConfig>().FirstAsync();
        }
        
        if (_db.DbMaintenance.IsAnyTable("FeishuAdminAccessKey"))
        {
            adminKeysBackup = await _db.Queryable<FeishuAdminAccessKey>().ToListAsync();
        }

        // Drop all existing tables in reverse order to handle dependencies
        var tablesToDrop = tablesToRecreate.Reverse().ToArray();
        foreach (var table in tablesToDrop)
        {
            try
            {
                if (_db.DbMaintenance.IsAnyTable(table.TableName))
                {
                    _db.DbMaintenance.DropTable(table.TableName);
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue with other tables
                System.Diagnostics.Debug.WriteLine($"Failed to drop table {table.TableName}: {ex.Message}");
            }
        }

        // Wait a moment for database to process the drops
        await Task.Delay(500);

        // Recreate all tables with proper schema
        var codeFirst = _db.CodeFirst
            .SetStringDefaultLength(4000);
            
        foreach (var table in tablesToRecreate)
        {
            try
            {
                // Force recreation with primary key configuration
                _db.CodeFirst.InitTables(table.Type);
            }
            catch (Exception ex)
            {
                // Log the error but continue with other tables
                System.Diagnostics.Debug.WriteLine($"Failed to create table {table.TableName}: {ex.Message}");
                throw; // Re-throw for critical table creation failures
            }
        }

        // Restore backed up data (don't set ID, let auto-increment handle it)
        if (accessConfigBackup != null)
        {
            var newConfig = new FeishuAccessConfig
            {
                AppId = accessConfigBackup.AppId,
                AppSecret = accessConfigBackup.AppSecret,
                EncryptKey = accessConfigBackup.EncryptKey,
                VerificationToken = accessConfigBackup.VerificationToken,
                CreatedAt = accessConfigBackup.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };
            await _db.Insertable(newConfig).ExecuteCommandAsync();
        }
        
        foreach (var adminKey in adminKeysBackup)
        {
            var newKey = new FeishuAdminAccessKey
            {
                PlainPassword = adminKey.PlainPassword,
                CreatedAt = adminKey.CreatedAt
            };
            await _db.Insertable(newKey).ExecuteCommandAsync();
        }
        
        await Task.CompletedTask;
    }

    public Task SaveRequestLogAsync(FeishuRequestLog log, CancellationToken cancellationToken = default)
        => _db.Insertable(log).ExecuteCommandAsync();

    public Task SaveResponseLogAsync(FeishuResponseLog log, CancellationToken cancellationToken = default)
        => _db.Insertable(log).ExecuteCommandAsync();

    public async Task<int> AddFailedJobAsync(FailedJob job, CancellationToken cancellationToken = default)
    {
        var id = await _db.Insertable(job).ExecuteReturnIdentityAsync();
        return id;
    }

    public Task MarkFailedJobSucceededAsync(int jobId, CancellationToken cancellationToken = default)
        => _db.Updateable<FailedJob>()
            .SetColumns(j => new FailedJob { IsResolved = true })
            .Where(j => j.Id == jobId)
            .ExecuteCommandAsync();

    public Task<IEnumerable<FailedJob>> QueryFailedJobsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => _db.Queryable<FailedJob>()
            .OrderBy(j => j.CreatedAt, OrderByType.Desc)
            .ToPageListAsync(page, pageSize)
            .ContinueWith<IEnumerable<FailedJob>>(t => t.Result);

    public Task SaveEventAsync(Shared.Events.ApprovalStatusChangedEvent evt, CancellationToken cancellationToken = default)
        => _db.Insertable(evt).ExecuteCommandAsync();

    public async Task<(IEnumerable<FeishuRequestLog> Items, int Total)> QueryRequestLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        RefAsync<int> total = 0;
        var list = await _db.Queryable<FeishuRequestLog>()
            .OrderBy(x => x.CreatedAt, OrderByType.Desc)
            .ToPageListAsync(page, pageSize, total);
        return (list, total);
    }

    public async Task<(IEnumerable<FeishuResponseLog> Items, int Total)> QueryResponseLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        RefAsync<int> total = 0;
        var list = await _db.Queryable<FeishuResponseLog>()
            .OrderBy(x => x.CreatedAt, OrderByType.Desc)
            .ToPageListAsync(page, pageSize, total);
        return (list, total);
    }

    public async Task<FeishuAccessConfig> GetAccessConfigAsync(CancellationToken cancellationToken = default)
        => await _db.Queryable<FeishuAccessConfig>().FirstAsync() ?? new FeishuAccessConfig();

    public async Task<FeishuUser> GetUserAsync(int userId)
    {
        var user = await _db.Queryable<FeishuUser>()
            .Where(t => t.Id == userId)
            .FirstAsync();

        return user;
    }

    public async Task UpsertAccessConfigAsync(FeishuAccessConfig config, CancellationToken cancellationToken = default)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        config.UpdatedAt = DateTime.UtcNow;
        var exists = await _db.Queryable<FeishuAccessConfig>().FirstAsync();
        if (exists == null)
        {
            config.CreatedAt = DateTime.UtcNow;
            await _db.Insertable(config).ExecuteCommandAsync();
        }
        else
        {
            config.Id = exists.Id;
            await _db.Updateable(config).ExecuteCommandAsync();
        }
    }

    private static void HashPassword(string password, out string hash, out string salt)
    {
        var saltBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        salt = Convert.ToBase64String(saltBytes);
        using var derive = new Rfc2898DeriveBytes(password, saltBytes, 100_000, HashAlgorithmName.SHA256);
        hash = Convert.ToBase64String(derive.GetBytes(32));
    }

    private static bool VerifyPassword(string password, string hash, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        using var derive = new Rfc2898DeriveBytes(password, saltBytes, 100_000, HashAlgorithmName.SHA256);
        var computed = Convert.ToBase64String(derive.GetBytes(32));
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(computed), Encoding.UTF8.GetBytes(hash));
    }

    public async Task SetAdminPasswordAsync(string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("password required", nameof(password));
        
        // 使用安全的PBKDF2哈希
        HashPassword(password, out var hash, out var salt);
        var hashedPassword = salt + ":" + hash;
        
        var entity = new FeishuAdminAccessKey { PlainPassword = hashedPassword };
        
        await _db.Deleteable<FeishuAdminAccessKey>().Where(_ => true).ExecuteCommandAsync();
        await _db.Insertable(entity).ExecuteCommandAsync();
    }

    public async Task<bool> VerifyAdminPasswordAsync(string password, CancellationToken cancellationToken = default)
    {
        try
        {
            // 开发环境调试日志
            Console.WriteLine($"[DEBUG] 开始验证管理密码，输入密码长度: {password?.Length ?? 0}");
            
            var key = await _db.Queryable<FeishuAdminAccessKey>().OrderBy(x => x.Id, OrderByType.Desc).FirstAsync();
            
            if (key == null) 
            {
                Console.WriteLine("[DEBUG] 数据库中未找到管理密钥记录");
                return false;
            }
            
            Console.WriteLine($"[DEBUG] 数据库中的密钥ID: {key.Id}");
            Console.WriteLine($"[DEBUG] 数据库密钥长度: {key.PlainPassword?.Length ?? 0}");
            Console.WriteLine($"[DEBUG] 数据库密钥前10字符: {(string.IsNullOrEmpty(key.PlainPassword) ? "null" : key.PlainPassword.Substring(0, Math.Min(10, key.PlainPassword.Length)))}");
            Console.WriteLine($"[DEBUG] 输入密码前10字符: {(string.IsNullOrEmpty(password) ? "null" : password.Substring(0, Math.Min(10, password.Length)))}");
            
            // 兼容旧的明文密码，同时支持新的哈希密码
            if (!string.IsNullOrEmpty(key.PlainPassword) && key.PlainPassword.Contains(":"))
            {
                Console.WriteLine("[DEBUG] 检测到哈希格式密码");
                // 新的哈希格式 (salt:hash)
                var parts = key.PlainPassword.Split(':', 2);
                if (parts.Length == 2)
                {
                    var result = VerifyPassword(password, parts[1], parts[0]);
                    Console.WriteLine($"[DEBUG] 哈希密码验证结果: {result}");
                    return result;
                }
            }
            
            // 旧的明文格式（为了向后兼容）
            var plainResult = password == key.PlainPassword;
            Console.WriteLine($"[DEBUG] 明文密码验证结果: {plainResult}");
            Console.WriteLine($"[DEBUG] 密码完全匹配: {password == key.PlainPassword}");
            
            return plainResult;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] 密码验证异常: {ex.Message}");
            return false;
        }
    }

    public async Task<int> RegisterApprovalAsync(FeishuApprovalRegistration reg, CancellationToken cancellationToken = default)
    {
        if (reg == null) throw new ArgumentNullException(nameof(reg));
        var id = await _db.Insertable(reg).ExecuteReturnIdentityAsync();
        return id;
    }

    public Task<IEnumerable<FeishuApprovalRegistration>> ListApprovalsAsync(CancellationToken cancellationToken = default)
        => _db.Queryable<FeishuApprovalRegistration>().ToListAsync().ContinueWith<IEnumerable<FeishuApprovalRegistration>>(t => t.Result);

    public async Task SaveManageLogAsync(FeishuManageLog log, CancellationToken cancellationToken = default)
    {
        if (log == null) throw new ArgumentNullException(nameof(log));
        await _db.Insertable(log).ExecuteCommandAsync();
    }

    public async Task<IEnumerable<FeishuManageLog>> QueryManageLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var logs = await _db.Queryable<FeishuManageLog>()
            .OrderBy(x => x.CreatedAt, OrderByType.Desc)
            .ToPageListAsync(page, pageSize);
        return logs;
    }

    public async Task UpdateUserOpenIdAsync(int id, string openId)
    {
        await _db.Updateable<FeishuUser>()
            .SetColumns(j => new FeishuUser { FeishuOpenId = openId })
            .Where(j => j.Id == id)
            .ExecuteCommandAsync();
    }

    // OpenId缓存管理方法实现
    public async Task<string> GetOpenIdByMobileAsync(string mobile)
    {
        var cache = await _db.Queryable<FeishuOpenIdCache>()
            .FirstAsync(x => x.UserIdType == "mobile" && x.UserIdValue == mobile);
        return cache?.OpenId ?? string.Empty;
    }

    public async Task<string> GetOpenIdByEmailAsync(string email)
    {
        var cache = await _db.Queryable<FeishuOpenIdCache>()
            .FirstAsync(x => x.UserIdType == "email" && x.UserIdValue == email);
        return cache?.OpenId ?? string.Empty;
    }

    public async Task<string> GetOpenIdByUnionIdAsync(string unionId)
    {
        var cache = await _db.Queryable<FeishuOpenIdCache>()
            .FirstAsync(x => x.UserIdType == "union_id" && x.UserIdValue == unionId);
        return cache?.OpenId ?? string.Empty;
    }

    public async Task<string> GetOpenIdByUserIdAsync(string userId)
    {
        var cache = await _db.Queryable<FeishuOpenIdCache>()
            .FirstAsync(x => x.UserIdType == "user_id" && x.UserIdValue == userId);
        return cache?.OpenId ?? string.Empty;
    }

    public async Task CacheOpenIdByMobileAsync(string mobile, string openId)
    {
        await UpsertOpenIdCacheAsync("mobile", mobile, openId);
    }

    public async Task CacheOpenIdByEmailAsync(string email, string openId)
    {
        await UpsertOpenIdCacheAsync("email", email, openId);
    }

    public async Task CacheOpenIdByUnionIdAsync(string unionId, string openId)
    {
        await UpsertOpenIdCacheAsync("union_id", unionId, openId);
    }

    public async Task CacheOpenIdByUserIdAsync(string userId, string openId)
    {
        await UpsertOpenIdCacheAsync("user_id", userId, openId);
    }

    public async Task ClearOpenIdCacheByMobileAsync(string mobile)
    {
        await _db.Deleteable<FeishuOpenIdCache>()
            .Where(x => x.UserIdType == "mobile" && x.UserIdValue == mobile)
            .ExecuteCommandAsync();
    }

    public async Task ClearOpenIdCacheByEmailAsync(string email)
    {
        await _db.Deleteable<FeishuOpenIdCache>()
            .Where(x => x.UserIdType == "email" && x.UserIdValue == email)
            .ExecuteCommandAsync();
    }

    public async Task ClearOpenIdCacheByUnionIdAsync(string unionId)
    {
        await _db.Deleteable<FeishuOpenIdCache>()
            .Where(x => x.UserIdType == "union_id" && x.UserIdValue == unionId)
            .ExecuteCommandAsync();
    }

    public async Task ClearOpenIdCacheByUserIdAsync(string userId)
    {
        await _db.Deleteable<FeishuOpenIdCache>()
            .Where(x => x.UserIdType == "user_id" && x.UserIdValue == userId)
            .ExecuteCommandAsync();
    }

    private async Task UpsertOpenIdCacheAsync(string userIdType, string userIdValue, string openId)
    {
        var existing = await _db.Queryable<FeishuOpenIdCache>()
            .FirstAsync(x => x.UserIdType == userIdType && x.UserIdValue == userIdValue);

        if (existing != null)
        {
            existing.OpenId = openId;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.Updateable(existing).ExecuteCommandAsync();
        }
        else
        {
            var newCache = new FeishuOpenIdCache
            {
                UserIdType = userIdType,
                UserIdValue = userIdValue,
                OpenId = openId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _db.Insertable(newCache).ExecuteCommandAsync();
        }
    }

    // 回调记录管理方法
    public async Task<int> SaveCallbackRecordAsync(FeishuCallbackRecord record, CancellationToken cancellationToken = default)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        record.CreatedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        var id = await _db.Insertable(record).ExecuteReturnIdentityAsync();
        return id;
    }

    public async Task UpdateCallbackRecordStatusAsync(int recordId, string status, string message = null, CancellationToken cancellationToken = default)
    {
        var updateData = new FeishuCallbackRecord 
        { 
            ProcessingStatus = status,
            UpdatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(message))
        {
            updateData.ProcessingMessage = message;
        }

        if (status == CallbackProcessingStatus.Processing && updateData.ProcessingStartedAt == null)
        {
            updateData.ProcessingStartedAt = DateTime.UtcNow;
        }
        else if (status == CallbackProcessingStatus.Completed || status == CallbackProcessingStatus.Failed)
        {
            updateData.ProcessingCompletedAt = DateTime.UtcNow;
        }

        await _db.Updateable(updateData)
            .Where(r => r.Id == recordId)
            .ExecuteCommandAsync();
    }

    public async Task<FeishuCallbackRecord> GetCallbackRecordByEventIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<FeishuCallbackRecord>()
            .Where(r => r.EventId == eventId)
            .FirstAsync();
    }

    public async Task<IEnumerable<FeishuCallbackRecord>> GetPendingCallbackRecordsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Queryable<FeishuCallbackRecord>()
            .Where(r => r.ProcessingStatus == CallbackProcessingStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<FeishuCallbackRecord>> QueryCallbackRecordsAsync(int page, int pageSize, string status = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Queryable<FeishuCallbackRecord>();
        
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.ProcessingStatus == status);
        }

        return await query
            .OrderBy(r => r.CreatedAt, OrderByType.Desc)
            .ToPageListAsync(page, pageSize);
    }

    public async Task IncrementCallbackRetryCountAsync(int recordId, CancellationToken cancellationToken = default)
    {
        await _db.Updateable<FeishuCallbackRecord>()
            .SetColumns(r => new FeishuCallbackRecord 
            { 
                RetryCount = r.RetryCount + 1,
                LastRetryAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .Where(r => r.Id == recordId)
            .ExecuteCommandAsync();
    }

    public async Task<string> GetApprovalTypeByInstanceCodeAsync(string instanceCode, CancellationToken cancellationToken = default)
    {
        var record = await _db.Queryable<FeishuCallbackRecord>()
            .Where(r => r.InstanceCode == instanceCode)
            .OrderBy(r => r.CreatedAt, OrderByType.Desc)
            .FirstAsync();
        
        return record?.ApprovalCode ?? string.Empty;
    }
}



