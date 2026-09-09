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
        services.AddControllers()
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
        services.AddMemoryCache();

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
