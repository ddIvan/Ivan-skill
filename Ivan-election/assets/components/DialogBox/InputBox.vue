<template>
  <transition name="dialog-fade">
    <div v-if="visible" class="dialog-overlay" @click.self="handleOverlayClick">
      <div class="dialog-container" :style="containerStyle">
        <div class="dialog-header">
          <span class="dialog-title">{{ title }}</span>
          <i class="el-icon-close dialog-close" @click="handleCancel"></i>
        </div>
        <div class="dialog-body">
          <div class="dialog-message" v-if="message">{{ message }}</div>
          <el-input
            v-model="inputValue"
            :placeholder="placeholder"
            :type="inputType"
            :maxlength="maxlength"
            @keyup.enter.native="handleConfirm"
            ref="inputRef"
          ></el-input>
        </div>
        <div class="dialog-footer">
          <el-button @click="handleCancel" size="small">{{ cancelText }}</el-button>
          <el-button type="primary" @click="handleConfirm" size="small">{{ confirmText }}</el-button>
        </div>
      </div>
    </div>
  </transition>
</template>

<script>
export default {
  name: 'InputBox',
  data() {
    return {
      visible: false,
      title: '请输入',
      message: '',
      placeholder: '请输入内容',
      inputType: 'text',
      inputValue: '',
      maxlength: null,
      confirmText: '确定',
      cancelText: '取消',
      width: '420px',
      closeOnOverlay: false,
      resolve: null
    };
  },
  computed: {
    containerStyle() {
      return { width: this.width };
    }
  },
  watch: {
    visible(val) {
      if (val) {
        this.$nextTick(() => {
          if (this.$refs.inputRef) {
            this.$refs.inputRef.focus();
          }
        });
      }
    }
  },
  methods: {
    open(options) {
      this.title = options.title || '请输入';
      this.message = options.message || '';
      this.placeholder = options.placeholder || '请输入内容';
      this.inputType = options.inputType || 'text';
      this.inputValue = options.defaultValue || '';
      this.maxlength = options.maxlength || null;
      this.confirmText = options.confirmText || '确定';
      this.cancelText = options.cancelText || '取消';
      this.width = options.width || '420px';
      this.closeOnOverlay = options.closeOnOverlay || false;
      this.visible = true;
      return new Promise(resolve => {
        this.resolve = resolve;
      });
    },
    handleConfirm() {
      this.visible = false;
      if (this.resolve) this.resolve(this.inputValue);
    },
    handleCancel() {
      this.visible = false;
      if (this.resolve) this.resolve(null);
    },
    handleOverlayClick() {
      if (this.closeOnOverlay) {
        this.handleCancel();
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
}
.dialog-message {
  font-size: 14px;
  color: #606266;
  margin-bottom: 12px;
}
.dialog-footer {
  padding: 12px 20px;
  display: flex;
  justify-content: flex-end;
  gap: 8px;
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
