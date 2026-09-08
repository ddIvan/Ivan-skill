---
name: ivan-db-web-gen
description: Ivan 的数据库 Web 层代码生成 Skill。自动连接指定数据库（SQLite/MSSQL/MySQL），读取表结构，生成 ASP.NET Core MVC Web 层代码。每个表生成三个文件：{WebProject}/Areas/{Area}/Controllers/{TableName}Controller.cs（控制器）、{WebProject}/Areas/{Area}/Views/{TableName}/Edit.cshtml（编辑视图）、{WebProject}/Areas/{Area}/Views/{TableName}/List.cshtml（列表视图）。触发词：生成Web层/生成MVC/生成Controller/生成View/Web代码生成等。生成前必须先询问用户数据库连接信息、输出目录等关键选项，得到确认后再动手。
---

# Ivan DB Web Generator

## Overview

自动连接数据库，读取指定表的结构，生成 ASP.NET Core MVC 的 Controller 和 View 代码。每个数据库表生成三个文件：

- **`{WebProject}/Areas/{Area}/Controllers/{TableName}Controller.cs`**：控制器，包含 Index、List(GET/POST)、Add(GET/POST)、Edit(GET/POST)、Delete 方法。此文件**仅首次生成**，后续不会覆盖。
- **`{WebProject}/Areas/{Area}/Views/{TableName}/Edit.cshtml`**：编辑视图，根据字段生成 el-form 表单。此文件**仅首次生成**，后续不会覆盖。
- **`{WebProject}/Areas/{Area}/Views/{TableName}/List.cshtml`**：列表视图，根据字段生成 el-table-column 列。此文件**仅首次生成**，后续不会覆盖。

支持三种数据库：SQLite、MSSQL、MySQL。

## 工作流决策树

```
用户提出生成 Web 层需求
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
│ 3. 要生成 Web 层的表名（支持多表，逗号分隔）   │
│ 4. 站点项目名称 {WebProject}                  │
│ 5. 区域 {Area}                                │
│ 6. Web 输出目录（Web 项目根路径）              │
│ 7. Web 命名空间（namespace）                   │
│ 8. Models 命名空间（Model 类所在的 namespace） │
│ 9. BLL Interface 命名空间（BLL 接口 namespace）│
└──────────────────────────────────────────────┘
        │  仅询问用户未明确给出的选项；已明确的直接采用
        ▼
┌─ 获取表结构元数据 ───────────────────────────┐
│ 调用 ivan-mcp-db-model-gen 的                │
│ db_model_gen_gui 工具获取表/字段元数据        │
│ 返回 JSON：{ tables: { TableName: [...] } }  │
│ Web 生成需要字段名和描述来渲染表格列和表单     │
└──────────────────────────────────────────────┘
        │
        ▼
┌─ 生成代码（由 Skill 直接执行） ──────────────┐
│ 对每个表，根据 assets/templates/ 中的模板：   │
│ 1. 检查 {TableName}Controller.cs 是否存在     │
│    - 不存在 → 生成控制器                      │
│    - 已存在 → 跳过（保留用户手动修改）         │
│ 2. 检查 Views/{TableName}/Edit.cshtml 是否存在│
│    - 不存在 → 生成含字段表单的编辑页           │
│    - 已存在 → 跳过（保留用户手动修改）         │
│ 3. 检查 Views/{TableName}/List.cshtml 是否存在│
│    - 不存在 → 生成含字段列的列表页             │
│    - 已存在 → 跳过（保留用户手动修改）         │
└──────────────────────────────────────────────┘
        │
        ▼
┌─ 输出结果 ───────────────────────────────────┐
│ 汇总生成结果：生成了哪些文件，跳过了哪些文件   │
└──────────────────────────────────────────────┘
```

## 第一步：环境检测（必须执行）

收到生成 Web 层的请求后，首先检测 MCP 服务 `ivan-mcp-db-model-gen` 是否可用：

- **如果 MCP 服务已安装**：跳过询问数据库类型和连接串的步骤，直接调用 `db_model_gen_gui` 工具弹出 GUI 界面。用户在 GUI 中填写数据库信息、选择表后，返回表/字段元数据。
- **如果 MCP 服务未安装**：进入手动信息收集流程，逐项询问用户。

## 第二步：信息收集

### MCP 模式（已安装 ivan-mcp-db-model-gen）

直接调用 `db_model_gen_gui`，用户在 GUI 中完成：
- 选择数据库类型
- 输入连接字符串
- 选择要生成 Web 层的表
- 预览字段

GUI 返回元数据后，仍需确认以下选项（GUI 不包含的）：
- **站点项目名称 `{WebProject}`**：Web 项目名称（如 `SaminWeb`）
- **区域 `{Area}`**：MVC Area 名称（如 `Biz`）
- **Web 输出目录**：Web 项目根目录的绝对路径（如 `D:\project\Samin\temp\SaminWeb`）
- **Web 命名空间**：Controller 类使用的 namespace（如 `SaminWeb.Areas.Biz.Controllers`）
- **Models 命名空间**：Model 类所在的 namespace（用于 using 引用，如 `Samin.Models`）
- **BLL Interface 命名空间**：BLL 接口所在的 namespace（用于 using 引用，如 `Samin.BLL.Interface`）

### 手动模式（未安装 MCP）

必问项（除非已明确）：

1. **数据库类型**：SQLite / MSSQL / MySQL
2. **数据库连接信息**：
   - SQLite：数据库文件路径
   - MSSQL：连接字符串
   - MySQL：连接字符串
3. **目标表名**：要生成 Web 层的表名，多个表用逗号分隔。输入 `*` 表示所有表
4. **站点项目名称**：Web 项目名称（如 `SaminWeb`）
5. **区域**：MVC Area 名称（如 `Biz`）
6. **Web 输出目录**：Web 项目根目录的绝对路径
7. **Web 命名空间**：Controller 类使用的 namespace
8. **Models 命名空间**：Model 类所在的 namespace
9. **BLL Interface 命名空间**：BLL 接口所在的 namespace

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

**Web 层代码生成依赖字段元数据**：字段的 `name` 用于绑定 prop，字段的 `description` 用于生成表格列标题和表单标签（如果 description 为空，则使用 name 作为显示文本）。

## 第四步：生成代码

代码生成由 Skill 直接执行（不依赖额外的 MCP 服务），根据 `assets/templates/` 中的模板生成文件。

### 文件 1：{TableName}Controller.cs

此文件为 MVC 控制器，继承 `BaseController`，模板见 `assets/templates/ControllerTemplate.cs`。

生成内容：
- **Index 方法**：`public IActionResult Index()` — 返回 `View("List")`
- **List(GET) 方法**：`[HttpGet] public IActionResult List()` — 返回 `View()`
- **List(POST) 方法**：`[HttpPost][JsonMethod] public IActionResult List(PageSearchModel model)` — 调用 `BLLInstance.List(model)`，返回 `GridDataSource`
- **Add(GET) 方法**：`public IActionResult Add()` — 创建新 Model 实例，序列化到 `ViewBag.ModelString`，返回 `View("Edit", ResultModel.BuildSuccess(model))`
- **Add(POST) 方法**：`[HttpPost][JsonMethod] public IActionResult Add({TableName} model)` — 调用 `BLLInstance.Add(model)`
- **Edit(GET) 方法**：`public IActionResult Edit(int ID)` — 调用 `BLLInstance.Select(new {TableName}{ID=ID})`，序列化到 `ViewBag.ModelString`
- **Edit(POST) 方法**：`[HttpPost][JsonMethod] public IActionResult Edit({TableName} model)` — 调用 `BLLInstance.Update(model)`
- **Delete 方法**：`[JsonMethod] public IActionResult Delete(int ID)` — 调用 `BLLInstance.Delete(new {TableName}(){ID=ID})`

生成规则：
- **如果文件已存在，跳过不生成**
- 类声明为 `[Area("{Area}")] public class {TableName}Controller : BaseController`
- 私有字段：`I{TableName}BLL BLLInstance;`
- 构造函数注入：`public {TableName}Controller(I{TableName}BLL bll)`
- 包含 `using {Models_NAMESPACE};` 和 `using {BLL_INTERFACE_NAMESPACE};` 引用

### 文件 2：Views/{TableName}/Edit.cshtml

此文件为编辑视图，使用 Element UI 的 `el-form` 组件，模板见 `assets/templates/EditTemplate.cshtml`。

生成内容：
- Layout 引用 `_LayoutEdit.cshtml`
- 根据字段生成 `el-form-item`，每个字段一行：
  ```html
  <el-row :span="24">
      <el-form-item label="{description | name}" prop="{name}">
          <el-input v-model="formObj.{name}" type="text"></el-input>
      </el-form-item>
  </el-row>
  ```
- `@section Section_Vue` 中：
  - `formObj` 通过 `@Html.Raw(ViewBag.ModelString)` 初始化
  - `rules` 对每个字段生成空的验证规则数组（`'FieldName':[]`）
  - `BeforeSubmit` 方法返回 true

生成规则：
- **如果文件已存在，跳过不生成**
- label 优先使用字段 description，无 description 则使用字段 name

### 文件 3：Views/{TableName}/List.cshtml

此文件为列表视图，使用 Element UI 的 `el-table-column` 组件，模板见 `assets/templates/ListTemplate.cshtml`。

生成内容：
- Layout 引用 `_LayoutList.cshtml`
- `ViewBag.IsPage = true`, `ViewBag.PageSize = 20`
- `ViewBag.ShowAddBtn = true`, `ViewBag.ShowEditBtn = true`, `ViewBag.ShowDeleteBtn = true`, `ViewBag.ShowCheckBox = true`
- `ViewData["DataSourceUrl"]` 指向 `Url.RouteUrl("areas", new { action = "List", controller = "{TableName}" })`
- `@section Section_TableHeader` 中根据字段生成列：
  ```html
  <el-table-column property="{name}" label="{description | name}"></el-table-column>
  ```
- `@section Section_Vue` 中：
  - `new Ctor({el:"#app"})` 初始化
  - `Query`、`HandleAddClick`、`HandleUpdateClick`、`HandleDeleteClick` 方法

生成规则：
- **如果文件已存在，跳过不生成**
- label 优先使用字段 description，无 description 则使用字段 name

### 文件命名与目录结构

```
{Web输出目录}/
├── Areas/
│   └── {Area}/
│       ├── Controllers/
│       │   ├── Table1Controller.cs
│       │   ├── Table2Controller.cs
│       │   └── ...
│       └── Views/
│           ├── Table1/
│           │   ├── Edit.cshtml
│           │   └── List.cshtml
│           ├── Table2/
│           │   ├── Edit.cshtml
│           │   └── List.cshtml
│           └── ...
```

> 注意：实际生成路径需要结合输出目录、WebProject 和 Area 拼接。Controller 输出到 `{输出目录}/Areas/{Area}/Controllers/`，View 输出到 `{输出目录}/Areas/{Area}/Views/{TableName}/`。

## 第五步：输出结果汇总

生成完成后，汇总输出：

- 新创建了哪些 `{TableName}Controller.cs` 控制器文件
- 新创建了哪些 `Edit.cshtml` 编辑视图文件
- 新创建了哪些 `List.cshtml` 列表视图文件
- 跳过了哪些已存在的文件
- 如有错误，列出失败的表及原因

## 注意事项

1. **所有文件仅首次生成**，后续不会覆盖，保护用户手动添加的代码。
2. 表名和字段名保持数据库中的原始命名，不做任何转换。
3. Controller 中的路由连接使用 `Url.RouteUrl("areas", new { ... })`。
4. Edit.cshtml 中的 `ViewBag.ModelString` 用于将 Model 序列化为 JSON 赋给 Vue 的 `formObj`。
5. Edit.cshtml 序列化时使用 `DateFormatHandling.MicrosoftDateFormat` + `"yyyy-MM-dd HH:mm:ss"` 格式。
6. List.cshtml 使用 GridDataSource 分页表格模式，DataSourceUrl 指向 List(POST) 方法。
7. 所有 Delete/Add/Edit POST 方法使用 `[JsonMethod]` 特性，返回 JSON。
8. Add GET 返回 `ResultModel<{TableName}>.BuildSuccess(model)`，Edit GET 返回 `ResultModel<{TableName}>.BuildSuccess(model.Data)` 或原 model（含错误信息）。

## Resources

本 Skill 附带以下资源：

### assets/templates/（代码生成模板）
- `ControllerTemplate.cs` — Controller 代码模板
- `EditTemplate.cshtml` — Edit.cshtml 编辑视图模板
- `ListTemplate.cshtml` — List.cshtml 列表视图模板
