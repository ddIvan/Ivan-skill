using System.Security.Claims;
using IvanProject.BLL;
using IvanProject.Common;
using IvanProject.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IvanProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>登录</summary>
    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        var result = _authService.Login(request);
        return Ok(ApiResult<LoginResult>.Ok(result, "登录成功"));
    }

    /// <summary>注册（按需使用）</summary>
    [HttpPost("register")]
    public IActionResult Register(RegisterRequest request)
    {
        _authService.Register(request.UserName, request.Password, request.DisplayName);
        return Ok(ApiResult.Ok("注册成功"));
    }

    /// <summary>获取当前登录用户信息</summary>
    [Authorize]
    [HttpGet("profile")]
    public IActionResult Profile()
    {
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "";
        var result = _authService.GetProfile(userName);
        return Ok(ApiResult<LoginResult>.Ok(result));
    }
}
