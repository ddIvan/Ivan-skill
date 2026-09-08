<template>
  <transition name="dialog-fade">
    <div v-if="visible" class="dialog-overlay" @click.self="handleOverlayClick">
      <div class="dialog-container" :style="containerStyle">
        <div class="dialog-header">
          <span class="dialog-title">{{ title }}</span>
          <i class="el-icon-close dialog-close" @click="handleClose"></i>
        </div>
        <div class="dialog-body">
          <div class="dialog-icon" v-if="type">
            <i :class="iconClass" :style="{ color: iconColor }"></i>
          </div>
          <div class="dialog-message">{{ message }}</div>
        </div>
        <div class="dialog-footer">
          <el-button type="primary" @click="handleConfirm" size="small">{{ confirmText }}</el-button>
        </div>
      </div>
    </div>
  </transition>
</template>

<script>
export default {
  name: 'MessageBox',
  data() {
    return {
      visible: false,
      title: '提示',
      message: '',
      type: '', // success / warning / error / info
      confirmText: '确定',
      width: '420px',
      closeOnOverlay: false,
      resolve: null
    };
  },
  computed: {
    containerStyle() {
      return { width: this.width };
    },
    iconClass() {
      const map = {
        success: 'el-icon-success',
        warning: 'el-icon-warning',
        error: 'el-icon-error',
        info: 'el-icon-info'
      };
      return map[this.type] || 'el-icon-info';
    },
    iconColor() {
      const map = {
        success: '#67c23a',
        warning: '#e6a23c',
        error: '#f56c6c',
        info: '#909399'
      };
      return map[this.type] || '#909399';
    }
  },
  methods: {
    open(options) {
      this.title = options.title || '提示';
      this.message = options.message || '';
      this.type = options.type || '';
      this.confirmText = options.confirmText || '确定';
      this.width = options.width || '420px';
      this.closeOnOverlay = options.closeOnOverlay || false;
      this.visible = true;
      return new Promise(resolve => {
        this.resolve = resolve;
      });
    },
    handleConfirm() {
      this.visible = false;
      if (this.resolve) this.resolve('confirm');
    },
    handleClose() {
      this.visible = false;
      if (this.resolve) this.resolve('close');
    },
    handleOverlayClick() {
      if (this.closeOnOverlay) {
        this.handleClose();
      }
    }
  }
};
</script>

<style scoped>
.dialog-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
}
.dialog-container {
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
  overflow: hidden;
}
.dialog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid #ebeef5;
}
.dialog-title {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}
.dialog-close {
  font-size: 18px;
  color: #909399;
  cursor: pointer;
}
.dialog-close:hover {
  color: #303133;
}
.dialog-body {
  padding: 24px 20px;
  display: flex;
  align-items: flex-start;
  gap: 12px;
}
.dialog-icon {
  font-size: 24px;
  flex-shrink: 0;
}
.dialog-message {
  font-size: 14px;
  color: #606266;
  line-height: 1.6;
  word-break: break-all;
}
.dialog-footer {
  padding: 12px 20px;
  display: flex;
  justify-content: flex-end;
  border-top: 1px solid #ebeef5;
}
.dialog-fade-enter-active,
.dialog-fade-leave-active {
  transition: opacity 0.3s;
}
.dialog-fade-enter,
.dialog-fade-leave-to {
  opacity: 0;
}
</style>
