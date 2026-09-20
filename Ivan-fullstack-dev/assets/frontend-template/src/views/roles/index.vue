<template>
  <div class="roles-page">
    <!-- 搜索/操作栏 -->
    <el-form :inline="true" :model="searchForm" class="search-bar">
      <el-form-item>
        <el-input v-model="searchForm.keyword" placeholder="搜索角色名称" clearable @clear="fetchData" @keyup.enter="fetchData" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="fetchData">查询</el-button>
        <el-button v-perm="'add'" type="success" @click="openDialog()">新增角色</el-button>
      </el-form-item>
    </el-form>

    <!-- 角色列表 -->
    <el-table :data="tableData" border stripe v-loading="loading" style="width: 100%">
      <el-table-column prop="id" label="ID" width="80" />
      <el-table-column prop="name" label="角色名称" />
      <el-table-column prop="code" label="角色编码" />
      <el-table-column prop="description" label="描述" />
      <el-table-column prop="createTime" label="创建时间" width="120" />
      <el-table-column v-if="canSeeOps" label="操作" width="360" fixed="right">
        <template #default="{ row }">
          <el-button v-perm="'edit'" type="primary" size="small" @click="openDialog(row)">编辑</el-button>
          <el-button v-perm="'permission'" type="warning" size="small" @click="openPermission(row)">权限</el-button>
          <el-button v-perm="'delete'" type="danger" size="small" @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 分页 -->
    <el-pagination
      v-model:current-page="pagination.pageIndex"
      v-model:page-size="pagination.pageSize"
      :total="pagination.total"
      layout="total, sizes, prev, pager, next"
      @size-change="fetchData"
      @current-change="fetchData"
      style="margin-top: 16px; justify-content: flex-end"
    />

    <!-- 新增/编辑弹窗 -->
    <el-dialog :title="dialogTitle" v-model="dialogVisible" width="500px" @closed="resetForm">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入角色名称" />
        </el-form-item>
        <el-form-item label="编码" prop="code">
          <el-input v-model="form.code" placeholder="请输入角色编码（如 admin）" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input v-model="form.description" type="textarea" placeholder="请输入描述" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave" :loading="saving">保存</el-button>
      </template>
    </el-dialog>

    <!-- 权限配置弹窗（统一权限树：目录/菜单/按钮一次勾选授权） -->
    <el-dialog :title="'权限配置 - ' + permRoleName" v-model="permVisible" width="640px" @closed="resetPerm">
      <div class="perm-dialog" v-loading="treeLoading" element-loading-text="权限加载中...">
        <div class="perm-section">
          <h4>权限树（含按钮节点，勾选即授权）</h4>
          <div class="form-tip" style="margin: 4px 0 8px">
            勾选自动保存；按钮节点显示其权限编码（{resource}:{action}，与后端 [RequirePerm] 一致）
          </div>
          <div v-if="permLoadError" class="perm-error">
            <span>权限数据加载失败，为避免覆盖已有权限，已暂停勾选保存。</span>
            <el-button type="primary" size="small" link @click="retryPermLoad">重试</el-button>
          </div>
          <el-tree
            ref="menuTreeRef"
            :data="menuTree"
            show-checkbox
            node-key="id"
            :props="{ label: 'name', children: 'children' }"
            @check="onMenuCheck"
            style="margin-top: 8px"
          >
            <template #default="{ data }">
              <span class="perm-node">
                <span>{{ data.name }}</span>
                <el-tag v-if="data.menuType === 3" type="warning" size="small" style="margin-left: 6px">
                  {{ data.permissionCode }}
                </el-tag>
              </span>
            </template>
          </el-tree>
        </div>
      </div>
      <template #footer>
        <el-button @click="permVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed, nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules, ElTree } from 'element-plus'
import {
  getRolesPage, createRole, updateRole, deleteRole,
  getRoleMenus, saveRoleMenus,
  type Role
} from '@/api/roles'
import { getMenuTree, type MenuTreeNode } from '@/api/menus'
import { useRoute } from 'vue-router'
import { useUserStore } from '@/stores/user'

defineOptions({ name: 'Roles' })

const route = useRoute()
const userStore = useUserStore()
/** 无编辑/权限/删除权限时隐藏整个操作列（v-perm 仅移除按钮本身，列头仍会渲染空列） */
const canSeeOps = computed(() =>
  ['edit', 'permission', 'delete'].some((k) => userStore.hasButton(route.path, k))
)

// 搜索
const searchForm = reactive({ keyword: '' })

// 列表
const tableData = ref<Role[]>([])
const loading = ref(false)
const pagination = reactive({ pageIndex: 1, pageSize: 10, total: 0 })

async function fetchData() {
  loading.value = true
  try {
    const res = await getRolesPage({ ...searchForm, pageIndex: pagination.pageIndex, pageSize: pagination.pageSize })
    tableData.value = res.items
    pagination.total = res.total
  } finally {
    loading.value = false
  }
}

// 新增/编辑
const dialogVisible = ref(false)
const isEdit = ref(false)
const editId = ref(0)
const saving = ref(false)
const formRef = ref<FormInstance>()
const form = reactive<Partial<Role>>({ name: '', code: '', description: '' })
const rules: FormRules = {
  name: [{ required: true, message: '请输入角色名称', trigger: 'blur' }],
  code: [{ required: true, message: '请输入角色编码', trigger: 'blur' }],
}

const dialogTitle = computed(() => isEdit.value ? '编辑角色' : '新增角色')

function openDialog(row?: Role) {
  if (row) {
    isEdit.value = true
    editId.value = row.id
    form.name = row.name
    form.code = row.code
    form.description = row.description || ''
  } else {
    isEdit.value = false
    editId.value = 0
  }
  dialogVisible.value = true
}

function resetForm() {
  formRef.value?.resetFields()
  form.name = ''
  form.code = ''
  form.description = ''
}

async function handleSave() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  saving.value = true
  try {
    if (isEdit.value) {
      await updateRole(editId.value, form)
      ElMessage.success('更新成功')
    } else {
      await createRole(form)
      ElMessage.success('新增成功')
    }
    dialogVisible.value = false
    fetchData()
  } finally {
    saving.value = false
  }
}

async function handleDelete(row: Role) {
  await ElMessageBox.confirm(`确定删除角色"${row.name}"？`, '提示', { type: 'warning' })
  await deleteRole(row.id)
  ElMessage.success('删除成功')
  fetchData()
}

// 权限配置（统一权限树：勾选即授权，按钮节点为树叶子）
const permVisible = ref(false)
const permRoleId = ref(0)
const permRoleName = ref('')
const menuTree = ref<MenuTreeNode[]>([])
const menuTreeRef = ref<InstanceType<typeof ElTree>>()
const treeLoading = ref(false)
// 树与已选权限都加载成功后才允许勾选保存，防止加载失败时用户误清空已有权限
const permReady = ref(false)
const permLoadError = ref(false)
// 请求序号守卫：快速切换角色时丢弃过期响应，防止旧角色的勾选状态覆盖新角色
let permSeq = 0

async function openPermission(row: Role) {
  permRoleId.value = row.id
  permRoleName.value = row.name
  permVisible.value = true
  await loadPermData(row.id)
}

async function loadPermData(roleId: number) {
  const seq = ++permSeq
  treeLoading.value = true
  permReady.value = false
  permLoadError.value = false
  try {
    const [tree, menuIds] = await Promise.all([
      getMenuTree(),
      getRoleMenus(roleId)
    ])
    if (seq !== permSeq) return // 已切换到其他角色/已关闭，丢弃过期响应
    menuTree.value = tree
    // 等树渲染完成后再显式回显勾选（default-checked-keys 仅首次渲染生效，复用弹窗时不可靠）
    await nextTick()
    if (seq !== permSeq || !menuTreeRef.value) return
    // 仅回显叶子节点（按钮/末级菜单）：父节点勾选态由 el-tree 依据子级自动推导，
    // 避免历史数据中保存的父节点 ID 导致整棵子树被误勾选；
    // 树中已删除的节点 ID 自然被过滤，不影响其余勾选
    const leafIds = collectLeafIds(tree)
    const checkedIds = [...new Set(menuIds)].filter(id => leafIds.has(id))
    menuTreeRef.value.setCheckedKeys(checkedIds)
    permReady.value = true
  } catch {
    if (seq !== permSeq) return
    // 降级：保留弹窗与错误提示，禁用勾选保存，避免在未知状态下覆盖已有权限
    permLoadError.value = true
    ElMessage.error('权限数据加载失败，已暂停勾选保存，请重试')
  } finally {
    if (seq === permSeq) treeLoading.value = false
  }
}

/** 递归收集树中所有叶子节点 ID */
function collectLeafIds(nodes: MenuTreeNode[], acc: Set<number> = new Set()): Set<number> {
  for (const n of nodes) {
    if (n.children && n.children.length > 0) collectLeafIds(n.children, acc)
    else acc.add(n.id)
  }
  return acc
}

async function retryPermLoad() {
  await loadPermData(permRoleId.value)
}

async function onMenuCheck() {
  if (!permReady.value) {
    ElMessage.warning('权限数据尚未加载完成，暂不能修改')
    return
  }
  const checked = menuTreeRef.value?.getCheckedKeys() as number[] || []
  const halfChecked = menuTreeRef.value?.getHalfCheckedKeys() as number[] || []
  const allKeys = [...checked, ...halfChecked]
  try {
    await saveRoleMenus(permRoleId.value, allKeys)
    ElMessage.success('权限已自动保存')
  } catch {
    ElMessage.error('保存失败，请重试')
  }
}

function resetPerm() {
  permSeq++ // 使在途请求失效
  menuTree.value = []
  permReady.value = false
  permLoadError.value = false
}

onMounted(fetchData)
</script>

<style scoped>
.roles-page {
  padding: 16px;
}
.search-bar {
  margin-bottom: 16px;
}
.perm-dialog {
  max-height: 500px;
}
.perm-section h4 {
  margin: 0 0 8px 0;
  font-size: 14px;
  color: #333;
}
.perm-node {
  display: inline-flex;
  align-items: center;
}
.perm-error {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 4px 0 8px;
  padding: 6px 10px;
  border-radius: 4px;
  background: #fef0f0;
  color: #f56c6c;
  font-size: 12px;
  line-height: 1.4;
}
.form-tip {
  font-size: 12px;
  color: #909399;
  line-height: 1.4;
}
</style>
