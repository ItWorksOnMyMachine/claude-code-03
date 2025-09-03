import { appTools, defineConfig } from '@modern-js/app-tools';
import { moduleFederationPlugin } from '@module-federation/modern-js';
import * as fs from 'fs';
import * as path from 'path';

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
    host: 'host-fe.platform.local',
    hmr: true, // Explicitly enable HMR
    //https: true, // Let webpack devServer handle HTTPS
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
      https: {
        cert: fs.readFileSync(
          path.resolve(
            __dirname,
            '../../certs/_wildcard.platform.local-fullchain.pem',
          ),
          'utf8',
        ),
        key: fs.readFileSync(
          path.resolve(
            __dirname,
            '../../certs/_wildcard.platform.local-key-pkcs8.pem',
          ),
          'utf8',
        ),
      },
      proxy: {
        '/api': {
          target: 'https://host-bff.platform.local:5086',
          changeOrigin: true,
          secure: false,
          logLevel: 'debug',
        },
      },
    },
    webpack: (config: any, { webpack }: any) => {
      config.output = config.output || {};
      config.output.publicPath = 'auto';
      config.output.crossOriginLoading = 'anonymous';

      // Exclude test files from the build using IgnorePlugin
      if (!config.plugins) {
        config.plugins = [];
      }

      // Ignore test files
      config.plugins.push(
        new webpack.IgnorePlugin({
          resourceRegExp: /\.(test|spec)\.(ts|tsx|js|jsx)$/,
        }),
      );

      // Ignore __tests__ directories
      config.plugins.push(
        new webpack.IgnorePlugin({
          resourceRegExp: /\/__tests__\//,
        }),
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
