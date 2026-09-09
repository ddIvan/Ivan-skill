using IvanProject.BLL.Interface;
using IvanProject.Common;
using IvanProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IvanProject.Controllers;

/// <summary>
/// 菜单管理接口
/// </summary>
[ApiController]
[Route("api/menus")]
[Authorize]
public class MenusController : ControllerBase
{
    private readonly IMenusBLL _menusBLL;

    public MenusController(IMenusBLL menusBLL)
    {
        _menusBLL = menusBLL;
    }

    /// <summary>获取菜单树</summary>
    [HttpGet("tree")]
    public ApiResult<List<Menus>> GetTree()
    {
        return ApiResult<List<Menus>>.Ok(_menusBLL.GetTree());
    }

    /// <summary>获取所有菜单（平铺列表）</summary>
    [HttpGet]
    public ApiResult<List<Menus>> GetAll()
    {
        return ApiResult<List<Menus>>.Ok(_menusBLL.GetAll());
    }

    /// <summary>根据 ID 查询菜单</summary>
    [HttpGet("{id:int}")]
    public ApiResult<Menus?> GetById(int id)
    {
        return ApiResult<Menus?>.Ok(_menusBLL.GetById(id));
    }

    /// <summary>新增菜单</summary>
    [HttpPost]
    public ApiResult<int> Create(Menus menu)
    {
        var result = _menusBLL.Create(menu);
        return result.Success ? ApiResult<int>.Ok(result.Data, "新增成功") : ApiResult<int>.Fail(result.Message ?? "新增失败");
    }

    /// <summary>更新菜单</summary>
    [HttpPut("{id:int}")]
    public ApiResult Update(int id, Menus menu)
    {
        menu.Id = id;
        var result = _menusBLL.Modify(menu);
        return result.Success ? ApiResult.Ok("更新成功") : ApiResult.Fail(result.Message ?? "更新失败");
    }

    /// <summary>删除菜单</summary>
    [HttpDelete("{id:int}")]
    public ApiResult Delete(int id)
    {
        var result = _menusBLL.Remove(id);
        return result.Success ? ApiResult.Ok("删除成功") : ApiResult.Fail(result.Message ?? "删除失败");
    }
}
