---
name: ivan-db-bll-gen
description: Ivan 的数据库 BLL 层代码生成 Skill。自动连接指定数据库（SQLite/MSSQL/MySQL），读取表结构，生成 .NET BLL 层代码。每个表生成两个文件：{TableName}BLL.cs（业务逻辑类，仅首次生成）、Interface/I{TableName}BLL.cs（接口，仅首次生成）。触发词：生成BLL/生成业务层/数据库生成BLL/BLL代码生成等。生成前必须先询问用户数据库连接信息、输出目录等关键选项，得到确认后再动手。
---

# Ivan DB BLL Generator

## Overview

自动连接数据库，读取指定表的结构，生成 .NET BLL 层代码。每个数据库表生成两个 C# 文件：

- **`{TableName}BLL.cs`**：业务逻辑类，包含 Add、Select、Update、Delete、GetALL、List、Create 方法。此文件**仅首次生成**，后续不会覆盖。
- **`Interface/I{TableName}BLL.cs`**：BLL 接口文件，默认只包含 List、GetALL、Create 函数签名。此文件**仅首次生成**，后续不会覆盖。

支持三种数据库：SQLite、MSSQL、MySQL。

## 工作流决策树

```
用户提出生成 BLL 需求
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
│ 3. 要生成 BLL 的表名（支持多表，逗号分隔）     │
│ 4. 输出目录（BLL 代码存放路径）               │
│ 5. 命名空间（namespace）                      │
│ 6. Interface 命名空间（接口所在的 namespace）  │
│ 7. Models 命名空间（Model 类所在的 namespace） │
│ 8. DAL 命名空间（DAL 类所在的 namespace）      │
└──────────────────────────────────────────────┘
        │  仅询问用户未明确给出的选项；已明确的直接采用
        ▼
┌─ 获取表结构元数据 ───────────────────────────┐
│ 调用 ivan-mcp-db-model-gen 的                │
│ db_model_gen_gui 工具获取表/字段元数据        │
│ 返回 JSON：{ tables: { TableName: [...] } }  │
└──────────────────────────────────────────────┘
        │
        ▼
┌─ 生成代码（由 Skill 直接执行） ──────────────┐
│ 对每个表，根据 assets/templates/ 中的模板：   │
│ 1. 检查 {TableName}BLL.cs 是否存在           │
│    - 不存在 → 生成业务逻辑类                  │
│    - 已存在 → 跳过（保留用户手动修改）         │
│ 2. 检查 Interface/I{TableName}BLL.cs 是否存在│
│    - 不存在 → 生成含 List/GetALL/Create 接口  │
│    - 已存在 → 跳过（保留用户手动修改）         │
└──────────────────────────────────────────────┘
        │
        ▼
┌─ 输出结果 ───────────────────────────────────┐
│ 汇总生成结果：生成了哪些文件，跳过了哪些文件   │
└──────────────────────────────────────────────┘
```

## 第一步：环境检测（必须执行）

收到生成 BLL 的请求后，首先检测 MCP 服务 `ivan-mcp-db-model-gen` 是否可用：

- **如果 MCP 服务已安装**：跳过询问数据库类型和连接串的步骤，直接调用 `db_model_gen_gui` 工具弹出 GUI 界面。用户在 GUI 中填写数据库信息、选择表后，返回表/字段元数据。
- **如果 MCP 服务未安装**：进入手动信息收集流程，逐项询问用户。

## 第二步：信息收集

### MCP 模式（已安装 ivan-mcp-db-model-gen）

直接调用 `db_model_gen_gui`，用户在 GUI 中完成：
- 选择数据库类型
- 输入连接字符串
- 选择要生成 BLL 的表
- 预览字段

GUI 返回元数据后，仍需确认以下选项（GUI 不包含的）：
- **输出目录**：BLL 代码存放的根目录路径
- **命名空间**：BLL 类使用的 namespace（如 `Samin.BLL`）
- **Interface 命名空间**：接口类使用的 namespace（如 `Samin.BLL.Interface`）
- **Models 命名空间**：Model 类所在的 namespace（用于 using 引用）
- **DAL 命名空间**：DAL 类所在的 namespace（用于 using 引用）

### 手动模式（未安装 MCP）

必问项（除非已明确）：

1. **数据库类型**：SQLite / MSSQL / MySQL
2. **数据库连接信息**：
   - SQLite：数据库文件路径
   - MSSQL：连接字符串
   - MySQL：连接字符串
3. **目标表名**：要生成 BLL 的表名，多个表用逗号分隔。输入 `*` 表示所有表
4. **输出目录**：BLL 代码存放的根目录路径
5. **命名空间**：BLL 类使用的 namespace
6. **Interface 命名空间**：接口类使用的 namespace
7. **Models 命名空间**：Model 类所在的 namespace
8. **DAL 命名空间**：DAL 类所在的 namespace

> 若用户回复"你看着办"或不做选择，则无法继续，必须明确以上信息后才能开始生成。

## 第三步：获取表结构元数据

调用 `ivan-mcp-db-model-gen` 的 `db_model_gen_gui` 工具，获取所选表的字段元数据。

返回格式：
```json
{
  "success": true,
  "db_type": "mssql",
  "tables": {
    "TableName": [
      {"name": "Id", "db_type": "int", "is_nullable": false, "description": ""},
      {"name": "Name", "db_type": "varchar", "is_nullable": true, "description": "名称"}
    ]
  }
}
```

BLL 层代码生成**不依赖具体字段**，只需要表名即可生成完整的业务逻辑类。字段元数据仅用于确认表存在和获取表名。

## 第四步：生成代码

代码生成由 Skill 直接执行（不依赖额外的 MCP 服务），根据 `assets/templates/` 中的模板生成文件。

### 文件 1：{TableName}BLL.cs

此文件为业务逻辑类，继承 `BaseBLL<I{TableName}DAL, {TableName}>`，实现 `I{TableName}BLL` 接口。模板见 `assets/templates/BLLTemplate.cs`。

生成内容：
- **Add 方法**：`public override ResultModel Add({TableName} model)` — 调用 `base.Add(model)`
- **Select 方法**：`public override ResultModel<{TableName}> Select({TableName} model)` — 调用 `base.Select(model)`
- **Update 方法**：`public override ResultModel Update({TableName} model)` — 调用 `base.Update(model)`
- **Delete 方法**：`public override ResultModel Delete({TableName} model)` — 调用 `base.Delete(model)`，含事务注释模板
- **GetALL 方法**：`public List<{TableName}> GetALL()` — 通过 session 调用 DAL 的 `SelectAll()`
- **List 方法**：`public PageResultModel<List<{TableName}>> List(PageSearchModel searchModel)` — 通过 session 调用 DAL 的 `Search()`
- **Create 方法**：`public ResultModel<int> Create({TableName} model)` — 通过 session 调用 DAL 的 `InsertForGeneratedKey()`

生成规则：
- **如果文件已存在，跳过不生成**
- 类声明为 `public partial class {TableName}BLL : BaseBLL<I{TableName}DAL, {TableName}>, I{TableName}BLL`
- 包含构造函数：`public {TableName}BLL(IDataSessionFactory factory) : base(factory)`
- 包含 `using {DAL_NAMESPACE};` 和 `using {INTERFACE_NAMESPACE};` 引用

### 文件 2：Interface/I{TableName}BLL.cs

此文件为 BLL 接口，定义业务逻辑契约。

生成内容：
- **List 方法签名**：`PageResultModel<List<{TableName}>> List(PageSearchModel searchModel);`
- **GetALL 方法签名**：`List<{TableName}> GetALL();`
- **Create 方法签名**：`ResultModel<int> Create({TableName} model);`

生成规则：
- **如果文件已存在，跳过不生成**
- 接口声明为 `public interface I{TableName}BLL : IBaseBLL<{TableName}>`
- 带 `[Ivan.IOC.Core.Attributes.InjectIOC]` 特性
- 使用独立的 Interface namespace

模板见 `assets/templates/InterfaceBLLTemplate.cs`。

### 文件命名与目录结构

```
{输出目录}/
├── Interface/
│   ├── ITable1BLL.cs
│   ├── ITable2BLL.cs
│   └── ...
├── Table1BLL.cs
├── Table2BLL.cs
└── ...
```

## 第五步：输出结果汇总

生成完成后，汇总输出：

- 新创建了哪些 `{TableName}BLL.cs` 业务逻辑文件
- 新创建了哪些 `Interface/I{TableName}BLL.cs` 接口文件
- 跳过了哪些已存在的文件
- 如有错误，列出失败的表及原因

## 注意事项

1. **BLL 文件仅首次生成**，后续不会覆盖，保护用户手动添加的代码。
2. **Interface 文件仅首次生成**，后续不会覆盖。
3. 表名和字段名保持数据库中的原始命名，不做任何转换。
4. BLL 类继承 `BaseBLL<I{TableName}DAL, {TableName}>`，DAL 接口名遵循 `I{TableName}DAL` 约定。
5. BLL 接口继承 `IBaseBLL<{TableName}>`。
6. 所有 DAL 调用通过 `session.CreateDAL<I{TableName}DAL>()` 获取数据层实例。
7. Interface 使用独立的 namespace（默认 `{BLL_NAMESPACE}.Interface`）。

## Resources

本 Skill 附带以下资源：

### assets/templates/（代码生成模板）
- `BLLTemplate.cs` — {TableName}BLL.cs 业务逻辑类模板
- `InterfaceBLLTemplate.cs` — Interface/I{TableName}BLL.cs 接口模板
