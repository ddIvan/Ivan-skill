<template>
  <div>
    <el-card>
      <h2>欢迎使用 Ivan Project</h2>
      <p>当前登录用户：{{ userStore.displayName || userStore.userName }}</p>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { useUserStore } from '@/stores/user'

defineOptions({ name: 'HomePage' })

const userStore = useUserStore()
</script>
