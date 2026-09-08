// Vue Router 路由配置模板
import Vue from 'vue';
import VueRouter from 'vue-router';

Vue.use(VueRouter);

// 懒加载页面
const ListPage = () => import('@/views/ListPage.vue');
const FormPage = () => import('@/views/FormPage.vue');
const DetailPage = () => import('@/views/DetailPage.vue');

const routes = [
  {
    path: '/',
    redirect: '/list'
  },
  {
    path: '/list',
    name: 'ListPage',
    component: ListPage,
    meta: { title: '列表页', requiresAuth: true }
  },
  {
    path: '/form',
    name: 'FormPage',
    component: FormPage,
    meta: { title: '表单页', requiresAuth: true }
  },
  {
    path: '/detail',
    name: 'DetailPage',
    component: DetailPage,
    meta: { title: '详情页', requiresAuth: true }
  }
];

const router = new VueRouter({
  mode: 'hash', // Electron 推荐使用 hash 模式
  routes
});

// 路由守卫 - 设置页面标题
router.afterEach(to => {
  if (to.meta && to.meta.title) {
    document.title = to.meta.title;
  }
});

export default router;
