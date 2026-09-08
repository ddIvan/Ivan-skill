# ivan-mcp-db-model-gen

MCP 服务：连接数据库，通过 GUI 选择表和字段，**返回元数据**给调用方。

> **职责定位**：本服务只负责获取数据库元数据（表名、字段名、类型、注释等），不生成代码文件。代码生成由调用方（如 `ivan-db-model-gen` skill）负责。

## 功能

1. **弹出 GUI 界面**：用户交互式配置数据库连接
2. **支持三种数据库**：SQLite / MSSQL / MySQL
3. **罗列数据库表**：连接后自动列出所有用户表
4. **字段预览与类型映射**：选中表后预览字段及对应的 C# 类型
5. **返回元数据**：确认后返回所选表的字段元数据 JSON，供调用方使用

---

## 一、安装

### 1.1 安装依赖

```bash
cd ivan-mcp-db-model-gen
pip install -e .
```

或使用 uv：

```bash
uv pip install -e .
```

### 1.2 依赖说明

| 依赖 | 用途 |
|------|------|
| `fastmcp>=3.2.0` | MCP 服务框架 |
| `pymssql>=2.3.0` | MSSQL 数据库连接（可选，按需安装） |
| `pymysql>=1.1.0` | MySQL 数据库连接（可选，按需安装） |
| `tkinter` | GUI 界面（Python 内置，无需额外安装） |

> **注意**：如果只使用 SQLite，`pymssql` 和 `pymysql` 不是必须的。可以只安装 `fastmcp`。

---

## 二、发布与运行

### 2.1 本地开发运行

```bash
cd ivan-mcp-db-model-gen
python server.py
```

### 2.2 作为 MCP 服务运行（推荐）

MCP 服务通过 **stdio 传输协议** 与客户端通信，不需要手动启动，由 MCP 客户端自动管理进程。

#### 方式一：CodeBuddy IDE 配置

在 CodeBuddy 的 MCP 配置中添加（`.codebuddy/mcp.json`）：

```json
{
  "mcpServers": {
    "ivan-db-model-gen": {
      "command": "python",
      "args": ["server.py"],
      "cwd": "d:/project/aispace/Ivan-skills/ivan-mcp-db-model-gen"
    }
  }
}
```

#### 方式二：Claude Desktop 配置

在 Claude Desktop 的配置文件中添加（`%APPDATA%\Claude\claude_desktop_config.json`）：

```json
{
  "mcpServers": {
    "ivan-db-model-gen": {
      "command": "python",
      "args": ["server.py"],
      "cwd": "d:/project/aispace/Ivan-skills/ivan-mcp-db-model-gen"
    }
  }
}
```

#### 方式三：使用模块方式运行

```json
{
  "mcpServers": {
    "ivan-db-model-gen": {
      "command": "python",
      "args": ["-m", "server"],
      "cwd": "d:/project/aispace/Ivan-skills/ivan-mcp-db-model-gen"
    }
  }
}
```

### 2.3 打包分发

项目已打包为 zip，位于 `dist/ivan-mcp-db-model-gen.zip`。

接收方解压后执行：

```bash
pip install -e .
```

然后按上述方式配置 MCP 客户端即可。

---

## 三、如何被调用

### 3.1 被 MCP 客户端（AI Agent）调用

配置好 MCP 客户端后，AI Agent 会自动发现该服务提供的 3 个 tools：

| Tool | 说明 |
|------|------|
| `db_model_gen_gui` | 弹出 GUI，用户选表后返回字段元数据（不生成代码） |
| `get_type_mapping` | 查询单个字段的类型映射 |
| `get_type_mapping_rules` | 获取完整类型映射规则表 |

**调用示例（AI Agent 视角）**：

```
用户: "帮我从数据库生成 Model 代码"
  ↓
AI Agent 调用: db_model_gen_gui()
  ↓
弹出 GUI → 用户配置数据库 → 选择表 → 确认
  ↓
返回元数据 JSON: {"success": true, "db_type": "mssql", "tables": {...}}
  ↓
AI Agent / Skill 根据元数据生成代码文件
```

### 3.2 被其他 Skill 调用

`ivan-db-model-gen` skill 通过 MCP 协议调用本服务获取元数据，然后自己生成代码：

```
ivan-db-model-gen skill:
  ① 调用 db_model_gen_gui() → 获取表/字段元数据
  ② 询问用户输出目录、命名空间
  ③ 根据元数据 + 模板生成 Generate/xxx.cs + xxx.cs
```

### 3.3 被 Python 代码直接调用

```python
from server import mcp

# 直接调用 tool 函数
result = db_model_gen_gui()  # 弹出 GUI
print(result)  # JSON 字符串

# 查询类型映射
csharp_type = get_type_mapping("mssql", "varchar", True)
print(csharp_type)  # "string"
```

### 3.4 调用流程图

```
┌─────────────────────────────────────────────────┐
│  MCP 客户端 (CodeBuddy / Claude Desktop)         │
│                                                   │
│  ┌─────────────────────────────────────────────┐ │
│  │  AI Agent / Skill (如 ivan-db-model-gen)     │ │
│  │                                              │ │
│  │  ① 调用 tool: db_model_gen_gui()            │ │
│  │  ② 收到元数据 JSON                           │ │
│  │  ③ 自己生成代码文件                          │ │
│  └──────────────────┬──────────────────────────┘ │
│                     │ stdio (JSON-RPC)            │
└─────────────────────┼────────────────────────────┘
                      │
┌─────────────────────┼────────────────────────────┐
│  ivan-mcp-db-model-gen (Python 进程)              │
│                     │                             │
│  ┌──────────────────▼──────────────────────────┐ │
│  │  FastMCP Server                             │ │
│  │  - db_model_gen_gui()  → 返回元数据          │ │
│  │  - get_type_mapping()  → 返回 C# 类型        │ │
│  │  - get_type_mapping_rules() → 返回映射规则   │ │
│  └──────────────────┬──────────────────────────┘ │
│                     │                             │
│  ┌──────────────────▼──────────────────────────┐ │
│  │  GUI (tkinter)                              │ │
│  │  用户交互：选数据库 → 选表 → 预览 → 确认     │ │
│  │  返回：表名 + 字段元数据（不生成代码）        │ │
│  └─────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────┘
```

---

## 四、类型映射规则

与 `ivan-db-model-gen` skill 保持一致：
- **数值类型**（int/long/short/byte/float/double/decimal）：默认不可空
- **日期时间类型**（DateTime/TimeSpan/DateTimeOffset）：根据 NULL 决定
- **bool/Guid**：根据 NULL 决定
- **string/byte[]**：本身可为 null

---

## 五、项目结构

```
ivan-mcp-db-model-gen/
├── server.py           # MCP 服务主入口（3 个 tools，只返回元数据）
├── gui.py              # tkinter GUI 界面（选表、预览、确认）
├── db_connector.py     # 数据库连接与表结构读取
├── type_mapping.py     # 类型映射规则
├── code_generator.py   # C# 代码生成（保留供直接调用，MCP tool 不使用）
├── pyproject.toml      # 项目配置
└── README.md
```
