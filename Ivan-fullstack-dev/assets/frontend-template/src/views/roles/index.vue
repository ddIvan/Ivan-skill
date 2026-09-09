<template>
  <div class="roles-page">
    <!-- 搜索/操作栏 -->
    <el-form :inline="true" :model="searchForm" class="search-bar">
      <el-form-item>
        <el-input v-model="searchForm.keyword" placeholder="搜索角色名称" clearable @clear="fetchData" @keyup.enter="fetchData" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="fetchData">查询</el-button>
        <el-button type="success" @click="openDialog()" v-if="userStore.hasButton('/roles', 'add')">新增角色</el-button>
      </el-form-item>
    </el-form>

    <!-- 角色列表 -->
    <el-table :data="tableData" border stripe v-loading="loading" style="width: 100%">
      <el-table-column prop="id" label="ID" width="80" />
      <el-table-column prop="name" label="角色名称" />
      <el-table-column prop="code" label="角色编码" />
      <el-table-column prop="description" label="描述" />
      <el-table-column prop="createTime" label="创建时间" width="120" />
      <el-table-column label="操作" width="360" fixed="right">
        <template #default="{ row }">
          <el-button type="primary" size="small" @click="openDialog(row)" v-if="userStore.hasButton('/roles', 'edit')">编辑</el-button>
          <el-button type="warning" size="small" @click="openPermission(row)" v-if="userStore.hasButton('/roles', 'permission')">权限</el-button>
          <el-button type="danger" size="small" @click="handleDelete(row)" v-if="userStore.hasButton('/roles', 'delete')">删除</el-button>
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

    <!-- 权限配置弹窗 -->
    <el-dialog title="权限配置" v-model="permVisible" width="700px" @closed="resetPerm">
      <div class="perm-dialog">
        <!-- 菜单树勾选 -->
        <div class="perm-section">
          <h4>菜单权限</h4>
          <el-tree
            ref="menuTreeRef"
            :data="menuTree"
            show-checkbox
            node-key="id"
            :default-checked-keys="checkedMenuIds"
            :props="{ label: 'name', children: 'children' }"
            @check="onMenuCheck"
            style="margin-top: 8px"
          />
        </div>

        <!-- 按钮权限（在选中菜单后显示） -->
        <div class="perm-section" style="margin-top: 20px">
          <h4>按钮权限（选择左侧菜单后配置）</h4>
          <el-select v-model="selectedMenuId" placeholder="请选择菜单" @change="loadButtonPerms" style="width: 280px; margin-top: 8px">
            <el-option v-for="m in flatMenus" :key="m.id" :label="m.name" :value="m.id" />
          </el-select>

          <div v-if="selectedMenuId" style="margin-top: 12px">
            <el-checkbox-group v-model="checkedButtons">
              <el-checkbox v-for="btn in availableButtons" :key="btn" :label="btn" style="margin-right: 16px">{{ btnLabel(btn) }}</el-checkbox>
            </el-checkbox-group>
            <div style="margin-top: 12px">
              <el-button type="primary" size="small" @click="saveButtonPerms" :loading="savingButtons">保存按钮权限</el-button>
            </div>
          </div>
        </div>
      </div>
      <template #footer>
        <el-button @click="permVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules, ElTree } from 'element-plus'
import { useUserStore } from '@/stores/user'
import {
  getRolesPage, createRole, updateRole, deleteRole,
  getRoleMenus, saveRoleMenus, getRoleButtons, saveRoleButtons,
  type Role
} from '@/api/roles'
import { getMenuTree, getMenusAll, type Menu } from '@/api/menus'

defineOptions({ name: 'Roles' })

const userStore = useUserStore()

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

// 权限配置
const permVisible = ref(false)
const permRoleId = ref(0)
const menuTree = ref<Menu[]>([])
const flatMenus = ref<Menu[]>([])
const checkedMenuIds = ref<number[]>([])
const menuTreeRef = ref<InstanceType<typeof ElTree>>()

// 按钮权限
const selectedMenuId = ref<number | null>(null)
const checkedButtons = ref<string[]>([])
const savingButtons = ref(false)
const availableButtons = ['add', 'edit', 'delete', 'export', 'import', 'permission']

function btnLabel(key: string) {
  const map: Record<string, string> = { add: '新增', edit: '编辑', delete: '删除', export: '导出', import: '导入', permission: '权限分配' }
  return map[key] || key
}

async function openPermission(row: Role) {
  permRoleId.value = row.id
  permVisible.value = true
  selectedMenuId.value = null
  checkedButtons.value = []

  const [menus, menuIds] = await Promise.all([getMenusAll(), getRoleMenus(row.id)])
  menuTree.value = await getMenuTree()
  flatMenus.value = menus
  checkedMenuIds.value = menuIds
}

function onMenuCheck() {
  const checked = menuTreeRef.value?.getCheckedKeys() as number[] || []
  const halfChecked = menuTreeRef.value?.getHalfCheckedKeys() as number[] || []
  const allKeys = [...checked, ...halfChecked]
  saveRoleMenus(permRoleId.value, allKeys)
    .then(() => ElMessage.success('菜单权限已自动保存'))
    .catch(() => ElMessage.error('保存失败'))
}

async function loadButtonPerms() {
  if (!selectedMenuId.value) return
  const buttons = await getRoleButtons(permRoleId.value)
  const keys = buttons.filter(b => b.menuId === selectedMenuId.value).map(b => b.buttonKey)
  checkedButtons.value = keys
}

async function saveButtonPerms() {
  if (!selectedMenuId.value) return
  savingButtons.value = true
  try {
    await saveRoleButtons(permRoleId.value, selectedMenuId.value, checkedButtons.value)
    ElMessage.success('按钮权限保存成功')
  } finally {
    savingButtons.value = false
  }
}

function resetPerm() {
  menuTree.value = []
  flatMenus.value = []
  checkedMenuIds.value = []
  selectedMenuId.value = null
  checkedButtons.value = []
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
</style>
