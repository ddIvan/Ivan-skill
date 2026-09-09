<template>
  <div class="users-page">
    <el-form :inline="true" :model="searchForm" class="search-bar">
      <el-form-item>
        <el-input v-model="searchForm.keyword" placeholder="搜索用户名/显示名" clearable @clear="fetchData" @keyup.enter="fetchData" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="fetchData">查询</el-button>
      </el-form-item>
    </el-form>

    <el-table :data="tableData" border stripe v-loading="loading" style="width: 100%">
      <el-table-column prop="id" label="ID" width="80" />
      <el-table-column prop="userName" label="用户名" />
      <el-table-column prop="displayName" label="显示名" />
      <el-table-column prop="role" label="角色" />
      <el-table-column label="状态" width="80">
        <template #default="{ row }">
          <el-tag :type="row.isEnabled ? 'success' : 'danger'">{{ row.isEnabled ? '启用' : '禁用' }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="createTime" label="创建时间" width="120" />
      <el-table-column label="操作" width="160" fixed="right">
        <template #default="{ row }">
          <el-button type="primary" size="small" @click="openDialog(row)">编辑</el-button>
          <el-button type="danger" size="small" @click="handleDelete(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 占位 - 用户管理页面的完整实现参照现有 UserController -->
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import request from '@/api/request'
import type { PageResult } from '@/api/roles'

defineOptions({ name: 'Users' })

interface User {
  id: number
  userName: string
  displayName?: string
  role: string
  isEnabled?: boolean
  createTime?: string
}

const searchForm = reactive({ keyword: '' })
const tableData = ref<User[]>([])
const loading = ref(false)

async function fetchData() {
  loading.value = true
  try {
    const res = await request.get<unknown, PageResult<User>>('/users', { params: searchForm })
    tableData.value = res.items
  } finally {
    loading.value = false
  }
}

onMounted(fetchData)
</script>

<style scoped>
.users-page { padding: 16px; }
.search-bar { margin-bottom: 16px; }
</style>
