import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: () => import('@/views/login/index.vue'),
      meta: { public: true },
    },
    {
      path: '/',
      component: () => import('@/views/layout/index.vue'),
      redirect: '/home',
      children: [
        {
          path: 'home',
          name: 'Home',
          component: () => import('@/views/home/index.vue'),
          meta: { title: '首页', icon: 'HomeFilled', menuId: 1 },
        },
        {
          path: 'users',
          name: 'Users',
          component: () => import('@/views/users/index.vue'),
          meta: { title: '用户管理', icon: 'User', menuId: 3 },
        },
        {
          path: 'roles',
          name: 'Roles',
          component: () => import('@/views/roles/index.vue'),
          meta: { title: '角色管理', icon: 'UserFilled', menuId: 4 },
        },
        {
          path: 'menus',
          name: 'Menus',
          component: () => import('@/views/menus/index.vue'),
          meta: { title: '菜单管理', icon: 'Menu', menuId: 5 },
        },
      ],
    },
  ],
})

// 全局守卫：未登录跳转登录页
router.beforeEach((to) => {
  if (to.meta.public) return true
  const token = localStorage.getItem('token')
  if (!token) {
    return { path: '/login', query: { redirect: to.fullPath } }
  }
  return true
})

export default router
