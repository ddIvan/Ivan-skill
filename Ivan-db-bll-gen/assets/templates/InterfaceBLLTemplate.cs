
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
namespace {{INTERFACE_NAMESPACE}}
{
    /// <summary>
    /// {{TABLE_NAME}}操作类
    /// </summary>
    [Ivan.IOC.Core.Attributes.InjectIOC]
    public interface I{{TABLE_NAME}}BLL : IBaseBLL<{{TABLE_NAME}}>
	{	
        PageResultModel<List<{{TABLE_NAME}}>> List(PageSearchModel searchModel);
        List<{{TABLE_NAME}}> GetALL();
        ResultModel<int> Create({{TABLE_NAME}} model);
	}
}
