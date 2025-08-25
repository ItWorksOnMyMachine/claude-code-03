import { createModuleFederationConfig } from '@module-federation/modern-js';

export default createModuleFederationConfig({
  name: 'platform_host',
  filename: 'remoteEntry.js',
  exposes: {},
  remotes: {
    // Remote modules will be dynamically added at runtime
    // Example: cms: 'cms@http://localhost:3003/remoteEntry.js',
  },
  shared: {
    // React and React-DOM as singletons to prevent multiple instances
    react: {
      singleton: true,
      requiredVersion: '^18.3.1',
    },
    'react-dom': {
      singleton: true,
      requiredVersion: '^18.3.1',
    },
    // Material-UI packages as singletons
    '@mui/material': {
      singleton: true,
      requiredVersion: '^7.3.1',
    },
    '@mui/system': {
      singleton: true,
    },
    '@mui/utils': {
      singleton: true,
    },
    // Emotion packages as singletons for consistent styling
    '@emotion/react': {
      singleton: true,
      requiredVersion: '^11.14.0',
    },
    '@emotion/styled': {
      singleton: true,
      requiredVersion: '^11.14.1',
    },
    // React Query for shared state management
    '@tanstack/react-query': {
      singleton: true,
      requiredVersion: '^5.85.5',
    },
    // ModernJS runtime
    '@modern-js/runtime': {
      singleton: true,
      requiredVersion: '2.68.10',
    },
  },
  // Enable runtime plugins for dynamic remote loading
  runtimePlugins: ['./src/runtime/module-federation-plugin.ts'],
});