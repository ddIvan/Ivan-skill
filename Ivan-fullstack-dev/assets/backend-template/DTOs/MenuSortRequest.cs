namespace IvanProject.DTOs;

/// <summary>菜单排序请求（拖拽排序）</summary>
public class MenuSortRequest
{
    /// <summary>被拖拽的菜单ID</summary>
    public int Id { get; set; }

    /// <summary>新的父节点ID（null=移到根级）</summary>
    public int? TargetParentId { get; set; }

    /// <summary>目标位置：插入到该节点之前（该节点ID）</summary>
    public int? BeforeId { get; set; }

    /// <summary>目标位置：插入到该节点之后（该节点ID）</summary>
    public int? AfterId { get; set; }
}
