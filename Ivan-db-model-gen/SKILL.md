---
name: ivan-db-model-gen
description: Ivan 的数据库 Model 层代码生成 Skill。自动连接指定数据库（SQLite/MSSQL/MySQL），读取表结构，生成 .NET Model 层代码。每个表生成两个文件：Generate/xxx.cs（根据数据库字段自动生成，不可手动修改）和 xxx.cs（partial 扩展类，供手动扩展）。触发词：生成Model/生成模型/数据库生成Model/DB生成代码/从数据库生成实体等。生成前必须先询问用户数据库连接信息、输出目录等关键选项，得到确认后再动手。
---

# Ivan DB Model Generator

## Overview

自动连接数据库，读取指定表的结构，生成 .NET Model 层代码。每个数据库表生成两个 C# 文件：

- **`Generate/{TableName}.cs`**：根据数据库字段自动生成，包含所有字段属性、注释、`[Serializable]` 特性。此文件由工具自动维护，**不可手动修改**。
- **`{TableName}.cs`**：partial 扩展类，与 Generate 中的类配对，供开发者手动添加扩展逻辑。此文件仅首次生成，后续不会覆盖。

支持三种数据库：SQLite、MSSQL、MySQL。

## 工作流决策树

```
用户提出生成 Model 需求
        │
        ▼
┌─ 环境检测 ───────────────────────────────────┐
│ 检测用户环境中是否已安装 MCP 服务              │
│ ivan-mcp-db-model-gen                        │
│                                              │
│ 检测方法：                                    │
│ 1. 检查当前可用的 MCP tools 列表中是否存在     │
│    db_model_gen_gui / get_type_mapping 等     │
│ 2. 或尝试调用 get_type_mapping_rules()        │
│    看是否能成功返回结果                        │
└──────────────────────────────────────────────┘
        │
        ├─── MCP 已安装 ──────────────────────────┐
        │                                         │
        │  【MCP 模式 - GUI 交互获取元数据】        │
        │                                         │
        │  ① 跳过询问数据库类型和连接串             │
        │     → 直接调用 db_model_gen_gui()        │
        │     → 用户在 GUI 中自行填写：             │
        │       数据库类型、连接串、选表、预览      │
        │     → 点击「确认选择，返回元数据」         │
        │                                         │
        │  ② 异常处理与超时机制（详见下方）          │
        │     ├─ 超时（>60s）→ 提示用户手动完成     │
        │     ├─ 用户取消 → 降级到手动模式          │
        │     ├─ 连接失败 → 提示错误，允许重试       │
        │     └─ 返回空表 → 提示并允许重试          │
        │                                         │
        │  ③ 拿到元数据后，询问输出目录和命名空间     │
        │     （用户已指定的直接采用）               │
        │                                         │
        │  ④ 本 Skill 根据元数据生成代码文件         │
        │                                         │
        └─────────────────────────────────────────┘
        │
        │  MCP 未安装 / MCP 调用失败降级
        ▼
┌─ 信息收集（手动模式）─────────────────────────┐
│ 依次确认：                                    │
│ 1. 数据库类型：SQLite / MSSQL / MySQL        │
│ 2. 数据库连接信息（连接字符串或文件路径）      │
│ 3. 要生成 Model 的表名（支持多表，逗号分隔）   │
│ 4. 输出目录（Model 代码存放路径）             │
│ 5. 命名空间（namespace）                      │
│ 6. 是否生成全部表（* 表示所有表）             │
└──────────────────────────────────────────────┘
        │  仅询问用户未明确给出的选项；已明确的直接采用
        ▼
┌─ 连接数据库 ─────────────────────────────────┐
│ 根据数据库类型建立连接，验证连通性             │
│ 读取表结构：表名、字段名、数据类型、注释       │
└──────────────────────────────────────────────┘
        │
        ▼
┌─ 生成代码（两种模式共用）─────────────────────┐
│ 对每个表：                                    │
│ 1. 生成/覆盖 Generate/{TableName}.cs         │
│    - 已存在 → 直接替换                       │
│    - 不存在 → 新建                           │
│ 2. 检查 {TableName}.cs 是否存在              │
│    - 不存在 → 生成空的 partial 扩展类         │
│    - 已存在 → 跳过（保留用户手动修改）         │
└──────────────────────────────────────────────┘
        │
        ▼
┌─ 输出结果 ───────────────────────────────────┐
│ 汇总生成结果：生成了哪些文件，跳过了哪些文件   │
└──────────────────────────────────────────────┘
```

## 第零步：环境检测（必须首先执行）

在开始任何信息收集之前，**必须先检测用户环境中是否已安装 `ivan-mcp-db-model-gen` MCP 服务**。

### 检测方法

1. **检查当前可用的 MCP tools 列表**：查看当前会话中是否存在以下 MCP tools：
   - `db_model_gen_gui`
   - `get_type_mapping`
   - `get_type_mapping_rules`

2. **主动探测**：尝试调用 `get_type_mapping_rules()` tool，如果返回了有效的 JSON 映射规则，则说明 MCP 服务已安装且可用。

### 分支处理

#### 分支 A：MCP 服务已安装 → 使用 MCP 获取元数据

如果检测到 MCP 服务可用，按以下步骤操作：

**步骤 1：调用 MCP 获取元数据**

调用 `db_model_gen_gui()` tool → 弹出 GUI 窗口，用户在 GUI 中：
- 选择数据库类型（SQLite/MSSQL/MySQL）
- 输入连接字符串
- 点击"连接数据库"查看所有表
- 多选要生成 Model 的表
- 预览字段及 C# 类型映射
- 点击「确认选择，返回元数据」

MCP 返回的 JSON 格式（**仅元数据，不含代码**）：
```json
{
  "success": true,
  "db_type": "mssql",
  "tables": {
    "Users": [
      {"name": "Id", "db_type": "int", "is_nullable": false, "description": "主键ID"},
      {"name": "Name", "db_type": "varchar", "is_nullable": true, "description": "用户名"},
      {"name": "CreatedAt", "db_type": "datetime", "is_nullable": true, "description": ""}
    ],
    "Orders": [
      {"name": "Id", "db_type": "int", "is_nullable": false, "description": ""},
      {"name": "Amount", "db_type": "decimal", "is_nullable": true, "description": "金额"}
    ]
  }
}
```

如果用户取消 GUI 操作，返回 `{"success": false, "message": "用户取消了操作"}`，告知用户已取消。

**步骤 2：询问输出配置**

MCP 不负责输出目录和命名空间，需要向用户确认：
- **输出目录**：Model 代码存放的根目录路径（如 `D:\project\MyApp\Models\`）
- **命名空间**：生成的 Model 类使用的 namespace（如 `MyApp.Models`）

如果用户已在请求中明确指定，直接采用。

**步骤 3：本 Skill 根据元数据生成代码**

拿到 MCP 返回的元数据后，本 Skill 负责：
1. 对每个表，根据字段元数据生成 `Generate/{TableName}.cs`（使用 `assets/templates/GenerateTemplate.cs` 模板）
2. 对每个表，检查 `{TableName}.cs` 是否存在，不存在则生成空的 partial 扩展类
3. 类型映射使用 `get_type_mapping_rules()` 返回的规则（或本 Skill 内置的映射表）
4. 汇总输出生成结果

**MCP 模式下的职责划分**：
| 职责 | 负责方 |
|------|--------|
| 连接数据库、读取表结构 | MCP 服务 (`ivan-mcp-db-model-gen`) |
| 弹出 GUI 让用户选表 | MCP 服务 (`ivan-mcp-db-model-gen`) |
| 返回表/字段元数据 | MCP 服务 (`ivan-mcp-db-model-gen`) |
| 类型映射规则 | MCP 服务提供，本 Skill 也可内置 |
| 询问输出目录、命名空间 | 本 Skill (`ivan-db-model-gen`) |
| 生成 .cs 代码文件 | 本 Skill (`ivan-db-model-gen`) |
| 汇总展示结果 | 本 Skill (`ivan-db-model-gen`) |

#### 分支 B：MCP 服务未安装 → 使用手动交互模式

如果未检测到 MCP 服务，则按照原有的手动交互流程继续（见下方"第一步：信息收集"）。

## 第一步：信息收集（仅手动模式，MCP 模式下跳过）

> **注意**：此步骤仅在 MCP 服务未安装时执行。如果 MCP 服务已安装，直接调用 `db_model_gen_gui()` 即可，跳过此步骤。

收到生成 Model 的请求后，先列出所有**用户未明确指定**的关键选项，用提问的方式逐项确认。已由用户明确指定的选项直接采用，不要重复询问。

必问项（除非已明确）：

1. **数据库类型**：SQLite / MSSQL / MySQL。必须明确指定。
2. **数据库连接信息**：
   - SQLite：数据库文件路径（如 `D:\data\mydb.db`）
   - MSSQL：连接字符串（如 `Server=.;Database=MyDb;Trusted_Connection=True;`）
   - MySQL：连接字符串（如 `Server=localhost;Database=MyDb;User=root;Password=123456;`）
3. **目标表名**：要生成 Model 的表名，多个表用逗号分隔。输入 `*` 表示生成数据库中所有表的 Model。
4. **输出目录**：Model 代码存放的根目录路径（如 `D:\project\MyApp\Models\`）。Generate 子目录会自动创建。
5. **命名空间**：生成的 Model 类使用的 namespace（如 `MyApp.Models`）。

> 若用户回复"你看着办"或不做选择，则无法继续，必须明确以上信息后才能开始生成。

## 第二步：连接数据库并读取表结构

根据用户指定的数据库类型，使用对应的连接方式读取表结构。

### SQLite

使用 `System.Data.SQLite` 或 `Microsoft.Data.Sqlite` 连接：

```sql
-- 获取所有表名
SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;

-- 获取指定表的字段信息
PRAGMA table_info('{TableName}');
-- 返回：cid, name, type, notnull, dflt_value, pk
```

### MSSQL

使用 `Microsoft.Data.SqlClient` 连接：

```sql
-- 获取所有用户表名
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME;

-- 获取指定表的字段信息
SELECT 
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.IS_NULLABLE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.NUMERIC_PRECISION,
    c.NUMERIC_SCALE,
    ISNULL(ep.value, '') AS DESCRIPTION
FROM INFORMATION_SCHEMA.COLUMNS c
LEFT JOIN sys.extended_properties ep 
    ON ep.major_id = OBJECT_ID(c.TABLE_NAME) 
    AND ep.minor_id = c.ORDINAL_POSITION 
    AND ep.name = 'MS_Description'
WHERE c.TABLE_NAME = '{TableName}'
ORDER BY c.ORDINAL_POSITION;
```

### MySQL

使用 `MySql.Data.MySqlClient` 连接：

```sql
-- 获取所有表名
SHOW TABLES;

-- 获取指定表的字段信息
SHOW FULL COLUMNS FROM `{TableName}`;
-- 返回：Field, Type, Collation, Null, Key, Default, Extra, Privileges, Comment
```

## 第三步：类型映射

数据库字段类型到 C# 类型的映射规则：

| SQLite | MSSQL | MySQL | C# Type |
|--------|-------|-------|---------|
| INTEGER (PK auto) | int (IDENTITY) | int (AUTO_INCREMENT) | `int` |
| INTEGER | int | int | `int` |
| BIGINT | bigint | bigint | `long` |
| SMALLINT | smallint | smallint | `short` |
| TINYINT | tinyint | tinyint | `byte` |
| REAL / FLOAT | float / real | float | `float` |
| DOUBLE | float(53) | double | `double` |
| NUMERIC / DECIMAL | decimal / numeric / money | decimal | `decimal` |
| TEXT / CLOB | ntext / text | text / longtext | `string` |
| VARCHAR / NVARCHAR | nvarchar / varchar | varchar | `string` |
| CHAR / NCHAR | nchar / char | char | `string` |
| BLOB | varbinary / image | blob | `byte[]` |
| DATETIME | datetime / datetime2 | datetime | `DateTime` |
| DATE | date | date | `DateTime` |
| TIME | time | time | `TimeSpan` |
| BIT / BOOLEAN | bit | bit / bool | `bool` |
| UNIQUEIDENTIFIER | uniqueidentifier | — | `Guid` |

**可空类型处理**：
- **数值类型（int、long、short、byte、float、double、decimal）默认不使用可空类型**，即使数据库字段允许 NULL，也生成非可空类型（如 `int`、`long`、`decimal` 等）。
- **日期时间类型（DateTime、DateTimeOffset、TimeSpan）和 bool、Guid**：如果数据库字段允许 NULL，则使用可空类型（`DateTime?`、`bool?`、`Guid?` 等）。
- 引用类型（string、byte[]）本身可为 null，不需要额外处理。

## 第四步：生成代码

### 文件 1：Generate/{TableName}.cs

此文件根据数据库字段自动生成，包含完整的属性定义。模板见 `assets/templates/GenerateTemplate.cs`。

生成规则：
- **如果文件已存在，直接覆盖替换**，确保与数据库结构同步。
- 类名 = 表名（保持原样，不做转换）
- 类带 `[Serializable()]` 特性
- 类声明为 `public partial class {TableName}`
- 包含无参构造函数
- 每个字段生成一个属性，使用 `get; set;` 自动属性
- 如果数据库字段有注释（如 MSSQL 的 MS_Description、MySQL 的 Comment），生成 `<summary>` XML 注释
- 属性名 = 字段名（保持原样）
- 属性类型按类型映射表转换
- 所有属性放在 `#region Public member` 区域中

### 文件 2：{TableName}.cs

此文件为 partial 扩展类，供开发者手动添加扩展逻辑。

生成规则：
- **如果文件已存在，跳过不生成**，保护用户手动添加的扩展代码。
- 仅在文件不存在时才创建新文件。
- 类声明为 `public partial class {TableName}`
- 类体为空，仅包含基本的 using 语句和 namespace

模板见 `assets/templates/PartialTemplate.cs`。

### 文件命名与目录结构

```
{输出目录}/
├── Generate/
│   ├── Table1.cs
│   ├── Table2.cs
│   └── ...
├── Table1.cs
├── Table2.cs
└── ...
```

## 第五步：输出结果汇总

生成完成后，汇总输出：

- 成功生成了哪些 `Generate/xxx.cs` 文件
- 新创建了哪些 `xxx.cs` 扩展文件
- 跳过了哪些已存在的 `xxx.cs` 扩展文件
- 如有错误，列出失败的表及原因

## 注意事项

1. **Generate 目录下的文件每次都会被覆盖**，确保与数据库结构同步。
2. **外层 partial 文件仅首次生成**，后续不会覆盖，保护用户手动添加的代码。
3. 表名和字段名保持数据库中的原始命名，不做任何转换（如单复数、大小写等）。
4. 如果数据库字段没有注释，生成的属性不包含 `<summary>` 注释。
5. 主键字段不特殊处理，与其他字段一样生成属性。

## Resources

本 Skill 附带以下资源：

### references/（加载进上下文的指南）
- `db-connection-guide.md` — SQLite / MSSQL / MySQL 连接方式与 NuGet 包参考
- `type-mapping.md` — 详细的数据库类型到 C# 类型映射表

### assets/templates/（代码生成模板）
- `GenerateTemplate.cs` — Generate/xxx.cs 文件模板
- `PartialTemplate.cs` — 外层 xxx.cs partial 扩展类模板
