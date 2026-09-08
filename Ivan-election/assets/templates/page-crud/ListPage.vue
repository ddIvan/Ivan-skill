<template>
  <div class="list-page">
    <!-- 搜索区域 -->
    <div class="search-bar">
      <el-form :model="searchForm" inline size="small">
        <el-form-item label="关键词">
          <el-input v-model="searchForm.keyword" placeholder="请输入搜索关键词" clearable @keyup.enter.native="handleSearch"></el-input>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch" icon="el-icon-search">搜索</el-button>
          <el-button @click="handleReset" icon="el-icon-refresh">重置</el-button>
        </el-form-item>
      </el-form>
    </div>

    <!-- 操作栏 -->
    <div class="action-bar">
      <el-button type="primary" @click="handleAdd" icon="el-icon-plus" size="small">新增</el-button>
      <el-button type="danger" @click="handleBatchDelete" :disabled="!selectedRows.length" icon="el-icon-delete" size="small">
        批量删除 ({{ selectedRows.length }})
      </el-button>
    </div>

    <!-- 数据表格 -->
    <el-table
      :data="tableData"
      border
      stripe
      style="width: 100%"
      v-loading="loading"
      @selection-change="handleSelectionChange"
    >
      <el-table-column type="selection" width="50" align="center"></el-table-column>
      <!-- 根据实际字段配置列 -->
      <el-table-column prop="id" label="ID" width="80" align="center"></el-table-column>
      <el-table-column prop="name" label="名称" min-width="150"></el-table-column>
      <el-table-column prop="createTime" label="创建时间" width="180" align="center"></el-table-column>
      <el-table-column label="操作" width="200" align="center" fixed="right">
        <template slot-scope="scope">
          <el-button type="text" size="small" @click="handleEdit(scope.row)">编辑</el-button>
          <el-button type="text" size="small" @click="handleView(scope.row)">查看</el-button>
          <el-button type="text" size="small" style="color: #f56c6c" @click="handleDelete(scope.row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 分页 -->
    <div class="pagination-bar">
      <el-pagination
        background
        @size-change="handleSizeChange"
        @current-change="handleCurrentChange"
        :current-page="pagination.page"
        :page-sizes="[10, 20, 50, 100]"
        :page-size="pagination.limit"
        layout="total, sizes, prev, pager, next, jumper"
        :total="pagination.total"
      ></el-pagination>
    </div>
  </div>
</template>

<script>
export default {
  name: 'ListPage',
  data() {
    return {
      searchForm: {
        keyword: ''
      },
      tableData: [],
      selectedRows: [],
      loading: false,
      pagination: {
        page: 1,
        limit: 20,
        total: 0
      }
    };
  },
  created() {
    this.fetchData();
  },
  methods: {
    async fetchData() {
      this.loading = true;
      try {
        // 调用 API 获取数据
        // const res = await api.getList({ ...this.searchForm, ...this.pagination });
        // this.tableData = res.data.list;
        // this.pagination.total = res.data.total;
      } catch (error) {
        this.$dialog.alert('数据加载失败', '错误', 'error');
      } finally {
        this.loading = false;
      }
    },
    handleSearch() {
      this.pagination.page = 1;
      this.fetchData();
    },
    handleReset() {
      this.searchForm.keyword = '';
      this.pagination.page = 1;
      this.fetchData();
    },
    handleAdd() {
      this.$router.push({ name: 'FormPage', query: { mode: 'add' } });
    },
    handleEdit(row) {
      this.$router.push({ name: 'FormPage', query: { mode: 'edit', id: row.id } });
    },
    handleView(row) {
      this.$router.push({ name: 'DetailPage', query: { id: row.id } });
    },
    async handleDelete(row) {
      const ok = await this.$dialog.confirm(`确定要删除 "${row.name}" 吗？此操作不可恢复。`, '删除确认', {
        confirmType: 'danger',
        confirmText: '删除'
      });
      if (!ok) return;

      try {
        // await api.delete(row.id);
        this.$dialog.notify('删除成功', 'success');
        this.fetchData();
      } catch (error) {
        this.$dialog.alert('删除失败', '错误', 'error');
      }
    },
    async handleBatchDelete() {
      if (!this.selectedRows.length) return;

      const ok = await this.$dialog.confirm(
        `确定要删除选中的 ${this.selectedRows.length} 条记录吗？此操作不可恢复。`,
        '批量删除确认',
        { confirmType: 'danger', confirmText: '删除' }
      );
      if (!ok) return;

      try {
        // const ids = this.selectedRows.map(r => r.id);
        // await api.batchDelete(ids);
        this.$dialog.notify('批量删除成功', 'success');
        this.fetchData();
      } catch (error) {
        this.$dialog.alert('批量删除失败', '错误', 'error');
      }
    },
    handleSelectionChange(rows) {
      this.selectedRows = rows;
    },
    handleSizeChange(limit) {
      this.pagination.limit = limit;
      this.pagination.page = 1;
      this.fetchData();
    },
    handleCurrentChange(page) {
      this.pagination.page = page;
      this.fetchData();
    }
  }
};
</script>

<style scoped>
.list-page {
  padding: 20px;
}
.search-bar {
  background: #fff;
  padding: 16px 20px 0;
  border-radius: 4px;
  margin-bottom: 16px;
}
.action-bar {
  margin-bottom: 16px;
}
.pagination-bar {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>
