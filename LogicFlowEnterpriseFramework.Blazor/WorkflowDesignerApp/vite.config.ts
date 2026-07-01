import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  base: '/workflow-designer/',
  build: {
    outDir: '../wwwroot/workflow-designer',
    emptyOutDir: true,
    cssCodeSplit: false,
    rollupOptions: {
      output: {
        entryFileNames: 'assets/workflow-designer.js',
        chunkFileNames: 'assets/workflow-designer.js',
        assetFileNames: (assetInfo) => {
          if ((assetInfo.name ?? '').endsWith('.css')) {
            return 'assets/workflow-designer.css'
          }

          return 'assets/[name]-[hash][extname]'
        },
      },
    },
  },
  server: {
    host: '127.0.0.1',
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://127.0.0.1:5077',
        changeOrigin: true,
      },
    },
  },
})
