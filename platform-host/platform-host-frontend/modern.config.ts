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
    polyfill: 'off', // Disable polyfills to avoid core-js issues
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
          secure: false,
          logLevel: 'debug',
        },
      },
    },
    webpack: (config: any, { webpack }: any) => {
      // Exclude test files from the build using IgnorePlugin
      if (!config.plugins) {
        config.plugins = [];
      }
      
      // Ignore test files
      config.plugins.push(
        new webpack.IgnorePlugin({
          resourceRegExp: /\.(test|spec)\.(ts|tsx|js|jsx)$/,
        })
      );
      
      // Ignore __tests__ directories
      config.plugins.push(
        new webpack.IgnorePlugin({
          resourceRegExp: /\/__tests__\//,
        })
      );
      
      return config;
    },
  },
  plugins: [
    appTools({
      bundler: 'webpack', // Use webpack instead of rspack for better Module Federation compatibility
    }),
    moduleFederationPlugin(),
  ],
});
