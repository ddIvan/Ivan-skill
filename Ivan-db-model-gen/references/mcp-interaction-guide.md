# MCP 交互指南：异常处理与超时机制

本文档详细描述 `ivan-db-model-gen` Skill 在 MCP 模式下与 `ivan-mcp-db-model-gen` 服务交互时的异常处理、超时机制和降级策略。

---

## 1. 核心原则

- **MCP 模式下不询问数据库类型和连接串**：这些信息由用户在 GUI 中自行填写
- **MCP 只负责元数据**：连接数据库、读取表结构、返回字段元数据
- **Skill 负责代码生成**：根据元数据生成 .cs 文件
- **异常必须优雅降级**：任何 MCP 异常都应提供降级到手动模式的路径

---

## 2. 异常分类与处理策略

### 2.1 超时异常 (Timeout)

**触发条件**：MCP 调用 `db_model_gen_gui()` 超过 60 秒无响应。

**原因分析**：
- GUI 窗口弹出后用户未及时操作
- MCP 服务进程响应缓慢
- 网络/管道通信延迟

**处理流程**：

```
第 1 次超时：
  1. 告知用户："MCP GUI 调用超时（>60s），正在重试（第 1/2 次）..."
  2. 重新调用 db_model_gen_gui()
  3. 等待结果

第 2 次超时：
  1. 告知用户："MCP GUI 调用再次超时（已重试 2 次）。"
  2. 提供选项：
     A. 重新执行命令再次尝试 GUI 模式
     B. 切换到手动模式（我来询问数据库信息）
  3. 根据用户选择执行

用户选择手动模式：
  → 跳转到「分支 B：手动交互模式」
  → 依次询问：数据库类型、连接串、表名、输出目录、命名空间
```

**对话示例**：

```
AI: MCP GUI 调用超时（>60s），正在重试（第 1/2 次）...
    [重新调用 db_model_gen_gui()]

AI: MCP GUI 调用再次超时（已重试 2 次）。
    可能原因：GUI 窗口未及时操作、MCP 服务响应慢。
    您可以选择：
    A. 重新执行命令，再次尝试 GUI 模式
    B. 切换到手动模式（我来询问数据库信息）

User: B

AI: 好的，切换到手动模式。请提供以下信息：
    1. 数据库类型？（SQLite / MSSQL / MySQL）
    ...
```

### 2.2 用户取消 (User Cancellation)

**触发条件**：用户在 GUI 中点击取消/关闭窗口，MCP 返回取消信号。

**MCP 返回格式**：
```json
{
  "success": false,
  "message": "用户取消了操作"
}
```

**处理流程**：

```
1. 告知用户："您取消了 GUI 操作。"
2. 询问："是否切换到手动模式？"
3. 用户确认 → 降级到分支 B（手动模式）
4. 用户拒绝 → 结束流程，提示"已取消生成"
```

**对话示例**：

```
AI: 您取消了 GUI 操作。是否切换到手动模式继续生成？

User: 是

AI: 好的，切换到手动模式。请提供以下信息：
    1. 数据库类型？（SQLite / MSSQL / MySQL）
    ...
```

### 2.3 数据库连接失败 (Connection Failure)

**触发条件**：用户在 GUI 中输入了错误的连接信息，数据库连接失败。

**MCP 返回格式**：
```json
{
  "success": false,
  "message": "数据库连接失败: Login failed for user 'sa'."
}
```

**处理流程**：

```
1. 告知用户具体错误信息
2. 提供选项：
   A. 重新打开 GUI 重试（请修正连接串后重试）
   B. 切换到手动模式
3. 根据用户选择执行
```

**对话示例**：

```
AI: 数据库连接失败：Login failed for user 'sa'.
    您可以选择：
    A. 重新打开 GUI 重试（请修正连接信息）
    B. 切换到手动模式

User: A

AI: 好的，重新打开 GUI，请在 GUI 中修正连接信息后重试。
    [重新调用 db_model_gen_gui()]
```

### 2.4 空表列表 (Empty Table Selection)

**触发条件**：用户连接成功但未选择任何表就点击了确认。

**MCP 返回格式**：
```json
{
  "success": true,
  "db_type": "mssql",
  "tables": {}
}
```

**处理流程**：

```
1. 告知用户："未选择任何表，请重新操作 GUI 并选择要生成的表。"
2. 重新调用 db_model_gen_gui()
3. 或提供降级到手动模式的选项
```

**对话示例**：

```
AI: 未选择任何表。请重新操作 GUI，在表列表中勾选要生成 Model 的表。
    [重新调用 db_model_gen_gui()]
```

### 2.5 返回格式异常 (Malformed Response)

**触发条件**：MCP 返回的 JSON 格式不符合预期。

**可能情况**：
- 缺少 `success` 字段
- `tables` 字段不是对象
- 字段列表中缺少 `name` 或 `db_type`
- JSON 解析失败

**处理流程**：

```
1. 记录异常详情（内部日志）
2. 告知用户："MCP 返回数据格式异常，自动降级到手动模式。"
3. 自动降级到分支 B（手动模式），无需用户确认
4. 开始手动模式信息收集
```

**对话示例**：

```
AI: MCP 返回数据格式异常，自动降级到手动模式。
    请提供以下信息：
    1. 数据库类型？（SQLite / MSSQL / MySQL）
    ...
```

---

## 3. 元数据校验规则

在解析 MCP 返回的元数据后，必须进行以下校验：

```python
# 伪代码：元数据校验逻辑

def validate_metadata(response):
    """校验 MCP 返回的元数据，返回 (is_valid, error_message)"""
    
    # 1. 检查 success 字段
    if not response.get("success"):
        return False, f"操作失败: {response.get('message', '未知错误')}"
    
    # 2. 检查 tables 字段
    tables = response.get("tables")
    if not isinstance(tables, dict):
        return False, "tables 字段格式错误，应为对象"
    
    if len(tables) == 0:
        return False, "未选择任何表"
    
    # 3. 检查每个表的字段
    for table_name, columns in tables.items():
        if not isinstance(columns, list):
            return False, f"表 {table_name} 的字段列表格式错误"
        
        if len(columns) == 0:
            return False, f"表 {table_name} 没有字段"
        
        for col in columns:
            if "name" not in col:
                return False, f"表 {table_name} 的字段缺少 name"
            if "db_type" not in col:
                return False, f"表 {table_name} 的字段 {col.get('name', '?')} 缺少 db_type"
    
    return True, ""
```

---

## 4. 降级决策矩阵

| 异常类型 | 是否自动降级 | 重试次数 | 用户确认 |
|---------|-------------|---------|---------|
| 超时 | 否 | 2 次 | 需要（询问是否降级） |
| 用户取消 | 否 | 0 | 需要（询问是否降级） |
| 连接失败 | 否 | 0 | 需要（提供重试/降级选项） |
| 空表列表 | 否 | 1 次（重新调用 GUI） | 不需要 |
| 格式异常 | **是** | 0 | 不需要（自动降级） |

---

## 5. 完整交互伪代码

```python
# 伪代码：MCP 模式完整交互流程

MAX_RETRIES = 2

def mcp_mode_workflow():
    """MCP 模式下的完整工作流"""
    
    # ========== 步骤 1：调用 MCP GUI ==========
    # 注意：不询问数据库类型和连接串
    
    for attempt in range(1, MAX_RETRIES + 1):
        try:
            response = call_mcp_tool("db_model_gen_gui", timeout=60)
            
            # 成功返回
            if response:
                break
                
        except TimeoutError:
            if attempt < MAX_RETRIES:
                tell_user(f"MCP GUI 调用超时（>60s），正在重试（第 {attempt}/{MAX_RETRIES} 次）...")
                continue
            else:
                # 所有重试都超时
                choice = ask_user(
                    "MCP GUI 调用超时（已重试 2 次）。\n"
                    "A. 重新尝试 GUI 模式\n"
                    "B. 切换到手动模式"
                )
                if choice == "A":
                    return mcp_mode_workflow()  # 递归重试
                else:
                    return manual_mode_workflow()  # 降级
    
    # ========== 步骤 2：处理响应 ==========
    
    # 2.1 用户取消
    if not response.get("success") and "取消" in response.get("message", ""):
        choice = ask_user("您取消了 GUI 操作。是否切换到手动模式？")
        if choice == "是":
            return manual_mode_workflow()
        else:
            return tell_user("已取消生成。")
    
    # 2.2 连接失败
    if not response.get("success") and "连接" in response.get("message", ""):
        choice = ask_user(
            f"数据库连接失败：{response['message']}\n"
            "A. 重新打开 GUI 重试\n"
            "B. 切换到手动模式"
        )
        if choice == "A":
            return mcp_mode_workflow()
        else:
            return manual_mode_workflow()
    
    # 2.3 格式异常（自动降级）
    is_valid, error = validate_metadata(response)
    if not is_valid:
        tell_user(f"MCP 返回数据格式异常（{error}），自动降级到手动模式。")
        return manual_mode_workflow()
    
    # 2.4 空表列表
    tables = response.get("tables", {})
    if len(tables) == 0:
        tell_user("未选择任何表，请重新操作 GUI 选择表。")
        return mcp_mode_workflow()  # 重新调用 GUI
    
    # ========== 步骤 3：询问输出配置 ==========
    output_dir = ask_user("请输入输出目录：")  # 如果用户已指定则跳过
    namespace = ask_user("请输入命名空间：")    # 如果用户已指定则跳过
    
    # ========== 步骤 4：生成代码 ==========
    for table_name, columns in tables.items():
        generate_model_file(table_name, columns, output_dir, namespace)
        generate_partial_file(table_name, output_dir, namespace)
    
    # ========== 步骤 5：汇总结果 ==========
    summarize_results(tables, output_dir)
```

---

## 6. 对话模板

### 6.1 正常流程

```
User: 生成 User 和 Order 表的 Model

AI: [检测到 MCP 服务已安装]
    正在打开数据库配置 GUI，请在 GUI 中选择数据库类型、输入连接串并选择要生成的表...
    [调用 db_model_gen_gui()]

    [GUI 返回元数据]

AI: 已获取到 2 个表的元数据：Users (5 字段)、Orders (8 字段)。
    请确认输出配置：
    - 输出目录？
    - 命名空间？

User: 输出目录 D:\project\MyApp\Models，命名空间 MyApp.Models

AI: [生成代码...]
    生成完成！
    - Generate/Users.cs ✓
    - Users.cs ✓（新建）
    - Generate/Orders.cs ✓
    - Orders.cs ✓（新建）
```

### 6.2 超时降级流程

```
User: 生成所有表的 Model

AI: [检测到 MCP 服务已安装]
    正在打开数据库配置 GUI...
    [调用 db_model_gen_gui()，60s 后超时]
    
    MCP GUI 调用超时（>60s），正在重试（第 1/2 次）...
    [重新调用，再次超时]
    
    MCP GUI 调用再次超时（已重试 2 次）。
    可能原因：GUI 窗口未及时操作、MCP 服务响应慢。
    您可以选择：
    A. 重新执行命令，再次尝试 GUI 模式
    B. 切换到手动模式（我来询问数据库信息）

User: B

AI: 好的，切换到手动模式。请提供以下信息：
    1. 数据库类型？（SQLite / MSSQL / MySQL）
    ...
```

### 6.3 连接失败重试流程

```
User: 生成 Attach 表的 Model

AI: [检测到 MCP 服务已安装]
    正在打开数据库配置 GUI...
    [调用 db_model_gen_gui()]

    [MCP 返回连接失败]

AI: 数据库连接失败：Login failed for user 'sa'.
    您可以选择：
    A. 重新打开 GUI 重试（请修正连接信息）
    B. 切换到手动模式

User: A

AI: 好的，重新打开 GUI，请在 GUI 中修正连接信息后重试。
    [重新调用 db_model_gen_gui()]
```

---

## 7. 注意事项

1. **超时时间**：MCP 调用默认超时 60 秒，GUI 操作可能超过此时间，必须实现重试机制
2. **重试上限**：最多重试 2 次，避免无限循环
3. **降级路径**：所有异常都必须提供降级到手动模式的路径
4. **用户感知**：每次异常处理都要清晰告知用户发生了什么、有哪些选项
5. **自动降级**：格式异常类问题自动降级，不需要用户确认（因为用户无法修复此类问题）
6. **不重复询问**：如果用户在初始请求中已指定输出目录和命名空间，MCP 模式下不再重复询问
