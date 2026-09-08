---
name: ivan-election
description: Ivan 的 Electron + Vue + Element UI 项目开发 Skill。提供项目初始化、组件封装、页面模板生成、路由配置、状态管理等开发辅助能力。强制使用自定义对话框替代原生 alert/confirm/prompt，确保 Electron 窗口焦点稳定。触发词：electron项目/electron开发/electron+vue/创建electron应用/ivan-election等。
---

# Ivan Election — Electron + Vue + Element UI 开发 Skill

## Overview

基于 Electron + Vue 2.x + Element UI 的桌面应用开发 Skill，提供从项目初始化到完整功能模块的全流程开发辅助。

**核心约束**：禁止使用 `alert()`、`confirm()`、`prompt()` 等浏览器原生对话框，必须使用自定义对话框组件，避免 Electron 窗口焦点丢失。

## 技术栈

| 层级 | 技术 |
|------|------|
| 桌面框架 | Electron |
| 前端框架 | Vue 2.x |
| UI 组件库 | Element UI |
| 状态管理 | Vuex |
| 路由 | Vue Router |
| 构建工具 | Vue CLI / electron-builder |
| HTTP 客户端 | Axios |

## 工作流决策树

```
用户提出 Electron 项目开发需求
        │
        ▼
┌─ 需求分析 ───────────────────────────────────┐
│ 1. 确认项目类型：新建项目 / 已有项目添加功能  │
│ 2. 确认功能模块：页面、组件、路由、状态等     │
│ 3. 确认是否需要 IPC 通信（主进程↔渲染进程）   │
└──────────────────────────────────────────────┘
        │
        ▼
┌─ 项目初始化（如需要） ───────────────────────┐
│ 使用模板创建 Electron + Vue + Element UI 项目 │
│ 配置 electron-builder 打包                    │
│ 安装自定义对话框组件                          │
└──────────────────────────────────────────────┘
        │
        ▼
┌─ 功能开发 ───────────────────────────────────┐
│ 1. 页面/组件生成（使用模板）                  │
│ 2. 路由配置                                  │
│ 3. Vuex Store 模块                           │
│ 4. IPC 通信（如需要）                        │
│ 5. 所有用户交互使用自定义对话框               │
└──────────────────────────────────────────────┘
        │
        ▼
┌─ 代码审查 ───────────────────────────────────┐
│ 检查是否使用了 alert/confirm/prompt           │
│ 检查对话框组件是否正确引入                    │
│ 检查路由和状态管理配置                        │
└──────────────────────────────────────────────┘
```

## 第一步：需求分析

收到开发需求后，确认以下信息：

1. **项目类型**：
   - 新建项目 → 使用项目初始化模板
   - 已有项目 → 直接进入功能开发
2. **功能需求**：需要哪些页面、组件、功能模块
3. **IPC 需求**：是否需要主进程与渲染进程通信（如文件操作、系统通知等）
4. **打包需求**：目标平台（Windows / macOS / Linux）

## 第二步：项目初始化

### 新建项目

使用 `assets/templates/project-init/` 中的模板创建项目结构：

```
project/
├── public/
│   └── index.html
├── src/
│   ├── main/                    # Electron 主进程
│   │   ├── index.js             # 主进程入口
│   │   └── preload.js           # 预加载脚本
│   ├── renderer/                # Vue 渲染进程
│   │   ├── App.vue
│   │   ├── main.js
│   │   ├── router/
│   │   │   └── index.js
│   │   ├── store/
│   │   │   └── index.js
│   │   ├── views/               # 页面
│   │   ├── components/          # 公共组件
│   │   │   ├── DialogBox/       # 自定义对话框组件（必须）
│   │   │   └── ...
│   │   ├── api/                 # API 接口
│   │   ├── utils/               # 工具函数
│   │   └── styles/              # 全局样式
│   └── ...
├── package.json
├── vue.config.js
└── electron-builder.yml
```

### 关键依赖

```json
{
  "dependencies": {
    "vue": "^2.6.14",
    "vue-router": "^3.5.0",
    "vuex": "^3.6.0",
    "element-ui": "^2.15.0",
    "axios": "^0.27.0"
  },
  "devDependencies": {
    "electron": "^28.0.0",
    "electron-builder": "^24.0.0",
    "@vue/cli-service": "^5.0.0",
    "vue-cli-plugin-electron-builder": "^3.0.0"
  }
}
```

## 第三步：自定义对话框组件（必须）

### 核心原则

**禁止使用原生对话框**：`alert()`、`confirm()`、`prompt()` 会导致 Electron 窗口焦点丢失，严重影响用户体验。

**必须使用自定义对话框组件**，提供以下功能：

- **消息提示**（替代 alert）
- **确认对话框**（替代 confirm）
- **输入对话框**（替代 prompt）
- **通知提示**（替代 Notification）

### 对话框组件模板

模板文件：`assets/components/DialogBox/`

包含以下组件：

| 组件 | 文件 | 用途 |
|------|------|------|
| MessageBox | `MessageBox.vue` | 消息提示框 |
| ConfirmBox | `ConfirmBox.vue` | 确认对话框 |
| InputBox | `InputBox.vue` | 输入对话框 |
| NotificationBox | `NotificationBox.vue` | 通知提示 |

### 使用方式

```javascript
// 在 Vue 原型上挂载，全局可用
import DialogService from '@/components/DialogBox/DialogService';
Vue.prototype.$dialog = DialogService;

// 使用示例
this.$dialog.alert('操作成功！');
this.$dialog.confirm('确定要删除吗？').then(() => { /* 确认操作 */ });
this.$dialog.prompt('请输入名称：').then(value => { /* 处理输入 */ });
this.$dialog.notify('数据已保存', 'success');
```

## 第四步：页面模板生成

### 标准 CRUD 页面模板

模板文件：`assets/templates/page-crud/`

包含：
- `ListPage.vue` — 列表页（含搜索、分页、批量操作）
- `FormPage.vue` — 表单页（新增/编辑）
- `DetailPage.vue` — 详情页

### 页面生成规则

1. 所有页面必须引入 DialogBox 组件
2. 删除操作必须使用 `$dialog.confirm` 确认
3. 操作结果使用 `$dialog.alert` 或 `$dialog.notify` 提示
4. 表单验证失败使用 `$dialog.alert` 提示

## 第五步：路由配置

### 路由模板

模板文件：`assets/templates/router-template.js`

路由配置规则：
- 使用懒加载 `() => import()`
- 需要权限的路由添加 `meta.requiresAuth`
- 页面标题设置 `meta.title`

## 第六步：状态管理

### Vuex Store 模块模板

模板文件：`assets/templates/store-module.js`

每个业务模块一个 Store Module，使用 `namespaced: true`。

## 第七步：IPC 通信（可选）

当需要主进程能力时（文件读写、系统对话框、托盘等），使用 Electron IPC：

```javascript
// 渲染进程 → 主进程
const { ipcRenderer } = window.require('electron');
ipcRenderer.invoke('channel-name', ...args);

// 主进程处理
const { ipcMain } = require('electron');
ipcMain.handle('channel-name', async (event, ...args) => {
  // 处理逻辑
});
```

## 代码审查清单

每次生成代码后，必须检查：

- [ ] 没有使用 `alert()`、`confirm()`、`prompt()`
- [ ] 所有用户交互使用 `$dialog` 自定义对话框
- [ ] DialogBox 组件已正确引入和注册
- [ ] 路由配置正确，使用懒加载
- [ ] Vuex Store 模块使用 namespaced
- [ ] 组件命名遵循 PascalCase
- [ ] 样式使用 scoped 避免污染

## 注意事项

1. **对话框焦点**：自定义对话框基于 Element UI 的 `el-dialog`，不会导致 Electron 窗口焦点丢失。
2. **IPC 安全**：使用 `contextBridge` + `preload.js` 暴露安全 API，不直接暴露 `ipcRenderer`。
3. **打包配置**：electron-builder 配置需根据目标平台调整。
4. **性能优化**：大列表使用虚拟滚动，路由懒加载，组件按需引入。

## Resources

### assets/templates/
- `project-init/` — 项目初始化模板
- `page-crud/` — CRUD 页面模板
- `router-template.js` — 路由配置模板
- `store-module.js` — Vuex Store 模块模板

### assets/components/
- `DialogBox/` — 自定义对话框组件（MessageBox、ConfirmBox、InputBox、NotificationBox、DialogService）
