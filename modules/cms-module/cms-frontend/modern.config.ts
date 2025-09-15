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
    port: 3003,
  },
  dev: {
    port: 3003,
    host: 'cms.platform.local',
    hmr: true,
  },
  output: {
    assetPrefix: '/',
    polyfill: 'off',
    disableTsChecker: false,
    distPath: {
      root: 'dist',
      js: 'static/js',
      css: 'static/css',
      image: 'static/images',
      font: 'static/fonts',
      html: '',
    },
    cleanDistPath: true,
    enableAssetManifest: true,
    enableInlineScripts: false,
    enableInlineStyles: false,
  },
  performance: {
    chunkSplit: {
      strategy: 'split-by-experience',
      minSize: 20000,
      maxSize: 244000,
    },
    buildCache: true,
    removeMomentJs: true,
  },
  tools: {
    devServer: {
      https: {
        cert: fs.readFileSync(
          path.resolve(
            __dirname,
            '../certs/_wildcard.platform.local-fullchain.pem',
          ),
          'utf8',
        ),
        key: fs.readFileSync(
          path.resolve(
            __dirname,
            '../certs/_wildcard.platform.local-key-pkcs8.pem',
          ),
          'utf8',
        ),
      },
      headers: {
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, PATCH, OPTIONS',
        'Access-Control-Allow-Headers': 'X-Requested-With, content-type, Authorization',
      },
    },
    webpack: (config: any, { webpack }: any) => {
      config.output = config.output || {};
      config.output.publicPath = 'auto';
      config.output.crossOriginLoading = 'anonymous';

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
      bundler: 'webpack',
    }),
    moduleFederationPlugin(),
  ],
});