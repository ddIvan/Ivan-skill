using Ivan.Common;
using IvanProject.BLL.Interface;
using IvanProject.Common;
using IvanProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IvanProject.Controllers;

/// <summary>
/// 用户管理接口（示例：标准 Controller 写法 —— 薄控制器，只做参数处理并调用 BLL）
/// 注意：路由必须显式写 "api/users"，不能用 [controller]——
/// [controller] 解析为控制器类名去 Controller 后缀的小写（UserController → "user" 单数），
/// 与前端约定的复数路径 /api/users 不一致会导致 404
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "admin")]
public class UserController : ControllerBase
{
    private readonly IUsersBLL _usersBLL;

    public UserController(IUsersBLL usersBLL)
    {
        _usersBLL = usersBLL;
    }

    /// <summary>分页查询用户</summary>
    [HttpGet]
    public ApiResult<PageResult<Users>> GetPage([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        var searchModel = new PageSearchModel();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            searchModel["keyword"] = keyword.Trim();
        }
        searchModel.Page = pageIndex < 1 ? 1 : pageIndex;
        searchModel.Limit = pageSize < 1 ? 10 : pageSize;

        var pageResult = _usersBLL.List(searchModel);
        return ApiResult<PageResult<Users>>.Ok(new PageResult<Users>
        {
            Total = pageResult.TotalRecord,
            Items = pageResult.Data ?? new List<Users>()
        });
    }

    /// <summary>根据 ID 查询用户</summary>
    [HttpGet("{id:int}")]
    public ApiResult<Users?> GetById(int id)
    {
        return ApiResult<Users?>.Ok(_usersBLL.GetById(id));
    }

    /// <summary>更新用户</summary>
    [HttpPut("{id:int}")]
    public ApiResult Update(int id, Users user)
    {
        user.Id = id;
        var result = _usersBLL.Modify(user);
        return result.Success ? ApiResult.Ok("更新成功") : ApiResult.Fail(result.Message ?? "更新失败");
    }

    /// <summary>删除用户</summary>
    [HttpDelete("{id:int}")]
    public ApiResult Delete(int id)
    {
        var result = _usersBLL.Remove(id);
        return result.Success ? ApiResult.Ok("删除成功") : ApiResult.Fail(result.Message ?? "删除失败");
    }
}
