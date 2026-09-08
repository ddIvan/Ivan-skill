# 前端模板使用说明（Vue 3 + TypeScript + Element Plus）

## 使用方式

将 `frontend-template/` 下的所有文件复制到项目 `frontend/` 目录，然后：

```bash
cd frontend
npm install
npm run dev      # 开发，默认 http://localhost:5173
```

## 目录结构

```
frontend/
├── src/
│   ├── api/          # Axios 实例(request.ts) + 按模块拆分接口文件
│   ├── router/       # 路由 + 登录守卫
│   ├── stores/       # Pinia（user.ts 已含登录/登出）
│   ├── views/        # 页面：login / layout / home
│   ├── App.vue
│   └── main.ts
├── index.html
├── package.json
├── tsconfig.json
└── vite.config.ts    # 已配置 @ 别名与 /api 代理
```

## 新增业务模块的标准步骤

1. **API 层**：在 `src/api/` 新建 `xxx.ts`，从 `request` 导入实例并导出接口函数。
2. **路由**：在 `src/router/index.ts` 的 `children` 中追加路由（懒加载）。
3. **菜单**：在 `src/views/layout/index.vue` 的 `menus` 数组追加菜单项（图标用 Element Plus 图标组件名）。
4. **页面**：在 `src/views/xxx/` 创建页面组件，表格 + 分页 + 搜索为标准模式。

## 后端接口约定

- 基础路径：`/api`（开发环境由 Vite proxy 转发到 `https://localhost:53988`，协议/端口必须与后端 launchSettings.json 一致）
- 响应格式：`{ "code": 0, "message": "ok", "data": ... }`（见 `src/api/request.ts`）
- 登录接口：`POST /api/auth/login`，body `{ "userName": "", "password": "" }`

## 构建发布

```bash
npm run build   # 输出 dist/，复制到 IIS 站点即可
```
