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
┌─ 第一步：需求澄清 ──────────────────────────┐
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
┌─ 第二步：确认代码生成位置（必须执行）───────┐
│ 询问用户项目要创建在哪个绝对路径下           │
│ e.g. D:\project\Samin\temp\IvanTest       │
└────────────────────────────────────────────┘
        │
        ▼
┌─ 第三步：项目初始化 ───────────────────────┐
│ 创建 database/ + frontend/ + backend/ 结构  │
│ 后端按 Model/DAL/BLL/Controller 分层        │
└────────────────────────────────────────────┘
        │
        ▼
┌─ 第四步：后端开发（.NET 6.0）──────────────┐
│ 搭建 API → 配置 Ivan.Data + 数据库 → 建表 →│
│ 编写 DAL/BLL/Controller → Swagger 联调     │
└────────────────────────────────────────────┘
        │
        ▼
┌─ 第五步：前端搭建（Vue3 + TS）─────────────┐
│ Vite 脚手架 → Router/Pinia → Axios 封装 →  │
│ 布局/登录/业务页面 → 对接后端 API          │
└────────────────────────────────────────────┘
        │
        ▼
┌─ 第六步：部署配置（IIS）───────────────────┐
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

## 第二步：确认代码生成位置（必须执行）

**在开始任何代码生成之前，必须先询问用户项目的创建位置（绝对路径）。**

> 示例提问："请问项目要创建在哪个目录下？例如 `D:\projects\IvanTest`，我会在该路径下创建 `<ProjectName>` 文件夹及其全部子结构。"

得到用户明确的绝对路径答复后，再进入第三步项目初始化。如果用户未给出绝对路径，不要自行猜测或使用默认路径。

## 第三步：项目初始化

在用户指定的绝对路径下创建 `<ProjectName>` 文件夹及以下统一结构：

```
<ProjectName>/
├── database/                  # 数据库脚本（SQL 建表、种子数据等）
│   └── create_tables.sql
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
│   ├── DAL/                   # 数据访问层
│   ├── BLL/                   # 业务逻辑层
│   ├── Controllers/           # API 控制器
│   ├── DTOs/                  # 传输对象
│   ├── Program.cs
│   ├── appsettings.json
│   └── <ProjectName>.csproj
└── README.md                  # 项目说明 + 启动/部署步骤
```

**目录说明**：
- `database/`：数据库相关文件（建表 SQL、种子数据脚本等）独立存放，不再混入 `backend/` 内部。
- `frontend/` 和 `backend/` 目录职责不变。

先创建后端再创建前端，保证前端联调时后端 API 已可访问。

## 第四步：后端开发（.NET 6.0 + Ivan.Data 框架）

遵循 `references/backend-guide.md` 的规范：

1. 创建 .NET 6.0 Web API 项目，核心包：`Ivan.Common`、`Ivan.Data`、`Ivan.Data.IOC`、`Ivan.IOC`（均从私有源还原）+ `System.Data.SqlClient`（按数据库选驱动）+ `Swashbuckle.AspNetCore` + `Microsoft.AspNetCore.Authentication.JwtBearer`。
2. **NuGet 源（必须）**：后端项目根目录必须包含 `nuget.config`（模板已内置）。`Ivan.*` 等内部库从私有源获取：`http://61.169.209.58:19081/repository/nuget-hosted/`（账号 `apps` / 密码 `123456`，凭据已写入 nuget.config 的 `packageSourceCredentials`，不要提交到公共仓库）。若用户需要其他 Ivan 开头的库，直接在 csproj 中添加 `PackageReference`，还原时自动走该源。
3. 配置 `appsettings.json` 中的 `ConnectionStrings:Default`、`Database:Provider`（`mssql`/`mysql`/`sqlite`）与 `Jwt` 配置节。
4. 按 `assets/backend-template/` 中模板搭建分层：Models（Generate/ + partial 扩展）→ DAL（Interface + BaseDAL 实现）→ BLL（Interface + BaseBLL 实现 + 手动 Service）→ Controllers。
5. **启动模式（必须，参照 SaminWeb）**：`Program.cs` 按四步固定流程编写——① `DatabaseInfo.SetMsSqlDatabase("default", conn)` 等注册连接串 → ② `ContextHelper.UseServiceProvider = false` + `builder.Host.AddIvanIOC(程序集数组, ...)` 扫描 `[InjectIOC]` 接口自动注册，回调中调用 `IOCInitExtensions.OnInit(builder)` → ③ 手动服务（`AuthService`/`UserManageService` 等）用 `services.AddScoped` 注册，其依赖的 `IUsersBLL` 等接口由容器解析 → ④ 中间件管道。**禁止**再直接注册带 `[InjectIOC]` 接口的实现类（如 `AddScoped<UsersBLL>()`）。
6. **using 引用（必须）**：本 skill 的后端为单项目分层（Models/DAL/BLL/Controllers 同在一个 csproj），生成各层代码时必须自动补充跨层 using，规则见 `references/backend-guide.md` 的"跨层 using 引用规则"一节（含 Ivan.Data/Ivan.Common 框架命名空间与各层 Interface 命名空间的完整对照表）。
7. 统一 API 响应格式（`ApiResult<T>`），统一异常处理中间件。
8. 若需要认证：启用 JWT Bearer 认证 + 用户/角色表 + 登录接口。
9. 启用 Swagger 便于联调。

## 第五步：数据库配置

按 `references/database-guide.md` 配置：

- 连接串写在 `appsettings.json` 的 `ConnectionStrings:Default`，`Database:Provider` 指定类型（`mssql` / `mysql` / `sqlite`）。
- `Program.cs` 启动时调用 `DatabaseInfo.SetMsSqlDatabase("default", conn)`（或 `SetMySqlDatabase` / `SetSqliteDatabase`）注册连接，key 固定 `"default"`。
- **不使用 EF Core 自动建表**：Ivan.Data 的 `BaseDAL` 只负责 CRUD，表结构需提前创建（手动执行 SQL 脚本或数据库管理工具）。
- 启用登录认证时，初始化脚本需内置默认管理员（admin / admin123），密码哈希必须与 `AuthService` 一致（PBKDF2/SHA256，格式 `base64(salt).base64(hash)`），规范见 `references/database-guide.md` 的"默认账户与密码哈希"章节；一体化初始化脚本参考模板 `assets/database/init_database.sql`（含全部表、种子数据与 admin 用户-角色绑定，幂等可重复执行）。

## 第六步：前端开发（Vue3 + TypeScript）

遵循 `references/frontend-guide.md` 的规范：

1. 按 `assets/frontend-template/` 模板初始化 Vite + Vue3 + TS 项目。
2. 安装依赖：`element-plus`、`vue-router`、`pinia`、`axios`、`@element-plus/icons-vue`。
3. 完成 Axios 封装（`assets/frontend-template/src/api/request.ts`）：baseURL、token 注入、统一错误处理、401 跳转登录。
4. 搭建布局框架（侧边栏菜单 + 顶栏 + 主内容区）与登录页。
5. 按功能模块逐个实现业务页面，统一走封装后的 API 层。

## 第七步：IIS 部署配置

按 `references/iis-deploy-guide.md` 配置：

1. **后端**：发布为独立/框架依赖包，站点根目录放置 `web.config`（见 `assets/web.config`），启用 ASP.NET Core Module (ANCM) 托管；若站点被其他应用占用端口或需要 URL 重写，注意处理。
2. **前端**：`npm run build` 后将 `dist/` 发布到站点，配 URL Rewrite 将非静态文件请求转发至后端 API（或后端设置 CORS 允许跨域直连）。
3. 记录每个站点所需配置（端口、物理路径、应用池 .NET CLR 版本设为"无托管代码"）。

## 错误处理、缓存与权限能力

本 Skill 内置以下三项关键能力，生成的项目默认包含完整实现：

### 1. 统一错误处理（前后端同步）

服务端所有响应（包括业务错误、权限不足、未处理异常）均通过 `ApiResult<T>` 统一包装，前端 `request.ts` 响应拦截器自动解包。

**错误码约定**：0 成功 / 非0 业务错误 / 401 未认证 / 403 权限不足 / 500 服务端异常。

**关键特性**：
- `ExceptionMiddleware` 全局捕获未处理异常 → `ApiResult.Fail(500, ex.Message)`
- `BusinessException` 用于 BLL 层业务校验失败 → `ApiResult.Fail(message, 400)`
- `PermAuthorizationFilter` 权限不足 → `ApiResult.Fail("权限不足", 403)` + HTTP 200（而非 `ForbidResult()`）
- 前端拦截器优先使用服务端返回的 `res.message`，确保错误信息前后端一致

详见 `references/backend-guide.md` 中"前后端错误处理统一策略"章节。

### 2. Ivan.Redis 缓存

项目提供两层缓存服务体系，支持开发/生产环境无缝切换。

**缓存服务**：
| 服务 | 用途 | 实现 |
|------|------|------|
| `ICacheService` | 通用缓存接口 | `MemoryCacheService`（开发）/ `RedisCacheService`（生产） |
| `MenuCacheService` | 菜单/权限专用缓存 | 基于 `IMemoryCache` 的进程级缓存 |

**缓存策略**：菜单树 30min / 权限编码 30min / 用户权限 10min / 单节点 10min

**异常安全**：所有 Redis 读写操作都有 `try/catch` 保护，Redis 不可用时降级查库，**不中断主流程**。

详见 `references/backend-guide.md` 中"Ivan.Redis 缓存配置"章节。

### 3. 按钮级别权限控制

从数据库 → 登录接口 → 前端状态 → 页面按钮 → 服务端 Action 的完整权限链路。

**数据结构（统一权限树模型）**：
- `Menus` 单表承载目录(1)/菜单(2)/按钮(3)三类节点：`MenuType` 区分；邻接表 `ParentId` + 路径枚举 `FullPath`（如 `/1/3/7`）+ 冗余 `Level`（根=0）
- **按钮节点**（MenuType=3）直接挂在菜单节点下，携带 `PermissionCode`（权限编码，两段式如 `users:edit`，全局唯一）+ `ControllerAction`（绑定的后端接口 `控制器.方法`，动态维护）；不设 Path/Icon
- `RoleMenus` 是**唯一**授权关联表（RoleId + MenuId）：勾选授权时目录/菜单/按钮一并写入，无需单独的按钮授权表
- 运行时权限判定与配置解耦：`[RequirePerm]` 只认编码，菜单管理里改 `PermissionCode` 后登录即可生效
- 数据库初始化：`assets/database/init_database.sql`（种子含 18 节点：系统管理目录 + 首页/用户/角色/菜单 4 菜单 + 13 按钮节点；admin 角色全授权）

**配置与分配入口**：
1. **菜单管理页（首选）**：树表"类型"列显示 `目录/菜单/按钮` 标签（按钮节点附 `PermissionCode` 标签），"绑定接口"列显示 `ControllerAction`；操作列"权限管理"打开按钮子节点表格弹窗（名称/权限编码/绑定接口/启用开关/编辑删除，"新增按钮"预填 `{resource}:` 前缀）；编辑/新增弹窗按 MenuType 动态切换字段（按钮节点显示权限编码 + 绑定接口下拉，下拉数据来自 `GET /api/menus/bindable-actions` 反射扫描，选项展示 HTTP 方法/路由/[RequirePerm] 声明编码供一致性核对）
2. **校验（后端 MenusBLL.ValidatePermissionConfig）**：按钮节点 PermissionCode 必填、两段式及以上（`^[a-zA-Z][a-zA-Z0-9_]*(:[a-zA-Z][a-zA-Z0-9_]*)+$`）、全局唯一（排除自身）；ControllerAction 选填但必须存在于扫描结果；菜单节点 Path 必填且全局唯一；目录/菜单节点不允许携带按钮属性
3. **角色权限分配弹窗**：统一权限树直接勾选（按钮节点显示其 PermissionCode 标签），勾选即自动保存（`PUT /api/roles/{id}/menus`，全量覆盖含按钮节点）
4. 所有写操作（Create/Modify/SetEnabled/Remove）级联失效 `MenuCacheService` + 清 `role_perm_codes_{roleId}` / `role_menus_{roleId}`（遍历角色）

**流程**：
1. `AuthService.Login()` 登录时汇总 `menuIds`（授权节点全集含按钮）+ `buttonPermissions`（`{ menuPath: [action] }`，action 取按钮节点 PermissionCode 的 `:` 后段归属父菜单 Path；admin 拥有全部菜单与按钮、缺 view 补 view）
2. 前端 `userStore` 存储权限数据；页面按钮统一用 `v-perm` 指令判定（底层走 `userStore.hasButton`）
3. 服务端 `[RequirePerm("users:edit")]` 注解声明 Action 所需权限（编码 = 按钮节点的 PermissionCode）
4. `PermAuthorizationFilter` 校验当前用户角色是否拥有任一所需权限（admin 角色直接放行）

**一个按钮 = 一条权限编码 = 三处一致**（Action 级权限对应关系）：
- 后端 Controller Action：`[RequirePerm("users:edit")]`
- 前端按钮：`<el-button v-perm="'edit'">`（action 部分与 PermissionCode 的 action 段相同）
- 数据库：`Menus` 表一条 MenuType=3 记录（PermissionCode=`users:edit`），且角色在 RoleMenus 中勾选了该按钮节点

**权限编码格式**：两段式 `{resource}:{action}`，resource 由菜单路径派生（去掉开头 `/`，`/` 替换为 `:`，如 `/users` → `users`），action 为按钮标识（如 `users:edit`），与 `RolePermissionService` 的编码体系一致；在菜单管理中编辑按钮节点可直接改 `PermissionCode`（全局唯一校验），改后 `[RequirePerm]` 需同步（或按按钮节点编码校验）。**按钮 ↔ 后端 Action 绑定**：按钮节点的 `ControllerAction` 字段（`控制器.方法`），下拉数据来自反射扫描（`GET /api/menus/bindable-actions`），选项内展示该 Action 的 HTTP 方法/路由/声明的 `[RequirePerm]` 编码，便于一致性核对；保存时后端校验存在性。

**前端按钮显隐**：页面按钮统一用全局 `v-perm` 指令（`src/directives/perm.ts`，已在 `main.ts` 注册）控制，不要用 `v-if="userStore.hasButton(...)"`（roles 页旧写法已迁移）：
- `<el-button v-perm="'add'">新增</el-button>` — 当前路由对应菜单无 `add` 按钮 Key 时元素被移除
- `<el-button v-perm="['edit','permission']">` — 任一 Key 命中即显示
- `<el-button v-perm:disable="'export'">导出</el-button>` — 无权限时**禁用（置灰）**而非移除，响应 updated 更新
- 判定基于登录接口返回的 `buttonPermissions = { menuPath: [buttonKey] }` 与当前路由 path；buttonKey 即按钮节点 PermissionCode 的 action 段，与后端 `[RequirePerm]` 的 action 段一致

**两个必踩的坑（模板已修复，自行编码时必须遵守）**：
1. **分页页码是 0-based**：`BaseDAL.SelectByPage` 生成的分页 SQL（MsSql `rownumber > page*size` / MySQL `limit page*size,` / SQLite `offset page*size`）假定页码从 0 开始。`PageSearchModel.Page` 是 1-based，传入前必须 `-1`，否则第 1 页会跳过前 N 条数据（total 正确但列表为空）。
2. **Action 级 [RequirePerm] 必须优先于 Controller 级**：`PermAuthorizationFilter` 不能用 `EndpointMetadata.OfType<RequirePerm>().FirstOrDefault()` 取注解——MVC 元数据中 Controller 特性排在 Action 特性之前，FirstOrDefault 永远取到类级注解，导致 `users:delete` 等按钮级权限全部失效。模板实现通过 `ControllerActionDescriptor.MethodInfo` 优先读取 Action 级注解。

**自定义 DAL SQL 必须用 DbHelper 而非 DbHelper.Connection**：`DbHelper.Query/Execute/ExecuteScalar` 自动携带会话事务；直接用 `DbHelper.Connection.Query`（裸 Dapper）在 BLL 事务内调用时报 "command has no transaction"，且 INSERT/UPDATE 列清单必须与表结构同步（新增列后漏改 UpdateSql 会导致"保存成功但值未持久化"）。

**标准分页写法**（DAL 内）：

```csharp
// SelectByPage 页码为 0-based，PageSearchModel.Page 为 1-based，必须 -1
var page = SelectByPage<Users>(dataSql, searchModel.Page - 1, searchModel.Limit,
    out totalRecordCount, param, "Id desc");
```

详见 `references/backend-guide.md` 中"按钮级别权限控制"章节。

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
- `database/` — 数据库初始化脚本模板（`init_database.sql`，与 backend-template 同级）
- `web.config` — IIS 托管 ASP.NET Core 后端所需配置模板

### scripts/（可执行脚本）
- 当前无需脚本；后续如需批量脚手架生成，在此目录添加。
