<template>
  <div class="menus-page">
    <div class="toolbar">
      <el-button v-perm="'add'" type="primary" @click="openDialog()">新增一级节点</el-button>
      <el-button @click="expandAll">全部展开</el-button>
      <el-button @click="collapseAll">全部折叠</el-button>
      <span class="tip">拖拽行可排序或移动节点</span>
    </div>

    <!-- 树形表格 -->
    <el-table
      ref="tableRef"
      :data="treeData"
      row-key="id"
      border
      stripe
      :load="lazyLoad"
      lazy
      :tree-props="{ children: 'children', hasChildren: 'hasChildren' }"
      :default-expand-all="false"
      style="width: 100%"
      @row-drag-end="handleDragEnd"
    >
      <el-table-column label="名称" prop="name" min-width="200">
        <template #default="{ row }">
          <span :style="{ paddingLeft: row.level * 20 + 'px' }">
            <el-icon v-if="row.icon" style="margin-right: 6px"><component :is="row.icon" /></el-icon>
            {{ row.name }}
          </span>
        </template>
      </el-table-column>
      <el-table-column label="路由路径" prop="path" width="150">
        <template #default="{ row }">{{ row.path || '-' }}</template>
      </el-table-column>
      <el-table-column label="类型" width="150" align="left">
        <template #default="{ row }">
          <el-tag :type="row.menuType === 1 ? 'info' : row.menuType === 3 ? 'warning' : 'success'" size="small">
            {{ nodeTypeLabel(row.menuType) }}
          </el-tag>
          <el-tag v-if="row.menuType === 3" type="danger" size="small" style="margin-left: 4px">
            {{ row.permissionCode }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="层级" prop="level" width="60" align="center" />
      <el-table-column label="排序" prop="sort" width="60" align="center" />
      <el-table-column label="可见" width="60" align="center">
        <template #default="{ row }">
          <el-tag :type="row.isVisible !== false ? 'success' : 'info'" size="small">
            {{ row.isVisible !== false ? '是' : '否' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="启用" width="60" align="center">
        <template #default="{ row }">
          <el-switch
            :model-value="row.isEnabled !== false"
            size="small"
            @change="(val: boolean) => handleToggleEnabled(row, val)"
          />
        </template>
      </el-table-column>
      <el-table-column label="绑定接口" min-width="200">
        <template #default="{ row }">
          <span v-if="row.controllerAction" style="font-size: 12px">{{ row.controllerAction }}</span>
          <span v-else style="color: #909399">-</span>
        </template>
      </el-table-column>
      <el-table-column v-if="canSeeOps" label="操作" width="300" fixed="right">
        <template #default="{ row }">
          <el-button v-if="row.menuType !== 3" v-perm="'add'" type="primary" size="small" link @click="openDialog(row, 'child')">添加子节点</el-button>
          <el-button v-perm="'edit'" type="primary" size="small" link @click="openDialog(row, 'edit')">编辑</el-button>
          <el-button v-if="row.menuType !== 3" v-perm="'edit'" type="primary" size="small" link @click="openPermDialog(row)">权限管理</el-button>
          <el-button v-perm="'delete'" type="danger" size="small" link @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 新增/编辑弹窗 -->
    <el-dialog :title="dialogTitle" v-model="dialogVisible" width="520px" @closed="resetForm">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="父节点" prop="parentId">
          <el-tree-select
            v-model="form.parentId"
            :data="parentOptions"
            :props="{ label: 'name', value: 'id', children: 'children' }"
            placeholder="不选则为一级菜单"
            clearable
            check-strictly
            :render-after-expand="false"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入菜单名称" />
        </el-form-item>
        <el-form-item label="类型" prop="menuType">
          <el-radio-group v-model="form.menuType" :disabled="isEdit">
            <el-radio-button :value="1">目录</el-radio-button>
            <el-radio-button :value="2">菜单</el-radio-button>
            <el-radio-button :value="3">按钮</el-radio-button>
            <div v-if="isEdit" class="form-tip">编辑时不可变更类型</div>
          </el-radio-group>
        </el-form-item>
        <el-form-item v-if="form.menuType === 2" label="路由路径" prop="path">
          <el-input v-model="form.path" placeholder="如 /users（菜单节点必填）" />
        </el-form-item>
        <el-form-item v-if="form.menuType === 3" label="权限编码" prop="permissionCode">
          <el-input v-model="form.permissionCode" placeholder="两段式如 users:edit（全局唯一）" />
        </el-form-item>
        <el-form-item v-if="form.menuType === 3" label="绑定接口" prop="controllerAction">
          <el-select
            v-model="form.controllerAction"
            placeholder="选择 控制器.方法（选填）"
            clearable
            filterable
            style="width: 100%"
          >
            <el-option
              v-for="opt in bindableActions"
              :key="opt.fullName"
              :value="opt.fullName"
              :label="`${opt.fullName} [${opt.httpMethod}]`"
            >
              <div class="bind-option">
                <span>{{ opt.fullName }}</span>
                <span class="bind-option-meta">{{ opt.httpMethod }} {{ opt.route }}<template v-if="opt.permCode"> · {{ opt.permCode }}</template></span>
              </div>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item v-if="form.menuType !== 3" label="图标" prop="icon">
          <el-input v-model="form.icon" placeholder="Element Plus 图标名，如 User" />
        </el-form-item>
        <el-form-item label="排序" prop="sort">
          <el-input-number v-model="form.sort" :min="0" />
        </el-form-item>
        <el-form-item v-if="form.menuType !== 3" label="是否可见" prop="isVisible">
          <el-switch v-model="form.isVisible" />
        </el-form-item>
        <el-form-item label="是否启用" prop="isEnabled">
          <el-switch v-model="form.isEnabled" />
        </el-form-item>
        <el-form-item v-if="form.menuType !== 3" label="备注" prop="remark">
          <el-input v-model="form.remark" type="textarea" :rows="2" maxlength="200" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave" :loading="saving">保存</el-button>
      </template>
    </el-dialog>

    <!-- 权限管理弹窗：查看/维护所选节点下的按钮（统一树模型：按钮=子节点） -->
    <el-dialog
      :title="`权限管理 — ${permRow?.name || ''}`"
      v-model="permDialogVisible"
      width="860px"
      @closed="permRow = null"
    >
      <div v-if="permRow" class="form-tip" style="margin-bottom: 12px">
        按钮即所选节点的<b>子节点（类型=按钮）</b>：权限编码两段式 {resource}:{action}（resource=路由路径，全局唯一，[RequirePerm] 与 v-perm 共用）；
        绑定接口对应控制器 Action，用于前后端一致性核对
      </div>
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px">
        <span style="font-weight: 600">按钮列表</span>
        <el-button v-perm="'add'" type="primary" size="small" @click="openDialog(permRow!, 'child', true)">新增按钮</el-button>
      </div>
      <el-table :data="permButtonRows" size="small" border max-height="420">
        <el-table-column label="名称" prop="name" min-width="110" />
        <el-table-column label="权限编码" prop="permissionCode" min-width="160" />
        <el-table-column label="绑定接口" prop="controllerAction" min-width="200">
          <template #default="{ row }">
            <span v-if="row.controllerAction" style="font-size: 12px">{{ row.controllerAction }}</span>
            <span v-else style="color: #909399">-</span>
          </template>
        </el-table-column>
        <el-table-column label="启用" width="70" align="center">
          <template #default="{ row }">
            <el-switch :model-value="row.isEnabled !== false" size="small" @change="(val: boolean) => handleToggleEnabled(row, val)" />
          </template>
        </el-table-column>
        <el-table-column v-if="canSeeBtnOps" label="操作" width="130" align="center">
          <template #default="{ row }">
            <el-button v-perm="'edit'" size="small" link type="primary" @click="openDialog(row, 'edit', true)">编辑</el-button>
            <el-button v-perm="'delete'" size="small" link type="danger" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import type { TableInstance } from 'element-plus'
import {
  getMenusAll,
  getMenuChildren,
  createMenu,
  updateMenu,
  setMenuEnabled,
  deleteMenu,
  type Menu,
  type MenuTreeNode,
  type MenuCreateRequest,
  type MenuUpdateRequest
} from '@/api/menus'
import { getBindableActions, type BindableAction } from '@/api/menu-actions'
import { useRoute } from 'vue-router'
import { useUserStore } from '@/stores/user'

defineOptions({ name: 'Menus' })

const route = useRoute()
const userStore = useUserStore()
/** 无新增/编辑/删除权限时隐藏主表操作列（v-perm 仅移除按钮本身，列头仍会渲染空列） */
const canSeeOps = computed(() =>
  ['add', 'edit', 'delete'].some((k) => userStore.hasButton(route.path, k))
)
/** 无编辑/删除权限时隐藏按钮管理弹窗的操作列 */
const canSeeBtnOps = computed(() =>
  ['edit', 'delete'].some((k) => userStore.hasButton(route.path, k))
)

// ==================== 数据 ====================

const tableRef = ref<TableInstance>()
const treeData = ref<MenuTreeNode[]>([])
const loading = ref(false)

async function fetchTree() {
  loading.value = true
  try {
    // 获取全量树（平铺 + children 结构）
    const all = await getMenusAll()
    // 转换为懒加载格式：只加载根节点，标记 hasChildren
    treeData.value = buildLazyTree(all)
  } finally {
    loading.value = false
  }
}

// ==================== 节点类型 ====================

function nodeTypeLabel(t: number): string {
  return t === 1 ? '目录' : t === 3 ? '按钮' : '菜单'
}

/** 后端全部可绑定 Action（反射扫描结果，下拉数据源） */
const bindableActions = ref<BindableAction[]>([])

async function fetchBindableActions() {
  if (bindableActions.value.length) return
  try {
    bindableActions.value = await getBindableActions()
  } catch {
    // 扫描接口失败不阻断页面
  }
}

/** 将平铺列表转为懒加载树：根节点直接显示，子节点按需加载 */
function buildLazyTree(flatList: Menu[]): MenuTreeNode[] {
  const map = new Map<number, MenuTreeNode>()
  const roots: MenuTreeNode[] = []

  // 第一遍：创建所有节点（不设 children）
  for (const m of flatList) {
    map.set(m.id, {
      id: m.id,
      name: m.name,
      menuType: m.menuType,
      path: m.path,
      icon: m.icon,
      parentId: m.parentId ?? null,
      fullPath: m.fullPath,
      level: m.level,
      sort: m.sort,
      isVisible: m.isVisible !== false,
      isEnabled: m.isEnabled !== false,
      permissionCode: m.permissionCode,
      controllerAction: m.controllerAction,
      remark: m.remark,
      hasChildren: false,
      children: []
    })
  }

  // 第二遍：标记 hasChildren + 建立父子关系（仅根节点的直接子节点）
  for (const m of flatList) {
    const node = map.get(m.id)!
    if (m.parentId && map.has(m.parentId)) {
      map.get(m.parentId)!.hasChildren = true
    }
    if (!m.parentId || !map.has(m.parentId)) {
      roots.push(node)
    }
  }

  // 按 sort 排序
  roots.sort((a, b) => a.sort - b.sort)
  return roots
}

/** 懒加载回调：展开节点时加载其子节点 */
async function lazyLoad(row: MenuTreeNode, _treeNode: unknown, resolve: (children: MenuTreeNode[]) => void) {
  try {
    const children = await getMenuChildren(row.id)
    resolve(children)
  } catch {
    resolve([])
  }
}

// ==================== 展开/折叠 ====================

function expandAll() {
  // 递归展开所有节点
  const expandRecursive = (nodes: MenuTreeNode[]) => {
    nodes.forEach(n => {
      tableRef.value?.toggleRowExpansion(n, true)
      if (n.children?.length) expandRecursive(n.children)
    })
  }
  expandRecursive(treeData.value)
}

function collapseAll() {
  treeData.value.forEach(n => {
    tableRef.value?.toggleRowExpansion(n, false)
  })
}

// ==================== 拖拽排序 ====================

async function handleDragEnd(
  _newIndex: number,
  _oldIndex: number,
  _rows: MenuTreeNode[],
  _event: Event
) {
  // element-plus el-table 的 row-drag-end 事件参数有限
  // 此处简化处理：提示用户拖拽完成后需手动调整排序号
  // 实际项目中可结合拖拽起始和结束位置调用 reorderMenu API
  ElMessage.info('拖拽排序功能需结合具体业务实现，当前暂以排序号为准')
}

// ==================== 新增/编辑 ====================

const dialogVisible = ref(false)
const isEdit = ref(false)
const editId = ref(0)
const saving = ref(false)
const formRef = ref<FormInstance>()
const form = reactive({
  name: '',
  menuType: 2 as number,
  path: '',
  icon: '',
  parentId: undefined as number | undefined,
  sort: 0,
  isVisible: true,
  isEnabled: true,
  permissionCode: '',
  controllerAction: '',
  remark: ''
})

const rules: FormRules = {
  name: [{ required: true, message: '请输入名称', trigger: 'blur' }],
  path: [{ required: true, message: '菜单节点必须填写路由路径', trigger: 'blur' }],
  permissionCode: [
    { required: true, message: '按钮节点必须填写权限编码', trigger: 'blur' },
    { pattern: /^[a-zA-Z][a-zA-Z0-9_]*(:[a-zA-Z][a-zA-Z0-9_]*)+$/, message: '格式：两段式及以上，如 users:edit', trigger: 'blur' }
  ]
}

// ==================== 权限管理弹窗（按钮子节点表格） ====================

const permDialogVisible = ref(false)
const permRow = ref<MenuTreeNode | null>(null)

/** 所选节点的按钮子节点（从全量数据中过滤） */
const permButtonRows = computed(() => {
  if (!permRow.value) return []
  const result: MenuTreeNode[] = []
  const walk = (nodes: MenuTreeNode[]) => {
    for (const n of nodes) {
      if (n.parentId === permRow.value!.id && n.menuType === 3) result.push(n)
      if (n.children?.length) walk(n.children)
    }
  }
  walk(treeData.value)
  return result
})

function openPermDialog(row: MenuTreeNode) {
  permRow.value = row
  permDialogVisible.value = true
  fetchBindableActions()
}

const dialogTitle = computed(() => {
  if (isEdit.value) return permEditingButton ? '编辑按钮' : '编辑节点'
  if (permRow.value) return '新增按钮'
  if (editId.value > 0) return '添加子节点'
  return '新增节点'
})

const permEditingButton = computed(() => form.menuType === 3)

// 父节点下拉选项：从 treeData 递归构建
const parentOptions = computed(() => {
  // 使用全量树（不含懒加载占位）
  return treeData.value
})

function openDialog(row?: MenuTreeNode, mode: 'edit' | 'child' = 'edit', fromPerm = false) {
  if (row && mode === 'edit') {
    isEdit.value = true
    editId.value = row.id
    form.name = row.name
    form.menuType = row.menuType
    form.path = row.path || ''
    form.icon = row.icon || ''
    form.parentId = row.parentId ?? undefined
    form.sort = row.sort
    form.isVisible = row.isVisible
    form.isEnabled = row.isEnabled
    form.permissionCode = row.permissionCode || ''
    form.controllerAction = row.controllerAction || ''
    form.remark = row.remark || ''
  } else if (row && mode === 'child') {
    isEdit.value = false
    editId.value = 0
    form.name = ''
    form.menuType = fromPerm ? 3 : 2
    form.path = ''
    form.icon = ''
    form.parentId = row.id
    form.sort = 0
    form.isVisible = true
    form.isEnabled = true
    form.permissionCode = fromPerm && row.path
      ? row.path.replace(/^\//, '').replace(/\//g, ':') + ':'
      : ''
    form.controllerAction = ''
    form.remark = ''
  } else {
    isEdit.value = false
    editId.value = 0
    form.name = ''
    form.menuType = 2
    form.path = ''
    form.icon = ''
    form.parentId = undefined
    form.sort = 0
    form.isVisible = true
    form.isEnabled = true
    form.permissionCode = ''
    form.controllerAction = ''
    form.remark = ''
  }
  dialogVisible.value = true
  fetchBindableActions()
}

function resetForm() {
  formRef.value?.resetFields()
}

async function handleSave() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  saving.value = true
  try {
    const payload: MenuCreateRequest | MenuUpdateRequest = {
      name: form.name,
      menuType: form.menuType,
      path: form.menuType === 2 ? form.path || undefined : undefined,
      icon: form.menuType !== 3 ? form.icon || undefined : undefined,
      parentId: form.parentId || undefined,
      sort: form.sort,
      isVisible: form.isVisible,
      isEnabled: form.isEnabled,
      permissionCode: form.menuType === 3 ? form.permissionCode.trim() : undefined,
      controllerAction: form.menuType === 3 ? form.controllerAction || undefined : undefined,
      remark: form.remark || undefined
    }
    if (isEdit.value) {
      await updateMenu(editId.value, payload as MenuUpdateRequest)
      ElMessage.success('更新成功')
    } else {
      await createMenu(payload as MenuCreateRequest)
      ElMessage.success('新增成功')
    }
    dialogVisible.value = false
    fetchTree()
  } finally {
    saving.value = false
  }
}

// ==================== 启用/禁用 ====================

async function handleToggleEnabled(row: MenuTreeNode, val: boolean) {
  try {
    await setMenuEnabled(row.id, val)
    row.isEnabled = val
    ElMessage.success(val ? '已启用' : '已禁用')
  } catch {
    // 失败时恢复原值
    row.isEnabled = !val
  }
}

// ==================== 删除 ====================

async function handleDelete(row: MenuTreeNode) {
  await ElMessageBox.confirm(
    `确定删除菜单"${row.name}"及其所有子节点？此操作不可恢复。`,
    '删除确认',
    { type: 'warning', confirmButtonText: '确定删除' }
  )
  await deleteMenu(row.id)
  ElMessage.success('删除成功')
  fetchTree()
}

// ==================== 生命周期 ====================

onMounted(() => {
  fetchTree()
})
</script>

<style scoped>
.menus-page {
  padding: 16px;
}

.toolbar {
  margin-bottom: 16px;
  display: flex;
  align-items: center;
  gap: 8px;
}

.toolbar .tip {
  margin-left: 16px;
  color: #909399;
  font-size: 13px;
}

.form-tip {
  font-size: 12px;
  color: #909399;
  line-height: 1.4;
}

/* 权限配置弹窗：权限编码错误提示 */
.perm-error-text {
  font-size: 12px;
  color: var(--el-color-danger);
  line-height: 1.3;
  margin-top: 2px;
}

/* 权限配置弹窗：绑定接口下拉选项的双行展示 */
.bind-option {
  display: flex;
  flex-direction: column;
  line-height: 1.4;
}

.bind-option-meta {
  font-size: 12px;
  color: #909399;
}

:deep(.el-table .el-table__row) {
  cursor: grab;
}

:deep(.el-table .el-table__row:active) {
  cursor: grabbing;
}
</style>
