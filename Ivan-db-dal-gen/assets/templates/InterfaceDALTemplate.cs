using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Ivan.Data;
using Ivan.Data.IOC;
using Ivan.Common;
using {{MODELS_NAMESPACE}};

// CS_CreateTableScript
namespace {{NAMESPACE}}
{
	 /// <summary>
    /// {{TABLE_NAME}}数据层接口
    /// </summary>
    [Ivan.IOC.Core.Attributes.InjectIOC]
	public interface I{{TABLE_NAME}}DAL:IBaseDAL<{{TABLE_NAME}}>
	{	
        List<{{TABLE_NAME}}> Search(PageSearchModel searchModel, out int totalRecordCount);
	}
}
