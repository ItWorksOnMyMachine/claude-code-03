import { appTools, defineConfig } from '@modern-js/app-tools';
import { moduleFederationPlugin } from '@module-federation/modern-js';

// https://modernjs.dev/en/configure/app/usage
export default defineConfig({
  runtime: {
    router: true,
  },
  server: {
    port: 3002,
  },
  dev: {
    port: 3002,
  },
  tools: {
    devServer: {
      proxy: {
        '/api': {
          target: 'http://localhost:5000',
          changeOrigin: true,
        },
      },
    },
    // Switch to webpack for Module Federation support
    bundler: 'webpack',
  },
  plugins: [
    appTools({
      bundler: 'webpack', // Module Federation requires webpack
    }),
    moduleFederationPlugin(),
  ],
});
