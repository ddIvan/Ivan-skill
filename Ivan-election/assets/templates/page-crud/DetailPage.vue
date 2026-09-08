<template>
  <div class="detail-page">
    <div class="page-header">
      <el-button @click="handleBack" icon="el-icon-arrow-left" size="small">返回</el-button>
      <span class="page-title">详情</span>
    </div>

    <div class="detail-container" v-loading="loading">
      <el-descriptions :column="2" border size="small">
        <el-descriptions-item label="ID">{{ detail.id }}</el-descriptions-item>
        <el-descriptions-item label="名称">{{ detail.name }}</el-descriptions-item>
        <el-descriptions-item label="描述">{{ detail.description }}</el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ detail.createTime }}</el-descriptions-item>
        <el-descriptions-item label="更新时间">{{ detail.updateTime }}</el-descriptions-item>
      </el-descriptions>
    </div>
  </div>
</template>

<script>
export default {
  name: 'DetailPage',
  data() {
    return {
      loading: false,
      detail: {}
    };
  },
  created() {
    const { id } = this.$route.query;
    if (id) {
      this.loadDetail(id);
    }
  },
  methods: {
    async loadDetail(id) {
      this.loading = true;
      try {
        // const res = await api.getDetail(id);
        // this.detail = res.data;
      } catch (error) {
        this.$dialog.alert('数据加载失败', '错误', 'error');
      } finally {
        this.loading = false;
      }
    },
    handleBack() {
      this.$router.back();
    }
  }
};
</script>

<style scoped>
.detail-page {
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
.detail-container {
  background: #fff;
  padding: 24px;
  border-radius: 4px;
}
</style>
