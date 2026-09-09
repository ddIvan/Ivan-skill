namespace IvanProject.Common;

/// <summary>分页返回结构（与前端 el-table + el-pagination 配套）</summary>
public class PageResult<T>
{
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}
