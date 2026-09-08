---
name: ivan-db-dal-gen
description: Ivan 的数据库 DAL 层代码生成 Skill。自动连接指定数据库（SQLite/MSSQL/MySQL），读取表结构，生成 .NET DAL 层代码。每个表生成三个文件：Generate/{TableName}DAL.cs（自动生成，可覆盖）、{TableName}DAL.cs（partial 扩展类，仅首次生成）、Interface/I{TableName}DAL.cs（接口，仅首次生成）。触发词：生成DAL/生成数据层/数据库生成DAL/DAL代码生成等。生成前必须先询问用户数据库连接信息、输出目录等关键选项，得到确认后再动手。
---

# Ivan DB DAL Generator

## Overview

自动连接数据库，读取指定表的结构，生成 .NET DAL 层代码。每个数据库表生成三个 C# 文件：

- **`Generate/{TableName}DAL.cs`**：根据数据库字段自动生成，包含 InsertSql、InsertSqlForGeneratedKey、DeleteSql、UpdateSql、SelectAllSql、Select 方法。此文件由工具自动维护，**每次覆盖**。
- **`{TableName}DAL.cs`**：partial 扩展类，与 Generate 中的类配对，供开发者手动添加扩展逻辑（如 Search 方法）。此文件**仅首次生成**，后续不会覆盖。
- **`Interface/I{TableName}DAL.cs`**：DAL 接口文件，默认只包含 Search 函数签名。此文件**仅首次生成**，后续不会覆盖。

支持三种数据库：SQLite、MSSQL、MySQL。

## 工作流决策树

```
用户提出生成 DAL 需求
        │
        ▼
┌─ 环境检测 ───────────────────────────────────┐
│ 检测是否已安装 MCP 服务 ivan-mcp-db-model-gen │
│ - 已安装 → 跳过询问数据库信息，直接调用 GUI   │
│ - 未安装 → 进入手动信息收集流程               │
└──────────────────────────────────────────────┘
        │
        ▼
┌─ 信息收集 ───────────────────────────────────┐
│ 依次确认：                                    │
│ 1. 数据库类型：SQLite / MSSQL / MySQL        │
│ 2. 数据库连接信息（连接字符串或文件路径）      │
│ 3. 要生成 DAL 的表名（支持多表，逗号分隔）     │
│ 4. 输出目录（DAL 代码存放路径）               │
│ 5. 命名空间（namespace）                      │
│ 6. Models 命名空间（Model 类所在的 namespace） │
└──────────────────────────────────────────────┘
        │  仅询问用户未明确给出的选项；已明确的直接采用
        ▼
┌─ 连接数据库 ─────────────────────────────────┐
│ 根据数据库类型建立连接，验证连通性             │
│ 读取表结构：表名、字段名、数据类型、注释       │
│ 识别主键字段（用于 Delete/Update/Select）     │
└──────────────────────────────────────────────┘
        │
        ▼
┌─ 生成代码 ───────────────────────────────────┐
│ 对每个表：                                    │
│ 1. 生成/覆盖 Generate/{TableName}DAL.cs      │
│    - 已存在 → 直接替换                       │
│    - 不存在 → 新建                           │
│ 2. 检查 {TableName}DAL.cs 是否存在           │
│    - 不存在 → 生成含 Search 方法的扩展类      │
│    - 已存在 → 跳过（保留用户手动修改）         │
│ 3. 检查 Interface/I{TableName}DAL.cs 是否存在│
│    - 不存在 → 生成含 Search 签名的接口        │
│    - 已存在 → 跳过（保留用户手动修改）         │
└──────────────────────────────────────────────┘
        │
        ▼
┌─ 输出结果 ───────────────────────────────────┐
│ 汇总生成结果：生成了哪些文件，跳过了哪些文件   │
└──────────────────────────────────────────────┘
```

## 第一步：环境检测（必须执行）

收到生成 DAL 的请求后，首先检测 MCP 服务 `ivan-mcp-db-model-gen` 是否可用：

- **如果 MCP 服务已安装**：跳过询问数据库类型和连接串的步骤，直接调用 `db_model_gen_gui` 工具弹出 GUI 界面。用户在 GUI 中填写数据库信息、选择表后，返回表/字段元数据。
- **如果 MCP 服务未安装**：进入手动信息收集流程，逐项询问用户。

## 第二步：信息收集

### MCP 模式（已安装 ivan-mcp-db-model-gen）

直接调用 `db_model_gen_gui`，用户在 GUI 中完成：
- 选择数据库类型
- 输入连接字符串
- 选择要生成 DAL 的表
- 预览字段

GUI 返回元数据后，仍需确认以下选项（GUI 不包含的）：
- **输出目录**：DAL 代码存放的根目录路径
- **命名空间**：DAL 类使用的 namespace
- **Models 命名空间**：Model 类所在的 namespace（用于 using 引用）

### 手动模式（未安装 MCP）

必问项（除非已明确）：

1. **数据库类型**：SQLite / MSSQL / MySQL
2. **数据库连接信息**：
   - SQLite：数据库文件路径
   - MSSQL：连接字符串
   - MySQL：连接字符串
3. **目标表名**：要生成 DAL 的表名，多个表用逗号分隔。输入 `*` 表示所有表
4. **输出目录**：DAL 代码存放的根目录路径
5. **命名空间**：DAL 类使用的 namespace
6. **Models 命名空间**：Model 类所在的 namespace

> 若用户回复"你看着办"或不做选择，则无法继续，必须明确以上信息后才能开始生成。

## 第三步：连接数据库并读取表结构

与 ivan-db-model-gen 完全一致，使用相同的数据库连接和表结构读取逻辑。

**关键**：需要额外识别主键字段，用于生成 DeleteSql、UpdateSql 和 Select 方法中的 WHERE 条件。

### 主键识别规则

- **MSSQL**：查询 `INFORMATION_SCHEMA.KEY_COLUMN_USAGE` 或使用 `sys.indexes` 识别主键
- **MySQL**：`SHOW FULL COLUMNS` 中 Key = 'PRI' 的字段
- **SQLite**：`PRAGMA table_info` 中 pk > 0 的字段

如果无法识别主键，默认使用第一个字段作为主键。

## 第四步：生成代码

### 文件 1：Generate/{TableName}DAL.cs

此文件根据数据库字段自动生成，包含 SQL 语句和基础方法。模板见 `assets/templates/GenerateDALTemplate.cs`。

生成内容：
- **InsertSql**：INSERT 语句，排除自增主键字段
- **InsertSqlForGeneratedKey**：InsertSql + `;select SCOPE_IDENTITY();`（仅 MSSQL）
- **DeleteSql**：DELETE 语句，WHERE 条件使用主键
- **UpdateSql**：UPDATE 语句，SET 所有非主键字段，WHERE 条件使用主键
- **SelectAllSql**：`select * from {TableName} (nolock)`（MSSQL）或 `select * from {TableName}`
- **Select 方法**：按主键查询单个对象

生成规则：
- **如果文件已存在，直接覆盖替换**
- 类声明为 `public partial class {TableName}DAL : BaseDAL<{TableName}>, I{TableName}DAL`
- 包含两个构造函数：无参和带 `IDbHelper` 参数
- 所有 SQL 属性使用 `protected override string`
- Select 方法使用 `public override`

### 文件 2：{TableName}DAL.cs

此文件为 partial 扩展类，供开发者手动添加扩展逻辑。

生成内容：
- **Search 方法**：分页查询方法，使用 IvanSQL 构建动态查询

生成规则：
- **如果文件已存在，跳过不生成**
- 类声明为 `public partial class {TableName}DAL`
- 包含 `using Ivan.Data.SQLBuilder;` 引用
- Search 方法签名：`public List<{TableName}> Search(PageSearchModel searchModel, out int totalRecordCount)`

模板见 `assets/templates/PartialDALTemplate.cs`。

### 文件 3：Interface/I{TableName}DAL.cs

此文件为 DAL 接口，定义数据访问契约。

生成内容：
- **Search 方法签名**

生成规则：
- **如果文件已存在，跳过不生成**
- 接口声明为 `public interface I{TableName}DAL : IBaseDAL<{TableName}>`
- 带 `[Ivan.IOC.Core.Attributes.InjectIOC]` 特性
- 包含 Search 方法签名

模板见 `assets/templates/InterfaceDALTemplate.cs`。

### 文件命名与目录结构

```
{输出目录}/
├── Generate/
│   ├── Table1DAL.cs
│   ├── Table2DAL.cs
│   └── ...
├── Interface/
│   ├── ITable1DAL.cs
│   ├── ITable2DAL.cs
│   └── ...
├── Table1DAL.cs
├── Table2DAL.cs
└── ...
```

## 第五步：输出结果汇总

生成完成后，汇总输出：

- 成功生成了哪些 `Generate/xxxDAL.cs` 文件
- 新创建了哪些 `xxxDAL.cs` 扩展文件
- 新创建了哪些 `Interface/IxxxDAL.cs` 接口文件
- 跳过了哪些已存在的文件
- 如有错误，列出失败的表及原因

## 注意事项

1. **Generate 目录下的文件每次都会被覆盖**，确保与数据库结构同步。
2. **外层 partial 文件和 Interface 文件仅首次生成**，后续不会覆盖，保护用户手动添加的代码。
3. 表名和字段名保持数据库中的原始命名，不做任何转换。
4. InsertSql 排除自增主键字段（通常是第一个字段 ID）。
5. DeleteSql 和 UpdateSql 的 WHERE 条件使用主键字段。
6. SelectAllSql 在 MSSQL 下使用 `(nolock)` 提示。
7. 如果数据库字段没有注释，生成的属性不包含 `<summary>` 注释。

## Resources

本 Skill 附带以下资源：

### assets/templates/（代码生成模板）
- `GenerateDALTemplate.cs` — Generate/xxxDAL.cs 文件模板
- `PartialDALTemplate.cs` — 外层 xxxDAL.cs partial 扩展类模板
- `InterfaceDALTemplate.cs` — Interface/IxxxDAL.cs 接口模板
