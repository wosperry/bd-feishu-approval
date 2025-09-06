using BD.FeishuApproval.Persistence;
using BD.FeishuApproval.Shared.Models;
using SqlSugar;

namespace BD.FeishuApproval.XUnit;

public class RepositoryInitTests
{
    [Fact]
    public async Task Initialize_CreatesTables()
    {
        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = "DataSource=:memory:",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true
        });
        var repo = new SqlSugarFeishuApprovalRepository(db);
        await repo.InitializeAsync();

        var hasRequest = db.DbMaintenance.IsAnyTable("FeishuRequestLog", false);
        var hasResponse = db.DbMaintenance.IsAnyTable("FeishuResponseLog", false);
        var hasFailed = db.DbMaintenance.IsAnyTable("FailedJob", false);
        Assert.True(hasRequest && hasResponse && hasFailed);
    }
}


