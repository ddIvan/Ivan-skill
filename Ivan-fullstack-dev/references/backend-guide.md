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
| `DAL/` | `Ivan.Data`、`Ivan.Data.SQLBuilder`、`Dapper`、`Ivan.Common`、`<ProjectName>.Models`、`<ProjectName>.DAL.Interface` | `System.Linq`、`System.Data` |
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
using IvanTest.BLL;        // UsersBLL / AuthService
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
        services.AddScoped<UsersBLL>();

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
2. **`AuthService` / `UsersBLL` 等手动服务注入的是接口**（`IUsersBLL`），由 AddIvanIOC 注册的接口绑定解析。
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
│   └── UsersBLL.cs
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

### 日期格式统一规范（yyyy-MM-dd）

- 后端 JSON 输出统一 `yyyy-MM-dd`：模板 `Common/DateTimeJsonConverter.cs` 提供 `DateTimeJsonConverter` / `NullableDateTimeJsonConverter`
  （序列化只输出日期部分，可空输出 null；反序列化兼容完整时间戳），必须在 Program.cs 的 `AddJsonOptions` 中注册（模板已注册）。
- 前端 `<el-date-picker>` 统一 `type="date"` + `value-format="YYYY-MM-DD"`（不要用 datetime 带时分秒）；表格日期列宽约 110。

### 接口测试中文乱码注意（测试工具问题）

PowerShell 5.1 用 `-Body '含中文的json'` 发送请求时默认按 ASCII 编码，中文会入库为 `??`（码点 63）。
测试时必须用 UTF-8 字节发送：`Invoke-WebRequest ... -Body ([System.Text.Encoding]::UTF8.GetBytes($json))`。
这只是测试工具的问题——浏览器/axios 提交与 ASP.NET Core 接收链路本身没有编码缺陷。验证库内字符用
`SELECT UNICODE(col)`（真乱码码点为 63，控制台 `??` 显示问题不影响实际数据）。

### 数据库访问（Ivan.Data）

- 连接串在 `appsettings.json` 的 `ConnectionStrings:Default`，`Database:Provider` 指定数据库类型（`mssql` / `mysql` / `sqlite`）。
- `Program.cs` 启动时调用 `DatabaseInfo.SetMsSqlDatabase("default", conn)`（或其他 Provider 对应方法）注册。
- DAL 通过 `BaseDAL<T>` 基类获得 CRUD 能力；分页用 `PageSearchModel`（`Page` / `Limit`）。

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
