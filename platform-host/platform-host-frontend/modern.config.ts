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
    hmr: true, // Explicitly enable HMR
  },
  output: {
    // Public path configuration for different environments
    assetPrefix: '/',
    polyfill: 'entry',
    disableTsChecker: false,
  },
  performance: {
    // Bundle analysis only when enabled
  },
  tools: {
    devServer: {
      proxy: {
        '/api': {
          target: 'http://localhost:5086',
          changeOrigin: true,
          ws: true, // Enable WebSocket proxying
          logLevel: 'debug', // Debug logging in development
        },
      },
    },
    rspack: (config: any) => {
      // Exclude test files from the build using IgnorePlugin
      const { IgnorePlugin } = require('@rspack/core');
      
      if (!config.plugins) {
        config.plugins = [];
      }
      
      // Ignore test files
      config.plugins.push(
        new IgnorePlugin({
          resourceRegExp: /\.(test|spec)\.(ts|tsx|js|jsx)$/,
        })
      );
      
      // Ignore __tests__ directories
      config.plugins.push(
        new IgnorePlugin({
          resourceRegExp: /\/__tests__\//,
        })
      );
      
      return config;
    },
  },
  plugins: [
    appTools({
      // Use default rspack bundler instead of webpack
    }),
    moduleFederationPlugin(),
  ],
});
