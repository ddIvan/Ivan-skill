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

## 一体化 SQL 初始化脚本（MSSQL）

模板 `assets/database/init_database.sql` 将全部表结构与种子数据合并为**单个幂等脚本**（重复执行不产生脏数据），覆盖：

- Users / Roles / Menus（统一权限树：MenuType 1=目录 2=菜单 3=按钮，层次结构内建 ParentId + FullPath + Level，按钮节点携带 PermissionCode + ControllerAction）/ RoleMenus（唯一授权关联）/ UserRoles
- 种子数据：admin 账户（admin123，PBKDF2 哈希）、默认角色、18 节点菜单树（系统管理目录 + 首页/用户/角色/菜单 4 菜单 + 13 按钮节点，PermissionCode 与 Controller 内 `[RequirePerm]` 声明一致）、admin↔admin 角色绑定、admin 角色全量授权
- 种子不硬编码 Id，按 UserName/Code/Path/名称锚定变量关联插入；菜单 FullPath 最后统一递归计算

执行方式（含中文脚本必须 `-f 65001`，否则中文乱码/插入失败）：

```bash
sqlcmd -S tcp:127.0.0.1 -U sa -P <密码> -d <数据库名> -i init_database.sql -f 65001
```

执行完成后脚本末尾自带验证查询（菜单动作配置、用户-角色绑定）。

## 默认账户与密码哈希（种子数据规范）

启用登录认证的项目，初始化脚本需要内置默认管理员账户（admin / admin123）。密码哈希必须与 `AuthService` 的算法一致：

- **算法**：PBKDF2，迭代 10000 次，SHA256，盐 16 字节，哈希 32 字节。
- **存储格式**：`base64(salt).base64(hash)`，例如 `AQIDBAUGBwgJCgsMDQ4PEA==.SJ34IaoONdCI30nVhP7h41dba31Qp+CGBYhy6bY72To=`（即 admin123 的 PBKDF2 哈希）。
- **注意**：不要使用 BCrypt 等其他格式（如 `$2a$` 开头）。`AuthService.VerifyPassword` 按 `.` 拆分为两段校验，其他格式会导致登录必然失败（提示"用户名或密码错误"）。

种子脚本示例：

```sql
INSERT INTO Users (UserName, PasswordHash, DisplayName, CreateTime, IsEnabled)
VALUES ('admin', 'AQIDBAUGBwgJCgsMDQ4PEA==.SJ34IaoONdCI30nVhP7h41dba31Qp+CGBYhy6bY72To=', N'系统管理员', GETDATE(), 1);
```

若需修改默认密码，用与 `AuthService.HashPassword` 相同算法重新生成后再写入。

## 注意事项

- 不要在生产连接字符串中硬编码密码；IIS 场景可改用环境变量或 Web.config 的 `appSettings` 覆盖。
- 所有 Provider 共用一个 `Default` 连接串配置，切换时只需改 `Database:Provider` 与连接串本身。
