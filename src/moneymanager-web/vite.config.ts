/// <reference types="vitest/config" />
import path from "path"
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

const reactRefreshPreamble = {
  name: 'react-refresh-preamble',
  apply: 'serve',
  enforce: 'post',
  transformIndexHtml(html: string) {
    if (html.includes('window.$RefreshSig$') || html.includes('__vite_plugin_react_preamble_installed__')) {
      return html
    }

    return [{
      tag: 'script',
      attrs: { type: 'module' },
      injectTo: 'head-prepend',
      children: `import RefreshRuntime from "/@react-refresh"
RefreshRuntime.injectIntoGlobalHook(window)
window.$RefreshReg$ = () => {}
window.$RefreshSig$ = () => (type) => type
window.__vite_plugin_react_preamble_installed__ = true`,
    }]
  },
}

export default defineConfig({
  plugins: [reactRefreshPreamble, react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    proxy: {
      '/api': 'http://localhost:5000',
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
})
