
using System;
using System.Collections.Generic;
using System.Linq;
using Ivan.Admin.BLL.Attributes;
using Ivan.Admin.BLL.Auth;
using {{WEB_PROJECT}}.Common;
using Ivan.Admin.Models;
using Ivan.Common;
using {{MODELS_NAMESPACE}};
using {{BLL_INTERFACE_NAMESPACE}};
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace {{WEB_NAMESPACE}}
{
    [Area("{{AREA}}")]
    public class {{TABLE_NAME}}Controller : BaseController
    {
        I{{TABLE_NAME}}BLL BLLInstance;
        

        public {{TABLE_NAME}}Controller(I{{TABLE_NAME}}BLL bll)
        {
            BLLInstance = bll;
            
        }

        public IActionResult Index()
        {
            return View("List");
        }
        [HttpGet]
        public IActionResult List()
        {
        
            return View();
        }

        [JsonMethod]
        [HttpPost]
        public IActionResult List(PageSearchModel model)
        {
            //业务逻辑类应自行实现List分页查询
            var result = BLLInstance.List(model); 
            

            
            GridDataSource<{{TABLE_NAME}}> gridJbxxs = new GridDataSource<{{TABLE_NAME}}>() { total = result.TotalRecord, rows = result.Data.ToArray() };            
            return Json(ResultModel<GridDataSource<{{TABLE_NAME}}>>.BuildSuccess(gridJbxxs));
        }


       #region 增、删、改

        public IActionResult Add()
        {
            var model = new {{TABLE_NAME}}();
            ViewBag.ModelString = JsonConvert.SerializeObject(model);      
            
            return View("Edit", ResultModel<{{TABLE_NAME}}>.BuildSuccess(model));
        }

        [HttpPost]
        [JsonMethod]
        public IActionResult Add({{TABLE_NAME}} model)
        {
            var result = BLLInstance.Add(model);
            return Json(result);
        }

        public IActionResult Edit(int ID)
        {
            //注意,默认的Select方法需要对应的IDAL接口继承了ISelectSingleDAL<{{TABLE_NAME}}>接口
            var model = BLLInstance.Select(new {{TABLE_NAME}}{  ID = ID});
            if (model.Success)
            {
                ViewBag.ModelString = JsonConvert.SerializeObject(model.Data, new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.MicrosoftDateFormat, DateFormatString = "yyyy-MM-dd HH:mm:ss", StringEscapeHandling = Newtonsoft.Json.StringEscapeHandling.EscapeHtml });

            } 
            return View(model);
        }


        [HttpPost]
        [JsonMethod]
        public IActionResult Edit({{TABLE_NAME}} model)
        {
            var result = BLLInstance.Update(model);
            return Json(result);
        }

        [JsonMethod]
        public IActionResult Delete(int ID)
        {
            var result = BLLInstance.Delete(new {{TABLE_NAME}}() {  ID = ID });
            return Json(result);
        }
        #endregion

    }
}
