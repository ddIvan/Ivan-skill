// Vue 渲染进程入口
import Vue from 'vue';
import App from './App.vue';
import router from './router';
import store from './store';
import ElementUI from 'element-ui';
import 'element-ui/lib/theme-chalk/index.css';
import DialogService from '@/components/DialogBox/DialogService';
import '@/styles/global.css';

Vue.use(ElementUI);

// 挂载自定义对话框服务（替代 alert/confirm/prompt）
Vue.prototype.$dialog = DialogService;

Vue.config.productionTip = false;

new Vue({
  router,
  store,
  render: h => h(App)
}).$mount('#app');
