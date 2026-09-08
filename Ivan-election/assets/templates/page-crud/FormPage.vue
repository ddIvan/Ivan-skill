<template>
  <div class="form-page">
    <div class="page-header">
      <el-button @click="handleBack" icon="el-icon-arrow-left" size="small">返回</el-button>
      <span class="page-title">{{ isEdit ? '编辑' : '新增' }}</span>
    </div>

    <div class="form-container">
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
        size="small"
      >
        <el-form-item label="名称" prop="name">
          <el-input v-model="formData.name" placeholder="请输入名称" maxlength="50"></el-input>
        </el-form-item>

        <el-form-item label="描述" prop="description">
          <el-input
            v-model="formData.description"
            type="textarea"
            :rows="4"
            placeholder="请输入描述"
            maxlength="200"
          ></el-input>
        </el-form-item>

        <el-form-item>
          <el-button type="primary" @click="handleSubmit" :loading="submitting">保存</el-button>
          <el-button @click="handleBack">取消</el-button>
        </el-form-item>
      </el-form>
    </div>
  </div>
</template>

<script>
export default {
  name: 'FormPage',
  data() {
    return {
      isEdit: false,
      editId: null,
      submitting: false,
      formData: {
        name: '',
        description: ''
      },
      formRules: {
        name: [
          { required: true, message: '请输入名称', trigger: 'blur' },
          { max: 50, message: '名称不能超过50个字符', trigger: 'blur' }
        ]
      }
    };
  },
  created() {
    const { mode, id } = this.$route.query;
    this.isEdit = mode === 'edit';
    this.editId = id;
    if (this.isEdit && id) {
      this.loadDetail(id);
    }
  },
  methods: {
    async loadDetail(id) {
      try {
        // const res = await api.getDetail(id);
        // this.formData = res.data;
      } catch (error) {
        this.$dialog.alert('数据加载失败', '错误', 'error');
      }
    },
    handleBack() {
      this.$router.back();
    },
    handleSubmit() {
      this.$refs.formRef.validate(async valid => {
        if (!valid) {
          this.$dialog.alert('请完善表单信息', '提示', 'warning');
          return;
        }

        this.submitting = true;
        try {
          if (this.isEdit) {
            // await api.update(this.editId, this.formData);
            this.$dialog.notify('更新成功', 'success');
          } else {
            // await api.create(this.formData);
            this.$dialog.notify('创建成功', 'success');
          }
          this.$router.back();
        } catch (error) {
          this.$dialog.alert('保存失败', '错误', 'error');
        } finally {
          this.submitting = false;
        }
      });
    }
  }
};
</script>

<style scoped>
.form-page {
  padding: 20px;
}
.page-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 20px;
}
.page-title {
  font-size: 18px;
  font-weight: 600;
  color: #303133;
}
.form-container {
  background: #fff;
  padding: 24px;
  border-radius: 4px;
  max-width: 700px;
}
</style>
