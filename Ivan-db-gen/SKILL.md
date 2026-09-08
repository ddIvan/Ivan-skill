---
name: ivan-db-gen
description: Ivan 的一键式全栈代码生成 Skill。自动连接数据库，读取表结构，按顺序生成 Model → DAL → BLL → Web 四层代码。触发词：一键生成/全栈生成/生成全部/ivan-db-gen/DB一键生成等。生成前必须询问用户所有关键选项（输出目录、各层命名空间、Web项目名称、区域等），得到确认后再动手。
---

# Ivan DB Full-Stack Generator (ivan-db-gen)

## Overview

一键式全栈代码生成器。一次连接数据库、一次选择表、一次确认参数，按顺序自动调用以下 4 个子 Skill，生成完整的 .NET 三层架构 + Web 层代码：

| 序号 | 子 Skill | 生成内容 | 每个表生成的文件 |
|------|----------|----------|-----------------|
| 1 | `ivan-db-model-gen` | Model 层 | `Generate/{TableName}.cs` + `{TableName}.cs` |
| 2 | `ivan-db-dal-gen` | DAL 数据层 | `Generate/{TableName}DAL.cs` + `{TableName}DAL.cs` + `Interface/I{TableName}DAL.cs` |
| 3 | `ivan-db-bll-gen` | BLL 业务层 | `{TableName}BLL.cs` + `Interface/I{TableName}BLL.cs` |
| 4 | `ivan-db-web-gen` | Web 层 | `{WebProject}/Areas/{Area}/Controllers/{TableName}Controller.cs` + `Views/{TableName}/Edit.cshtml` + `Views/{TableName}/List.cshtml` |

支持三种数据库：SQLite、MSSQL、MySQL。

## 工作流决策树

```
用户提出一键生成需求
        │
        ▼
┌─ 环境检测 ───────────────────────────────────┐
│ 检测是否已安装 MCP 服务 ivan-mcp-db-model-gen │
│ - 已安装 → 跳过询问数据库信息，直接调用 GUI   │
│ - 未安装 → 进入手动信息收集流程               │
└──────────────────────────────────────────────┘
        │
        ▼
┌─ 统一信息收集（一次性确认所有参数）──────────┐
│                                               │
│  【MCP 模式】                                  │
│  ① 调用 db_model_gen_gui → 用户在 GUI 中：    │
│     - 选择数据库类型                           │
│     - 输入连接字符串                           │
│     - 选择表                                   │
│     - 预览字段                                 │
│  ② GUI 返回表/字段元数据                       │
│  ③ 确认剩余参数：                              │
│     - 输出目录（统一的根目录）                  │
│     - Model 命名空间                           │
│     - DAL 命名空间                             │
│     - BLL 命名空间                             │
│     - BLL Interface 命名空间                   │
│     - Web 项目名称 {WebProject}                │
│     - Area 区域 {Area}                         │
│     - Web 输出目录（Web 项目根路径）            │
│     - Web 命名空间                             │
│                                               │
│  【手动模式】                                  │
│  逐项确认以上所有参数                          │
└──────────────────────────────────────────────┘
        │  仅询问用户未明确给出的选项；已明确的直接采用
        ▼
┌─ 按顺序调用子 Skill ─────────────────────────┐
│                                               │
│  ① 调用 ivan-db-model-gen                     │
│     - 传入：元数据、输出目录、model命名空间     │
│     - 生成 Model 层代码                        │
│                                               │
│  ② 调用 ivan-db-dal-gen                       │
│     - 传入：元数据、输出目录、dal命名空间、     │
│             model命名空间                      │
│     - 生成 DAL 层代码                          │
│                                               │
│  ③ 调用 ivan-db-bll-gen                       │
│     - 传入：元数据/表名、输出目录、bll命名空间、 │
│             model命名空间、dal命名空间、        │
│             interface命名空间                  │
│     - 生成 BLL 层代码                          │
│                                               │
│  ④ 调用 ivan-db-web-gen                       │
│     - 传入：元数据/表名、web输出目录、          │
│             web命名空间、model命名空间、        │
│             bll interface命名空间、            │
│             webproject、area                   │
│     - 生成 Web 层代码                          │
└──────────────────────────────────────────────┘
        │
        ▼
┌─ 统一输出结果汇总 ───────────────────────────┐
│ 汇总四层的生成结果                             │
└──────────────────────────────────────────────┘
```

## 第一步：环境检测（必须执行）

收到生成请求后，首先检测 MCP 服务 `ivan-mcp-db-model-gen` 是否可用：

- **如果 MCP 服务已安装**：跳过询问数据库类型和连接串的步骤，直接调用 `db_model_gen_gui` 工具弹出 GUI 界面。用户在 GUI 中填写数据库信息、选择表后，返回表/字段元数据。
- **如果 MCP 服务未安装**：进入手动信息收集流程，逐项询问用户。

## 第二步：统一信息收集

这是核心步骤。一次性收集所有需要的参数，避免重复询问用户。

### MCP 模式（已安装 ivan-mcp-db-model-gen）

**步骤 1：调用 GUI 获取元数据**

直接调用 `db_model_gen_gui`，用户在 GUI 中完成：
- 选择数据库类型
- 输入连接字符串
- 选择要生成的表
- 预览字段

GUI 返回的元数据格式：
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

**步骤 2：确认剩余参数**

GUI 返回元数据后，一次性确认以下选项（GUI 不包含的）：

| 参数 | 说明 | 示例值 | 用途 |
|------|------|--------|------|
| **输出目录（根目录）** | 所有代码的统一根路径 | `D:\project\Samin\temp\` | Model/DAL/BLL 共享 |
| **Model 命名空间** | Model 类的 namespace | `Samin.Models` | Model/DAL/BLL/Web |
| **DAL 命名空间** | DAL 类的 namespace | `Samin.DAL` | DAL/BLL |
| **DAL Interface 命名空间** | DAL 接口的 namespace | `Samin.DAL.Interface` | BLL |
| **BLL 命名空间** | BLL 类的 namespace | `Samin.BLL` | BLL/Web |
| **BLL Interface 命名空间** | BLL 接口的 namespace | `Samin.BLL.Interface` | BLL/Web |
| **Web 项目名称** | Web 站点项目名称 | `SaminWeb` | Web |
| **Area** | MVC Area 名称 | `Biz` | Web |
| **Web 输出目录** | Web 项目根路径 | `D:\project\Samin\temp\SaminWeb` | Web |
| **Web 命名空间** | Controller 的 namespace | `SaminWeb` | Web |

> 提示：如果用户的项目各层目录结构是统一的（如都在同一个根目录下），可以简化询问。例如 Model/DAL/BLL 都在 `D:\project\Samin\temp\` 下。

### 手动模式（未安装 MCP）

如果 MCP 服务未安装，需要逐项确认以下所有信息：

1. **数据库类型**：SQLite / MSSQL / MySQL
2. **数据库连接信息**：
   - SQLite：数据库文件路径
   - MSSQL：连接字符串
   - MySQL：连接字符串
3. **目标表名**：多个表用逗号分隔。输入 `*` 表示所有表
4. **输出目录（根目录）**：统一根路径
5. **Model 命名空间**
6. **DAL 命名空间**
7. **DAL Interface 命名空间**
8. **BLL 命名空间**
9. **BLL Interface 命名空间**
10. **Web 项目名称**
11. **Area**
12. **Web 输出目录**
13. **Web 命名空间**

> 若用户回复"你看着办"或不做选择，则无法继续，必须明确以上信息后才能开始生成。

### 参数推导规则

为减少询问项，以下参数可以根据已有信息推导：

| 参数 | 推导规则 |
|------|----------|
| **DAL Interface 命名空间** | 如果用户未指定，通常 = `{DAL 命名空间}.Interface` |
| **BLL Interface 命名空间** | 如果用户未指定，通常 = `{BLL 命名空间}.Interface` |
| **DAL 输出目录** | 如果输出根目录和其他层一致，默认 = `{输出目录}\{DAL命名空间}` |
| **BLL 输出目录** | 如果输出根目录和其他层一致，默认 = `{输出目录}\{BLL命名空间}` |
| **Model 输出目录** | 如果输出根目录和其他层一致，默认 = `{输出目录}\{Model命名空间}` |

如果推导值与实际不符，用户可以纠正。

## 第三步：按顺序调用子 Skill

元数据和参数都收集完毕后，**按顺序**调用三个子 Skill 进行代码生成。

> **重要**：这里的"调用子 Skill"是指由本 Skill（ivan-db-gen）**直接执行**各子 Skill 中定义的代码生成逻辑，而非触发工具调用。各子 Skill 的模板文件和生成规则已在上方加载的 SKILL.md 中有完整描述。本 Skill 需要：
> 1. 读取各子 Skill 的模板文件
> 2. 按照各子 Skill 的生成规则，执行代码生成
> 3. 汇总输出

### 顺序一：生成 Model 层（ivan-db-model-gen）

**输入参数：**
- 表/字段元数据
- 输出目录 → Model 输出路径（`{输出目录}\{Model命名空间}`）
- Model 命名空间

**生成文件：**
1. `Generate/{TableName}.cs` — 覆盖
2. `{TableName}.cs` — 仅首次生成

**模板位置：** `C:\Users\Administrator\.codebuddy\skills\Ivan-db-model-gen\assets\templates\`
- `GenerateTemplate.cs`
- `PartialTemplate.cs`

### 顺序二：生成 DAL 层（ivan-db-dal-gen）

**输入参数：**
- 表/字段元数据
- 输出目录 → DAL 输出路径（`{输出目录}\{DAL命名空间}`）
- 主键字段名（用于 SQL 生成）

**生成文件：**
1. `Generate/{TableName}DAL.cs` — 覆盖
2. `{TableName}DAL.cs` — 仅首次生成
3. `Interface/I{TableName}DAL.cs` — 仅首次生成

**模板位置：** `C:\Users\Administrator\.codebuddy\skills\Ivan-db-dal-gen\assets\templates\`
- `GenerateDALTemplate.cs`
- `PartialDALTemplate.cs`
- `InterfaceDALTemplate.cs`

### 顺序三：生成 BLL 层（ivan-db-bll-gen）

**输入参数：**
- 表/字段元数据
- 输出目录 → BLL 输出路径（`{输出目录}\{BLL命名空间}`）

**生成文件：**
1. `{TableName}BLL.cs` — 仅首次生成
2. `Interface/I{TableName}BLL.cs` — 仅首次生成

**模板位置：** `C:\Users\Administrator\.codebuddy\skills\Ivan-db-bll-gen\assets\templates\`
- `BLLTemplate.cs`
- `InterfaceBLLTemplate.cs`

### 顺序四：生成 Web 层（ivan-db-web-gen）

**输入参数：**
- 表/字段元数据
- Web 输出目录、Web 项目名称、Area

**生成文件：**
1. `{WebProject}/Areas/{Area}/Controllers/{TableName}Controller.cs` — 仅首次生成
2. `{WebProject}/Areas/{Area}/Views/{TableName}/Edit.cshtml` — 仅首次生成
3. `{WebProject}/Areas/{Area}/Views/{TableName}/List.cshtml` — 仅首次生成

**模板位置：** `C:\Users\Administrator\.codebuddy\skills\Ivan-db-web-gen\assets\templates\`
- `ControllerTemplate.cs`
- `EditTemplate.cshtml`
- `ListTemplate.cshtml`

## 第四步：输出结果汇总

全部生成完成后，按层汇总输出：

```
=== Model 层 ===
✅ 生成: Generate/Table1.cs
✅ 生成: Generate/Table2.cs
⏭️ 跳过: Table1.cs (已存在)
✅ 新增: Table2.cs

=== DAL 层 ===
✅ 生成: Generate/Table1DAL.cs
⏭️ 跳过: Table1DAL.cs (已存在)
✅ 新增: Interface/ITable1DAL.cs

=== BLL 层 ===
⏭️ 跳过: Table1BLL.cs (已存在)
✅ 新增: Interface/ITable1BLL.cs

=== Web 层 ===
✅ 生成: Table1Controller.cs
✅ 生成: Views/Table1/Edit.cshtml
✅ 生成: Views/Table1/List.cshtml

总计: 新增 X 个文件，覆盖 Y 个文件，跳过 Z 个文件
```

## 注意事项

1. **生成顺序固定**：Model → DAL → BLL → Web，不可更改。因为 DAL 依赖 Model，BLL 依赖 DAL，Web 依赖 BLL。
2. **统一参数收集**：一次性收集所有参数，避免在每个子 Skill 中重复询问。
3. **子 Skill 完整执行**：本 Skill 直接读取各子 Skill 的模板文件并执行代码生成逻辑，确保生成规则与单独调用子 Skill 一致。
4. **不依赖单独的 MCP**：代码生成不需要额外的 MCP 服务，由本 Skill 根据模板直接生成文件。仅数据库元数据获取需要 `ivan-mcp-db-model-gen` MCP 服务。
5. **如果某个子 Skill 生成失败，继续执行后续 Skill**，最后统一报告。
6. **表名和字段名保持数据库中的原始命名**，不做任何转换。
7. **所有文件生成规则与各子 Skill 单独调用时完全一致**（覆盖规则、跳过规则）。

## Resources

本 Skill 不附带独立的模板文件。代码生成使用以下子 Skill 的模板：

| 层 | 模板来源 |
|------|----------|
| Model | `Ivan-db-model-gen/assets/templates/` |
| DAL | `Ivan-db-dal-gen/assets/templates/` |
| BLL | `Ivan-db-bll-gen/assets/templates/` |
| Web | `Ivan-db-web-gen/assets/templates/` |
