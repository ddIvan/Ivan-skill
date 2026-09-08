using IvanProject.Common;
using IvanProject.DAL;
using IvanProject.Models;
using Microsoft.EntityFrameworkCore;

namespace IvanProject.BLL;

/// <summary>
/// 用户管理业务（示例：展示标准 CRUD + 分页 + 搜索模式，新业务模块参照此写法）
/// </summary>
public class UserManageService
{
    private readonly AppDbContext _db;

    public UserManageService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>分页查询（keyword 可选，模糊搜索用户名/显示名）</summary>
    public async Task<PageResult<User>> GetPageAsync(string? keyword, int pageIndex, int pageSize)
    {
        var query = _db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(u => u.UserName.Contains(keyword) || (u.DisplayName != null && u.DisplayName.Contains(keyword)));
        }
        var total = await query.CountAsync();
        var list = await query.OrderByDescending(u => u.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PageResult<User> { Total = total, Items = list };
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _db.Users.FindAsync(id);
    }

    public async Task UpdateAsync(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _db.Users.FindAsync(id)
            ?? throw new BusinessException("用户不存在");
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }
}

/// <summary>分页返回结构（与前端 el-table + el-pagination 配套）</summary>
public class PageResult<T>
{
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}
