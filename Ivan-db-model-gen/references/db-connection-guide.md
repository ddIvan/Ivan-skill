# 数据库连接指南

## SQLite

### NuGet 包

推荐使用 `Microsoft.Data.Sqlite`（轻量、跨平台）：

```xml
<PackageReference Include="Microsoft.Data.Sqlite" Version="7.0.0" />
```

或使用 `System.Data.SQLite`：

```xml
<PackageReference Include="System.Data.SQLite.Core" Version="1.0.118" />
```

### 连接字符串

```
Data Source=D:\data\mydb.db
Data Source=D:\data\mydb.db;Mode=ReadWriteCreate
```

### 读取表结构

```csharp
using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=path/to/db.db");
connection.Open();

// 获取所有表名
using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    string tableName = reader.GetString(0);
}

// 获取指定表的字段信息
cmd.CommandText = $"PRAGMA table_info('{tableName}')";
using var reader2 = cmd.ExecuteReader();
while (reader2.Read())
{
    int cid = reader2.GetInt32(0);       // 序号
    string name = reader2.GetString(1);   // 字段名
    string type = reader2.GetString(2);   // 类型
    bool notNull = reader2.GetInt32(3) == 1;
    object dfltValue = reader2.GetValue(4); // 默认值
    bool isPk = reader2.GetInt32(5) == 1;   // 是否主键
}
```

> **注意**：SQLite 的 PRAGMA table_info 不返回字段注释。如需注释，需在建表时通过特殊方式存储。

---

## MSSQL

### NuGet 包

```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.0" />
```

### 连接字符串

```
Server=.;Database=MyDb;Trusted_Connection=True;TrustServerCertificate=True;
Server=192.168.1.100;Database=MyDb;User Id=sa;Password=123456;TrustServerCertificate=True;
Server=localhost\\SQLEXPRESS;Database=MyDb;Integrated Security=True;TrustServerCertificate=True;
```

### 读取表结构

```csharp
using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
connection.Open();

// 获取所有用户表名
using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME";
using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    string tableName = reader.GetString(0);
}
reader.Close();

// 获取指定表的字段信息（含注释）
cmd.CommandText = @"
SELECT 
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.IS_NULLABLE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.NUMERIC_PRECISION,
    c.NUMERIC_SCALE,
    ISNULL(CAST(ep.value AS NVARCHAR(MAX)), '') AS DESCRIPTION
FROM INFORMATION_SCHEMA.COLUMNS c
LEFT JOIN sys.extended_properties ep 
    ON ep.major_id = OBJECT_ID(c.TABLE_NAME) 
    AND ep.minor_id = c.ORDINAL_POSITION 
    AND ep.name = 'MS_Description'
WHERE c.TABLE_NAME = @tableName
ORDER BY c.ORDINAL_POSITION";
cmd.Parameters.AddWithValue("@tableName", tableName);
using var reader2 = cmd.ExecuteReader();
while (reader2.Read())
{
    string columnName = reader2.GetString(0);
    string dataType = reader2.GetString(1);
    string isNullable = reader2.GetString(2); // "YES" or "NO"
    int? maxLength = reader2.IsDBNull(3) ? null : reader2.GetInt32(3);
    int? precision = reader2.IsDBNull(4) ? null : (int)reader2.GetByte(4);
    int? scale = reader2.IsDBNull(5) ? null : reader2.GetInt32(5);
    string description = reader2.GetString(6);
}
```

---

## MySQL

### NuGet 包

```xml
<PackageReference Include="MySql.Data" Version="8.1.0" />
```

或使用 `MySqlConnector`（性能更好）：

```xml
<PackageReference Include="MySqlConnector" Version="2.2.0" />
```

### 连接字符串

```
Server=localhost;Database=MyDb;User=root;Password=123456;
Server=192.168.1.100;Port=3306;Database=MyDb;User=root;Password=123456;Charset=utf8mb4;
```

### 读取表结构

```csharp
using var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
connection.Open();

// 获取所有表名
using var cmd = connection.CreateCommand();
cmd.CommandText = "SHOW TABLES";
using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    string tableName = reader.GetString(0);
}
reader.Close();

// 获取指定表的字段信息（含注释）
cmd.CommandText = $"SHOW FULL COLUMNS FROM `{tableName}`";
using var reader2 = cmd.ExecuteReader();
while (reader2.Read())
{
    string field = reader2.GetString(0);      // 字段名
    string type = reader2.GetString(1);        // 类型（如 "int(11)", "varchar(255)"）
    string collation = reader2.IsDBNull(2) ? "" : reader2.GetString(2);
    string nullable = reader2.GetString(3);    // "YES" or "NO"
    string key = reader2.IsDBNull(4) ? "" : reader2.GetString(4); // PRI/UNI/MUL
    string defaultValue = reader2.IsDBNull(5) ? "" : reader2.GetString(5);
    string extra = reader2.IsDBNull(6) ? "" : reader2.GetString(6); // auto_increment 等
    string privileges = reader2.IsDBNull(7) ? "" : reader2.GetString(7);
    string comment = reader2.IsDBNull(8) ? "" : reader2.GetString(8); // 字段注释
}
```

> **注意**：MySQL 的 `SHOW FULL COLUMNS` 返回的 `Type` 字段包含长度信息（如 `varchar(255)`），需要解析出基础类型名。
