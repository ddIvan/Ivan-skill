# Ivan Skills

Ivan 的个人技能（Skills）仓库，为 AI 编程助手（CodeBuddy）提供一系列**基于数据库表的 .NET 代码生成**、**全栈 Web 项目开发**、**Electron 桌面应用开发**等能力。本仓库采用标准 Skills 结构：每个子技能一个目录，含 `SKILL.md`（技能定义与工作流）、`assets/`（模板等资源）、`references/`（加载进上下文的参考指南）。

## 仓库总览

| 子技能 | 分类 | 一句话定位 |
|--------|------|-----------|
| [`ican-db-gen`](./Ivan-db-gen) | 数据库代码生成 | 一键式从数据库生成 Model → DAL → BLL → Web 四层完整代码 |
| [`ivan-db-model-gen`](./Ivan-db-model-gen) | 数据库代码生成 | 读取表结构生成 .NET Model 层（实体类） |
| [`ivan-db-dal-gen`](./Ivan-db-dal-gen) | 数据库代码生成 | 读取表结构生成 .NET DAL 数据访问层 |
| [`ivan-db-bll-gen`](./Ivan-db-bll-gen) | 数据库代码生成 | 读取表结构生成 .NET BLL 业务逻辑层 |
| [`ivan-db-web-gen`](./Ivan-db-web-gen) | 数据库代码生成 | 读取表结构生成 ASP.NET Core MVC 的 Controller + View |
| [`ivan-fullstack-dev`](./Ivan-fullstack-dev) | Web 全栈开发 | 从零开发完整业务系统（前端 Vue3+TS + 后端 .NET6 + 数据库 + IIS 部署） |
| [`ivan-election`](./Ivan-election) | 桌面应用开发 | Electron + Vue 2.x + Element UI 桌面应用开发 |
| [`ivan-mcp-db-model-gen`](./ivan-mcp-db-model-gen) | MCP 服务 | Python MCP 服务：连接数据库，GUI 选表并**返回元数据**给调用方 |

## 技术生态

| 领域 | 技术 |
|------|------|
| 目标语言 | C#（.NET 6.0） |
| 分层架构 | Model → DAL → BLL（基于 **Ivan.Data** 框架：`BaseDAL` / `BaseBLL` + **Ivan.IOC** 依赖注入） |
| Web 后端 | ASP.NET Core Web API |
| Web 前端 | Vue 3 + TypeScript + Element Plus（构建：Vite） |
| 数据库 | SQLite / MSSQL / MySQL 三选一 |
| 桌面应用 | Electron + Vue 2.x + Element UI |
| 部署 | Windows IIS（ANCM 托管后端 + 前端静态发布） |
| 元数据来源 | `ivan-mcp-db-model-gen`（FastMCP + tkinter GUI） |

---

## 核心设计理念

所有数据库代码生成类 Skill 共享同一套设计哲学：

- **环境检测优先**：先检测是否安装 `ivan-mcp-db-model-gen` MCP 服务，已安装则跳过手动询问连接串，直接弹出 GUI 选表拿元数据；未安装则降级为手动交互。
- **只问没明确的**：仅就用户未指定（数据库/连接串/表名/输出目录/命名空间等）的关键选项提问，已明确的直接采用；用户回复"你看着办"时采用默认值并明示。
- **生成文件覆盖策略**：
  - `Generate/` 目录下的文件是**自动生成、每次覆盖**的（与数据库结构同步，不可手改）。
  - 外层 `partial` 扩展类和 `Interface/` 接口文件是**仅首次生成**的，用于保护开发者手动添加的代码。

---

## 子技能详解

### 1. `ivan-db-gen` — 一键式全栈代码生成

自动连接数据库、读取表结构，**按顺序自动调用下面 4 个子技能**，一次生成整套 .NET 三层 + Web 层代码：

| 序号 | 子 Skill | 每个表生成的文件 |
|------|----------|-----------------|
| 1 | `ivan-db-model-gen` | `Generate/{Table}.cs` + `{Table}.cs` |
| 2 | `ivan-db-dal-gen` | `Generate/{Table}DAL.cs` + `{Table}DAL.cs` + `Interface/I{Table}DAL.cs` |
| 3 | `ivan-db-bll-gen` | `{Table}BLL.cs` + `Interface/I{Table}BLL.cs` |
| 4 | `ivan-db-web-gen` | `Areas/{Area}/Controllers/{Table}Controller.cs` + `Views/{Table}/{Edit,List}.cshtml` |

> 触发词：一键生成 / 全栈生成 / 生成全部 / DB一键生成。

### 2. `ivan-db-model-gen` — Model 层生成

每个表生成两个 C# 文件：

- `Generate/{Table}.cs`：字段属性 + `<summary>` 注释 + `[Serializable]`，每次覆盖。
- `{Table}.cs`：空的 partial 扩展类，仅首次生成。

类型映射：数值类型默认非可空；`DateTime/TimeSpan/bool/Guid` 按数据库是否允许 NULL 决定是否可空；`string/byte[]` 本身可为 null。

> 触发词：生成Model / 生成模型 / 从数据库生成实体。

### 3. `ivan-db-dal-gen` — DAL 数据访问层生成

每个表生成三个文件（`Generate/{Table}DAL.cs` 每次覆盖，另两个仅首次生成），自动生成：

- `InsertSql` / `InsertSqlForGeneratedKey` / `DeleteSql` / `UpdateSql` / `SelectAllSql`（MSSQL 下使用 `(nolock)`）与按主键 `Select` 方法。
- 自动识别主键字段（MSSQL / MySQL / SQLite 各有对应识别规则），主键用于 WHERE 条件，Insert 排除自增主键。
- partial 扩展类含 `Search` 方法（IvanSQL 构建动态分页查询），接口带 `[InjectIOC]` 特性。

> 触发词：生成DAL / 生成数据层 / DAL代码生成。

### 4. `ivan-db-bll-gen` — BLL 业务逻辑层生成

每个表生成两个仅首次生成的文件：`{Table}BLL.cs` 与 `Interface/I{Table}BLL.cs`，包含 `Add / Select / Update / Delete / GetALL / List / Create` 方法，业务类继承 `BaseBLL<I{Table}DAL, {Table}>`。

> 触发词：生成BLL / 生成业务层 / BLL代码生成。

### 5. `ivan-db-web-gen` — Web 层生成

每个表生成三个仅首次生成的文件：`{Table}Controller.cs`（Index / List / Add / Edit / Delete 五个 Action）、`Views/{Table}/Edit.cshtml`（el-form 表单）、`Views/{Table}/List.cshtml`（el-table 列）。

> 触发词：生成Web层 / 生成MVC / 生成Controller / 生成View。

### 6. `ivan-fullstack-dev` — 全栈 Web 项目开发

从零开发一个完整业务系统，覆盖全流程：

1. **需求澄清** → 确认项目名/端口、数据库、UI 库、认证、部署方式、功能模块；有默认值速查表（`references/questionnaire.md`）。
2. **代码生成位置确认** → 必选绝对路径，绝不擅自落盘。
3. **项目初始化** → 创建 `database/` + `frontend/` + `backend/` 结构。
4. **后端**：.NET 6.0 Web API + Ivan.Data 分层（Model/DAL/BLL/Controller），`Program.cs` 通过 `AddIvanIOC` 自动扫描 `[InjectIOC]` 接口实现，提供 `AuthService` / `UserManageService` 示例、JWT 登录、统一 `ApiResult` 与异常中间件。
5. **前端**：Vue 3 + TS + Element Plus，封装 Axios、登录守卫、Pinia、布局+路由+菜单。
6. **部署**：IIS 发布（后端 ANCM）+ 前端静态发布（`references/iis-deploy-guide.md`）。

> 触发词：开发系统 / 开发项目 / 新建项目 / 做一个管理系统。

### 7. `ivan-election` — Electron 桌面应用开发

基于 Electron + Vue 2.x + Element UI 的桌面应用开发 Skill，提供项目初始化、组件封装、页面模板生成、路由配置、状态管理等能力。

**核心约束**：禁止使用 `alert()` / `confirm()` / `prompt()` 等浏览器原生对话框，必须使用自定义对话框组件，避免 Electron 窗口焦点丢失。

> 触发词：electron项目 / electron开发 / electron+vue / 创建electron应用。

### 8. `ivan-mcp-db-model-gen` — MCP 数据库元数据服务

独立的 Python MCP 服务（FastMCP + stdio 协议），是上述 DB 生成类 Skill 的**元数据来源**：

- 弹出 tkinter GUI 让用户选数据库类型、填连接串、选表、预览 C# 类型映射，确认后**返回字段元数据 JSON**（不生成代码）。
- 提供 3 个 tool：`db_model_gen_gui`、`get_type_mapping`、`get_type_mapping_rules`。
- 安装：`pip install -e .`；作为 MCP 服务在 CodeBuddy / Claude Desktop 中配置运行。

## 目录结构与约定

```
Ivan-skills/
├── README.md                      # 本文件
├── <skill-name>/
│   ├── SKILL.md                   # 技能定义（name/description）+ 工作流决策树 + 步骤
│   ├── assets/                    # 代码生成模板 / 前后端项目模板
│   ├── references/                # 加载进上下文的参考指南（连接、类型映射、部署等）
│   └── README.md                  # （部分子技能）单独的使用说明
├── ivan-mcp-db-model-gen/         # 独立的 Python MCP 服务项目
└── .gitignore
```

> 注意：`Ivan-fullstack-dev/assets/` 下的 `backend-template` / `frontend-template` 是可直接复制到业务项目使用的**完整前后端模板**（含教材级示例 Users 模块）；`assets/web.config` 用于 IIS 部署。