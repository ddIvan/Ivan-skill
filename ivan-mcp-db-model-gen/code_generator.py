"""
代码生成模块
根据选中的表和字段生成 .NET Model 层 C# 代码
"""

from type_mapping import map_db_type_to_csharp

GENERATE_TEMPLATE = """using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace {namespace}
{{
\t/// <summary>
    /// {table_comment}
    /// </summary>
    [Serializable()]
\tpublic partial class {table_name}
\t{{
\t\tpublic {table_name}() {{ }}
\t\t
\t
\t\t#region Public member
{properties}
\t\t#endregion
\t}}
}}
"""

PARTIAL_TEMPLATE = """using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace {namespace}
{{
\t/// <summary>
    /// {table_comment}
    /// </summary>

\tpublic partial class {table_name}
\t{{

\t}}
}}
"""

PROPERTY_TEMPLATE = """\t\t/// <summary>
    	///{description}
    	/// </summary>
\t\tpublic {csharp_type} {name}
\t\t{{
\t\t\tget ;
\t\t\tset ;
\t\t}}"""

PROPERTY_NO_COMMENT_TEMPLATE = """\t\tpublic {csharp_type} {name}
\t\t{{
\t\t\tget ;
\t\t\tset ;
\t\t}}"""


def generate_property(name: str, db_type: str, is_nullable: bool, description: str) -> str:
    """生成单个属性的代码"""
    csharp_type = map_db_type_to_csharp(db_type, is_nullable)
    if description:
        return PROPERTY_TEMPLATE.format(
            description=description,
            csharp_type=csharp_type,
            name=name,
        )
    else:
        return PROPERTY_NO_COMMENT_TEMPLATE.format(
            csharp_type=csharp_type,
            name=name,
        )


def generate_model(
    table_name: str,
    columns: list[dict],
    namespace: str,
    table_comment: str = "",
) -> str:
    """生成 Generate/xxx.cs 文件内容"""
    properties = []
    for col in columns:
        prop = generate_property(
            name=col["name"],
            db_type=col["db_type"],
            is_nullable=col["is_nullable"],
            description=col.get("description", ""),
        )
        properties.append(prop)

    # 属性之间用空行分隔
    properties_str = "\n\n\n".join(properties)

    return GENERATE_TEMPLATE.format(
        namespace=namespace,
        table_comment=table_comment,
        table_name=table_name,
        properties=properties_str,
    )


def generate_partial(table_name: str, namespace: str, table_comment: str = "") -> str:
    """生成外层 xxx.cs partial 扩展类内容"""
    return PARTIAL_TEMPLATE.format(
        namespace=namespace,
        table_comment=table_comment,
        table_name=table_name,
    )
