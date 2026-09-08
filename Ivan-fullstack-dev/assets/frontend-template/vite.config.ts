import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    proxy: {
      // 开发环境将 /api 代理到后端，避免跨域
      // 后端 launchSettings.json 监听 https://localhost:53988（自签名证书需 secure: false）
      '/api': {
        target: 'https://localhost:53988',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
