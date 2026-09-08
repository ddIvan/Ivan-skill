<template>
  <transition name="notification-slide" v-for="(item, index) in notifications" :key="item.id">
    <div
      v-if="item.visible"
      class="notification-item"
      :class="[`notification-${item.type}`]"
      :style="{ top: `${20 + index * 70}px` }"
    >
      <div class="notification-icon">
        <i :class="iconClass(item.type)"></i>
      </div>
      <div class="notification-content">
        <div class="notification-title" v-if="item.title">{{ item.title }}</div>
        <div class="notification-message">{{ item.message }}</div>
      </div>
      <i class="el-icon-close notification-close" @click="remove(item.id)"></i>
    </div>
  </transition>
</template>

<script>
let idCounter = 0;

export default {
  name: 'NotificationBox',
  data() {
    return {
      notifications: []
    };
  },
  methods: {
    iconClass(type) {
      const map = {
        success: 'el-icon-success',
        warning: 'el-icon-warning',
        error: 'el-icon-error',
        info: 'el-icon-info'
      };
      return map[type] || 'el-icon-info';
    },
    show(options) {
      const id = ++idCounter;
      const item = {
        id,
        title: options.title || '',
        message: options.message || '',
        type: options.type || 'info',
        duration: options.duration || 3000,
        visible: true
      };
      this.notifications.unshift(item);

      if (item.duration > 0) {
        setTimeout(() => {
          this.remove(id);
        }, item.duration);
      }
    },
    remove(id) {
      const index = this.notifications.findIndex(n => n.id === id);
      if (index !== -1) {
        this.notifications[index].visible = false;
        setTimeout(() => {
          this.notifications.splice(index, 1);
        }, 300);
      }
    }
  }
};
</script>

<style scoped>
.notification-item {
  position: fixed;
  right: 20px;
  z-index: 10000;
  min-width: 320px;
  max-width: 420px;
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
  padding: 14px 16px;
  display: flex;
  align-items: flex-start;
  gap: 10px;
  border-left: 4px solid #909399;
}
.notification-success {
  border-left-color: #67c23a;
}
.notification-warning {
  border-left-color: #e6a23c;
}
.notification-error {
  border-left-color: #f56c6c;
}
.notification-info {
  border-left-color: #909399;
}
.notification-icon {
  font-size: 20px;
  flex-shrink: 0;
  margin-top: 2px;
}
.notification-success .notification-icon {
  color: #67c23a;
}
.notification-warning .notification-icon {
  color: #e6a23c;
}
.notification-error .notification-icon {
  color: #f56c6c;
}
.notification-info .notification-icon {
  color: #909399;
}
.notification-content {
  flex: 1;
  min-width: 0;
}
.notification-title {
  font-size: 14px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 4px;
}
.notification-message {
  font-size: 13px;
  color: #606266;
  line-height: 1.5;
  word-break: break-all;
}
.notification-close {
  font-size: 14px;
  color: #c0c4cc;
  cursor: pointer;
  flex-shrink: 0;
  margin-top: 2px;
}
.notification-close:hover {
  color: #909399;
}
.notification-slide-enter-active {
  transition: all 0.3s ease;
}
.notification-slide-leave-active {
  transition: all 0.3s ease;
}
.notification-slide-enter {
  transform: translateX(100%);
  opacity: 0;
}
.notification-slide-leave-to {
  transform: translateX(100%);
  opacity: 0;
}
</style>
