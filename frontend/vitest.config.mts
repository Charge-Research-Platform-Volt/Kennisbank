import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import tsconfigPaths from 'vite-tsconfig-paths'
import path from 'path'
export default defineConfig({
  plugins: [tsconfigPaths(), react()],
  test: {
    environment: 'jsdom',
    globals : true,
    setupFiles: ['./vitest.setup.ts'],
    deps:{
      moduleDirectories: ['node-modules', path.resolve('./package.json')]
    }
    },
})