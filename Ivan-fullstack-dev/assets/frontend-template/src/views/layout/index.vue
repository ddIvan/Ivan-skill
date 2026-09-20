<template>
  <el-container class="layout">
    <!-- 侧边栏 -->
    <el-aside :width="isCollapsed ? '64px' : '220px'" class="layout-aside" :class="{ collapsed: isCollapsed }">
      <div class="logo">
        <span v-if="!isCollapsed">Ivan Project</span>
        <span v-else class="logo-mini">IP</span>
      </div>

      <el-menu
        :default-active="route.path"
        :collapse="isCollapsed"
        :collapse-transition="true"
        router
        background-color="#1f2d3d"
        text-color="#bfcbd9"
        active-text-color="#409eff"
      >
        <menu-item-render
          v-for="menu in menuTree"
          :key="menu.id"
          :menu="menu"
          :collapsed="isCollapsed"
        />
      </el-menu>
    </el-aside>

    <!-- 主体 -->
    <el-container>
      <el-header class="layout-header">
        <div class="header-left">
          <el-icon class="collapse-btn" @click="isCollapsed = !isCollapsed" :size="20">
            <Fold v-if="!isCollapsed" />
            <Expand v-else />
          </el-icon>
          <span>{{ route.meta.title || '' }}</span>
        </div>
        <el-dropdown @command="handleCommand">
          <span class="user-info">
            <el-icon><UserFilled /></el-icon>
            {{ userStore.displayName || userStore.userName }}
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="logout">
                <el-icon><SwitchButton /></el-icon>
                退出登录
              </el-dropdown-item>
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
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { getFrontendTree, type MenuTreeNode } from '@/api/menus'
import MenuItemRender from './MenuItemRender.vue'

defineOptions({ name: 'Layout' })

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

const isCollapsed = ref(false)
const menuTree = ref<MenuTreeNode[]>([])

// 加载前端菜单树（仅返回启用+可见节点，由后端根据用户权限过滤）
async function loadMenus() {
  try {
    menuTree.value = await getFrontendTree()
  } catch {
    // 降级：使用路由表中的菜单
    menuTree.value = []
  }
}

function handleCommand(command: string) {
  if (command === 'logout') {
    userStore.clear()
    router.push('/login')
  }
}

onMounted(loadMenus)
</script>

<style scoped>
.layout {
  height: 100vh;
}

.layout-aside {
  background: #1f2d3d;
  transition: width 0.3s ease;
  overflow: hidden;
}

.layout-aside .el-menu {
  border-right: none;
  user-select: none;
}

.layout-aside.collapsed .el-menu {
  width: 64px;
}

.logo {
  height: 60px;
  line-height: 60px;
  text-align: center;
  color: #fff;
  font-weight: bold;
  font-size: 18px;
  white-space: nowrap;
  overflow: hidden;
}

.logo-mini {
  font-size: 16px;
  letter-spacing: 2px;
}

.layout-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: 1px solid #e6e6e6;
  background: #fff;
  padding: 0 20px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 16px;
  font-weight: 500;
}

.collapse-btn {
  cursor: pointer;
  color: #666;
  transition: color 0.2s;
}

.collapse-btn:hover {
  color: #409eff;
}

.user-info {
  cursor: pointer;
  color: #333;
  display: flex;
  align-items: center;
  gap: 4px;
}

.el-main {
  background: #f5f7fa;
  padding: 0;
  overflow-y: auto;
}
</style>
