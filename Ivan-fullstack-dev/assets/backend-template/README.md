# 后端模板使用说明（.NET 6.0 Web API + Ivan.Data 分层架构）

## 使用方式

1. 将 `backend-template/` 下的所有文件复制到项目 `backend/` 目录（**必须包含 `nuget.config`**）。
2. 将解决方案/程序集名从 `IvanProject` 改为实际项目名（csproj 文件名、`RootNamespace`、各文件 `namespace IvanProject.*`、`web.config` 中的 dll 名，以及各文件中所有 `using IvanProject.*` 引用；`Program.cs` 中 `strAssemblies` 数组里的程序集名也要同步修改）。
3. 安装依赖：

```bash
dotnet restore
```

> `nuget.config` 已配置私有源 `IvanNuGet`（http://61.169.209.58:19081/repository/nuget-hosted/，账号 apps / 密码 123456）。`Ivan.*` 等内部第三方库直接在 csproj 中添加 `<PackageReference>` 即可从此源还原；公共包走 nuget.org。

4. 按需修改 `appsettings.json`：
   - `Database:Provider`：`sqlite` / `mssql` / `mysql`
   - `ConnectionStrings:Default`：对应连接串
   - `Jwt:Key`：改为至少 32 字符的随机密钥

5. **提前建表**：本框架不自动建表，请先在数据库中执行建表 SQL（示例 `Users` 表），再启动。

6. 运行：

```bash
dotnet run        # 开发监听 https://localhost:53988;http://localhost:53989（见 launchSettings.json），Swagger: /swagger
```

## 分层结构

```
backend/
├── Models/            # 实体（Generate/ 自动生成 + partial 手动扩展，Users 为示例）
├── DAL/
│   ├── Interface/     # IUsersDAL（[InjectIOC] 标注）
│   └── UsersDAL.cs    # BaseDAL<Users> 实现 + partial 扩展
├── BLL/
│   ├── Interface/     # IUsersBLL（[InjectIOC] 标注）
│   ├── UsersBLL.cs    # BaseBLL<IUsersDAL, Users> 实现
│   ├── AuthService.cs        # 手动编写（注入 IUsersBLL，登录/JWT）
│   └── UsersBLL.cs  # 手动编写（封装 IUsersBLL 的 CRUD 示例）
├── DTOs/              # 入参/出参（LoginRequest / LoginResult / RegisterRequest）
├── Controllers/       # 薄控制器（AuthController / UserController 示例）
├── Common/            # ApiResult / BusinessException / ExceptionMiddleware
├── Program.cs         # DatabaseInfo 注册连接 → AddIvanIOC 扫描注册 → 手动服务 → 管道
├── nuget.config
└── appsettings.json
```

## 依赖注入机制（Ivan.IOC）

- `Program.cs` 调用 `builder.Host.AddIvanIOC(程序集数组, ...)`，自动扫描程序集中 `[InjectIOC]` 标注的接口（`IUsersBLL` / `IUsersDAL`），注册其实现类（`UsersBLL` / `UsersDAL`）。
- **手动编写的服务**（`AuthService` / `UsersBLL`）无 `[InjectIOC]` 接口，在 `Program.cs` 的 `services.AddScoped<...>()` 注册；其构造函数注入的 `IUsersBLL` 由容器自动解析。
- **禁止**再手动注册带 `[InjectIOC]` 接口的实现类（如 `services.AddScoped<UsersBLL>()`），会导致重复注册。

## 新增业务模块的标准步骤

1. **三层代码生成**：通过 ivan-db-model-gen → ivan-db-dal-gen → ivan-db-bll-gen 从数据库表生成 `Models/Generate/` + `DAL/` + `BLL/` 三层（接口带 `[InjectIOC]`，注册自动完成）。
2. **BLL 扩展**：自定义方法写进 `XxxBLL.cs` partial 类，同步在 `BLL/Interface/IXxxBLL.cs` 声明；实现里用 `Factory.OpenSession()` → `session.CreateDAL<IXxxDAL>()` 调 DAL。
3. **Service（可选）**：聚合多个 BLL 的业务写在手动 Service 中，`Program.cs` 注册 `services.AddScoped<XxxService>()`。
4. **Controller**：在 `Controllers/` 新建控制器，只做参数处理 + 调用 BLL/Service，返回 `ApiResult`。
5. **DTO（可选）**：接口入参出参较复杂时在 `DTOs/` 定义。

> 单项目分层下各文件必须手动写全跨层 using（BLL → Models/DAL(含 Interface)/BLL.Interface/Common；Controller → BLL/Common/Models；DAL → Models/DAL.Interface + Ivan.Data 框架命名空间），详见 `references/backend-guide.md` 的"跨层 using 引用规则"。

## 切换数据库

| 数据库 | Provider 值 | 连接串示例 |
|--------|-------------|-----------|
| SQLite | `sqlite` | `Data Source=app.db` |
| MSSQL | `mssql` | `Server=localhost;Database=IvanProject;User Id=sa;Password=xxx;TrustServerCertificate=True;` |
| MySQL | `mysql` | `Server=localhost;Port=3306;Database=IvanProject;User=root;Password=xxx;CharSet=utf8mb4;` |

同时需在 csproj 中替换对应数据库驱动包；`Program.cs` 中的 `DatabaseInfo.Set*Database` 分支按 Provider 自动选择，无需改动。

## IIS 发布

```bash
dotnet publish -c Release -o ./publish
```

将 `publish/` 复制到 IIS 站点物理路径，根目录放入 `assets/web.config`（把 `arguments` 改为实际 dll 名），应用池 .NET CLR 选"无托管代码"。详见 `references/iis-deploy-guide.md`。
