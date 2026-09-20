using System.Reflection;
using System.Text;
using Ivan.Common;
using Ivan.Data;
using Ivan.Data.IOC;
using Ivan.IOC;
using Ivan.IOC.Core;
using IvanProject.BLL;
using IvanProject.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// ==========================================================================
// Ivan.Data / Ivan.IOC 启动模式（参照 SaminWeb/Startup 模式），固定四步流程：
// ① DatabaseInfo 注册连接串 → ② AddIvanIOC 扫描注册 → ③ 手动服务注册 → ④ 中间件管道
// ==========================================================================

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
var strAssemblies = new string[]
{
    "IvanProject"
};
var assemblies = strAssemblies.Select(a => Assembly.Load(a)).ToArray();

builder.Host.AddIvanIOC(assemblies,
    builder =>
    {
        // 注册 Ivan.Data 基础设施：IDataSessionFactory / IDbHelper / IIOCFactoryImp 等
        IOCInitExtensions.OnInit(builder);
    },
    services =>
    {
        services.AddControllers(o =>
            {
                // 注册 PermAuthorizationFilter 全局权限校验过滤器
                o.Filters.Add<PermAuthorizationFilter>();
            })
            .AddJsonOptions(options =>
            {
                // 统一日期序列化格式：yyyy-MM-dd（可空日期输出 null；反序列化兼容完整时间戳）
                options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new NullableDateTimeJsonConverter());
            });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ivan Project API", Version = "v1" });
            // Swagger 支持 JWT 认证
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "请输入 JWT Token，无需 Bearer 前缀"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // ③ 手动编写的服务（无 [InjectIOC] 接口的普通类）在此注册，
        //    其构造函数依赖（如 IUsersBLL）由容器自动解析。
        //    禁止再直接注册带 [InjectIOC] 接口的实现类（如 AddScoped<UsersBLL>()），会重复注册。
        services.AddScoped<AuthService>();
        services.AddScoped<RolePermissionService>();
        services.AddScoped<PermAuthorizationFilter>();

        // ==================== 缓存服务配置 ====================
        //
        // 缓存体系说明：
        //   1. ICacheService（通用缓存接口）→ 用于 RolePermissionService 等权限/业务缓存
        //   2. MenuCacheService（菜单专用缓存）→ 直接使用 Ivan.Redis 的 RedisClient
        //
        // 环境切换（修改 ICacheService 注册即可）：
        //   开发/单机环境：MemoryCacheService（进程内内存缓存，无需 Redis）
        //   生产/分布式环境：RedisCacheService（Ivan.Redis 实现，支持集群共享缓存）
        //
        // 切换为 RedisCacheService 的步骤：
        //   1. csproj 中取消注释 <PackageReference Include="Ivan.Redis" Version="1.0.1" />
        //   2. appsettings.json 中添加 Redis 配置节：
        //      "Redis": { "ConfigKey": "Default", "ConnectionString": "127.0.0.1:6379,password=xxx" }
        //   3. 将下面的 MemoryCacheService 替换为 RedisCacheService
        // ====================

        // MenuCacheService 使用 singleton 生命周期，确保缓存在整个应用范围内共享
        services.AddSingleton<MenuCacheService>();

        // 通用缓存服务：开发环境用 MemoryCacheService，生产环境切换为 RedisCacheService
        services.AddMemoryCache();  // MemoryCacheService 依赖 IMemoryCache
        services.AddSingleton<ICacheService, MemoryCacheService>();
        // services.AddSingleton<ICacheService, RedisCacheService>();  // 生产环境取消注释并注释上一行

        // JWT 认证
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // 认证失败/未携带 token（如 localStorage 残留旧项目签发的 token）：
                // 返回统一 ApiResult（HTTP 200 + code=401），前端拦截器可解析并引导重新登录
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(ApiResult.Fail("登录已失效，请重新登录", 401));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(ApiResult.Fail("权限不足，禁止访问", 403));
                    }
                };
            });

        // CORS：IIS 部署时若前后端分开部署且不走反向代理，需放开跨域
        services.AddCors(o => o.AddPolicy("Default", p =>
            p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
    });

var app = builder.Build();

// ④ 中间件管道
// 注意：不使用 EF Core EnsureCreated() 自动建表。
// 数据库表结构需提前创建（执行建表 SQL 或通过数据库管理工具）。
// Ivan.Data 框架的 BaseDAL 不负责建表，只负责 CRUD 操作。

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
