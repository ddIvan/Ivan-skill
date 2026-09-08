using IvanProject.BLL;
using IvanProject.Common;
using IvanProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IvanProject.Controllers;

/// <summary>
/// 用户管理接口（示例：标准 Controller 写法 —— 薄控制器，只做参数处理并调用 BLL）
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class UserController : ControllerBase
{
    private readonly UserManageService _service;

    public UserController(UserManageService service)
    {
        _service = service;
    }

    /// <summary>分页查询用户</summary>
    [HttpGet]
    public async Task<ApiResult<PageResult<User>>> GetPage([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetPageAsync(keyword, pageIndex, pageSize);
        return ApiResult<PageResult<User>>.Ok(result);
    }

    /// <summary>根据 ID 查询用户</summary>
    [HttpGet("{id:int}")]
    public async Task<ApiResult<User?>> GetById(int id)
    {
        return ApiResult<User?>.Ok(await _service.GetByIdAsync(id));
    }

    /// <summary>更新用户</summary>
    [HttpPut("{id:int}")]
    public async Task<ApiResult> Update(int id, User user)
    {
        user.Id = id;
        await _service.UpdateAsync(user);
        return ApiResult.Ok("更新成功");
    }

    /// <summary>删除用户</summary>
    [HttpDelete("{id:int}")]
    public async Task<ApiResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return ApiResult.Ok("删除成功");
    }
}
