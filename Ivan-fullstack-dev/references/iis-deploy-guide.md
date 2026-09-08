# IIS 部署指南

前后端分别发布为两个 IIS 站点，或后端作为子应用。以下为最常用方案。

## 前置条件

- 服务器安装 IIS，并安装 **ASP.NET Core Module (ANCM) v2**（下载 `aspnetcore-runtime` 安装包或独立安装 Hosting Bundle）。
- .NET 6 Runtime 已安装（框架依赖发布时）。
- 若用 URL Rewrite，需安装 **IIS URL Rewrite 模块**。

## 后端部署（ASP.NET Core Web API）

1. **发布**：

```bash
dotnet publish -c Release -o ./publish
```

2. 将 `publish/` 内容复制到站点物理目录（如 `C:\inetpub\wwwroot\IvanBackend`）。
3. 站点根目录放置 `web.config`（内容见 `assets/web.config`），核心配置：

```xml
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\IvanProject.dll"
                  stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

> `processPath` 与 `arguments` 中的 DLL 名替换为实际发布程序集名。

4. **应用池设置**：.NET CLR 版本选择 **"无托管代码 (No Managed Code)"**。
5. 确认连接字符串中的数据库路径/服务对应用池账户（`IIS_IUSRS` / `IIS APPPOOL\<名称>`）可读写。

## 前端部署（Vue3 静态站点）

1. **构建**：

```bash
npm run build   # 生成 dist/
```

2. 将 `dist/` 内容复制到前端站点物理目录（如 `C:\inetpub\wwwroot\IvanWeb`）。
3. 前端 API 地址在构建时确定：
   - **方式 A（推荐，同源代理）**：前端请求 `/api/...`，IIS 上配置 URL Rewrite 把 `/api/(.*)` 转发到后端，例如：

```xml
<rewrite>
  <rules>
    <rule name="ApiProxy" stopProcessing="true">
      <match url="^api/(.*)" />
      <action type="Rewrite" url="http://localhost:53989/api/{R:1}" />
    </rule>
  </rules>
</rewrite>
```

   - **方式 B（跨域直连）**：前端 axios `baseURL` 直接指向后端地址，后端启用 CORS：

```csharp
builder.Services.AddCors(o => o.AddPolicy("Default", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
app.UseCors("Default");
```

   注意 `AllowAnyOrigin` 与 `AllowCredentials` 不能同时使用（JWT 走 Header 时用 `AllowAnyHeader` 即可）。

4. **历史路由回退**（若前端使用 history 模式而非 hash 模式）：URL Rewrite 增加规则，将非文件/非 `/api` 的请求 rewrite 到 `index.html`：

```xml
<rule name="SpaFallback" stopProcessing="true">
  <match url=".*" />
  <conditions>
    <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
    <add input="{REQUEST_URI}" pattern="^/api/" negate="true" />
  </conditions>
  <action type="Rewrite" url="/index.html" />
</rule>
```

## 验证清单

- [ ] 后端站点访问 `/swagger/index.html` 正常（开发环境）
- [ ] 后端接口返回统一 `ApiResult` 格式
- [ ] 前端页面正常加载，登录后 API 请求成功
- [ ] 数据库文件/服务对 IIS 应用池账户可访问
- [ ] 日志：后端 `logs/` 目录可写，出错时查看 stdout 日志
