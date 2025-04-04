import { reactRouter } from '@react-router/dev/vite';
import tailwindcss from '@tailwindcss/vite';
import { reactRouterDevTools } from 'react-router-devtools';
import { defineConfig } from 'vite';
import tsconfigPaths from 'vite-tsconfig-paths';
import { configDefaults } from 'vitest/config';

export default defineConfig(({ isSsrBuild }) => ({
  build: {
    rollupOptions: isSsrBuild
      ? {
          input: './server/app.ts',
        }
      : undefined,
  },
  plugins: [
    tailwindcss(),
    process.env.NODE_ENV === 'development' ? reactRouterDevTools() : null,
    // See: https://github.com/vitest-dev/vitest/issues/7794#issuecomment-2777307476
    process.env.VITEST ? null : reactRouter(),
    tsconfigPaths(),
  ],
  optimizeDeps: {
    exclude: ['@node-rs/argon2'],
  },
  server: {
    port: 3000,
  },
  test: {
    setupFiles: ['./test/setup.ts'],
    exclude: [...configDefaults.exclude, 'e2e/*'],
  },
}));
