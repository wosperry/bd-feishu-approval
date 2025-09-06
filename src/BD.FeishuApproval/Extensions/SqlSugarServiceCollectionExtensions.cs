using System;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace BD.FeishuApproval.Extensions;

public static class SqlSugarServiceCollectionExtensions
{
	/// <summary>
	/// 使用给定的 ConnectionConfig 注册 ISqlSugarClient（若已注册则跳过）。
	/// </summary>
	public static IServiceCollection AddSqlSugar(this IServiceCollection services, ConnectionConfig connectionConfig)
	{
		if (services == null) throw new ArgumentNullException(nameof(services));
		if (connectionConfig == null) throw new ArgumentNullException(nameof(connectionConfig));

		// 只有在未注册时才注册，避免使用方自定义覆写失效
		var alreadyRegistered = false;
		foreach (var sd in services)
		{
			if (sd.ServiceType == typeof(ISqlSugarClient)) { alreadyRegistered = true; break; }
		}
		if (!alreadyRegistered)
		{
			services.AddSingleton<ISqlSugarClient>(_ => new SqlSugarClient(connectionConfig));
		}
		return services;
	}

	/// <summary>
	/// 以 MySQL 连接串注册 ISqlSugarClient，并接入飞书审批服务。
	/// </summary>
	/// <param name="services">DI 容器</param>
	/// <param name="connectionString">MySQL 连接串</param>
	/// <param name="baseAddress">飞书开放平台 BaseAddress，默认 https://open.feishu.cn</param>
	public static IServiceCollection AddFeishuApprovalWithMySql(this IServiceCollection services, string connectionString, Uri baseAddress = null)
	{
		if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string is required", nameof(connectionString));

		services.AddSqlSugar(new ConnectionConfig
		{
			ConnectionString = connectionString,
			DbType = DbType.MySql,
			IsAutoCloseConnection = true
		});

		services.AddFeishuApproval(baseAddress);
		return services;
	}

	/// <summary>
	/// 以 SQLite 连接串注册 ISqlSugarClient，并接入飞书审批服务。适用于开发和轻量化部署。
	/// </summary>
	/// <param name="services">DI 容器</param>
	/// <param name="connectionString">SQLite 连接串，如 "Data Source=./feishu.db"</param>
	/// <param name="baseAddress">飞书开放平台 BaseAddress，默认 https://open.feishu.cn</param>
	public static IServiceCollection AddFeishuApprovalWithSQLite(this IServiceCollection services, string connectionString, Uri baseAddress = null)
	{
		if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string is required", nameof(connectionString));

		services.AddSqlSugar(new ConnectionConfig
		{
			ConnectionString = connectionString,
			DbType = DbType.Sqlite,
			IsAutoCloseConnection = true
		});

		services.AddFeishuApproval(baseAddress);
		return services;
	}

	/// <summary>
	/// 以 SQL Server 连接串注册 ISqlSugarClient，并接入飞书审批服务。
	/// </summary>
	/// <param name="services">DI 容器</param>
	/// <param name="connectionString">SQL Server 连接串</param>
	/// <param name="baseAddress">飞书开放平台 BaseAddress，默认 https://open.feishu.cn</param>
	public static IServiceCollection AddFeishuApprovalWithSqlServer(this IServiceCollection services, string connectionString, Uri baseAddress = null)
	{
		if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string is required", nameof(connectionString));

		services.AddSqlSugar(new ConnectionConfig
		{
			ConnectionString = connectionString,
			DbType = DbType.SqlServer,
			IsAutoCloseConnection = true
		});

		services.AddFeishuApproval(baseAddress);
		return services;
	}

	/// <summary>
	/// 以 PostgreSQL 连接串注册 ISqlSugarClient，并接入飞书审批服务。
	/// </summary>
	/// <param name="services">DI 容器</param>
	/// <param name="connectionString">PostgreSQL 连接串</param>
	/// <param name="baseAddress">飞书开放平台 BaseAddress，默认 https://open.feishu.cn</param>
	public static IServiceCollection AddFeishuApprovalWithPostgreSql(this IServiceCollection services, string connectionString, Uri baseAddress = null)
	{
		if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string is required", nameof(connectionString));

		services.AddSqlSugar(new ConnectionConfig
		{
			ConnectionString = connectionString,
			DbType = DbType.PostgreSQL,
			IsAutoCloseConnection = true
		});

		services.AddFeishuApproval(baseAddress);
		return services;
	}

	/// <summary>
	/// 使用内存SQLite数据库进行快速开发和测试。注意：应用重启后数据会丢失。
	/// </summary>
	/// <param name="services">DI 容器</param>
	/// <param name="baseAddress">飞书开放平台 BaseAddress，默认 https://open.feishu.cn</param>
	public static IServiceCollection AddFeishuApprovalWithInMemorySQLite(this IServiceCollection services, Uri baseAddress = null)
	{
		services.AddSqlSugar(new ConnectionConfig
		{
			ConnectionString = "Data Source=:memory:",
			DbType = DbType.Sqlite,
			IsAutoCloseConnection = true
		});

		services.AddFeishuApproval(baseAddress);
		return services;
	}

	/// <summary>
	/// 使用指定的数据库类型注册 ISqlSugarClient，并接入飞书审批服务。
	/// </summary>
	/// <param name="services">DI 容器</param>
	/// <param name="connectionString">数据库连接字符串</param>
	/// <param name="databaseType">数据库类型</param>
	/// <param name="baseAddress">飞书开放平台 BaseAddress，默认 https://open.feishu.cn</param>
	public static IServiceCollection AddFeishuApproval(this IServiceCollection services, string connectionString, string databaseType, Uri baseAddress = null)
	{
		if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string is required", nameof(connectionString));
		if (string.IsNullOrWhiteSpace(databaseType)) throw new ArgumentException("Database type is required", nameof(databaseType));

		var dbType = ParseDatabaseType(databaseType);
		
		services.AddSqlSugar(new ConnectionConfig
		{
			ConnectionString = connectionString,
			DbType = dbType,
			IsAutoCloseConnection = true
		});

		services.AddFeishuApproval(baseAddress);
		return services;
	}

	/// <summary>
	/// 解析数据库类型字符串
	/// </summary>
	/// <param name="databaseType">数据库类型字符串</param>
	/// <returns>SqlSugar的DbType</returns>
	/// <exception cref="ArgumentException">不支持的数据库类型</exception>
	private static DbType ParseDatabaseType(string databaseType)
	{
		return databaseType.ToLowerInvariant() switch
		{
			"mysql" => DbType.MySql,
			"sqlserver" or "sql-server" or "mssql" => DbType.SqlServer,
			"postgresql" or "postgres" or "pgsql" => DbType.PostgreSQL,
			"sqlite" => DbType.Sqlite,
			"oracle" => DbType.Oracle,
			"clickhouse" => DbType.ClickHouse,
			"dm" or "dameng" => DbType.Dm,
			"kdbndp" => DbType.Kdbndp,
			"oscar" => DbType.Oscar,
			"mysqlconnector" => DbType.MySqlConnector,
			"access" => DbType.Access,
			"openguass" => DbType.OpenGauss,
			"questtdb" => DbType.QuestDB,
			"hg" => DbType.HG,
			"custom" => DbType.Custom,
			_ => throw new ArgumentException($"不支持的数据库类型: {databaseType}。支持的类型: mysql, sqlserver, postgresql, sqlite, oracle, clickhouse, dm, kdbndp, oscar, mysqlconnector, access, openguass, questtdb, hg, custom", nameof(databaseType))
		};
	}
}


