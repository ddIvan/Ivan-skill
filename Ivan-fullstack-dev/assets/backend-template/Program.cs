using System.Text;
using IvanProject.DAL;
using IvanProject.BLL;
using IvanProject.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 统一日期序列化格式：yyyy-MM-dd（可空日期输出 null；反序列化兼容完整时间戳）
        options.JsonSerializerOptions.Converters.Add(new IvanProject.Common.DateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new IvanProject.Common.NullableDateTimeJsonConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
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

// 数据库：按 Database:Provider 切换 sqlite / mssql / mysql
var conn = builder.Configuration.GetConnectionString("Default");
var provider = builder.Configuration["Database:Provider"];
builder.Services.AddDbContext<AppDbContext>(options =>
{
    switch (provider)
    {
        case "mssql": options.UseSqlServer(conn); break;
        case "mysql": options.UseMySQL(conn); break;
        default: options.UseSqlite(conn); break;
    }
});

// 业务服务注册（新增 BLL 服务后在此追加）
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserManageService>();

// JWT 认证
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
builder.Services.AddCors(o => o.AddPolicy("Default", p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// 启动时自动建表（正式项目可改用 EF Migration）
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

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
