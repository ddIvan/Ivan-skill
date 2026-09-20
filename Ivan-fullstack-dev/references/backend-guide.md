# 后端开发规范（.NET 6.0 Web API + Ivan.Data 框架）

## 技术栈

- .NET 6.0（ASP.NET Core Web API）
- **Ivan.Data**（内部框架，提供 `BaseDAL` / `BaseBLL` / `IDataSessionFactory` / Dapper 封装）
- **Ivan.Data.IOC**（`[InjectIOC]` 特性 + `IOCInitExtensions`）
- **Ivan.IOC**（`AddIvanIOC` 启动扩展，Autofac 模式，扫描 `[InjectIOC]` 接口自动注册）
- 数据库：MSSQL（`System.Data.SqlClient`）/ MySQL / SQLite，由 `DatabaseInfo` 按连接串注册
- JWT Bearer 认证（可选）
- Swashbuckle（Swagger）

## NuGet 源配置（必须）

项目根目录必须包含 `nuget.config`（模板 `assets/backend-template/nuget.config` 已内置），内容如下：

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <!-- Ivan 私有 NuGet 源：Ivan.* 等内部第三方库从此处获取 -->
    <add key="IvanNuGet" value="http://61.169.209.58:19081/repository/nuget-hosted/" />
  </packageSources>
  <packageSourceCredentials>
    <IvanNuGet>
      <add key="Username" value="apps" />
      <add key="ClearTextPassword" value="123456" />
    </IvanNuGet>
  </packageSourceCredentials>
</configuration>
```

- **Ivan 开头的库**（`Ivan.Common`、`Ivan.Data`、`Ivan.Data.IOC`、`Ivan.IOC`、`Ivan.Log` 等）全部从 `IvanNuGet` 源还原；csproj 中直接写 `<PackageReference Include="Ivan.xxx" Version="x.y.z" />` 即可，无需指定 source。
- 若手动执行 `dotnet add package Ivan.xxx`，也必须保证在含 `nuget.config` 的目录下运行（子目录继承该配置）。
- 禁止删除 `<clear />` 后漏配 nuget.org，否则公共包将无法还原。
- 凭据为明文（`ClearTextPassword`），仅用于内网环境，不要提交到公共仓库。

## 核心包组合（csproj）

```xml
<ItemGroup>
  <PackageReference Include="Ivan.Common" Version="1.0.8" />
  <PackageReference Include="Ivan.Data" Version="1.0.12" />
  <PackageReference Include="Ivan.Data.IOC" Version="1.0.9" />
  <PackageReference Include="Ivan.IOC" Version="1.0.5" />
  <!-- MSSQL 数据库驱动；MySQL/SQLite 项目替换为对应驱动包 -->
  <PackageReference Include="System.Data.SqlClient" Version="4.8.6" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="6.0.33" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
</ItemGroup>
```

## 分层架构与跨层 using 引用规则（必须）

本 skill 的后端是**单项目分层**：Models / DAL / BLL / Controllers / Common / DTOs 都在同一个 csproj 中，层与层之间靠命名空间区分（默认前缀 `<ProjectName>`，模板为 `IvanProject`）。

**生成任何一个 .cs 文件时，都必须按下表自动补全跨层 using，不要依赖 IDE 自动补全：**

| 生成的文件所在层 | 必带 using | 按需 using |
|------------------|-----------|-----------|
| `Models/` | — | `System`（实体带 `[Serializable]` 时） |
| `DAL/Interface/` | `Ivan.Data`、`Ivan.Data.IOC`、`Ivan.Common`、`<ProjectName>.Models` | — |
| `DAL/` | `Ivan.Data`、`Dapper`、`Ivan.Common`、`<ProjectName>.Models`、`<ProjectName>.DAL.Interface` | `System.Linq`、`System.Data` |
| `BLL/Interface/` | `Ivan.Data`、`Ivan.Data.IOC`、`Ivan.Common`、`<ProjectName>.Models` | — |
| `BLL/` | `Ivan.Data`、`Ivan.Data.IOC`、`Ivan.Common`、`<ProjectName>.Models`、`<ProjectName>.DAL`、`<ProjectName>.DAL.Interface`、`<ProjectName>.BLL.Interface` | `System.Linq`、`System.Data` |
| `Controllers/` | `<ProjectName>.BLL`、`<ProjectName>.Common`、`<ProjectName>.Models`、`Microsoft.AspNetCore.Mvc` | `<ProjectName>.DTOs`（入参/出参用到 DTO 时）、`Microsoft.AspNetCore.Authorization`（用到 [Authorize] 时）、`System.Security.Claims`（取当前用户 Claim 时） |
| `Common/` | — | 按实际类型引用（ExceptionMiddleware 需 `System.Net`） |
| `DTOs/` | `System.ComponentModel.DataAnnotations`（有特性标注时） | — |
| `Program.cs` | `System.Reflection`、`Ivan.Data`、`Ivan.Data.IOC`、`Ivan.IOC`、`Ivan.IOC.Core`、`<ProjectName>.BLL`、`<ProjectName>.Common`、`Microsoft.AspNetCore.Authentication.JwtBearer`、`Microsoft.IdentityModel.Tokens`、`Microsoft.OpenApi.Models` | — |

生成新文件后若编译报 CS0246（找不到类型/命名空间），首先对照上表检查是否漏加 using，而不是改动代码结构。

### 三层接口约定（[InjectIOC]）

由 ivan-db-model-gen → ivan-db-dal-gen → ivan-db-bll-gen 生成三层代码时：

- **DAL 接口** `I<Table>DAL : IBaseDAL<Table>` 标注 `[InjectIOC]`，实现类 `<Table>DAL : BaseDAL<Table>`
- **BLL 接口** `I<Table>BLL : IBaseBLL<Table>` 标注 `[InjectIOC]`，实现类 `<Table>BLL : BaseBLL<I<Table>DAL, Table>`，构造函数注入 `IDataSessionFactory factory`
- 自定义方法在接口中声明，实现里通过 `Factory.OpenSession()` → `session.CreateDAL<I<Table>DAL>()` 调用 DAL

```csharp
// BLL 示例：通过会话工厂创建 DAL 执行查询
public List<Users> GetALL()
{
    using (var session = Factory.OpenSession())
    {
        return session.CreateDAL<IUsersDAL>().SelectAll().ToList();
    }
}
```

### 示例：标准 BLL 文件头部（对照模板 BLL/UsersBLL.cs）

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Ivan.Data;
using Ivan.Data.IOC;
using Ivan.Common;
using IvanTest.Models;
using IvanTest.DAL;
using IvanTest.DAL.Interface;
using IvanTest.BLL.Interface;

namespace IvanTest.BLL;

public partial class UsersBLL : BaseBLL<IUsersDAL, Users>, IUsersBLL
{
    public UsersBLL(IDataSessionFactory factory) : base(factory) { }
    // ...
}
```

### 示例：标准 Controller 文件头部（对照模板 Controllers/UserController.cs）

```csharp
using IvanTest.BLL;        // UserManageService / AuthService
using IvanTest.Common;     // ApiResult / PageResult
using IvanTest.Models;     // Users
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IvanTest.Controllers;
```

## Program.cs 启动模式（必须，参照 SaminWeb 模式）

启动流程固定为四步：**① DatabaseInfo 注册连接串 → ② AddIvanIOC 扫描注册 → ③ 手动服务注册 → ④ 中间件管道**。

```csharp
using System.Reflection;
using Ivan.Data;
using Ivan.Data.IOC;
using Ivan.IOC;
using Ivan.IOC.Core;

var builder = WebApplication.CreateBuilder(args);

// ① 数据库连接注册：按 Database:Provider 选择数据库类型
//    注意：DatabaseInfo 注册必须在 AddIvanIOC / Build 之前完成
var conn = builder.Configuration.GetConnectionString("Default");
var provider = (builder.Configuration["Database:Provider"] ?? "mssql").ToLower();
switch (provider)
{
    case "sqlite": DatabaseInfo.SetSqliteDatabase("default", conn!); break;
    case "mysql": DatabaseInfo.SetMySqlDatabase("default", conn!); break;
    default: DatabaseInfo.SetMsSqlDatabase("default", conn!); break;
}

// 使用 Autofac 作为服务提供程序（false = Autofac；true = 微软原生 DI）
ContextHelper.UseServiceProvider = false;

// ② AddIvanIOC：扫描程序集中 [InjectIOC] 接口，自动注册其实现类
var strAssemblies = new string[] { "IvanTest" };   // 单项目分层即项目自身程序集名
var assemblies = strAssemblies.Select(a => Assembly.Load(a)).ToArray();

builder.Host.AddIvanIOC(assemblies,
    builder =>
    {
        // 注册 Ivan.Data 基础设施：IDataSessionFactory / IDbHelper / IIOCFactoryImp 等
        IOCInitExtensions.OnInit(builder);
    },
    services =>
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        // Swagger 配置...

        // ③ 手动编写的服务（无 [InjectIOC] 接口的普通类）在此注册，
        //    其构造函数依赖（如 IUsersBLL）由容器自动解析
        services.AddScoped<AuthService>();
        services.AddScoped<UserManageService>();

        // JWT / CORS 配置...
    });

var app = builder.Build();

// ④ 中间件管道
app.UseMiddleware<ExceptionMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

**关键规则：**

1. **禁止直接注册带 `[InjectIOC]` 接口的类**（如 `services.AddScoped<UsersBLL>()`）——`AddIvanIOC` 会自动注册 `IUsersBLL → UsersBLL`，重复注册会导致歧义。
2. **`AuthService` / `UserManageService` 等手动服务注入的是接口**（`IUsersBLL`），由 AddIvanIOC 注册的接口绑定解析。
3. **`DatabaseInfo.Set*Database("default", conn)` 必须在任何容器构建之前调用**，key 固定为 `"default"`。
4. **`ContextHelper.UseServiceProvider = false`** 固定为 Autofac 模式（与 SaminWeb 一致）。
5. 不使用 EF Core `EnsureCreated()` 自动建表；Ivan.Data 的 `BaseDAL` 只负责 CRUD，表结构需提前创建。

## 分层架构

```
backend/
├── Models/            # 实体（与数据库表一一对应）
│   ├── Generate/      #   自动生成部分（不可手动修改）
│   └── Users.cs       #   partial 扩展（手动扩展）
├── DAL/
│   ├── Interface/     #   IUsersDAL 等（[InjectIOC]）
│   └── UsersDAL.cs    #   BaseDAL 实现 + partial 扩展
├── BLL/
│   ├── Interface/     #   IUsersBLL 等（[InjectIOC]）
│   ├── UsersBLL.cs    #   BaseBLL 实现 + partial 扩展
│   ├── AuthService.cs #   手动编写（注入 IUsersBLL）
│   └── UserManageService.cs
├── DTOs/              # 入参/出参对象
├── Controllers/       # API 控制器（薄，只做参数校验+调用 BLL）
├── Common/            # 统一响应、异常中间件、工具
│   ├── ApiResult.cs
│   └── ExceptionMiddleware.cs
├── Program.cs
├── nuget.config
└── appsettings.json
```

## 核心约定

### 统一响应格式（ApiResult）

所有接口返回 `ApiResult<T>`，前端 axios 封装依赖此格式：

```csharp
public class ApiResult<T>
{
    public int Code { get; set; }        // 0 成功；非 0 失败
    public string Message { get; set; }  // 提示信息
    public T? Data { get; set; }

    public static ApiResult<T> Ok(T data, string message = "ok") => new() { Code = 0, Message = message, Data = data };
    public static ApiResult<T> Fail(string message, int code = 500) => new() { Code = code, Message = message };
}
```

### 异常处理

- 注册全局异常处理中间件，捕获未处理异常并返回 `ApiResult.Fail(500, ex.Message)`。
- BLL 中业务校验失败时抛出自定义 `BusinessException`（含提示消息），中间件统一转换为 `ApiResult.Fail`。

### ResultModel 成功标记陷阱（重要）

`Ivan.Common.ResultModel` / `ResultModel<T>` **默认 `Success=false`、`Message=null`**。因此 BLL 写方法绝不能写
`return new ResultModel()` 或先 `new` 再赋值 `Data`——调用方 `if (!result.Success) throw new BusinessException(result.Message)`
会把成功操作误判为失败，抛出 `Exception of type 'BusinessException' was thrown.`（空消息，HTTP 200 但 body `code:400`）。

正确写法：

```csharp
return ResultModel.BuildSuccess();                 // 非泛型
return ResultModel<int>.BuildSuccess(id);          // 泛型（BuildSuccess 是静态方法，接收数据作为参数）
```

### 可空日期字段（DateTime?）反序列化陷阱（重要）

前端空日期输入框提交 `""` 空字符串时，System.Text.Json 无法把 `""` 反序列化为 `DateTime?`，
`[ApiController]` 会直接返回 400（body 为验证错误 details，`code` 不是 0）。
前端提交前必须把空日期归一化为 `null`：`payload = { ...form, hireDate: form.hireDate || null }`。
另外注意数据库列名与模型属性名必须一致（如列 `CreateTime` 对应属性 `CreateTime`，不要凭空写 `CreatedAt`），
INSERT/UPDATE 的 SQL 显式列出列名，列名写错会报"列名无效"。

### 数据库访问（Ivan.Data）

- 连接串在 `appsettings.json` 的 `ConnectionStrings:Default`，`Database:Provider` 指定数据库类型（`mssql` / `mysql` / `sqlite`）。
- `Program.cs` 启动时调用 `DatabaseInfo.SetMsSqlDatabase("default", conn)`（或其他 Provider 对应方法）注册。
- DAL 通过 `BaseDAL<T>` 基类获得 CRUD 能力；分页用 `PageSearchModel`（`Page` / `Limit`）。
- **自定义分页查询（`SelectByPage`）注意事项**：
  - SQL 内**禁止**写 `order by`，排序必须通过第 6 个参数 `orderString` 传入（如 `"Id desc"`）；
  - JOIN 查询时 `orderString` 需带表别名（如 `"o.CreateTime desc"`），避免列名歧义；
  - 取搜索条件用 `searchModel.TryGetModelValue("key", out var v) ? v : null`，**禁止**直接索引 `searchModel["key"]`（key 不存在时抛 `KeyNotFoundException`）；
  - **页码是 0-based**：`PageSearchModel.Page` 是 1-based（从 1 开始），传入 `SelectByPage` 前必须 `-1`（框架生成的分页 SQL：MsSql `rownumber > page*size` / MySQL `limit page*size,` / SQLite `offset page*size`）。否则第 1 页会跳过前 N 条——total 正确但列表为空。模板 DAL 均已按 `searchModel.Page - 1` 传参。

### JWT 认证（需要登录时）

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateLifetime = true
        };
    });
```

- 密码存储使用不可逆哈希（如 PBKDF2），禁止明文。
- `[Authorize]` 标注需要登录的 Controller/Action。

### Controller 编写规范

- Controller 只负责：参数校验、调用 BLL、返回 `ApiResult`。
- 禁止在 Controller 中直接写 SQL。
- 路由统一前缀 `[Route("api/[controller]")]`，但**控制器类名与前端路径单复数不一致时必须显式写死**：
  `[controller]` 令牌解析为"控制器类名去掉 Controller 后缀的小写"（`UserController` → `api/user` 单数），
  而实体/前端约定通常是复数 `/api/users`，直接用 `[controller]` 会导致 404。
  规则：`Users/Employees/Orders` 这类复数实体 → 显式 `[Route("api/users")]`（复数）；
  `AuthController` 等非实体控制器 → 可用 `[Route("api/[controller]")]`。

## Swagger

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

保留 Swagger 便于前后端联调；IIS 生产环境可按需关闭。

## 前后端错误处理统一策略

### 错误码约定

| code | 含义 | 前端处理 |
|------|------|----------|
| 0 | 成功 | 正常解包 `res.data` |
| 1 / 非0 | 业务错误 | `ElMessage.error(res.message)` |
| 401 | 未认证/登录过期 | 清除登录态，跳转登录页 |
| 403 | 权限不足 | 清除登录态，提示权限不足后跳转登录页 |
| 500 | 服务端异常 | 显示后端 message 或默认提示 |

### 服务端错误产生路径

```
1. 业务校验失败 → throw new BusinessException("用户名或密码错误")
   → ExceptionMiddleware 捕获 → ApiResult.Fail(message, 400) → HTTP 200

2. [RequirePerm] 权限校验失败
   → PermAuthorizationFilter → ApiResult.Fail("权限不足", 403) → HTTP 200

3. 未处理异常（如 null ref）
   → ExceptionMiddleware 捕获 → ApiResult.Fail(ex.Message, 500) → HTTP 200

4. [Authorize] / JWT 认证失败
   → ASP.NET 框架返回 HTTP 401
   → 前端 axios 拦截器 error.response 分支捕获
```

### 关键原则

1. **服务端所有响应均为 HTTP 200**，错误信息在 `code` / `message` 字段中，不依赖 HTTP 状态码（401/403 例外）。
2. **PermAuthorizationFilter** 返回 `ObjectResult(ApiResult.Fail("权限不足", 403)) { StatusCode = 200 }`，而非 `ForbidResult()`，确保前端响应拦截器能正确解析。
3. **前端请求拦截器** 优先使用服务端返回的 `res.message`，不自行生成错误文案——确保错误信息前后端一致。
4. **ExceptionMiddleware** 捕获所有未处理异常，提取 `ex.Message` 作为 `message` 返回前端。

## Ivan.Redis 缓存配置

### 依赖引入

后端项目若需使用 Redis 缓存，取消 `IvanProject.csproj` 中 Ivan.Redis 包的注释：

```xml
<!-- 引入 Ivan.Redis（使用 Redis 缓存时启用） -->
<PackageReference Include="Ivan.Redis" Version="1.0.1" />
```

### 配置 Redis 连接

在 `appsettings.json` 添加 Redis 配置节：

```json
{
  "Redis": {
    "ConfigKey": "Default",
    "ConnectionString": "127.0.0.1:6379,defaultDatabase=0,poolsize=10"
  }
}
```

### 缓存服务体系

项目提供两层缓存服务：

| 服务 | 接口/类 | 适用场景 | 实现 |
|------|---------|----------|------|
| 通用缓存 | `ICacheService` | 权限、业务数据缓存 | `MemoryCacheService`（开发）/ `RedisCacheService`（生产） |
| 菜单缓存 | `MenuCacheService` | 菜单树、节点缓存、权限缓存 | 基于 `IMemoryCache` 的进程级缓存 |

### 环境切换

在 `Program.cs` 中切换 `ICacheService` 的实现：

```csharp
// 开发/单机环境（默认）
services.AddSingleton<ICacheService, MemoryCacheService>();

// 生产/分布式环境（取消上行的注释，启用下行）
// services.AddSingleton<ICacheService, RedisCacheService>();
```

### 缓存策略

| 缓存键前缀 | 内容 | 过期时间 | 失效时机 |
|-----------|------|----------|----------|
| `ivan:cache:*` | `ICacheService` 通用缓存 | 按场景设定（默认1小时） | 业务数据变更时显式 `Remove()` |
| `menu:tree` | 菜单全量树 | 30分钟 | 菜单增删改/启用禁用/排序移动/角色权限变更 |
| `menu:all` | 菜单平铺列表 | 30分钟 | 同上 |
| `menu:{id}` | 单菜单节点 | 10分钟 | 该节点变更时精准清除 |
| `role_menus_{roleId}` | 角色菜单权限 | 30分钟 | 角色菜单变更时清除 |
| `role_buttons_{roleId}` | 角色按钮权限 | 30分钟 | 角色按钮变更时清除 |
| `role_perm_codes_{roleId}` | 角色权限编码集 | 30分钟 | 角色权限变更时清除 |
| `user_perms_{userId}` | 用户全量权限编码 | 10分钟 | 用户角色关系变更时清除 |

### 异常安全策略

所有 Redis 操作（读写、删除）都有 `try/catch` 保护：
- **读操作失败**：降级返回 `default(T)` 或 `null`，触发查库
- **写操作失败**：仅记录日志，跳过缓存写入，不中断主流程
- **删除操作失败**：仅记录日志，不影响业务操作

> 核心原则：**Redis 不可用时系统仍能正常运行**（仅缓存失效，功能不中断）

## 按钮级别权限控制

### 整体流程

```
用户登录
  │
  ▼
AuthService.Login() → 汇总角色的 menuIds + buttonPermissions（按菜单路径分组）
  │
  ▼
前端 userStore 存储：
  - menuIds: number[]           → hasMenu(menuId)    // 左侧菜单显隐
  - buttonPermissions: { [menuPath: string]: string[] }
                                → hasButton(menuPath, buttonKey)  // 页面按钮显隐
  │
  ▼
Controller/Action 添加 [RequirePerm("users:edit")]
  │
  ▼
PermAuthorizationFilter 拦截
  → 提取当前用户角色 → 调用 RolePermissionService.HasAnyPermissionAsync()
  → 有权限 → 放行
  → 无权限 → ApiResult.Fail("权限不足", 403)
```

### 权限数据结构（统一权限树模型）

| 表/字段 | 说明 |
|---------|------|
| `Menus.MenuType` | 节点类型：1=目录 2=菜单(页面) 3=按钮，单表承载统一权限树 |
| `Menus.PermissionCode` | 按钮节点的权限编码（两段式如 `users:edit`，全局唯一）；后端 `[RequirePerm]` 与前端 `v-perm` 共用 |
| `Menus.ControllerAction` | 按钮节点绑定的后端接口（`控制器.方法` 如 `UserController.Create`），动态维护、保存时校验存在性 |
| `Menus.FullPath` | 路径枚举（如 `/1/3/7`），子树查询加速；`Level` 冗余层级（根=0） |
| `RoleMenus` | **唯一**授权关联表（RoleId + MenuId）：目录/菜单/按钮节点统一写入 |

按钮节点即权限：新增按钮 = 在菜单管理中给某菜单添加 MenuType=3 子节点并填写 PermissionCode；角色授权 = 权限树勾选（含按钮叶子）。数据库种子由 `database/init_database.sql` 一体化脚本提供（18 节点：系统管理目录 + 4 菜单 + 13 按钮；admin 角色全授权，按钮节点 PermissionCode 与 Controller 内 `[RequirePerm]` 声明一致）。

**校验**（`BLL/MenusBLL.ValidatePermissionConfig`，Create/Modify 共用）：
- 按钮节点：PermissionCode 必填、两段式及以上（`^[a-zA-Z][a-zA-Z0-9_]*(:[a-zA-Z][a-zA-Z0-9_]*)+$`）、全局唯一（排除自身）；ControllerAction 选填但必须存在于 `ControllerActionScanner` 扫描结果
- 菜单节点（MenuType=2）：Path 必填且全局唯一
- 目录/菜单节点：不允许携带 PermissionCode/ControllerAction（后端强制置空）

**接口**：

| 端点 | 方法 | 说明 |
|------|------|------|
| `/api/menus/tree` | GET | 统一权限树（含按钮节点，前端角色授权树与菜单管理共用） |
| `/api/menus` | GET/POST | 菜单平铺列表 / 新增节点 |
| `/api/menus/{id}` | PUT/DELETE | 更新节点（仅名称/类型字段/排序/可见启用/按钮属性）/ 删除（级联删子孙 + RoleMenus） |
| `/api/menus/bindable-actions` | GET | 可绑定的后端 Controller Action 列表（反射扫描，供按钮节点"绑定接口"下拉与一致性核对） |

### 权限编码格式

`{resource}:{action}`，例如：

| 编码 | 含义 |
|------|------|
| `users:view` | 用户管理-查看 |
| `users:edit` | 用户管理-编辑 |
| `roles:delete` | 角色管理-删除 |
| `orders:export` | 订单管理-导出 |

编码生成逻辑：菜单路径 `/users` → 去掉前导 `/`，`/` 替换为 `:` → `users`；按钮 key `edit` → 拼接 → `users:edit`。

**自定义权限编码**：在菜单管理中直接编辑按钮节点的 `PermissionCode`（如改为 `sys:user:add` 的多段式，须通过唯一性 + 格式校验）。配置后：
- 后端 `[RequirePerm]` 声明编码须与节点 PermissionCode 一致（权限码集合直接来自按钮节点）
- 前端按钮显隐（`v-perm`）按 PermissionCode 的 action 段判定
- 校验规则：字母开头的冒号分段（两段或多段，段内字母/数字/下划线）、全局唯一（保存时后端校验并给出明确错误）

**按钮 ↔ 后端 Action 绑定**：按钮节点的 `ControllerAction` 字段（`控制器.方法`，如 `UserController.Create`），数据来自 `GET /api/menus/bindable-actions`（反射扫描全部 Controller Action，含 HTTP 方法、路由、声明的权限编码），保存时后端校验存在性。绑定关系用于可视化管理与前后端一致性核对（下拉选项会显示该 Action 声明的 `[RequirePerm]` 编码，与按钮权限编码不一致时一目了然）。

**重要**：`[RequirePerm]` 声明的编码必须与按钮节点的 PermissionCode 一致。同时页面查看权限 `view` 必须作为按钮节点存在（`[RequirePerm("{path}:view")]` 才能通过），种子数据已为每个页面内置 `查看xx`（`{resource}:view`）按钮节点。

### 角色权限管理页面

RolesController 提供以下端点管理权限：

| 端点 | 方法 | 权限要求 | 说明 |
|------|------|----------|------|
| `/api/roles/{id}/menus` | GET | `roles:view` | 获取角色授权节点ID列表（含按钮节点） |
| `/api/roles/{id}/menus` | PUT | `roles:permission` | 保存角色权限（全量覆盖，目录/菜单/按钮统一写入 RoleMenus） |

**权限分配弹窗**：统一权限树直接勾选（按钮节点显示其 PermissionCode 标签），勾选即自动保存；登录返回的 `menuIds` 含按钮节点（供树回显），`buttonPermissions` 按父菜单 Path 分组的 action 段供 `v-perm` 判定。

### 前端按钮显示控制

```typescript
// userStore.ts
hasButton(menuPath: string, buttonKey: string): boolean {
  if (this.roles.includes('admin')) return true
  return this.buttonPermissions[menuPath]?.includes(buttonKey) ?? false
}
```

页面中使用全局 `v-perm` 指令（`src/directives/perm.ts`，main.ts 已注册）——**页面按钮统一用指令控制，不要写 `v-if="userStore.hasButton(...)"`**：

```vue
<el-button v-perm="'add'">新增</el-button>              <!-- 无权限时移除元素（默认） -->
<el-button v-perm="['edit','permission']">编辑</el-button>  <!-- 任一 Key 命中即显示 -->
<el-button v-perm:disable="'export'">导出</el-button>    <!-- 无权限时禁用置灰，不移除 -->
```

`v-perm` 的 Key 即按钮节点 PermissionCode 的 action 段，与后端 `[RequirePerm("{resource}:{action}")]` 的 action 段一一对应。菜单管理页（`src/views/menus/index.vue`）"类型"列以标签展示按钮节点及其权限编码，"权限管理"弹窗表格化维护按钮子节点。

登录返回 `buttonPermissions = { menuPath: [buttonKey] }`（buttonKey = 按钮节点 PermissionCode 的 action 段，归属父菜单 Path）；admin 角色拥有全部菜单与按钮权限（缺 view 补 view）。

### 服务端 Action 权限校验

在 Controller 或 Action 上添加 `[RequirePerm]` 特性声明所需权限：

```csharp
[ApiController]
[Route("api/users")]
[Authorize]
[RequirePerm("users:view")]             // Controller 级：所有 Action 都需要 view 权限
public class UsersController : ControllerBase
{
    [RequirePerm("users:add")]            // Action 级：可覆盖/追加权限要求
    [HttpPost]
    public ApiResult<int> Create(Users user) { ... }
}
```

> **实现注意**：`PermAuthorizationFilter` 取注解时**必须 Action 级优先**。不能用
> `EndpointMetadata.OfType<RequirePermAttribute>().FirstOrDefault()`——MVC 元数据中 Controller 特性
> 排在 Action 特性之前，FirstOrDefault 永远取到类级注解，导致 `users:delete` 等按钮级权限全部失效。
> 模板通过 `ControllerActionDescriptor.MethodInfo.GetCustomAttributes(...)` 优先读取 Action 级注解。

`PermAuthorizationFilter` 在 `OnAuthorizationAsync` 中校验：
1. 获取 Action 上的 `[RequirePerm]`，若没有则取 Controller 上的
2. 获取当前用户的所有角色编码（从 JWT Claim 中）
3. 调用 `RolePermissionService.HasAnyPermissionAsync(roleCode, requiredPermissions)` 检查任一角色是否拥有任一所需权限
4. 管理员角色 `admin` 自动通过所有权限校验

### 缓存级联失效

角色权限变更时，`RolePermissionService.ClearRolePermissionCache(roleId)` 会：
1. 清除角色自身的权限缓存（`role_menus_*`、`role_buttons_*`、`role_perm_codes_*`）
2. 级联清除**所有拥有该角色的用户**的权限缓存（`user_perms_*`）

确保权限变更即时生效，无需用户重新登录。
