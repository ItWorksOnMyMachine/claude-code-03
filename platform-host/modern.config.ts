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
    path: './dist',
    // Public path configuration for different environments
    assetPrefix: process.env.ASSET_PREFIX || '/',
    polyfill: 'entry',
    disableTsChecker: false,
  },
  performance: {
    bundleAnalyze: process.env.BUNDLE_ANALYZE === 'true' ? {} : false,
  },
  tools: {
    devServer: {
      proxy: {
        '/api': {
          target: 'http://localhost:5000',
          changeOrigin: true,
          ws: true, // Enable WebSocket proxying
          logLevel: 'debug', // Debug logging in development
        },
      },
    },
    // Switch to webpack for Module Federation support
    bundler: 'webpack',
    webpack: (config, { env }) => {
      if (env === 'production') {
        // Production optimizations
        config.optimization = {
          ...config.optimization,
          splitChunks: {
            chunks: 'all',
            cacheGroups: {
              vendor: {
                test: /[\\/]node_modules[\\/]/,
                name: 'vendors',
                priority: 10,
              },
              mui: {
                test: /[\\/]node_modules[\\/]@mui[\\/]/,
                name: 'mui',
                priority: 20,
              },
              common: {
                minChunks: 2,
                priority: 5,
                reuseExistingChunk: true,
              },
            },
          },
          runtimeChunk: 'single',
          moduleIds: 'deterministic',
        };
      }
      return config;
    },
  },
  plugins: [
    appTools({
      bundler: 'webpack', // Module Federation requires webpack
    }),
    moduleFederationPlugin(),
  ],
});
