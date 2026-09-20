import type { Directive, DirectiveBinding } from 'vue'
import router from '@/router'
import { useUserStore } from '@/stores/user'

/**
 * 按钮级权限指令：根据当前登录用户的按钮权限控制元素显隐或禁用。
 *
 * 用法：
 *   <el-button v-perm="'add'">新增</el-button>              无权限时移除元素（默认）
 *   <el-button v-perm="['edit', 'permission']">编辑</el-button>  任一 Key 命中即显示
 *   <el-button v-perm:disable="'export'">导出</el-button>    无权限时禁用（置灰），不移除
 *
 * 权限数据来源：登录接口返回 buttonPermissions = { "/users": ["add","edit","delete","view",...] }
 * 判定规则：当前路由 path 作为 menuPath，value 中任一 buttonKey 命中即视为有权限。
 * 注意：remove 模式在 mounted 时判定并直接移除元素；disable 模式响应 updated 更新。
 */

/** disable 模式的禁用样式（仅注入一次） */
function ensureStyle() {
  if (document.getElementById('v-perm-style')) return
  const style = document.createElement('style')
  style.id = 'v-perm-style'
  style.textContent = '.is-perm-disabled{opacity:.5;cursor:not-allowed!important}'
  document.head.appendChild(style)
}

function apply(el: HTMLElement, binding: DirectiveBinding<string | string[]>): boolean {
  const store = useUserStore()
  const route = router.currentRoute.value
  const keys = Array.isArray(binding.value) ? binding.value : [binding.value]
  const granted = keys.some((k) => store.hasButton(route.path, k))

  if (binding.arg === 'disable') {
    ensureStyle()
    ;(el as HTMLButtonElement).disabled = !granted
    el.classList.toggle('is-perm-disabled', !granted)
  } else if (!granted) {
    el.parentNode?.removeChild(el)
  }
  return granted
}

export const vPerm: Directive<HTMLElement, string | string[]> = {
  mounted(el: HTMLElement, binding: DirectiveBinding<string | string[]>) {
    apply(el, binding)
  },
  updated(el: HTMLElement, binding: DirectiveBinding<string | string[]>) {
    // remove 模式元素可能已被移除，仅 disable 模式响应更新
    if (binding.arg === 'disable') apply(el, binding)
  },
}
