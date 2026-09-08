"""
数据库类型 → C# 类型映射模块
与 ivan-db-model-gen skill 保持一致
"""

# 数值类型（默认不可空）
NUMERIC_TYPES = {
    "int": "int",
    "integer": "int",
    "mediumint": "int",
    "smallint": "short",
    "tinyint": "byte",
    "bigint": "long",
    "float": "float",
    "real": "float",
    "double": "double",
    "decimal": "decimal",
    "numeric": "decimal",
    "money": "decimal",
    "smallmoney": "decimal",
}

# 日期时间类型（可空时使用 T?）
DATETIME_TYPES = {
    "datetime": "DateTime",
    "datetime2": "DateTime",
    "smalldatetime": "DateTime",
    "date": "DateTime",
    "timestamp": "DateTime",
    "time": "TimeSpan",
    "datetimeoffset": "DateTimeOffset",
}

# 布尔和 GUID 类型（可空时使用 T?）
OTHER_VALUE_TYPES = {
    "bit": "bool",
    "bool": "bool",
    "boolean": "bool",
    "uniqueidentifier": "Guid",
}

# 引用类型（本身可为 null）
REFERENCE_TYPES = {
    "nvarchar": "string",
    "varchar": "string",
    "nchar": "string",
    "char": "string",
    "ntext": "string",
    "text": "string",
    "xml": "string",
    "tinytext": "string",
    "mediumtext": "string",
    "longtext": "string",
    "enum": "string",
    "set": "string",
    "json": "string",
    "varbinary": "byte[]",
    "binary": "byte[]",
    "image": "byte[]",
    "blob": "byte[]",
    "tinyblob": "byte[]",
    "mediumblob": "byte[]",
    "longblob": "byte[]",
}


def map_db_type_to_csharp(db_type: str, is_nullable: bool) -> str:
    """
    将数据库字段类型映射为 C# 类型。

    规则（与 ivan-db-model-gen 一致）：
    - 数值类型（int/long/short/byte/float/double/decimal）：默认不可空
    - DateTime/TimeSpan/DateTimeOffset/bool/Guid：根据 is_nullable 决定是否可空
    - string/byte[]：本身可为 null
    """
    base_type = db_type.lower().strip()
    # 去掉括号内容，如 "varchar(200)" → "varchar"
    if "(" in base_type:
        base_type = base_type.split("(")[0]

    # 数值类型 → 始终非可空
    if base_type in NUMERIC_TYPES:
        return NUMERIC_TYPES[base_type]

    # 日期时间类型 → 根据 is_nullable
    if base_type in DATETIME_TYPES:
        cs_type = DATETIME_TYPES[base_type]
        return f"{cs_type}?" if is_nullable else cs_type

    # bool / Guid → 根据 is_nullable
    if base_type in OTHER_VALUE_TYPES:
        cs_type = OTHER_VALUE_TYPES[base_type]
        return f"{cs_type}?" if is_nullable else cs_type

    # 引用类型
    if base_type in REFERENCE_TYPES:
        return REFERENCE_TYPES[base_type]

    # 兜底：尝试模糊匹配
    for pattern, cs_type in REFERENCE_TYPES.items():
        if pattern in base_type:
            return cs_type

    # 最终兜底
    return "string"
