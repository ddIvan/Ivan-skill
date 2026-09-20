import request from './request'

/** 可绑定后端接口信息（后端 ControllerActionScanner 反射扫描结果） */
export interface BindableAction {
  /** 控制器.方法，如 UserController.Update */
  fullName: string
  /** HTTP 方法：GET/POST/PUT/DELETE */
  httpMethod: string
  /** 路由模板，如 api/users/{id} */
  route: string
  /** [RequirePerm] 声明的权限编码（未声明为空串），供一致性核对 */
  permCode: string
}

/** 获取可绑定的后端接口列表（供按钮节点绑定接口下拉） */
export function getBindableActions() {
  return request.get<unknown, BindableAction[]>('/menus/bindable-actions')
}
