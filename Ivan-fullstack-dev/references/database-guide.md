# 数据库配置指南（SQLite / MSSQL / MySQL）

通过 `appsettings.json` 的 `Database:Provider` 切换数据库，业务代码无需改动。

## appsettings.json 配置模板

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=app.db"
  },
  "Database": {
    "Provider": "sqlite"
  },
  "Jwt": {
    "Key": "your-32+char-secret-key-here",
    "Issuer": "IvanProject",
    "Audience": "IvanProjectClient"
  }
}
```

## SQLite（默认）

- 连接串：`Data Source=app.db`（相对路径，文件位于运行目录）。
- NuGet 包：`Microsoft.EntityFrameworkCore.Sqlite`。
- 优点：零安装、随项目走、适合开发与小型部署。
- 注意：IIS 发布时确保站点物理路径对应的目录对 IIS 应用池账户可写（`IIS_IUSRS`）。

## MSSQL

- 连接串示例：

```
Server=localhost;Database=IvanProject;User Id=sa;Password=your_password;TrustServerCertificate=True;
```

- NuGet 包：`Microsoft.EntityFrameworkCore.SqlServer`。
- 本地开发常用 `Server=localhost` 或 `Server=.\\SQLEXPRESS`。
- IIS 部署时若使用 Windows 集成认证，连接串可用 `Integrated Security=True`，并确保应用池账户有数据库访问权限。

## MySQL

- 连接串示例：

```
Server=localhost;Port=3306;Database=IvanProject;User=root;Password=your_password;CharSet=utf8mb4;
```

- NuGet 包：`MySql.EntityFrameworkCore`（注意与 EF Core 6 的版本兼容）。
- 库需提前创建，字符集建议 `utf8mb4`。

## 表结构初始化

两种方式：

1. **开发快速起步**：`db.Database.EnsureCreated()` —— 首次启动自动建库建表，适合原型与内网工具。
2. **正式演进**：EF Migration —— `dotnet ef migrations add Init` / `dotnet ef database update`，可跟踪结构变更。

选择 Migration 时需安装 `dotnet-ef` 工具：`dotnet tool install --global dotnet-ef`。

## 注意事项

- 不要在生产连接字符串中硬编码密码；IIS 场景可改用环境变量或 Web.config 的 `appSettings` 覆盖。
- 所有 Provider 共用一个 `Default` 连接串配置，切换时只需改 `Database:Provider` 与连接串本身。
