<template>
  <div class="users-page">
    <el-form :inline="true" :model="searchForm" class="search-bar">
      <el-form-item>
        <el-input v-model="searchForm.keyword" placeholder="搜索用户名/显示名" clearable @clear="handleSearch" @keyup.enter="handleSearch" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="handleSearch">查询</el-button>
        <el-button v-perm="'add'" type="success" @click="openDialog()">新增用户</el-button>
      </el-form-item>
    </el-form>

    <el-table :data="tableData" border stripe v-loading="loading" style="width: 100%">
      <el-table-column prop="id" label="ID" width="80" />
      <el-table-column prop="userName" label="用户名" />
      <el-table-column prop="displayName" label="显示名" />
      <el-table-column label="角色" min-width="140">
        <template #default="{ row }">
          <el-tag v-for="r in rolesOf(row)" :key="r.id" size="small" style="margin-right: 4px">{{ r.name }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="状态" width="80">
        <template #default="{ row }">
          <el-tag :type="row.isEnabled ? 'success' : 'danger'">{{ row.isEnabled ? '启用' : '禁用' }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="createTime" label="创建时间" width="120" />
      <el-table-column v-if="canSeeOps" label="操作" width="160" fixed="right">
        <template #default="{ row }">
          <el-button v-perm="'edit'" type="primary" size="small" @click="openDialog(row)">编辑</el-button>
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
    <el-dialog :title="dialogTitle" v-model="dialogVisible" width="520px" @closed="resetForm">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="用户名" prop="userName">
          <el-input v-model="form.userName" :disabled="isEdit" placeholder="请输入用户名" />
        </el-form-item>
        <el-form-item v-if="!isEdit" label="初始密码" prop="password">
          <el-input v-model="form.password" type="password" show-password placeholder="不少于 6 位" />
        </el-form-item>
        <el-form-item v-else label="重置密码" prop="newPassword">
          <el-input v-model="form.newPassword" type="password" show-password placeholder="留空表示不修改密码" />
        </el-form-item>
        <el-form-item label="显示名" prop="displayName">
          <el-input v-model="form.displayName" placeholder="请输入显示名" />
        </el-form-item>
        <el-form-item label="角色" prop="roleIds">
          <el-select v-model="form.roleIds" multiple placeholder="请选择角色（可多选）" style="width: 100%">
            <el-option v-for="r in allRoles" :key="r.id" :label="r.name" :value="r.id" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="isEdit" label="状态">
          <el-switch v-model="form.isEnabled" active-text="启用" inactive-text="禁用" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSave" :loading="saving">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useUserStore } from '@/stores/user'
import {
  getUsersPage, getUserById, createUser, updateUser, deleteUser,
  type User
} from '@/api/users'
import { getRolesAll, type Role } from '@/api/roles'

defineOptions({ name: 'Users' })

const userStore = useUserStore()

// 搜索
const searchForm = reactive({ keyword: '' })

// 列表 + 分页
const tableData = ref<User[]>([])
const loading = ref(false)
const pagination = reactive({ pageIndex: 1, pageSize: 10, total: 0 })

async function fetchData() {
  loading.value = true
  try {
    const res = await getUsersPage({ ...searchForm, pageIndex: pagination.pageIndex, pageSize: pagination.pageSize })
    tableData.value = res.items ?? []
    pagination.total = res.total ?? 0
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  pagination.pageIndex = 1
  fetchData()
}

// 角色下拉数据（id -> 角色对象映射）
const allRoles = ref<Role[]>([])
const roleMap = computed(() => new Map(allRoles.value.map((r) => [r.id, r])))
function rolesOf(row: User) {
  return (row.roleIds ?? []).map((id) => roleMap.value.get(id)).filter(Boolean) as Role[]
}

// 新增/编辑弹窗
const dialogVisible = ref(false)
const isEdit = ref(false)
const editId = ref(0)
const saving = ref(false)
const formRef = ref<FormInstance>()
const form = reactive({
  userName: '',
  password: '',
  newPassword: '',
  displayName: '',
  roleIds: [] as number[],
  isEnabled: true,
})
const rules: FormRules = {
  userName: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [
    { required: true, message: '请输入初始密码', trigger: 'blur' },
    { min: 6, message: '密码不少于 6 位', trigger: 'blur' },
  ],
  newPassword: [{ min: 6, message: '密码不少于 6 位', trigger: 'blur' }],
}

const dialogTitle = computed(() => (isEdit.value ? '编辑用户' : '新增用户'))

/** 无编辑/删除权限时隐藏整个操作列 */
const canSeeOps = computed(() => userStore.hasButton('/users', 'edit') || userStore.hasButton('/users', 'delete'))

async function openDialog(row?: User) {
  if (row) {
    isEdit.value = true
    editId.value = row.id
    // 详情接口带回 roleIds，保证回显准确
    const detail = await getUserById(row.id)
    form.userName = detail.userName
    form.displayName = detail.displayName ?? ''
    form.roleIds = detail.roleIds ?? []
    form.isEnabled = detail.isEnabled !== false
    form.newPassword = ''
  } else {
    isEdit.value = false
    editId.value = 0
    form.userName = ''
    form.password = ''
    form.displayName = ''
    form.roleIds = []
    form.isEnabled = true
  }
  dialogVisible.value = true
}

function resetForm() {
  formRef.value?.resetFields()
  form.userName = ''
  form.password = ''
  form.newPassword = ''
  form.displayName = ''
  form.roleIds = []
  form.isEnabled = true
}

async function handleSave() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  saving.value = true
  try {
    if (isEdit.value) {
      const data: Record<string, unknown> = {
        displayName: form.displayName,
        roleIds: form.roleIds,
        isEnabled: form.isEnabled,
      }
      if (form.newPassword) {
        data.newPassword = form.newPassword
      }
      await updateUser(editId.value, data)
      ElMessage.success('更新成功')
    } else {
      await createUser({
        userName: form.userName,
        password: form.password,
        displayName: form.displayName,
        roleIds: form.roleIds,
      })
      ElMessage.success('新增成功')
    }
    dialogVisible.value = false
    fetchData()
  } finally {
    saving.value = false
  }
}

async function handleDelete(row: User) {
  await ElMessageBox.confirm(`确定删除用户"${row.userName}"？`, '提示', { type: 'warning' })
  await deleteUser(row.id)
  ElMessage.success('删除成功')
  fetchData()
}

onMounted(async () => {
  fetchData()
  // 角色下拉数据（roles:permission 不具备时静默降级，列表角色名显示为空 tag 不受影响）
  getRolesAll()
    .then((roles) => (allRoles.value = roles ?? []))
    .catch(() => {})
})
</script>

<style scoped>
.users-page {
  padding: 16px;
}
.search-bar {
  margin-bottom: 16px;
}
</style>
