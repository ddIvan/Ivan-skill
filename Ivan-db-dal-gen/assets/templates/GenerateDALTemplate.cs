

using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Ivan.Data;
using Ivan.Common;
using Dapper;
using Ivan.Data.SQLBuilder;
using {{MODELS_NAMESPACE}};
// CS_CreateTableScript
namespace {{NAMESPACE}}
{
	 /// <summary>
    /// {{TABLE_NAME}}操作类
    /// </summary>
	public partial class {{TABLE_NAME}}DAL
        :BaseDAL<{{TABLE_NAME}}>,I{{TABLE_NAME}}DAL
	{	
    
        public {{TABLE_NAME}}DAL():base()
        {                
        }

        public {{TABLE_NAME}}DAL(IDbHelper dbHelper):base(dbHelper)
        {            
        }

        
			

        
        protected override string InsertSql => @"INSERT into {{TABLE_NAME}} ({{INSERT_COLUMNS}})
                                 VALUES ({{INSERT_VALUES}})";
                                
        protected override string InsertSqlForGeneratedKey => InsertSql + ";select SCOPE_IDENTITY();";

		protected override string DeleteSql => @"DELETE from {{TABLE_NAME}} WHERE {{PRIMARY_KEY}} = @{{PRIMARY_KEY}}";
        
        protected override string UpdateSql => @"UPDATE {{TABLE_NAME}} SET {{UPDATE_SET}}
                                        where {{PRIMARY_KEY}} = @{{PRIMARY_KEY}}";
                                        
        protected override string SelectAllSql => @"select * from {{TABLE_NAME}} (nolock)";
                                        
		#region 按索引进行删除、修改或查询
  		
		#endregion
		        
        

		#region 查询
		
		
		/// <summary>
        /// 按主键查询一个对象
        /// </summary>
        /// <param name="{{PRIMARY_KEY}}">主键值</param>
		/// <returns>没有查到返回null</returns>
        public override {{TABLE_NAME}} Select({{TABLE_NAME}} value)
        {
            string sql = "SELECT * FROM {{TABLE_NAME}} (nolock) WHERE {{PRIMARY_KEY}} = @{{PRIMARY_KEY}}";
		    return SelectFirst(sql,value);
           
        }
        
			
		
		

		
		#endregion 
        
       
        
	}
}
