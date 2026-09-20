<template>
  <!-- 有子节点的目录菜单 -->
  <el-sub-menu v-if="menu.hasChildren" :index="menu.fullPath || String(menu.id)">
    <template #title>
      <el-icon v-if="menu.icon"><component :is="menu.icon" /></el-icon>
      <span v-if="!collapsed">{{ menu.name }}</span>
    </template>
    <!-- 懒加载子节点：展开时从后端加载（el-menu 的展开事件通过 @expand 处理） -->
    <menu-item-render
      v-for="child in menu.children"
      :key="child.id"
      :menu="child"
      :collapsed="collapsed"
    />
  </el-sub-menu>

  <!-- 叶子节点 -->
  <el-menu-item v-else :index="menu.path || ''" :disabled="!menu.isEnabled">
    <el-icon v-if="menu.icon"><component :is="menu.icon" /></el-icon>
    <template #title>
      <span :style="{ paddingLeft: menu.level > 0 && collapsed ? '0' : menu.level * 12 + 'px' }">
        {{ menu.name }}
      </span>
    </template>
  </el-menu-item>
</template>

<script setup lang="ts">
import type { MenuTreeNode } from '@/api/menus'

defineOptions({ name: 'MenuItemRender' })

defineProps<{
  menu: MenuTreeNode
  collapsed: boolean
}>()
</script>
