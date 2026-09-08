

using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Ivan.Data;
using Ivan.Common;
using Dapper;

using {{MODELS_NAMESPACE}};
using Ivan.Data.SQLBuilder;
// CS_CreateTableScript
namespace {{NAMESPACE}}
{
    /// <summary>
    /// {{TABLE_NAME}}操作类
    /// </summary>
    public partial class {{TABLE_NAME}}DAL
    {

        public List<{{TABLE_NAME}}> Search(PageSearchModel searchModel, out int totalRecordCount)
        {
            var param = new DynamicParameters();

            IvanSQL sql = new IvanSQL();
            sql.Select("*").From("{{TABLE_NAME}}");
{{SEARCH_CONDITIONS}}
            if (searchModel.Page <= 0) { searchModel.Page = 1; }
            sql.Offset(searchModel.Limit * (searchModel.Page - 1)).FetchFirstRowsOnly(searchModel.Limit);
            sql.OrderBy("{{PRIMARY_KEY}} desc");
            return SelectByPage<{{TABLE_NAME}}>(sql, out totalRecordCount, param).AsList();

        }

    }
}
