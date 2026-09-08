
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Ivan.Data;
using Ivan.Data.IOC;
using Ivan.Common;
using {{MODELS_NAMESPACE}};
using {{DAL_NAMESPACE}};
using {{INTERFACE_NAMESPACE}};
// CS_CreateTableScript
namespace {{NAMESPACE}}
{
	 /// <summary>
    /// {{TABLE_NAME}}操作类
    /// </summary>
	public partial class {{TABLE_NAME}}BLL:BaseBLL<I{{TABLE_NAME}}DAL, {{TABLE_NAME}}>,I{{TABLE_NAME}}BLL
	{	
    

        public {{TABLE_NAME}}BLL(IDataSessionFactory factory) :base(factory)
        {            
        }

        public override ResultModel Add({{TABLE_NAME}} model)
        {
            //可以自定义增加实体记录的逻辑
        
            return base.Add(model);
        }
        
        public override ResultModel<{{TABLE_NAME}}> Select({{TABLE_NAME}} model)
        {
            //可以自定义查询单个实体逻辑
            //调用数据层实体进行查询
 
            return base.Select(model);
        }
        
        public override ResultModel Update({{TABLE_NAME}} model)
        {
            //可以自定义修改实体记录的逻辑
            return base.Update(model);
        }
        
        public override ResultModel Delete({{TABLE_NAME}} model)
        {
            //可以自定义增加实体记录的逻辑
            //获取一个数据库会话
            //using(var session = Factory.OpenSession())
            //{
                //获取之前注入的其他实体的数据层实例
                //ISomeModelDAL dal1=session.CreateDAL<ISomeModelDAL>();
                //ISomeModel2DAL dal2=session.CreateDAL<ISomeModelDAL2>();
                //开启事务
                //try
                //{
                    //session.BeginTrans();
                    //dal1.DoSomething();
                    //dal2.DoSomething();
                    //session.CommitTrans();
                //}
                //catch(Exception ex)
                //{
                    //session.RollbackTrans();
                    //return ResultModel.BuildFail(500, ex.Message);
                //}
             //}
            return base.Delete(model);
        }
        public List<{{TABLE_NAME}}> GetALL()
        {
            using (var session=Factory.OpenSession())
            {
                return session.CreateDAL<I{{TABLE_NAME}}DAL>().SelectAll().ToList();
            }
        }
        public PageResultModel<List<{{TABLE_NAME}}>> List(PageSearchModel searchModel)
        {
            //throw new NotImplementedException();        
            using(var session=Factory.OpenSession())
            {
                var dal=session.CreateDAL<I{{TABLE_NAME}}DAL>();
                var result = dal.Search(searchModel, out int totalCount);
                return PageResultModel<List<{{TABLE_NAME}}>>.BuildSuccess(result, totalCount);              
            }
        }
        
        public ResultModel<int> Create({{TABLE_NAME}} model)
        {
            using (var session = Factory.OpenSession())
            {
                var dal = session.CreateDAL<I{{TABLE_NAME}}DAL>();
                var res = dal.InsertForGeneratedKey(model);
                return ResultModel<int>.BuildSuccess(res);
            }
        }
	}
}
