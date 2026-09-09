<template>
  <div class="menus-page">
    <el-button type="primary" @click="openDialog()" style="margin-bottom: 16px">新增菜单</el-button>

    <el-table :data="tableData" border stripe row-key="id" style="width: 100%" default-expand-all>
      <el-table-column prop="name" label="菜单名称" />
      <el-table-column prop="path" label="路由路径" />
      <el-table-column prop="icon" label="图标" width="120">
        <template #default="{ row }">
          <el-icon v-if="row.icon" style="margin-right: 4px"><component :is="row.icon" /></el-icon>
          <span>{{ row.icon || '-' }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="sort" label="排序" width="80" />
      <el-table-column label="可见" width="80">
        <template #default="{ row }">{{ row.isVisible !== false ? '是' : '否' }}</template>
      </el-table-column>
      <el-table-column label="操作" width="200" fixed="right">
        <template #default="{ row }">
          <el-button type="primary" size="small" @click="openDialog(row)">编辑</el-button>
          <el-button type="danger" size="small" @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 新增/编辑弹窗 -->
    <el-dialog :title="dialogTitle" v-model="dialogVisible" width="500px" @closed="resetForm">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入菜单名称" />
        </el-form-item>
        <el-form-item label="路由路径" prop="path">
          <el-input v-model="form.path" placeholder="如 /users（一级菜单可为空）" />
        </el-form-item>
        <el-form-item label="图标" prop="icon">
          <el-input v-model="form.icon" placeholder="Element Plus 图标名" />
        </el-form-item>
        <el-form-item label="父菜单" prop="parentId">
          <el-select v-model="form.parentId" placeholder="不选则为一级菜单" clearable style="width: 100%">
            <el-option v-for="m in topMenus" :key="m.id" :label="m.name" :value="m.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="排序" prop="sort">
          <el-input-number v-model="form.sort" :min="0" />
        </el-form-item>
        <el-form-item label="是否可见" prop="isVisible">
          <el-switch v-model="form.isVisible" />
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
import { ref, reactive, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { getMenusAll, createMenu, updateMenu, deleteMenu, type Menu } from '@/api/menus'

defineOptions({ name: 'Menus' })

const tableData = ref<Menu[]>([])
const loading = ref(false)

async function fetchData() {
  loading.value = true
  try {
    tableData.value = await getMenusAll()
  } finally {
    loading.value = false
  }
}

const topMenus = computed(() => tableData.value.filter(m => !m.parentId))

// 新增/编辑
const dialogVisible = ref(false)
const isEdit = ref(false)
const editId = ref(0)
const saving = ref(false)
const formRef = ref<FormInstance>()
const form = reactive<Partial<Menu>>({ name: '', path: '', icon: '', parentId: undefined, sort: 0, isVisible: true })
const rules: FormRules = {
  name: [{ required: true, message: '请输入菜单名称', trigger: 'blur' }],
}

const dialogTitle = computed(() => isEdit.value ? '编辑菜单' : '新增菜单')

function openDialog(row?: Menu) {
  if (row) {
    isEdit.value = true
    editId.value = row.id
    Object.assign(form, { name: row.name, path: row.path || '', icon: row.icon || '', parentId: row.parentId, sort: row.sort, isVisible: row.isVisible !== false })
  } else {
    isEdit.value = false
    editId.value = 0
    Object.assign(form, { name: '', path: '', icon: '', parentId: undefined, sort: 0, isVisible: true })
  }
  dialogVisible.value = true
}

function resetForm() {
  formRef.value?.resetFields()
}

async function handleSave() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  saving.value = true
  try {
    // 父菜单选择空字符串'' -> null
    const payload = { ...form, parentId: form.parentId || null, isVisible: !!form.isVisible }
    if (isEdit.value) {
      await updateMenu(editId.value, payload)
      ElMessage.success('更新成功')
    } else {
      await createMenu(payload)
      ElMessage.success('新增成功')
    }
    dialogVisible.value = false
    fetchData()
  } finally {
    saving.value = false
  }
}

async function handleDelete(row: Menu) {
  await ElMessageBox.confirm(`确定删除菜单"${row.name}"？删除后不影响已分配的角色。`, '提示', { type: 'warning' })
  await deleteMenu(row.id)
  ElMessage.success('删除成功')
  fetchData()
}

onMounted(fetchData)
</script>

<style scoped>
.menus-page {
  padding: 16px;
}
</style>
