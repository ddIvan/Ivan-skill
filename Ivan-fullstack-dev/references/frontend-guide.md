# 前端开发规范（Vue 3 + TypeScript + Element Plus）

## 技术栈

- Vue 3（`<script setup>` 语法）
- TypeScript（strict 模式）
- Vite 构建
- Vue Router 4
- Pinia（状态管理）
- Axios（HTTP 请求）
- Element Plus + @element-plus/icons-vue（UI，可按用户偏好更换）

## 项目初始化

按 `assets/frontend-template/` 模板复制项目，或执行：

```bash
npm create vite@latest frontend -- --template vue-ts
cd frontend
npm install element-plus vue-router pinia axios @element-plus/icons-vue
```

## 目录结构

```
frontend/
├── src/
│   ├── api/               # 接口定义，按业务模块拆分文件
│   │   └── request.ts     # Axios 实例封装（唯一入口）
│   ├── assets/
│   ├── components/        # 公共组件
│   ├── router/            # 路由配置 + 守卫
│   ├── stores/            # Pinia store
│   ├── utils/             # 通用工具
│   ├── views/             # 页面组件，按模块分目录
│   ├── App.vue
│   └── main.ts
├── index.html
├── package.json
├── tsconfig.json
└── vite.config.ts
```

## 关键约定

### Axios 封装（request.ts）

- 统一 `baseURL`（如 `/api`），实际后端地址由 `vite.config.ts` 的 proxy 配置决定，也可通过 `.env` 配置。
- 请求拦截器注入 `Authorization: Bearer <token>`（从 Pinia/localStorage 读取）。
- 响应拦截器统一解包后端 `ApiResult<T>`：`code === 0` 时返回 `data`，否则 `ElMessage.error`；`code === 401` 时清除 token 并跳转登录页。
- 推荐后端返回格式：`{ "code": 0, "message": "ok", "data": {...} }`。

### 路由与守卫

- 路由懒加载：`const X = () => import('@/views/x/index.vue')`。
- 全局前置守卫：未登录（无 token）访问需鉴权路由时，`router.push('/login')`。
- 菜单与路由结构保持一致，便于从后端返回的动态菜单扩展。

### 页面开发约定

- 页面统一 `defineOptions({ name: 'XxxPage' })` + `<script setup lang="ts">`。
- 表格 + 分页 + 搜索条件为标准管理页面模式：搜索区 → 操作按钮 → `el-table` → `el-pagination`。
- 弹窗表单使用 `el-dialog` + `el-form`，表单校验用 `rules`。
- 所有接口调用必须经过 `src/api/` 下的函数，禁止在页面里直接写 `axios.get(...)`。

## 与后端联调

- 开发期建议 `vite.config.ts` 配置 proxy，避免跨域：

```ts
export default defineConfig({
  server: {
    port: 5173,
    proxy: {
      '/api': {
        // 必须与后端 launchSettings.json 的 applicationUrl 协议、端口完全一致
        // 后端模板默认监听 https://localhost:53988（自签名证书需 secure: false）
        target: 'https://localhost:53988',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
```

**易错点（必须检查）**：

- proxy `target` 的协议必须与后端监听协议一致：后端 launchSettings.json 若为 `https://localhost:53988`，target 不能写成 `http://localhost:53988`（协议不匹配会导致代理转发失败，前端表现为网络异常/请求打到自身 5173）。
- 后端使用自签名开发证书时，proxy 必须加 `secure: false`，否则 Vite 代理因证书校验失败而 502。
- 修改后端监听端口时，必须同步修改 `vite.config.ts` 的 proxy target。

- 生产环境（IIS）：前端请求同源路径（如 `/api`），由 IIS URL Rewrite 转发到后端，见 `iis-deploy-guide.md`。

## 构建

```bash
npm run build   # 输出到 dist/
```
