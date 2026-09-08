// 自定义对话框服务 - 替代 alert/confirm/prompt
// 所有用户交互必须通过此服务，禁止使用原生对话框

import Vue from 'vue';
import MessageBox from './MessageBox.vue';
import ConfirmBox from './ConfirmBox.vue';
import InputBox from './InputBox.vue';
import NotificationBox from './NotificationBox.vue';

// 创建单例组件实例
function createInstance(Component) {
  const Constructor = Vue.extend(Component);
  const instance = new Constructor().$mount(document.createElement('div'));
  document.body.appendChild(instance.$el);
  return instance;
}

let messageBoxInstance = null;
let confirmBoxInstance = null;
let inputBoxInstance = null;
let notificationBoxInstance = null;

function getMessageBox() {
  if (!messageBoxInstance) {
    messageBoxInstance = createInstance(MessageBox);
  }
  return messageBoxInstance;
}

function getConfirmBox() {
  if (!confirmBoxInstance) {
    confirmBoxInstance = createInstance(ConfirmBox);
  }
  return confirmBoxInstance;
}

function getInputBox() {
  if (!inputBoxInstance) {
    inputBoxInstance = createInstance(InputBox);
  }
  return inputBoxInstance;
}

function getNotificationBox() {
  if (!notificationBoxInstance) {
    notificationBoxInstance = createInstance(NotificationBox);
  }
  return notificationBoxInstance;
}

const DialogService = {
  /**
   * 消息提示框（替代 alert）
   * @param {string} message - 提示消息
   * @param {string} title - 标题，默认"提示"
   * @param {string} type - 类型：success / warning / error / info
   * @returns {Promise}
   *
   * 示例：
   *   this.$dialog.alert('操作成功！', '提示', 'success');
   *   await this.$dialog.alert('数据加载失败', '错误', 'error');
   */
  alert(message, title = '提示', type = '') {
    return getMessageBox().open({ message, title, type });
  },

  /**
   * 确认对话框（替代 confirm）
   * @param {string} message - 确认消息
   * @param {string} title - 标题，默认"确认"
   * @param {object} options - 可选配置
   * @returns {Promise<boolean>} true=确认, false=取消
   *
   * 示例：
   *   const ok = await this.$dialog.confirm('确定要删除吗？');
   *   if (ok) { /* 执行删除 * / }
   */
  confirm(message, title = '确认', options = {}) {
    return getConfirmBox().open({
      message,
      title,
      type: options.type || 'warning',
      confirmText: options.confirmText || '确定',
      cancelText: options.cancelText || '取消',
      confirmType: options.confirmType || 'primary'
    });
  },

  /**
   * 输入对话框（替代 prompt）
   * @param {string} message - 提示消息
   * @param {string} title - 标题，默认"请输入"
   * @param {object} options - 可选配置
   * @returns {Promise<string|null>} 输入值或 null（取消时）
   *
   * 示例：
   *   const name = await this.$dialog.prompt('请输入名称：');
   *   if (name) { /* 处理输入 * / }
   */
  prompt(message, title = '请输入', options = {}) {
    return getInputBox().open({
      message,
      title,
      placeholder: options.placeholder || '请输入内容',
      inputType: options.inputType || 'text',
      defaultValue: options.defaultValue || '',
      maxlength: options.maxlength || null
    });
  },

  /**
   * 通知提示（替代 Notification）
   * @param {string} message - 通知消息
   * @param {string} type - 类型：success / warning / error / info
   * @param {object} options - 可选配置
   *
   * 示例：
   *   this.$dialog.notify('数据已保存', 'success');
   *   this.$dialog.notify('网络异常', 'error', { duration: 5000 });
   */
  notify(message, type = 'info', options = {}) {
    getNotificationBox().show({
      message,
      type,
      title: options.title || '',
      duration: options.duration || 3000
    });
  }
};

export default DialogService;
