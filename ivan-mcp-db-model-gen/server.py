"""
ivan-mcp-db-model-gen MCP 服务

提供 MCP tools 供其他 skill（如 ivan-db-model-gen）调用。
本服务只负责获取数据库元数据（表名、字段信息），不生成代码。
代码生成由调用方（skill）负责。

Tools:
- db_model_gen_gui: 弹出 GUI，让用户连接数据库、选择表，返回表/字段元数据
- get_type_mapping: 获取单个字段的 C# 类型映射
- get_type_mapping_rules: 获取完整的类型映射规则表

架构说明：
- 本服务使用 fastmcp 的 STDIO 传输协议
- GUI 调用（tkinter mainloop）必须在独立线程中运行，否则会阻塞 asyncio 事件循环
- 使用 asyncio.to_thread 将 GUI 操作隔离到独立线程
"""

import asyncio
import json
from fastmcp import FastMCP

from gui import show_gui_and_get_result
from type_mapping import map_db_type_to_csharp

mcp = FastMCP("ivan-mcp-db-model-gen")


@mcp.tool
async def db_model_gen_gui() -> str:
    """
    弹出 GUI 界面，让用户交互式地：
    1. 选择数据库类型（SQLite/MSSQL/MySQL）并输入连接字符串
    2. 连接数据库后罗列所有表
    3. 多选要导出的表，预览字段及 C# 类型映射
    4. 确认后返回所选表的字段元数据

    注意：本 tool 只返回元数据，不生成任何代码文件。
    代码生成由调用方（如 ivan-db-model-gen skill）负责。

    由于 GUI（tkinter mainloop）是阻塞操作，本函数使用 asyncio.to_thread
    将 GUI 放到独立线程中运行，避免阻塞 MCP 服务的 asyncio 事件循环。

    Returns:
        JSON 字符串，包含所选表的字段元数据：
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
    """
    # 关键：使用 asyncio.to_thread 将阻塞的 GUI 操作放到独立线程
    # 这样 MCP 服务的 asyncio 事件循环不会被 tkinter mainloop 阻塞
    result = await asyncio.to_thread(show_gui_and_get_result)

    if result is None:
        return json.dumps({
            "success": False,
            "message": "用户取消了操作",
        }, ensure_ascii=False)

    return json.dumps({
        "success": True,
        "db_type": result["db_type"],
        "tables": result["tables"],
    }, ensure_ascii=False, indent=2)


@mcp.tool
def get_type_mapping(db_type: str, column_type: str, is_nullable: bool) -> str:
    """
    获取单个数据库字段类型对应的 C# 类型。

    Args:
        db_type: 数据库类型（sqlite/mssql/mysql）
        column_type: 数据库字段类型（如 varchar, int, datetime）
        is_nullable: 是否允许 NULL

    Returns:
        对应的 C# 类型字符串（如 string, int, DateTime?）
    """
    return map_db_type_to_csharp(column_type, is_nullable)


@mcp.tool
def get_type_mapping_rules() -> str:
    """
    获取完整的数据库类型到 C# 类型映射规则表。

    Returns:
        JSON 格式的映射规则
    """
    rules = {
        "numeric_types": {
            "description": "数值类型默认不可空，即使数据库允许 NULL 也生成非可空类型",
            "mappings": {
                "int/integer/mediumint": "int",
                "smallint": "short",
                "tinyint": "byte",
                "bigint": "long",
                "float/real": "float",
                "double": "double",
                "decimal/numeric/money": "decimal",
            },
        },
        "datetime_types": {
            "description": "日期时间类型根据 is_nullable 决定是否可空",
            "mappings": {
                "datetime/datetime2/date/timestamp": "DateTime / DateTime?",
                "time": "TimeSpan / TimeSpan?",
                "datetimeoffset": "DateTimeOffset / DateTimeOffset?",
            },
        },
        "other_value_types": {
            "description": "bool 和 Guid 根据 is_nullable 决定是否可空",
            "mappings": {
                "bit/bool/boolean": "bool / bool?",
                "uniqueidentifier": "Guid / Guid?",
            },
        },
        "reference_types": {
            "description": "引用类型本身可为 null",
            "mappings": {
                "varchar/nvarchar/char/text/...": "string",
                "varbinary/blob/image/...": "byte[]",
            },
        },
    }
    return json.dumps(rules, ensure_ascii=False, indent=2)


def main():
    """MCP 服务入口"""
    mcp.run()


if __name__ == "__main__":
    main()
