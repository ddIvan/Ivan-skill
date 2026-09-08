"""
数据库连接与表结构读取模块
支持 SQLite / MSSQL / MySQL
"""

import sqlite3
from typing import Any


def connect_db(db_type: str, connection_string: str):
    """根据数据库类型建立连接"""
    if db_type == "sqlite":
        return sqlite3.connect(connection_string)
    elif db_type == "mssql":
        import pymssql
        # 解析连接字符串
        params = _parse_connection_string(connection_string)
        return pymssql.connect(**params)
    elif db_type == "mysql":
        import pymysql
        params = _parse_connection_string(connection_string)
        return pymysql.connect(**params)
    else:
        raise ValueError(f"不支持的数据库类型: {db_type}")


def _parse_connection_string(conn_str: str) -> dict[str, Any]:
    """解析分号分隔的连接字符串为字典"""
    params: dict[str, Any] = {}
    for part in conn_str.split(";"):
        part = part.strip()
        if "=" in part:
            key, value = part.split("=", 1)
            key = key.strip().lower()
            value = value.strip()
            # 映射常见键名
            key_map = {
                "server": "host",
                "data source": "host",
                "host": "host",
                "database": "database",
                "initial catalog": "database",
                "db": "database",
                "user id": "user",
                "uid": "user",
                "user": "user",
                "password": "password",
                "pwd": "password",
                "port": "port",
                "trusted_connection": "trusted",
                "integrated security": "trusted",
                "trustservercertificate": "trust_cert",
            }
            mapped_key = key_map.get(key, key)
            if mapped_key == "port":
                params[mapped_key] = int(value)
            elif mapped_key in ("trusted", "trust_cert"):
                pass  # pymssql 不需要这些
            else:
                params[mapped_key] = value
    return params


def get_tables(db_type: str, conn) -> list[str]:
    """获取数据库中所有用户表名"""
    if db_type == "sqlite":
        cursor = conn.cursor()
        cursor.execute(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name"
        )
        return [row[0] for row in cursor.fetchall()]
    elif db_type == "mssql":
        cursor = conn.cursor()
        cursor.execute(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME"
        )
        return [row[0] for row in cursor.fetchall()]
    elif db_type == "mysql":
        cursor = conn.cursor()
        cursor.execute("SHOW TABLES")
        return [row[0] for row in cursor.fetchall()]
    else:
        raise ValueError(f"不支持的数据库类型: {db_type}")


def get_columns(db_type: str, conn, table_name: str) -> list[dict]:
    """获取指定表的字段信息"""
    if db_type == "sqlite":
        return _get_columns_sqlite(conn, table_name)
    elif db_type == "mssql":
        return _get_columns_mssql(conn, table_name)
    elif db_type == "mysql":
        return _get_columns_mysql(conn, table_name)
    else:
        raise ValueError(f"不支持的数据库类型: {db_type}")


def _get_columns_sqlite(conn, table_name: str) -> list[dict]:
    cursor = conn.cursor()
    cursor.execute(f"PRAGMA table_info('{table_name}')")
    columns = []
    for row in cursor.fetchall():
        columns.append({
            "name": row[1],
            "db_type": row[2],
            "is_nullable": row[3] == 0,  # notnull: 0=可空, 1=不可空
            "description": "",
        })
    return columns


def _get_columns_mssql(conn, table_name: str) -> list[dict]:
    cursor = conn.cursor()
    sql = """
    SELECT 
        c.COLUMN_NAME,
        c.DATA_TYPE,
        c.IS_NULLABLE,
        ISNULL(CAST(ep.value AS NVARCHAR(MAX)), '') AS DESCRIPTION
    FROM INFORMATION_SCHEMA.COLUMNS c
    LEFT JOIN sys.extended_properties ep 
        ON ep.major_id = OBJECT_ID(c.TABLE_NAME) 
        AND ep.minor_id = c.ORDINAL_POSITION 
        AND ep.name = 'MS_Description'
    WHERE c.TABLE_NAME = %s
    ORDER BY c.ORDINAL_POSITION
    """
    cursor.execute(sql, (table_name,))
    columns = []
    for row in cursor.fetchall():
        columns.append({
            "name": row[0],
            "db_type": row[1],
            "is_nullable": row[2].upper() == "YES",
            "description": row[3] or "",
        })
    return columns


def _get_columns_mysql(conn, table_name: str) -> list[dict]:
    cursor = conn.cursor()
    cursor.execute(f"SHOW FULL COLUMNS FROM `{table_name}`")
    columns = []
    for row in cursor.fetchall():
        columns.append({
            "name": row[0],
            "db_type": row[1],
            "is_nullable": row[3].upper() == "YES",
            "description": row[8] or "",
        })
    return columns
