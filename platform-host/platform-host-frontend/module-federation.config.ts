import { createModuleFederationConfig } from '@module-federation/modern-js';
import pkg from './package.json' assert { type: 'json' };

export default createModuleFederationConfig({
  name: 'platform_host',
  remotes: {},
  shared: {
    react: {
      singleton: true,
      eager: true,
      requiredVersion: pkg.dependencies.react,
    },
    'react-dom': {
      singleton: true,
      eager: true,
      requiredVersion: pkg.dependencies['react-dom'],
    },

    // Pin MUI versions; keep styled-engine and emotion unshared
    '@mui/material': {
      singleton: true,
      eager: true,
      version: pkg.dependencies['@mui/material'],
      requiredVersion: pkg.dependencies['@mui/material'],
    },
    '@mui/system': {
      singleton: true,
      eager: true,
      version: pkg.dependencies['@mui/system'],
      requiredVersion: pkg.dependencies['@mui/system'],
    },

    // '@mui/styled-engine': undefined,
    // '@emotion/react': undefined,
    // '@emotion/styled': undefined,

    '@tanstack/react-query': {
      singleton: true,
      requiredVersion: pkg.dependencies['@tanstack/react-query'],
    },
    '@modern-js/runtime': {
      singleton: true,
      requiredVersion: pkg.dependencies['@modern-js/runtime'],
    },
  },
  // Enable runtime plugins for dynamic remote loading
  runtimePlugins: ['./src/runtime/module-federation-plugin.ts'],
});
