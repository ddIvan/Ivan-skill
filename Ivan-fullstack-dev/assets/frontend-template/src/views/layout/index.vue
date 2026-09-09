<template>
  <el-container class="layout">
    <el-aside width="220px" class="layout-aside">
      <div class="logo">Ivan Project</div>
      <el-menu :default-active="route.path" router background-color="#1f2d3d" text-color="#bfcbd9" active-text-color="#409eff">
        <el-menu-item v-for="menu in menus" :key="menu.path" :index="menu.path">
          <el-icon><component :is="menu.icon" /></el-icon>
          <span>{{ menu.title }}</span>
        </el-menu-item>
      </el-menu>
    </el-aside>
    <el-container>
      <el-header class="layout-header">
        <span>{{ route.meta.title || '' }}</span>
        <el-dropdown @command="handleCommand">
          <span class="user-info">{{ userStore.displayName || userStore.userName }}</span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="logout">退出登录</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </el-header>
      <el-main>
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'

defineOptions({ name: 'Layout' })

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

// 从路由表动态生成菜单，并根据用户 menuIds 权限过滤
const menuFromRouter = router.options.routes
  .find(r => r.path === '/')?.children
  ?.filter(r => r.meta?.title && r.path !== 'home')
  ?.map(r => ({ path: `/${r.path}`, title: r.meta?.title as string, icon: r.meta?.icon as string, menuId: r.meta?.menuId as number })) || []

const menus = [
  { path: '/home', title: '首页', icon: 'HomeFilled' },
  // 仅展示用户在权限内的菜单
  ...menuFromRouter.filter(m => userStore.menuIds.includes(m.menuId)),
]

function handleCommand(command: string) {
  if (command === 'logout') {
    userStore.clear()
    router.push('/login')
  }
}
</script>

<style scoped>
.layout {
  height: 100%;
}
.layout-aside {
  background: #1f2d3d;
}
.layout-aside .el-menu {
  border-right: none;
}
.logo {
  height: 60px;
  line-height: 60px;
  text-align: center;
  color: #fff;
  font-weight: bold;
  font-size: 18px;
}
.layout-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: 1px solid #e6e6e6;
  background: #fff;
}
.user-info {
  cursor: pointer;
  color: #333;
}
</style>
