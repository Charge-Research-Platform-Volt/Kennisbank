import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import tsconfigPaths from "vite-tsconfig-paths";
import path from "path";
export default defineConfig({
  plugins: [tsconfigPaths(), react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./vitest.setup.ts", "./src/__tests__/custom-matchers"],
    deps: {
      moduleDirectories: ["node-modules", path.resolve("./package.json")],
    },
    coverage:{
      provider: 'v8'
    },
  },
});
