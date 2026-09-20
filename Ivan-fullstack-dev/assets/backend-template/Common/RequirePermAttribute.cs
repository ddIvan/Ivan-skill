namespace IvanProject.Common;

/// <summary>
/// 权限声明注解，用于 Controller Action 或 Controller 级别的细粒度权限控制。
/// 支持多个权限标签（切片），用户只需拥有其中任一权限即可通过校验。
///
/// 权限编码为两段式 {resource}:{action}：resource 由菜单路由路径派生（去掉开头 /、/ 替换为 :，如 /users → users），
/// action 即动作字典（MenuActions）中的 ActionKey，每个按钮请求对应一个独立的 action 标识，
/// 与前端 v-perm 指令使用的按钮 Key 一一对应。
///
/// 用法示例（单个权限）：
///   [RequirePerm("users:edit")]
///   public ApiResult Edit(User model) { }
///
/// 用法示例（多个权限，满足任一即可）：
///   [RequirePerm("orders:export", "orders:audit")]
///   public ApiResult BatchAction() { }
///
/// 用法示例（类级别权限 + 方法级别权限叠加）：
///   [RequirePerm("users:view")]
///   public class UserController : ControllerBase
///   {
///       [RequirePerm("users:edit")]
///       public ApiResult Edit() { }
///   }
///   方法级别注解会覆盖类级别注解，而非叠加。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequirePermAttribute : Attribute
{
    /// <summary>所需的权限编码集合</summary>
    public string[] Permissions { get; }

    /// <summary>
    /// 声明权限要求。
    /// </summary>
    /// <param name="permissions">一个或多个权限编码，用户拥有其中任一即可。两段式格式如 "users:edit"</param>
    public RequirePermAttribute(params string[] permissions)
    {
        Permissions = permissions;
    }
}
