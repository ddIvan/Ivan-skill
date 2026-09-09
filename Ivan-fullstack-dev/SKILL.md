---
name: ivan-fullstack-dev
description: Ivan 的全栈 Web 项目开发 Skill。当需要从零开发一个完整业务系统（如管理系统、门户网站、业务平台等）时使用，覆盖 需求澄清 → 前端 Vue3+TypeScript → 后端 .NET 6.0 → 数据库(SQLite/MSSQL/MySQL) → IIS 部署 全流程。触发词：开发系统/开发项目/新建项目/做一个管理系统等。开发前必须先询问用户关键选项（数据库、UI 组件库、认证方式、部署方式等），得到确认后再动手。
---

# Ivan Fullstack Dev

## Overview

用于开发 Ivan 的完整全栈业务项目。前端固定使用 Vue 3 + TypeScript（UI 组件库不限定，默认 Element Plus），后端使用 .NET 6.0 Web API + **Ivan.Data 框架**（BaseDAL/BaseBLL 分层 + Ivan.IOC 依赖注入），数据库支持 SQLite / MSSQL / MySQL，前后端最终可能发布到 Windows IIS。

**核心原则：先问清楚，再动手。** 任何项目在生成代码前，必须先将关键的不确定项以提问方式与用户确认，得到明确答复后才开始搭建。

## 工作流决策树

```
用户提出开发需求
        │
        ▼
┌─ 需求澄清 ─────────────────────────────────┐
│ 依次确认：                                  │
│ 1. 项目名称 / 中文名 / 端口                 │
│ 2. 数据库：SQLite / MSSQL / MySQL          │
│ 3. UI 组件库：Element Plus / 其他          │
│ 4. 是否需要登录认证(用户/角色/权限)         │
│ 5. 部署方式：IIS / 直接运行                │
│ 6. 项目具体功能模块清单                     │
└────────────────────────────────────────────┘
        │  仅询问用户未明确给出的选项；已明确的直接采用
        ▼
┌─ 项目初始化 ───────────────────────────────┐
│ 创建 前端(frontend/) + 后端(backend/) 结构  │
│ 后端按 Model/DAL/BLL/Controller 分层        │
└────────────────────────────────────────────┘
        │
        ▼
┌─ 后端先行（.NET 6.0）─────────────────────┐
│ 搭建 API → 配置 EF Core + 数据库 → 建表 →  │
│ 编写 DAL/BLL/Controller → Swagger 联调     │
└────────────────────────────────────────────┘
        │
        ▼
┌─ 前端搭建（Vue3 + TS）────────────────────┐
│ Vite 脚手架 → Router/Pinia → Axios 封装 →  │
│ 布局/登录/业务页面 → 对接后端 API          │
└────────────────────────────────────────────┘
        │
        ▼
┌─ 部署配置（IIS）───────────────────────────┐
│ 后端发布 + web.config                      │
│ 前端 build + 静态托管 / 反向代理           │
└────────────────────────────────────────────┘
```

## 第一步：需求澄清（必须执行）

收到开发请求后，先列出所有**用户未明确指定**的关键选项，用提问的方式逐项确认。已由用户明确指定的选项直接采用，不要重复询问。

必问项（除非已明确）：

1. **项目名称**：项目英文名（用作解决方案/目录名）、中文显示名、开发端口（前端 5173、后端 5000 仅作默认值）。
2. **数据库**：SQLite（默认，零配置）/ MSSQL / MySQL。若选 MSSQL 或 MySQL，同时确认连接字符串或服务器信息。
3. **UI 组件库**：默认 Element Plus，若用户有偏好（如 Ant Design Vue、Naive UI、Vuetify）则按其选择。
4. **认证方式**：是否需要登录/用户管理/角色权限。需要时默认采用 JWT + 基于 EF Core 的用户表方案。
5. **部署环境**：是否发布到 IIS。IIS 下后端需提供 `web.config`（ASP.NET Core Module 托管），前端需配置静态文件 + 反向代理。
6. **功能模块**：明确本次要开发的具体业务模块清单，按模块逐个实现。

> 若用户回复"你看着办"或不做选择，采用默认值：SQLite + Element Plus + JWT 认证 + IIS 部署，并告知用户采用的默认方案。

## 第二步：项目初始化

创建统一的项目结构：

```
<ProjectName>/
├── frontend/                  # Vue3 + TS 前端
│   ├── src/
│   │   ├── api/               # Axios 请求封装 + 各模块 API
│   │   ├── assets/            # 静态资源
│   │   ├── components/        # 公共组件
│   │   ├── router/            # Vue Router 路由
│   │   ├── stores/            # Pinia 状态
│   │   ├── views/             # 页面
│   │   ├── utils/             # 工具函数
│   │   ├── App.vue
│   │   └── main.ts
│   ├── index.html
│   ├── package.json
│   ├── tsconfig.json
│   └── vite.config.ts
├── backend/                   # .NET 6.0 Web API 后端
│   ├── Models/                # 实体模型
│   ├── DAL/                   # 数据访问层 (EF Core)
│   ├── BLL/                   # 业务逻辑层
│   ├── Controllers/           # API 控制器
│   ├── DTOs/                  # 传输对象
│   ├── Program.cs
│   ├── appsettings.json
│   └── <ProjectName>.csproj
└── README.md                  # 项目说明 + 启动/部署步骤
```

先创建后端再创建前端，保证前端联调时后端 API 已可访问。

## 第三步：后端开发（.NET 6.0 + Ivan.Data 框架）

遵循 `references/backend-guide.md` 的规范：

1. 创建 .NET 6.0 Web API 项目，核心包：`Ivan.Common`、`Ivan.Data`、`Ivan.Data.IOC`、`Ivan.IOC`（均从私有源还原）+ `System.Data.SqlClient`（按数据库选驱动）+ `Swashbuckle.AspNetCore` + `Microsoft.AspNetCore.Authentication.JwtBearer`。
2. **NuGet 源（必须）**：后端项目根目录必须包含 `nuget.config`（模板已内置）。`Ivan.*` 等内部库从私有源获取：`http://61.169.209.58:19081/repository/nuget-hosted/`（账号 `apps` / 密码 `123456`，凭据已写入 nuget.config 的 `packageSourceCredentials`，不要提交到公共仓库）。若用户需要其他 Ivan 开头的库，直接在 csproj 中添加 `PackageReference`，还原时自动走该源。
3. 配置 `appsettings.json` 中的 `ConnectionStrings:Default`、`Database:Provider`（`mssql`/`mysql`/`sqlite`）与 `Jwt` 配置节。
4. 按 `assets/backend-template/` 中模板搭建分层：Models（Generate/ + partial 扩展）→ DAL（Interface + BaseDAL 实现）→ BLL（Interface + BaseBLL 实现 + 手动 Service）→ Controllers。
5. **启动模式（必须，参照 SaminWeb）**：`Program.cs` 按四步固定流程编写——① `DatabaseInfo.SetMsSqlDatabase("default", conn)` 等注册连接串 → ② `ContextHelper.UseServiceProvider = false` + `builder.Host.AddIvanIOC(程序集数组, ...)` 扫描 `[InjectIOC]` 接口自动注册，回调中调用 `IOCInitExtensions.OnInit(builder)` → ③ 手动服务（`AuthService`/`UsersBLL` 等）用 `services.AddScoped` 注册，其依赖的 `IUsersBLL` 等接口由容器解析 → ④ 中间件管道。**禁止**再直接注册带 `[InjectIOC]` 接口的实现类（如 `AddScoped<UsersBLL>()`）。
6. **using 引用（必须）**：本 skill 的后端为单项目分层（Models/DAL/BLL/Controllers 同在一个 csproj），生成各层代码时必须自动补充跨层 using，规则见 `references/backend-guide.md` 的"跨层 using 引用规则"一节（含 Ivan.Data/Ivan.Common 框架命名空间与各层 Interface 命名空间的完整对照表）。
7. 统一 API 响应格式（`ApiResult<T>`），统一异常处理中间件。
8. 若需要认证：启用 JWT Bearer 认证 + 用户/角色表 + 登录接口。
9. 启用 Swagger 便于联调。

## 第四步：数据库配置

按 `references/database-guide.md` 配置：

- 连接串写在 `appsettings.json` 的 `ConnectionStrings:Default`，`Database:Provider` 指定类型（`mssql` / `mysql` / `sqlite`）。
- `Program.cs` 启动时调用 `DatabaseInfo.SetMsSqlDatabase("default", conn)`（或 `SetMySqlDatabase` / `SetSqliteDatabase`）注册连接，key 固定 `"default"`。
- **不使用 EF Core 自动建表**：Ivan.Data 的 `BaseDAL` 只负责 CRUD，表结构需提前创建（手动执行 SQL 脚本或数据库管理工具）。

## 第五步：前端开发（Vue3 + TypeScript）

遵循 `references/frontend-guide.md` 的规范：

1. 按 `assets/frontend-template/` 模板初始化 Vite + Vue3 + TS 项目。
2. 安装依赖：`element-plus`、`vue-router`、`pinia`、`axios`、`@element-plus/icons-vue`。
3. 完成 Axios 封装（`assets/frontend-template/src/api/request.ts`）：baseURL、token 注入、统一错误处理、401 跳转登录。
4. 搭建布局框架（侧边栏菜单 + 顶栏 + 主内容区）与登录页。
5. 按功能模块逐个实现业务页面，统一走封装后的 API 层。

## 第六步：IIS 部署配置

按 `references/iis-deploy-guide.md` 配置：

1. **后端**：发布为独立/框架依赖包，站点根目录放置 `web.config`（见 `assets/web.config`），启用 ASP.NET Core Module (ANCM) 托管；若站点被其他应用占用端口或需要 URL 重写，注意处理。
2. **前端**：`npm run build` 后将 `dist/` 发布到站点，配 URL Rewrite 将非静态文件请求转发至后端 API（或后端设置 CORS 允许跨域直连）。
3. 记录每个站点所需配置（端口、物理路径、应用池 .NET CLR 版本设为"无托管代码"）。

## Resources

本 Skill 附带以下资源：

### references/（加载进上下文的指南）
- `questionnaire.md` — 需求澄清提问模板与默认值速查表
- `frontend-guide.md` — Vue3 + TS + Element Plus 前端开发规范
- `backend-guide.md` — .NET 6.0 后端分层开发规范
- `database-guide.md` — SQLite / MSSQL / MySQL 配置与连接串
- `iis-deploy-guide.md` — IIS 部署（后端 ANCM + 前端静态托管/代理）

### assets/（复制到项目中的模板）
- `frontend-template/` — Vue3 + TS + Element Plus 前端脚手架模板
- `backend-template/` — .NET 6.0 分层后端脚手架模板（含 nuget.config 私有源配置）
- `web.config` — IIS 托管 ASP.NET Core 后端所需配置模板

### scripts/（可执行脚本）
- 当前无需脚本；后续如需批量脚手架生成，在此目录添加。
