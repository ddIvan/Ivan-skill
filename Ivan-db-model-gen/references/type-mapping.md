# 数据库类型到 C# 类型映射表

## 完整映射

| SQLite | MSSQL | MySQL | C# Type | 可空时 |
|--------|-------|-------|---------|--------|
| INTEGER | int | int / mediumint | `int` | `int`（数值类型默认不可空） |
| INTEGER | bigint | bigint | `long` | `long`（数值类型默认不可空） |
| INTEGER | smallint | smallint | `short` | `short`（数值类型默认不可空） |
| INTEGER | tinyint | tinyint | `byte` | `byte`（数值类型默认不可空） |
| REAL | float / real | float | `float` | `float`（数值类型默认不可空） |
| REAL | float(53) | double / real | `double` | `double`（数值类型默认不可空） |
| NUMERIC | decimal / numeric / money / smallmoney | decimal / numeric | `decimal` | `decimal`（数值类型默认不可空） |
| TEXT | nvarchar / varchar / nchar / char / ntext / text / xml | varchar / char / tinytext / text / mediumtext / longtext / enum / set / json | `string` | `string` |
| BLOB | varbinary / binary / image / timestamp / rowversion | blob / tinyblob / mediumblob / longblob / binary / varbinary | `byte[]` | `byte[]` |
| — | datetime / datetime2 / smalldatetime | datetime / timestamp | `DateTime` | `DateTime?` |
| — | date | date | `DateTime` | `DateTime?` |
| — | time | time | `TimeSpan` | `TimeSpan?` |
| — | datetimeoffset | — | `DateTimeOffset` | `DateTimeOffset?` |
| INTEGER (0/1) | bit | bit / bool / boolean | `bool` | `bool?` |
| — | uniqueidentifier | — | `Guid` | `Guid?` |

## 类型解析规则

### SQLite 类型解析

SQLite 使用动态类型系统，`PRAGMA table_info` 返回的类型名可能是：
- `INTEGER` → `int`（如果同时是主键自增）或 `long`
- `TEXT` → `string`
- `REAL` → `double`
- `BLOB` → `byte[]`
- `NUMERIC` → `decimal`
- 包含 `CHAR` 的类型名 → `string`
- 包含 `INT` 的类型名 → `int` 或 `long`（根据大小判断）

### MSSQL 类型解析

根据 `DATA_TYPE` 字段精确映射：
- `bit` → `bool`
- `int` / `smallint` / `tinyint` / `bigint` → 对应 C# 整数类型
- `decimal` / `numeric` / `money` / `smallmoney` → `decimal`
- `float` / `real` → `float` 或 `double`
- `nvarchar` / `varchar` / `nchar` / `char` / `ntext` / `text` / `xml` → `string`
- `datetime` / `datetime2` / `smalldatetime` / `date` → `DateTime`
- `time` → `TimeSpan`
- `datetimeoffset` → `DateTimeOffset`
- `uniqueidentifier` → `Guid`
- `varbinary` / `binary` / `image` / `timestamp` → `byte[]`

### MySQL 类型解析

MySQL 的 `SHOW FULL COLUMNS` 返回的 Type 字段包含长度信息，需要提取基础类型名：

```csharp
// 提取基础类型名
string baseType = rawType.Split('(')[0].Trim().ToLower();
```

- `int` / `mediumint` / `smallint` / `tinyint` / `bigint` → 对应 C# 整数类型
- `decimal` / `numeric` → `decimal`
- `float` / `double` / `real` → `float` 或 `double`
- `varchar` / `char` / `tinytext` / `text` / `mediumtext` / `longtext` / `enum` / `set` / `json` → `string`
- `datetime` / `timestamp` / `date` → `DateTime`
- `time` → `TimeSpan`
- `bit` / `bool` / `boolean` → `bool`
- `blob` / `tinyblob` / `mediumblob` / `longblob` / `binary` / `varbinary` → `byte[]`

## 可空判断

**核心规则：数值类型默认不可空。**

- **数值类型（int、long、short、byte、float、double、decimal）**：无论数据库字段是否允许 NULL，始终生成非可空类型（如 `int`、`long`、`decimal`），不使用 `T?` 形式。
- **日期时间类型（DateTime、DateTimeOffset、TimeSpan）和 bool、Guid**：如果数据库字段允许 NULL，则使用可空类型（`DateTime?`、`bool?`、`Guid?` 等）。
- 引用类型（`string`、`byte[]`）本身可为 null，不需要额外处理。
- 对于 MSSQL，`IS_NULLABLE` 返回 `"YES"` 或 `"NO"`。
- 对于 MySQL，`Null` 字段返回 `"YES"` 或 `"NO"`。
- 对于 SQLite，`notnull` 为 `0` 表示可为空，`1` 表示不可为空。
